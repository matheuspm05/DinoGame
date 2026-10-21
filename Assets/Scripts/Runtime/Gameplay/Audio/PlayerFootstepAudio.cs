using UnityEngine;
using ProjetoRetorno.Level01;

namespace ProjetoRetorno.Gameplay
{
    /// <summary>Passos por distância realmente percorrida e contato com o chão; não altera a movimentação existente.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerFootstepAudio : MonoBehaviour
    {
        public NaveWalkthroughController player;
        public AudioSource source;
        [Tooltip("Somente passos reais. Vazio até que clipes adequados sejam importados.")]
        public AudioClip[] footsteps = new AudioClip[0];
        public AudioClip landing;
        [Min(.1f)] public float walkInterval = .48f, runInterval = .31f;
        public Vector2 pitchRange = new Vector2(.96f, 1.04f);
        [Range(0,1)] public float volume = .32f;
        public float CurrentSpeed { get; private set; }
        public float CurrentInterval { get; private set; }
        public bool Grounded { get; private set; }
        public int StepsPlayed { get; private set; }
        private CharacterController body;
        private Vector3 previous;
        private float clock;
        private int previousClip = -1;
        private bool wasGrounded, airborne;

        /// <summary>Sem argumentos/retorno. Guarda o controlador e a posição para medir deslocamentos após o movimento normal.</summary>
        private void Awake()
        {
            body = GetComponent<CharacterController>();
            if (player == null) player = GetComponent<NaveWalkthroughController>();
            if (source == null) source = GetComponent<AudioSource>();
            previous = transform.position; CurrentInterval = walkInterval;
        }

        /// <summary>Sem argumentos/retorno. Acumula ritmo conforme velocidade horizontal real, sem passos parado, pausado ou durante saltos.</summary>
        private void LateUpdate()
        {
            Vector3 delta = transform.position - previous; previous = transform.position;
            if (Time.deltaTime <= 0 || player == null || !player.enabled)
            { CurrentSpeed = 0; clock = 0; return; }
            Grounded = body.isGrounded;
            CurrentSpeed = Vector3.ProjectOnPlane(delta, Vector3.up).magnitude / Time.deltaTime;
            if (delta.magnitude > 1) { clock = 0; wasGrounded = Grounded; return; } // mudança de posição de teste/respawn
            if (!Grounded) airborne = true;
            if (Grounded && !wasGrounded && airborne)
            {
                airborne = false; clock = 0;
                if (landing != null) source.PlayOneShot(landing, volume * .6f);
            }
            wasGrounded = Grounded;
            if (!Grounded || CurrentSpeed < .12f) { clock = 0; return; }
            float running = Mathf.InverseLerp(player.walkSpeed, player.runSpeed, CurrentSpeed);
            CurrentInterval = Mathf.Lerp(walkInterval, runInterval, running) *
                Mathf.Clamp(player.walkSpeed / Mathf.Max(CurrentSpeed, .2f), 1f, 2f);
            clock += Time.deltaTime;
            if (clock >= Mathf.Max(.1f, CurrentInterval)) { clock = 0; PlayStep(); }
        }

        /// <summary>Sem argumentos/retorno. Escolhe um passo válido sem repetição imediata quando possível e varia levemente o pitch.</summary>
        public void PlayStep()
        {
            if (footsteps == null || footsteps.Length == 0 || source == null || Time.timeScale <= 0 || !Grounded) return;
            int index = Random.Range(0, footsteps.Length);
            if (footsteps.Length > 1 && index == previousClip) index = (index + Random.Range(1, footsteps.Length)) % footsteps.Length;
            if (footsteps[index] == null) return;
            previousClip = index; source.pitch = Random.Range(pitchRange.x, pitchRange.y);
            source.PlayOneShot(footsteps[index], volume); StepsPlayed++;
        }
    }
}
