using ContraLike.CameraControl;
using ContraLike.Campaign;
using ContraLike.Player;
using UnityEngine;
using UnityEngine.Events;

namespace ContraLike.Arenas
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    [AddComponentMenu("Contra-Like/Арены/Зона боя и камера")]
    public sealed class ArenaZone : MonoBehaviour
    {
        [InspectorName("Размер зоны боя"), Tooltip("Ширина и высота арены. Положение этого объекта — центр зафиксированной камеры. Зелёный Box Collider 2D задаёт отдельную область входа.")]
        public Vector2 arenaSize = new Vector2(18, 10.5f);
        [InspectorName("Закрывать выходы"), Tooltip("На время боя создаёт преграды слева и справа по границам зоны.")]
        public bool closeExits = true;
        [Header("События")]
        [InspectorName("Бой начался")] public UnityEvent onStarted = new UnityEvent();
        [InspectorName("Арена пройдена")] public UnityEvent onCleared = new UnityEvent();
        [InspectorName("Арена сброшена")] public UnityEvent onReset = new UnityEvent();

        private readonly Collider2D[] overlaps = new Collider2D[16];
        private BoxCollider2D entry;
        private SideScrollCamera2D cameraControl;
        private ArenaEncounter encounter;
        private PlayerController2D hero;
        private GameObject gates;
        private bool waitForExit;
        private bool warned;
        public bool IsActive { get; private set; }
        public bool IsCleared { get; private set; }
        internal string AttemptKey { get; private set; }

        private void Reset()
        {
            var trigger = GetComponent<BoxCollider2D>(); trigger.isTrigger = true;
            trigger.size = new Vector2(1.5f, 8); trigger.offset = new Vector2(-7.5f, 0);
        }
        private void OnValidate()
        {
            arenaSize = Vector2.Max(Vector2.one, arenaSize);
            GetComponent<BoxCollider2D>().isTrigger = true;
        }
        private void Awake()
        {
            entry = GetComponent<BoxCollider2D>();
            entry.isTrigger = true;
            AttemptKey = "arena:" + LevelRun.ObjectKey(transform);
        }
        private void Start()
        {
            if (GetComponent<EnemyWaveArena>() != null)
                IsCleared = LevelRun.WasConsumed(gameObject.scene.path, AttemptKey);
        }
        private void OnEnable() => waitForExit = false;
        private void Update()
        {
            if (waitForExit && !PlayerInsideEntry()) waitForExit = false;
            if (IsActive && (hero == null || cameraControl == null || !cameraControl.isActiveAndEnabled || !cameraControl.IsLockedBy(this))) ResetArena();
        }
        private void OnTriggerEnter2D(Collider2D other) => TryEnter(other);
        private void OnTriggerStay2D(Collider2D other) => TryEnter(other);
        private void TryEnter(Collider2D other)
        {
            if (!isActiveAndEnabled || IsActive || IsCleared || waitForExit) return;
            var player = other.GetComponentInParent<PlayerController2D>();
            if (player == null) return;
            encounter = GetComponent<ArenaEncounter>();
            var error = encounter != null && encounter.isActiveAndEnabled ? encounter.ConfigurationError : "Добавьте BossArena или EnemyWaveArena.";
            if (!string.IsNullOrEmpty(error))
            {
                if (!warned) Debug.LogWarning("Арена «" + name + "»: " + error, this);
                warned = true; return;
            }
            cameraControl = Camera.main != null ? Camera.main.GetComponent<SideScrollCamera2D>() : null;
            if (cameraControl == null) cameraControl = FindFirstObjectByType<SideScrollCamera2D>();
            if (cameraControl == null || !cameraControl.isActiveAndEnabled || !cameraControl.TryLockToArena(transform, this)) return;
            hero = player; hero.Respawned += ResetArena;
            IsActive = true;
            if (closeExits) CreateGates();
            encounter.BeginEncounter(this);
            if (IsActive) onStarted.Invoke();
        }

        internal void CompleteEncounter()
        {
            if (!IsActive) return;
            IsActive = false; IsCleared = true;
            if (encounter is EnemyWaveArena) LevelRun.RecordReward(gameObject.scene.path, AttemptKey, 0);
            encounter.EndEncounter(false);
            ReleaseZone();
            onCleared.Invoke();
        }

        public void ResetArena()
        {
            var notify = IsActive || IsCleared;
            IsActive = false; IsCleared = false; waitForExit = true; warned = false;
            if (encounter != null) encounter.EndEncounter(true);
            ReleaseZone();
            if (notify) onReset.Invoke();
        }

        private bool PlayerInsideEntry()
        {
            if (entry == null || !entry.enabled) return false;
            var count = entry.Overlap(new ContactFilter2D { useTriggers = false }, overlaps);
            for (var i = 0; i < count; i++) if (overlaps[i] != null && overlaps[i].GetComponentInParent<PlayerController2D>() != null) return true;
            return false;
        }
        private void CreateGates()
        {
            gates = new GameObject("Closed Gates"); gates.transform.SetParent(transform, false);
            foreach (var side in new[] { -1, 1 })
            {
                var gate = new GameObject(side < 0 ? "Left Gate" : "Right Gate", typeof(BoxCollider2D)); gate.transform.SetParent(gates.transform, false);
                gate.transform.localPosition = new Vector3(side * (arenaSize.x * 0.5f + 0.175f), 0, 0);
                gate.GetComponent<BoxCollider2D>().size = new Vector2(0.35f, arenaSize.y + 2);
            }
        }
        private void ReleaseZone()
        {
            if (hero != null) hero.Respawned -= ResetArena;
            hero = null;
            if (cameraControl != null) cameraControl.ReleaseArena(this);
            if (gates != null) { gates.SetActive(false); Destroy(gates); gates = null; }
        }
        private void OnDisable()
        {
            IsActive = false;
            if (encounter != null) encounter.EndEncounter(true);
            ReleaseZone();
        }
        private void OnDrawGizmos()
        {
            var previous = Gizmos.matrix; Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = IsCleared ? Color.green : new Color(1, 0.72f, 0.28f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(arenaSize.x, arenaSize.y, 0));
            Gizmos.color = Color.cyan; Gizmos.DrawLine(Vector3.left * 0.4f, Vector3.right * 0.4f); Gizmos.DrawLine(Vector3.down * 0.4f, Vector3.up * 0.4f);
            var trigger = GetComponent<BoxCollider2D>();
            if (trigger != null) { Gizmos.color = new Color(0.2f, 1, 0.4f); Gizmos.DrawWireCube(trigger.offset, trigger.size); }
            if (closeExits)
            {
                Gizmos.color = new Color(1, 0.3f, 0.2f);
                foreach (var side in new[] { -1, 1 }) Gizmos.DrawLine(new Vector3(side * arenaSize.x * 0.5f, -arenaSize.y * 0.5f, 0), new Vector3(side * arenaSize.x * 0.5f, arenaSize.y * 0.5f, 0));
            }
            Gizmos.matrix = previous;
            var view = Camera.main;
            if (view != null && view.orthographic)
            {
                Gizmos.color = new Color(0.2f, 0.7f, 1, 0.6f); Gizmos.DrawWireCube(transform.position, new Vector3(view.orthographicSize * 2 * view.aspect, view.orthographicSize * 2, 0));
            }
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.45f, name + " — центр камеры");
#endif
        }
    }
}
