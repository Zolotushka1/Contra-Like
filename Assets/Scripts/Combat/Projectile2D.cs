using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ContraLike.Combat
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    [AddComponentMenu("Contra-Like/Снаряд")]
    public sealed class Projectile2D : MonoBehaviour
    {
        [InspectorName("При попадании"), Tooltip("Необязательный звук или дополнительный эффект. Урон и взрыв выполняются автоматически.")]
        public UnityEvent onImpact = new UnityEvent();

        private readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        private readonly HashSet<Health> damaged = new HashSet<Health>();
        private Rigidbody2D body;
        private BoxCollider2D shape;
        private WeaponSettings settings;
        private Transform owner;
        private int ownerInstanceId;
        internal int OwnerInstanceId => ownerInstanceId;
        private CombatTeam ownerTeam;
        private Vector2 direction;
        private float expiresAt;
        private bool spent;
        public WeaponSettings Settings => settings;

        public void Initialize(WeaponSettings weapon, Transform shotOwner, Vector2 shotDirection)
        {
            settings = weapon;
            owner = shotOwner;
            ownerInstanceId = owner != null ? owner.GetInstanceID() : 0;
            var ownerHealth = owner != null ? owner.GetComponentInParent<Health>() : null;
            ownerTeam = ownerHealth != null ? ownerHealth.team : CombatTeam.Neutral;
            direction = shotDirection.normalized;
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<BoxCollider2D>();
            body.gravityScale = 0;
            body.linearVelocity = direction * Mathf.Max(0.1f, weapon.projectileSpeed);
            expiresAt = Time.time + Mathf.Max(0.1f, weapon.projectileLifetime);
            if (owner != null)
                foreach (var ownerCollider in owner.GetComponentsInChildren<Collider2D>())
                    Physics2D.IgnoreCollision(shape, ownerCollider);
        }

        private void FixedUpdate()
        {
            if (spent || settings == null) return;
            if (Time.time >= expiresAt) { Destroy(gameObject); return; }
            // A swept shape catches thin walls even when a fast trigger would skip them.
            var filter = new ContactFilter2D { useTriggers = true };
            var distance = body.linearVelocity.magnitude * Time.fixedDeltaTime;
            var count = shape.Cast(direction, filter, hits, distance);
            var nearest = float.PositiveInfinity;
            Collider2D target = null;
            var point = Vector2.zero;
            for (var i = 0; i < count; i++)
                if (CanHit(hits[i].collider) && hits[i].distance < nearest)
                {
                    nearest = hits[i].distance;
                    target = hits[i].collider;
                    point = hits[i].point;
                }
            if (target != null) Impact(target, point);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!spent && settings != null && CanHit(other)) Impact(other, other.ClosestPoint(transform.position));
        }

        private bool CanHit(Collider2D other)
        {
            if (other == null || other == shape || other.GetComponentInParent<Projectile2D>() != null) return false;
            if (owner != null && other.transform.IsChildOf(owner)) return false;
            if (IsFriendly(other.GetComponentInParent<Health>())) return false;
            return !other.isTrigger || other.GetComponentInParent<Health>() != null;
        }

        private void Impact(Collider2D directHit, Vector2 point)
        {
            if (spent) return;
            spent = true;
            body.simulated = false;
            Damage(directHit);
            if (settings.explosionRadius > 0)
            {
                foreach (var other in Physics2D.OverlapCircleAll(point, settings.explosionRadius)) Damage(other);
                if (settings.explosionEffect != null)
                {
                    var effect = Instantiate(settings.explosionEffect, new Vector3(point.x, point.y, -0.9f), Quaternion.identity);
                    effect.transform.localScale = Vector3.one * settings.explosionRadius;
                    effect.Play();
                    Destroy(effect.gameObject, 2);
                }
            }
            onImpact.Invoke();
            Destroy(gameObject);
        }

        private void Damage(Collider2D collider)
        {
            if (owner != null && collider.transform.IsChildOf(owner)) return;
            var health = collider.GetComponentInParent<Health>();
            if (health != null && !IsFriendly(health) && damaged.Add(health)) health.TakeDamage(settings.damage);
        }

        private bool IsFriendly(Health target)
        {
            return target != null && ownerTeam != CombatTeam.Neutral && target.team == ownerTeam;
        }
    }
}
