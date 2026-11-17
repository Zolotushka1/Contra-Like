using System.Collections.Generic;
using ContraLike.Campaign;
using UnityEngine;

namespace ContraLike.Combat
{
    [CreateAssetMenu(fileName = "Player Arsenal", menuName = "Contra-Like/Список оружия игрока")]
    public sealed class WeaponCatalog : ScriptableObject
    {
        [InspectorName("Открывать оружие за монеты"), Tooltip("В начале доступно только первое оружие. Снимите галочку, чтобы сразу открыть весь список.")]
        public bool requirePurchases = true;
        [InspectorName("Оружие по порядку"), Tooltip("Первое оружие выдаётся бесплатно. Остальные покупаются по порядку. Добавьте сюда созданные файлы настроек оружия.")]
        public List<WeaponSettings> weapons = new List<WeaponSettings>();

        public WeaponSettings StarterWeapon
        {
            get
            {
                foreach (var weapon in weapons)
                    if (IsUsable(weapon)) return weapon;
                return null;
            }
        }

        internal IEnumerable<WeaponSettings> UsableWeapons()
        {
            var shown = new HashSet<WeaponSettings>();
            foreach (var weapon in weapons)
                if (IsUsable(weapon) && shown.Add(weapon)) yield return weapon;
        }

        internal bool IsUnlocked(WeaponSettings weapon, LevelProgress progress)
        {
            return IsUsable(weapon) && (!requirePurchases || weapon == StarterWeapon || progress.IsWeaponPurchased(weapon.UnlockId));
        }

        internal WeaponSettings NextPurchase(LevelProgress progress)
        {
            if (!requirePurchases) return null;
            foreach (var weapon in UsableWeapons())
                if (!IsUnlocked(weapon, progress)) return weapon;
            return null;
        }

        internal bool TryPurchase(WeaponSettings weapon, LevelProgress progress)
        {
            return weapon != null && weapon == NextPurchase(progress) && progress.TryPurchaseWeapon(weapon.UnlockId, weapon.price);
        }

        private static bool IsUsable(WeaponSettings weapon) => weapon != null && weapon.projectile != null;
    }
}
