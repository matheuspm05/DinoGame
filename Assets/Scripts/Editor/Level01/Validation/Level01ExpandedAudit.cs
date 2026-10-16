using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjetoRetorno.Level01.Editor
{
    /// <summary>Audita instâncias reais da cena por geometria de origem; exclui UI, repetição, materiais e variações de transformação.</summary>
    internal static class Level01ExpandedAudit
    {
        [Serializable] internal sealed class Row
        {
            public string name, sourcePath, package, category, geometryHash, meshOrigins;
            public int instances;
            public List<string> sectors = new List<string>();
        }
        [Serializable] private sealed class AssetReport
        {
            public string scene;
            public int distinctModels, instances;
            public List<Row> models;
        }

        /// <summary>Cena e inventário confirmado são entradas; retorna total distinto. Percorre prefabs presentes, agrupa geometria e grava Markdown/CSV/JSON auditáveis.</summary>
        internal static int WriteAssetsReport(Scene scene, Level01NaveGenerator.VisualSource[] sources)
        {
            var specs = sources.ToDictionary(s => s.path);
            var meshHashes = new Dictionary<Mesh, string>();
            var rows = new Dictionary<string, Row>();
            var sourceHash = new Dictionary<string, string>();
            foreach (Transform t in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
            {
                if (!t.gameObject.activeInHierarchy || !PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)) continue;
                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
                if (!specs.TryGetValue(path, out var spec)) continue;
                Mesh[] meshes = t.GetComponentsInChildren<MeshFilter>(false).Where(f => f.sharedMesh != null && f.GetComponent<Renderer>() != null && f.GetComponent<Renderer>().enabled)
                    .Select(f => f.sharedMesh).Concat(t.GetComponentsInChildren<SkinnedMeshRenderer>(false).Where(r => r.enabled && r.sharedMesh != null).Select(r => r.sharedMesh)).Distinct().ToArray();
                if (meshes.Length == 0) throw new InvalidOperationException("Asset sem geometria visível: " + path);
                if (!sourceHash.TryGetValue(path, out string signature))
                {
                    foreach (Mesh mesh in meshes) if (!meshHashes.ContainsKey(mesh)) meshHashes.Add(mesh, MeshSignature(mesh));
                    signature = Digest(string.Join("\n", meshes.Select(m => meshHashes[m]).Distinct().OrderBy(hash => hash)));
                    sourceHash.Add(path, signature);
                }
                if (!rows.TryGetValue(signature, out Row row))
                {
                    row = new Row { name = spec.key, sourcePath = path, package = spec.package, category = spec.category, geometryHash = signature,
                        meshOrigins = string.Join("; ", meshes.Select(m => AssetDatabase.GetAssetPath(m) + " :: " + m.name).Distinct().OrderBy(x => x)) };
                    rows.Add(signature, row);
                }
                else if (!row.sourcePath.Split('\n').Contains(path)) row.sourcePath += "\n" + path;
                row.instances++;
                Transform sector = t;
                while (sector.parent != null && sector.parent.name != "Level01_Nave_Expanded") sector = sector.parent;
                if (!row.sectors.Contains(sector.name)) row.sectors.Add(sector.name);
            }
            List<Row> sorted = rows.Values.OrderBy(r => r.category).ThenBy(r => r.name).ToList();
            var report = new AssetReport { scene = Level01NaveGenerator.ExpandedScenePath, distinctModels = sorted.Count, instances = sorted.Sum(r => r.instances), models = sorted };
            var md = new StringBuilder("# Assets do Nível 1 expandido\n\n");
            md.AppendLine("**" + report.distinctModels + " modelos visuais distintos**, presentes em **" + report.instances + " instâncias de cenário**.\n");
            md.AppendLine("Contagem obtida da cena montada, percorrendo as raízes dos prefabs ativos com renderizadores. Cada origem é assinada pela geometria das suas malhas. Cores, materiais, UVs, nomes, posição, rotação e escala das instâncias não entram na assinatura. Prefabs com a mesma geometria compartilham uma linha. Formatos alternativos OBJ/glTF não foram instanciados. A lista registra conjuntos de geometria dos modelos, sem contar cada submalha como outro asset. UI, jogador, textos e objetos vazios ficam fora.\n");
            md.AppendLine("Os hashes e as origens das malhas estão no JSON e CSV para conferência. A leitura de malhas usa a API do Editor [MeshUtility.AcquireReadOnlyMeshData](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MeshUtility.AcquireReadOnlyMeshData.html), sem abrir arquivos binários manualmente.\n");
            md.AppendLine("| # | Modelo | Caminho original | Pacote | Categoria | Setores | Instâncias |\n|---|---|---|---|---|---|---|");
            var csv = new StringBuilder("Numero,Modelo,CaminhoOriginal,Pacote,Categoria,Setores,Instancias,HashGeometria,OrigensDasMalhas\n");
            for (int i = 0; i < sorted.Count; i++)
            {
                Row r = sorted[i]; string sectors = string.Join("; ", r.sectors.OrderBy(x => x));
                md.AppendLine("| " + (i + 1) + " | " + r.name + " | " + r.sourcePath.Replace("\n", "<br>") + " | " + r.package + " | " + r.category + " | " + sectors + " | " + r.instances + " |");
                csv.AppendLine(string.Join(",", new[] { (i + 1).ToString(), r.name, r.sourcePath, r.package, r.category, sectors, r.instances.ToString(), r.geometryHash, r.meshOrigins }.Select(Csv)));
            }
            md.AppendLine("\n## Distribuição\n");
            foreach (var group in sorted.GroupBy(r => r.package)) md.AppendLine("- " + group.Key + ": " + group.Count() + " modelos distintos.");
            md.AppendLine("\n## Pacotes investigados\n\nImportados e utilizados: Barking Dog, Modular SciFi MegaKit, Free Sci-Fi Office Pack, Surveillance Camera e Low Poly Storage Pack.\n\nCity People FREE Samples está importado, mas personagens não foram usados nesta etapa.\n\nNão encontrados em Assets: Energy Cells Free, Free Sockets and Switches, Planet Earth Free e Hand Painted Nature Kit LITE. Nenhum pacote foi baixado ou instalado. A energia utiliza componentes industriais já disponíveis; uma cápsula científica específica não foi encontrada, então a contenção utiliza um berço tecnológico montado com modelos existentes.");
            Directory.CreateDirectory("Docs/Relatorios/Nivel01_Expandido");
            File.WriteAllText("Docs/Relatorios/Nivel01_Expandido/Level01_Expanded_Assets.md", md.ToString());
            File.WriteAllText("Docs/Relatorios/Nivel01_Expandido/Level01_Expanded_Assets.csv", csv.ToString(), new UTF8Encoding(true));
            File.WriteAllText("Docs/Relatorios/Nivel01_Expandido/Level01_Expanded_Assets.json", JsonUtility.ToJson(report, true));
            return sorted.Count;
        }

        /// <summary>mesh é a malha original; retorna hash de triângulos normalizados. Ignora UV/material e ordem dos vértices, usando apenas a geometria.</summary>
        private static string MeshSignature(Mesh mesh)
        {
            using (var data = MeshUtility.AcquireReadOnlyMeshData(mesh))
            using (var vertices = new NativeArray<Vector3>(data[0].vertexCount, Allocator.Temp))
            {
                data[0].GetVertices(vertices);
                Bounds bounds = mesh.bounds;
                Vector3 size = bounds.size;
                var points = new Vector3[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 p = vertices[i] - bounds.center;
                    points[i] = new Vector3(size.x < .000001f ? 0 : p.x / size.x, size.y < .000001f ? 0 : p.y / size.y, size.z < .000001f ? 0 : p.z / size.z);
                }
                var triangles = new List<string>();
                for (int sub = 0; sub < data[0].subMeshCount; sub++)
                {
                    var desc = data[0].GetSubMesh(sub);
                    using (var indices = new NativeArray<int>(desc.indexCount, Allocator.Temp))
                    {
                        data[0].GetIndices(indices, sub);
                        for (int i = 0; i + 2 < indices.Length; i += 3)
                        {
                            Vector3 a = points[indices[i]], b = points[indices[i + 1]], c = points[indices[i + 2]];
                            int[] edges = { Q((a - b).sqrMagnitude), Q((b - c).sqrMagnitude), Q((c - a).sqrMagnitude) };
                            int[] radii = { Q(a.sqrMagnitude), Q(b.sqrMagnitude), Q(c.sqrMagnitude) };
                            Array.Sort(edges); Array.Sort(radii);
                            triangles.Add(string.Join(",", edges) + ":" + string.Join(",", radii));
                        }
                    }
                }
                triangles.Sort(StringComparer.Ordinal);
                return Digest(string.Join("\n", triangles));
            }
        }

        /// <summary>value é uma medida normalizada; retorna inteiro com tolerância de 0,00001 para pequenas diferenças de importação.</summary>
        private static int Q(float value) { return Mathf.RoundToInt(value * 100000); }

        /// <summary>value é o texto geométrico; retorna SHA-256 estável usado para agrupar modelos equivalentes.</summary>
        private static string Digest(string value)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>value é uma célula; retorna texto CSV entre aspas, preservando caminhos e possíveis quebras de linha.</summary>
        private static string Csv(string value) { return "\"" + value.Replace("\"", "\"\"") + "\""; }
    }
}
