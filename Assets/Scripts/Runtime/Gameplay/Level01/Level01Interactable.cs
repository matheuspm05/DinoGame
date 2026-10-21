using UnityEngine;

namespace ProjetoRetorno.Gameplay
{
    public enum Level01InteractionKind { Battery, EnergyPanel, Keycard, CardReader, HangarTerminal }

    /// <summary>Configuração reutilizável dos itens e painéis, sem manter uma segunda cópia do progresso.</summary>
    public sealed class Level01Interactable : MonoBehaviour, IInteractable
    {
        public Level01GameManager level;
        public Level01InteractionKind kind;
        [Range(0,2)] public int terminalIndex;
        public Renderer indicator;

        /// <summary>Sem argumentos. Retorna a ação contextual correspondente ao tipo e ao estado atual.</summary>
        public string GetPrompt()
        {
            if (level == null) return "";
            switch (kind)
            {
                case Level01InteractionKind.Battery: return "[F] Coletar bateria";
                case Level01InteractionKind.EnergyPanel: return level.EnergyRestored ? "Energia restaurada" : "[F] Instalar bateria";
                case Level01InteractionKind.Keycard: return "[F] Coletar cartão";
                case Level01InteractionKind.CardReader: return level.HangarUnlocked ? "Acesso autorizado" : "[F] Usar cartão de acesso";
                default: return level.IsTerminalActive(terminalIndex) ? "Terminal ativado" : "[F] Ativar terminal " + (terminalIndex + 1);
            }
        }

        /// <summary>Sem argumentos/retorno. Encaminha a ação ao gerente da cena, que verifica e atualiza o estado único.</summary>
        public void Interact()
        {
            if (level != null) level.Interact(this);
        }

        /// <summary>active define a cor do indicador; sem retorno. Usa MaterialPropertyBlock e preserva materiais compartilhados.</summary>
        public void SetIndicator(bool active)
        {
            if (indicator == null) return;
            var block = new MaterialPropertyBlock();
            indicator.GetPropertyBlock(block);
            Color color = active ? new Color(.15f, 1f, .5f) : new Color(1f, .3f, .08f);
            block.SetColor("_BaseColor", color);
            block.SetColor("_EmissionColor", color * .7f);
            indicator.SetPropertyBlock(block);
        }
    }
}
