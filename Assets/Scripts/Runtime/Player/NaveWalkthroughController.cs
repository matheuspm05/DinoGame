using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjetoRetorno.Level01
{
    /// <summary>Controle provisório para visitar e validar a arquitetura; não contém gameplay de missão.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class NaveWalkthroughController : MonoBehaviour
    {
        public Transform view;
        public float walkSpeed = 3.5f;
        public float runSpeed = 5.5f;
        public float sensitivity = 0.09f;
        [Header("Agachamento (segurar Ctrl)")]
        [Min(0.6f)] public float crouchHeight = 1.1f;
        [Min(0f)] public float crouchSpeed = 1.8f;
        [Min(0.1f)] public float cameraHeightSpeed = 3f;
        public bool IsCrouching { get; private set; }
        private CharacterController controller;
        private float verticalSpeed;
        private float pitch;
        private float standingHeight;
        private Vector3 standingCenter;
        private Vector3 standingViewPosition;
        private readonly Collider[] standingObstacles = new Collider[32];

        /// <summary>Sem argumentos/retorno. Guarda as dimensões originais uma vez, preservando-as ao pausar e reativar.</summary>
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            standingHeight = controller.height;
            standingCenter = controller.center;
            if (view != null) standingViewPosition = view.localPosition;
        }

        /// <summary>Sem argumentos/retorno. Obtém a cápsula e captura o cursor para a visita.</summary>
        private void OnEnable()
        {
            controller = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>Sem argumentos/retorno. Ignora entradas durante pausa; lê teclado/mouse e move/gira com gravidade e colisão.</summary>
        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null || view == null) return;
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (Cursor.lockState != CursorLockMode.Locked) return;
            UpdateCrouch(keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed);
            Vector2 delta = mouse.delta.ReadValue() * sensitivity;
            transform.Rotate(0f, delta.x, 0f);
            pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);
            view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            float x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            float z = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            Vector3 direction = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f);
            if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
            if (!IsCrouching && controller.isGrounded && keyboard.spaceKey.wasPressedThisFrame) verticalSpeed = 4.5f;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            float speed = IsCrouching ? crouchSpeed : (keyboard.leftShiftKey.isPressed ? runSpeed : walkSpeed);
            controller.Move((direction * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
        }

        /// <summary>held indica Ctrl pressionado; sem retorno. Ajusta cápsula mantendo os pés e suaviza a câmera; só levanta com espaço livre.</summary>
        private void UpdateCrouch(bool held)
        {
            bool crouching = held || (IsCrouching && !CanStand());
            if (crouching != IsCrouching)
            {
                IsCrouching = crouching;
                float height = crouching ? Mathf.Clamp(crouchHeight, controller.radius * 2f, standingHeight) : standingHeight;
                controller.height = height;
                controller.center = standingCenter + Vector3.up * ((height - standingHeight) * .5f);
            }
            Vector3 target = standingViewPosition + Vector3.up * (controller.height - standingHeight);
            view.localPosition = Vector3.MoveTowards(view.localPosition, target, cameraHeightSpeed * Time.deltaTime);
        }

        /// <summary>Sem argumentos; retorna se a cápsula em pé cabe. Consulta colisores sólidos, ignora o próprio jogador e respeita camadas.</summary>
        private bool CanStand()
        {
            // Margem interna evita interpretar o piso e paredes tangentes como um teto bloqueando a subida.
            float radius = Mathf.Max(.01f, controller.radius - .02f);
            float halfSegment = Mathf.Max(0f, standingHeight * .5f - controller.radius);
            Vector3 center = transform.TransformPoint(standingCenter);
            Vector3 offset = transform.up * halfSegment;
            int count = Physics.OverlapCapsuleNonAlloc(center - offset, center + offset, radius,
                standingObstacles, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            if (count == standingObstacles.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Collider obstacle = standingObstacles[i];
                if (obstacle == controller || obstacle.transform.IsChildOf(transform)) continue;
                if (Physics.GetIgnoreLayerCollision(gameObject.layer, obstacle.gameObject.layer)
                    || Physics.GetIgnoreCollision(controller, obstacle)) continue;
                return false;
            }
            return true;
        }

        /// <summary>Sem argumentos/retorno. Libera o cursor ao desativar o controle ou encerrar Play.</summary>
        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
