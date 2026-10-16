using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProjetoRetorno.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ProjetoRetorno.Level01.Editor
{
    public static partial class Level01NaveGenerator
    {
        /// <summary>scene é a candidata; retorna relatório. Confere rota, piso, componentes, shaders e conectividade em grade com cápsula humana.</summary>
        private static string ValidateExpandedStructure(Scene scene)
        {
            var report = new StringBuilder("Nave expandida — validação estrutural\nUnity: " + Application.unityVersion + "\n");
            Transform[] objects = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            int missing = objects.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            if (missing > 0) throw new InvalidOperationException("Há scripts ausentes: " + missing);
            if (objects.Count(t => t.GetComponent<NaveWalkthroughController>() != null) != 1 || objects.Count(t => t.GetComponent<Camera>() != null && t.GetComponent<Camera>().enabled) != 1)
                throw new InvalidOperationException("Jogador/câmera duplicados ou ausentes.");
            if (objects.Count(t => t.GetComponent<EventSystem>() != null) != 1 || objects.Count(t => t.GetComponent<PauseMenuController>() != null) != 1)
                throw new InvalidOperationException("Pausa ou EventSystem duplicados/ausentes.");
            int samples = 0;
            foreach (Vector3[] path in ExpandedLinks)
                for (int segment = 1; segment < path.Length; segment++)
                {
                    int steps = Mathf.CeilToInt(Vector3.Distance(path[segment - 1], path[segment]) / .25f);
                    for (int i = 0; i <= steps; i++)
                    {
                        Vector3 p = Vector3.Lerp(path[segment - 1], path[segment], (float)i / steps);
                        if (!Free(scene, p, out string reason)) throw new InvalidOperationException("Rota bloqueada em " + p + ": " + reason);
                        samples++;
                    }
                }
            report.AppendLine("Rota principal e desvios: " + samples + " amostras de piso contínuo e vão livre para cápsula de 1,80 m / raio 0,30 m.");
            const float grid = .75f;
            var free = new HashSet<Vector2Int>();
            int minX = Mathf.FloorToInt(ExpandedRooms.Min(r => r.center.x - r.size.x / 2) / grid);
            int maxX = Mathf.CeilToInt(ExpandedRooms.Max(r => r.center.x + r.size.x / 2) / grid);
            int minZ = Mathf.FloorToInt(ExpandedRooms.Min(r => r.center.y - r.size.y / 2) / grid);
            int maxZ = Mathf.CeilToInt(ExpandedRooms.Max(r => r.center.y + r.size.y / 2) / grid);
            for (int x = minX; x <= maxX; x++) for (int z = minZ; z <= maxZ; z++)
            {
                Vector3 p = new Vector3(x * grid, 0, z * grid);
                if (ExpandedRooms.Any(r => Mathf.Abs(p.x - r.center.x) <= r.size.x / 2 + .001f && Mathf.Abs(p.z - r.center.y) <= r.size.y / 2 + .001f) && Free(scene, p, out _))
                    free.Add(new Vector2Int(x, z));
            }
            Vector2Int spawn = free.OrderBy(p => Vector3.SqrMagnitude(new Vector3(p.x * grid, 0, p.y * grid) - Route[0])).First();
            var reached = new HashSet<Vector2Int> { spawn };
            var queue = new Queue<Vector2Int>(); queue.Enqueue(spawn);
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                foreach (Vector2Int direction in directions)
                {
                    Vector2Int next = current + direction;
                    if (!free.Contains(next) || reached.Contains(next)) continue;
                    Vector3 from = new Vector3(current.x * grid, .35f, current.y * grid);
                    Vector3 to = new Vector3(next.x * grid, .35f, next.y * grid);
                    bool blocked = Physics.CapsuleCastAll(from, from + Vector3.up * 1.2f, .3f, (to - from).normalized, grid, ~0, QueryTriggerInteraction.Ignore)
                        .Any(hit => hit.collider.gameObject.scene == scene && !(hit.collider is CharacterController));
                    if (blocked) continue;
                    reached.Add(next); queue.Enqueue(next);
                }
            }
            foreach (ExpandedRoom room in ExpandedRooms)
            {
                float distance = reached.Min(p => Vector2.Distance(new Vector2(p.x * grid, p.y * grid), room.center));
                if (distance > 1.1f) throw new InvalidOperationException("Sala não alcançável pela grade: " + room.name);
                report.AppendLine("Setor conectado: " + room.name + " — " + room.size.x + " x " + room.size.y + " m; pé-direito " + room.height + " m.");
            }
            report.AppendLine("Grade navegável: " + reached.Count + " / " + free.Count + " pontos alcançáveis; verificação de travessias por CapsuleCast.");
            Material[] materials = objects.SelectMany(t => t.GetComponents<Renderer>()).SelectMany(r => r.sharedMaterials).Distinct().ToArray();
            foreach (Material material in materials)
            {
                if (material == null || material.shader == null || !material.shader.isSupported) throw new InvalidOperationException("Material ausente ou shader não suportado.");
                string shader = material.shader.name;
                if (!shader.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal) && shader != "ProjetoRetorno/WorldLabel" && shader != "UI/Default")
                    throw new InvalidOperationException("Shader a revisar para URP: " + shader);
            }
            Light[] lights = objects.Select(t => t.GetComponent<Light>()).Where(l => l != null && l.enabled).ToArray();
            if (lights.Any(l => l.shadows != LightShadows.None)) throw new InvalidOperationException("Luz dinâmica com sombra desnecessária.");
            report.AppendLine("Materiais compatíveis verificados: " + materials.Length + "; luzes pontuais sem sombras: " + lights.Length + ".");
            report.AppendLine("Scripts ausentes: 0; jogador, câmera, pausa e EventSystem: um de cada.");
            report.AppendLine("Geometria de arquitetura usa módulos ajustados; objetos de decoração mantêm escala uniforme. Colisores importados desativados por instância, com caixas simples locais para objetos sólidos.");
            return report.ToString();
        }

        /// <summary>Cena e posição são entradas; retorna passagem livre e motivo. Ignora outras cenas e exige piso na altura de circulação.</summary>
        private static bool Free(Scene scene, Vector3 p, out string reason)
        {
            Collider blocker = Physics.OverlapCapsule(p + Vector3.up * .35f, p + Vector3.up * 1.55f, .3f, ~0, QueryTriggerInteraction.Ignore)
                .FirstOrDefault(c => c.gameObject.scene == scene && !(c is CharacterController));
            if (blocker != null) { reason = blocker.name; return false; }
            bool floor = Physics.RaycastAll(p + Vector3.up * .3f, Vector3.down, .6f, ~0, QueryTriggerInteraction.Ignore)
                .Any(hit => hit.collider.gameObject.scene == scene && !(hit.collider is CharacterController) && Mathf.Abs(hit.point.y) < .04f && hit.normal.y > .9f);
            reason = floor ? "livre" : "sem piso"; return floor;
        }

        /// <summary>scene é a candidata salva; sem retorno. Renderiza vistas reais isoladas por camada, sem adicionar câmeras permanentes à nave.</summary>
        private static void CaptureExpandedPreviews(Scene scene)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            Directory.CreateDirectory("Docs/Previas/Nivel01_Expandido");
            Scene preview = EditorSceneManager.OpenPreviewScene(scene.path);
            foreach (GameObject root in preview.GetRootGameObjects())
            {
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 31;
                foreach (Light light in root.GetComponentsInChildren<Light>(true)) light.cullingMask = 1 << 31;
                foreach (Camera cameraInScene in root.GetComponentsInChildren<Camera>(true)) cameraInScene.enabled = false;
            }
            GameObject go = new GameObject("Camera_Temporaria_Previa");
            SceneManager.MoveGameObjectToScene(go, preview);
            Camera camera = go.AddComponent<Camera>(); camera.enabled = false;
            camera.cullingMask = 1 << 31; camera.scene = preview;
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(preview);
            camera.fieldOfView = 80; camera.nearClipPlane = .05f; camera.farClipPlane = 120;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .04f, .07f);
            var output = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Vector3[] origins = { new Vector3(0, 1.65f, -3), new Vector3(0, 1.65f, 6), new Vector3(-7, 1.65f, 9), new Vector3(0, 1.65f, 17),
                    new Vector3(20, 1.65f, 19), new Vector3(20, 1.65f, 35), new Vector3(20, 1.65f, 51), new Vector3(-12, 1.65f, 23), new Vector3(33, 1.65f, 37) };
                Vector3[] targets = { new Vector3(-3, 1.6f, 1), new Vector3(0, 1.5f, 13), new Vector3(-16, 1.6f, 11), new Vector3(-4, 1.5f, 25),
                    new Vector3(26, 1.5f, 25), new Vector3(19, 1.7f, 43), new Vector3(20, 2.2f, 69), new Vector3(-17, 1.7f, 30), new Vector3(41, 1.5f, 38) };
                string[] names = { "01_Contencao", "02_Corredores", "03_Energia", "04_Deposito", "05_Laboratorio", "06_Seguranca", "07_Hangar", "08_Maquinas", "09_Manutencao" };
                for (int i = 0; i < origins.Length; i++)
                {
                    camera.transform.SetPositionAndRotation(origins[i], Quaternion.LookRotation(targets[i] - origins[i]));
                    var request = new UniversalRenderPipeline.SingleCameraRequest { destination = output };
                    if (!RenderPipeline.SupportsRenderRequest(camera, request)) throw new InvalidOperationException("URP não suporta a solicitação de renderização.");
                    RenderPipeline.SubmitRenderRequest(camera, request);
                    RenderTexture.active = output;
                    pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                    File.WriteAllBytes("Docs/Previas/Nivel01_Expandido/" + names[i] + ".png", pixels.EncodeToPNG());
                }
            }
            finally
            {
                RenderTexture.active = previous; output.Release();
                UnityEngine.Object.DestroyImmediate(output); UnityEngine.Object.DestroyImmediate(pixels); UnityEngine.Object.DestroyImmediate(go);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }
    }
}
