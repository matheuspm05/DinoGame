using System.Linq;
using UnityEngine;

namespace ProjetoRetorno.Level01.Editor
{
    public static partial class Level01NaveGenerator
    {
        /// <summary>Sem argumentos/retorno. Distribui equipamentos em áreas funcionais, preservando eixos centrais e acessos às salas.</summary>
        private static void DecorateExpanded()
        {
            ExpandedRoom c = ExpandedRooms.Single(r => r.name == "01_Contencao");
            P("Column_Hollow", c, -4, -2, 1.5f, 2.8f, 1.5f);
            P("Prop_ItemHolder", c, -4, 1.6f, 1.4f, 1.2f, 1.2f);
            P("Column_Astra", c, 4.5f, -2, .7f, 3.4f, .7f);
            float containmentBench = Top(P("Office_Table Metal Variant", c, 3.5f, 1.5f, 3, .85f, 1.3f));
            P("Office_PC 2", c, 3.5f, 1.5f, .9f, .65f, .65f, 0, containmentBench);
            P("Office_Stool", c, 3.5f, -.2f, .6f, .7f, .6f);
            Mount("Prop_AccessPoint", c, -5.7f, 1.6f, 2.5f, .75f, 1.1f, .15f, 90);
            Mount("Door_Arch_01", c, 0, 1.5f, -4.75f, 3.2f, 3, .2f);
            Mount("Light_01", c, -4, 2.8f, -4.5f, 1, .25f, .3f);
            Mount("Prop_Cable_1", c, -5.7f, 2.6f, -1, .2f, .35f, 5, 90);
            Reserve(c.root, "Contencao_Abertura_Futura", new Vector3(-4, 0, -2));

            ExpandedRoom e = ExpandedRooms.Single(r => r.name == "03_Energia");
            P("Column_Pipes", e, -4.8f, -3.2f, 1.2f, 3.3f, 1.2f);
            P("Column_Pipes", e, -4.8f, 3.2f, 1.2f, 3.3f, 1.2f);
            P("Prop_Clamp", e, -3, -3, 2.2f, 1.7f, 2);
            P("Storage_Electric_Box_01", e, -.3f, -4.8f, 1.7f, 1.7f, .8f);
            P("Storage_Amp_Rack_01", e, 2.2f, -4.8f, 1.4f, 2.1f, .85f);
            P("Office_Server Rack", e, -5.8f, .2f, 1, 2.3f, 1, 90);
            Mount("Prop_Vent_Big", e, -6.65f, 2.3f, 1, 3, 1.3f, .2f, 90);
            Mount("Prop_Cable_3", e, -1.5f, 3, -5.5f, 5, .4f, .25f);
            Mount("Prop_Computer", e, -1, 1.5f, 5.6f, 1.5f, 1.1f, .3f);
            Mount("Prop_Light_Small", e, -1, 2.5f, 5.5f, .5f, .25f, .2f);
            Label(e.root, "ENERGIA AUXILIAR / CIRCUITO OFFLINE", new Vector3(-12, 2.8f, 14.6f), 0);
            Reserve(e.root, "Puzzle_Restaurar_Energia", new Vector3(-12, 0, 12));

            ExpandedRoom d = ExpandedRooms.Single(r => r.name == "04_Deposito");
            P("Storage_Storage_Rack_01", d, -6, -5, 3.2f, 2.5f, 1.3f);
            P("Storage_Storage_Rack_02", d, -6, 5.2f, 3.2f, 2.5f, 1.3f);
            float pallet = Top(P("Storage_EUR-Pallet", d, 5.6f, -5.7f, 2.1f, .2f, 1.5f));
            P("Storage_Crate_01", d, 5.5f, -5.7f, 1.4f, 1.2f, 1.2f, 0, pallet);
            P("Storage_Crate_02", d, 3.4f, -5.7f, 1.4f, 1.2f, 1.2f);
            P("Storage_Container_01_Blue", d, 5.8f, 5, 2, 2.2f, 2.5f);
            P("Prop_Crate3", d, 3.6f, 5.8f, 1.4f, 1.3f, 1.4f);
            P("Prop_Crate4", d, .9f, 6, 1.5f, 1.5f, 1.5f);
            P("Storage_Barrel_01", d, -3.4f, -6.5f, .85f, 1.3f, .85f);
            P("Storage_Barrel_02", d, -2.2f, -6.5f, .85f, 1.3f, .85f);
            P("Prop_Barrel_Large", d, -1, -6.5f, .85f, 1.5f, .85f);
            P("Storage_Cabinet_01", d, -6.9f, -.5f, 1.2f, 2, .8f, 90);
            Mount("Prop_Light_Floor", d, 7.6f, .2f, -.2f, .25f, .3f, 1, 90);
            Label(d.root, "CARGA A ESQUERDA / LABORATORIO A DIREITA", new Vector3(0, 2.8f, 28.6f), 0);

            ExpandedRoom lab = ExpandedRooms.Single(r => r.name == "05_Laboratorio");
            foreach (float x in new[] { -6f, 6f })
            {
                float bench = Top(P("Office_Table Metal Variant", lab, x, -4.6f, 3.2f, .9f, 1.3f));
                P("Office_PC 2", lab, x - .7f, -4.6f, .9f, .65f, .65f, 0, bench);
                P("Office_Office Chair", lab, x, -2.9f, .75f, 1.1f, .75f, 180);
                P("Office_Tray", lab, x + .8f, -4.6f, .5f, .12f, .4f, 0, bench);
            }
            float researchBench = Top(P("Office_Drawer Table Long Blue Plastic Variant", lab, 6.2f, 4.8f, 4, .9f, 1.6f));
            P("Office_Mechanical arm 1", lab, 5.5f, 4.8f, 1.3f, 1.5f, 1.2f, 180, researchBench);
            P("Office_Mechanical arm 2", lab, 7.5f, 4.8f, 1.3f, 1.5f, 1.2f, 180, researchBench);
            P("Office_Drawer 2", lab, -7.2f, 5.3f, 1.3f, 1.1f, 1);
            P("Office_Stool", lab, 5.5f, 3.1f, .65f, .65f, .65f);
            P("Office_Server Rack", lab, -8.5f, -6.5f, 1.1f, 2.4f, 1.1f);
            P("Office_Shelf without Crates", lab, -6.5f, 6.4f, 2.5f, 2.3f, 1);
            float island = Top(P("Office_Table Metal Variant", lab, 4.2f, -.6f, 2.8f, .9f, 1.3f));
            P("Office_PC 2", lab, 4.2f, -.6f, .9f, .65f, .65f, 0, island);
            P("Office_Tray", lab, 5.1f, -.6f, .45f, .12f, .35f, 0, island);
            P("Office_Stool", lab, 4.2f, -2.1f, .6f, .65f, .6f);
            P("Office_Server Rack", lab, -8.4f, 3.6f, 1.1f, 2.4f, 1.1f);
            Mount("Office_Tv 32 Inch", lab, 0, 2.2f, -7.7f, 2.2f, 1.2f, .4f);
            Place("WallWindow_Straight", lab.root, new Vector3(26, 1.4f, 22.5f), new Vector3(4, 2.8f, .14f), 0, true, true);
            Place("WallAstra_Straight_Flat_Window", lab.root, new Vector3(13.5f, 1.4f, 24.4f), new Vector3(3.8f, 2.8f, .2f), 0, true, true);
            P("Column_Round", lab, 9.3f, 6.8f, .65f, 3.8f, .65f);
            P("Column_Round", lab, -9.3f, -6.8f, .65f, 3.8f, .65f);
            Reserve(lab.root, "Pesquisa_Futura", new Vector3(26, 0, 25.8f));

            ExpandedRoom s = ExpandedRooms.Single(r => r.name == "06_Seguranca");
            float securityBench = Top(P("Office_Table Metal Variant", s, -3.6f, 4, 3.5f, .9f, 1.2f));
            P("Prop_Computer", s, -3.6f, 4, 1.8f, 1.2f, .55f, 180, securityBench);
            P("Office_Office Chair", s, -3.6f, 2.3f, .75f, 1.1f, .75f, 180);
            P("Office_Server Rack", s, 5.7f, 3.8f, 1.1f, 2.4f, 1.1f);
            P("Storage_Locker", s, -5.8f, -3.8f, 1.2f, 2, .85f);
            float terminalBench = Top(P("Office_Table Metal Variant", s, 3.5f, 4, 3.5f, .9f, 1.2f));
            P("Office_PC 2", s, 3.5f, 4, .9f, .65f, .65f, 0, terminalBench);
            P("Office_Office Chair", s, 3.5f, 2.3f, .75f, 1.1f, .75f, 180);
            Mount("Office_Tv 32 Inch", s, -3.6f, 2.6f, 5.65f, 2.3f, 1.25f, .4f, 180);
            Mount("Office_Tv 32 Inch", s, 3.5f, 2.6f, 5.65f, 2.3f, 1.25f, .4f, 180);
            Mount("Prop_AccessPoint", s, -6.7f, 1.6f, 0, .85f, 1.3f, .2f, 90);
            Mount("Surveillance", s, 5.8f, 3, -4.5f, .7f, .6f, .9f, 210);
            Reserve(s.root, "Terminal_Controle_Hangar", new Vector3(23.5f, 0, 43));
            Label(s.root, "TERMINAL HANGAR", new Vector3(23.5f, 3.1f, 44.5f), 0);

            ExpandedRoom h = ExpandedRooms.Single(r => r.name == "07_Hangar");
            string[] platforms = { "Platform_3Plates", "Platform_DarkPlates", "Platform_Metal", "Platform_Metal2", "Platform_Squares" };
            Vector2[] bays = { new Vector2(-8, -7), new Vector2(-8, 0), new Vector2(-8, 7), new Vector2(8, -7), new Vector2(8, 7) };
            for (int i = 0; i < bays.Length; i++)
            {
                Place(platforms[i], h.root, new Vector3(h.center.x + bays[i].x, .07f, h.center.y + bays[i].y), new Vector3(4, .14f, 4), 0, true);
                P(i % 2 == 0 ? "Storage_Container_02_Blue" : "Prop_Crate4", h, bays[i].x, bays[i].y, 2.7f, 2.8f, 2.8f, 0, .14f);
            }
            P("Column_MetalSupport", h, -11.8f, -7, 1.3f, 5.8f, 1.3f);
            P("Column_MetalSupport", h, 11.8f, -7, 1.3f, 5.8f, 1.3f);
            P("Column_MetalSupport", h, -11.8f, 7, 1.3f, 5.8f, 1.3f);
            P("Column_MetalSupport", h, 11.8f, 7, 1.3f, 5.8f, 1.3f);
            P("Prop_Rail_2", h, -8, -4.7f, 3, 1.1f, .2f);
            P("Prop_Rail_3", h, 8, 4.7f, 3, 1.1f, .2f);
            P("Prop_Chest", h, 10.5f, 0, 2, 1.4f, 1.5f);
            P("Storage_Barrel_04", h, -5.2f, 8.6f, 1, 1.5f, 1);
            Place("Door_DarkMetal", h.root, new Vector3(20, 2.8f, 70.7f), new Vector3(7, 5.6f, .3f), 0, false, true);
            Mount("Prop_Light_Corner", h, -4, 4, 10.5f, 1, 1, .3f);
            Mount("Prop_Light_Corner", h, 4, 4, 10.5f, 1, 1, .3f);
            Mount("Prop_Vent_Wide", h, -12.7f, 4, 0, 3, 1.8f, .25f, 90);
            Label(h.root, "SAIDA EXTERNA / PORTAO SELADO", new Vector3(20, 4.7f, 70.3f), 0);
            Reserve(h.root, "Conclusao_Nivel", new Vector3(20, 0, 68));

            ExpandedRoom m = ExpandedRooms.Single(r => r.name == "08_Maquinas");
            P("Column_Simple", m, -3.8f, -5.8f, .8f, 4, .8f);
            P("Column_Pipes", m, -3.8f, -.8f, 1, 3.6f, 1);
            P("Column_Hollow", m, -3.8f, 5.5f, 1.3f, 3.4f, 1.3f);
            P("Storage_Electric_Box_02", m, 2.8f, 5.8f, 1.7f, 2.1f, 1);
            Mount("Fan_01", m, 0, 3.7f, 5, 2, .3f, 2);
            Mount("Prop_Fan_Small", m, 0, 3.6f, -.5f, 1.5f, .4f, 1.5f);
            Mount("Prop_PipeHolder", m, -4.5f, 2.6f, -3, .8f, 1.2f, .3f, 90);
            Mount("Prop_Vent_Small", m, 4.65f, 2.8f, 2.8f, 1.2f, 1.2f, .2f, 90);
            Mount("Prop_Cable_3", m, -4.4f, 3.3f, 0, .3f, .4f, 10, 90);
            Mount("Prop_Light_Small", m, 0, 3.5f, 7.5f, .7f, .3f, .2f);
            Mount("Door_Simple", m, 0, 1.5f, -7.7f, 2.4f, 3, .2f);

            ExpandedRoom t = ExpandedRooms.Single(r => r.name == "09_Manutencao");
            P("Storage_Cabinet_02", t, 4.6f, -2.8f, 1.5f, 2, .9f);
            P("Storage_Cabinet_03", t, 4.6f, -.4f, 1.5f, 2, .9f);
            P("Storage_Cabinet_04", t, 4.6f, 2.5f, 1.5f, 2, .9f);
            float maintenanceBench = Top(P("Office_Table Metal Variant", t, 0, 3.4f, 3.8f, .9f, 1.4f));
            P("Prop_Clamp", t, -.7f, 3.4f, 1, .65f, .7f, 0, maintenanceBench);
            P("Prop_PipeHolder", t, .8f, 3.4f, .7f, .7f, .7f, 0, maintenanceBench);
            P("Office_Stool", t, 0, 1.8f, .6f, .65f, .6f);
            P("Office_Shelf without Crates", t, -3.7f, -3.8f, 2.8f, 2.3f, 1);
            Mount("Prop_AccessPoint", t, -2.3f, 1.5f, 4.6f, .8f, 1.1f, .15f);

            foreach (ExpandedRoom corridor in ExpandedRooms.Where(r => r.root.parent.name == "02_Corredores" && r.size.y >= 4))
            {
                // Acessos curtos recebem apenas luzes superiores; o eixo comprido recebe detalhes fora da faixa central.
                if (corridor.name != "Eixo_Contencao_Deposito") continue;
                Mount("Prop_Vent_Small", corridor, 1.75f, 2.7f, -2.5f, .9f, .8f, .2f, 90);
                Mount("Surveillance", corridor, 1.5f, 3.1f, 2.8f, .65f, .55f, .85f, 210);
                Mount("Prop_Cable_1", corridor, 1.72f, 3.4f, 0, .2f, .25f, 5, 90);
                Label(corridor.root, "ENERGIA  <     DEPOSITO  ^", new Vector3(0, 2.8f, 12.5f), 0);
            }
        }

        /// <summary>Modelo, sala, posição no piso, limites, rotação e cota definem prop; retorna suporte. Ajusta uniformemente, mantém proporções e apoia a base.</summary>
        private static Transform P(string key, ExpandedRoom room, float x, float z, float width, float height, float depth, float yaw = 0, float floor = 0)
        {
            return ExpandedProp(key, room, new Vector3(x, floor, z), new Vector3(width, height, depth), yaw, true);
        }

        /// <summary>support é o suporte sólido; retorna a cota superior para apoiar equipamentos sem flutuar sobre mesas e paletes.</summary>
        private static float Top(Transform support)
        {
            return support.position.y + support.GetComponent<BoxCollider>().size.y * .5f;
        }

        /// <summary>Modelo, sala, centro, limites e rotação definem objeto montado; sem retorno. Mantém proporções e evita colisores em detalhes elevados.</summary>
        private static void Mount(string key, ExpandedRoom room, float x, float y, float z, float width, float height, float depth, float yaw = 0)
        {
            ExpandedProp(key, room, new Vector3(x, y, z), new Vector3(width, height, depth), yaw, false);
        }

        /// <summary>Modelo, sala, posição, caixa máxima, rotação e apoio são entradas; retorna instância com referência preservada e escala uniforme para objetos.</summary>
        private static Transform ExpandedProp(string key, ExpandedRoom room, Vector3 local, Vector3 maximum, float yaw, bool grounded)
        {
            Transform wrapper = Place(key, room.root, new Vector3(room.center.x + local.x, local.y, room.center.y + local.z), maximum, yaw, grounded,
                (!grounded || key.Contains("Rail")) && maximum.z < maximum.x);
            Transform fit = wrapper.Find("Ajuste");
            Vector3 scale = fit.localScale;
            float uniform = Mathf.Min(scale.x, scale.y, scale.z);
            Vector3 actual = new Vector3(maximum.x * uniform / scale.x, maximum.y * uniform / scale.y, maximum.z * uniform / scale.z);
            fit.localScale = Vector3.one * uniform;
            if (grounded)
            {
                wrapper.GetComponent<BoxCollider>().size = actual;
                wrapper.position += Vector3.up * (actual.y * .5f);
            }
            return wrapper;
        }
    }
}
