using ContraLike.Player;
using UnityEngine;

namespace ContraLike.CameraControl
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    [AddComponentMenu("Contra-Like/Камера/Следование за героем")]
    public sealed class SideScrollCamera2D : MonoBehaviour
    {
        [InspectorName("Границы уровня по X"), Tooltip("X — левый край уровня, Y — правый. Камера не показывает пространство за этими краями.")]
        public Vector2 horizontalBounds = new Vector2(0, 72);
        [InspectorName("Высота камеры")]
        public float cameraHeight = 3.5f;
        [InspectorName("Подниматься за героем"), Tooltip("Поднимает камеру вместе с героем на высоких платформах. На арене действует её собственный центр.")]
        public bool followVertically;
        [Min(0), InspectorName("Смещение вперёд"), Tooltip("Насколько дальше героя камера смотрит по направлению его движения.")]
        public float lookAhead = 2;
        [Min(0.1f), InspectorName("Скорость следования")]
        public float followSpeed = 6;
        [Min(0.05f), InspectorName("Время перехода к арене"), Tooltip("За сколько секунд камера плавно переместится к центру арены после входа.")]
        public float arenaTransitionDuration = 0.35f;

        private Camera view;
        private PlayerController2D player;
        private Rigidbody2D playerBody;
        private Object arenaOwner;
        private Transform arenaCenter;
        private Vector2 arenaTransitionStart;
        private float arenaTransitionProgress;
        public bool IsArenaLocked => arenaOwner != null && arenaCenter != null;

        private void Awake() => view = GetComponent<Camera>();
        private void Start() => FindPlayer();
        private void OnDisable()
        {
            if (player != null) player.Respawned -= Snap;
            player = null;
            playerBody = null;
            arenaOwner = null;
            arenaCenter = null;
        }

        private void LateUpdate()
        {
            if (IsArenaLocked)
            {
                arenaTransitionProgress = Mathf.Clamp01(arenaTransitionProgress + Time.deltaTime / Mathf.Max(0.05f, arenaTransitionDuration));
                var blend = Mathf.SmoothStep(0, 1, arenaTransitionProgress);
                var position = Vector2.Lerp(arenaTransitionStart, arenaCenter.position, blend);
                transform.position = new Vector3(position.x, position.y, transform.position.z);
                return;
            }
            if (player == null) FindPlayer();
            if (player == null) return;
            transform.position = Vector3.Lerp(transform.position, DesiredPosition(), 1 - Mathf.Exp(-Mathf.Max(0.1f, followSpeed) * Time.deltaTime));
        }

        private void FindPlayer()
        {
            player = FindFirstObjectByType<PlayerController2D>();
            if (player == null) return;
            playerBody = player.GetComponent<Rigidbody2D>();
            player.Respawned += Snap;
            if (!IsArenaLocked) Snap();
        }

        private Vector3 DesiredPosition()
        {
            var halfWidth = view.orthographicSize * view.aspect;
            var left = Mathf.Min(horizontalBounds.x, horizontalBounds.y);
            var right = Mathf.Max(horizontalBounds.x, horizontalBounds.y);
            var position = player.transform.position;
            var x = (playerBody != null ? playerBody.position.x : position.x) + player.FacingDirection * lookAhead;
            x = right - left < 2 * halfWidth ? (left + right) * 0.5f : Mathf.Clamp(x, left + halfWidth, right - halfWidth);
            var y = cameraHeight + (followVertically ? Mathf.Max(0, position.y) : 0);
            return new Vector3(x, y, transform.position.z);
        }

        private void Snap()
        {
            arenaOwner = null;
            arenaCenter = null;
            if (player != null) transform.position = DesiredPosition();
        }

        public bool TryLockToArena(Transform center, Object owner)
        {
            if (center == null || owner == null) return false;
            if (IsArenaLocked)
            {
                if (arenaOwner != owner) return false;
                if (arenaCenter == center) return true;
            }
            arenaOwner = owner;
            arenaCenter = center;
            arenaTransitionStart = transform.position;
            arenaTransitionProgress = 0;
            return true;
        }

        public bool IsLockedBy(Object owner) => IsArenaLocked && arenaOwner == owner;

        public void ReleaseArena(Object owner)
        {
            if (arenaOwner != owner) return;
            arenaOwner = null;
            arenaCenter = null;
        }
    }
}
