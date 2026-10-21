using System;
using System.IO;
using System.Linq;
using ProjetoRetorno.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjetoRetorno.Level01.Editor
{
    /// <summary>Auditoria estática e prévias das duas faces do cartão, usando somente cópias temporárias de Editor.</summary>
    public static class Level01GameplayAudit
    {
        /// <summary>Sem argumentos/retorno. Inspeciona a cena salva e renderiza o cartão local sem regenerar o cenário ou modificar suas fontes.</summary>
        [MenuItem("Tools/Gameplay Level 01/Auditar cena e visualizar cartao")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Execute fora de Play.");
            Directory.CreateDirectory("Docs/Relatorios/Gameplay");Directory.CreateDirectory("Docs/Previas/Gameplay");
            Scene previous=SceneManager.GetActiveScene();
            Scene scene=SceneManager.GetSceneByPath(Level01GameplaySetup.ScenePath);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(Level01GameplaySetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();
                var objects=roots.SelectMany(go=>go.GetComponentsInChildren<Transform>(true)).ToArray();
                var lines=new System.Collections.Generic.List<string>{"Auditoria estática — "+DateTime.Now.ToString("s"),"Cena: "+scene.path};
                int missing=objects.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                lines.Add("Scripts ausentes: "+missing);
                var manager=objects.Select(t=>t.GetComponent<Level01GameManager>()).First(m=>m!=null);
                lines.Add("Gerentes: "+objects.Count(t=>t.GetComponent<Level01GameManager>()!=null));
                lines.Add("Jogadores: "+objects.Count(t=>t.GetComponent<NaveWalkthroughController>()!=null));
                lines.Add("EventSystems: "+objects.Count(t=>t.GetComponent<UnityEngine.EventSystems.EventSystem>()!=null));
                foreach(var item in new[]{manager.battery,manager.energyPanel,manager.keycard,manager.cardReader}.Concat(manager.terminals))
                {
                    lines.Add(item.name+" | "+item.kind+" | posição "+item.transform.position+" | gerente="+(item.level==manager));
                    foreach(var c in item.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled))lines.Add("  Collider "+c.GetType().Name+" trigger="+c.isTrigger+" bounds="+c.bounds);
                    foreach(var renderer in item.GetComponentsInChildren<Renderer>(true))
                    {
                        string source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(renderer.gameObject);
                        lines.Add("  Modelo: "+source);
                        foreach(var mat in renderer.sharedMaterials)
                            lines.Add("  Material: "+AssetDatabase.GetAssetPath(mat)+" | shader "+mat.shader.name+
                                " | textura "+AssetDatabase.GetAssetPath(mat.HasProperty("_BaseMap")?mat.GetTexture("_BaseMap"):null));
                    }
                }
                foreach(var door in new[]{manager.laboratoryDoor,manager.hangarDoor,manager.exitDoor})
                    lines.Add("Porta: "+door.name+" | closed="+door.closedLocalPosition+" | deslocamento="+door.openLocalOffset+" | sólido="+(door.solidCollider!=null&&door.solidCollider.enabled&&!door.solidCollider.isTrigger));
                foreach(var door in new[]{manager.laboratoryDoor,manager.hangarDoor,manager.exitDoor})
                {
                    if(door.movingLeaf.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetStaticEditorFlags(t.gameObject)!=0))
                        throw new InvalidOperationException("Folha móvel marcada como estática: "+door.name);
                    var walls=objects.Select(t=>t.GetComponent<BoxCollider>()).Where(c=>c!=null&&c.enabled&&
                        (c.name.StartsWith("Wall")||c.name.StartsWith("Fechamento_")||c.name.StartsWith("Deck_Conclusao_Lateral")));
                    foreach(bool open in new[]{false,true})
                    {
                        Vector3 position=door.movingLeaf.parent.TransformPoint(door.closedLocalPosition+(open?door.openLocalOffset:Vector3.zero));
                        foreach(var wall in walls)
                            if(Physics.ComputePenetration(door.solidCollider,position,door.movingLeaf.rotation,wall,wall.transform.position,wall.transform.rotation,out Vector3 direction,out float distance)&&distance>.005f)
                                throw new InvalidOperationException("Porta sobrepõe parede: "+door.name+" / "+wall.name+" / open="+open+" / "+distance);
                    }
                    lines.Add("Folha dinâmica e sem interpenetração nas paredes (fechada/aberta): "+door.name);
                }
                var cardFilter=manager.keycard.GetComponentInChildren<MeshFilter>(true);
                lines.Add("Cartão: malha="+AssetDatabase.GetAssetPath(cardFilter.sharedMesh)+" | submalhas="+cardFilter.sharedMesh.subMeshCount+" | UV="+cardFilter.sharedMesh.uv.Length);
                lines.Add("Contador HUD serializado: "+(manager.hud.progressText!=null));
                lines.Add("OnClick da conclusão: "+manager.hud.menuButton.onClick.GetPersistentMethodName(0));
                lines.Add("Fontes originais de Energy e ARCADIA preservadas; adaptações em Assets/Materials/Gameplay/Level01.");
                if(missing!=0)throw new InvalidOperationException("Scripts ausentes.");
                File.WriteAllLines("Docs/Relatorios/Gameplay/Auditoria_Cena.txt",lines);
                RenderCard();
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
        }

        /// <summary>Sem argumentos/retorno. Renderiza frente e verso do prefab local em PreviewScene com iluminação temporária; não salva objetos na nave.</summary>
        private static void RenderCard()
        {
            Scene preview=EditorSceneManager.NewPreviewScene();
            GameObject card=null,cameraObject=null,lightObject=null;
            var texture=new RenderTexture(1000,650,24);
            var image=new Texture2D(1000,650,TextureFormat.RGB24,false);
            RenderTexture previousTexture=RenderTexture.active;
            try
            {
                card=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Level01GameplaySetup.PrefabFolder+"/Cartao_ARCADIA.prefab"),preview);
                card.transform.position=Vector3.zero;card.transform.rotation=Quaternion.identity;
                card.GetComponent<Level01Interactable>().enabled=false;
                cameraObject=new GameObject("Camera_Preview",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraObject,preview);
                var camera=cameraObject.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.05f,.07f,.09f);
                camera.orthographic=true;camera.orthographicSize=.105f;camera.nearClipPlane=.01f;camera.farClipPlane=4;camera.targetTexture=texture;camera.scene=preview;
                lightObject=new GameObject("Luz_Preview",typeof(Light));SceneManager.MoveGameObjectToScene(lightObject,preview);
                var light=lightObject.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.8f;light.shadows=LightShadows.None;
                foreach(int face in new[]{1,-1})
                {
                    camera.transform.position=new Vector3(0,0,face*.5f);camera.transform.LookAt(Vector3.zero,Vector3.up);
                    lightObject.transform.rotation=camera.transform.rotation;
                    camera.Render();RenderTexture.active=texture;image.ReadPixels(new Rect(0,0,1000,650),0,0);image.Apply();
                    File.WriteAllBytes("Docs/Previas/Gameplay/Cartao_"+(face==1?"Lado_A":"Lado_B")+".png",image.EncodeToPNG());
                }
            }
            finally
            {
                RenderTexture.active=previousTexture;
                if(card!=null)UnityEngine.Object.DestroyImmediate(card);if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);if(lightObject!=null)UnityEngine.Object.DestroyImmediate(lightObject);
                texture.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(image);EditorSceneManager.ClosePreviewScene(preview);
            }
        }
    }
}
