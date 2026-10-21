using System;
using System.IO;
using System.Linq;
using ProjetoRetorno.Gameplay;
using UnityEditor;
using UnityEngine;

namespace ProjetoRetorno.Level01.Editor
{
    public static partial class Level01PolishSetup
    {
        /// <summary>Sem argumentos/retorno. Organiza bibliotecas e seis clipes utilizados por MoveAsset, verificando a preservação dos GUIDs.</summary>
        private static void OrganizeAudio()
        {
            foreach(string folder in new[]{"Assets/Audio/SFX/Footsteps","Assets/Audio/SFX/Doors","Assets/Audio/SFX/Interactions",
                "Assets/Audio/SFX/Alarms","Assets/Audio/SFX/Bird","Assets/Audio/Ambient","Assets/Audio/Music","Assets/Audio/Libraries"})
                Folder(folder);
            MovePreservingGUID("Assets/kenney_sci-fi-sounds","Assets/Audio/Libraries/Kenney_SciFi");
            MovePreservingGUID("Assets/kenney_interface-sounds","Assets/Audio/Libraries/Kenney_Interface");
            foreach(string clip in new[]{"doorOpen_000","doorOpen_001","doorOpen_002"})
                MoveClip(clip,"Assets/Audio/SFX/Doors");
            MoveClip("confirmation_001","Assets/Audio/SFX/Interactions");
            MoveClip("error_006","Assets/Audio/SFX/Alarms");
            MoveClip("engineCircular_000","Assets/Audio/Ambient");
        }

        /// <summary>source/destination são caminhos reais. Sem retorno. Move o asset com sua meta e confirma que o identificador não mudou.</summary>
        private static void MovePreservingGUID(string source,string destination)
        {
            string guid=AssetDatabase.AssetPathToGUID(source);
            if(string.IsNullOrEmpty(guid))return;
            if(!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(destination)))throw new InvalidOperationException("Destino já ocupado: "+destination);
            string error=AssetDatabase.MoveAsset(source,destination);
            if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
            if(AssetDatabase.AssetPathToGUID(destination)!=guid)throw new InvalidOperationException("GUID alterado em "+destination);
            Report.Add("MOVE|"+source+"|"+destination+"|GUID="+guid);
        }

        /// <summary>name/folder identificam um clipe existente e sua categoria. Sem retorno. Move somente o áudio selecionado, sem duplicação.</summary>
        private static void MoveClip(string name,string folder)
        {
            string target=folder+"/Kenney_"+name+".ogg";
            if(AssetDatabase.LoadAssetAtPath<AudioClip>(target)!=null)return;
            string source=AssetDatabase.FindAssets(name+" t:AudioClip").Select(AssetDatabase.GUIDToAssetPath)
                .SingleOrDefault(path=>Path.GetFileNameWithoutExtension(path)==name);
            if(source==null)throw new FileNotFoundException("Clipe Kenney não encontrado: "+name);
            MovePreservingGUID(source,target);
        }

        /// <summary>path identifica um áudio real. Retorna AudioClip ou falha explicitamente, sem referências fictícias.</summary>
        private static AudioClip Clip(string path)
        {var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(clip==null)throw new FileNotFoundException(path);return clip;}

        /// <summary>parent/name/distance/volume configuram a fonte local. Retorna AudioSource 3D sem play automático e com pausa individual.</summary>
        private static AudioSource SoundSource(Transform parent,string name,float minimum,float maximum,float volume)
        {
            Transform root=Child(parent,name);
            var source=Add<AudioSource>(root.gameObject);
            source.playOnAwake=false;source.spatialBlend=1;source.volume=volume;
            source.minDistance=minimum;source.maxDistance=maximum;source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=0;
            Add<PauseAwareAudio>(root.gameObject).source=source;return source;
        }

        /// <summary>level fornece jogador, painel e quatro portas. Sem retorno. Conecta apresentação aos componentes existentes e mantém campos sem clipes adequados vazios.</summary>
        private static void ConfigureAudioAndEffects(Level01GameManager level)
        {
            var feet=Add<PlayerFootstepAudio>(level.player.gameObject);
            feet.player=level.player;
            feet.source=SoundSource(level.player.transform,"Audio_Passos",.5f,5,1);
            // O componente fica junto ao CharacterController, mas a fonte não precisa ficar no mesmo GameObject.
            var doors=new[]{level.initialRoom.initialDoor,level.laboratoryDoor,level.hangarDoor,level.exitDoor};
            for(int i=0;i<doors.Length;i++)
            {
                var door=doors[i];var presentation=Add<DoorPresentation>(door.gameObject);presentation.door=door;
                presentation.movementSource=SoundSource(door.transform,"Audio_Abertura",2,18,.42f);
                presentation.movementSource.transform.position=door.transform.position+Vector3.up*1.5f;
                presentation.openingClip=Clip("Assets/Audio/SFX/Doors/Kenney_doorOpen_00"+(i%3)+".ogg");
                var volume=door.solidCollider.bounds;
                presentation.steam=new ParticleSystem[2];
                for(int side=0;side<2;side++)
                {
                    string name=side==0?"Vapor_Esquerda":"Vapor_Direita";
                    var steam=Effect(door.transform,name,"VFX_DoorSteam");
                    // Folha pode ser movida por Translate; os emissores permanecem nas laterais fixas do vão.
                    steam.transform.position=volume.center+door.transform.right*((side==0?-1:1)*(door.solidCollider.size.x*.5f-.12f))+
                        Vector3.down*(volume.extents.y-.25f)-door.transform.forward*.5f;
                    steam.transform.rotation=Quaternion.Euler(-90,0,0);presentation.steam[side]=steam;
                }
                if(i==3)
                {
                    presentation.alarmSource=SoundSource(door.transform,"Audio_Alerta",4,28,.24f);
                    presentation.alarmSource.transform.position=door.transform.position+Vector3.up*2.5f;
                    presentation.alarmClip=Clip("Assets/Audio/SFX/Alarms/Kenney_error_006.ogg");
                    presentation.alarmDuration=4;
                    Report.Add("Alerta externo: error_006 (aviso eletrônico de interface), repetido por 4s; não é uma fala/anúncio importado.");
                }
            }
            var energy=Add<EnergyActivationVFX>(level.energyPanel.gameObject);energy.level=level;
            energy.effect=Effect(level.energyPanel.transform,"Energia_Ativacao","VFX_EnergyActivation");
            var console=level.energyPanel.transform.Find("Console_SciFi").GetComponent<BoxCollider>().bounds;
            energy.effect.transform.position=console.center+Vector3.up*.42f+Vector3.back*(console.extents.z+.15f);
            energy.effect.transform.rotation=Quaternion.Euler(-90,0,0);
            energy.source=SoundSource(energy.effect.transform,"Audio_Ativacao",1,8,.28f);
            energy.activationClip=Clip("Assets/Audio/SFX/Interactions/Kenney_confirmation_001.ogg");
            var ambient=SoundSource(level.transform,"Audio_Ambiente_Energia",1,9,.035f);
            ambient.transform.position=console.center;ambient.clip=Clip("Assets/Audio/Ambient/Kenney_engineCircular_000.ogg");
            ambient.loop=true;ambient.playOnAwake=true;
            foreach(var source in Components<AudioSource>(level.gameObject.scene))
                if(source.clip!=null)Report.Add("SOURCE|"+source.name+"|"+AssetDatabase.GetAssetPath(source.clip)+"|volume="+source.volume);
        }
    }
}
