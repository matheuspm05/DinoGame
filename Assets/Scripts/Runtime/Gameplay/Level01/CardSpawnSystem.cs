using System.Collections.Generic;
using UnityEngine;

namespace ProjetoRetorno.Gameplay
{
    [System.Serializable]
    public sealed class CardSpawnPoint
    {
        public Transform point;
        public Collider support;
    }

    /// <summary>Sorteia apenas pontos apoiados e livres sobre móveis existentes; reutiliza o único cartão da cena.</summary>
    public sealed class CardSpawnSystem : MonoBehaviour
    {
        public Level01GameManager level;
        public CardSpawnPoint[] candidates;
        public Bounds laboratoryBounds = new Bounds(new Vector3(20,1.5f,21),new Vector3(19.5f,3,15.5f));
        public Vector3 clearanceHalfExtents = new Vector3(.15f,.015f,.12f);
        public int LastSelectedIndex { get; private set; } = -1;

        /// <summary>Sem argumentos/retorno. Faz o sorteio depois da inicialização e sincronização dos colliders da cena.</summary>
        private void Start() { Physics.SyncTransforms(); TrySpawn(); }

        /// <summary>index identifica um candidato. Retorna true somente com apoio físico, limites internos e espaço livre para o cartão.</summary>
        public bool IsCandidateValid(int index)
        {
            if (candidates == null || index < 0 || index >= candidates.Length) return false;
            var candidate = candidates[index];
            if (candidate == null || candidate.point == null || candidate.support == null || !candidate.support.enabled) return false;
            Vector3 position = candidate.point.position;
            if (!laboratoryBounds.Contains(position)) return false;
            var ray = new Ray(position + Vector3.up * .08f, Vector3.down);
            if (!candidate.support.Raycast(ray, out RaycastHit supportHit, .16f) || Mathf.Abs(supportHit.point.y-position.y) > .04f) return false;
            foreach (var c in Physics.OverlapBox(position, clearanceHalfExtents, Quaternion.identity,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (c != candidate.support && !c.transform.IsChildOf(level.keycard.transform)) return false;
            return true;
        }

        /// <summary>Sem argumentos. Retorna true ao sortear um dos pontos verificados; não instancia nem duplica o cartão.</summary>
        public bool TrySpawn()
        {
            var valid = new List<int>();
            for (int i=0; candidates != null && i<candidates.Length; i++) if (IsCandidateValid(i)) valid.Add(i);
            if (valid.Count == 0)
            {
                Debug.LogError("Cartão: nenhum ponto de laboratório está apoiado/livre. Reveja CardSpawnSystem.");
                return false;
            }
            LastSelectedIndex = valid[Random.Range(0,valid.Count)];
            Transform point = candidates[LastSelectedIndex].point;
            level.keycard.transform.SetPositionAndRotation(point.position, point.rotation);
            level.keycard.gameObject.SetActive(true);
            Physics.SyncTransforms();
            return true;
        }
    }
}
