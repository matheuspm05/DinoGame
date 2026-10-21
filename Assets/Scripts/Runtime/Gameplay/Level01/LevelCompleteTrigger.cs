using ProjetoRetorno.Level01;
using UnityEngine;

namespace ProjetoRetorno.Gameplay
{
    /// <summary>Detecta somente o jogador no volume após o portão; o gerente continua validando os três terminais.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class LevelCompleteTrigger : MonoBehaviour
    {
        public Level01GameManager level;
        private BoxCollider region;

        /// <summary>Sem argumentos/retorno. Guarda o trigger existente para a verificação de presença do CharacterController.</summary>
        private void Awake() { region=GetComponent<BoxCollider>(); }

        /// <summary>other é o collider que entrou. Sem retorno. Valida a identidade do jogador e solicita a conclusão existente.</summary>
        private void OnTriggerEnter(Collider other) { CheckPlayer(other); }

        /// <summary>other é o collider que permanece no volume. Sem retorno. Recupera contatos cujo evento de entrada tenha sido perdido.</summary>
        private void OnTriggerStay(Collider other) { CheckPlayer(other); }

        /// <summary>other identifica o contato. Sem retorno. Ignora pet/caixas, pausa e partidas já concluídas antes de encaminhar ao gerente.</summary>
        private void CheckPlayer(Collider other)
        {
            if (level==null||level.player==null||level.IsComplete||Time.timeScale<=0) return;
            if (other==level.player.GetComponent<CharacterController>()) level.TryComplete();
        }

        /// <summary>Sem argumentos/retorno. Confere presença real no mesmo volume para não depender de um único callback físico do CharacterController.</summary>
        private void LateUpdate()
        {
            if (level==null||level.player==null||level.IsComplete||level.ActiveTerminals!=3||Time.timeScale<=0||
                region==null||!region.enabled||!region.isTrigger) return;
            var body=level.player.GetComponent<CharacterController>();
            if (body==null||!body.enabled) return;
            Vector3 local=region.transform.InverseTransformPoint(body.bounds.center)-region.center;
            Vector3 half=region.size*.5f;
            if (Mathf.Abs(local.x)<=half.x&&Mathf.Abs(local.y)<=half.y&&Mathf.Abs(local.z)<=half.z) level.TryComplete();
        }
    }
}
