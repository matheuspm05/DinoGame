using UnityEngine;

namespace ProjetoRetorno.Gameplay
{
    /// <summary>Som e vapor ligados ao evento da porta existente. O portão externo também pode emitir um alerta temporário.</summary>
    public sealed class DoorPresentation : MonoBehaviour
    {
        public DoorController door;
        public AudioSource movementSource, alarmSource;
        public AudioClip openingClip, alarmClip;
        public ParticleSystem[] steam = new ParticleSystem[0];
        [Min(.1f)] public float alarmDuration = 4f;
        public int OpeningCount { get; private set; }
        public bool AlarmActive { get; private set; }
        private float alarmRemaining;
        private bool alarmTriggered;

        /// <summary>Sem argumentos/retorno. Assina a abertura já controlada pela progressão, sem criar outra lógica de porta.</summary>
        private void OnEnable() { if (door != null) door.OpeningStarted += OnOpening; }

        /// <summary>Sem argumentos/retorno. Remove a assinatura e encerra o alarme ao descarregar/desativar a cena.</summary>
        private void OnDisable()
        {
            if (door != null) door.OpeningStarted -= OnOpening;
            if (alarmSource != null) alarmSource.Stop();
            AlarmActive = false;
        }

        /// <summary>opened identifica a porta que começou a mover. Sem retorno. Dispara som/vapor uma vez por abertura e o alerta uma vez por partida.</summary>
        private void OnOpening(DoorController opened)
        {
            if (opened != door || Time.timeScale <= 0) return;
            OpeningCount++;
            if (movementSource != null && openingClip != null) { movementSource.clip = openingClip; movementSource.Play(); }
            foreach (var effect in steam) if (effect != null) effect.Play(true);
            if (!alarmTriggered && alarmSource != null && alarmClip != null)
            {
                alarmTriggered = true; AlarmActive = true; alarmRemaining = alarmDuration;
                alarmSource.clip = alarmClip; alarmSource.loop = true; alarmSource.Play();
            }
        }

        /// <summary>Sem argumentos/retorno. Conta segundos de jogo e para o alerta automaticamente; a pausa não consome sua duração.</summary>
        private void Update()
        {
            if (!AlarmActive || Time.timeScale <= 0) return;
            alarmRemaining -= Time.deltaTime;
            if (alarmRemaining > 0) return;
            alarmSource.Stop(); AlarmActive = false;
        }
    }
}
