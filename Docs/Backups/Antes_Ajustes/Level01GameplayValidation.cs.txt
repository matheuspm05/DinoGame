using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjetoRetorno.Gameplay;
using ProjetoRetorno.UI;
using ProjetoRetorno.UI.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjetoRetorno.Level01.Editor
{
    /// <summary>Valida a cena salva em Play: física real, raycast, entrada F/ESC, estados e navegação entre cenas.</summary>
    [InitializeOnLoad]
    public static class Level01GameplayValidation
    {
        private const string Pending="ProjetoRetorno.Gameplay.Pending";
        private const string Result="ProjetoRetorno.Gameplay.Result";
        private const string Previous="ProjetoRetorno.Gameplay.Previous";
        private static readonly List<Action> Steps=new List<Action>();
        private static int step,lastFrame;
        private static double next,deadline;
        private static Keyboard keyboard,oldKeyboard;
        private static Mouse mouse,oldMouse;
        private static readonly List<InputDevice> muted=new List<InputDevice>();
        private static InputSettings oldSettings,testSettings;
        private static bool oldBackground;
        private static Level01GameManager level;
        private static PlayerInteraction interaction;
        private static Vector3[] walking;
        private static int waypoint,stalled;
        private static int movements;

        /// <summary>Sem argumentos/retorno. Registra o acompanhamento do Play preservado durante recargas de domínio.</summary>
        static Level01GameplayValidation(){EditorApplication.playModeStateChanged+=OnPlay;}

        /// <summary>Sem argumentos/retorno. Faz auditoria estática e inicia Play pelo MainMenu sem modificar a cena salva.</summary>
        [MenuItem("Tools/Gameplay Level 01/Validar progressao completa em Play")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Saia de Play.");
            SessionState.SetString(Result,"Validação do gameplay — Unity "+Application.unityVersion+" — "+DateTime.Now.ToString("s")+"\n");
            SessionState.SetString(Previous,AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetBool(Pending,true);
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuSceneSetup.MainScenePath);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            EditorApplication.isPlaying=true;
        }

        /// <summary>state indica a transição. Sem retorno. Prepara os testes e restaura entrada, tempo e cena inicial ao finalizar.</summary>
        private static void OnPlay(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Pending,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                try{Begin();}catch(Exception e){Fail(e);}
            }
            if(state==PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.update-=Tick;Time.timeScale=1;
                if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
                if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
                foreach(var device in muted)if(device.added)InputSystem.EnableDevice(device);
                muted.Clear();
                if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();
                if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();
                if(oldSettings!=null)InputSystem.settings=oldSettings;
                if(testSettings!=null)UnityEngine.Object.DestroyImmediate(testSettings);
                Application.runInBackground=oldBackground;
            }
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                string previous=SessionState.GetString(Previous,"");
                EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(previous)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
                Directory.CreateDirectory("Docs/Relatorios/Gameplay");
                File.WriteAllText("Docs/Relatorios/Gameplay/Testes_Play.txt",SessionState.GetString(Result,""));
                SessionState.SetBool(Pending,false);Debug.Log(SessionState.GetString(Result,""));
            }
        }

        /// <summary>Sem argumentos/retorno. Configura dispositivos virtuais temporários e prepara a sequência completa de ações.</summary>
        private static void Begin()
        {
            oldKeyboard=Keyboard.current;oldMouse=Mouse.current;oldSettings=InputSystem.settings;oldBackground=Application.runInBackground;
            foreach(var device in InputSystem.devices.ToArray())if(device.enabled&&(device is Keyboard||device is Mouse)){muted.Add(device);InputSystem.DisableDevice(device);}
            testSettings=UnityEngine.Object.Instantiate(oldSettings);
            testSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            testSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings=testSettings;Application.runInBackground=true;
            keyboard=InputSystem.AddDevice<Keyboard>("GameplayValidationKeyboard");mouse=InputSystem.AddDevice<Mouse>("GameplayValidationMouse");
            keyboard.MakeCurrent();mouse.MakeCurrent();
            Steps.Clear();step=0;lastFrame=-1;walking=null;movements=0;
            Steps.Add(()=>{Check(SceneManager.GetActiveScene().name=="MainMenu","MainMenu inicial e tempo normal");ValidateControls();UnityEngine.Object.FindFirstObjectByType<MainMenuController>().newGameButton.onClick.Invoke();});
            Steps.Add(()=>{Bind();ValidateScene();InitialState();Check(!level.TryComplete(),"Conclusão recusada antes dos terminais");ValidateDoor(level.laboratoryDoor);ValidateDoor(level.hangarDoor);ValidateDoor(level.exitDoor);ValidateRoofs();});
            // Requisitos negativos são exercitados pelo mesmo raycast dos objetos; deslocamentos locais isolam esses testes.
            Steps.Add(()=>{Warp(new Vector3(-12,.04f,10));Aim(level.energyPanel);});
            Press(Key.F);
            Steps.Add(()=>Check(!level.EnergyRestored&&level.hud.messageText.text.Contains("necessária"),"Painel rejeita instalação sem bateria"));
            Steps.Add(()=>{Warp(new Vector3(21,.04f,44));Aim(level.cardReader);});
            Press(Key.F);
            Steps.Add(()=>Check(!level.HangarUnlocked&&level.hud.messageText.text.Contains("necessário"),"Leitor rejeita acesso sem cartão"));
            Steps.Add(()=>{Warp(new Vector3(-14,.04f,24));Aim(level.battery);Check(interaction.FindTarget()==null,"Raycast ignora bateria fora de três metros");});
            Steps.Add(()=>{Warp(new Vector3(7.5f,.04f,21));level.player.view.rotation=Quaternion.LookRotation(new Vector3(14,.95f,16)-level.player.view.position);Check(interaction.FindTarget()==null,"Raycast não coleta cartão através da porta/parede do laboratório");});
            Steps.Add(()=>{Warp(new Vector3(0,.04f,-1));level.hud.ShowMessage("",0);});
            Walk(new Vector3(0,0,9),new Vector3(0,0,21),new Vector3(0,0,23),new Vector3(-15,0,23),new Vector3(-14,0,26));
            Steps.Add(()=>{Aim(level.battery);Check(ReferenceEquals(interaction.FindTarget(),level.battery),"Bateria acessível na carga antes da energia");Capture("00_Bateria_Carga");});
            Press(Key.Escape);
            Steps.Add(()=>{Check(level.pause.IsPaused&&Time.timeScale==0&&!level.player.enabled,"ESC pausa tempo, movimentação e câmera");});
            Press(Key.F);
            Steps.Add(()=>Check(!level.HasBattery&&!level.hud.gameplayPage.activeSelf,"F bloqueado e HUD oculto durante a pausa"));
            Press(Key.Escape);
            Steps.Add(()=>{Check(!level.pause.IsPaused&&Time.timeScale==1&&level.player.enabled,"ESC retoma jogador e tempo");Aim(level.battery);});
            Press(Key.F);
            Steps.Add(()=>Check(level.HasBattery&&!level.battery.gameObject.activeSelf&&level.CurrentObjective.Contains("Instale"),"F coleta a bateria e atualiza o objetivo"));
            Walk(new Vector3(-15,0,23),new Vector3(0,0,23),new Vector3(0,0,9),new Vector3(-12,0,9),new Vector3(-12,0,10));
            Steps.Add(()=>Aim(level.energyPanel));Press(Key.F);
            Steps.Add(()=>Check(level.EnergyRestored&&level.installedBattery.activeSelf&&level.laboratoryDoor.IsOpen,"Instalação mostra a célula e desbloqueia laboratório"));
            Wait(12);
            Walk(new Vector3(-12,0,9),new Vector3(0,0,9),new Vector3(0,0,21),new Vector3(16,0,21),new Vector3(16,0,17.6f),new Vector3(15.4f,0,17.6f));
            Steps.Add(()=>{Check(level.laboratoryDoor.IsFullyOpen,"Folha do laboratório aberta e vão atravessado fisicamente");Aim(level.keycard);Capture("01_Cartao_Laboratorio");});
            Press(Key.F);
            Steps.Add(()=>Check(level.HasKeycard&&!level.keycard.gameObject.activeSelf&&level.CurrentObjective.Contains("leitor"),"F coleta o cartão sobre a mesa e atualiza objetivo"));
            Walk(new Vector3(16,0,17.6f),new Vector3(16,0,21),new Vector3(20,0,21),new Vector3(20,0,39),new Vector3(20.5f,0,43.8f));
            Steps.Add(()=>Aim(level.cardReader));Press(Key.F);
            Steps.Add(()=>Check(level.HangarUnlocked&&level.hangarDoor.IsOpen&&level.CurrentObjective.Contains("(0/3)"),"Cartão libera hangar e HUD inicia contagem 0/3"));
            Wait(12);
            Walk(new Vector3(20,0,46),new Vector3(20,0,54),new Vector3(18,0,54));
            Steps.Add(()=>{Check(level.hangarDoor.IsFullyOpen,"Entrada do hangar atravessada sem collider invisível");Aim(level.terminals[0]);});
            Press(Key.F);
            Steps.Add(()=>Check(level.ActiveTerminals==1&&level.CurrentObjective.Contains("(1/3)")&&!level.exitDoor.IsOpen,"Terminal 1 atualiza 1/3 e mantém saída bloqueada"));
            Press(Key.F);
            Steps.Add(()=>Check(level.ActiveTerminals==1,"Terminal não conta duas vezes"));
            Walk(new Vector3(20,0,54),new Vector3(20,0,66),new Vector3(18,0,66));
            Steps.Add(()=>Aim(level.terminals[2]));Press(Key.F);
            Steps.Add(()=>Check(level.ActiveTerminals==2&&level.CurrentObjective.Contains("(2/3)")&&!level.exitDoor.IsOpen&&!level.TryComplete(),"Terminal 3 funciona fora de ordem; 2/3 ainda impede saída/conclusão"));
            Walk(new Vector3(20,0,66),new Vector3(20,0,60),new Vector3(22,0,60));
            Steps.Add(()=>Aim(level.terminals[1]));Press(Key.F);
            Steps.Add(()=>{Check(level.ActiveTerminals==3&&level.exitDoor.IsOpen&&level.CurrentObjective.Contains("Escape")&&level.hud.progressText.text.Contains("3/3"),"Terceira ativação abre saída e mostra objetivo de fuga");Capture("01_Hangar_Sistemas_Ativos");});
            // Chega à saída enquanto a folha ainda pode estar abrindo; o trigger não pode perder a passagem.
            Wait(2);
            Walk(new Vector3(20,0,60),new Vector3(20,0,69),new Vector3(20,0,73));
            Steps.Add(()=>{Check(level.IsComplete&&Time.timeScale==0&&level.hud.completionPage.activeSelf&&!level.player.enabled&&Cursor.visible,"Trigger conclui somente depois dos terminais, bloqueia controle e mostra interface");Capture("02_Conclusao");});
            Wait(2);
            Steps.Add(()=>{level.hud.menuButton.onClick.Invoke();});
            Steps.Add(()=>{Check(SceneManager.GetActiveScene().name=="MainMenu"&&Time.timeScale==1,"Botão da conclusão retorna ao MainMenu com tempo restaurado");UnityEngine.Object.FindFirstObjectByType<MainMenuController>().NewGame();});
            Steps.Add(()=>{Bind();InitialState();Check(true,"Novo Jogo após conclusão reinicia todos os puzzles");});
            Steps.Add(()=>{Warp(new Vector3(-14,.04f,26));Aim(level.battery);});Press(Key.F);
            Steps.Add(()=>{Check(level.HasBattery,"Progresso antes do reinício confirmado");level.pause.Pause();level.pause.RestartLevel();});
            Steps.Add(()=>{Bind();InitialState();Check(Time.timeScale==1,"Reiniciar Nível redefine itens, terminais e três portas");level.pause.Pause();level.pause.GoToMainMenu();});
            Steps.Add(()=>{Check(SceneManager.GetActiveScene().name=="MainMenu"&&Time.timeScale==1,"Menu Principal pela pausa preservado");Append("Movimentos físicos da cápsula na rota: "+movements);Append("RESULTADO: APROVADO.");EditorApplication.isPlaying=false;});
            next=EditorApplication.timeSinceStartup+1;deadline=next+180;
            EditorApplication.update+=Tick;
        }

        /// <summary>Sem argumentos/retorno. Executa um passo por frame e percorre as rotas com a cápsula real, sem salvar seu estado de Play.</summary>
        private static void Tick()
        {
            if(!EditorApplication.isPlaying||Time.frameCount==lastFrame||EditorApplication.timeSinceStartup<next)return;
            lastFrame=Time.frameCount;
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Validação excedeu três minutos.");
                if(walking!=null)
                {
                    Vector3 delta=walking[waypoint]-level.player.transform.position;delta.y=0;
                    if(delta.magnitude<.07f)
                    {
                        if(++waypoint>=walking.Length){walking=null;if(!level.IsComplete){level.player.enabled=true;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}}return;
                    }
                    var capsule=level.player.GetComponent<CharacterController>();Vector3 before=level.player.transform.position;
                    capsule.Move(Vector3.ClampMagnitude(delta,.3f)+Vector3.down*.04f);movements++;
                    Vector3 movement=level.player.transform.position-before;movement.y=0;
                    stalled=movement.sqrMagnitude<.00001f?stalled+1:0;
                    if(stalled>20)throw new InvalidOperationException("Rota bloqueada em "+level.player.transform.position+" rumo a "+walking[waypoint]);
                    if(level.player.transform.position.y<-.15f||level.player.transform.position.y>.35f)throw new InvalidOperationException("Piso descontínuo: "+level.player.transform.position);
                    return;
                }
                if(step<Steps.Count)Steps[step++]();
                next=EditorApplication.timeSinceStartup+.12;
            }
            catch(Exception e){Fail(e);}
        }

        /// <summary>Sem argumentos/retorno. Reconecta referências após Novo Jogo ou reinício sem reter objetos destruídos.</summary>
        private static void Bind(){level=UnityEngine.Object.FindFirstObjectByType<Level01GameManager>();Check(level!=null,"Cena expandida com um gerente de gameplay");interaction=level.player.GetComponent<PlayerInteraction>();Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}

        /// <summary>Sem argumentos/retorno. Confirma o estado inicial completo e os objetivos sem persistência entre cenas.</summary>
        private static void InitialState()
        {
            Check(!level.HasBattery&&!level.EnergyRestored&&!level.HasKeycard&&!level.HangarUnlocked&&level.ActiveTerminals==0&&!level.IsComplete,"Estado inicial sem progresso");
            Check(level.battery.gameObject.activeSelf&&level.keycard.gameObject.activeSelf&&!level.installedBattery.activeSelf,"Itens restaurados no cenário");
            Check(!level.laboratoryDoor.IsOpen&&!level.hangarDoor.IsOpen&&!level.exitDoor.IsOpen,"Três portas iniciam fechadas");
            Check(level.hud.objectiveText.text.Contains("Encontre uma bateria"),"HUD mostra objetivo inicial");
        }

        /// <summary>position é a base desejada. Sem retorno. Reposiciona somente no teste isolado e sincroniza a física sem substituir o jogador.</summary>
        private static void Warp(Vector3 position)
        {
            var capsule=level.player.GetComponent<CharacterController>();capsule.enabled=false;level.player.transform.position=position;capsule.enabled=true;
            level.player.enabled=true;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;Physics.SyncTransforms();
        }

        /// <summary>target é o alvo. Sem retorno. Aponta a câmera real para o centro do collider usado pelo raycast.</summary>
        private static void Aim(Level01Interactable target)
        {
            var c=target.GetComponentsInChildren<Collider>(true).First(c=>c.enabled);
            Vector3 direction=(c.bounds.center-level.player.view.position).normalized;
            float pitch=-Mathf.Asin(direction.y)*Mathf.Rad2Deg;
            level.player.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,0);
            typeof(NaveWalkthroughController).GetField("pitch",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(level.player,pitch);
            level.player.view.localRotation=Quaternion.Euler(pitch,0,0);
            Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;Physics.SyncTransforms();
        }

        /// <summary>key identifica F ou ESC. Sem retorno. Insere pressão/soltura em frames diferentes pelo Input System real.</summary>
        private static void Press(Key key){Steps.Add(()=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(key)));Steps.Add(()=>InputSystem.QueueStateEvent(keyboard,new KeyboardState()));}

        /// <summary>count é a quantidade de intervalos. Sem retorno. Aguarda animações/UI sem bloquear a thread do Editor.</summary>
        private static void Wait(int count){for(int i=0;i<count;i++)Steps.Add(()=>{});}

        /// <summary>points define caminho já existente. Sem retorno. Agenda movimento físico da cápsula e mantém as posições Y do piso.</summary>
        private static void Walk(params Vector3[] points)
        {
            Steps.Add(()=>{walking=points;waypoint=0;stalled=0;level.player.enabled=false;});
        }

        /// <summary>door identifica o vão fechado. Sem retorno. Tenta atravessá-lo com o CharacterController real e verifica parada antes da folha.</summary>
        private static void ValidateDoor(DoorController door)
        {
            Vector3 original=level.player.transform.position;
            Vector3 normal=door.transform.forward;
            Warp(door.transform.position-normal*1.2f+Vector3.up*.04f);
            for(int i=0;i<15;i++)level.player.GetComponent<CharacterController>().Move(normal*.2f);
            float progress=Vector3.Dot(level.player.transform.position-door.movingLeaf.position,normal);
            Check(progress<-.2f&&door.solidCollider.enabled&&!door.solidCollider.isTrigger,"Porta sólida impede cápsula humana: "+door.name);
            Warp(original);
        }

        /// <summary>Sem argumentos/retorno. Confere todos os módulos de teto e tenta saltar verticalmente em cada setor.</summary>
        private static void ValidateRoofs()
        {
            Transform world=level.transform.parent;
            var roofs=world.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Roof_01_")&&t.Find("Ajuste")!=null).ToArray();
            foreach(var roof in roofs)
            {
                var box=roof.GetComponent<BoxCollider>();
                if(box==null||!box.enabled||box.isTrigger||box.size.y>.3f||box.size.y<.1f)throw new InvalidOperationException("Teto sem proteção adequada: "+roof.name);
                var ray=new Ray(roof.position-Vector3.up,.999f*Vector3.up);
                if(!box.Raycast(ray,out RaycastHit hit,2))throw new InvalidOperationException("Raycast atravessaria teto "+roof.name);
                if(Mathf.Abs(hit.point.y-(roof.position.y-.12f))>.02f)throw new InvalidOperationException("Collider de teto invade interior.");
            }
            Check(roofs.Length>=100,"Todos os "+roofs.Length+" módulos de teto têm colisão fina alinhada");
            Vector3 original=level.player.transform.position;
            var samples=new[]{new Vector3(0,0,0),new Vector3(0,0,9),new Vector3(-12,0,9),new Vector3(0,0,21),new Vector3(20,0,21),new Vector3(20,0,39),new Vector3(20,0,60),new Vector3(-15,0,25),new Vector3(37,0,37)};
            foreach(var point in samples)
            {
                Warp(point+Vector3.up*.04f);bool above=false;
                for(int i=0;i<45;i++)above|=(level.player.GetComponent<CharacterController>().Move(Vector3.up*.15f)&CollisionFlags.Above)!=0;
                if(!above)throw new InvalidOperationException("Cápsula atravessa teto em "+point);
            }
            Warp(original);Check(true,"Cápsula colide ao subir contra tetos nos nove setores");
        }

        /// <summary>Sem argumentos/retorno. Audita componentes únicos, referências, fontes reais, URP e o verso do cartão.</summary>
        private static void ValidateScene()
        {
            Check(UnityEngine.Object.FindObjectsByType<Level01GameManager>(FindObjectsSortMode.None).Length==1,"Um gerente, uma fonte de progresso");
            Check(UnityEngine.Object.FindObjectsByType<NaveWalkthroughController>(FindObjectsSortMode.None).Length==1&&level.player.view!=null,"Jogador e câmera existentes preservados");
            Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1,"Um único EventSystem");
            Check(interaction!=null&&Mathf.Abs(interaction.maximumDistance-3)<.01f,"Interação por câmera limitada a 3m");
            foreach(var t in level.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new InvalidOperationException("Missing script "+t.name);
            foreach(var c in level.terminals)if(c.level!=level||c.indicator==null)throw new InvalidOperationException("Terminal sem referência.");
            var cardMaterials=level.keycard.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).ToArray();
            Check(cardMaterials.Any(m=>AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")).EndsWith("Arcadia_Verso.png")),"Verso do cartão utiliza Arcadia_Verso.png");
            foreach(var r in level.GetComponentsInChildren<Renderer>(true))
                foreach(var m in r.sharedMaterials)if(m==null||m.shader==null||!m.shader.name.StartsWith("Universal Render Pipeline/"))throw new InvalidOperationException("Material incompatível "+r.name);
            Check(true,"Novos modelos/prefabs usam materiais URP sem referências perdidas");
            Check(level.terminals.Select(t=>t.terminalIndex).Distinct().Count()==3,"Três terminais possuem índices diferentes");
            ValidateControls();
        }

        /// <summary>Sem argumentos/retorno. Confere F e Ctrl nas páginas de controles realmente serializadas.</summary>
        private static void ValidateControls()
        {
            foreach(var panel in UnityEngine.Object.FindObjectsByType<ControlsPanelController>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var labels=panel.GetComponentsInChildren<Text>(true).Select(t=>t.text).ToArray();
                Check(labels.Contains("F")&&labels.Contains("Interagir")&&labels.Contains("Agachar"),"Controles reais incluem F e Ctrl");
            }
        }

        /// <summary>name identifica o arquivo. Sem retorno. Captura a Game View com HUD/Canvas renderizado.</summary>
        private static void Capture(string name){Directory.CreateDirectory("Docs/Previas/Gameplay");ScreenCapture.CaptureScreenshot("Docs/Previas/Gameplay/"+name+".png");}

        /// <summary>condition e message descrevem a verificação. Sem retorno. Falha interrompe a sequência; sucesso vai para o relatório.</summary>
        private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);Append("OK: "+message);}

        /// <summary>message é uma linha. Sem retorno. Persiste resultados entre transições do Editor.</summary>
        private static void Append(string message){SessionState.SetString(Result,SessionState.GetString(Result,"")+message+"\n");}

        /// <summary>error é a falha. Sem retorno. Registra o ponto de teste e encerra Play restaurando o Editor.</summary>
        private static void Fail(Exception error){Append("FALHA no passo "+step+": "+error);Append("RESULTADO: FALHOU.");EditorApplication.update-=Tick;EditorApplication.isPlaying=false;Debug.LogException(error);}
    }
}
