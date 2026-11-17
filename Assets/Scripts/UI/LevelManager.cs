using System.Collections.Generic;
using ContraLike.UI;
using ContraLike.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ContraLike.Campaign
{
    [ExecuteAlways, DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    [AddComponentMenu("Contra-Like/Карта уровней")]
    public sealed class LevelManager : MonoBehaviour
    {
        [Header("Цвета кнопок")]
        [InspectorName("Доступный уровень"), Tooltip("Цвет рамки и номера доступного уровня.")]
        public Color availableColor = new Color(1f, 0.72f, 0.28f);
        [InspectorName("Пройденный уровень"), Tooltip("Цвет уровня после победы.")]
        public Color completedColor = new Color(0.36f, 0.87f, 0.65f);
        [InspectorName("Закрытый уровень"), Tooltip("Цвет ещё недоступного уровня.")]
        public Color lockedColor = new Color(0.35f, 0.38f, 0.43f);

        [Header("Пунктирные связи")]
        [InspectorName("Цвет линии"), Tooltip("Цвет соединений между уровнями.")]
        public Color connectionColor = new Color(0.36f, 0.39f, 0.44f);
        [Min(1), InspectorName("Толщина линии"), Tooltip("Толщина пунктирной линии в единицах Canvas.")]
        public float lineThickness = 4;
        [Min(2), InspectorName("Длина штриха"), Tooltip("Длина одного штриха пунктирной линии.")]
        public float dashLength = 14;
        [Min(1), InspectorName("Промежуток"), Tooltip("Расстояние между штрихами.")]
        public float gapLength = 12;

        private readonly List<LevelNode> levels = new List<LevelNode>();
        private readonly List<LevelNode> foundLevels = new List<LevelNode>();
        private LevelConnectionsGraphic connections;
        private LevelDetailsView details;
        private int lastSignature;
        private bool needsRefresh = true;
        private bool subscribed;
        private bool isLoading;

        public IReadOnlyList<LevelNode> Levels => levels;
        public bool IsLoading => isLoading;

        private void OnEnable()
        {
            needsRefresh = true;
            isLoading = false;
            if (Application.IsPlaying(gameObject))
            {
                LevelProgress.Current.Changed += RequestRefresh;
                subscribed = true;
            }
        }

        private void OnDisable()
        {
            if (subscribed) LevelProgress.Current.Changed -= RequestRefresh;
            subscribed = false;
        }

        private void OnValidate() => needsRefresh = true;
        private void OnTransformChildrenChanged() => needsRefresh = true;
        private void OnRectTransformDimensionsChange() => needsRefresh = true;

        private void Update()
        {
            CollectLevels();
            var signature = CalculateSignature();
            if (needsRefresh || signature != lastSignature) RefreshMap();
        }

        public void RefreshMap()
        {
            CollectLevels();
#if UNITY_EDITOR
            if (!Application.IsPlaying(gameObject) && !UnityEditor.EditorUtility.IsPersistent(this))
            {
                var ids = new HashSet<string>();
                foreach (var level in levels) level.EnsureUniqueEditorId(ids);
            }
#endif
            for (var i = 0; i < levels.Count; i++)
                levels[i].ApplyState(this, i, IsAvailable(levels[i]), IsCompleted(levels[i]));
            if (connections == null) connections = GetComponentInChildren<LevelConnectionsGraphic>(true);
            if (connections != null) connections.SetVerticesDirty();
            lastSignature = CalculateSignature();
            needsRefresh = false;
        }

        public bool IsAvailable(LevelNode level)
        {
            return LevelGraph.IsAvailable(level, levels,
                id => Application.IsPlaying(gameObject) && LevelProgress.Current.IsCompleted(id));
        }

        public bool IsCompleted(LevelNode level) => Application.IsPlaying(gameObject) && LevelProgress.Current.IsCompleted(level.Id);

        public bool IsDoubleJumpUnlocked
        {
            get
            {
                foreach (var level in levels)
                    if (level.unlocksDoubleJump && IsCompleted(level)) return true;
                return false;
            }
        }

        internal void ShowDetails(LevelNode level)
        {
            if (isLoading || level == null || level.Manager != this) return;
            if (details == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null) details = canvas.GetComponentInChildren<LevelDetailsView>(true);
            }
            if (details != null) details.Show(level);
            else Debug.LogWarning("Добавьте префаб LevelDetailsWindow в Canvas карты уровней.", this);
        }

        internal void LoadLevel(LevelNode level)
        {
            if (isLoading || !IsAvailable(level)) return;
            isLoading = true;
            RefreshMap();
            LevelRun.Begin(level.Id, level.scenePath, IsDoubleJumpUnlocked && LevelProgress.Current.DoubleJumpEnabled);
            level.onSelected.Invoke();
            // A UI event can start an optional effect, but cannot unlock a locked node.
            if (GameManager.Instance != null)
            {
                if (!GameManager.Instance.LoadLevel(level.scenePath)) CancelLoad();
                return;
            }
            var operation = SceneManager.LoadSceneAsync(level.scenePath);
            if (operation == null) CancelLoad();
        }

        private void CancelLoad()
        {
            LevelRun.Reset();
            isLoading = false;
            RefreshMap();
        }

        private void RequestRefresh() => needsRefresh = true;

        private void CollectLevels()
        {
            foundLevels.Clear();
            GetComponentsInChildren(true, foundLevels);
            levels.Clear();
            foreach (var level in foundLevels)
                if (level != null && level.isActiveAndEnabled && level.Manager == this) levels.Add(level);
        }

        private int CalculateSignature()
        {
            unchecked
            {
                var hash = levels.Count;
                hash = hash * 31 + availableColor.GetHashCode();
                hash = hash * 31 + completedColor.GetHashCode();
                hash = hash * 31 + lockedColor.GetHashCode();
                hash = hash * 31 + connectionColor.GetHashCode();
                hash = hash * 31 + lineThickness.GetHashCode();
                hash = hash * 31 + dashLength.GetHashCode();
                hash = hash * 31 + gapLength.GetHashCode();
                foreach (var node in levels)
                {
                    hash = hash * 31 + node.GetInstanceID();
                    hash = hash * 31 + node.Rect.localToWorldMatrix.GetHashCode();
                    hash = hash * 31 + node.Rect.rect.size.GetHashCode();
                    hash = hash * 31 + (node.title?.GetHashCode() ?? 0);
                    hash = hash * 31 + (node.scenePath?.GetHashCode() ?? 0);
                    hash = hash * 31 + node.availableFromStart.GetHashCode();
                    if (node.nextLevels == null) continue;
                    foreach (var next in node.nextLevels) hash = hash * 31 + (next == null ? 0 : next.GetInstanceID());
                }
                return hash;
            }
        }
    }
}
