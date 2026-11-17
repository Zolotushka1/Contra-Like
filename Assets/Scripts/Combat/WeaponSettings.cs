using UnityEngine;

namespace ContraLike.Combat
{
    [CreateAssetMenu(fileName = "Weapon", menuName = "Contra-Like/Настройки оружия")]
    public sealed class WeaponSettings : ScriptableObject
    {
        [InspectorName("Название")]
        public string title = "Оружие";
        [Min(0), InspectorName("Цена в оружейной"), Tooltip("Цена в монетах. Первое оружие в списке игрока выдаётся бесплатно.")]
        public int price;
        [Header("Выстрел")]
        [InspectorName("Снаряд"), Tooltip("Готовый префаб пули или ракеты.")]
        public Projectile2D projectile;
        [Min(0), InspectorName("Урон")]
        public float damage = 10;
        [Min(0.02f), InspectorName("Пауза между выстрелами"), Tooltip("Секунды между выстрелами. Меньше — выше скорострельность.")]
        public float fireInterval = 0.25f;
        [InspectorName("Стрелять при удержании"), Tooltip("Включите для автоматической винтовки. Выключите для одиночного выстрела на нажатие.")]
        public bool automatic;
        [Range(1, 12), InspectorName("Снарядов за выстрел"), Tooltip("Один для обычного оружия, несколько для дробовика. Урон применяется к каждому снаряду.")]
        public int projectilesPerShot = 1;
        [Range(0, 120), InspectorName("Разброс, градусы"), Tooltip("Полный угол веера. Используется, если за выстрел летит больше одного снаряда.")]
        public float spreadAngle = 30;
        [Min(0.1f), InspectorName("Скорость снаряда"), Tooltip("Единиц в секунду. Для РПГ используется медленная ракета.")]
        public float projectileSpeed = 24;
        [Min(0.1f), InspectorName("Время полёта"), Tooltip("Через сколько секунд снаряд исчезнет, если не встретит препятствие.")]
        public float projectileLifetime = 4;
        [Min(0), InspectorName("Радиус взрыва"), Tooltip("0 — обычная пуля. Больше 0 — урон по области вокруг попадания.")]
        public float explosionRadius;
        [InspectorName("Эффект взрыва"), Tooltip("Необязательный готовый эффект для ракеты.")]
        public ParticleSystem explosionEffect;
        [InspectorName("Звук выстрела")]
        public AudioClip shotSound;

        [SerializeField, HideInInspector] private string unlockId;
        public string UnlockId => unlockId;

        internal void SpawnProjectiles(Transform owner, Vector3 origin, Vector2 direction)
        {
            if (projectile == null) return;
            var count = Mathf.Clamp(projectilesPerShot, 1, 12);
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var spread = Mathf.Clamp(spreadAngle, 0, 120);
            for (var i = 0; i < count; i++)
            {
                var offset = count == 1 ? 0 : Mathf.Lerp(-spread * 0.5f, spread * 0.5f, (float)i / (count - 1));
                var rotation = Quaternion.Euler(0, 0, angle + offset);
                var shot = Instantiate(projectile, origin, rotation);
                shot.Initialize(this, owner, rotation * Vector3.right);
            }
        }
    }
}
