using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjetoRetorno.Gameplay
{
    /// <summary>Uma única interação por câmera; o primeiro obstáculo sempre interrompe a busca.</summary>
    public sealed class PlayerInteraction : MonoBehaviour
    {
        public Transform view;
        public Level01GameManager level;
        public float maximumDistance = 3f;

        /// <summary>Sem argumentos/retorno. Atualiza a indicação e lê F pelo novo Input System somente durante gameplay.</summary>
        private void Update()
        {
            if (level == null || level.hud == null) return;
            IInteractable target = FindTarget();
            level.hud.SetPrompt(target == null ? "" : target.GetPrompt());
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) TryInteract();
        }

        /// <summary>Sem argumentos. Retorna o primeiro interativo visível a até três metros, ou null se houver uma obstrução/pausa.</summary>
        public IInteractable FindTarget()
        {
            if (view == null || level == null || !level.CanInteract) return null;
            RaycastHit[] hits = Physics.RaycastAll(view.position, view.forward, maximumDistance, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.IsChildOf(transform)) continue;
                var candidate = hit.collider.GetComponentInParent<IInteractable>();
                if (candidate != null) return candidate;
                // Triggers de conclusão/volumes não escondem objetos; geometria sólida interrompe o raio.
                if (!hit.collider.isTrigger) return null;
            }
            return null;
        }

        /// <summary>Sem argumentos. Retorna true quando encontrou um alvo e executa a mesma interação utilizada pela tecla F.</summary>
        public bool TryInteract()
        {
            IInteractable target = FindTarget();
            if (target == null) return false;
            target.Interact();
            return true;
        }
    }
}
