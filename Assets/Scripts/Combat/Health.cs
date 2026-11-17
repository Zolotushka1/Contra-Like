using System;
using UnityEngine;
using UnityEngine.Events;

namespace ContraLike.Combat
{
    public enum CombatTeam
    {
        [InspectorName("Нейтральный объект")] Neutral,
        [InspectorName("Герой")] Player,
        [InspectorName("Враг")] Enemy
    }

    [DisallowMultipleComponent, AddComponentMenu("Contra-Like/Здоровье")]
    public sealed class Health : MonoBehaviour
    {
        [Min(1), InspectorName("Здоровье"), Tooltip("Количество здоровья при появлении объекта.")]
        public float maxHealth = 100;
        [InspectorName("Команда"), Tooltip("Снаряды не ранят персонажей своей команды. Нейтральные мишени получают урон от всех.")]
        public CombatTeam team;
        [Min(0), InspectorName("Неуязвимость после урона"), Tooltip("Секунды защиты от повторного урона. Для врагов обычно 0, для героя — небольшая пауза.")]
        public float invulnerabilityDuration;
        [InspectorName("Уничтожить после смерти")]
        public bool destroyOnDeath = true;
        [Header("События")]
        [InspectorName("При получении урона")]
        public UnityEvent onDamaged = new UnityEvent();
        [InspectorName("При смерти")]
        public UnityEvent onDied = new UnityEvent();

        private float currentHealth;
        private bool dead;
        private float damageAllowedAt;
        public float CurrentHealth => currentHealth;
        public bool IsDead => dead;
        public event Action<Health> Damaged;
        public event Action<Health> Died;

        private void Awake() => currentHealth = Mathf.Max(1, maxHealth);

        public void TakeDamage(float damage)
        {
            if (dead || damage <= 0 || Time.time < damageAllowedAt) return;
            currentHealth = Mathf.Max(0, currentHealth - damage);
            damageAllowedAt = Time.time + Mathf.Max(0, invulnerabilityDuration);
            var fatal = currentHealth <= 0;
            if (fatal) dead = true;
            Damaged?.Invoke(this);
            onDamaged.Invoke();
            if (!fatal) return;
            Died?.Invoke(this);
            onDied.Invoke();
            if (destroyOnDeath) Destroy(gameObject);
        }

        public void RestoreHealth()
        {
            currentHealth = Mathf.Max(1, maxHealth);
            dead = false;
            damageAllowedAt = Time.time + Mathf.Max(0, invulnerabilityDuration);
        }
    }
}
