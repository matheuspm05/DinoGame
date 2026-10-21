using UnityEngine;

namespace ProjetoRetorno.Gameplay
{
    /// <summary>Pausa apenas a fonte local, sem alterar AudioListener global ou o áudio dos menus.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class PauseAwareAudio : MonoBehaviour
    {
        public AudioSource source;
        public bool IsSuspended { get; private set; }
        private bool resumePlayback;

        /// <summary>Sem argumentos/retorno. Obtém a fonte já configurada e mantém a configuração editável no Inspector.</summary>
        private void Awake() { if (source == null) source = GetComponent<AudioSource>(); }

        /// <summary>Sem argumentos/retorno. Guarda se havia áudio ativo ao pausar e retoma somente essa reprodução ao continuar.</summary>
        private void LateUpdate()
        {
            bool suspended = Time.timeScale <= 0;
            if (suspended == IsSuspended || source == null) return;
            IsSuspended = suspended;
            if (suspended) { resumePlayback = source.isPlaying; if (resumePlayback) source.Pause(); }
            else { if (resumePlayback) source.UnPause(); resumePlayback = false; }
        }

        /// <summary>Sem argumentos/retorno. Limpa apenas a suspensão local quando a cena/fonte é desativada, sem estados persistentes.</summary>
        private void OnDisable() { if (source != null) source.Stop(); IsSuspended = resumePlayback = false; }
    }
}
