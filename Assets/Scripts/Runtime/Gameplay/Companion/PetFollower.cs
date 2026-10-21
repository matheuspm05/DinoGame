using System.Collections.Generic;
using UnityEngine;

namespace ProjetoRetorno.Gameplay
{
    /// <summary>Sparrow em voo: alvo lateral, varreduras contra sólidos e trilha curta para contornar cantos, sem NavMesh.</summary>
    public sealed class PetFollower : MonoBehaviour, IInteractable
    {
        public Level01GameManager level;
        public Transform target;
        public Animator animator;
        public Vector3 followOffset = new Vector3(.65f,.14f,.9f);
        public float smoothTime = .24f, maximumSpeed = 7f, obstacleRadius = .14f;
        public string flightState = "Base Layer.Fly";
        public bool IsRecovering { get; private set; }
        public bool IsAdopted { get; private set; }
        public Collider adoptionTarget;
        private Vector3 velocity, lastHead;
        private float bobTime, stuckTime, recoveryTime;
        private readonly List<Vector3> trail = new List<Vector3>();
        private Renderer[] renderers;
        private bool[] originalVisible;

        /// <summary>Sem argumentos/retorno. Reutiliza o controller, preserva o local de espera e deixa as animações reais sob responsabilidade do componente de apresentação.</summary>
        private void Start()
        {
            renderers = GetComponentsInChildren<Renderer>(true); originalVisible = new bool[renderers.Length];
            for (int i=0;i<renderers.Length;i++) originalVisible[i]=renderers[i].enabled;
            animator.applyRootMotion = false; animator.updateMode = AnimatorUpdateMode.Normal;
            IsAdopted = false;
            if (adoptionTarget != null) adoptionTarget.enabled = true;
            lastHead = target.position;
        }

        /// <summary>Sem argumentos/retorno. Interpola o voo, registra a trilha do jogador e respeita paredes/portas e pausa.</summary>
        private void LateUpdate()
        {
            if (!IsAdopted || target == null || level == null || Time.timeScale <= 0 || level.IsComplete) return;
            bobTime += Time.deltaTime;
            if ((target.position-lastHead).sqrMagnitude > .16f)
            {
                trail.Add(target.position); lastHead=target.position;
                if(trail.Count>48)trail.RemoveAt(0);
            }
            if (Vector3.Distance(transform.position,target.position)>9 || stuckTime>4)
            { if(!IsRecovering){IsRecovering=true;recoveryTime=.18f;SetVisible(false);} }
            if (IsRecovering)
            {
                recoveryTime-=Time.deltaTime;
                if(recoveryTime<=0 && TryAnchor(true,out Vector3 safe))
                {transform.position=safe;velocity=Vector3.zero;stuckTime=0;trail.Clear();IsRecovering=false;SetVisible(true);}
                return;
            }
            if (!TryAnchor(false,out Vector3 goal)) goal=target.position;
            if (!ClearSegment(transform.position,goal,out _))
            {
                while(trail.Count>0 && Vector3.Distance(transform.position,trail[0])<.3f)trail.RemoveAt(0);
                if(trail.Count>0 && ClearSegment(transform.position,trail[0],out _))goal=trail[0];
            }
            Vector3 next=Vector3.SmoothDamp(transform.position,goal,ref velocity,smoothTime,maximumSpeed,Time.deltaTime);
            if(!ClearSegment(transform.position,next,out RaycastHit obstruction))
            {
                Vector3 direction=(next-transform.position).normalized;
                next=transform.position+direction*Mathf.Max(0,obstruction.distance-.015f);velocity=Vector3.zero;
            }
            Vector3 movement=next-transform.position;
            bool moving=movement.sqrMagnitude>.000002f;
            stuckTime=!moving && Vector3.Distance(transform.position,target.position)>2?stuckTime+Time.deltaTime:0;
            transform.position=next;
            Vector3 heading = Vector3.ProjectOnPlane(movement,Vector3.up);
            if(heading.sqrMagnitude>.00001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(heading.normalized,Vector3.up),Time.deltaTime*8);
            animator.speed=moving?1:.8f;
        }

        /// <summary>Sem argumentos. Retorna a ação opcional apenas antes da adoção e durante gameplay válido.</summary>
        public string GetPrompt() { return !IsAdopted && level != null && level.CanInteract ? "[F] Fazer amizade com o passarinho" : ""; }

        /// <summary>Sem argumentos/retorno. Adota uma única vez, desativa o alvo de raycast e inicia a subida suave sem alterar os puzzles.</summary>
        public void Interact()
        {
            if (IsAdopted || level == null || !level.CanInteract) return;
            IsAdopted = true;
            if (adoptionTarget != null) adoptionTarget.enabled = false;
            trail.Clear(); lastHead = target.position;
            level.hud.ShowMessage("Novo companheiro encontrado!");
        }
        /// <summary>behind seleciona recuperação fora da vista e anchor recebe a posição; retorna true quando cabe no ambiente atual do jogador.</summary>
        private bool TryAnchor(bool behind,out Vector3 anchor)
        {
            Vector3 forward=Vector3.ProjectOnPlane(target.forward,Vector3.up).normalized;
            if(forward.sqrMagnitude<.01f)forward=level.player.transform.forward;
            Vector3 right=Vector3.Cross(Vector3.up,forward);
            foreach(float side in new[]{1f,-1f})
            {
                anchor=target.position+right*followOffset.x*side+forward*followOffset.z*(behind?-1:1)+
                    Vector3.up*(followOffset.y+Mathf.Sin(bobTime*2)*.035f);
                if(ClearSegment(target.position,anchor,out _) && IsFree(anchor))return true;
            }
            anchor=target.position;return false;
        }

        /// <summary>position define a esfera de segurança; retorna true sem sólidos, ignorando jogador, projéteis e o próprio pet.</summary>
        private bool IsFree(Vector3 position)
        {
            foreach(var c in Physics.OverlapSphere(position,obstacleRadius,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                if(!Ignored(c))return false;
            return true;
        }

        /// <summary>from/to definem o trajeto e hit recebe o primeiro obstáculo; retorna true quando a esfera pode avançar sem atravessar sólidos.</summary>
        private bool ClearSegment(Vector3 from,Vector3 to,out RaycastHit hit)
        {
            hit=default;Vector3 delta=to-from;
            if(delta.sqrMagnitude<.000001f)return true;
            var hits=Physics.SphereCastAll(from,obstacleRadius,delta.normalized,delta.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var candidate in hits)if(!Ignored(candidate.collider)){hit=candidate;return false;}
            return true;
        }

        /// <summary>collider identifica o corpo consultado. Retorna true para jogador, pet ou caixa, que não devem impedir o acompanhamento visual.</summary>
        private bool Ignored(Collider collider)
        {
            return collider.transform.IsChildOf(level.player.transform) || collider.transform.IsChildOf(transform) ||
                collider.GetComponentInParent<ThrownObject>()!=null;
        }

        /// <summary>visible define a visibilidade; sem retorno. Oculta a recuperação e restaura somente renderizadores originalmente habilitados.</summary>
        private void SetVisible(bool visible)
        {
            for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].enabled=visible&&originalVisible[i];
        }
    }
}
