using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjetoRetorno.Gameplay;
using ProjetoRetorno.UI;
using ProjetoRetorno.UI.Editor;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjetoRetorno.Level01.Editor
{
    /// <summary>Integra gameplay na nave já montada; não chama o gerador nem altera os pacotes de terceiros.</summary>
    public static class Level01GameplaySetup
    {
        public const string ScenePath = "Assets/Scenes/Level01_Nave_Expanded.unity";
        public const string PrefabFolder = "Assets/Prefabs/Gameplay/Level01";
        public const string MaterialFolder = "Assets/Materials/Gameplay/Level01";
        private const string RootName = "Gameplay_Level01";
        private const string EnergySource = "Assets/Energy/Type 1/EnergyCell_1/Prefabs/EnergyCell_1_1.prefab";
        private const string CardSource = "Assets/Cartao_ARCADIA_Unity/Cartao_Arcadia.obj";
        private const string Mega = "Assets/ThirdParty/Modular SciFi MegaKit[Standard]/Modular SciFi MegaKit[Standard]/FBX (Unity)/";
        private static readonly List<string> Report = new List<string>();

        /// <summary>Sem argumentos/retorno. Preserva o estado aberto, integra a cena existente e atualiza os controles dos menus.</summary>
        [MenuItem("Tools/Gameplay Level 01/Integrar puzzles e corrigir colisoes")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia de Play antes de integrar.");
            Report.Clear();
            Directory.CreateDirectory("Docs/Relatorios/Gameplay");
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                string backup = "Assets/Scenes/Backups/Level01_Antes_Gameplay.unity";
                if (!File.Exists(backup))
                {
                    Directory.CreateDirectory("Assets/Scenes/Backups"); AssetDatabase.Refresh();
                    if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Falha ao preservar a cena aberta.");
                }
                InstallIntoScene(scene);
                UpdateMenus(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Falha ao salvar gameplay.");
                AssetDatabase.SaveAssets();
                File.WriteAllLines("Docs/Relatorios/Gameplay/Integracao.txt", Report);
                Debug.Log("Gameplay do Level 01 integrado na cena existente. A nave não foi regenerada.");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        /// <summary>scene é a nave carregada. Sem retorno. Reutiliza a instalação existente ou adiciona objetos e referências exatamente uma vez.</summary>
        public static void InstallIntoScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) throw new ArgumentException("Cena inválida.");
            Directory.CreateDirectory(PrefabFolder); Directory.CreateDirectory(MaterialFolder); AssetDatabase.Refresh();
            var players = Components<NaveWalkthroughController>(scene);
            if (players.Length != 1) throw new InvalidOperationException("A integração exige o único jogador existente.");
            Transform world = scene.GetRootGameObjects().Single(go => go.name == "Level01_Nave_Expanded").transform;
            PauseMenuController pause = MenuSceneSetup.EnsurePauseMenu(scene);
            FixShellColliders(world);
            var existing = Components<Level01GameManager>(scene);
            if (existing.Length > 1) throw new InvalidOperationException("Gerentes duplicados.");
            if (existing.Length == 1)
            {
                EnsureProgressHUD(existing[0]); EnsureDoorClearance(existing[0].laboratoryDoor); EnsureDoorClearance(existing[0].hangarDoor); MakeDoorDynamic(existing[0].laboratoryDoor); MakeDoorDynamic(existing[0].hangarDoor); MakeDoorDynamic(existing[0].exitDoor); ValidateReferences(existing[0]); Physics.SyncTransforms();
                Report.Add("Instalação existente preservada; nenhum item duplicado."); return;
            }
            Transform root = Group(RootName, world);
            var manager = root.gameObject.AddComponent<Level01GameManager>();
            manager.player = players[0]; manager.pause = pause;
            var interaction = players[0].GetComponent<PlayerInteraction>(); if (interaction == null) interaction = players[0].gameObject.AddComponent<PlayerInteraction>();
            interaction.level = manager; interaction.view = players[0].view; interaction.maximumDistance = 3;
            Transform items = Group("Itens_e_Paineis", root);
            Transform doors = Group("Portas_de_Progressao", root);
            manager.laboratoryDoor = CreateDoor(doors, "Porta_05_Laboratorio", new Vector3(10,0,21), 90, 3.4f, 3.3f);
            manager.hangarDoor = CreateDoor(doors, "Porta_07_Hangar", new Vector3(20,0,49), 0, 4, 3.3f);
            manager.exitDoor = AdaptExit(world, doors);

            GameObject batteryPrefab = EnsureBatteryPrefab();
            GameObject cardPrefab = EnsureCardPrefab();
            // Apoio real do MegaKit, distante da passagem do depósito e acessível antes de qualquer porta bloqueada.
            Piece(Mega + "Props/Prop_ItemHolder.fbx", items, "Suporte_Bateria_Carga", new Vector3(-14,.4f,28), new Vector3(1,.8f,1), 0, true);
            var batteryObject = (GameObject)PrefabUtility.InstantiatePrefab(batteryPrefab, items);
            batteryObject.name = "Bateria_Carga_Esquerda"; batteryObject.transform.position = new Vector3(-14,.81f,28);
            manager.battery = Configure(batteryObject, manager, Level01InteractionKind.Battery);
            manager.energyPanel = CreatePanel(items, manager, "Painel_Instalar_Bateria", new Vector3(-12,0,12), Level01InteractionKind.EnergyPanel);
            var installed = (GameObject)PrefabUtility.InstantiatePrefab(batteryPrefab, manager.energyPanel.transform);
            installed.name = "Bateria_Instalada"; installed.transform.localPosition = new Vector3(.5f,.86f,0);
            // A célula instalada é somente visual, e o alvo interativo continua sendo o painel.
            installed.GetComponent<Level01Interactable>().enabled = false;
            foreach (Collider c in installed.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            manager.installedBattery = installed; installed.SetActive(false);

            var cardObject = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab, items);
            cardObject.name = "Cartao_Seguranca_Laboratorio";
            // Mesa oeste já existente: PC à esquerda e bandeja à direita; o cartão fica na borda dianteira.
            cardObject.transform.SetPositionAndRotation(new Vector3(14,.914f,16.0f), Quaternion.Euler(-90,0,12));
            manager.keycard = Configure(cardObject, manager, Level01InteractionKind.Keycard);

            // Leitor ao lado do acesso norte da Segurança, fora do eixo de passagem.
            manager.cardReader = CreatePanel(items, manager, "Leitor_Cartao_Seguranca", new Vector3(22.5f,0,44.1f), Level01InteractionKind.CardReader);
            var terminals = new[] { new Vector3(16,0,54), new Vector3(24,0,60), new Vector3(16,0,66) };
            string[] descriptions = { "ENERGIA AUXILIAR", "SISTEMA DE SEGURANÇA", "CONTROLE DA SAÍDA" };
            for (int i=0;i<3;i++)
            {
                manager.terminals[i] = CreatePanel(items, manager, "Terminal_" + (i+1) + "_" + descriptions[i], terminals[i], Level01InteractionKind.HangarTerminal);
                manager.terminals[i].terminalIndex = i;
            }
            FaceTerminals(manager);
            CreateHUD(root, manager);
            Transform exitTrigger = Group("Conclusao_Apos_Porta_Externa", root);
            exitTrigger.position = new Vector3(20,1.3f,73);
            var trigger = exitTrigger.gameObject.AddComponent<BoxCollider>(); trigger.isTrigger = true; trigger.size = new Vector3(6.7f,2.6f,1.8f);
            exitTrigger.gameObject.AddComponent<LevelCompleteTrigger>().level = manager;
            Physics.SyncTransforms(); ValidateReferences(manager);
            Report.Add("Cena: " + scene.path);
            Report.Add("Jogador, câmera, controles e menus existentes preservados. Sem regenerar a nave.");
            Report.Add("Bateria: Maquinas/carga à esquerda do depósito (-14, 0.81, 28).");
            Report.Add("Painel de energia: (-12, 0, 12). Cartão sobre mesa oeste do laboratório: (14, 0.914, 16).");
            Report.Add("Leitor: Segurança (22.5, 0, 44.1). Terminais: (16,0,54), (24,0,60), (16,0,66).");
            Report.Add("Conclusão: pequeno deck depois da saída externa, centro (20,1.3,73).");
            Report.Add("Fonte bateria: " + EnergySource); Report.Add("Fonte cartão: " + CardSource);
        }

        /// <summary>world contém os setores. Sem retorno. Corrige cada teto por suas dimensões de piso e verifica folhas decorativas recolhidas.</summary>
        private static void FixShellColliders(Transform world)
        {
            int roofs=0, doors=0;
            foreach (Transform t in world.GetComponentsInChildren<Transform>(true))
            {
                if (t.Find("Ajuste") == null) continue;
                bool roof=t.name.StartsWith("Roof_01_",StringComparison.Ordinal);
                bool door=t.name.StartsWith("Door_Left_01_",StringComparison.Ordinal)||t.name.StartsWith("Door_Simple_",StringComparison.Ordinal)||t.name.StartsWith("Door_DarkMetal_",StringComparison.Ordinal);
                if (!roof && !door) continue;
                foreach(var c in t.GetComponentsInChildren<Collider>(true)) if(c.transform!=t)c.enabled=false;
                var box=t.GetComponent<BoxCollider>(); if(box==null)box=t.gameObject.AddComponent<BoxCollider>();
                if(roof)
                {
                    var floor=t.parent.Cast<Transform>().FirstOrDefault(f=>f.name.StartsWith("Floor_01_") &&
                        Mathf.Abs(f.position.x-t.position.x)<.02f && Mathf.Abs(f.position.z-t.position.z)<.02f);
                    if(floor==null || floor.GetComponent<BoxCollider>()==null)throw new InvalidOperationException("Teto sem piso correspondente: "+t.name);
                    Vector3 size=floor.GetComponent<BoxCollider>().size; size.y=.24f;
                    box.center=Vector3.zero; box.size=size; roofs++;
                }
                else
                {
                    Bounds b=LocalBounds(t); box.center=b.center; box.size=Vector3.Max(b.size,new Vector3(.04f,.04f,.04f)); doors++;
                }
                box.enabled=true; box.isTrigger=false;
            }
            Report.Add("Tetos com BoxCollider na espessura acima do ambiente: "+roofs+". Folhas decorativas sólidas: "+doors+".");
        }

        /// <summary>Pai, nome, posição, rotação e tamanho definem o vão. Retorna uma porta com folha de prefab real e collider móvel.</summary>
        private static DoorController CreateDoor(Transform parent,string name,Vector3 position,float yaw,float width,float height)
        {
            Transform root=Group(name,parent); root.position=position; root.rotation=Quaternion.Euler(0,yaw,0);
            Transform leaf=Piece(Mega+"Platforms/Door_Simple.fbx",root,"Folha_Movel",position+Vector3.up*(height/2)+root.forward*.25f,new Vector3(width+.08f,height,.24f),yaw,true);
            var door=root.gameObject.AddComponent<DoorController>(); door.movingLeaf=leaf; door.solidCollider=leaf.GetComponent<BoxCollider>();
            door.closedLocalPosition=leaf.localPosition; door.openLocalOffset=new Vector3(-width-.45f,0,0); door.SetOpen(false,true); MakeDoorDynamic(door);
            return door;
        }

        /// <summary>world e parent delimitam os objetos existentes e novos. Retorna o controlador da saída e adapta somente a parede atrás do portão.</summary>
        private static DoorController AdaptExit(Transform world,Transform parent)
        {
            Transform hangar=world.Find("07_Hangar"); Transform shell=hangar.Find("Arquitetura");
            foreach(Transform t in shell)
                if(t.Find("Ajuste")!=null && Mathf.Abs(t.position.z-71)<.05f && Mathf.Abs(t.position.x-20)<4)
                    t.gameObject.SetActive(false);
            // Três módulos centrais ficam preservados/inativos. Fechamentos estreitos mantêm a borda do novo vão.
            Piece(Mega+"Walls/WallAstra_Straight.fbx",parent,"Fechamento_Saida_Esquerda",new Vector3(16.08333f,3,71),new Vector3(.83333f,6,.22f),0,true);
            Piece(Mega+"Walls/WallAstra_Straight.fbx",parent,"Fechamento_Saida_Direita",new Vector3(23.91667f,3,71),new Vector3(.83333f,6,.22f),0,true);
            Transform old=hangar.GetComponentsInChildren<Transform>(true).Single(t=>t.name.StartsWith("Door_DarkMetal_")&&t.Find("Ajuste")!=null&&Mathf.Abs(t.position.z-70.7f)<.2f);
            Transform root=Group("Porta_Saida_Externa",parent); root.position=new Vector3(20,0,70.7f);
            old.SetParent(root,true); old.name="Folha_Externa_Existente";
            var door=root.gameObject.AddComponent<DoorController>(); door.movingLeaf=old;
            door.solidCollider=old.GetComponent<BoxCollider>(); if(door.solidCollider==null)door.solidCollider=old.gameObject.AddComponent<BoxCollider>();
            door.solidCollider.center=Vector3.zero; door.solidCollider.size=new Vector3(7,5.6f,.3f); door.solidCollider.isTrigger=false;
            door.closedLocalPosition=old.localPosition; door.openLocalOffset=new Vector3(-7.4f,0,0); door.SetOpen(false,true);
            MakeDoorDynamic(door);
            string bark="Assets/ThirdParty/Barking_Dog/3D Free Modular Kit/Prefabs/";
            Piece(bark+"Floor_01.prefab",parent,"Deck_Conclusao_Piso",new Vector3(20,-.12f,73),new Vector3(7,.24f,4),0,true);
            Piece(bark+"Roof_01.prefab",parent,"Deck_Conclusao_Teto",new Vector3(20,6.12f,73),new Vector3(7,.24f,4),0,true);
            Piece(Mega+"Walls/WallAstra_Straight.fbx",parent,"Deck_Conclusao_Lateral_E",new Vector3(16.5f,3,73),new Vector3(4,6,.22f),90,true);
            Piece(Mega+"Walls/WallAstra_Straight.fbx",parent,"Deck_Conclusao_Lateral_D",new Vector3(23.5f,3,73),new Vector3(4,6,.22f),90,true);
            Piece(Mega+"Walls/WallAstra_Straight.fbx",parent,"Deck_Conclusao_Fundo",new Vector3(20,3,75),new Vector3(7,6,.22f),0,true);
            return door;
        }

        /// <summary>Sem argumentos. Retorna prefab local de bateria, com fonte real e materiais URP próprios; reutiliza o arquivo quando já existe.</summary>
        private static GameObject EnsureBatteryPrefab()
        {
            string path=PrefabFolder+"/Bateria_Energia.prefab";
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(existing!=null)return existing;
            var root=new GameObject("Bateria_Energia");
            try
            {
                Transform visual=Piece(EnergySource,root.transform,"Modelo_Energy_Cells",new Vector3(0,.3f,0),new Vector3(.24f,.6f,.24f),0,false);
                var mat=Material("Bateria_URP",new Color(.9f,.95f,1),Texture("Assets/Energy/Type 1/EnergyCell_1/Textures/EnergyCell_1_1_Albedo.png"));
                mat.SetTexture("_BumpMap",Texture("Assets/Energy/Type 1/EnergyCell_1/Textures/EnergyCell_1_1_Normal.png")); mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_OcclusionMap",Texture("Assets/Energy/Type 1/EnergyCell_1/Textures/EnergyCell_1_1_Occlusion.png"));
                mat.SetTexture("_EmissionMap",Texture("Assets/Energy/Type 1/EnergyCell_1/Textures/EnergyCell_1_1_Emission.png"));
                mat.SetColor("_EmissionColor",Color.cyan*.8f); mat.EnableKeyword("_EMISSION"); EditorUtility.SetDirty(mat);
                foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterials=new[]{mat};
                var box=root.AddComponent<BoxCollider>();box.isTrigger=true;box.center=new Vector3(0,.3f,0);box.size=new Vector3(.28f,.62f,.28f);
                root.AddComponent<Level01Interactable>().kind=Level01InteractionKind.Battery;
                return PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        /// <summary>Sem argumentos. Retorna prefab do cartão existente; separa frente, verso Arcadia_Verso e borda por submalha real.</summary>
        private static GameObject EnsureCardPrefab()
        {
            string path=PrefabFolder+"/Cartao_ARCADIA.prefab";
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(existing!=null)return existing;
            var root=new GameObject("Cartao_ARCADIA");
            try
            {
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(CardSource);
                if(model==null)throw new FileNotFoundException(CardSource);
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(model,root.transform);
                visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;visual.transform.localScale=Vector3.one*3;
                var front=Material("Cartao_Frente_URP",Color.white,Texture("Assets/Cartao_ARCADIA_Unity/Arcadia_Frente.png"));
                var back=Material("Cartao_Verso_URP",Color.white,Texture("Assets/Cartao_ARCADIA_Unity/Arcadia_Verso.png"));
                var edge=Material("Cartao_Borda_URP",new Color(.13f,.14f,.15f));
                foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m!=null&&m.name.ToLowerInvariant().Contains("front")?front:m!=null&&m.name.ToLowerInvariant().Contains("back")?back:edge).ToArray();
                // A espessura visual é real, mas a área do raycast fica levemente ampliada para facilitar a coleta.
                var box=root.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(.285f,.185f,.04f);
                root.AddComponent<Level01Interactable>().kind=Level01InteractionKind.Keycard;
                return PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        /// <summary>Pai, gerente, nome, posição e tipo configuram o terminal. Retorna o interativo sobre um console sci-fi real.</summary>
        private static Level01Interactable CreatePanel(Transform parent,Level01GameManager manager,string name,Vector3 position,Level01InteractionKind kind)
        {
            Transform root=Group(name,parent);root.position=position;
            Transform console=Piece(Mega+"Props/Prop_AccessPoint.fbx",root,"Console_SciFi",position+new Vector3(0,.62f,0),new Vector3(.85f,1.24f,.5f),0,true);
            var interaction=Configure(root.gameObject,manager,kind);
            // Indicador usa o mesmo modelo de painel existente, em escala pequena; sem cubos de arquitetura.
            Transform light=Piece(Mega+"Props/Prop_Light_Small.fbx",root,"Indicador_de_Estado",position+new Vector3(0,1.32f,0),new Vector3(.55f,.12f,.22f),0,false);
            var indicatorMaterial=Material("Indicador_Interacao",new Color(1,.3f,.08f));
            indicatorMaterial.EnableKeyword("_EMISSION");indicatorMaterial.SetColor("_EmissionColor",new Color(.7f,.15f,.02f));
            foreach(var r in light.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>indicatorMaterial).ToArray();
            interaction.indicator=light.GetComponentInChildren<Renderer>(true);
            interaction.SetIndicator(false);
            return interaction;
        }

        /// <summary>Objeto, gerente e tipo são as entradas; retorna o componente reutilizado e conectado à única fonte de progresso.</summary>
        private static Level01Interactable Configure(GameObject go,Level01GameManager manager,Level01InteractionKind kind)
        {
            var component=go.GetComponent<Level01Interactable>(); if(component==null)component=go.AddComponent<Level01Interactable>();
            component.level=manager;component.kind=kind;
            if(PrefabUtility.IsPartOfPrefabInstance(go))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            return component;
        }

        /// <summary>Pai e gerente recebem o Canvas. Sem retorno. Cria HUD e conclusão em UI tradicional com OnClick persistente.</summary>
        private static void CreateHUD(Transform parent,Level01GameManager manager)
        {
            var go=new GameObject("Canvas_Gameplay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            go.transform.SetParent(parent,false);
            Canvas canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=5;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var hud=go.AddComponent<ObjectiveHUD>();manager.hud=hud;hud.level=manager;
            RectTransform page=Rect("HUD",go.transform,Vector2.zero,Vector2.zero);Stretch(page);
            hud.gameplayPage=page.gameObject;
            RectTransform objectivePanel=Rect("PainelObjetivo",page,new Vector2(650,135),new Vector2(32,-32));objectivePanel.anchorMin=objectivePanel.anchorMax=new Vector2(0,1);objectivePanel.pivot=new Vector2(0,1);
            var bg=objectivePanel.gameObject.AddComponent<Image>();bg.color=new Color(.01f,.035f,.06f,.84f);bg.raycastTarget=false;
            hud.objectiveText=Label("Objetivo",objectivePanel,"OBJETIVO\nEncontre uma bateria na nave",new Vector2(600,110),Vector2.zero,26);hud.objectiveText.alignment=TextAnchor.MiddleLeft;
            hud.objectiveText.rectTransform.anchorMin=hud.objectiveText.rectTransform.anchorMax=new Vector2(.5f,.5f);
            EnsureProgressHUD(manager);
            Label("Mira",page,"·",new Vector2(24,30),Vector2.zero,35);
            hud.promptText=Label("Interacao_F",page,"",new Vector2(1100,65),new Vector2(0,100),30);hud.promptText.rectTransform.anchorMin=hud.promptText.rectTransform.anchorMax=new Vector2(.5f,0);
            hud.messageText=Label("Mensagem_Temporaria",page,"",new Vector2(1300,65),new Vector2(0,215),29);hud.messageText.rectTransform.anchorMin=hud.messageText.rectTransform.anchorMax=new Vector2(.5f,0);
            RectTransform complete=Rect("Conclusao",go.transform,Vector2.zero,Vector2.zero);Stretch(complete);
            var dark=complete.gameObject.AddComponent<Image>();dark.color=new Color(.005f,.02f,.035f,.96f);
            Label("Titulo",complete,"LEVEL 01 CONCLUÍDO",new Vector2(1200,110),new Vector2(0,180),64);
            Label("Mensagem",complete,"Você conseguiu escapar da nave.",new Vector2(1200,80),new Vector2(0,65),34);
            var buttonRect=Rect("Voltar_Menu_Principal",complete,new Vector2(650,90),new Vector2(0,-110));
            var image=buttonRect.gameObject.AddComponent<Image>();image.color=new Color(.025f,.16f,.22f);
            var button=buttonRect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.highlightedColor=new Color(.35f,.9f,1);colors.pressedColor=new Color(.2f,.55f,.7f);button.colors=colors;
            Label("Texto",buttonRect,"Voltar ao Menu Principal",new Vector2(620,80),Vector2.zero,32);
            UnityEventTools.AddPersistentListener(button.onClick,manager.ReturnToMainMenu);
            hud.menuButton=button;hud.completionPage=complete.gameObject;complete.gameObject.SetActive(false);
        }

        /// <summary>door é a porta móvel. Sem retorno. Remove flags estáticas das folhas para que renderização e collider se movam juntos também na build.</summary>
        private static void MakeDoorDynamic(DoorController door)
        {
            if(door==null || door.movingLeaf==null)return;
            foreach(var child in door.movingLeaf.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(child.gameObject,0);
        }

        /// <summary>door identifica uma entrada. Sem retorno. Afasta a folha 25 cm da parede para deslizar ao lado dela sem atravessar a parede modular.</summary>
        private static void EnsureDoorClearance(DoorController door)
        {
            if(door==null || (Mathf.Abs(door.closedLocalPosition.z)>.001f && Mathf.Abs(door.closedLocalPosition.z+.25f)>.001f))return;
            door.closedLocalPosition=new Vector3(door.closedLocalPosition.x,door.closedLocalPosition.y,.25f);
            door.SetOpen(false,true);
        }

        /// <summary>manager é o gerente existente. Sem retorno. Acrescenta contador 0/3 a 3/3 editável sem reconstruir o HUD.</summary>
        private static void EnsureProgressHUD(Level01GameManager manager)
        {
            var hud=manager.hud;
            if(hud==null || hud.gameplayPage==null || hud.progressText!=null)return;
            hud.progressText=Label("Progresso_Terminais",hud.gameplayPage.transform,"SISTEMAS DO HANGAR  0/3",new Vector2(650,42),new Vector2(32,-185),24);
            var rect=hud.progressText.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,.5f);
            hud.progressText.alignment=TextAnchor.MiddleLeft;hud.progressText.gameObject.SetActive(false);
        }

        /// <summary>manager contém os terminais. Sem retorno. Orienta os consoles novos para o corredor central do hangar.</summary>
        private static void FaceTerminals(Level01GameManager manager)
        {
            if(manager.terminals==null)return;
            foreach(var terminal in manager.terminals)if(terminal!=null)
                terminal.transform.rotation=Quaternion.Euler(0,terminal.transform.position.x<20?90:-90,0);
        }

        /// <summary>Nome, pai, tamanho e posição definem o RectTransform. Retorna objeto centralizado e editável na Hierarchy.</summary>
        private static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
        }

        /// <summary>rect é o elemento desejado; sem retorno. Faz o elemento preencher o Canvas/painel pai.</summary>
        private static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}

        /// <summary>Nome, pai, texto, tamanho, posição e fonte configuram o rótulo. Retorna Text nativo sem bloquear raycasts de UI.</summary>
        private static Text Label(string name,Transform parent,string value,Vector2 size,Vector2 position,int fontSize)
        {
            var rect=Rect(name,parent,size,position);var label=rect.gameObject.AddComponent<Text>();
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.text=value;label.fontSize=fontSize;
            label.color=new Color(.82f,.96f,1);label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;label.supportRichText=false;
            return label;
        }

        /// <summary>scene é a nave carregada. Sem retorno. Atualiza os painéis existentes de MainMenu, prefab de pausa e instância sem duplicar EventSystems.</summary>
        private static void UpdateMenus(Scene scene)
        {
            var prefab=PrefabUtility.LoadPrefabContents(MenuSceneSetup.PausePrefabPath);
            try { foreach(var panel in prefab.GetComponentsInChildren<ControlsPanelController>(true))UpdateControls(panel);PrefabUtility.SaveAsPrefabAsset(prefab,MenuSceneSetup.PausePrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            foreach(var panel in Components<ControlsPanelController>(scene))UpdateControls(panel);
            Scene main=SceneManager.GetSceneByPath(MenuSceneSetup.MainScenePath);bool opened=!main.IsValid()||!main.isLoaded;
            if(opened)main=EditorSceneManager.OpenScene(MenuSceneSetup.MainScenePath,OpenSceneMode.Additive);
            try
            {
                if(main.isDirty && !File.Exists("Assets/Scenes/Backups/MainMenu_Antes_Gameplay.unity"))
                    EditorSceneManager.SaveScene(main,"Assets/Scenes/Backups/MainMenu_Antes_Gameplay.unity",true);
                foreach(var panel in Components<ControlsPanelController>(main))UpdateControls(panel);
                EditorSceneManager.MarkSceneDirty(main);EditorSceneManager.SaveScene(main);
            }
            finally{if(opened)EditorSceneManager.CloseScene(main,true);SceneManager.SetActiveScene(scene);}
            Report.Add("Controles atualizados nos dois menus: F — Interagir; Ctrl continua Agachar.");
        }

        /// <summary>panel recebe a nova linha. Sem retorno. Reposiciona sete comandos dentro do painel atual e mantém Voltar/OnClick existentes.</summary>
        internal static void UpdateControls(ControlsPanelController panel)
        {
            string[] keys={"W A S D","MOUSE","SHIFT ESQUERDO","ESPAÇO","CTRL (SEGURAR)","F","MOUSE ESQUERDO","ESC"};
            string[] actions={"Movimentação","Olhar ao redor","Correr","Pular","Agachar","Interagir / Pegar caixa","Arremessar caixa","Pausar / Continuar no jogo"};
            for(int i=0;i<keys.Length;i++)
            {
                foreach(string prefix in new[]{"Tecla_","Acao_"})
                {
                    Transform child=panel.transform.Find(prefix+i);
                    if(child==null){var source=panel.transform.Find(prefix+"5");child=UnityEngine.Object.Instantiate(source.gameObject,panel.transform,false).transform;child.name=prefix+i;}
                    var text=child.GetComponent<Text>();text.text=prefix=="Tecla_"?keys[i]:actions[i];
                    var rect=(RectTransform)child;rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,180-i*56);
                    if(PrefabUtility.IsPartOfPrefabInstance(child))PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                    if(PrefabUtility.IsPartOfPrefabInstance(child))PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
                    EditorUtility.SetDirty(text);
                }
            }
        }

        /// <summary>source é caminho real; pai/nome/centro/tamanho/rotação definem o objeto. Retorna wrapper unitário com referência de prefab preservada.</summary>
        internal static Transform Piece(string source,Transform parent,string name,Vector3 center,Vector3 size,float yaw,bool solid)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(source);if(asset==null)throw new FileNotFoundException("Asset não encontrado",source);
            Transform root=Group(name,parent);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset,root);
            instance.transform.localPosition=Vector3.zero;instance.transform.localRotation=Quaternion.identity;instance.transform.localScale=Vector3.one;
            foreach(var a in instance.GetComponentsInChildren<Animator>(true))a.enabled=false;
            foreach(var a in instance.GetComponentsInChildren<Animation>(true)){a.Stop();a.enabled=false;}
            foreach(var c in instance.GetComponentsInChildren<Collider>(true))c.enabled=false;
            foreach(var b in instance.GetComponentsInChildren<Rigidbody>(true)){b.isKinematic=true;b.detectCollisions=false;}
            Bounds bnd=LocalBounds(root);
            if(source.Contains("/Platforms/") && bnd.size.z>bnd.size.x){instance.transform.localRotation=Quaternion.Euler(0,90,0);bnd=LocalBounds(root);}
            Vector3 scale=new Vector3(size.x/Mathf.Max(.00001f,bnd.size.x),bnd.size.y<.0001f?1:size.y/bnd.size.y,size.z/Mathf.Max(.00001f,bnd.size.z));
            instance.transform.localScale=scale;instance.transform.localPosition=-Vector3.Scale(bnd.center,scale);
            if(bnd.size.y<.0001f)instance.transform.localPosition+=Vector3.up*(source.Contains("Roof")?-size.y/2:size.y/2);
            foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials=renderer.sharedMaterials.Select(LocalUrp).ToArray();
            root.SetPositionAndRotation(center,Quaternion.Euler(0,yaw,0));
            if(solid){var box=root.gameObject.AddComponent<BoxCollider>();box.size=size;box.isTrigger=false;}
            return root;
        }

        /// <summary>root define o espaço local. Retorna bounds de vértices reais, incluindo SkinnedMesh na pose estática.</summary>
        internal static Bounds LocalBounds(Transform root)
        {
            Bounds result=default;bool first=true;
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var mf=renderer.GetComponent<MeshFilter>();Bounds b=mf!=null&&mf.sharedMesh!=null?mf.sharedMesh.bounds:renderer.localBounds;
                if(renderer is SkinnedMeshRenderer skin && skin.sharedMesh!=null)
                {
                    var mesh=new Mesh();try{skin.BakeMesh(mesh);mesh.RecalculateBounds();b=mesh.bounds;skin.localBounds=b;}finally{UnityEngine.Object.DestroyImmediate(mesh);}
                }
                for(int i=0;i<8;i++)
                {
                    Vector3 p=root.InverseTransformPoint(renderer.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
                    if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);
                }
            }
            if(first)throw new InvalidOperationException("Modelo sem Renderer: "+root.name);
            return result;
        }

        /// <summary>source identifica o material; retorna cópia local URP ou material URP já compatível, sem editar o original.</summary>
        private static Material LocalUrp(Material source)
        {
            if(source!=null && source.shader!=null && source.shader.name.StartsWith("Universal Render Pipeline/"))return source;
            string guid="default";long id=0;if(source!=null)AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out guid,out id);
            var color=source!=null&&source.HasProperty("_Color")?source.GetColor("_Color"):new Color(.35f,.42f,.48f);
            return Material("Adaptado_"+guid+"_"+id,color,source!=null?source.mainTexture:null);
        }

        /// <summary>Nome, cor e textura definem material local. Retorna asset persistente com shader URP/Lit e textura na propriedade correta.</summary>
        private static Material Material(string name,Color color,Texture texture=null)
        {
            string path=MaterialFolder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color);if(texture!=null)material.SetTexture("_BaseMap",texture);material.SetFloat("_Smoothness",.25f);
            EditorUtility.SetDirty(material);return material;
        }

        /// <summary>path é o caminho real da textura. Retorna o asset ou lança erro explícito quando ausente.</summary>
        private static Texture2D Texture(string path){var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(texture==null)throw new FileNotFoundException(path);return texture;}

        /// <summary>Nome e pai definem o grupo; retorna Transform com escala unitária sem deslocar objetos existentes.</summary>
        private static Transform Group(string name,Transform parent){var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}

        /// <summary>scene delimita a busca. Retorna componentes T inclusive inativos, evitando objetos de outras cenas abertas.</summary>
        private static T[] Components<T>(Scene scene) where T:Component{return scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<T>(true)).ToArray();}

        /// <summary>manager é o gerente configurado. Sem retorno. Rejeita referências ausentes antes de salvar a cena.</summary>
        private static void ValidateReferences(Level01GameManager manager)
        {
            if(manager.player==null||manager.pause==null||manager.hud==null||manager.battery==null||manager.keycard==null||
                manager.energyPanel==null||manager.cardReader==null||manager.installedBattery==null||manager.laboratoryDoor==null||
                manager.hangarDoor==null||manager.exitDoor==null||manager.terminals.Length!=3||manager.terminals.Any(t=>t==null))
                throw new InvalidOperationException("A integração contém referências de gameplay ausentes.");
        }
    }
}
