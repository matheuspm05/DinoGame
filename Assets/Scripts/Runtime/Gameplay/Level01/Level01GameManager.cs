using ProjetoRetorno.Level01;
using ProjetoRetorno.UI;
using UnityEngine;

namespace ProjetoRetorno.Gameplay
{
    /// <summary>Estado exclusivo desta cena. Recarregar a cena recria os puzzles sem dados persistentes.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed class Level01GameManager : MonoBehaviour
    {
        public NaveWalkthroughController player;
        public PauseMenuController pause;
        public ObjectiveHUD hud;
        public Level01Interactable battery, energyPanel, keycard, cardReader;
        public Level01Interactable[] terminals = new Level01Interactable[3];
        public GameObject installedBattery;
        public DoorController laboratoryDoor, hangarDoor, exitDoor;
        public IntroThrowingSystem initialRoom;
        public CardSpawnSystem cardSpawn;
        public Transform batterySocket;
        public GameObject exitLight;
        public bool LaboratoryEntered { get; private set; }
        private readonly bool[] activated = new bool[3];
        public bool HasBattery { get; private set; }
        public bool EnergyRestored { get; private set; }
        public bool HasKeycard { get; private set; }
        public bool HangarUnlocked { get; private set; }
        public bool IsComplete { get; private set; }
        public int ActiveTerminals => (activated[0] ? 1 : 0) + (activated[1] ? 1 : 0) + (activated[2] ? 1 : 0);
        public bool CanInteract => !IsComplete && Time.timeScale > 0 && player != null && player.enabled &&
            (pause == null || !pause.IsPaused) && Cursor.lockState == CursorLockMode.Locked;
        public string CurrentObjective => initialRoom != null && !initialRoom.IsSolved ?
            (initialRoom.HasStarted ? "Acerte o botão acima da porta com a caixa" : "Pegue uma caixa na sala inicial") :
            !HasBattery && !EnergyRestored ? "Encontre uma bateria na nave" :
            !EnergyRestored ? "Instale a bateria na sala de energia" :
            !HasKeycard ? (LaboratoryEntered ? "Encontre o cartão de segurança" : "Entre no laboratório") :
            !HangarUnlocked ? "Utilize o cartão no leitor da segurança" :
            ActiveTerminals < 3 ? "Ative os três terminais do hangar (" + ActiveTerminals + "/3)" :
            "Atravesse a saída para escapar da nave";

        /// <summary>Sem argumentos/retorno. Inicializa itens, portas e indicadores no estado inicial ao carregar a cena.</summary>
        private void Awake()
        {
            HasBattery = EnergyRestored = HasKeycard = HangarUnlocked = IsComplete = LaboratoryEntered = false;
            if (exitLight != null) exitLight.SetActive(false);
            for (int i = 0; i < activated.Length; i++) activated[i] = false;
            laboratoryDoor.SetOpen(false, true); hangarDoor.SetOpen(false, true); exitDoor.SetOpen(false, true);
            battery.gameObject.SetActive(true); keycard.gameObject.SetActive(true);
            installedBattery.SetActive(false);
            energyPanel.SetIndicator(false); cardReader.SetIndicator(false);
            foreach (var terminal in terminals) terminal.SetIndicator(false);
            hud.level = this;
            hud.ShowCompletion(false);
            hud.SetObjective(CurrentObjective);
        }

        /// <summary>index identifica um dos três terminais. Retorna false para índices inválidos ou sistemas ainda não ativados.</summary>
        public bool IsTerminalActive(int index) { return index >= 0 && index < activated.Length && activated[index]; }

        /// <summary>item identifica o alvo; sem retorno. Valida requisitos, atualiza portas/itens e apresenta objetivo e mensagem.</summary>
        public void Interact(Level01Interactable item)
        {
            if (!CanInteract || item == null || item.level != this) return;
            switch (item.kind)
            {
                case Level01InteractionKind.Battery:
                    if (HasBattery || EnergyRestored) return;
                    HasBattery = true; battery.gameObject.SetActive(false); hud.ShowMessage("Bateria coletada!"); break;
                case Level01InteractionKind.EnergyPanel:
                    if (EnergyRestored) return;
                    if (!HasBattery) { hud.ShowMessage("Bateria necessária"); return; }
                    HasBattery = false; EnergyRestored = true;
                    if (batterySocket != null) installedBattery.transform.SetPositionAndRotation(batterySocket.position, batterySocket.rotation);
                    installedBattery.SetActive(true);
                    energyPanel.SetIndicator(true); laboratoryDoor.SetOpen(true);
                    hud.ShowMessage("Energia restaurada! Laboratório desbloqueado!"); break;
                case Level01InteractionKind.Keycard:
                    if (!EnergyRestored) { hud.ShowMessage("Restaure a energia primeiro"); return; }
                    if (HasKeycard) return;
                    HasKeycard = true; keycard.gameObject.SetActive(false);
                    hud.ShowMessage("Cartão de segurança adquirido!"); break;
                case Level01InteractionKind.CardReader:
                    if (HangarUnlocked) return;
                    if (!HasKeycard) { hud.ShowMessage("Cartão de acesso necessário"); return; }
                    HangarUnlocked = true; cardReader.SetIndicator(true); hangarDoor.SetOpen(true);
                    hud.ShowMessage("Acesso autorizado. Hangar desbloqueado!"); break;
                case Level01InteractionKind.HangarTerminal:
                    int index = item.terminalIndex;
                    if (!HangarUnlocked || index < 0 || index >= activated.Length || activated[index]) return;
                    activated[index] = true; item.SetIndicator(true);
                    if (ActiveTerminals == 3) { exitDoor.SetOpen(true); if (exitLight != null) exitLight.SetActive(true); hud.ShowMessage("Todos os sistemas ativados. Saída liberada!"); }
                    else hud.ShowMessage("Terminal " + (index + 1) + " ativado! (" + ActiveTerminals + "/3)");
                    break;
            }
            hud.SetObjective(CurrentObjective);
        }

        /// <summary>Sem argumentos/retorno. Recalcula o único objetivo após mudanças do desafio inicial ou da visita ao laboratório.</summary>
        public void RefreshObjective() { if (hud != null) hud.SetObjective(CurrentObjective); }

        /// <summary>Sem argumentos/retorno. Registra a entrada no laboratório somente com energia e atualiza o HUD sem alterar a coleta.</summary>
        public void EnterLaboratory()
        {
            if (!EnergyRestored || LaboratoryEntered) return;
            LaboratoryEntered = true; RefreshObjective();
        }

        /// <summary>Sem argumentos. Retorna true apenas depois dos três terminais e da liberação da saída; congela o jogo e mostra a conclusão.</summary>
        public bool TryComplete()
        {
            if (IsComplete || ActiveTerminals != 3 || !exitDoor.IsOpen) return false;
            IsComplete = true;
            if (pause != null) pause.enabled = false;
            player.enabled = false;
            Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            hud.ShowCompletion(true);
            return true;
        }

        /// <summary>Sem argumentos/retorno. Restaura o tempo através da navegação existente e carrega o Menu Principal.</summary>
        public void ReturnToMainMenu() { MenuSceneActions.LoadScene(MenuSceneActions.MainMenuScene); }
    }
}
