using UnityEngine;

namespace ProjetoRetorno.Gameplay
{
    /// <summary>Reutiliza estados reais do AC_Sparrow: Idle_A/B/C no chão e Fly durante o acompanhamento.</summary>
    public sealed class SparrowAnimationController : MonoBehaviour
    {
        public PetFollower pet;
        public Animator animator;
        public string[] restingStates = { "Base Layer.Idle_A", "Base Layer.Idle_B", "Base Layer.Idle_C" };
        public string flightState = "Base Layer.Fly";
        public Vector2 idleInterval = new Vector2(3, 7);
        [Min(0)] public float transitionTime = .18f;
        public int IdleChanges { get; private set; }
        public bool Flying { get; private set; }
        private float nextIdle;
        private int previous = -1;

        /// <summary>Sem argumentos/retorno. Mantém o controller original e inicia uma pose real de repouso, sem root motion.</summary>
        private void Start()
        {
            if (animator == null || pet == null) return;
            animator.applyRootMotion = false; animator.updateMode = AnimatorUpdateMode.Normal;
            ChooseIdle(true);
        }

        /// <summary>Sem argumentos/retorno. Varia idles periodicamente apenas no chão e preserva Fly sem interrupções durante o voo.</summary>
        private void Update()
        {
            if (pet == null || animator == null || Time.timeScale <= 0 || (pet.level != null && pet.level.IsComplete)) return;
            if (pet.IsAdopted)
            {
                if (!Flying) { Flying = true; PlayExisting(flightState, false); }
                return; // O pacote tem um único voo; poses terrestres não são usadas no ar.
            }
            nextIdle -= Time.deltaTime;
            if (nextIdle <= 0) ChooseIdle(false);
        }

        /// <summary>immediate inicia sem transição quando solicitado; sem retorno. Sorteia um idle diferente e agenda a próxima troca.</summary>
        private void ChooseIdle(bool immediate)
        {
            if (restingStates == null || restingStates.Length == 0) return;
            int index = Random.Range(0, restingStates.Length);
            if (restingStates.Length > 1 && index == previous) index = (index + Random.Range(1, restingStates.Length)) % restingStates.Length;
            PlayExisting(restingStates[index], immediate); previous = index; IdleChanges++;
            nextIdle = Random.Range(Mathf.Max(.5f, idleInterval.x), Mathf.Max(idleInterval.x, idleInterval.y));
        }

        /// <summary>state é o caminho real; immediate evita transição inicial. Sem retorno. Só executa estados existentes no controller atribuído.</summary>
        private void PlayExisting(string state, bool immediate)
        {
            int hash = Animator.StringToHash(state);
            if (!animator.HasState(0, hash)) { Debug.LogWarning("Sparrow: estado indisponível: " + state, this); return; }
            if (immediate) animator.Play(hash, 0, 0);
            else animator.CrossFadeInFixedTime(hash, transitionTime, 0);
        }
    }
}
