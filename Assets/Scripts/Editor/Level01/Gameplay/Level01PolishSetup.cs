using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjetoRetorno.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjetoRetorno.Level01.Editor
{
    /// <summary>Integra apresentação e colisões na cena existente. Não executa o gerador do mapa.</summary>
    public static partial class Level01PolishSetup
    {
        public const string ReportFolder = "Docs/Relatorios/Polimento_Nivel01";
        private static readonly List<string> Report = new List<string>();
        private sealed class SavedPose
        {
            public Transform target;
            public Vector3 position, scale;
            public Quaternion rotation;
        }

        /// <summary>Sem argumentos/retorno. Faz backup da cena aberta, aplica integração idempotente e comprova a preservação da decoração antes de salvar.</summary>
        [MenuItem("Tools/Gameplay Level 01/Integrar audio VFX e adocao")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Execute fora de Play.");
            Directory.CreateDirectory(ReportFolder); Report.Clear();
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(Level01GameplaySetup.ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(Level01GameplaySetup.ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                const string backup = "Assets/Scenes/Backups/Level01_Antes_Polimento.unity";
                if (!File.Exists(backup)) EditorSceneManager.SaveScene(scene, backup, true);
                var pet = Components<PetFollower>(scene).Single();
                var poses = Components<Renderer>(scene).Where(r => !(r is ParticleSystemRenderer) && !r.transform.IsChildOf(pet.transform))
                    .Select(r => new SavedPose { target=r.transform, position=r.transform.position, rotation=r.transform.rotation, scale=r.transform.lossyScale }).ToArray();
                InstallIntoScene(scene);
                foreach (var pose in poses)
                    if (pose.target == null || Vector3.Distance(pose.position, pose.target.position) > .0001f ||
                        Quaternion.Angle(pose.rotation, pose.target.rotation) > .001f || Vector3.Distance(pose.scale, pose.target.lossyScale) > .0001f)
                        throw new InvalidOperationException("Um modelo do cenário foi deslocado: " + (pose.target == null ? "removido" : pose.target.name));
                Report.Add("Transformações preservadas: " + poses.Length + " renderizadores existentes, exceto o pet que agora espera no corredor.");
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                File.WriteAllLines(ReportFolder + "/Integracao.txt", Report);
                Debug.Log("Polimento integrado sem reconstruir a nave. Consulte " + ReportFolder);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        /// <summary>scene é a nave pronta. Sem retorno. Acrescenta componentes uma única vez, preserva pontos editados e conecta os estados existentes.</summary>
        public static void InstallIntoScene(Scene scene)
        {
            Directory.CreateDirectory(ReportFolder);
            var level = Components<Level01GameManager>(scene).Single();
            if (level.initialRoom == null || level.cardSpawn == null) throw new InvalidOperationException("Integre o gameplay antes do polimento.");
            OrganizeAudio();
            EnsureVFXAssets();
            ReviewCollisions(level);
            ConfigurePet(level, Components<PetFollower>(scene).Single());
            ConfigureAudioAndEffects(level);
            Physics.SyncTransforms();
            RepairCardPoints(level.cardSpawn);
            Report.Add("Cena: " + scene.path + "; jogador, HUD, menus e condições dos puzzles preservados.");
            Report.Add("Ausentes nesta inspeção: passos reais, cantos de pássaro e pacote adicional de anúncios. Campos de passos/cantos preparados sem sons substitutos.");
        }

        /// <summary>level/pet fornecem a nave e Sparrow existente. Sem retorno. Prepara espera no chão e alvo opcional; mantém o prefab e controller originais.</summary>
        private static void ConfigurePet(Level01GameManager level, PetFollower pet)
        {
            bool firstInstall = pet.adoptionTarget == null;
            pet.level = level; pet.target = level.player.view; pet.obstacleRadius = .1f;
            pet.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var collider in pet.GetComponentsInChildren<Collider>(true))
                if (collider != pet.adoptionTarget) collider.enabled = false;
            foreach (var child in pet.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 2;
            if (firstInstall)
            {
                Vector3 entrance = level.initialRoom.initialDoor.transform.position;
                pet.transform.SetPositionAndRotation(entrance + new Vector3(1.12f, .116f, 1.35f), Quaternion.Euler(0, 210, 0));
                var target = Child(pet.transform, "Alvo_Adocao");
                var box = Add<BoxCollider>(target.gameObject);
                box.center = Vector3.zero; box.size = new Vector3(.3f, .26f, .3f);
                pet.adoptionTarget = box;
            }
            pet.adoptionTarget.gameObject.layer = 0; pet.adoptionTarget.isTrigger = true; pet.adoptionTarget.enabled = true;
            var animation = Add<SparrowAnimationController>(pet.gameObject);
            animation.pet = pet; animation.animator = pet.animator;
            var chirps = Add<CompanionChirps>(pet.gameObject);
            chirps.pet = pet; chirps.source = SoundSource(pet.transform, "Audio_Cantos", .7f, 5, .8f);
            // Array vazio é intencional: os pacotes importados não contêm cantos de pássaro.
            Report.Add("Sparrow espera em " + pet.transform.position + "; alvo " + pet.adoptionTarget.bounds +
                "; Idle_A/B/C no chão, Fly após adoção; controller original reutilizado.");
        }

        /// <summary>spawn fornece pontos e apoios reais. Sem retorno. Reposiciona somente marcadores inválidos após alinhar a colisão de mesas movidas manualmente.</summary>
        private static void RepairCardPoints(CardSpawnSystem spawn)
        {
            for (int i=0; i<spawn.candidates.Length; i++)
            {
                if (spawn.IsCandidateValid(i)) continue;
                var candidate = spawn.candidates[i]; Bounds b = candidate.support.bounds;
                bool found = false;
                foreach (float x in new[]{.5f,.2f,.8f,.35f,.65f})
                {
                    foreach (float z in new[]{b.min.z+.18f,b.max.z-.18f})
                    {
                        candidate.point.position = new Vector3(Mathf.Lerp(b.min.x,b.max.x,x), b.max.y+.006f,z);
                        Physics.SyncTransforms();
                        if (spawn.IsCandidateValid(i)) { found=true; break; }
                    }
                    if(found)break;
                }
                if (!found) throw new InvalidOperationException("Sem apoio livre para cartão: " + candidate.point.name);
                Report.Add("Marcador do cartão ajustado à mesa real: " + candidate.point.name + " em " + candidate.point.position);
            }
        }

        /// <summary>path define uma pasta AssetDatabase. Sem retorno. Cria apenas os segmentos ausentes, com metadados gerados pela Unity.</summary>
        private static void Folder(string path)
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\','/');
            if (AssetDatabase.IsValidFolder(path)) return;
            if (!AssetDatabase.IsValidFolder(parent)) Folder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        /// <summary>parent/name identificam um auxiliar. Retorna o existente ou cria um filho unitário sem alterar outros objetos.</summary>
        private static Transform Child(Transform parent, string name)
        {
            Transform child=parent.Find(name); if(child!=null)return child;
            var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;
        }

        /// <summary>go identifica o objeto. Retorna componente existente ou adiciona um único componente do tipo solicitado.</summary>
        private static T Add<T>(GameObject go) where T:Component
        {
            T component=go.GetComponent<T>();if(component==null)component=go.AddComponent<T>();return component;
        }

        /// <summary>scene limita a busca. Retorna todos os componentes T, inclusive inativos, sem incluir outras cenas abertas.</summary>
        private static T[] Components<T>(Scene scene) where T:Component
        { return scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<T>(true)).ToArray(); }
    }
}
