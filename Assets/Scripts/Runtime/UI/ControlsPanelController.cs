using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjetoRetorno.UI
{
    /// <summary>Alterna o painel de comandos e sua página de origem, usando objetos editáveis do Canvas.</summary>
    public sealed class ControlsPanelController : MonoBehaviour
    {
        public GameObject menuPage;
        public Button backButton;
        public Button controlsButton;

        /// <summary>Sem argumentos/retorno. Oculta a página do menu, exibe os controles e seleciona Voltar.</summary>
        public void Show()
        {
            if (menuPage != null) menuPage.SetActive(false);
            gameObject.SetActive(true);
            if (EventSystem.current != null && backButton != null)
                EventSystem.current.SetSelectedGameObject(backButton.gameObject);
        }

        /// <summary>Sem argumentos/retorno. Fecha os controles e devolve a seleção ao botão da página anterior.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            if (menuPage != null) menuPage.SetActive(true);
            if (EventSystem.current != null && controlsButton != null && controlsButton.gameObject.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(controlsButton.gameObject);
        }
    }
}
