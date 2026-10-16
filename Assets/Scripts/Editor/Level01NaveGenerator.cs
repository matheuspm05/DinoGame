using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProjetoRetorno.Level01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ProjetoRetorno.Level01.Editor
{
    /// <summary>Gera somente a cena da nave e seus materiais locais. Não altera assets dos fornecedores.</summary>
    public static class Level01NaveGenerator
    {
        public const string ScenePath = "Assets/Scenes/Level01_Nave.unity";
        private const string MaterialFolder = "Assets/Materials/Level01";
        private const string BarkRoot = "Assets/ThirdParty/Barking_Dog/3D Free Modular Kit/Prefabs/";
        private const string MegaRoot = "Assets/ThirdParty/Modular SciFi MegaKit[Standard]/Modular SciFi MegaKit[Standard]/FBX (Unity)/";
        private const float WallHeight = 3.4f;
        private static readonly Dictionary<string, GameObject> Models = new Dictionary<string, GameObject>();
        private static readonly Dictionary<Material, Material> Converted = new Dictionary<Material, Material>();
        private static readonly List<WallInterval> BuiltWalls = new List<WallInterval>();
        private static readonly HashSet<string> UsedAssets = new HashSet<string>();
        private static readonly List<Vector3> Route = new List<Vector3>();
        private static readonly StringBuilder Report = new StringBuilder();
        private static int moduleCount;
        private static Material labelMaterial;

        private struct WallInterval
        {
            public bool horizontal;
            public float line, start, end;
        }

        private sealed class Room
        {
            public Transform root;
            public Vector2 center, size;
            public Color color;
            public readonly Dictionary<string, Vector2> openings = new Dictionary<string, Vector2>();
        }

        /// <summary>Comando sem argumentos/retorno. Preserva alterações abertas e pede confirmação antes de substituir a nave.</summary>
        [MenuItem("Tools/Gerar Level 01")]
        public static void GenerateFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Saia do Play Mode antes de gerar a nave.");
                return;
            }
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Gerar Level 01",
                    "A cena Level01_Nave será regenerada. Edições manuais nessa cena serão substituídas. Continuar?", "Gerar", "Cancelar")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Generate(false);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(1f, 0f, 18f), new Vector3(32f, 12f, 50f)), false);
        }

        /// <summary>Entrada batch, sem argumentos/retorno. Salva e fecha a cena gerada, preservando as cenas já abertas.</summary>
        public static void GenerateBatch()
        {
            Generate(false);
        }

        /// <summary>keepOpen indica se a cena gerada permanece carregada; sem retorno. Constrói, valida e salva apenas a nave.</summary>
        private static void Generate(bool keepOpen)
        {
            Models.Clear(); Converted.Clear(); BuiltWalls.Clear(); UsedAssets.Clear(); Route.Clear();
            Report.Clear(); moduleCount = 0; labelMaterial = null;
            ResolveAssets(); // Falha antes de criar a cena se qualquer caminho obrigatório estiver ausente.
            Directory.CreateDirectory(MaterialFolder);
            AssetDatabase.Refresh();
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.22f, 0.28f, 0.36f);
                RenderSettings.ambientIntensity = 1f;
                RenderSettings.skybox = null;
                RenderSettings.fog = false;
                Transform world = Group("Level01_Nave", null);
                Room containment = MakeRoom(world, "01_Contencao", new Vector2(0, 0), new Vector2(8, 8), new Color(0.3f, 0.7f, 1f), "N", 0, 3);
                Room corridor = MakeRoom(world, "02_Corredor", new Vector2(0, 8), new Vector2(4, 8), new Color(0.3f, 0.7f, 1f), "S", 0, 3);
                corridor.openings["N"] = new Vector2(0, 3);
                corridor.openings["W"] = new Vector2(0, 4);
                Room energy = MakeRoom(world, "03_Energia", new Vector2(-8, 8), new Vector2(8, 8), new Color(1f, 0.58f, 0.22f), "E", 0, 4);
                Room link = MakeRoom(energy.root, "Acesso_Lateral", new Vector2(-3, 8), new Vector2(2, 4), energy.color, "E", 0, 4);
                link.openings["W"] = new Vector2(0, 4);
                Room storage = MakeRoom(world, "04_Deposito", new Vector2(0, 16), new Vector2(8, 8), new Color(1f, 0.7f, 0.32f), "S", 0, 3);
                storage.openings["E"] = new Vector2(0, 3);
                Room lab = MakeRoom(world, "05_Laboratorio", new Vector2(8, 16), new Vector2(8, 8), new Color(0.35f, 1f, 0.83f), "W", 0, 3);
                lab.openings["N"] = new Vector2(0, 3);
                Room security = MakeRoom(world, "06_Seguranca", new Vector2(8, 24), new Vector2(8, 8), new Color(0.48f, 0.68f, 1f), "S", 0, 3);
                security.openings["N"] = new Vector2(0, 4);
                Room hangar = MakeRoom(world, "07_Hangar", new Vector2(8, 34), new Vector2(12, 12), new Color(0.45f, 0.75f, 1f), "S", 0, 4);
                Room[] rooms = { containment, corridor, energy, link, storage, lab, security, hangar };
                foreach (Room room in rooms) BuildShell(room);
                Portal(containment.root, new Vector3(0, 0, 4), 0, 3, "02  CORREDOR");
                Portal(energy.root, new Vector3(-4, 0, 8), 90, 4, "03  ENERGIA");
                Portal(storage.root, new Vector3(0, 0, 12), 0, 3, "04  DEPOSITO");
                Portal(lab.root, new Vector3(4, 0, 16), 90, 3, "05  LABORATORIO");
                Portal(security.root, new Vector3(8, 0, 20), 0, 3, "06  SEGURANCA");
                Portal(hangar.root, new Vector3(8, 0, 28), 0, 4, "07  HANGAR");
                Decorate(containment, energy, storage, lab, security, hangar);
                CreatePlayer(world);
                ProjetoRetorno.UI.Editor.MenuSceneSetup.EnsurePauseMenu(scene);
                Route.AddRange(new[] { new Vector3(0, 0, -1), new Vector3(0, 0, 8), new Vector3(-8, 0, 8),
                    new Vector3(0, 0, 8), new Vector3(0, 0, 16), new Vector3(8, 0, 16), new Vector3(8, 0, 24), new Vector3(8, 0, 34) });
                Physics.SyncTransforms();
                Validate(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Não foi possível salvar " + ScenePath);
                AssetDatabase.SaveAssets();
                try { CapturePreviews(scene); }
                catch (Exception exception) { Report.AppendLine("Prévia visual indisponível: " + exception.Message); }
                Report.AppendLine("\nAssets reais utilizados (vínculos de prefab preservados):");
                foreach (string path in UsedAssets.OrderBy(p => p)) Report.AppendLine(path);
                Directory.CreateDirectory("Docs");
                File.WriteAllText("Docs/Level01_Validacao.txt", Report.ToString());
                Debug.Log("Level 01 salvo e validado: " + ScenePath + "\n" + Report);
            }
            finally
            {
                if (!keepOpen)
                {
                    EditorSceneManager.CloseScene(scene, true);
                    if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                }
            }
        }

        /// <summary>Sem argumentos/retorno. Resolve caminhos reais via AssetDatabase e rejeita modelos não instanciáveis.</summary>
        private static void ResolveAssets()
        {
            string[] bark = { "Floor_01", "Roof_01", "Door_Left_01", "Door_Arch_01", "Fan_01" };
            string[] mega = { "Walls/WallAstra_Straight", "Walls/TopSimple_Straight", "Props/Prop_Computer", "Props/Prop_AccessPoint",
                "Props/Prop_Crate3", "Props/Prop_Crate4", "Props/Prop_Barrel_Large",
                "Props/Prop_Cable_3", "Props/Prop_Vent_Small", "Props/Prop_Vent_Wide", "Props/Prop_Light_Wide", "Props/Prop_ItemHolder",
                "Platforms/Door_Frame_Square", "Platforms/Door_DarkMetal", "Columns/Column_Pipes" };
            var available = new HashSet<string>(AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/ThirdParty/Barking_Dog", "Assets/ThirdParty/Modular SciFi MegaKit[Standard]" })
                .Select(AssetDatabase.GUIDToAssetPath));
            foreach (string name in bark) LoadModel(name, BarkRoot + name + ".prefab", available);
            foreach (string entry in mega) LoadModel(Path.GetFileName(entry), MegaRoot + entry + ".fbx", available);
            if (Shader.Find("Universal Render Pipeline/Lit") == null) throw new InvalidOperationException("Shader URP/Lit não encontrado.");
        }

        /// <summary>key identifica o modelo, path é o caminho confirmado, available contém o inventário; sem retorno. Carrega ou informa o erro.</summary>
        private static void LoadModel(string key, string path, HashSet<string> available)
        {
            if (!available.Contains(path)) throw new FileNotFoundException("Asset obrigatório não encontrado: " + path);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null || PrefabUtility.GetPrefabAssetType(model) == PrefabAssetType.NotAPrefab)
                throw new InvalidOperationException("Asset não instanciável por PrefabUtility: " + path);
            Models.Add(key, model);
        }

        /// <summary>name/parent definem a hierarquia; retorna um agrupador vazio com transformação neutra.</summary>
        private static Transform Group(string name, Transform parent)
        {
            Transform group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        /// <summary>Define setor, centro, dimensões, cor e abertura inicial; retorna os dados para montagem posterior.</summary>
        private static Room MakeRoom(Transform parent, string name, Vector2 center, Vector2 size, Color color, string side, float offset, float width)
        {
            Room room = new Room { root = Group(name, parent), center = center, size = size, color = color };
            room.openings[side] = new Vector2(offset, width);
            return room;
        }

        /// <summary>room define o retângulo e as aberturas; sem retorno. Instancia piso, teto, paredes, luzes e identificação.</summary>
        private static void BuildShell(Room room)
        {
            Transform architecture = Group("Arquitetura", room.root);
            int nx = Mathf.CeilToInt(room.size.x / 4f), nz = Mathf.CeilToInt(room.size.y / 4f);
            float dx = room.size.x / nx, dz = room.size.y / nz;
            for (int x = 0; x < nx; x++)
            for (int z = 0; z < nz; z++)
            {
                Vector3 center = new Vector3(room.center.x - room.size.x / 2 + dx * (x + .5f), -.12f,
                    room.center.y - room.size.y / 2 + dz * (z + .5f));
                Place("Floor_01", architecture, center, new Vector3(dx, .24f, dz), 0, true);
                Place("Roof_01", architecture, new Vector3(center.x, WallHeight + .12f, center.z), new Vector3(dx, .24f, dz), 0, true);
            }
            foreach (string side in new[] { "N", "S", "E", "W" })
            {
                bool horizontal = side == "N" || side == "S";
                float span = horizontal ? room.size.x : room.size.y;
                float line = horizontal ? room.center.y + (side == "N" ? 1 : -1) * room.size.y / 2
                    : room.center.x + (side == "E" ? 1 : -1) * room.size.x / 2;
                float middle = horizontal ? room.center.x : room.center.y;
                if (room.openings.TryGetValue(side, out Vector2 opening))
                {
                    WallRun(architecture, horizontal, line, middle - span / 2, middle + opening.x - opening.y / 2);
                    WallRun(architecture, horizontal, line, middle + opening.x + opening.y / 2, middle + span / 2);
                }
                else WallRun(architecture, horizontal, line, middle - span / 2, middle + span / 2);
            }
            Transform lighting = Group("Iluminacao", room.root);
            float offset = room.size.y >= 8 ? 2f : 0f;
            AddLight(lighting, new Vector3(room.center.x, 2.9f, room.center.y - offset), room.color, 3.5f, 8f);
            if (offset > 0) AddLight(lighting, new Vector3(room.center.x, 2.9f, room.center.y + offset), Color.Lerp(room.color, Color.white, .6f), 3f, 8f);
            if (room.size.x >= 8)
            {
                Place("Prop_Light_Wide", lighting, new Vector3(room.center.x, 3.22f, room.center.y - 2f), new Vector3(1.5f, .12f, .55f), 0, false);
                Place("Prop_Light_Wide", lighting, new Vector3(room.center.x, 3.22f, room.center.y + 2f), new Vector3(1.5f, .12f, .55f), 0, false);
            }
        }

        /// <summary>Orientação, linha e intervalo definem a parede; sem retorno. Subtrai trechos já existentes para evitar paredes sobrepostas.</summary>
        private static void WallRun(Transform parent, bool horizontal, float line, float start, float end)
        {
            if (end - start < .01f) return;
            var pending = new List<Vector2> { new Vector2(start, end) };
            foreach (WallInterval used in BuiltWalls.Where(w => w.horizontal == horizontal && Mathf.Abs(w.line - line) < .01f))
            {
                var next = new List<Vector2>();
                foreach (Vector2 p in pending)
                {
                    if (used.end <= p.x || used.start >= p.y) next.Add(p);
                    else
                    {
                        if (used.start > p.x) next.Add(new Vector2(p.x, used.start));
                        if (used.end < p.y) next.Add(new Vector2(used.end, p.y));
                    }
                }
                pending = next;
            }
            foreach (Vector2 p in pending)
            {
                BuiltWalls.Add(new WallInterval { horizontal = horizontal, line = line, start = p.x, end = p.y });
                int count = Mathf.CeilToInt((p.y - p.x) / 2f);
                float length = (p.y - p.x) / count;
                for (int i = 0; i < count; i++)
                {
                    float middle = p.x + length * (i + .5f);
                    Place("WallAstra_Straight", parent, horizontal ? new Vector3(middle, WallHeight / 2, line) : new Vector3(line, WallHeight / 2, middle),
                        new Vector3(length, WallHeight, .2f), horizontal ? 0 : 90, true, true);
                    Place("TopSimple_Straight", parent, horizontal ? new Vector3(middle, WallHeight - .13f, line) : new Vector3(line, WallHeight - .13f, middle),
                        new Vector3(length, .3f, .24f), horizontal ? 0 : 90, false, true);
                }
            }
        }

        /// <summary>Modelo, pai, centro, tamanho e rotação definem a instância; retorna o suporte normalizado. solid cria apenas um colisor ativo.</summary>
        private static Transform Place(string key, Transform parent, Vector3 center, Vector3 size, float yaw, bool solid, bool alignWall = false)
        {
            GameObject source = Models[key];
            Transform wrapper = Group(key + "_" + (++moduleCount).ToString("D3"), parent);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source, wrapper);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            // A estrutura é estática: animações importadas não podem restaurar escalas/poses durante a visita.
            foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (Animation animation in instance.GetComponentsInChildren<Animation>(true))
            {
                animation.playAutomatically = false;
                animation.Stop();
                animation.enabled = false;
            }
            Bounds bounds = RendererBounds(instance);
            if (alignWall && bounds.size.z > bounds.size.x)
            {
                instance.transform.localRotation = Quaternion.Euler(0, 90, 0);
                bounds = RendererBounds(instance);
            }
            if (bounds.size.sqrMagnitude < .000001f)
                throw new InvalidOperationException("Limites inválidos do modelo: " + key + ": " + bounds);
            instance.transform.localPosition -= bounds.center;
            wrapper.localScale = new Vector3(bounds.size.x < .0001f ? 1 : size.x / bounds.size.x,
                bounds.size.y < .0001f ? 1 : size.y / bounds.size.y, bounds.size.z < .0001f ? 1 : size.z / bounds.size.z);
            // Guarda a escala no filho, mantendo o suporte com escala unitária para edição e colisão previsíveis.
            Transform fitted = Group("Ajuste", wrapper);
            fitted.localScale = wrapper.localScale;
            // Pisos/tetos planos continuam no limite interno do colisor, sem divisão por espessura zero.
            if (bounds.size.y < .0001f && key.StartsWith("Floor", StringComparison.Ordinal)) fitted.localPosition = Vector3.up * size.y / 2;
            if (bounds.size.y < .0001f && key.StartsWith("Roof", StringComparison.Ordinal)) fitted.localPosition = Vector3.down * size.y / 2;
            wrapper.localScale = Vector3.one;
            instance.transform.SetParent(fitted, false);
            wrapper.position = center;
            wrapper.rotation = Quaternion.Euler(0, yaw, 0);
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.detectCollisions = false; }
            foreach (Light light in instance.GetComponentsInChildren<Light>(true)) light.enabled = false;
            foreach (Camera camera in instance.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
            foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            if (solid)
            {
                BoxCollider collider = wrapper.gameObject.AddComponent<BoxCollider>();
                collider.size = size;
            }
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = renderer.sharedMaterials.Select(ConvertMaterial).ToArray();
            UsedAssets.Add(AssetDatabase.GetAssetPath(source));
            return wrapper;
        }

        /// <summary>instance é o objeto instanciado; retorna limites combinados dos renderizadores ou falha se não houver geometria.</summary>
        private static Bounds RendererBounds(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Modelo sem renderizador: " + instance.name);
            Bounds bounds = default;
            bool first = true;
            foreach (Renderer renderer in renderers)
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                Bounds local = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds : renderer.localBounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 offset = Vector3.Scale(local.extents, new Vector3((corner & 1) == 0 ? -1 : 1,
                        (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    Vector3 point = renderer.transform.TransformPoint(local.center + offset);
                    if (first) { bounds = new Bounds(point, Vector3.zero); first = false; }
                    else bounds.Encapsulate(point);
                }
            }
            return bounds;
        }

        /// <summary>source é o material original; retorna material URP, criando cópia local quando necessário, sem editar o fornecedor.</summary>
        private static Material ConvertMaterial(Material source)
        {
            if (source == null) return CreateMaterial("Metal_Default", new Color(.28f, .32f, .38f), false);
            if (Converted.TryGetValue(source, out Material result)) return result;
            bool alreadyUrp = source.shader != null && source.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long localId);
            string safe = new string(source.name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            string path = MaterialFolder + "/" + safe + "_" + guid + "_" + localId + ".mat";
            result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null)
            {
                result = alreadyUrp ? new Material(source) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(result, path);
            }
            // Kits com paredes de face única precisam ser visíveis dos dois lados nos setores conectados.
            if (result.HasProperty("_Cull")) result.SetFloat("_Cull", (float)CullMode.Off);
            if (alreadyUrp)
            {
                EditorUtility.SetDirty(result);
                Converted[source] = result;
                return result;
            }
            result.color = source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
            result.SetFloat("_Metallic", source.HasProperty("_Metallic") ? source.GetFloat("_Metallic") : .35f);
            result.SetFloat("_Smoothness", source.HasProperty("_Glossiness") ? source.GetFloat("_Glossiness") : .35f);
            if (source.HasProperty("_MainTex"))
            {
                result.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
                result.SetTextureScale("_BaseMap", source.GetTextureScale("_MainTex"));
                result.SetTextureOffset("_BaseMap", source.GetTextureOffset("_MainTex"));
            }
            if (source.HasProperty("_BumpMap") && source.GetTexture("_BumpMap") != null)
            {
                result.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
                result.EnableKeyword("_NORMALMAP");
            }
            if (source.HasProperty("_EmissionColor"))
            {
                result.SetColor("_EmissionColor", source.GetColor("_EmissionColor"));
                if (source.IsKeywordEnabled("_EMISSION")) result.EnableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(result);
            Converted[source] = result;
            return result;
        }

        /// <summary>name/color/emissive definem material local; retorna asset existente ou novo, compatível com URP.</summary>
        private static Material CreateMaterial(string name, Color color, bool emissive)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Smoothness", .3f);
            if (emissive) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 1.4f); }
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Pai, posição, cor, intensidade e alcance definem uma luz pontual URP; sem retorno e sem sombras caras.</summary>
        private static void AddLight(Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            Light light = Group("Luz", parent).gameObject.AddComponent<Light>();
            light.transform.position = position;
            light.type = LightType.Point;
            light.color = color; light.intensity = intensity; light.range = range;
            light.shadows = LightShadows.None;
        }

        /// <summary>Pai, centro, rotação, largura e texto definem passagem aberta; sem retorno. Moldura não recebe colisor que feche o vão.</summary>
        private static void Portal(Transform parent, Vector3 center, float yaw, float width, string text)
        {
            Transform portal = Group("Passagem_" + text, parent);
            Place("Door_Frame_Square", portal, center + Vector3.up * 1.6f, new Vector3(width + .5f, 3.2f, .28f), yaw, false, true);
            Quaternion rotation = Quaternion.Euler(0, yaw, 0);
            Place("Door_Left_01", portal, center + rotation * new Vector3(width / 2 + .65f, 1.38f, .12f), new Vector3(.95f, 2.76f, .2f), yaw, false, true);
            Label(portal, text, center + Vector3.up * 3f + rotation * new Vector3(0, 0, -.2f), yaw);
        }

        /// <summary>Dados definem uma placa espacial simples; sem retorno. Texto é sinalização do cenário, sem Canvas/HUD.</summary>
        private static void Label(Transform parent, string text, Vector3 position, float yaw)
        {
            GameObject go = Group("Placa_" + text, parent).gameObject;
            go.transform.position = position; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (labelMaterial == null)
            {
                string path = MaterialFolder + "/Placas.mat";
                labelMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                Shader shader = Shader.Find("ProjetoRetorno/WorldLabel");
                if (shader == null) throw new InvalidOperationException("Shader das placas não importado.");
                if (labelMaterial == null) { labelMaterial = new Material(shader); AssetDatabase.CreateAsset(labelMaterial, path); }
            }
            go.GetComponent<MeshRenderer>().sharedMaterial = labelMaterial;
            mesh.text = text; mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center;
            mesh.characterSize = .065f; mesh.fontSize = 48; mesh.color = new Color(.5f, .88f, 1f);
            go.AddComponent<NaveWorldLabel>();
        }

        /// <summary>Setores determinam locais de decoração e reservas; sem retorno. Mantém eixos de circulação livres.</summary>
        private static void Decorate(Room containment, Room energy, Room storage, Room lab, Room security, Room hangar)
        {
            Transform a = Group("Equipamentos", containment.root);
            Place("Prop_ItemHolder", a, new Vector3(-2.65f, .75f, -1.8f), new Vector3(1.4f, 1.5f, 1.8f), 0, true);
            Place("Door_Arch_01", a, new Vector3(-2.65f, 1.4f, -1.8f), new Vector3(1.7f, 2.8f, 1.9f), 0, false);
            Place("Prop_Computer", a, new Vector3(2.7f, .8f, -1.8f), new Vector3(1.4f, 1.6f, .75f), 180, true);
            Reserve(containment.root, "Inicio_Capsula", new Vector3(0, 0, -1));
            Label(a, "01  CONTENCAO", new Vector3(0, 2.6f, -3.84f), 180);

            a = Group("Equipamentos", energy.root);
            for (int i = 0; i < 3; i++)
                Place("Column_Pipes", a, new Vector3(-10.8f + i * 1.8f, 1.5f, 10.8f), new Vector3(.8f, 3f, .7f), 0, true);
            Place("Prop_AccessPoint", a, new Vector3(-10.8f, 1.1f, 6), new Vector3(.55f, 1.5f, 1.2f), 90, true);
            Place("Prop_Cable_3", a, new Vector3(-8, 3.1f, 8), new Vector3(5.5f, .3f, .4f), 0, false);
            Place("Fan_01", a, new Vector3(-11.8f, 2.1f, 8), new Vector3(1.2f, 1.2f, .2f), 90, false, true);
            Reserve(energy.root, "Puzzle_Bateria_Painel", new Vector3(-10, 0, 6));
            Reserve(energy.root, "Spawn_Bateria", new Vector3(-6, 0, 10));

            a = Group("Carga", storage.root);
            Place("Prop_Crate3", a, new Vector3(-2.8f, .6f, 14), new Vector3(1.3f, 1.2f, 1.3f), 15, true);
            Place("Prop_Crate4", a, new Vector3(-2.8f, .6f, 17), new Vector3(1.4f, 1.2f, 1.4f), 0, true);
            Place("Prop_Crate3", a, new Vector3(-2.8f, 1.7f, 17), new Vector3(1.1f, 1f, 1.1f), -8, true);
            Place("Prop_Crate4", a, new Vector3(1.9f, .5f, 18.7f), new Vector3(1.8f, 1f, .8f), 0, true);
            Place("Prop_Barrel_Large", a, new Vector3(-1, .75f, 18.7f), new Vector3(.8f, 1.5f, .8f), 0, true);
            Reserve(storage.root, "Coleta_Cartao", new Vector3(1.9f, 1.1f, 18.7f));

            a = Group("Bancadas", lab.root);
            Place("Prop_Computer", a, new Vector3(10.9f, .9f, 14), new Vector3(1.6f, 1.8f, .7f), -90, true);
            Place("Prop_Computer", a, new Vector3(10.9f, .9f, 17.5f), new Vector3(1.6f, 1.8f, .7f), -90, true);
            Place("Prop_ItemHolder", a, new Vector3(6, .6f, 13.1f), new Vector3(1.5f, 1.2f, .9f), 0, true);
            Place("Prop_Vent_Wide", a, new Vector3(8, 2.8f, 12.15f), new Vector3(2.5f, .6f, .2f), 0, false, true);
            Reserve(lab.root, "Registro_Narrativo", new Vector3(10.4f, 1.4f, 17.5f));

            a = Group("Posto_Controle", security.root);
            for (int i = 0; i < 3; i++)
                Place("Prop_Computer", a, new Vector3(10.9f, .8f, 22 + i * 1.8f), new Vector3(1.4f, 1.6f, .65f), -90, true);
            Place("Prop_AccessPoint", a, new Vector3(5.2f, 1.2f, 25.5f), new Vector3(.6f, 1.5f, 1f), 90, true);
            Reserve(security.root, "Terminal_Tres_Sistemas", new Vector3(10, 0, 24));
            Reserve(security.root, "Evento_Alarme", new Vector3(8, 0, 26));

            a = Group("Carga_e_Manutencao", hangar.root);
            Place("Prop_Crate4", a, new Vector3(3.4f, .9f, 31), new Vector3(1.8f, 1.8f, 1.8f), 0, true);
            Place("Prop_Crate3", a, new Vector3(3.4f, .65f, 34), new Vector3(1.4f, 1.3f, 1.4f), 10, true);
            Place("Prop_Barrel_Large", a, new Vector3(12.3f, .9f, 32), new Vector3(1f, 1.8f, 1f), 0, true);
            Place("Prop_Vent_Small", a, new Vector3(13.8f, 2.5f, 35), new Vector3(1.6f, 1f, .2f), 90, false, true);
            Place("Door_DarkMetal", a, new Vector3(8, 1.65f, 39.75f), new Vector3(5f, 3.3f, .25f), 0, false, true);
            Label(a, "SAIDA EXTERNA / RESERVADA", new Vector3(8, 2.9f, 39.5f), 0);
            Reserve(hangar.root, "Conclusao_Nivel", new Vector3(8, 0, 37));
            Reserve(hangar.root, "Rota_Futura_Drone", new Vector3(11, 1.8f, 35));
        }

        /// <summary>Nome e posição definem marcador vazio editável; sem retorno. Não adiciona triggers nem lógica futura.</summary>
        private static void Reserve(Transform parent, string name, Vector3 position)
        {
            Transform reserve = Group("RESERVADO_" + name, parent);
            reserve.position = position;
        }

        /// <summary>parent recebe jogador de visita; sem retorno. Cria câmera, listener e cápsula humana de 1,8 m.</summary>
        private static void CreatePlayer(Transform parent)
        {
            Transform player = Group("Visitante_Provisorio", parent);
            player.position = new Vector3(0, .04f, -1);
            CharacterController capsule = player.gameObject.AddComponent<CharacterController>();
            capsule.height = 1.8f; capsule.radius = .3f; capsule.center = Vector3.up * .9f;
            capsule.skinWidth = .025f; capsule.stepOffset = .25f; capsule.slopeLimit = 45f;
            Transform view = Group("Camera", player);
            view.localPosition = Vector3.up * 1.65f;
            Camera camera = view.gameObject.AddComponent<Camera>();
            camera.nearClipPlane = .05f; camera.farClipPlane = 100f; camera.fieldOfView = 72;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .04f, .07f);
            camera.tag = "MainCamera";
            view.gameObject.AddComponent<AudioListener>();
            player.gameObject.AddComponent<NaveWalkthroughController>().view = view;
        }

        /// <summary>scene identifica a nave; sem retorno. Renderiza quatro prévias URP sem salvar câmeras extras na cena.</summary>
        private static void CapturePreviews(Scene scene)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            Directory.CreateDirectory("Docs/Level01_Previas");
            Scene preview = EditorSceneManager.OpenPreviewScene(scene.path);
            foreach (GameObject root in preview.GetRootGameObjects())
            {
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 31;
                foreach (Light light in root.GetComponentsInChildren<Light>(true)) light.cullingMask = 1 << 31;
            }
            GameObject go = new GameObject("Camera_Temporaria_Previa");
            SceneManager.MoveGameObjectToScene(go, preview);
            Camera camera = go.AddComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = 1 << 31;
            camera.scene = preview;
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(preview);
            camera.fieldOfView = 78; camera.nearClipPlane = .05f; camera.farClipPlane = 80;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .04f, .07f);
            var output = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Vector3[] origins = { new Vector3(0, 1.65f, -2.8f), new Vector3(0, 1.65f, 16), new Vector3(8, 1.65f, 16), new Vector3(8, 1.65f, 29.5f) };
                Vector3[] targets = { new Vector3(0, 1.5f, 3), new Vector3(10, 1.5f, 16), new Vector3(8, 1.5f, 24), new Vector3(8, 1.4f, 39) };
                string[] names = { "01_Contencao", "04_Deposito", "05_Laboratorio", "07_Hangar" };
                for (int i = 0; i < origins.Length; i++)
                {
                    camera.transform.SetPositionAndRotation(origins[i], Quaternion.LookRotation(targets[i] - origins[i]));
                    var request = new UniversalRenderPipeline.SingleCameraRequest { destination = output };
                    if (!RenderPipeline.SupportsRenderRequest(camera, request)) return;
                    RenderPipeline.SubmitRenderRequest(camera, request);
                    RenderTexture.active = output;
                    pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                    pixels.Apply();
                    File.WriteAllBytes("Docs/Level01_Previas/" + names[i] + ".png", pixels.EncodeToPNG());
                }
                Report.AppendLine("Quatro prévias visuais em Docs/Level01_Previas.");
            }
            finally
            {
                RenderTexture.active = previous;
                output.Release();
                UnityEngine.Object.DestroyImmediate(output);
                UnityEngine.Object.DestroyImmediate(pixels);
                UnityEngine.Object.DestroyImmediate(go);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        /// <summary>scene é a nave carregada; sem retorno. Verifica piso e volume humano em toda a rota e referências de scripts/prefabs.</summary>
        private static void Validate(Scene scene)
        {
            int samples = 0;
            for (int segment = 1; segment < Route.Count; segment++)
            {
                Vector3 from = Route[segment - 1], to = Route[segment];
                int steps = Mathf.CeilToInt(Vector3.Distance(from, to) / .25f);
                for (int i = 0; i <= steps; i++)
                {
                    Vector3 point = Vector3.Lerp(from, to, (float)i / steps);
                    // Margem inferior evita considerar o piso como obstáculo; cápsula de teste tem 1,8 m e raio 0,30 m.
                    Collider[] blockers = Physics.OverlapCapsule(point + Vector3.up * .35f, point + Vector3.up * 1.55f, .3f,
                        ~0, QueryTriggerInteraction.Ignore).Where(c => !(c is CharacterController) && c.gameObject.scene == scene).ToArray();
                    if (blockers.Length > 0) throw new InvalidOperationException("Passagem bloqueada em " + point + ": " + blockers[0].name);
                    bool floor = Physics.RaycastAll(point + Vector3.up * .3f, Vector3.down, .6f)
                        .Any(hit => hit.collider.gameObject.scene == scene && !(hit.collider is CharacterController));
                    if (!floor) throw new InvalidOperationException("Piso ausente na rota: " + point);
                    samples++;
                }
            }
            int missing = scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true)
                .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
            if (missing != 0) throw new InvalidOperationException("Scripts ausentes na cena: " + missing);
            Report.AppendLine("Level01_Nave — validação da estrutura");
            Report.AppendLine("Unity: " + Application.unityVersion);
            Report.AppendLine("Módulos vinculados: " + moduleCount + "; modelos distintos: " + UsedAssets.Count);
            Report.AppendLine("Amostras de rota com piso e vão humano livre: " + samples);
            Report.AppendLine("Scripts ausentes: " + missing);
            foreach (MeshRenderer renderer in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MeshRenderer>(true)))
            {
                if (renderer.bounds.size.magnitude > 10f)
                    Report.AppendLine("Revisar limites de renderização: " + renderer.name + " " + renderer.bounds);
            }
            Report.AppendLine("Portas internas abertas; saída externa fechada, reservada para etapa futura.");
            Report.AppendLine("Sem alteração em SampleScene, Build Settings, Tags, Input Settings ou assets dos fornecedores.");
        }
    }
}
