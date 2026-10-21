using System;
using System.Linq;
using ProjetoRetorno.Gameplay;
using UnityEditor;
using UnityEngine;

namespace ProjetoRetorno.Level01.Editor
{
    public static partial class Level01PolishSetup
    {
        /// <summary>level limita os objetos do cenário. Sem retorno. Alinha sólidos ao visual real e usa composição simples para não preencher vãos de portas e móveis.</summary>
        private static void ReviewCollisions(Level01GameManager level)
        {
            int reviewed=0, adjusted=0, added=0;
            var wrappers=level.transform.parent.GetComponentsInChildren<Transform>(true).Where(t=>t.Find("Ajuste")!=null).ToArray();
            foreach(var root in wrappers)
            {
                if(!root.gameObject.activeInHierarchy || root.GetComponentInParent<DoorController>()!=null || root.GetComponentsInChildren<Renderer>(true).Length==0)continue;
                if(root.name.StartsWith("Floor_01_")||root.name.StartsWith("Roof_01_"))
                {
                    var existing=root.GetComponent<BoxCollider>();
                    if(existing==null||!existing.enabled||existing.isTrigger)throw new InvalidOperationException("Arquitetura sem proteção: "+root.name);
                    reviewed++;continue; // pisos têm superfície sem espessura; conservar a laje física já alinhada
                }
                bool frame=root.name.StartsWith("Door_Frame_");
                bool table=root.name.StartsWith("Office_Table ")||root.name.StartsWith("Office_Drawer Table");
                bool solid=frame||table||new[]{"Wall","Column_","Storage_","Office_","Prop_Computer","Prop_Crate","Prop_Barrel",
                    "Prop_Chest","Platform_","Door_Left_","Door_Arch_","Door_Simple","Prop_Vent_Big","Prop_Vent_Wide","Prop_Fan","Fan_"}
                    .Any(prefix=>root.name.StartsWith(prefix));
                if(!solid)continue;
                reviewed++;
                var bounds=Level01GameplaySetup.LocalBounds(root);
                var active=root.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&!c.isTrigger).ToArray();
                int before=root.GetComponentsInChildren<BoxCollider>(true).Length;
                if(frame) ConfigureFrame(root,bounds);
                else if(table) ConfigureTable(root,bounds);
                else
                {
                    foreach(var collider in active)collider.enabled=false;
                    int pieces=0;
                    foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if(filter.sharedMesh==null||!filter.gameObject.activeInHierarchy)continue;
                        Transform child=Child(filter.transform,"Colisao_Solida");
                        var collider=Add<BoxCollider>(child.gameObject);
                        collider.center=filter.sharedMesh.bounds.center;
                        Vector3 scale=filter.transform.lossyScale;
                        collider.size=Vector3.Max(filter.sharedMesh.bounds.size,
                            new Vector3(.02f/Mathf.Max(.001f,Mathf.Abs(scale.x)),.02f/Mathf.Max(.001f,Mathf.Abs(scale.y)),.02f/Mathf.Max(.001f,Mathf.Abs(scale.z))));
                        collider.enabled=true;collider.isTrigger=false;pieces++;
                    }
                    if(pieces==0)
                    {
                        // Modelos estáticos importados como SkinnedMeshRenderer usam o volume medido pela malha deformada.
                        var collider=Add<BoxCollider>(root.gameObject);collider.center=bounds.center;
                        collider.size=Vector3.Max(bounds.size,Vector3.one*.02f);collider.enabled=true;collider.isTrigger=false;
                    }
                }
                adjusted++;
                added+=Mathf.Max(0,root.GetComponentsInChildren<BoxCollider>(true).Length-before);
                Report.Add("COLLIDER|"+root.name+"|geometria local="+bounds+"|sólidos="+root.GetComponentsInChildren<Collider>(true).Count(c=>c.enabled&&!c.isTrigger));
            }
            // Colisores dinâmicos continuam pertencendo exclusivamente à folha que já abre por Translate.
            foreach(var door in new[]{level.initialRoom.initialDoor,level.laboratoryDoor,level.hangarDoor,level.exitDoor})
            {
                var bounds=Level01GameplaySetup.LocalBounds(door.movingLeaf);
                door.solidCollider.center=bounds.center;door.solidCollider.size=Vector3.Max(bounds.size,new Vector3(.1f,.1f,.18f));
                door.solidCollider.enabled=true;door.solidCollider.isTrigger=false;
            }
            ReviewManualObjects(level);
            Physics.SyncTransforms();
            Report.Add("Revisão: "+reviewed+" objetos arquitetônicos/sólidos; "+adjusted+" compostos/alinhados; "+added+" caixas auxiliares novas. Quatro folhas móveis preservadas.");
        }


        /// <summary>level indica a cena. Sem retorno. Revê também modelos avulsos adicionados manualmente, reaproveitando colisores que já cobrem suas malhas.</summary>
        private static void ReviewManualObjects(Level01GameManager level)
        {
            int models=0,created=0;
            foreach(var root in level.gameObject.scene.GetRootGameObjects())
            {
                if(root==level.transform.root.gameObject||root.GetComponentInChildren<Canvas>(true)!=null)continue;
                var filters=root.GetComponentsInChildren<MeshFilter>(true);
                if(filters.Length==0)continue;
                bool reviewed=false;
                foreach(var filter in filters)
                {
                    var renderer=filter.GetComponent<Renderer>();
                    if(filter.sharedMesh==null||renderer==null||!renderer.gameObject.activeInHierarchy)continue;
                    Bounds visual=renderer.bounds;
                    if(visual.size.y<.16f||Mathf.Max(visual.size.x,visual.size.z)<.16f)continue; // cabos rente ao chão e miudezas não viram barreiras
                    reviewed=true;
                    var original=filter.GetComponentsInParent<Collider>(true).FirstOrDefault(c=>c.enabled&&!c.isTrigger&&c.gameObject.name!="Colisao_Solida");
                    if(original!=null)
                    {
                        var redundant=filter.transform.Find("Colisao_Solida");
                        if(redundant!=null)foreach(var collider in redundant.GetComponents<Collider>())collider.enabled=false;
                        continue; // preservar colisão própria adequada do prefab, sem sobrepor uma segunda caixa
                    }
                    bool covered=root.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled&&!c.isTrigger&&
                        c.bounds.Contains(visual.min+Vector3.one*.004f)&&c.bounds.Contains(visual.max-Vector3.one*.004f));
                    if(covered)continue;
                    var existing=filter.transform.Find("Colisao_Solida");
                    bool fresh=existing==null;
                    var box=Add<BoxCollider>(Child(filter.transform,"Colisao_Solida").gameObject);
                    box.center=filter.sharedMesh.bounds.center;box.size=filter.sharedMesh.bounds.size;
                    box.enabled=true;box.isTrigger=false;if(fresh)created++;
                    Report.Add("EXTRA_COLLIDER|"+root.name+"/"+filter.name+"|"+visual);
                }
                if(reviewed)models++;
            }
            Report.Add("Modelos avulsos revistos: "+models+"; "+created+" colisores simples adicionais. Cabos baixos e miudezas não bloqueiam a passagem.");
        }

        /// <summary>root/bounds identificam o marco e sua geometria. Sem retorno. Cria laterais e verga finas e mantém o centro transitável.</summary>
        private static void ConfigureFrame(Transform root,Bounds bounds)
        {
            foreach(var collider in root.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            float thickness=Mathf.Min(.1f,bounds.size.x*.03f);
            for(int side=0;side<2;side++)
            {
                var box=Add<BoxCollider>(Child(root,side==0?"Colisao_Lateral_E":"Colisao_Lateral_D").gameObject);
                box.center=bounds.center+Vector3.right*(side==0?-1:1)*(bounds.extents.x-thickness*.5f);
                box.size=new Vector3(thickness,bounds.size.y,Mathf.Max(.12f,bounds.size.z));
                box.enabled=true;box.isTrigger=false;
            }
            var lintel=Add<BoxCollider>(Child(root,"Colisao_Verga").gameObject);
            lintel.center=bounds.center+Vector3.up*(bounds.extents.y-.05f);
            lintel.size=new Vector3(bounds.size.x,.1f,Mathf.Max(.12f,bounds.size.z));lintel.enabled=true;lintel.isTrigger=false;
        }

        /// <summary>root/bounds definem a mesa atual. Sem retorno. Conserva o collider referenciado como tampo e acrescenta quatro pés, sem bloco invisível sob a mesa.</summary>
        private static void ConfigureTable(Transform root,Bounds bounds)
        {
            foreach(var collider in root.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            var top=Add<BoxCollider>(root.gameObject);
            float thickness=Mathf.Min(.09f,bounds.size.y*.15f);
            top.center=new Vector3(bounds.center.x,bounds.max.y-thickness*.5f,bounds.center.z);
            top.size=new Vector3(bounds.size.x,thickness,bounds.size.z);top.enabled=true;top.isTrigger=false;
            float legWidth=Mathf.Min(.1f,bounds.size.x*.08f,bounds.size.z*.12f);
            float height=Mathf.Max(.05f,bounds.size.y-thickness);
            int index=0;
            foreach(float x in new[]{-1f,1f})foreach(float z in new[]{-1f,1f})
            {
                var leg=Add<BoxCollider>(Child(root,"Colisao_Pe_"+index++).gameObject);
                leg.center=new Vector3(bounds.center.x+x*(bounds.extents.x-legWidth*.7f),bounds.min.y+height*.5f,
                    bounds.center.z+z*(bounds.extents.z-legWidth*.7f));
                leg.size=new Vector3(legWidth,height,legWidth);leg.enabled=true;leg.isTrigger=false;
            }
        }
    }
}
