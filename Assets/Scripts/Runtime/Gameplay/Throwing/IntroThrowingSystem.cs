using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjetoRetorno.Gameplay
{
    /// <summary>Desafio inicial: coleta única, spawn por função, lançamentos físicos e abertura permanente por impacto válido.</summary>
    public sealed class IntroThrowingSystem : MonoBehaviour
    {
        public Level01GameManager level;
        public Transform holdPoint, projectilesRoot;
        public GameObject[] throwablePrefabs;
        public GameObject initialBox;
        public DoorController initialDoor;
        public ImpactButton impactButton;
        [Min(.1f)] public float throwImpulse = 11f;
        [Min(0)] public float throwTorque = .08f;
        [Min(.1f)] public float throwInterval = .45f;
        [Min(1)] public float projectileLifetime = 12f;
        [Range(1,20)] public int maximumProjectiles = 8;
        public bool HasStarted { get; private set; }
        public bool IsSolved { get; private set; }
        public ThrownObject HeldObject { get; private set; }
        private float nextThrow;
        private readonly Queue<ThrownObject> projectiles = new Queue<ThrownObject>();

        /// <summary>Sem argumentos/retorno. Reinicia porta, alvo e caixa junto com a cena, sem estado persistente.</summary>
        private void Awake()
        {
            HasStarted = IsSolved = false; nextThrow = 0;
            initialDoor.SetOpen(false, true);
            initialBox.SetActive(true);
            impactButton.SetActiveIndicator(false);
        }

        /// <summary>Sem argumentos/retorno. Lê o mouse pelo Input System apenas durante gameplay e respeita o intervalo entre lançamentos.</summary>
        private void Update()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) TryThrow();
        }

        /// <summary>Sem argumentos/retorno. Esconde a primeira caixa, cria a caixa segurada e muda o objetivo para acertar o alvo.</summary>
        public void BeginThrowing()
        {
            if (level == null || !level.CanInteract || HasStarted || IsSolved) return;
            HasStarted = true; initialBox.SetActive(false);
            SpawnHeldObject(); level.RefreshObjective();
        }

        /// <summary>Sem argumentos. Retorna a nova instância segurada, sorteada no array de prefabs reais; não cria nada após resolver o desafio.</summary>
        public ThrownObject SpawnHeldObject()
        {
            if (IsSolved || !HasStarted || HeldObject != null || throwablePrefabs == null || throwablePrefabs.Length == 0) return HeldObject;
            GameObject prefab = throwablePrefabs[Random.Range(0, throwablePrefabs.Length)];
            GameObject instance = Instantiate(prefab, holdPoint, false);
            instance.name = "Caixa_Segurada";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            HeldObject = instance.GetComponent<ThrownObject>();
            HeldObject.Hold(this);
            return HeldObject;
        }

        /// <summary>Sem argumentos. Retorna true quando lançou a caixa, após verificar pausa, cooldown e espaço livre diante da câmera.</summary>
        public bool TryThrow()
        {
            if (level == null || !level.CanInteract || IsSolved || HeldObject == null || Time.time < nextThrow) return false;
            Transform view = level.player.view;
            Vector3 position = view.position + view.forward * .6f + view.right * .12f;
            Vector3 segment = position - view.position;
            foreach (var hit in Physics.SphereCastAll(view.position, HeldObject.radius, segment.normalized, segment.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(level.player.transform))
                { level.hud.ShowMessage("Afaste-se da parede para arremessar"); return false; }
            ThrownObject launched = HeldObject; HeldObject = null;
            launched.transform.SetParent(projectilesRoot, true);
            launched.transform.position = position;
            launched.Launch(this, view.forward, throwImpulse, throwTorque, projectileLifetime);
            projectiles.Enqueue(launched);
            while (projectiles.Count > maximumProjectiles)
            { ThrownObject oldest = projectiles.Dequeue(); if (oldest != null) Destroy(oldest.gameObject); }
            nextThrow = Time.time + throwInterval;
            SpawnHeldObject();
            return true;
        }

        /// <summary>projectile identifica o impacto. Retorna true somente para caixa lançada por este sistema; abre a porta e encerra o spawn.</summary>
        public bool TrySolve(ThrownObject projectile)
        {
            if (IsSolved || projectile == null || !projectile.WasThrown || projectile.Owner != this) return false;
            IsSolved = true; initialDoor.SetOpen(true); impactButton.SetActiveIndicator(true);
            if (HeldObject != null) Destroy(HeldObject.gameObject);
            HeldObject = null;
            while (projectiles.Count > 0)
            { var old = projectiles.Dequeue(); if (old != null) Destroy(old.gameObject); }
            Destroy(projectile.gameObject);
            level.hud.ShowMessage("Alvo ativado! Porta da contenção aberta.");
            level.RefreshObjective();
            return true;
        }
    }
}
