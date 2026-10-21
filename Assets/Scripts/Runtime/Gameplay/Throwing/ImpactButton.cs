using UnityEngine;

namespace ProjetoRetorno.Gameplay
{
    /// <summary>Alvo sólido acima da porta; contato do jogador ou decoração não resolve o desafio.</summary>
    public sealed class ImpactButton : MonoBehaviour
    {
        public IntroThrowingSystem owner;
        public Renderer indicator;
        /// <summary>collision descreve o contato. Sem retorno. Confere o Rigidbody e a identidade de caixa lançada antes de aceitar o impacto.</summary>
        private void OnCollisionEnter(Collision collision)
        {
            var projectile = collision.rigidbody == null ? null : collision.rigidbody.GetComponent<ThrownObject>();
            if (owner != null) owner.TrySolve(projectile);
        }

        /// <summary>active define o estado visual; sem retorno. Usa propriedade por instância e preserva o material original.</summary>
        public void SetActiveIndicator(bool active)
        {
            if (indicator == null) return;
            var block = new MaterialPropertyBlock();
            Color color = active ? new Color(.1f,1,.4f) : new Color(.1f,.75f,1);
            block.SetColor("_BaseColor", color); block.SetColor("_EmissionColor", color * 2f);
            indicator.SetPropertyBlock(block);
        }
    }
}
