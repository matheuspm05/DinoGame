using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjetoRetorno.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace ProjetoRetorno.Level01.Editor
{
    /// <summary>Audita a integração, compara os modelos com o backup e gera uma prévia estática do Sparrow original.</summary>
    public static class Level01AdjustmentsAudit
    {
        /// <summary>Sem argumentos/retorno. Verifica componentes, referências, apoios, fontes e preservação do layout sem salvar a cena auditada.</summary>
        [MenuItem("Tools/Gameplay Level 01/Auditar ajustes e Sparrow")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Execute fora de Play.");
            Directory.CreateDirectory("Docs/Relatorios/Ajustes_Nivel01");Directory.CreateDirectory("Docs/Previas/Ajustes_Nivel01");
            Scene scene=SceneManager.GetSceneByPath(Level01AdjustmentsSetup.ScenePath);
            bool opened=!scene.IsValid()||!scene.isLoaded;Scene previous=SceneManager.GetActiveScene();
            if(opened)scene=EditorSceneManager.OpenScene(Level01AdjustmentsSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
                var level=transforms.Select(t=>t.GetComponent<Level01GameManager>()).Single(m=>m!=null);
                var pet=transforms.Select(t=>t.GetComponent<PetFollower>()).Single(p=>p!=null);
                var lines=new List<string>{"Auditoria — "+DateTime.Now.ToString("s"),"Cena: "+scene.path};
                int missing=transforms.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                Check(missing==0,"Zero scripts ausentes",lines);
                Check(transforms.Count(t=>t.GetComponent<NaveWalkthroughController>()!=null)==1,"Um jogador existente",lines);
                Check(transforms.Count(t=>t.GetComponent<EventSystem>()!=null)==1,"Um EventSystem",lines);
                Check(transforms.Count(t=>t.GetComponent<ObjectiveHUD>()!=null)==1,"Um HUD",lines);
                Check(transforms.Count(t=>t.GetComponent<IntroThrowingSystem>()!=null)==1,"Um sistema de arremesso",lines);
                Check(transforms.Count(t=>t.GetComponent<Level01Interactable>()!=null&&t.GetComponent<Level01Interactable>().kind==Level01InteractionKind.Keycard)==1,"Um cartão",lines);
                Physics.SyncTransforms();
                for(int i=0;i<level.cardSpawn.candidates.Length;i++)
                {
                    Check(level.cardSpawn.IsCandidateValid(i),"Ponto de cartão válido "+i,lines);
                    lines.Add("Posição "+level.cardSpawn.candidates[i].point.position+" apoio "+level.cardSpawn.candidates[i].support.name);
                }
                foreach(var door in new[]{level.initialRoom.initialDoor,level.laboratoryDoor,level.hangarDoor,level.exitDoor})
                {
                    Check(door.solidCollider.enabled&&!door.solidCollider.isTrigger,"Porta sólida "+door.name,lines);
                    Check(door.movingLeaf.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetStaticEditorFlags(t.gameObject)==0),"Folha dinâmica "+door.name,lines);
                }
                var impact=level.initialRoom.impactButton;
                Check(impact.GetComponent<Collider>()!=null&&impact.GetComponent<Collider>().enabled,"ImpactButton recebe colisão no próprio collider",lines);
                foreach(var prefab in level.initialRoom.throwablePrefabs)
                {
                    Check(prefab.GetComponent<ThrownObject>()!=null&&prefab.GetComponent<Rigidbody>()!=null,"Prefab físico "+AssetDatabase.GetAssetPath(prefab),lines);
                    foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true))lines.Add("Modelo caixa: "+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(renderer.gameObject));
                }
                var original=AssetDatabase.LoadAssetAtPath<GameObject>(Level01AdjustmentsSetup.SparrowPath);
                var originalAnimator=original.GetComponentInChildren<Animator>(true);
                Check(pet.animator.runtimeAnimatorController==originalAnimator.runtimeAnimatorController,"Controller original Sparrow reutilizado",lines);
                lines.Add("Prefab: "+Level01AdjustmentsSetup.SparrowPath);
                lines.Add("Controller: "+AssetDatabase.GetAssetPath(originalAnimator.runtimeAnimatorController));
                lines.Add("Clipes reais: "+string.Join(", ",originalAnimator.runtimeAnimatorController.animationClips.Select(c=>c.name).Distinct().OrderBy(n=>n)));
                var fly=originalAnimator.runtimeAnimatorController.animationClips.First(c=>c.name=="Fly");
                lines.Add("Fly duração="+fly.length+" loop="+fly.isLooping);
                lines.Add("Caminhos animados Fly: "+string.Join(", ",AnimationUtility.GetCurveBindings(fly).Select(b=>b.path).Distinct().Take(30)));
                var originalMaterial=original.GetComponentInChildren<Renderer>(true).sharedMaterial;
                foreach(var renderer in pet.GetComponentsInChildren<Renderer>(true))
                    foreach(var material in renderer.sharedMaterials)
                        Check(material.shader.name.StartsWith("Universal Render Pipeline/")&&material.GetTexture("_BaseMap")==originalMaterial.mainTexture,"Atlas original preservado em URP: "+renderer.name,lines);
                Check(!pet.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled&&!c.isTrigger),"Pet não colide fisicamente",lines);
                var trigger=transforms.Select(t=>t.GetComponent<LevelCompleteTrigger>()).Single(t=>t!=null);
                Check(trigger.level==level&&trigger.gameObject.activeSelf&&trigger.GetComponent<BoxCollider>().isTrigger&&trigger.GetComponent<BoxCollider>().enabled,"Trigger de conclusão reutilizado/ativo",lines);
                lines.Add("Volume final "+trigger.GetComponent<BoxCollider>().bounds);
                lines.Add("Luz: "+level.exitLight.name+" inicialmente ativa="+level.exitLight.activeSelf);
                Check(!level.exitLight.activeSelf,"Luz começa desativada",lines);
                CheckCardAccess(level,lines);
                CompareBackup(scene,lines);
                File.WriteAllLines("Docs/Relatorios/Ajustes_Nivel01/Auditoria.txt",lines);
                RenderPet(pet);
                Debug.Log("Auditoria dos ajustes salva, com prévia estática do Sparrow.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
        }

        /// <summary>level e lines fornecem cena e relatório. Sem retorno. Percorre grade de cápsulas no laboratório e verifica aproximação e linha de visão para todos os pontos.</summary>
        private static void CheckCardAccess(Level01GameManager level,List<string> lines)
        {
            Bounds area=level.cardSpawn.laboratoryBounds;const float step=.5f;
            int width=Mathf.FloorToInt(area.size.x/step),depth=Mathf.FloorToInt(area.size.z/step);
            var free=new bool[width*depth];var visited=new bool[free.Length];
            for(int z=0;z<depth;z++)for(int x=0;x<width;x++)
            {
                Vector3 feet=new Vector3(area.min.x+(x+.5f)*step,.04f,area.min.z+(z+.5f)*step);
                free[z*width+x]=!Physics.OverlapCapsule(feet+Vector3.up*.3f,feet+Vector3.up*1.5f,.26f,
                    Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(c=>!c.transform.IsChildOf(level.player.transform));
            }
            Vector3 entrance=level.laboratoryDoor.transform.position+level.laboratoryDoor.transform.forward*1.5f;
            int sx=Mathf.Clamp(Mathf.FloorToInt((entrance.x-area.min.x)/step),0,width-1),sz=Mathf.Clamp(Mathf.FloorToInt((entrance.z-area.min.z)/step),0,depth-1);
            var queue=new Queue<int>();int start=sz*width+sx;Check(free[start],"Entrada da grade de laboratório livre",lines);queue.Enqueue(start);visited[start]=true;
            while(queue.Count>0)
            {
                int cell=queue.Dequeue(),x=cell%width,z=cell/width;
                foreach(var move in new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down})
                {
                    int nx=x+move.x,nz=z+move.y;if(nx<0||nz<0||nx>=width||nz>=depth)continue;
                    int next=nz*width+nx;if(!visited[next]&&free[next]){visited[next]=true;queue.Enqueue(next);}
                }
            }
            foreach(var candidate in level.cardSpawn.candidates)
            {
                Vector3 approach=new Vector3(candidate.point.position.x,.04f,candidate.support.bounds.min.z-1.15f);
                int x=Mathf.Clamp(Mathf.FloorToInt((approach.x-area.min.x)/step),0,width-1),z=Mathf.Clamp(Mathf.FloorToInt((approach.z-area.min.z)/step),0,depth-1);
                Check(visited[z*width+x],"Cápsula alcança aproximação de "+candidate.point.name,lines);
                Vector3 eye=approach+Vector3.up*1.65f,direction=candidate.point.position-eye;
                bool blocked=Physics.RaycastAll(eye,direction.normalized,direction.magnitude-.025f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)
                    .Any(hit=>!hit.transform.IsChildOf(level.player.transform));
                Check(!blocked,"Linha de visão livre para "+candidate.point.name,lines);
            }
        }

        /// <summary>scene/lines recebem cena atual e relatório. Sem retorno. Compara transforms de renderizadores com o backup anterior, sem salvar ou mover modelos.</summary>
        private static void CompareBackup(Scene scene,List<string> lines)
        {
            const string backupPath="Assets/Scenes/Backups/Level01_Antes_Ajustes.unity";
            Scene backup=SceneManager.GetSceneByPath(backupPath);bool opened=!backup.IsValid()||!backup.isLoaded;
            if(opened)backup=EditorSceneManager.OpenScene(backupPath,OpenSceneMode.Additive);
            try
            {
                var current=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).GroupBy(r=>Path(r.transform)).ToDictionary(g=>g.Key,g=>g.First());
                int compared=0;
                foreach(var previous in backup.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)))
                {
                    if(!current.TryGetValue(Path(previous.transform),out var renderer))throw new InvalidOperationException("Modelo do backup ausente: "+Path(previous.transform));
                    if(Vector3.Distance(previous.transform.position,renderer.transform.position)>.0002f ||
                        Quaternion.Angle(previous.transform.rotation,renderer.transform.rotation)>.01f ||
                        Vector3.Distance(previous.transform.lossyScale,renderer.transform.lossyScale)>.0002f)
                        throw new InvalidOperationException("Modelo original deslocado: "+Path(previous.transform));
                    compared++;
                }
                lines.Add("Comparação independente com backup: "+compared+" renderizadores originais preservados em posição, rotação e escala.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(backup,true);Physics.SyncTransforms();}
        }

        /// <summary>transform define o objeto. Retorna caminho hierárquico determinístico para comparar os modelos entre duas cenas.</summary>
        private static string Path(Transform transform)
        {
            string path=transform.name;
            while(transform.parent!=null){transform=transform.parent;path=transform.name+"/"+path;}
            return path;
        }

        /// <summary>pet é a instância configurada. Sem retorno. Renderiza uma prévia estática do modelo em uma PreviewScene, sem modificar o Sparrow original.</summary>
        private static void RenderPet(PetFollower pet)
        {
            Scene preview=EditorSceneManager.NewPreviewScene();
            GameObject clone=null,cameraObject=null,lightObject=null;
            var texture=new RenderTexture(720,640,24);var picture=new Texture2D(720,640,TextureFormat.RGB24,false);
            RenderTexture previousTexture=RenderTexture.active;
            try
            {
                clone=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Level01AdjustmentsSetup.SparrowPath),preview);
                clone.transform.SetPositionAndRotation(new Vector3(0,-.106f,0),Quaternion.identity);clone.transform.localScale=Vector3.one*.28f;
                foreach(var renderer in clone.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterials=pet.GetComponentInChildren<Renderer>(true).sharedMaterials;
                var animator=clone.GetComponentInChildren<Animator>(true);animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();
                foreach(var lod in clone.GetComponentsInChildren<LODGroup>(true))lod.ForceLOD(0);
                cameraObject=new GameObject("PreviewCamera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraObject,preview);
                var camera=cameraObject.GetComponent<Camera>();camera.scene=preview;camera.targetTexture=texture;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.045f,.065f,.09f);
                camera.orthographic=true;camera.orthographicSize=.18f;camera.nearClipPlane=.01f;camera.farClipPlane=3;
                camera.transform.position=new Vector3(.4f,.16f,.65f);camera.transform.LookAt(new Vector3(0,.015f,0));
                lightObject=new GameObject("PreviewLight",typeof(Light));SceneManager.MoveGameObjectToScene(lightObject,preview);
                var light=lightObject.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.shadows=LightShadows.None;
                lightObject.transform.rotation=Quaternion.Euler(35,-35,0);
                // O movimento real das asas é verificado em Play; esta captura é somente uma prévia do modelo.
                {
                    animator.enabled=false;
                    var fly=animator.runtimeAnimatorController.animationClips.First(c=>c.name=="Fly");
                    fly.SampleAnimation(animator.gameObject,fly.length*.1f);
                    foreach(var skin in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true))skin.updateWhenOffscreen=true;
                    camera.Render();RenderTexture.active=texture;picture.ReadPixels(new Rect(0,0,720,640),0,0);picture.Apply();
                    File.WriteAllBytes("Docs/Previas/Ajustes_Nivel01/Sparrow_Preview.png",picture.EncodeToPNG());
                }
            }
            finally
            {
                RenderTexture.active=previousTexture;
                if(clone!=null)UnityEngine.Object.DestroyImmediate(clone);if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);if(lightObject!=null)UnityEngine.Object.DestroyImmediate(lightObject);
                texture.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(picture);EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        /// <summary>condition/message/lines são resultado, descrição e relatório. Sem retorno. Falha explicitamente ou registra a verificação aprovada.</summary>
        private static void Check(bool condition,string message,List<string> lines){if(!condition)throw new InvalidOperationException(message);lines.Add("OK: "+message);}
    }
}
