using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjetoRetorno.Gameplay
{
    /// <summary>HUD nativo editável na Hierarchy; a pausa e a conclusão ocultam os textos de gameplay.</summary>
    public sealed class ObjectiveHUD : MonoBehaviour
    {
        public Level01GameManager level;
        public GameObject gameplayPage, completionPage;
        public Text objectiveText, promptText, messageText, progressText;
        public Button menuButton;
        private float messageRemaining;

        /// <summary>value é o objetivo já calculado pelo gerente; sem retorno. Atualiza o texto no canto superior esquerdo.</summary>
        public void SetObjective(string value) { objectiveText.text = "OBJETIVO\n" + value; if (progressText != null) { progressText.gameObject.SetActive(level != null && level.HangarUnlocked); progressText.text = "SISTEMAS DO HANGAR  " + (level != null ? level.ActiveTerminals : 0) + "/3"; } }

        /// <summary>value é a indicação do alvo; sem retorno. Atualiza o texto inferior sem alterar o estado do jogo.</summary>
        public void SetPrompt(string value) { promptText.text = value; }

        /// <summary>value contém a confirmação/impedimento e seconds sua duração; sem retorno. Reinicia a mensagem temporária.</summary>
        public void ShowMessage(string value, float seconds = 3.5f) { messageText.text = value; messageRemaining = seconds; }

        /// <summary>visible seleciona a página final; sem retorno. Alterna o HUD e seleciona o botão de retorno ao menu.</summary>
        public void ShowCompletion(bool visible)
        {
            completionPage.SetActive(visible); gameplayPage.SetActive(!visible);
            if (visible && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(menuButton.gameObject);
        }

        /// <summary>Sem argumentos/retorno. Oculta o HUD sobre a pausa e conta a duração das mensagens somente enquanto o tempo avança.</summary>
        private void LateUpdate()
        {
            if (level == null) return;
            gameplayPage.SetActive(!level.IsComplete && (level.pause == null || !level.pause.IsPaused));
            if (messageRemaining <= 0) return;
            messageRemaining -= Time.deltaTime;
            if (messageRemaining <= 0) messageText.text = "";
        }
    }
}
