using ContraLike.Combat;
using UnityEngine;
using UnityEngine.Events;

namespace ContraLike.Enemies
{
    public enum EnemyAttackType
    {
        [InspectorName("Таранит при касании")] Contact,
        [InspectorName("Стреляет с расстояния")] Ranged
    }

    [DisallowMultipleComponent, RequireComponent(typeof(EnemyMovement2D), typeof(Health), typeof(AudioSource))]
    [AddComponentMenu("Contra-Like/Враг/Атака врага")]
    public sealed class EnemyAttack2D : MonoBehaviour
    {
        [InspectorName("Тип атаки")]
        public EnemyAttackType attackType;
        [Min(0), InspectorName("Урон при касании")]
        public float contactDamage = 10;
        [Min(0.05f), InspectorName("Пауза между ударами"), Tooltip("Секунды между ударами таранщика. Не наносит урон каждый физический кадр.")]
        public float contactInterval = 0.8f;
        [Min(0.1f), InspectorName("Дальность стрельбы")]
        public float attackRange = 12;
        [InspectorName("Оружие врага"), Tooltip("Профиль снаряда, урона, скорости, паузы и звука. Может использовать ту же систему оружия, что и герой.")]
        public WeaponSettings weapon;
        [Header("События")]
        [InspectorName("При атаке"), Tooltip("Дополнительная анимация, звук или эффект. Сама атака выполняется автоматически.")]
        public UnityEvent onAttacked = new UnityEvent();

        private readonly RaycastHit2D[] sightHits = new RaycastHit2D[16];
        private EnemyMovement2D movement;
        private Health health;
        private CapsuleCollider2D shape;
        private AudioSource audioSource;
        private Transform muzzle;
        private float nextAttackAt;
        public event System.Action Attacked;
        public float PreferredStopDistance => attackType == EnemyAttackType.Ranged ? Mathf.Max(0.1f, attackRange) * 0.65f : 0;

        private void Awake()
        {
            movement = GetComponent<EnemyMovement2D>();
            health = GetComponent<Health>();
            shape = GetComponent<CapsuleCollider2D>();
            audioSource = GetComponent<AudioSource>();
            muzzle = transform.Find("Muzzle");
        }

        private void FixedUpdate()
        {
            if (health.IsDead || !movement.HasDetectedTarget || movement.Target == null || Time.time < nextAttackAt) return;
            var target = movement.Target;
            var targetHealth = target.GetComponent<Health>();
            var targetCollider = target.GetComponent<Collider2D>();
            if (targetHealth == null || targetHealth.IsDead || targetCollider == null) return;
            if (attackType == EnemyAttackType.Contact)
            {
                if (shape.Distance(targetCollider).distance > 0.05f) return;
                nextAttackAt = Time.time + Mathf.Max(0.05f, contactInterval);
                targetHealth.TakeDamage(Mathf.Max(0, contactDamage));
                Attacked?.Invoke();
                onAttacked.Invoke();
                return;
            }
            if (weapon == null || weapon.projectile == null) return;
            var offset = muzzle != null ? muzzle.localPosition : new Vector3(0.7f, 1, -0.85f);
            offset.x = Mathf.Abs(offset.x) * movement.FacingDirection;
            var origin = transform.TransformPoint(offset);
            var delta = (Vector2)targetCollider.bounds.center - (Vector2)origin;
            if (delta.magnitude > attackRange || !HasLineOfSight(origin, delta, targetCollider)) return;
            weapon.SpawnProjectiles(transform, origin, delta);
            nextAttackAt = Time.time + Mathf.Max(0.02f, weapon.fireInterval);
            if (weapon.shotSound != null) audioSource.PlayOneShot(weapon.shotSound);
            Attacked?.Invoke();
            onAttacked.Invoke();
        }

        private bool HasLineOfSight(Vector2 origin, Vector2 delta, Collider2D target)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            var count = Physics2D.Raycast(origin, delta.normalized, filter, sightHits, delta.magnitude);
            var nearest = float.PositiveInfinity;
            Collider2D first = null;
            for (var i = 0; i < count; i++)
            {
                var other = sightHits[i].collider;
                if (other == null || other.transform.IsChildOf(transform) || other.GetComponentInParent<Projectile2D>() != null) continue;
                if (sightHits[i].distance >= nearest) continue;
                nearest = sightHits[i].distance;
                first = other;
            }
            return first != null && (first == target || first.transform.IsChildOf(target.transform));
        }
    }
}
