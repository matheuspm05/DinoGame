namespace ProjetoRetorno.Gameplay
{
    /// <summary>Contrato comum para objetos alcançados pelo raycast da câmera.</summary>
    public interface IInteractable
    {
        /// <summary>Sem argumentos. Retorna a mensagem contextual calculada a partir do estado atual do objeto.</summary>
        string GetPrompt();
        /// <summary>Sem argumentos/retorno. Solicita a interação ao responsável pela progressão, que verifica seus requisitos.</summary>
        void Interact();
    }
}
