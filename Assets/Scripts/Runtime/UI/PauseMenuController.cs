using ProjetoRetorno.Level01;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjetoRetorno.UI
{
    /// <summary>Pausa o tempo e o controlador existente; permanece ativo mesmo quando o painel está fechado.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PauseMenuController : MonoBehaviour
    {
        public GameObject overlayRoot;
        public GameObject menuPage;
        public ControlsPanelController controlsPanel;
        public Button continueButton;
        public NaveWalkthroughController playerController;
        public bool IsPaused { get; private set; }
        private bool playerWasEnabled;

        /// <summary>Sem argumentos/retorno. Garante que cada entrada no nível comece sem pausa residual.</summary>
        private void Awake()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            if (overlayRoot != null) overlayRoot.SetActive(false);
            if (controlsPanel != null) controlsPanel.gameObject.SetActive(false);
            if (menuPage != null) menuPage.SetActive(true);
        }

        /// <summary>Sem argumentos/retorno. Lê ESC pelo Input System, inclusive com timeScale zero, e alterna a pausa.</summary>
        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (IsPaused) Resume();
            else Pause();
        }

        /// <summary>Sem argumentos/retorno. Congela o jogo, desativa o controle existente e mostra o painel/cursor.</summary>
        public void Pause()
        {
            if (IsPaused || overlayRoot == null) return;
            IsPaused = true;
            playerWasEnabled = playerController != null && playerController.enabled;
            if (playerController != null) playerController.enabled = false;
            Time.timeScale = 0f;
            if (controlsPanel != null) controlsPanel.gameObject.SetActive(false);
            if (menuPage != null) menuPage.SetActive(true);
            overlayRoot.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (EventSystem.current != null && continueButton != null)
                EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }

        /// <summary>Sem argumentos/retorno. Fecha todas as páginas, restaura tempo/controle e captura o cursor para continuar.</summary>
        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            if (overlayRoot != null) overlayRoot.SetActive(false);
            if (playerController != null && playerWasEnabled) playerController.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>Sem argumentos/retorno. Mostra os controles mantendo o tempo e o personagem pausados.</summary>
        public void ShowControls()
        {
            if (IsPaused && controlsPanel != null) controlsPanel.Show();
        }

        /// <summary>Sem argumentos/retorno. Encerra a pausa e recarrega a cena deste menu, preservando reinício na nave original e na expandida.</summary>
        public void RestartLevel()
        {
            Resume();
            MenuSceneActions.LoadScene(gameObject.scene.name);
        }

        /// <summary>Sem argumentos/retorno. Encerra a pausa e retorna à cena MainMenu.</summary>
        public void GoToMainMenu()
        {
            Resume();
            MenuSceneActions.LoadScene(MenuSceneActions.MainMenuScene);
        }

        /// <summary>Sem argumentos/retorno. Restaura o tempo e encerra a aplicação ou o Play Mode.</summary>
        public void Quit()
        {
            Resume();
            MenuSceneActions.Quit();
        }

        /// <summary>Sem argumentos/retorno. Evita deixar o tempo congelado se o sistema for desativado durante a pausa.</summary>
        private void OnDisable()
        {
            if (IsPaused) Resume();
        }
    }
}
