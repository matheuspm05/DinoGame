using System;
using System.IO;
using System.Linq;
using ProjetoRetorno.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjetoRetorno.Level01.Editor
{
    [Serializable] internal sealed class ExpandedTourRoute { public Vector3[] points; }

    /// <summary>Percorre a candidata em Play com o CharacterController real, sem salvar o estado temporário nem trocar o nível padrão.</summary>
    [InitializeOnLoad]
    public static class Level01ExpandedPlayValidation
    {
        private const string Pending = "ProjetoRetorno.ExpandedTour.Pending";
        private const string Previous = "ProjetoRetorno.ExpandedTour.Previous";
        private const string Result = "ProjetoRetorno.ExpandedTour.Result";
        private static NaveWalkthroughController player;
        private static CharacterController capsule;
        private static Vector3[] route;
        private static int target, steps, stalled;
        private static double deadline;

        /// <summary>Sem argumentos/retorno. Registra as transições de Play para iniciar e encerrar a validação com restauração da cena inicial.</summary>
        static Level01ExpandedPlayValidation() { EditorApplication.playModeStateChanged += OnPlayMode; }

        /// <summary>Sem argumentos/retorno. Inicia a cena candidata em Play preservando o fluxo normal de MainMenu.</summary>
        [MenuItem("Tools/Validar Nave Expandida em Play")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Execute fora de Play.");
            if (!File.Exists(Level01NaveGenerator.ExpandedScenePath)) throw new FileNotFoundException("Gere a cena expandida primeiro.");
            SessionState.SetString(Previous, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetString(Result, "Validação em Play da nave expandida\n");
            SessionState.SetBool(Pending, true);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(Level01NaveGenerator.ExpandedScenePath);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            EditorApplication.isPlaying = true;
        }

        /// <summary>state indica a transição; sem retorno. Configura o percurso em memória e restaura a cena inicial ao sair de Play.</summary>
        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                try
                {
                    player = UnityEngine.Object.FindFirstObjectByType<NaveWalkthroughController>();
                    if (player == null || player.gameObject.scene.path != Level01NaveGenerator.ExpandedScenePath) throw new InvalidOperationException("Cena/jogador incorretos.");
                    capsule = player.GetComponent<CharacterController>();
                    if (Mathf.Abs(capsule.height - 1.8f) > .01f || player.view == null || player.view.GetComponent<Camera>() == null)
                        throw new InvalidOperationException("Configuração original de jogador não preservada.");
                    route = JsonUtility.FromJson<ExpandedTourRoute>(File.ReadAllText("Docs/Relatorios/Nivel01_Expandido/Level01_Expanded_Navegacao.json")).points;
                    // O passeio verifica geometria com as portas abertas; a progressão fechada é testada pelo validador de gameplay.
                    foreach (var gate in UnityEngine.Object.FindObjectsByType<ProjetoRetorno.Gameplay.DoorController>(FindObjectsSortMode.None)) gate.SetOpen(true, true);
                    player.enabled = false;
                    target = 0; steps = 0; stalled = 0;
                    deadline = EditorApplication.timeSinceStartup + 120;
                    EditorApplication.update += Tick;
                }
                catch (Exception exception) { Fail(exception); }
            }
            if (state == PlayModeStateChange.ExitingPlayMode) { EditorApplication.update -= Tick; Time.timeScale = 1; }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                string previous = SessionState.GetString(Previous, "");
                EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
                string result = SessionState.GetString(Result, "");
                if (!result.Contains("RESULTADO:")) result += "RESULTADO: INTERROMPIDO.\n";
                File.WriteAllText("Docs/Relatorios/Nivel01_Expandido/Level01_Expanded_Play.txt", result);
                SessionState.SetBool(Pending, false);
                Debug.Log(result);
            }
        }

        /// <summary>Sem argumentos/retorno. Move a cápsula real pela rota, detecta bloqueios e confirma pausa/retomada ao concluir todos os setores.</summary>
        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Percurso excedeu dois minutos.");
                if (target >= route.Length)
                {
                    player.enabled = true;
                    PauseMenuController pause = UnityEngine.Object.FindFirstObjectByType<PauseMenuController>();
                    pause.Pause();
                    if (Time.timeScale != 0 || player.enabled || !Cursor.visible) throw new InvalidOperationException("Pausa da candidata falhou.");
                    pause.Resume();
                    if (Time.timeScale != 1 || !player.enabled) throw new InvalidOperationException("Retomada da candidata falhou.");
                    Append("Jogador existente, câmera, cápsula humana e pausa/retomada preservados.");
                    Append("Movimentos físicos da cápsula: " + steps + ". Rota principal e todos os desvios percorridos.");
                    Append("RESULTADO: APROVADO.");
                    EditorApplication.update -= Tick;
                    EditorApplication.isPlaying = false;
                    return;
                }
                Vector3 delta = route[target] - player.transform.position; delta.y = 0;
                if (delta.magnitude < .04f)
                {
                    Append("Ponto alcançado: " + route[target]); target++; stalled = 0; return;
                }
                Vector3 before = player.transform.position;
                capsule.Move(Vector3.ClampMagnitude(delta, .25f) + Vector3.down * .04f);
                Vector3 movement = player.transform.position - before; movement.y = 0;
                steps++;
                stalled = movement.sqrMagnitude < .00001f ? stalled + 1 : 0;
                if (stalled > 20) throw new InvalidOperationException("Cápsula bloqueada em " + player.transform.position + " seguindo para " + route[target]);
                if (player.transform.position.y < -.1f || player.transform.position.y > .4f) throw new InvalidOperationException("Piso descontínuo: " + player.transform.position);
            }
            catch (Exception exception) { Fail(exception); }
        }

        /// <summary>exception descreve a falha; sem retorno. Registra o erro e encerra Play para restaurar o Editor.</summary>
        private static void Fail(Exception exception)
        {
            Append(exception + "\nRESULTADO: FALHOU."); EditorApplication.isPlaying = false;
        }

        /// <summary>message é a linha de evidência; sem retorno. Persiste o relatório entre as recargas de domínio do Editor.</summary>
        private static void Append(string message) { SessionState.SetString(Result, SessionState.GetString(Result, "") + message + "\n"); }
    }
}
