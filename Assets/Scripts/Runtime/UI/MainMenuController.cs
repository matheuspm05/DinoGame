using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjetoRetorno.UI
{
    /// <summary>Recebe os eventos OnClick persistentes do Menu Principal.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        public ControlsPanelController controlsPanel;
        public Button newGameButton;

        /// <summary>Sem argumentos/retorno. Inicializa o menu com tempo normal, cursor visível e controles fechados.</summary>
        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (controlsPanel != null) controlsPanel.Hide();
        }

        /// <summary>Sem argumentos/retorno. Seleciona Novo Jogo após o EventSystem estar ativo.</summary>
        private void Start()
        {
            if (EventSystem.current != null && newGameButton != null)
                EventSystem.current.SetSelectedGameObject(newGameButton.gameObject);
        }

        /// <summary>Sem argumentos/retorno. ESC fecha o painel de controles quando ele está aberto no Menu Principal.</summary>
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && controlsPanel != null && controlsPanel.gameObject.activeSelf)
                controlsPanel.Hide();
        }

        /// <summary>Sem argumentos/retorno. Novo Jogo carrega o nível desde seu estado inicial salvo na cena.</summary>
        public void NewGame()
        {
            MenuSceneActions.LoadScene(MenuSceneActions.LevelScene);
        }

        /// <summary>Sem argumentos/retorno. Abre a página com os comandos realmente disponíveis.</summary>
        public void ShowControls()
        {
            if (controlsPanel != null) controlsPanel.Show();
        }

        /// <summary>Sem argumentos/retorno. Encaminha o encerramento da aplicação para a ação compartilhada.</summary>
        public void Quit()
        {
            MenuSceneActions.Quit();
        }
    }
}
