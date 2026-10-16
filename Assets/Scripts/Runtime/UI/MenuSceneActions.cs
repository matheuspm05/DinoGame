using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjetoRetorno.UI
{
    /// <summary>Ações compartilhadas de navegação, sem estado persistente ou sistema de save.</summary>
    public static class MenuSceneActions
    {
        public const string MainMenuScene = "MainMenu";
        public const string LevelScene = "Level01_Nave";

        /// <summary>sceneName é o nome da cena de destino; sem retorno. Restaura o tempo/cursor e carrega a cena desde o início.</summary>
        public static void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        /// <summary>Sem argumentos/retorno. Restaura o tempo e encerra a build; no Editor, encerra somente o Play Mode.</summary>
        public static void Quit()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
