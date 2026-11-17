using ContraLike.Combat;
using UnityEngine;

namespace ContraLike.Enemies
{
    [DisallowMultipleComponent, RequireComponent(typeof(EnemyMovement2D))]
    [AddComponentMenu("Contra-Like/Враг/Анимация врага")]
    public sealed class EnemyVisuals : MonoBehaviour
    {
        [Min(0.1f), InspectorName("Скорость анимации")]
        public float animationSpeed = 1;
        [InspectorName("Исходный спрайт смотрит влево"), Tooltip("Включите, если персонаж на исходной картинке смотрит влево. Направление движения и атаки не меняется.")]
        public bool spriteFacesLeft;
        private EnemyMovement2D movement;
        private Rigidbody2D body;
        private SpriteRenderer sprite;
        private Animator animator;
        private Health health;
        private EnemyAttack2D attack;
        private CapsuleCollider2D capsule;
        private Vector3 authoredVisualOffset;
        private bool authoredFlipX;
        private bool dying;
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Attack = Animator.StringToHash("Attack");
        private static readonly int Hurt = Animator.StringToHash("Hurt");

        private void Awake()
        {
            movement = GetComponent<EnemyMovement2D>();
            body = GetComponent<Rigidbody2D>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponentInChildren<Animator>();
            health = GetComponent<Health>();
            attack = GetComponent<EnemyAttack2D>();
            capsule = GetComponent<CapsuleCollider2D>();
            if (sprite != null)
            {
                // Keep the alignment authored in the prefab; mirror the pivot offset with the art.
                authoredVisualOffset = transform.InverseTransformPoint(sprite.transform.position) - (Vector3)capsule.offset;
                authoredFlipX = sprite.flipX;
            }
        }

        private void OnEnable()
        {
            if (health != null) health.Damaged += OnDamaged;
            if (attack != null) attack.Attacked += OnAttacked;
        }

        private void OnDisable()
        {
            if (health != null) health.Damaged -= OnDamaged;
            if (attack != null) attack.Attacked -= OnAttacked;
        }

        private void LateUpdate()
        {
            if (dying) return;
            if (sprite != null)
            {
                var flipX = (movement.FacingDirection < 0) != spriteFacesLeft;
                var offset = authoredVisualOffset;
                if (flipX != authoredFlipX) offset.x = -offset.x;
                sprite.transform.position = transform.TransformPoint((Vector3)capsule.offset + offset);
                sprite.flipX = flipX;
            }
            if (animator == null) return;
            animator.speed = Mathf.Max(0.1f, animationSpeed);
            animator.SetFloat(Speed, body.linearVelocity.magnitude);
        }

        private void OnAttacked()
        {
            if (!dying && animator != null) animator.SetTrigger(Attack);
        }

        private void OnDamaged(Health source)
        {
            if (!dying && !source.IsDead && animator != null) animator.SetTrigger(Hurt);
        }

        internal Animator PlayDeath()
        {
            dying = true;
            if (animator == null || !animator.isActiveAndEnabled || !animator.isInitialized || animator.layerCount == 0)
                return null;

            var state = Animator.StringToHash(animator.GetLayerName(0) + ".Death");
            if (!animator.HasState(0, state)) return null;

            animator.speed = Mathf.Max(0.1f, animationSpeed);
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.ResetTrigger(Attack);
            animator.ResetTrigger(Hurt);
            animator.Play(state, 0, 0);
            return animator;
        }
    }
}
