using System.Linq;
using ContraLike.Campaign;
using ContraLike.Combat;
using ContraLike.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace ContraLike.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerController2D), typeof(AudioSource))]
    [AddComponentMenu("Contra-Like/Герой/Стрельба героя")]
    public sealed class PlayerShooter : MonoBehaviour
    {
        [Header("Оружие")]
        [SerializeField, HideInInspector] private WeaponCatalog catalog;
        [InspectorName("Разрешить смену оружия"), Tooltip("Клавиши 1–9 выбирают оружие по номеру. Q перебирает все доступные виды.")]
        public bool allowWeaponSwitch = true;
        [Header("События")]
        [InspectorName("При выстреле")]
        public UnityEvent onFired = new UnityEvent();

        private PlayerController2D movement;
        private AudioSource audioSource;
        private Transform muzzle;
        private Transform upwardMuzzle;
        private Transform diagonalMuzzle;
        private static readonly Key[] WeaponKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
        };
        private float nextFireAt;

        public WeaponSettings CurrentWeapon { get; private set; }
        public Vector2 AimDirection { get; private set; } = Vector2.right;
        public bool IsAimingUp => AimDirection.y > 0;
        public bool IsAimingDiagonallyUp => IsAimingUp && AimDirection.x != 0;

        private void Awake()
        {
            movement = GetComponent<PlayerController2D>();
            audioSource = GetComponent<AudioSource>();
            muzzle = transform.Find("Muzzle");
            upwardMuzzle = transform.Find("Muzzle Up");
            diagonalMuzzle = transform.Find("Muzzle Diagonal Up");
            CurrentWeapon = catalog != null ? catalog.StarterWeapon : null;
        }

        private void Update()
        {
            if (!GameManager.GameplayInputAllowed) return;
            var keyboard = Keyboard.current;
            ReadAimDirection(keyboard);
            if (allowWeaponSwitch && keyboard != null && catalog != null) ReadWeaponSelection(keyboard);
            var weapon = CurrentWeapon;
            if (weapon == null) return;
            var mouse = Mouse.current;
            var held = keyboard != null && keyboard.jKey.isPressed || mouse != null && mouse.leftButton.isPressed;
            var pressed = keyboard != null && keyboard.jKey.wasPressedThisFrame || mouse != null && mouse.leftButton.wasPressedThisFrame;
            if (weapon.automatic ? held : pressed) Fire();
        }

        private void ReadWeaponSelection(Keyboard keyboard)
        {
            for (var i = 0; i < WeaponKeys.Length; i++)
            {
                if (!keyboard[WeaponKeys[i]].wasPressedThisFrame) continue;
                var requested = catalog.UsableWeapons().ElementAtOrDefault(i);
                if (catalog.IsUnlocked(requested, LevelProgress.Current)) CurrentWeapon = requested;
                return;
            }
            if (!keyboard.qKey.wasPressedThisFrame) return;
            var available = catalog.UsableWeapons().Where(w => catalog.IsUnlocked(w, LevelProgress.Current)).ToArray();
            if (available.Length == 0) return;
            var index = System.Array.IndexOf(available, CurrentWeapon);
            CurrentWeapon = available[(index + 1) % available.Length];
        }

        public void Fire()
        {
            var weapon = CurrentWeapon;
            if (!GameManager.GameplayInputAllowed || weapon == null || weapon.projectile == null || Time.time < nextFireAt) return;
            var keyboard = Keyboard.current;
            ReadAimDirection(keyboard);
            nextFireAt = Time.time + Mathf.Max(0.02f, weapon.fireInterval);
            var direction = AimDirection;
            var offset = IsAimingDiagonallyUp ? (diagonalMuzzle != null ? diagonalMuzzle.localPosition : new Vector3(0.62f, 1.8f, -0.85f))
                : IsAimingUp ? (upwardMuzzle != null ? upwardMuzzle.localPosition : new Vector3(-0.2f, 2.1f, -0.85f))
                : (muzzle != null ? muzzle.localPosition : new Vector3(0.7f, 1.1f, -0.8f));
            var facing = direction.x != 0 ? Mathf.Sign(direction.x) : movement.FacingDirection;
            offset.x = IsAimingUp ? offset.x * facing : Mathf.Abs(offset.x) * facing;
            weapon.SpawnProjectiles(transform, transform.TransformPoint(offset), direction);
            if (weapon.shotSound != null) audioSource.PlayOneShot(weapon.shotSound);
            onFired.Invoke();
        }

        private void ReadAimDirection(Keyboard keyboard)
        {
            var horizontal = 0;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal--;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal++;
            }
            var up = keyboard != null && (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed);
            AimDirection = up ? new Vector2(horizontal, 1).normalized
                : new Vector2(horizontal != 0 ? horizontal : movement.FacingDirection, 0);
        }

        private void OnDrawGizmosSelected()
        {
            var point = transform.Find("Muzzle Up");
            Gizmos.color = Color.yellow;
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, 0.06f);
                Gizmos.DrawLine(point.position, point.position + Vector3.up * 0.5f);
            }
            point = transform.Find("Muzzle Diagonal Up");
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, 0.06f);
                Gizmos.DrawLine(point.position, point.position + new Vector3(1, 1, 0).normalized * 0.5f);
            }
        }
    }
}
