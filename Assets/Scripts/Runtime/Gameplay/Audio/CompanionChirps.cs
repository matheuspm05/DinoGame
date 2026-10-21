using UnityEngine;

namespace ProjetoRetorno.Gameplay
{
    /// <summary>Cantos opcionais, curtos e espaçados, disponíveis somente após a adoção.</summary>
    public sealed class CompanionChirps : MonoBehaviour
    {
        public PetFollower pet;
        public AudioSource source;
        [Tooltip("Cantos reais de pássaro. Não substituir por efeitos eletrônicos.")]
        public AudioClip[] clips = new AudioClip[0];
        public Vector2 interval = new Vector2(8, 20);
        [Range(0,1)] public float volume = .18f;
        public int SoundsPlayed { get; private set; }
        private float remaining;
        private int previous = -1;
        private bool started;

        /// <summary>Sem argumentos/retorno. Após adoção agenda o primeiro canto; respeita pausa, conclusão, intervalos e ausência de clipes.</summary>
        private void Update()
        {
            if (pet == null || !pet.IsAdopted || Time.timeScale <= 0 || (pet.level != null && pet.level.IsComplete)) return;
            if (!started) { started = true; Schedule(); return; }
            remaining -= Time.deltaTime;
            if (remaining > 0) return;
            if (clips != null && clips.Length > 0 && source != null)
            {
                int index = Random.Range(0, clips.Length);
                if (clips.Length > 1 && index == previous) index = (index + Random.Range(1, clips.Length)) % clips.Length;
                if (clips[index] != null) { source.pitch = Random.Range(.97f, 1.03f); source.PlayOneShot(clips[index], volume); previous = index; SoundsPlayed++; }
            }
            Schedule();
        }

        /// <summary>Sem argumentos/retorno. Sorteia o intervalo do próximo canto usando tempo escalado para permitir pausa e reset pela cena.</summary>
        private void Schedule() { remaining = Random.Range(Mathf.Max(1, interval.x), Mathf.Max(interval.x, interval.y)); }
    }
}
