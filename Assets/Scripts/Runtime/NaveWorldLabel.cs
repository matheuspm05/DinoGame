using UnityEngine;

namespace ProjetoRetorno.Level01
{
    /// <summary>Mantém o atlas dinâmico da fonte em placas 3D que respeitam a profundidade das paredes.</summary>
    [ExecuteAlways, RequireComponent(typeof(TextMesh), typeof(MeshRenderer))]
    public sealed class NaveWorldLabel : MonoBehaviour
    {
        /// <summary>Sem argumentos/retorno. Solicita os caracteres e vincula o atlas atual ao renderizador desta placa.</summary>
        private void OnEnable()
        {
            TextMesh text = GetComponent<TextMesh>();
            if (text.font == null) return;
            text.font.RequestCharactersInTexture(text.text, text.fontSize, text.fontStyle);
            Font.textureRebuilt += RefreshAtlas;
            RefreshAtlas(text.font);
        }

        /// <summary>font identifica o atlas reconstruído; sem retorno. Atualiza somente a placa que usa essa fonte.</summary>
        private void RefreshAtlas(Font font)
        {
            TextMesh text = GetComponent<TextMesh>();
            if (font == null || font != text.font) return;
            var properties = new MaterialPropertyBlock();
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            renderer.GetPropertyBlock(properties);
            properties.SetTexture("_MainTex", font.material.mainTexture);
            renderer.SetPropertyBlock(properties);
        }

        /// <summary>Sem argumentos/retorno. Remove a inscrição ao fechar a cena, sair de Play ou desativar a placa.</summary>
        private void OnDisable()
        {
            Font.textureRebuilt -= RefreshAtlas;
        }
    }
}
