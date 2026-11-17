using ContraLike.Campaign;
using ContraLike.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace ContraLike.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(AudioSource))]
    [AddComponentMenu("Contra-Like/Герой/Движение и прыжок")]
    public sealed class PlayerController2D : MonoBehaviour
    {
        [Header("Движение")]
        [Min(0), InspectorName("Скорость бега"), Tooltip("Скорость движения влево и вправо, единиц в секунду.")]
        public float moveSpeed = 7;
        [Min(0.1f), InspectorName("Высота прыжка"), Tooltip("Примерная высота прыжка в единицах сцены.")]
        public float jumpHeight = 2.5f;
        [Min(0.1f), InspectorName("Сила притяжения"), Tooltip("Множитель обычной гравитации. Больше — более быстрый и резкий прыжок при той же высоте.")]
        public float gravityScale = 3;
        [InspectorName("Возврат при падении ниже"), Tooltip("Если герой упал ниже этой высоты Y, он вернётся в точку появления.")]
        public float respawnBelow = -8;

        [Header("Звук и события")]
        [InspectorName("Звук прыжка")]
        public AudioClip jumpSound;
        [InspectorName("При прыжке")]
        public UnityEvent onJumped = new UnityEvent();
        [InspectorName("При приземлении")]
        public UnityEvent onLanded = new UnityEvent();
        [InspectorName("При возвращении в точку появления")]
        public UnityEvent onRespawned = new UnityEvent();

        private const float GroundProbeDistance = 0.07f;
        private const float JumpBufferSeconds = 0.12f;
        private const float CoyoteSeconds = 0.10f;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        private Rigidbody2D body;
        private CapsuleCollider2D capsule;
        private AudioSource audioSource;
        private ContactFilter2D groundFilter;
        private Vector2 spawnPosition;
        private float horizontalInput;
        private float jumpRequestedAt = float.NegativeInfinity;
        private float lastGroundedAt = float.NegativeInfinity;
        private bool grounded;
        private bool doubleJumpEnabled;
        private bool airJumpUsed;
        private int facing = 1;

        public bool IsGrounded => grounded;
        public int FacingDirection => facing;
        public float HorizontalSpeed => body == null ? 0 : body.linearVelocity.x;
        public event System.Action Respawned;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            capsule = GetComponent<CapsuleCollider2D>();
            audioSource = GetComponent<AudioSource>();
            spawnPosition = body.position;
            groundFilter = new ContactFilter2D { useTriggers = false };
            doubleJumpEnabled = LevelRun.CanDoubleJump(gameObject.scene.path);
        }

        private void Update()
        {
            if (!GameManager.GameplayInputAllowed)
            {
                horizontalInput = 0;
                jumpRequestedAt = float.NegativeInfinity;
                return;
            }
            var keyboard = Keyboard.current;
            horizontalInput = 0;
            if (keyboard == null) return;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontalInput -= 1;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontalInput += 1;
            if (horizontalInput != 0) facing = horizontalInput > 0 ? 1 : -1;
            if (keyboard.spaceKey.wasPressedThisFrame) jumpRequestedAt = Time.time;
        }

        private void FixedUpdate()
        {
            body.gravityScale = Mathf.Max(0.1f, gravityScale);
            var onGround = ProbeGround() && body.linearVelocity.y <= 0.1f;
            if (onGround)
            {
                lastGroundedAt = Time.time;
                airJumpUsed = false;
            }
            if (onGround && !grounded) onLanded.Invoke();
            grounded = onGround;
            body.linearVelocity = new Vector2(horizontalInput * Mathf.Max(0, moveSpeed), body.linearVelocity.y);
            if (Time.time - jumpRequestedAt <= JumpBufferSeconds)
            {
                if (Time.time - lastGroundedAt <= CoyoteSeconds) Jump();
                else if (doubleJumpEnabled && !airJumpUsed)
                {
                    airJumpUsed = true;
                    Jump();
                }
            }
            if (body.position.y < respawnBelow)
                RestartOrRespawn();
        }

        internal void RestartOrRespawn()
        {
            if (GameManager.Instance?.State == GameState.Loading) return;
            if (LevelRun.Tracks(gameObject.scene.path) && GameManager.Instance != null && GameManager.Instance.TryRestartLevel()) return;
            Respawn();
        }

        private void Jump()
        {
            var gravity = Mathf.Abs(Physics2D.gravity.y * body.gravityScale);
            var jumpSpeed = Mathf.Sqrt(2 * gravity * Mathf.Max(0.1f, jumpHeight));
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpSpeed);
            jumpRequestedAt = lastGroundedAt = float.NegativeInfinity;
            grounded = false;
            if (jumpSound != null) audioSource.PlayOneShot(jumpSound);
            onJumped.Invoke();
        }

        public void Respawn()
        {
            body.position = spawnPosition;
            body.linearVelocity = Vector2.zero;
            jumpRequestedAt = lastGroundedAt = float.NegativeInfinity;
            grounded = false;
            airJumpUsed = false;
            Respawned?.Invoke();
            onRespawned.Invoke();
        }

        private bool ProbeGround()
        {
            var count = capsule.Cast(Vector2.down, groundFilter, groundHits, GroundProbeDistance);
            for (var i = 0; i < count; i++)
                if (groundHits[i].normal.y > 0.65f) return true;
            return false;
        }
    }
}
