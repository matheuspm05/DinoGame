using ProjetoRetorno.Level01;
using UnityEngine;

namespace ProjetoRetorno.Gameplay
{
    /// <summary>Detecta apenas o jogador após a porta externa; o gerente impede conclusão antecipada.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class LevelCompleteTrigger : MonoBehaviour
    {
        public Level01GameManager level;
        /// <summary>other é o collider que entrou. Sem retorno. Confirma a identidade do jogador e solicita a conclusão condicionada.</summary>
        private void OnTriggerEnter(Collider other)
        {
            if (level != null && other.GetComponentInParent<NaveWalkthroughController>() == level.player) level.TryComplete();
        }
    }
}
