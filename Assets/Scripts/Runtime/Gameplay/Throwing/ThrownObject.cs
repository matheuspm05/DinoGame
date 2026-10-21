using UnityEngine;

namespace ProjetoRetorno.Gameplay
{
    /// <summary>Identifica uma instância arremessável real e controla seus estados segurado/físico.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ThrownObject : MonoBehaviour
    {
        public Rigidbody body;
        public Collider solidCollider;
        public float radius = .18f;
        public IntroThrowingSystem Owner { get; private set; }
        public bool WasThrown { get; private set; }

        /// <summary>owner identifica o sistema; sem retorno. Torna o objeto visual no HoldPoint, sem colisões ou raycasts.</summary>
        public void Hold(IntroThrowingSystem owner)
        {
            Owner = owner; WasThrown = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.interpolation = RigidbodyInterpolation.None;
            body.isKinematic = true; body.useGravity = false;
            solidCollider.enabled = false;
            foreach (Transform child in GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 2;
        }

        /// <summary>owner, direção, impulso, torque e duração configuram o lançamento; sem retorno. Ativa física contínua e programa a remoção.</summary>
        public void Launch(IntroThrowingSystem owner, Vector3 direction, float impulse, float torque, float lifetime)
        {
            Owner = owner; WasThrown = true;
            foreach (Transform child in GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 0;
            solidCollider.enabled = true;
            body.isKinematic = false; body.useGravity = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            Physics.IgnoreCollision(solidCollider, owner.level.player.GetComponent<CharacterController>(), true);
            body.AddForce(direction.normalized * impulse, ForceMode.Impulse);
            body.AddTorque(Random.insideUnitSphere * torque, ForceMode.Impulse);
            Destroy(gameObject, lifetime);
        }
    }
}
