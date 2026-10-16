using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjetoRetorno.Level01;
using ProjetoRetorno.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjetoRetorno.UI.Editor
{
    /// <summary>Verifica os fluxos reais em Play com dispositivos virtuais; nunca regenera a nave nem salva cenas de gameplay.</summary>
    [InitializeOnLoad]
    public static class MenuPlayModeValidation
    {
        private const string Pending = "ProjetoRetorno.MenuTest.Pending";
        private const string Report = "ProjetoRetorno.MenuTest.Report";
        private const string Failed = "ProjetoRetorno.MenuTest.Failed";
        private const string ExpectedQuit = "ProjetoRetorno.MenuTest.ExpectedQuit";
        private const string PreviousStartScene = "ProjetoRetorno.MenuTest.PreviousStartScene";
        private static readonly List<Action> Steps = new List<Action>();
        private static Keyboard keyboard;
        private static Mouse mouse;
        private static Keyboard originalKeyboard;
        private static Mouse originalMouse;
        private static readonly List<InputDevice> mutedDevices = new List<InputDevice>();
        private static InputSettings originalSettings;
        private static InputSettings testSettings;
        private static bool originalRunInBackground;
        private static bool running;
        private static int step;
        private static double nextStep, deadline;
        private static Vector3 pausedPosition;
        private static Quaternion pausedRotation;
        private static int originalPlayerId;
        private static Vector2 pointerPosition;
        private static GameObject crouchCeiling;
        private static float standingCameraY, standingFeetY;

        /// <summary>Sem argumentos/retorno. Registra as transições do Editor para preservar o relatório durante recargas de domínio.</summary>
        static MenuPlayModeValidation()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        /// <summary>Sem argumentos/retorno. Inicia Play por MainMenu sem salvar/substituir a configuração de cenas abertas do usuário.</summary>
        [MenuItem("Tools/Validar Menus em Play")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Execute a validação fora do Play Mode.");
            if (!File.Exists(MenuSceneSetup.MainScenePath)) throw new FileNotFoundException("Configure os menus primeiro.");
            SessionState.SetBool(Pending, true);
            SessionState.SetBool(Failed, false);
            SessionState.SetBool(ExpectedQuit, false);
            SessionState.SetString(Report, "Validação dos menus — Unity " + Application.unityVersion + "\n");
            SessionState.SetString(PreviousStartScene, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuSceneSetup.MainScenePath);
            EditorApplication.isPlaying = true;
        }

        /// <summary>state indica a transição; sem retorno. Executa testes em Play e restaura dispositivos/configuração ao sair.</summary>
        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) Begin();
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.update -= Tick;
                running = false;
                if (!SessionState.GetBool(ExpectedQuit, false) && !SessionState.GetBool(Failed, false))
                {
                    SessionState.SetBool(Failed, true);
                    Append("FALHA: Play foi encerrado antes de concluir os testes.");
                }
                if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
                if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
                foreach (InputDevice device in mutedDevices)
                    if (device.added) InputSystem.EnableDevice(device);
                mutedDevices.Clear();
                if (originalKeyboard != null && originalKeyboard.added) originalKeyboard.MakeCurrent();
                if (originalMouse != null && originalMouse.added) originalMouse.MakeCurrent();
                if (originalSettings != null) InputSystem.settings = originalSettings;
                Application.runInBackground = originalRunInBackground;
                if (testSettings != null) UnityEngine.Object.DestroyImmediate(testSettings);
                Time.timeScale = 1;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                string previous = SessionState.GetString(PreviousStartScene, "");
                EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
                bool passed = !SessionState.GetBool(Failed, false) && SessionState.GetBool(ExpectedQuit, false);
                Append(passed ? "OK: Sair encerrou o Play Mode.\nRESULTADO: APROVADO." : "RESULTADO: FALHOU.");
                Directory.CreateDirectory("Docs");
                File.WriteAllText("Docs/Menus_Validacao.txt", SessionState.GetString(Report, ""));
                SessionState.SetBool(Pending, false);
                Debug.Log(SessionState.GetString(Report, ""));
            }
        }

        /// <summary>Sem argumentos/retorno. Configura entrada virtual apenas em memória e monta a sequência de testes.</summary>
        private static void Begin()
        {
            if (running) return;
            running = true; step = 0;
            originalRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            originalKeyboard = Keyboard.current; originalMouse = Mouse.current;
            // Isola apenas a entrada do teste em Play; restaura os dispositivos ao sair, sem alterar assets/configurações.
            mutedDevices.Clear();
            foreach (InputDevice device in InputSystem.devices.ToArray())
                if (device.enabled && (device is Keyboard || device is Mouse))
                {
                    mutedDevices.Add(device);
                    InputSystem.DisableDevice(device);
                }
            originalSettings = InputSystem.settings;
            testSettings = UnityEngine.Object.Instantiate(originalSettings);
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = testSettings;
            keyboard = InputSystem.AddDevice<Keyboard>("MenuTestKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("MenuTestMouse");
            keyboard.MakeCurrent(); mouse.MakeCurrent();
            Steps.Clear();
            Steps.Add(() => Check(SceneManager.GetActiveScene().name == MenuSceneActions.MainMenuScene, "MainMenu é a entrada em Play"));
            Steps.Add(ValidateUI);
            Steps.Add(() => Capture("01_MainMenu"));
            Click("Controles");
            Steps.Add(() => Check(ActiveControls() != null, "Controles abrem pelo OnClick do Menu Principal"));
            Steps.Add(() => Capture("02_Controles"));
            Click("Voltar");
            Steps.Add(() => Check(ActiveControls() == null, "Voltar retorna ao Menu Principal"));
            Click("NovoJogo");
            Steps.Add(() =>
            {
                Check(SceneManager.GetActiveScene().name == MenuSceneActions.LevelScene, "Novo Jogo carrega Level01_Nave");
                Check(Time.timeScale == 1 && Player().enabled, "Nível inicia com tempo e jogador ativos");
                Check(!Pause().IsPaused && !Pause().overlayRoot.activeSelf, "Pausa começa oculta");
                ValidateUI();
                originalPlayerId = Player().GetInstanceID();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            });
            Steps.Add(() =>
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                Check(Player().transform.position.z > -.9f, "Controle de movimento existente continua funcionando");
            });
            Steps.Add(() =>
            {
                standingCameraY = Player().view.localPosition.y;
                standingFeetY = Player().transform.position.y;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftCtrl));
            });
            Steps.Add(() =>
            {
                CharacterController capsule = Player().GetComponent<CharacterController>();
                Check(Player().IsCrouching && Mathf.Abs(capsule.height - 1.1f) < .01f, "Ctrl esquerdo reduz a cápsula para 1,1 m");
                Check(Player().view.localPosition.y < standingCameraY - .6f && Mathf.Abs(Player().transform.position.y - standingFeetY) < .05f,
                    "Agachar baixa a câmera e mantém os pés no piso");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightCtrl, Key.LeftShift, Key.Space));
            });
            Steps.Add(() =>
            {
                Check(Player().IsCrouching && Mathf.Abs(Player().transform.position.y - standingFeetY) < .05f,
                    "Ctrl direito também agacha e Espaço não permite pular agachado");
                crouchCeiling = new GameObject("Teto temporário de validação", typeof(BoxCollider));
                crouchCeiling.transform.position = Player().transform.position + Vector3.up * 1.5f;
                crouchCeiling.GetComponent<BoxCollider>().size = new Vector3(1f, .2f, 1f);
                Physics.SyncTransforms();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            });
            Steps.Add(() =>
            {
                Check(Player().IsCrouching, "Soltar Ctrl sob teto baixo mantém a cápsula agachada");
                UnityEngine.Object.Destroy(crouchCeiling);
            });
            Steps.Add(() =>
            {
                Check(!Player().IsCrouching && Mathf.Abs(Player().GetComponent<CharacterController>().height - 1.8f) < .01f,
                    "Sem teto, soltar Ctrl restaura a altura original automaticamente");
                Check(Mathf.Abs(Player().view.localPosition.y - standingCameraY) < .01f, "Câmera retorna à altura original");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftCtrl));
            });
            Escape();
            Steps.Add(() =>
            {
                Check(Pause().IsPaused && Time.timeScale == 0 && !Player().enabled, "ESC congela o tempo e bloqueia o controlador");
                Check(Cursor.lockState == CursorLockMode.None && Cursor.visible, "Pausa libera e mostra o cursor");
                Check(Player().IsCrouching && Mathf.Abs(Player().GetComponent<CharacterController>().height - 1.1f) < .01f,
                    "Abrir a pausa preserva o agachamento");
                pausedPosition = Player().transform.position; pausedRotation = Player().transform.rotation;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(120, 50) });
            });
            Steps.Add(() =>
            {
                Check(Vector3.Distance(Player().transform.position, pausedPosition) < .001f && Quaternion.Angle(Player().transform.rotation, pausedRotation) < .001f,
                    "WASD e mouse não movem/giram o jogador durante a pausa");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                Capture("03_Pausa");
            });
            Click("Controles");
            Steps.Add(() => Check(ActiveControls() != null && Time.timeScale == 0, "Cliques e Controles funcionam com timeScale zero"));
            Click("Voltar");
            Steps.Add(() => Check(ActiveControls() == null && Pause().IsPaused, "Voltar mantém a pausa"));
            Escape();
            Steps.Add(() => Check(!Pause().IsPaused && Time.timeScale == 1 && Player().enabled && Cursor.lockState == CursorLockMode.Locked,
                "Segundo ESC retoma o jogo e captura o cursor"));
            Steps.Add(() => Check(!Player().IsCrouching && Mathf.Abs(Player().GetComponent<CharacterController>().height - 1.8f) < .01f,
                "Continuar com Ctrl solto restaura a postura sem perder as dimensões originais"));
            Escape();
            Click("Continuar");
            Steps.Add(() => Check(!Pause().IsPaused && Player().enabled && Time.timeScale == 1, "Continuar retoma o jogo"));
            Escape();
            Click("ReiniciarNivel");
            Steps.Add(() =>
            {
                Check(Player().GetInstanceID() != originalPlayerId && Mathf.Abs(Player().transform.position.z + 1) < .05f, "Reiniciar Nível recria o estado inicial da cena");
                Check(Time.timeScale == 1 && Player().enabled && !Pause().IsPaused, "Reinício não deixa pausa residual");
                ValidateUI();
            });
            Escape();
            Click("MenuPrincipal");
            Steps.Add(() =>
            {
                Check(SceneManager.GetActiveScene().name == MenuSceneActions.MainMenuScene && Time.timeScale == 1, "Menu Principal troca de cena com tempo normal");
                Check(Cursor.visible && Cursor.lockState == CursorLockMode.None, "Menu Principal restaura o cursor");
                ValidateUI();
            });
            Click("NovoJogo");
            Steps.Add(() => Check(SceneManager.GetActiveScene().name == MenuSceneActions.LevelScene && Player().enabled, "Novo Jogo funciona após retornar ao menu"));
            Escape();
            Steps.Add(() => QueueClick("Sair", false));
            Steps.Add(() => QueueClick("Sair", true));
            Steps.Add(() =>
            {
                SessionState.SetBool(ExpectedQuit, true);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = pointerPosition, buttons = 0 });
            });
            Steps.Add(() => throw new InvalidOperationException("Sair não encerrou o Play Mode."));
            nextStep = EditorApplication.timeSinceStartup + 1.5;
            deadline = nextStep + 90;
            EditorApplication.update += Tick;
        }

        /// <summary>Sem argumentos/retorno. Executa passos com intervalo de tempo real, permitindo Input System e UI processarem os eventos.</summary>
        private static void Tick()
        {
            if (!running || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextStep) return;
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Tempo limite da validação.");
                if (step < Steps.Count) Steps[step++]();
                nextStep = EditorApplication.timeSinceStartup + .4;
            }
            catch (Exception exception)
            {
                SessionState.SetBool(Failed, true);
                Append("FALHA: " + exception);
                EditorApplication.isPlaying = false;
            }
        }

        /// <summary>name identifica o botão; sem retorno. Posiciona o ponteiro antes de pressionar/soltar para estabilizar a UI após liberar o cursor.</summary>
        private static void Click(string name)
        {
            Steps.Add(() => QueueClick(name, false));
            Steps.Add(() => QueueClick(name, true));
            Steps.Add(() =>
            {
                // Editor e Player têm buffers de estado distintos; preserva a posição enfileirada para soltar sobre o mesmo botão.
                InputSystem.QueueStateEvent(mouse, new MouseState { position = pointerPosition, buttons = 0 });
            });
        }

        /// <summary>name e down definem o clique; sem retorno. Confere raycast na posição real do botão antes de enfileirar o evento de mouse.</summary>
        private static void QueueClick(string name, bool down)
        {
            Button button = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Single(b => b.name == name && b.isActiveAndEnabled);
            Vector2 position = RectTransformUtility.WorldToScreenPoint(null, button.GetComponent<RectTransform>().position);
            pointerPosition = position;
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
            Check(hits.Any(hit => hit.gameObject.GetComponentInParent<Button>() == button), "Raycast alcança " + name);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = (ushort)(down ? 1 : 0) });
        }

        /// <summary>Sem argumentos/retorno. Acrescenta pressionar/soltar de ESC usando o mesmo Input System do jogador.</summary>
        private static void Escape()
        {
            Steps.Add(() => InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)));
            Steps.Add(() => InputSystem.QueueStateEvent(keyboard, new KeyboardState()));
        }

        /// <summary>Sem argumentos; retorna o controlador de jogador atual após cada troca/reinício de cena.</summary>
        private static NaveWalkthroughController Player()
        {
            return UnityEngine.Object.FindFirstObjectByType<NaveWalkthroughController>(FindObjectsInactive.Include);
        }

        /// <summary>Sem argumentos; retorna a pausa ativa da cena atual.</summary>
        private static PauseMenuController Pause()
        {
            return UnityEngine.Object.FindFirstObjectByType<PauseMenuController>();
        }

        /// <summary>Sem argumentos; retorna o painel de controles visível, ou null quando está fechado.</summary>
        private static ControlsPanelController ActiveControls()
        {
            return UnityEngine.Object.FindFirstObjectByType<ControlsPanelController>();
        }

        /// <summary>Sem argumentos/retorno. Confere EventSystem, scaler, botões persistentes e comandos implementados, incluindo agachar.</summary>
        private static void ValidateUI()
        {
            Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "Um único EventSystem na cena");
            Check(EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() != null, "UI usa o novo Input System");
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                Check(canvas.renderMode == RenderMode.ScreenSpaceOverlay && scaler != null && scaler.referenceResolution == new Vector2(1920, 1080)
                    && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize, "Canvas Overlay com scaler 1920x1080");
            }
            foreach (Button button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Check(button.onClick.GetPersistentEventCount() == 1 && button.onClick.GetPersistentTarget(0) != null, "OnClick serializado: " + button.name);
            foreach (ControlsPanelController panel in UnityEngine.Object.FindObjectsByType<ControlsPanelController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string[] labels = panel.GetComponentsInChildren<Text>(true).Select(text => text.text).ToArray();
                Check(labels.Contains("CTRL (SEGURAR)") && labels.Contains("Agachar"), "Painel de controles inclui Ctrl para agachar");
                Check(!labels.Any(label => label.Contains("Interagir")), "Painel não anuncia interação ausente");
            }
        }

        /// <summary>name nomeia a imagem; sem retorno. Captura a Game View, incluindo a UI Overlay realmente renderizada.</summary>
        private static void Capture(string name)
        {
            Directory.CreateDirectory("Docs/Menu_Previas");
            ScreenCapture.CaptureScreenshot("Docs/Menu_Previas/" + name + ".png");
        }

        /// <summary>condition é o resultado e message descreve a verificação; sem retorno. Registra sucesso ou interrompe em falha.</summary>
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            Append("OK: " + message);
        }

        /// <summary>message é a linha do relatório; sem retorno. Persiste o texto no SessionState entre recargas do Editor.</summary>
        private static void Append(string message)
        {
            SessionState.SetString(Report, SessionState.GetString(Report, "") + message + "\n");
        }
    }
}
