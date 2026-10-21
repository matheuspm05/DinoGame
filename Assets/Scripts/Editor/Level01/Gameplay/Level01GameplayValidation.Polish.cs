using System;
using System.Linq;
using ProjetoRetorno.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ProjetoRetorno.Level01.Editor
{
    public static partial class Level01GameplayValidation
    {
        private static float polishAudioTime, polishParticleTime;
        private static int polishIdleCount;
        private static Quaternion polishWing;

        /// <summary>Sem argumentos/retorno. Verifica espera opcional no chão e reset de adoção/efeitos/áudio sem usar progresso global.</summary>
        private static void ValidatePolishInitial()
        {
            var pet=Pet();var animation=pet.GetComponent<SparrowAnimationController>();
            Check(!pet.IsAdopted&&pet.adoptionTarget.enabled&&pet.adoptionTarget.isTrigger&&pet.transform.position.y<.25f,
                "Sparrow inicia esperando no chão, sem adoção ou acompanhamento");
            Check(pet.GetPrompt().Contains("Fazer amizade")&&animation!=null&&!animation.Flying,"Adoção é opcional e usa o sistema F existente");
            Check(pet.GetComponent<CompanionChirps>().SoundsPlayed==0,"Pássaro não canta antes da adoção");
            Check(!level.energyPanel.GetComponent<EnergyActivationVFX>().HasPlayed,"Efeito de energia resetado");
            foreach(var door in new[]{level.initialRoom.initialDoor,level.laboratoryDoor,level.hangarDoor,level.exitDoor})
                Check(door.GetComponent<DoorPresentation>().OpeningCount==0&&!door.GetComponent<DoorPresentation>().AlarmActive,"Apresentação resetada "+door.name);
            Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"Um único AudioListener em Play");
            var feet=level.player.GetComponent<PlayerFootstepAudio>();
            Check(feet.footsteps.Length==0&&pet.GetComponent<CompanionChirps>().clips.Length==0,
                "Sem passos/cantos fictícios: arrays vazios até importação dos clipes adequados");
        }

        /// <summary>Sem argumentos/retorno. Agenda testes do estado real de caminhada, corrida, salto e repouso, usando os dispositivos virtuais já existentes.</summary>
        private static void ScheduleFootstepChecks()
        {
            Steps.Add(()=>{Warp(new Vector3(0,.04f,-2.5f));AimPosition(new Vector3(0,1.65f,2));InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));});
            Wait(3);
            Steps.Add(()=>{var feet=level.player.GetComponent<PlayerFootstepAudio>();Check(feet.Grounded&&feet.CurrentSpeed>2&&Mathf.Abs(feet.CurrentInterval-feet.walkInterval)<.1f,"Cadência de passos usa deslocamento real ao caminhar");InputSystem.QueueStateEvent(keyboard,new KeyboardState());});
            Steps.Add(()=>{Warp(new Vector3(0,.04f,-2.5f));InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));});
            Wait(2);
            Steps.Add(()=>{var feet=level.player.GetComponent<PlayerFootstepAudio>();Check(feet.CurrentSpeed>4&&feet.CurrentInterval<feet.walkInterval,"Corrida aumenta a frequência dos passos");InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));});
            Steps.Add(()=>InputSystem.QueueStateEvent(keyboard,new KeyboardState()));
            Steps.Add(()=>Check(!level.player.GetComponent<PlayerFootstepAudio>().Grounded,"Salto suspende passos de caminhada no ar"));
            Wait(8);
            Steps.Add(()=>{var feet=level.player.GetComponent<PlayerFootstepAudio>();Check(feet.CurrentSpeed<.2f&&feet.StepsPlayed==0,"Parado não gera passos; nenhum áudio inadequado foi atribuído");});
        }

        /// <summary>Sem argumentos/retorno. Agenda repouso variado, F de adoção e confirmação de voo real depois da abertura inicial.</summary>
        private static void ScheduleAdoptionChecks()
        {
            Steps.Add(()=>{
                var pet=Pet();Check(!pet.IsAdopted&&pet.transform.position.y<.25f,"Pássaro continua esperando depois de abrir a porta");
                polishIdleCount=pet.GetComponent<SparrowAnimationController>().IdleChanges;
            });
            Wait(60);
            Steps.Add(()=>{
                Check(Pet().GetComponent<SparrowAnimationController>().IdleChanges>polishIdleCount,"Idle_A/B/C alternam periodicamente em Play");
                Warp(new Vector3(0,.04f,6.35f));AimPosition(Pet().adoptionTarget.bounds.center);
                Check(ReferenceEquals(interaction.FindTarget(),Pet()),"Raycast F encontra somente o pequeno alvo do Sparrow");
                CapturePolish("01_Sparrow_Esperando");
            });
            Press(Key.F);Wait(4);
            Steps.Add(()=>{
                var pet=Pet();Check(pet.IsAdopted&&!pet.adoptionTarget.enabled&&pet.GetPrompt()=="","F adota uma única vez e remove alvo do raycast");
                Check(pet.transform.position.y>1,"Sparrow decola suavemente após adoção, sem ficar preso no piso");ValidatePet();polishWing=pet.animator.transform.Find("Rig/root/body/wing.L").localRotation;
                Check(!pet.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled),"Após adoção não há colliders que interfiram nos puzzles/caixas");
                pet.Interact();Check(pet.IsAdopted,"Interação repetida não recria adoção");AimPosition(new Vector3(0,1.7f,9));
            });
            Wait(4);
            Steps.Add(()=>{Check(Quaternion.Angle(polishWing,Pet().animator.transform.Find("Rig/root/body/wing.L").localRotation)>.01f,"Fly movimenta as asas reais do Sparrow após a adoção");CapturePolish("02_Sparrow_Companheiro");});
        }

        /// <summary>Sem argumentos/retorno. Confirma disparo único do áudio/vapor e guarda tempos para testar a pausa.</summary>
        private static void CheckEnergyPresentation()
        {
            var energy=level.energyPanel.GetComponent<EnergyActivationVFX>();
            Check(energy.HasPlayed&&energy.effect.isPlaying&&energy.effect.particleCount>0,"Bateria instalada dispara partículas ciano reais");
            var presentation=level.laboratoryDoor.GetComponent<DoorPresentation>();
            Check(presentation.OpeningCount==1&&presentation.movementSource.isPlaying&&presentation.steam.All(p=>p.particleCount>0),
                "Abertura do laboratório dispara som 3D e duas rajadas de vapor");
            level.laboratoryDoor.SetOpen(true);
            Check(presentation.OpeningCount==1,"Solicitação repetida não redispara o som da porta");
            Check(energy.source.isPlaying,"Som curto de ativação reproduzido");
            CapturePolish("03_Energia_Vapor");CaptureDoorSteam();
            polishAudioTime=energy.source.time;polishParticleTime=energy.effect.time;
        }

        /// <summary>Sem argumentos/retorno. Confere congelamento das partículas e fontes locais com timeScale zero, preservando menus ativos.</summary>
        private static void CheckPresentationPaused()
        {
            var energy=level.energyPanel.GetComponent<EnergyActivationVFX>();
            Check(Time.timeScale==0&&Mathf.Abs(energy.effect.time-polishParticleTime)<.04f,"Partículas de energia congelam com ESC");
            var ambient=level.transform.Find("Audio_Ambiente_Energia").GetComponent<AudioSource>();
            Check(ambient.GetComponent<PauseAwareAudio>().IsSuspended&&!ambient.isPlaying&&Mathf.Abs(ambient.time-polishAudioTime)<.08f,
                "Áudio local pausa sem bloquear o EventSystem");
            Check(Pet().GetComponent<CompanionChirps>().SoundsPlayed==0,"Nenhum canto artificial foi inserido");
        }

        /// <summary>Sem argumentos/retorno. Verifica emissão finita e permite revisão posterior com novos clipes adequados no Inspector.</summary>
        private static void CheckPresentationStopped()
        {
            var energy=level.energyPanel.GetComponent<EnergyActivationVFX>();
            Check(!energy.effect.IsAlive(true),"Partículas azuis encerram automaticamente");
            Check(level.laboratoryDoor.GetComponent<DoorPresentation>().steam.All(p=>!p.IsAlive(true)),"Vapor encerra automaticamente sem colisão");
        }

        /// <summary>Sem argumentos/retorno. Confere alerta finito e não repetitivo disparado pela terceira ativação verdadeira.</summary>
        private static void CheckFinalAlarm()
        {
            var presentation=level.exitDoor.GetComponent<DoorPresentation>();
            Check(presentation.OpeningCount==1&&presentation.AlarmActive&&presentation.alarmSource.isPlaying,"3/3 inicia portão e alerta eletrônico");
            level.exitDoor.SetOpen(true);
            Check(presentation.OpeningCount==1,"Lógica repetida do portão não duplica alarme");
            CapturePolish("04_Portao_Alerta");
        }


        /// <summary>Sem argumentos/retorno. Renderiza uma evidência do vapor pela câmera existente e restaura imediatamente sua pose, sem criar câmeras ou listeners.</summary>
        private static void CaptureDoorSteam()
        {
            var camera=level.player.view.GetComponent<Camera>();
            Vector3 position=camera.transform.position;Quaternion rotation=camera.transform.rotation;
            var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
            var texture=new RenderTexture(960,540,24);var image=new Texture2D(960,540,TextureFormat.RGB24,false);
            try
            {
                var door=level.laboratoryDoor;
                camera.transform.position=door.transform.position-door.transform.forward*2.6f+Vector3.up*1.65f;
                camera.transform.LookAt(door.transform.position+Vector3.up*.8f);
                camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;
                image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();
                System.IO.Directory.CreateDirectory("Docs/Previas/Polimento_Nivel01");
                System.IO.File.WriteAllBytes("Docs/Previas/Polimento_Nivel01/05_Vapor_Porta.png",image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=previousTarget;RenderTexture.active=previousActive;
                camera.transform.SetPositionAndRotation(position,rotation);
                texture.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(image);
            }
        }

        /// <summary>name identifica uma imagem de evidência. Sem retorno. Captura o Game View real sem alterar materiais ou modelos.</summary>
        private static void CapturePolish(string name)
        {System.IO.Directory.CreateDirectory("Docs/Previas/Polimento_Nivel01");ScreenCapture.CaptureScreenshot("Docs/Previas/Polimento_Nivel01/"+name+".png");}
    }
}
