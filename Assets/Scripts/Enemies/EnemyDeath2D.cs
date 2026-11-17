using System.Collections;
using ContraLike.Combat;
using UnityEngine;

namespace ContraLike.Enemies
{
    [DisallowMultipleComponent, RequireComponent(typeof(Health), typeof(Rigidbody2D))]
    [AddComponentMenu("Contra-Like/Враг/Исчезновение после смерти")]
    public sealed class EnemyDeath2D : MonoBehaviour
    {
        [Min(0), InspectorName("Время до исчезновения"), Tooltip("Секунды после смерти. Враг перестаёт атаковать сразу и исчезает после этой паузы, но не раньше окончания анимации смерти.")]
        public float disappearAfter = 1;

        private Health health;
        private static readonly int Death = Animator.StringToHash("Death");
        internal bool DeathAnimationFinished { get; private set; } = true;

        private void OnEnable()
        {
            health = GetComponent<Health>();
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (health != null) health.Died -= OnDied;
        }

        private void OnDied(Health _)
        {
            var body = GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            foreach (var shape in GetComponentsInChildren<Collider2D>()) shape.enabled = false;
            var movement = GetComponent<EnemyMovement2D>();
            var attack = GetComponent<EnemyAttack2D>();
            if (movement != null) movement.enabled = false;
            if (attack != null) attack.enabled = false;
            var animator = GetComponent<EnemyVisuals>()?.PlayDeath();
            DeathAnimationFinished = animator == null;
            StartCoroutine(Disappear(animator));
        }

        private IEnumerator Disappear(Animator animator)
        {
            var disappearAt = Time.time + Mathf.Max(0.05f, disappearAfter);
            if (animator != null)
            {
                // Wait for the Animator to evaluate the state selected by PlayDeath.
                if (animator.updateMode == AnimatorUpdateMode.Fixed) yield return new WaitForFixedUpdate();
                yield return null;
                yield return null;
                while (animator != null && animator.isActiveAndEnabled && animator.isInitialized && animator.layerCount > 0)
                {
                    var state = animator.GetCurrentAnimatorStateInfo(0);
                    if (state.shortNameHash != Death || state.normalizedTime >= 1f) break;
                    yield return null;
                }
                DeathAnimationFinished = true;
            }

            var remaining = disappearAt - Time.time;
            if (remaining > 0) yield return new WaitForSeconds(remaining);
            Destroy(gameObject);
        }
    }
}
