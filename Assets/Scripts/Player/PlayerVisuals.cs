using UnityEngine;

namespace ContraLike.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerController2D))]
    [AddComponentMenu("Contra-Like/Герой/Анимация героя")]
    public sealed class PlayerVisuals : MonoBehaviour
    {
        [Min(0.1f), InspectorName("Скорость анимации"), Tooltip("Множитель скорости проигрывания анимаций. На физическую скорость героя не влияет.")]
        public float animationSpeed = 1;

        private PlayerController2D movement;
        private SpriteRenderer sprite;
        private Animator animator;
        private PlayerShooter shooter;
        private int aimUpLayer = -1;
        private int diagonalLayer = -1;
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Grounded = Animator.StringToHash("Grounded");

        private void Awake()
        {
            movement = GetComponent<PlayerController2D>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponentInChildren<Animator>();
            shooter = GetComponent<PlayerShooter>();
            if (animator != null)
            {
                aimUpLayer = animator.GetLayerIndex("Aim Up");
                diagonalLayer = animator.GetLayerIndex("Aim Diagonal Up");
            }
        }

        private void LateUpdate()
        {
            if (sprite != null) sprite.flipX = movement.FacingDirection < 0;
            if (animator == null) return;
            animator.speed = Mathf.Max(0.1f, animationSpeed);
            animator.SetFloat(Speed, Mathf.Abs(movement.HorizontalSpeed));
            animator.SetBool(Grounded, movement.IsGrounded);
            var diagonal = shooter != null && shooter.IsAimingDiagonallyUp;
            if (aimUpLayer >= 0) animator.SetLayerWeight(aimUpLayer, shooter != null && shooter.IsAimingUp && !diagonal ? 1 : 0);
            if (diagonalLayer >= 0) animator.SetLayerWeight(diagonalLayer, diagonal ? 1 : 0);
        }
    }
}
