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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjetoRetorno.Level01.Editor
{
    /// <summary>Adiciona somente os novos sistemas e refaz hitboxes de acordo com os modelos atuais, sem gerar a nave.</summary>
    public static class Level01AdjustmentsSetup
    {
        public const string ScenePath = Level01GameplaySetup.ScenePath;
        public const string SparrowPath = "Assets/Quirky Series Ultimate/FREE/Prefabs/Sparrow.prefab";
        private const string Mega = "Assets/ThirdParty/Modular SciFi MegaKit[Standard]/Modular SciFi MegaKit[Standard]/FBX (Unity)/";
        private static readonly List<string> Report = new List<string>();

        private sealed class OriginalPose
        {
            public Transform transform;
            public Vector3 position, scale;
            public Quaternion rotation;
        }

        /// <summary>Sem argumentos/retorno. Preserva a cena aberta, integra objetos novos e salva sem tocar no layout ou nos modelos existentes.</summary>
        [MenuItem("Tools/Gameplay Level 01/Integrar arremesso pet e ajustes")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia de Play.");
            Report.Clear(); Directory.CreateDirectory("Docs/Relatorios/Ajustes_Nivel01");
            Scene previous=SceneManager.GetActiveScene();
            Scene scene=SceneManager.GetSceneByPath(ScenePath);
            bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                string backup="Assets/Scenes/Backups/Level01_Antes_Ajustes.unity";
                if(!File.Exists(backup)){Directory.CreateDirectory("Assets/Scenes/Backups");EditorSceneManager.SaveScene(scene,backup,true);}
                var poses=Components<Renderer>(scene).Select(r=>new OriginalPose{transform=r.transform,position=r.transform.position,rotation=r.transform.rotation,scale=r.transform.lossyScale}).ToArray();
                InstallIntoScene(scene); UpdateControls(scene);
                foreach(var pose in poses)
                    if(pose.transform==null || Vector3.Distance(pose.position,pose.transform.position)>.0001f ||
                        Quaternion.Angle(pose.rotation,pose.transform.rotation)>.001f || Vector3.Distance(pose.scale,pose.transform.lossyScale)>.0001f)
                        throw new InvalidOperationException("Um modelo existente foi deslocado: "+(pose.transform==null?"removido":pose.transform.name));
                Report.Add("Posições, rotações e escalas de "+poses.Length+" renderizadores existentes preservadas.");
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
                File.WriteAllLines("Docs/Relatorios/Ajustes_Nivel01/Integracao.txt",Report);
                Debug.Log("Arremesso, spawn do cartão, Sparrow e luz integrados; cenário não regenerado.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
        }

        /// <summary>scene é a nave pronta. Sem retorno. Conecta a extensão uma única vez; reinstalar preserva a posição dos elementos e não duplica sistemas.</summary>
        public static void InstallIntoScene(Scene scene)
        {
            var managers=Components<Level01GameManager>(scene);
            if(managers.Length!=1)throw new InvalidOperationException("A nave deve ter exatamente um gerente de gameplay.");
            var level=managers[0];
            FixHitboxes(level);
            var existing=Components<IntroThrowingSystem>(scene);
            if(existing.Length>1)throw new InvalidOperationException("Sistemas de arremesso duplicados.");
            if(existing.Length==0)
            {
                CreateInitialRoom(level);
                CreateCardSpawn(level);
                CreatePet(level);
                CreateExitLight(level);
                var title=level.hud.completionPage.GetComponentsInChildren<Text>(true).First(t=>t.text.Contains("CONCLU"));
                title.text="NÍVEL 1 CONCLUÍDO";
            }
            else if(level.initialRoom==null || level.cardSpawn==null || level.exitLight==null || Components<PetFollower>(scene).Length!=1)
                throw new InvalidOperationException("Extensão incompleta. Reveja as referências sem remover os modelos existentes.");
            EnsureImpactReceiver(level.initialRoom); SparrowMaterial(); ConfigureExit(level);
            if(level.hud.messageText.GetComponent<Outline>()==null)
            { var outline=level.hud.messageText.gameObject.AddComponent<Outline>();outline.effectColor=new Color(0,.02f,.04f,1);outline.effectDistance=new Vector2(1.5f,-1.5f); }
            var completionImage=level.hud.completionPage.GetComponent<Image>();if(completionImage!=null){var color=completionImage.color;color.a=1;completionImage.color=color;}
            Physics.SyncTransforms();
            EnsureValidSpawnPoints(level.cardSpawn);
            for(int i=0;i<level.cardSpawn.candidates.Length;i++)
                if(!level.cardSpawn.IsCandidateValid(i))throw new InvalidOperationException("Ponto do cartão obstruído ou sem apoio: "+i);
            if(level.cardSpawn.candidates.Length<3)throw new InvalidOperationException("São necessários ao menos três pontos válidos.");
            Report.Add("Cena: "+scene.path+"; um gerente, um jogador e um EventSystem reutilizados.");
            Report.Add("Sparrow original: "+SparrowPath+"; controller AC_Sparrow e clipe Base Layer.Fly.");
            Report.Add("Bateria preservada em "+level.battery.transform.position+"; hitboxes seguem os modelos movidos manualmente.");
            Report.Add("Cartão: "+level.cardSpawn.candidates.Length+" candidatos sobre mesas existentes; sorteio somente em execução.");
        }

        /// <summary>level fornece os objetos atuais. Sem retorno. Mede somente o visual de cada alvo e ajusta seu collider sem alterar transforms.</summary>
        private static void FixHitboxes(Level01GameManager level)
        {
            foreach(var item in new[]{level.battery,level.keycard,level.energyPanel,level.cardReader}.Concat(level.terminals))
            {
                bool pickup=item.kind==Level01InteractionKind.Battery||item.kind==Level01InteractionKind.Keycard;
                Transform visual=pickup?item.transform:item.transform.Find("Console_SciFi");
                if(visual==null)throw new InvalidOperationException("Console visual não encontrado: "+item.name);
                var box=visual.GetComponent<BoxCollider>();if(box==null)box=visual.gameObject.AddComponent<BoxCollider>();
                Bounds bounds=Level01GameplaySetup.LocalBounds(visual);
                box.center=bounds.center;
                box.size=bounds.size+(pickup?new Vector3(.025f,.025f,.018f):new Vector3(.01f,.01f,.01f));
                box.isTrigger=pickup;box.enabled=true;
                foreach(var other in visual.GetComponentsInChildren<Collider>(true))if(other!=box)other.enabled=false;
                Report.Add("Hitbox "+item.name+" centro local="+box.center+" tamanho="+box.size+" trigger="+box.isTrigger);
            }
            foreach(var door in new[]{level.laboratoryDoor,level.hangarDoor,level.exitDoor})
            {
                var bounds=Level01GameplaySetup.LocalBounds(door.movingLeaf);
                door.solidCollider.center=bounds.center;
                door.solidCollider.size=Vector3.Max(bounds.size,new Vector3(.18f,.18f,.18f));
                door.solidCollider.enabled=true;door.solidCollider.isTrigger=false;
            }
            // A bateria instalada continua no lugar original no Editor; o encaixe real é usado apenas na instalação em runtime.
            if(level.batterySocket==null)
            {
                Transform console=level.energyPanel.transform.Find("Console_SciFi");
                var socket=Group("BatterySocket",console);
                var box=console.GetComponent<BoxCollider>();
                socket.position=box.bounds.center+Vector3.right*(box.bounds.extents.x+.13f)+Vector3.down*.3f;
                socket.rotation=level.installedBattery.transform.rotation;
                level.batterySocket=socket;
            }
        }

        /// <summary>level é o gerente atual. Sem retorno. Acrescenta porta adequada, alvo, caixa e HoldPoint sem mover a folha decorativa estreita existente.</summary>
        private static void CreateInitialRoom(Level01GameManager level)
        {
            Transform world=level.transform.parent;
            Transform room=world.Find("01_Contencao");
            Transform frame=room.GetComponentsInChildren<Transform>(true).First(t=>t.name.StartsWith("Door_Frame_Square_")&&t.Find("Ajuste")!=null);
            Vector3 doorway=new Vector3(frame.position.x,0,frame.position.z);
            Transform root=Group("InitialRoomMechanics",level.transform);
            Transform doorRoot=Group("Porta_Inicial",root);doorRoot.position=doorway;
            var leaf=Level01GameplaySetup.Piece(Mega+"Platforms/Door_Simple.fbx",doorRoot,"Folha_Movel",doorway+new Vector3(0,1.5f,-.25f),new Vector3(3.08f,3,.24f),0,true);
            var door=doorRoot.gameObject.AddComponent<DoorController>();door.movingLeaf=leaf;door.solidCollider=leaf.GetComponent<BoxCollider>();
            door.closedLocalPosition=leaf.localPosition;door.openLocalOffset=new Vector3(-3.45f,0,0);
            foreach(var t in leaf.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
            var throwingRoot=Group("ThrowingSystem",root);var intro=throwingRoot.gameObject.AddComponent<IntroThrowingSystem>();
            level.initialRoom=intro;intro.level=level;intro.initialDoor=door;
            intro.projectilesRoot=Group("Caixas_Arremessadas",throwingRoot);
            intro.holdPoint=Group("HoldPoint",level.player.view);intro.holdPoint.localPosition=new Vector3(.4f,-.28f,.85f);
            intro.throwablePrefabs=new[]{
                EnsureThrowable("Caixa_Crate3",Mega+"Props/Prop_Crate3.fbx"),
                EnsureThrowable("Caixa_Storage","Assets/ThirdParty/BrokenVector/LowPolyStoragePack/Prefabs/Crate_01.prefab")
            };
            var initial=(GameObject)PrefabUtility.InstantiatePrefab(intro.throwablePrefabs[0],root);
            initial.name="Caixa_Inicial";initial.transform.position=room.position+new Vector3(-1.2f,.22f,.5f);
            initial.AddComponent<ThrowablePickup>().owner=intro;intro.initialBox=initial;
            var buttonRoot=Group("Alvo_Impacto",root);buttonRoot.position=doorway+new Vector3(0,3.34f,-.5f);
            var face=Level01GameplaySetup.Piece(Mega+"Props/Prop_Light_Small.fbx",buttonRoot,"Botao_SciFi",buttonRoot.position,new Vector3(.72f,.48f,.18f),0,true);
            var button=face.gameObject.AddComponent<ImpactButton>();button.owner=intro;button.indicator=face.GetComponentInChildren<Renderer>(true);
            var material=LocalMaterial("Alvo_Impacto_URP",new Color(.1f,.7f,1));
            material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(.1f,.7f,1)*2);
            foreach(var r in face.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>material).ToArray();
            intro.impactButton=button;button.SetActiveIndicator(false);door.SetOpen(false,true);
            Report.Add("Porta inicial nova: a folha decorativa existente é estreita/recolhida e foi preservada.");
            Report.Add("Alvo de impacto: "+buttonRoot.position+"; caixa inicial: "+initial.transform.position+".");
        }

        /// <summary>name e source identificam o modelo real. Retorna prefab local de pequeno objeto com Rigidbody, BoxCollider e identificação de arremesso.</summary>
        private static GameObject EnsureThrowable(string name,string source)
        {
            string folder=Level01GameplaySetup.PrefabFolder+"/Throwing";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            string path=folder+"/"+name+".prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab!=null)return prefab;
            var root=new GameObject(name);
            try
            {
                var visual=Level01GameplaySetup.Piece(source,root.transform,"Modelo",Vector3.zero,Vector3.one*.32f,0,true);
                foreach(var child in root.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(child.gameObject,0);
                var body=root.AddComponent<Rigidbody>();body.mass=.65f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.interpolation=RigidbodyInterpolation.Interpolate;
                var projectile=root.AddComponent<ThrownObject>();projectile.body=body;projectile.solidCollider=visual.GetComponent<BoxCollider>();
                foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))renderer.shadowCastingMode=ShadowCastingMode.Off;
                return PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }

        /// <summary>level fornece o único cartão. Sem retorno. Cria três pontos de bancada com apoio, região interna e gatilho de visita do laboratório.</summary>
        private static void CreateCardSpawn(Level01GameManager level)
        {
            Transform root=Group("CardSpawnSystem",level.transform);var spawn=root.gameObject.AddComponent<CardSpawnSystem>();
            spawn.level=level;level.cardSpawn=spawn;
            Transform lab=level.transform.parent.Find("05_Laboratorio");
            var floors=lab.GetComponentsInChildren<BoxCollider>(true).Where(c=>c.enabled&&c.name.StartsWith("Floor_01_")).ToArray();
            Bounds area=floors[0].bounds;foreach(var floor in floors.Skip(1))area.Encapsulate(floor.bounds);
            spawn.laboratoryBounds=new Bounds(new Vector3(area.center.x,1.5f,area.center.z),new Vector3(area.size.x-.5f,3,area.size.z-.5f));
            var benches=lab.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Office_Table Metal Variant_")&&t.Find("Ajuste")!=null).OrderBy(t=>t.position.z).ThenBy(t=>t.position.x).ToArray();
            if(benches.Length<3)throw new InvalidOperationException("Três mesas reais do laboratório não foram encontradas.");
            spawn.candidates=new CardSpawnPoint[3];
            for(int i=0;i<3;i++)
            {
                var support=benches[i].GetComponent<BoxCollider>();if(support==null)throw new InvalidOperationException("Mesa sem apoio físico.");
                Transform point=Group("Ponto_Cartao_"+(i+1),root);
                point.SetPositionAndRotation(new Vector3(support.bounds.center.x,support.bounds.max.y+.006f,support.bounds.min.z+.18f),level.keycard.transform.rotation);
                spawn.candidates[i]=new CardSpawnPoint{point=point,support=support};
                Report.Add(point.name+" "+point.position+" apoio="+benches[i].name);
            }
            Transform entry=Group("Entrada_Laboratorio",root);entry.position=new Vector3(area.min.x+1.2f,1.3f,area.center.z);
            var trigger=entry.gameObject.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(1.6f,2.6f,3);
            entry.gameObject.AddComponent<LaboratoryEntryTrigger>().level=level;
        }

        /// <summary>level contém a saída existente. Sem retorno. Restaura o volume de conclusão como trigger ativo na região da luz, sem criar outro sistema.</summary>
        private static void ConfigureExit(Level01GameManager level)
        {
            var trigger=Components<LevelCompleteTrigger>(level.gameObject.scene).Single();
            trigger.level=level;trigger.enabled=true;trigger.gameObject.SetActive(true);
            trigger.transform.SetPositionAndRotation(new Vector3(level.exitDoor.transform.position.x,1.3f,level.exitDoor.transform.position.z+2.6f),Quaternion.identity);
            trigger.transform.localScale=Vector3.one;
            var box=trigger.GetComponent<BoxCollider>();box.center=Vector3.zero;box.size=new Vector3(6.5f,2.6f,.8f);box.isTrigger=true;box.enabled=true;
            var surface=level.exitLight.GetComponentInChildren<MeshRenderer>(true);
            var material=surface.sharedMaterial;
            material.shader=Shader.Find("Universal Render Pipeline/Unlit");
            material.SetColor("_BaseColor",Color.white*2);material.SetFloat("_Cull",0);
            EditorUtility.SetDirty(material);
            Report.Add("Trigger final ativo: "+box.bounds+" referência ao gerente="+(trigger.level==level));
        }

        /// <summary>intro contém o alvo. Sem retorno. Mantém o receptor de impacto no mesmo GameObject do collider estático que recebe OnCollisionEnter.</summary>
        private static void EnsureImpactReceiver(IntroThrowingSystem intro)
        {
            var old=intro.impactButton;
            if(old.GetComponent<Collider>()!=null)return;
            var collider=old.GetComponentsInChildren<BoxCollider>(true).First(c=>c.enabled);
            var receiver=collider.GetComponent<ImpactButton>();
            if(receiver==null)receiver=collider.gameObject.AddComponent<ImpactButton>();
            receiver.owner=intro;receiver.indicator=old.indicator;intro.impactButton=receiver;
            UnityEngine.Object.DestroyImmediate(old);
        }

        /// <summary>Sem argumentos. Retorna cópia URP que preserva atlas, cor e emissão do shader SoftSurface original do Sparrow.</summary>
        private static Material SparrowMaterial()
        {
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(SparrowPath).GetComponentInChildren<Renderer>(true).sharedMaterial;
            var material=LocalMaterial("Sparrow_URP",Color.white);
            material.SetTexture("_BaseMap",original.mainTexture);
            material.SetColor("_BaseColor",original.GetColor("_Color"));
            material.SetTexture("_EmissionMap",original.mainTexture);
            material.SetColor("_EmissionColor",Color.white*original.GetFloat("_Emission"));
            material.EnableKeyword("_EMISSION");material.SetFloat("_Smoothness",0);
            return material;
        }

        /// <summary>spawn contém os apoios existentes. Sem retorno. Procura uma pequena grade sobre cada mesa somente quando o ponto inicial estiver obstruído.</summary>
        private static void EnsureValidSpawnPoints(CardSpawnSystem spawn)
        {
            Physics.SyncTransforms();
            for(int i=0;i<spawn.candidates.Length;i++)
            {
                if(spawn.IsCandidateValid(i))continue;
                var candidate=spawn.candidates[i];Bounds b=candidate.support.bounds;bool found=false;
                foreach(float z in new[]{b.min.z+.18f,b.max.z-.18f})
                {
                    foreach(float fraction in new[]{.2f,.8f,.35f,.65f,.5f})
                    {
                        candidate.point.position=new Vector3(Mathf.Lerp(b.min.x,b.max.x,fraction),b.max.y+.006f,z);
                        if(spawn.IsCandidateValid(i)){found=true;break;}
                    }
                    if(found)break;
                }
                if(!found)throw new InvalidOperationException("Mesa sem área livre para cartão: "+candidate.support.name);
                Report.Add("Ponto livre verificado: "+candidate.point.name+" "+candidate.point.position);
            }
        }

        /// <summary>level fornece o jogador. Sem retorno. Instancia exclusivamente Sparrow e reutiliza controller, avatar e atlas; cria cópia URP quando necessária.</summary>
        private static void CreatePet(Level01GameManager level)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(SparrowPath);if(source==null)throw new FileNotFoundException(SparrowPath);
            Transform root=Group("CompanionPet",level.transform);
            var bird=(GameObject)PrefabUtility.InstantiatePrefab(source,root);
            bird.transform.localPosition=new Vector3(0,-.106f,0);bird.transform.localRotation=Quaternion.identity;bird.transform.localScale=Vector3.one*.28f;
            foreach(var t in bird.GetComponentsInChildren<Transform>(true)){t.gameObject.layer=2;GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);}
            foreach(var c in bird.GetComponentsInChildren<Collider>(true))c.enabled=false;
            foreach(var body in bird.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}
            foreach(var renderer in bird.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                renderer.sharedMaterials=renderer.sharedMaterials.Select(original=>{
                    if(original.shader.name.StartsWith("Universal Render Pipeline/"))return original;
                    return SparrowMaterial();
                }).ToArray();
            }
            var follower=root.gameObject.AddComponent<PetFollower>();follower.level=level;follower.target=level.player.view;follower.animator=bird.GetComponentInChildren<Animator>(true);
            if(follower.animator==null||follower.animator.runtimeAnimatorController==null)throw new InvalidOperationException("Sparrow sem Animator/controller.");
            if(!follower.animator.runtimeAnimatorController.animationClips.Any(c=>c.name=="Fly"))throw new InvalidOperationException("Clipe Fly ausente.");
            root.position=level.player.view.position+level.player.transform.right*.65f+level.player.transform.forward*.9f+Vector3.up*.14f;
            follower.animator.applyRootMotion=false;
        }

        /// <summary>level fornece o portão/trigger existentes. Sem retorno. Acrescenta superfície branca URP e luz sem sombras atrás da saída.</summary>
        private static void CreateExitLight(Level01GameManager level)
        {
            Transform root=Group("ExitLightEffect",level.transform);level.exitLight=root.gameObject;
            Vector3 center=level.exitDoor.transform.position;
            var plane=GameObject.CreatePrimitive(PrimitiveType.Quad);plane.name="Superficie_Branca";plane.transform.SetParent(root,false);
            plane.transform.position=new Vector3(center.x,2.8f,center.z+2.8f);plane.transform.localScale=new Vector3(6.6f,5.3f,1);
            UnityEngine.Object.DestroyImmediate(plane.GetComponent<Collider>());
            var material=LocalMaterial("Saida_Luz_Branca",Color.white);
            material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.white*4);material.SetFloat("_Cull",0);
            plane.GetComponent<Renderer>().sharedMaterial=material;
            var lightRoot=Group("Luz_Suave_Saida",root);lightRoot.position=new Vector3(center.x,3,center.z+1.5f);
            var light=lightRoot.gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=Color.white;light.intensity=2;light.range=4;light.shadows=LightShadows.None;
            var trigger=Components<LevelCompleteTrigger>(level.gameObject.scene).Single();
            trigger.transform.position=new Vector3(center.x,1.3f,center.z+2.6f);
            trigger.GetComponent<BoxCollider>().size=new Vector3(6.5f,2.6f,.8f);
            root.gameObject.SetActive(false);
        }

        /// <summary>scene é a nave carregada. Sem retorno. Atualiza as oito linhas existentes de controles no prefab, na cena e no MainMenu.</summary>
        private static void UpdateControls(Scene scene)
        {
            var prefab=PrefabUtility.LoadPrefabContents(MenuSceneSetup.PausePrefabPath);
            try{foreach(var panel in prefab.GetComponentsInChildren<ControlsPanelController>(true))Level01GameplaySetup.UpdateControls(panel);PrefabUtility.SaveAsPrefabAsset(prefab,MenuSceneSetup.PausePrefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            foreach(var panel in Components<ControlsPanelController>(scene))Level01GameplaySetup.UpdateControls(panel);
            var previous=SceneManager.GetActiveScene();
            Scene main=SceneManager.GetSceneByPath(MenuSceneSetup.MainScenePath);bool opened=!main.IsValid()||!main.isLoaded;
            if(opened)main=EditorSceneManager.OpenScene(MenuSceneSetup.MainScenePath,OpenSceneMode.Additive);
            try{foreach(var panel in Components<ControlsPanelController>(main))Level01GameplaySetup.UpdateControls(panel);EditorSceneManager.MarkSceneDirty(main);EditorSceneManager.SaveScene(main);}
            finally{if(opened)EditorSceneManager.CloseScene(main,true);SceneManager.SetActiveScene(previous);}
        }

        /// <summary>name/cor identificam a cópia própria. Retorna material URP persistente sem alterar fornecedores ou configurações globais.</summary>
        private static Material LocalMaterial(string name,Color color)
        {
            string path=Level01GameplaySetup.MaterialFolder+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);material.SetColor("_BaseColor",color);}
            EditorUtility.SetDirty(material);return material;
        }

        /// <summary>Nome e pai definem o grupo. Retorna Transform unitário sem reposicionar filhos existentes.</summary>
        private static Transform Group(string name,Transform parent){var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}

        /// <summary>scene limita a busca. Retorna componentes T inclusive inativos somente da cena indicada.</summary>
        private static T[] Components<T>(Scene scene) where T:Component{return scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<T>(true)).ToArray();}
    }
}
