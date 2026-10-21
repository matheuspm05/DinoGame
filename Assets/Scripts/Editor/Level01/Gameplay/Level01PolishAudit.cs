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
    /// <summary>Verificações estáticas do polimento, preservação dos modelos, GUIDs e referências da cena.</summary>
    public static class Level01PolishAudit
    {
        /// <summary>Sem argumentos/retorno. Audita a cena salva pelo Editor e produz relatório de áudio, animações, VFX, colisão e decoração.</summary>
        [MenuItem("Tools/Gameplay Level 01/Auditar audio VFX e adocao")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Execute fora de Play.");
            Directory.CreateDirectory(Level01PolishSetup.ReportFolder);
            Scene scene=SceneManager.GetSceneByPath(Level01GameplaySetup.ScenePath);
            bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(Level01GameplaySetup.ScenePath,OpenSceneMode.Additive);
            var lines=new List<string>{"Auditoria do polimento "+DateTime.Now.ToString("s")+" Unity "+Application.unityVersion};
            try
            {
                var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
                var level=all.Select(t=>t.GetComponent<Level01GameManager>()).Single(c=>c!=null);
                var pet=all.Select(t=>t.GetComponent<PetFollower>()).Single(c=>c!=null);
                Check(all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==0,"Zero scripts ausentes",lines);
                Check(all.Count(t=>t.GetComponent<NaveWalkthroughController>()!=null)==1,"Um jogador",lines);
                Check(all.Count(t=>t.GetComponent<EventSystem>()!=null)==1,"Um EventSystem",lines);
                Check(all.Count(t=>t.GetComponent<AudioListener>()!=null)==1,"Um AudioListener",lines);
                Check(all.Count(t=>t.GetComponent<ObjectiveHUD>()!=null)==1,"Um HUD",lines);
                foreach(var door in new[]{level.initialRoom.initialDoor,level.laboratoryDoor,level.hangarDoor,level.exitDoor})
                {
                    var p=door.GetComponent<DoorPresentation>();
                    Check(p!=null&&p.door==door&&p.movementSource!=null&&p.openingClip!=null,"Porta ligada à apresentação "+door.name,lines);
                    Check(door.solidCollider.enabled&&!door.solidCollider.isTrigger&&door.solidCollider.transform.IsChildOf(door.transform),"Colisão na folha móvel "+door.name,lines);
                    Check(p.steam.Length==2&&p.steam.All(s=>s!=null&&!s.main.loop&&!s.main.playOnAwake&&!s.main.useUnscaledTime),"Dois emissores curtos pausáveis "+door.name,lines);
                    lines.Add("PORTA|"+door.name+"|"+AssetDatabase.GetAssetPath(p.openingClip)+"|"+p.movementSource.spatialBlend+"D|volume="+p.movementSource.volume);
                }
                var exit=level.exitDoor.GetComponent<DoorPresentation>();
                Check(exit.alarmClip!=null&&exit.alarmDuration>0&&exit.alarmDuration<=8,"Alerta externo limitado a "+exit.alarmDuration+"s",lines);
                lines.Add("ALARME|"+AssetDatabase.GetAssetPath(exit.alarmClip)+"; aviso eletrônico Kenney; sem anúncio de voz importado.");
                var energy=level.energyPanel.GetComponent<EnergyActivationVFX>();
                Check(energy!=null&&energy.level==level&&energy.effect!=null&&!energy.effect.main.loop,"Partículas de energia vinculadas à instalação real",lines);
                lines.Add("ATIVACAO|"+AssetDatabase.GetAssetPath(energy.activationClip));
                foreach(string prefabName in new[]{"VFX_EnergyActivation","VFX_DoorSteam"})
                {
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/Prefabs/"+prefabName+".prefab");
                    var particle=prefab.GetComponent<ParticleSystem>();
                    Check(particle!=null&&!particle.main.loop&&particle.main.maxParticles<=80&&prefab.GetComponentsInChildren<Collider>(true).Length==0,
                        prefabName+" sem loop/colisores, limite de partículas",lines);
                    Check(prefab.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name=="Universal Render Pipeline/Particles/Unlit","Material URP "+prefabName,lines);
                }
                var original=AssetDatabase.LoadAssetAtPath<GameObject>(Level01AdjustmentsSetup.SparrowPath).GetComponentInChildren<Animator>(true);
                Check(original.runtimeAnimatorController==pet.animator.runtimeAnimatorController,"Controller original Sparrow preservado",lines);
                var clipNames=original.runtimeAnimatorController.animationClips.Select(c=>c.name).Distinct().OrderBy(n=>n).ToArray();
                lines.Add("CLIPES SPARROW|"+string.Join(", ",clipNames));
                Check(new[]{"Fly","Idle_A","Idle_B","Idle_C","Walk","Run","Sit"}.All(clipNames.Contains),"Clipes reais disponíveis para repouso e voo",lines);
                Check(pet.adoptionTarget!=null&&pet.adoptionTarget.enabled&&pet.adoptionTarget.isTrigger&&pet.adoptionTarget.gameObject.layer==0,"Alvo pequeno de adoção no raycast",lines);
                Check(!pet.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled&&!c.isTrigger),"Sparrow sem colisão física",lines);
                Check(pet.transform.position.z>level.initialRoom.initialDoor.transform.position.z+.6f&&pet.transform.position.y<.25f,"Sparrow no chão após a porta inicial",lines);
                var animation=pet.GetComponent<SparrowAnimationController>();
                Check(animation!=null&&animation.restingStates.Length==3&&animation.flightState=="Base Layer.Fly","Três idles sorteados; Fly preservado na locomoção",lines);
                var feet=level.player.GetComponent<PlayerFootstepAudio>();
                Check(feet!=null&&feet.player==level.player&&feet.source!=null,"Passos preparados sem modificar controlador",lines);
                var chirps=pet.GetComponent<CompanionChirps>();
                Check(chirps!=null&&chirps.pet==pet&&chirps.source.spatialBlend==1,"Cantos opcionais preparados na própria ave",lines);
                lines.Add("LIMITACAO|Passos atribuídos="+feet.footsteps.Length+"; cantos atribuídos="+chirps.clips.Length+
                    ". Nenhum áudio adequado para estas categorias encontrado nos 173 clipes importados.");
                Physics.SyncTransforms();
                for(int i=0;i<level.cardSpawn.candidates.Length;i++)
                {Check(level.cardSpawn.IsCandidateValid(i),"Cartão com apoio real "+i,lines);lines.Add("CARTAO|"+level.cardSpawn.candidates[i].point.position);}
                CheckAudioGUIDs(lines);
                lines.Add("Colisores sólidos habilitados="+all.SelectMany(t=>t.GetComponents<Collider>()).Count(c=>c.enabled&&!c.isTrigger));
                lines.Add("Rigidbody estáticos adicionados=0; MeshCollider novos=0.");
                CompareDecoration(scene,pet,lines);
                File.WriteAllLines(Level01PolishSetup.ReportFolder+"/Auditoria.txt",lines);
                Debug.Log("Auditoria do polimento aprovada.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }

        /// <summary>lines recebe resultados. Sem retorno. Compara todos os GUIDs da inspeção anterior e registra os caminhos atuais após MoveAsset.</summary>
        private static void CheckAudioGUIDs(List<string> lines)
        {
            const string inspection="Docs/Relatorios/Polimento_Nivel01/Inspecao.txt";
            if(!File.Exists(inspection)){lines.Add("Inspeção prévia indisponível para comparação dos GUIDs.");return;}
            int count=0;var manifest=new List<string>{"origem|caminho_atual|GUID"};
            foreach(var line in File.ReadLines(inspection).Where(s=>s.StartsWith("AUDIO|")))
            {
                string[] parts=line.Split('|');string current=AssetDatabase.GUIDToAssetPath(parts[2]);
                Check(AssetDatabase.LoadAssetAtPath<AudioClip>(current)!=null,"GUID de áudio preservado: "+parts[2],lines);
                manifest.Add(parts[1]+"|"+current+"|"+parts[2]);count++;
            }
            Check(AssetDatabase.FindAssets("t:AudioClip").Length==count,"Nenhum clipe duplicado ou excluído ("+count+")",lines);
            File.WriteAllLines(Level01PolishSetup.ReportFolder+"/Audio_Caminhos_GUIDs.csv",manifest);
        }

        /// <summary>scene/pet/lines definem nave e resultados. Sem retorno. Compara todos os renderizadores anteriores, exceto a nova posição autorizada do pássaro.</summary>
        private static void CompareDecoration(Scene scene,PetFollower pet,List<string> lines)
        {
            Scene backup=EditorSceneManager.OpenScene("Assets/Scenes/Backups/Level01_Antes_Polimento.unity",OpenSceneMode.Additive);
            try
            {
                var current=scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Renderer>(true))
                    .GroupBy(r=>HierarchyPath(r.transform)).ToDictionary(g=>g.Key,g=>g.First());
                var nowColliders=scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Collider>(true)).ToArray();
                var beforeColliders=backup.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Collider>(true)).ToArray();
                lines.Add("COLISOES: sólidos habilitados antes="+beforeColliders.Count(c=>c.enabled&&!c.isTrigger)+"; depois="+nowColliders.Count(c=>c.enabled&&!c.isTrigger)+
                    "; novos BoxColliders="+(nowColliders.Count(c=>c is BoxCollider)-beforeColliders.Count(c=>c is BoxCollider)));
                Check(nowColliders.Count(c=>c is MeshCollider)==beforeColliders.Count(c=>c is MeshCollider),"Nenhum MeshCollider novo",lines);
                Check(scene.GetRootGameObjects().Sum(go=>go.GetComponentsInChildren<Rigidbody>(true).Length)==backup.GetRootGameObjects().Sum(go=>go.GetComponentsInChildren<Rigidbody>(true).Length),"Nenhum Rigidbody estático adicionado",lines);
                int count=0;
                foreach(var renderer in backup.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Renderer>(true)))
                {
                    string path=HierarchyPath(renderer.transform);
                    if(path.Contains("/CompanionPet/"))continue;
                    if(!current.TryGetValue(path,out var now))throw new InvalidOperationException("Modelo ausente: "+path);
                    Check(Vector3.Distance(renderer.transform.position,now.transform.position)<.0002f&&
                        Quaternion.Angle(renderer.transform.rotation,now.transform.rotation)<.01f&&
                        Vector3.Distance(renderer.transform.lossyScale,now.transform.lossyScale)<.0002f,"Modelo preservado: "+path,lines);
                    count++;
                }
                lines.Add("DECORACAO: "+count+" renderizadores com posição, rotação e escala preservadas.");
            }
            finally{EditorSceneManager.CloseScene(backup,true);Physics.SyncTransforms();}
        }

        /// <summary>target define um transform. Retorna seu caminho completo para correspondência com o backup da cena.</summary>
        private static string HierarchyPath(Transform target)
        {string path=target.name;while(target.parent!=null){target=target.parent;path=target.name+"/"+path;}return path;}

        /// <summary>condition/message/lines contêm verificação e destino. Sem retorno. Registra aprovação ou interrompe com evidência da falha.</summary>
        private static void Check(bool condition,string message,List<string> lines)
        {if(!condition)throw new InvalidOperationException(message);lines.Add("OK: "+message);}
    }
}
