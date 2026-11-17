using ContraLike.Combat;
using ContraLike.Player;
using UnityEngine;

namespace ContraLike.Enemies
{
    public enum EnemyMovementType
    {
        [InspectorName("Ходит по земле")] Ground,
        [InspectorName("Летает")] Flying
    }

    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(Health))]
    [AddComponentMenu("Contra-Like/Враг/Движение врага")]
    public sealed class EnemyMovement2D : MonoBehaviour
    {
        [InspectorName("Тип движения")]
        public EnemyMovementType movementType;
        [Min(0), InspectorName("Скорость движения"), Tooltip("Единиц в секунду. Враг начинает преследовать героя, когда замечает его.")]
        public float moveSpeed = 2;
        [Min(0), InspectorName("Дальность обнаружения"), Tooltip("На каком расстоянии враг замечает главного героя.")]
        public float detectionRange = 14;
        [InspectorName("Патрулировать без героя"), Tooltip("Наземный враг ходит туда-сюда, пока герой не попал в дальность обнаружения.")]
        public bool patrolWhenIdle = true;
        [Min(0), InspectorName("Высота полёта над героем"), Tooltip("Высота для летающего стрелка. Летающий таранщик автоматически летит прямо к герою.")]
        public float flightHeight = 3;

        private readonly RaycastHit2D[] hits = new RaycastHit2D[12];
        private Rigidbody2D body;
        private CapsuleCollider2D shape;
        private Health health;
        private EnemyAttack2D attack;
        private PlayerController2D target;
        private ContactFilter2D terrainFilter;
        private int facing = -1;
        public int FacingDirection => facing;
        public PlayerController2D Target => target;
        public bool HasDetectedTarget { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<CapsuleCollider2D>();
            health = GetComponent<Health>();
            attack = GetComponent<EnemyAttack2D>();
            terrainFilter = new ContactFilter2D { useTriggers = false };
            target = FindFirstObjectByType<PlayerController2D>();
        }

        private void FixedUpdate()
        {
            if (health.IsDead) { body.linearVelocity = Vector2.zero; return; }
            if (target == null) target = FindFirstObjectByType<PlayerController2D>();
            HasDetectedTarget = target != null && Vector2.Distance(body.position, target.transform.position) <= detectionRange;
            var flying = movementType == EnemyMovementType.Flying;
            body.gravityScale = flying ? 0 : 3;
            if (HasDetectedTarget)
            {
                var goal = (Vector2)target.transform.position;
                if (flying && attack != null && attack.attackType == EnemyAttackType.Ranged) goal.y += Mathf.Max(0, flightHeight);
                var delta = goal - body.position;
                if (Mathf.Abs(delta.x) > 0.05f) facing = delta.x > 0 ? 1 : -1;
                var stopDistance = attack != null ? attack.PreferredStopDistance : 0;
                if (flying)
                {
                    if (attack != null && attack.attackType == EnemyAttackType.Ranged)
                    {
                        var desired = new Vector2(Mathf.Abs(delta.x) > stopDistance ? delta.x / Time.fixedDeltaTime : 0,
                            delta.y / Time.fixedDeltaTime);
                        body.linearVelocity = Vector2.ClampMagnitude(desired, Mathf.Max(0, moveSpeed));
                    }
                    else
                        body.linearVelocity = delta.magnitude > 0.05f ? delta.normalized * Mathf.Max(0, moveSpeed) : Vector2.zero;
                    return;
                }
                var canMove = Mathf.Abs(delta.x) > stopDistance && GroundAhead() && !WallAhead();
                body.linearVelocity = new Vector2(canMove ? facing * Mathf.Max(0, moveSpeed) : 0, body.linearVelocity.y);
            }
            else if (flying)
                body.linearVelocity = Vector2.zero;
            else
            {
                if (!GroundAhead() || WallAhead()) facing *= -1;
                body.linearVelocity = new Vector2(patrolWhenIdle ? facing * Mathf.Max(0, moveSpeed) : 0, body.linearVelocity.y);
            }
        }

        private bool GroundAhead()
        {
            var bounds = shape.bounds;
            var origin = new Vector2(bounds.center.x + facing * (bounds.extents.x + 0.12f), bounds.min.y + 0.12f);
            var count = Physics2D.Raycast(origin, Vector2.down, terrainFilter, hits, 0.5f);
            for (var i = 0; i < count; i++)
                if (IsTerrain(hits[i].collider) && hits[i].normal.y > 0.65f) return true;
            return false;
        }

        private bool WallAhead()
        {
            var count = shape.Cast(Vector2.right * facing, terrainFilter, hits, 0.09f);
            for (var i = 0; i < count; i++)
                if (IsTerrain(hits[i].collider) && Mathf.Abs(hits[i].normal.x) > 0.65f) return true;
            return false;
        }

        private bool IsTerrain(Collider2D other)
        {
            return other != null && other != shape && (other.attachedRigidbody == null || other.attachedRigidbody.bodyType != RigidbodyType2D.Dynamic);
        }
    }
}
