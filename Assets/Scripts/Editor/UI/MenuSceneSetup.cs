using System;
using System.IO;
using System.Linq;
using ProjetoRetorno.Level01;
using ProjetoRetorno.UI;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjetoRetorno.UI.Editor
{
    /// <summary>Instala UI tradicional nas cenas salvas, sem executar o gerador ou modificar a arquitetura.</summary>
    public static class MenuSceneSetup
    {
        public const string MainScenePath = "Assets/Scenes/MainMenu.unity";
        public const string LevelScenePath = "Assets/Scenes/Level01_Nave.unity";
        public const string PausePrefabPath = "Assets/Prefabs/UI/PauseMenu.prefab";
        private static readonly Color Background = new Color(.018f, .035f, .055f, 1f);
        private static readonly Color Surface = new Color(.025f, .075f, .105f, .98f);
        private static readonly Color Cyan = new Color(.3f, .9f, 1f, 1f);
        private static readonly Color White = new Color(.88f, .95f, .98f, 1f);

        /// <summary>Sem argumentos/retorno. Cria os menus faltantes, conecta a pausa ao jogador e configura a Scene List.</summary>
        [MenuItem("Tools/Configurar Menus")]
        public static void ConfigureMenus()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do Play Mode antes de configurar os menus.");
            if (!File.Exists(LevelScenePath)) throw new FileNotFoundException("A cena do nível precisa existir.", LevelScenePath);
            EnsurePausePrefab();
            Scene previous = SceneManager.GetActiveScene();
            Scene level = SceneManager.GetSceneByPath(LevelScenePath);
            bool openedLevel = !level.IsValid() || !level.isLoaded;
            if (openedLevel) level = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(level);
                EnsurePauseMenu(level);
                if (!EditorSceneManager.SaveScene(level)) throw new IOException("Não foi possível salvar a pausa no nível.");
            }
            finally
            {
                if (openedLevel) EditorSceneManager.CloseScene(level, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
            if (!File.Exists(MainScenePath)) CreateMainMenuScene();
            ConfigureBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Menus configurados. Abra Assets/Scenes/MainMenu.unity e entre em Play. A nave não foi regenerada.");
        }

        /// <summary>scene é a cena do nível; retorna o controlador. Instancia uma única pausa e vincula o jogador existente.</summary>
        public static PauseMenuController EnsurePauseMenu(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) throw new ArgumentException("Cena do nível não está carregada.", nameof(scene));
            EnsurePausePrefab();
            var pauses = Components<PauseMenuController>(scene);
            if (pauses.Length > 1) throw new InvalidOperationException("Há mais de um sistema de pausa na cena.");
            var players = Components<NaveWalkthroughController>(scene);
            if (players.Length != 1) throw new InvalidOperationException("É necessário exatamente um controlador de jogador existente.");
            PauseMenuController pause;
            if (pauses.Length == 0)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PausePrefabPath);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                pause = instance.GetComponent<PauseMenuController>();
            }
            else pause = pauses[0];
            pause.playerController = players[0];
            PrefabUtility.RecordPrefabInstancePropertyModifications(pause);
            EditorUtility.SetDirty(pause);
            EnsureEventSystem(scene);
            return pause;
        }

        /// <summary>scene delimita a busca; retorna todos os componentes T, incluindo objetos inativos, somente nessa cena.</summary>
        private static T[] Components<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }

        /// <summary>scene recebe o processador de cliques; retorna o EventSystem único, com módulo do novo Input System.</summary>
        private static EventSystem EnsureEventSystem(Scene scene)
        {
            EventSystem[] systems = Components<EventSystem>(scene);
            if (systems.Length > 1) throw new InvalidOperationException("EventSystems duplicados: mantenha apenas um na cena.");
            EventSystem system;
            if (systems.Length == 0)
            {
                GameObject go = new GameObject("EventSystem");
                SceneManager.MoveGameObjectToScene(go, scene);
                system = go.AddComponent<EventSystem>();
            }
            else system = systems[0];
            foreach (StandaloneInputModule legacy in system.GetComponents<StandaloneInputModule>()) legacy.enabled = false;
            InputSystemUIInputModule module = system.GetComponent<InputSystemUIInputModule>();
            if (module == null) module = system.gameObject.AddComponent<InputSystemUIInputModule>();
            if (module.actionsAsset == null) module.AssignDefaultActions();
            module.deselectOnBackgroundClick = false;
            module.enabled = true;
            return system;
        }

        /// <summary>Sem argumentos/retorno. Salva o prefab editável de pausa uma única vez, em cena temporária isolada.</summary>
        private static void EnsurePausePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PausePrefabPath) != null) return;
            Directory.CreateDirectory("Assets/Prefabs/UI");
            AssetDatabase.Refresh();
            Scene previous = SceneManager.GetActiveScene();
            Scene temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(temporary);
            try
            {
                GameObject canvas = CreateCanvas("UI_Pausa", 100);
                PauseMenuController controller = canvas.AddComponent<PauseMenuController>();
                RectTransform overlay = Rect("PauseOverlay", canvas.transform, Vector2.zero, Vector2.zero);
                Stretch(overlay, 0);
                Image dim = overlay.gameObject.AddComponent<Image>();
                dim.color = new Color(0, .015f, .03f, .8f);
                dim.raycastTarget = true;
                RectTransform page = Frame("MenuPausa", overlay, new Vector2(860, 900));
                Label("Identificacao", page, "PROJETO RETORNO", new Vector2(0, 380), new Vector2(730, 40), 22, Cyan);
                Label("Titulo", page, "JOGO PAUSADO", new Vector2(0, 310), new Vector2(770, 90), 58, White);
                Line("Divisor", page, new Vector2(0, 250), new Vector2(650, 2));
                Button resume = CreateButton("Continuar", page, "Continuar", 150, 580, controller.Resume);
                Button controls = CreateButton("Controles", page, "Controles", 50, 580, controller.ShowControls);
                CreateButton("ReiniciarNivel", page, "Reiniciar Nível", -50, 580, controller.RestartLevel);
                CreateButton("MenuPrincipal", page, "Menu Principal", -150, 580, controller.GoToMainMenu);
                CreateButton("Sair", page, "Sair", -250, 580, controller.Quit);
                Label("Ajuda", page, "ESC PARA CONTINUAR", new Vector2(0, -370), new Vector2(700, 36), 18, Cyan);
                ControlsPanelController controlsPanel = CreateControls(overlay, page.gameObject, controls);
                controller.overlayRoot = overlay.gameObject;
                controller.menuPage = page.gameObject;
                controller.controlsPanel = controlsPanel;
                controller.continueButton = resume;
                overlay.gameObject.SetActive(false);
                GameObject result = PrefabUtility.SaveAsPrefabAsset(canvas, PausePrefabPath);
                if (result == null) throw new IOException("Não foi possível salvar " + PausePrefabPath);
            }
            finally
            {
                EditorSceneManager.CloseScene(temporary, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        /// <summary>Sem argumentos/retorno. Cria MainMenu com Canvas/Panel/Button/Text e OnClick serializados, sem UI em tempo de execução.</summary>
        private static void CreateMainMenuScene()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                Camera camera = new GameObject("MenuCamera").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Background;
                camera.cullingMask = 0;
                GameObject canvas = CreateCanvas("UI_MenuPrincipal", 0);
                MainMenuController controller = canvas.AddComponent<MainMenuController>();
                RectTransform background = Rect("Fundo", canvas.transform, Vector2.zero, Vector2.zero);
                Stretch(background, 0);
                background.gameObject.AddComponent<Image>().color = Background;
                RectTransform page = Frame("MenuPrincipal", canvas.transform, new Vector2(1600, 900));
                Label("Identificacao", page, "NAVEGAÇÃO // ECOS", new Vector2(0, 360), new Vector2(1350, 40), 20, Cyan);
                Label("Titulo", page, "ECOS DO CRETÁCEO", new Vector2(0, 250), new Vector2(1450, 110), 76, White);
                Label("Subtitulo", page, "PROJETO RETORNO", new Vector2(0, 155), new Vector2(1200, 60), 34, Cyan);
                Line("Divisor", page, new Vector2(0, 95), new Vector2(500, 2));
                Button newGame = CreateButton("NovoJogo", page, "Novo Jogo", 0, 560, controller.NewGame);
                Button controls = CreateButton("Controles", page, "Controles", -110, 560, controller.ShowControls);
                CreateButton("Sair", page, "Sair", -220, 560, controller.Quit);
                Label("Rodape", page, "MENU PRINCIPAL", new Vector2(0, -365), new Vector2(1000, 36), 18, Cyan);
                controller.newGameButton = newGame;
                controller.controlsPanel = CreateControls(canvas.transform, page.gameObject, controls);
                EventSystem system = EnsureEventSystem(scene);
                system.firstSelectedGameObject = newGame.gameObject;
                if (!EditorSceneManager.SaveScene(scene, MainScenePath)) throw new IOException("Não foi possível salvar MainMenu.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        /// <summary>Sem argumentos/retorno. Define MainMenu primeiro e Level01_Nave segundo, incluindo override do perfil ativo se houver.</summary>
        private static void ConfigureBuildScenes()
        {
            var scenes = new[] { new EditorBuildSettingsScene(MainScenePath, true), new EditorBuildSettingsScene(LevelScenePath, true) };
            EditorBuildSettings.scenes = scenes;
            BuildProfile profile = BuildProfile.GetActiveBuildProfile();
            if (profile != null && profile.overrideGlobalScenes)
            {
                profile.scenes = scenes;
                EditorUtility.SetDirty(profile);
            }
        }

        /// <summary>name e order definem o Canvas; retorna objeto Overlay com scaler 1920x1080 e GraphicRaycaster.</summary>
        private static GameObject CreateCanvas(string name, int order)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            return go;
        }

        /// <summary>Nome, pai, tamanho e posição definem um RectTransform centralizado; retorna o objeto editável.</summary>
        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        /// <summary>rect é o elemento e inset sua margem; sem retorno. Expande o elemento pelos limites do pai.</summary>
        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset); rect.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>Nome, pai e tamanho definem o painel; retorna moldura com fundo escuro e detalhes ciano nativos.</summary>
        private static RectTransform Frame(string name, Transform parent, Vector2 size)
        {
            RectTransform rect = Rect(name, parent, size, Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = Surface; image.raycastTarget = false;
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, .3f); outline.effectDistance = new Vector2(1, -1);
            Line("LinhaSuperior", rect, new Vector2(0, size.y / 2 - 22), new Vector2(size.x - 44, 3));
            Line("LinhaInferior", rect, new Vector2(0, -size.y / 2 + 22), new Vector2(size.x - 44, 3));
            return rect;
        }

        /// <summary>Nome, pai, posição e tamanho definem um detalhe; sem retorno. Cria Image sem interceptar cliques.</summary>
        private static void Line(string name, Transform parent, Vector2 position, Vector2 size)
        {
            Image image = Rect(name, parent, size, position).gameObject.AddComponent<Image>();
            image.color = Cyan; image.raycastTarget = false;
        }

        /// <summary>Dados definem um texto nativo; retorna Text com fonte existente, cores legíveis e sem alvo de raycast.</summary>
        private static Text Label(string name, Transform parent, string text, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            Text label = Rect(name, parent, size, position).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text; label.fontSize = fontSize; label.color = color;
            label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        /// <summary>Nome, pai, legenda, altura, largura e ação definem um botão; retorna Button com OnClick persistente visível no Inspector.</summary>
        private static Button CreateButton(string name, Transform parent, string text, float y, float width, UnityAction action)
        {
            RectTransform rect = Rect(name, parent, new Vector2(width, 82), new Vector2(0, y));
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.045f, .16f, .22f, 1f);
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, .65f); outline.effectDistance = new Vector2(1, -1);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(.65f, .93f, 1f);
            colors.selectedColor = new Color(.65f, .93f, 1f); colors.pressedColor = new Color(.35f, .7f, .85f);
            colors.fadeDuration = .1f; button.colors = colors;
            Text label = Label("Texto", rect, text, Vector2.zero, new Vector2(width - 36, 64), 30, White);
            Stretch(label.rectTransform, 8);
            UnityEventTools.AddPersistentListener(button.onClick, action);
            return button;
        }

        /// <summary>Pai, página de origem e botão definem navegação de volta; retorna painel compartilhado com os comandos implementados.</summary>
        private static ControlsPanelController CreateControls(Transform parent, GameObject menuPage, Button controlsButton)
        {
            RectTransform panel = Frame("PainelControles", parent, new Vector2(1060, 900));
            ControlsPanelController controller = panel.gameObject.AddComponent<ControlsPanelController>();
            controller.menuPage = menuPage; controller.controlsButton = controlsButton;
            Label("Titulo", panel, "CONTROLES", new Vector2(0, 335), new Vector2(900, 85), 54, White);
            Line("Divisor", panel, new Vector2(0, 270), new Vector2(850, 2));
            string[] keys = { "W A S D", "MOUSE", "SHIFT ESQUERDO", "ESPAÇO", "CTRL (SEGURAR)", "ESC" };
            string[] actions = { "Movimentação", "Olhar ao redor", "Correr", "Pular", "Agachar", "Pausar / Continuar no jogo" };
            for (int i = 0; i < keys.Length; i++)
            {
                float y = 180 - i * 80;
                Label("Tecla_" + i, panel, keys[i], new Vector2(-265, y), new Vector2(340, 60), 25, Cyan);
                Text action = Label("Acao_" + i, panel, actions[i], new Vector2(205, y), new Vector2(540, 60), 27, White);
                action.alignment = TextAnchor.MiddleLeft;
            }
            controller.backButton = CreateButton("Voltar", panel, "Voltar", -310, 540, controller.Hide);
            panel.gameObject.SetActive(false);
            return controller;
        }
    }
}
