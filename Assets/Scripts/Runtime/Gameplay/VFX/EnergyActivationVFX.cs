using UnityEngine;

namespace ProjetoRetorno.Gameplay
{
    /// <summary>Apresenta o restabelecimento da energia sem manter outra cópia dos estados dos puzzles.</summary>
    public sealed class EnergyActivationVFX : MonoBehaviour
    {
        public Level01GameManager level;
        public ParticleSystem effect;
        public AudioSource source;
        public AudioClip activationClip;
        public bool HasPlayed { get; private set; }

        /// <summary>Sem argumentos/retorno. Observa a instalação real uma única vez e reproduz partículas/áudio somente após o sucesso.</summary>
        private void Update()
        {
            if (HasPlayed || level == null || !level.EnergyRestored || Time.timeScale <= 0) return;
            HasPlayed = true;
            if (effect != null) effect.Play(true);
            if (source != null && activationClip != null) { source.clip = activationClip; source.Play(); }
        }
    }
}
