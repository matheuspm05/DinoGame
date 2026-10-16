using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjetoRetorno.Level01.Editor
{
    public static partial class Level01NaveGenerator
    {
        internal sealed class ExpandedRoom
        {
            public string name, wall, trim;
            public Vector2 center, size;
            public float height;
            public Color color;
            public Transform root;
            public readonly Dictionary<string, Vector2> openings = new Dictionary<string, Vector2>();
        }
        internal static readonly List<ExpandedRoom> ExpandedRooms = new List<ExpandedRoom>();
        internal static readonly List<Vector3[]> ExpandedLinks = new List<Vector3[]>();

        /// <summary>world recebe os setores; sem retorno. Define oito salas e sete conexões compactas, com percurso legível e dois desvios opcionais.</summary>
        private static void BuildExpandedLayout(Transform world)
        {
            Color cold = new Color(.62f, .8f, 1), white = new Color(.85f, .92f, 1), amber = new Color(1, .61f, .3f);
            ExpandedRoom containment = ER(world, "01_Contencao", 0, 0, 12, 10, cold, "WallAstra_Straight", "TopAstra_Straight");
            Transform corridors = Group("02_Corredores", world);
            ExpandedRoom corridor = ER(corridors, "Eixo_Contencao_Deposito", 0, 9, 4, 8, white);
            ExpandedRoom energy = ER(world, "03_Energia", -12, 9, 14, 12, amber, "WallBand_Straight", "TopCables_Straight");
            ExpandedRoom energyLink = ER(corridors, "Acesso_Energia", -3.5f, 9, 3, 4, white);
            ExpandedRoom storage = ER(world, "04_Deposito", 0, 21, 16, 16, new Color(1, .83f, .6f), "WallBand_Straight");
            ExpandedRoom lab = ER(world, "05_Laboratorio", 20, 21, 20, 16, new Color(.35f, .78f, 1), "WallAstra_Straight_Divided", "TopPlastic_Straight");
            ExpandedRoom labLink = ER(corridors, "Acesso_Laboratorio", 9, 21, 2, 4, white);
            ExpandedRoom security = ER(world, "06_Seguranca", 20, 39, 14, 12, cold, "WallAstra_Straight", "TopAstra_Straight");
            ExpandedRoom securityLink = ER(corridors, "Acesso_Seguranca", 20, 31, 4, 4, white);
            ExpandedRoom hangar = ER(world, "07_Hangar", 20, 60, 26, 22, white, "WallBand_Straight", "TopCables_Straight", 6);
            ExpandedRoom hangarLink = ER(corridors, "Acesso_Hangar", 20, 47, 5, 4, white);
            ExpandedRoom machines = ER(world, "08_Maquinas", -15, 25, 10, 16, new Color(1, .42f, .22f), "WallBand_Straight", "TopCables_Straight", 4);
            ExpandedRoom machinesLink = ER(corridors, "Acesso_Maquinas", -9, 23, 2, 4, amber);
            ExpandedRoom maintenance = ER(world, "09_Manutencao", 37, 37, 12, 10, new Color(.55f, 1, .8f), "WallAstra_Straight_Divided");
            ExpandedRoom maintenanceLink = ER(corridors, "Acesso_Manutencao", 29, 37, 4, 4, white);
            Open(containment, "N", 0, 3); Open(corridor, "S", 0, 3); Open(corridor, "N", 0, 3);
            Open(corridor, "W", 0, 3.4f); Open(energyLink, "E", 0, 3.4f); Open(energyLink, "W", 0, 3.4f); Open(energy, "E", 0, 3.4f);
            Open(storage, "S", 0, 3); Open(storage, "E", 0, 3.4f); Open(labLink, "W", 0, 3.4f); Open(labLink, "E", 0, 3.4f); Open(lab, "W", 0, 3.4f);
            Open(lab, "N", 0, 3.4f); Open(securityLink, "S", 0, 3.4f); Open(securityLink, "N", 0, 3.4f); Open(security, "S", 0, 3.4f);
            Open(security, "N", 0, 4); Open(hangarLink, "S", 0, 4); Open(hangarLink, "N", 0, 4); Open(hangar, "S", 0, 4);
            Open(storage, "W", 2, 3.4f); Open(machinesLink, "E", 0, 3.4f); Open(machinesLink, "W", 0, 3.4f); Open(machines, "E", -2, 3.4f);
            Open(security, "E", -2, 3.4f); Open(maintenanceLink, "W", 0, 3.4f); Open(maintenanceLink, "E", 0, 3.4f); Open(maintenance, "W", 0, 3.4f);
            foreach (ExpandedRoom room in ExpandedRooms) ExpandedShell(room);
            ExpandedPortal(containment, new Vector3(0, 0, 5), 0, 3, "CORREDOR / DEPOSITO", "Door_Frame_Square");
            ExpandedPortal(energy, new Vector3(-5, 0, 9), 90, 3.4f, "03  ENERGIA", "Door_Frame_A");
            ExpandedPortal(storage, new Vector3(0, 0, 13), 0, 3, "04  DEPOSITO", "Door_Frame_Square");
            ExpandedPortal(lab, new Vector3(10, 0, 21), 90, 3.4f, "05  LABORATORIO", "Door_Frame_SquareTall");
            ExpandedPortal(security, new Vector3(20, 0, 33), 0, 3.4f, "06  SEGURANCA", "Door_Frame_Square");
            ExpandedPortal(hangar, new Vector3(20, 0, 49), 0, 4, "07  HANGAR", "Door_Frame_A");
            ExpandedPortal(machines, new Vector3(-10, 0, 23), 90, 3.4f, "08  MAQUINAS / OPCIONAL", "Door_Frame_A");
            ExpandedPortal(maintenance, new Vector3(31, 0, 37), 90, 3.4f, "09  MANUTENCAO", "Door_Frame_Square");
            Route.AddRange(new[] { new Vector3(0, 0, -1), new Vector3(0, 0, 9), new Vector3(-12, 0, 9), new Vector3(0, 0, 9),
                new Vector3(0, 0, 21), new Vector3(0, 0, 23), new Vector3(-15, 0, 23), new Vector3(-15, 0, 25), new Vector3(-15, 0, 23),
                new Vector3(0, 0, 23), new Vector3(0, 0, 21), new Vector3(20, 0, 21), new Vector3(20, 0, 39), new Vector3(20, 0, 37),
                new Vector3(37, 0, 37), new Vector3(20, 0, 37), new Vector3(20, 0, 39), new Vector3(20, 0, 60) });
            ExpandedLinks.Add(Route.ToArray());
        }

        /// <summary>Dados definem o retângulo, identidade e pé-direito; retorna sala registrada na lista de construção e auditoria.</summary>
        private static ExpandedRoom ER(Transform parent, string name, float x, float z, float width, float depth, Color color,
            string wall = "WallAstra_Straight", string trim = "TopSimple_Straight", float height = 3.8f)
        {
            var room = new ExpandedRoom { name = name, center = new Vector2(x, z), size = new Vector2(width, depth), color = color,
                wall = wall, trim = trim, height = height, root = Group(name, parent) };
            ExpandedRooms.Add(room); return room;
        }

        /// <summary>Sala, lado, deslocamento e largura definem vão transitável; sem retorno. Registra a interrupção da parede.</summary>
        private static void Open(ExpandedRoom room, string side, float offset, float width) { room.openings[side] = new Vector2(offset, width); }

        /// <summary>room fornece dimensões; sem retorno. Monta módulos alinhados, piso contínuo, teto, rodapés e luzes sem sombras.</summary>
        private static void ExpandedShell(ExpandedRoom room)
        {
            Transform shell = Group("Arquitetura", room.root);
            int nx = Mathf.CeilToInt(room.size.x / 4), nz = Mathf.CeilToInt(room.size.y / 4);
            float dx = room.size.x / nx, dz = room.size.y / nz;
            for (int x = 0; x < nx; x++) for (int z = 0; z < nz; z++)
            {
                Vector3 p = new Vector3(room.center.x - room.size.x / 2 + dx * (x + .5f), -.12f, room.center.y - room.size.y / 2 + dz * (z + .5f));
                Place("Floor_01", shell, p, new Vector3(dx, .24f, dz), 0, true);
                Place("Roof_01", shell, new Vector3(p.x, room.height + .12f, p.z), new Vector3(dx, .24f, dz), 0, false);
            }
            foreach (string side in new[] { "N", "S", "E", "W" })
            {
                bool horizontal = side == "N" || side == "S";
                float span = horizontal ? room.size.x : room.size.y;
                float line = horizontal ? room.center.y + (side == "N" ? 1 : -1) * room.size.y / 2 : room.center.x + (side == "E" ? 1 : -1) * room.size.x / 2;
                float middle = horizontal ? room.center.x : room.center.y;
                if (room.openings.TryGetValue(side, out Vector2 opening))
                {
                    ExpandedWall(room, shell, horizontal, line, middle - span / 2, middle + opening.x - opening.y / 2);
                    ExpandedWall(room, shell, horizontal, line, middle + opening.x + opening.y / 2, middle + span / 2);
                }
                else ExpandedWall(room, shell, horizontal, line, middle - span / 2, middle + span / 2);
            }
            Transform lights = Group("Iluminacao", room.root);
            int lx = room.size.x >= 18 ? 2 : 1, lz = room.size.y >= 16 ? 2 : 1;
            if (room.height >= 6) lz = 3;
            for (int x = 0; x < lx; x++) for (int z = 0; z < lz; z++)
            {
                float px = room.center.x + (x - (lx - 1) * .5f) * room.size.x * .48f;
                float pz = room.center.y + (z - (lz - 1) * .5f) * room.size.y * .35f;
                AddLight(lights, new Vector3(px, room.height - .7f, pz), Color.Lerp(room.color, Color.white, .3f), room.height >= 6 ? 6 : 3.5f,
                    Mathf.Max(7, Mathf.Min(12, Mathf.Max(room.size.x / lx, room.size.y / lz))));
                Place("Prop_Light_Wide", lights, new Vector3(px, room.height - .13f, pz), new Vector3(1.6f, .16f, .5f), 0, false);
            }
        }

        /// <summary>Sala, pai, orientação e intervalo definem parede; sem retorno. Subtrai paredes compartilhadas e encaixa módulos e rodapés.</summary>
        private static void ExpandedWall(ExpandedRoom room, Transform parent, bool horizontal, float line, float start, float end)
        {
            if (end - start < .01f) return;
            var pending = new List<Vector2> { new Vector2(start, end) };
            foreach (WallInterval used in BuiltWalls.Where(w => w.horizontal == horizontal && Mathf.Abs(w.line - line) < .01f))
            {
                var next = new List<Vector2>();
                foreach (Vector2 p in pending)
                {
                    if (used.end <= p.x || used.start >= p.y) next.Add(p);
                    else { if (used.start > p.x) next.Add(new Vector2(p.x, used.start)); if (used.end < p.y) next.Add(new Vector2(used.end, p.y)); }
                }
                pending = next;
            }
            foreach (Vector2 interval in pending)
            {
                BuiltWalls.Add(new WallInterval { horizontal = horizontal, line = line, start = interval.x, end = interval.y });
                int count = Mathf.CeilToInt((interval.y - interval.x) / 3);
                float length = (interval.y - interval.x) / count;
                for (int i = 0; i < count; i++)
                {
                    float coordinate = interval.x + length * (i + .5f);
                    Vector3 p = horizontal ? new Vector3(coordinate, room.height / 2, line) : new Vector3(line, room.height / 2, coordinate);
                    Place(room.wall, parent, p, new Vector3(length, room.height, .22f), horizontal ? 0 : 90, true, true);
                    Place(room.trim, parent, new Vector3(p.x, room.height - .16f, p.z), new Vector3(length, .32f, .26f), horizontal ? 0 : 90, false, true);
                    Place(room.name.Contains("Maquinas") || room.name.Contains("Energia") ? "BottomMetal_Straight" : "BottomSimple_Straight", parent,
                        new Vector3(p.x, .1f, p.z), new Vector3(length, .2f, .27f), horizontal ? 0 : 90, false, true);
                }
            }
        }

        /// <summary>Sala, centro, rotação, largura, placa e modelo definem portal; sem retorno. Mantém o vão aberto e a folha recolhida lateralmente.</summary>
        private static void ExpandedPortal(ExpandedRoom room, Vector3 p, float yaw, float width, string text, string frame)
        {
            Transform parent = Group("Passagem_" + text, room.root);
            Place(frame, parent, p + Vector3.up * 1.65f, new Vector3(width + .4f, 3.3f, .24f), yaw, false, true);
            Place("Door_Left_01", parent, p + Quaternion.Euler(0, yaw, 0) * new Vector3(width / 2 + .5f, 1.4f, .16f), new Vector3(.75f, 2.8f, .18f), yaw, false, true);
            Label(parent, text, p + Vector3.up * 3.55f + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, -.28f), yaw);
        }
    }
}
