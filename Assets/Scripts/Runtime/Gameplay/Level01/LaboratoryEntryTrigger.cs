using ProjetoRetorno.Level01;
using UnityEngine;

namespace ProjetoRetorno.Gameplay
{
    /// <summary>Visita ao laboratório atualiza o objetivo sem acionar coleta ou liberar portas.</summary>
    public sealed class LaboratoryEntryTrigger : MonoBehaviour
    {
        public Level01GameManager level;
        /// <summary>other é quem cruzou a entrada. Sem retorno. Aceita somente o jogador existente e solicita atualização ao gerente.</summary>
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<NaveWalkthroughController>() == level.player) level.EnterLaboratory();
        }
    }
}
