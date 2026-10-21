using UnityEngine;

namespace ProjetoRetorno.Gameplay
{
    /// <summary>Somente a primeira caixa do chão inicia a geração de projéteis no HoldPoint.</summary>
    public sealed class ThrowablePickup : MonoBehaviour, IInteractable
    {
        public IntroThrowingSystem owner;
        /// <summary>Sem argumentos. Retorna a indicação F somente enquanto o desafio inicial está disponível.</summary>
        public string GetPrompt() { return owner != null && !owner.IsSolved ? "[F] Pegar caixa" : ""; }
        /// <summary>Sem argumentos/retorno. Reutiliza a interação F e solicita a primeira caixa ao sistema de arremesso.</summary>
        public void Interact() { if (owner != null) owner.BeginThrowing(); }
    }
}
