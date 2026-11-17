using System;
using System.Collections.Generic;
using ContraLike.Campaign;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ContraLike.Campaign
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Image), typeof(Button))]
    [AddComponentMenu("Contra-Like/Уровень на карте")]
    public sealed class LevelNode : MonoBehaviour
    {
        [Header("Уровень")]
        [InspectorName("Название")]
        [Tooltip("Название, которое игрок увидит под кнопкой.")]
        public string title = "Уровень";

        [SceneDropdown, InspectorName("Сцена для загрузки")]
        [Tooltip("Сцена уровня из списка сборки. Добавьте новую сцену в Build Profiles, чтобы она появилась здесь.")]
        public string scenePath;

        [Header("Окно уровня")]
        [InspectorName("Картинка параллакса"), Tooltip("Перетащите PNG с фоном уровня. Картинка появится в окне перед запуском.")]
        public Texture2D previewImage;
        [TextArea(3, 7), InspectorName("Описание"), Tooltip("Текст в окне выбора уровня. Длинное описание можно прокручивать.")]
        public string description;

        [Header("Открытие уровней")]
        [InspectorName("Открыт с начала игры")]
        [Tooltip("Этот уровень доступен без прохождения предыдущих. Обычно включено у первого уровня.")]
        public bool availableFromStart;

        [InspectorName("Следующие уровни")]
        [Tooltip("После победы откроются ВСЕ уровни из этого списка. Для развилки добавьте несколько кнопок.")]
        public List<LevelNode> nextLevels = new List<LevelNode>();
        [InspectorName("Открывает двойной прыжок")]
        [Tooltip("После победы двойной прыжок станет доступен в окне запуска уровней.")]
        public bool unlocksDoubleJump;

        [Header("События")]
        [InspectorName("При запуске уровня")]
        [Tooltip("Дополнительный звук или эффект при нажатии «Играть», перед загрузкой сцены.")]
        public UnityEvent onSelected = new UnityEvent();

        [InspectorName("При открытии уровня")]
        [Tooltip("Дополнительный эффект, если закрытый уровень стал доступен, пока карта открыта.")]
        public UnityEvent onUnlocked = new UnityEvent();

        [SerializeField, HideInInspector] private string levelId;
        private Button button;
        private Image face;
        private Outline outline;
        private Text numberLabel;
        private Text titleLabel;
        private Text statusLabel;
        private bool hasState;
        private bool wasAvailable;

        public string Id => levelId;
        public RectTransform Rect => (RectTransform)transform;
        public LevelManager Manager => GetComponentInParent<LevelManager>();

        public void ShowDetails()
        {
            var manager = Manager;
            if (!isActiveAndEnabled || manager == null || manager.IsLoading) return;
            manager.ShowDetails(this);
        }

        public void OpenLevel()
        {
            var manager = Manager;
            if (manager == null || !manager.IsAvailable(this) || manager.IsLoading) return;
            if (string.IsNullOrWhiteSpace(scenePath) || !Application.CanStreamedLevelBeLoaded(scenePath))
            {
                Debug.LogWarning("Выберите включённую в сборку сцену для уровня «" + title + "».", this);
                return;
            }
            manager.LoadLevel(this);
        }

        internal void ApplyState(LevelManager manager, int index, bool available, bool completed)
        {
            CacheView();
            if (button == null || face == null) return;
            button.interactable = !manager.IsLoading;
            var accent = completed ? manager.completedColor : available ? manager.availableColor : manager.lockedColor;
            var fill = Color.Lerp(Color.black, accent, available ? 0.15f : 0.09f);
            fill.a = 1;
            if (face.color != fill) face.color = fill;
            if (outline != null && outline.effectColor != accent) outline.effectColor = accent;
            SetText(numberLabel, (index + 1).ToString("00"), accent);
            SetText(titleLabel, title, available ? new Color(0.94f, 0.94f, 0.94f) : manager.lockedColor);
            SetText(statusLabel, completed ? "ПРОЙДЕН" : available ? "ДОСТУПЕН" : "ЗАКРЫТ", accent);
            var notify = hasState && !wasAvailable && available && Application.IsPlaying(gameObject);
            hasState = true;
            wasAvailable = available;
            if (notify) onUnlocked.Invoke();
        }

        private void CacheView()
        {
            if (button == null) button = GetComponent<Button>();
            if (face == null) face = GetComponent<Image>();
            if (outline == null) outline = GetComponent<Outline>();
            if (numberLabel == null) numberLabel = transform.Find("Number")?.GetComponent<Text>();
            if (titleLabel == null) titleLabel = transform.Find("Title")?.GetComponent<Text>();
            if (statusLabel == null) statusLabel = transform.Find("Status")?.GetComponent<Text>();
        }

        private static void SetText(Text label, string text, Color color)
        {
            if (label == null) return;
            if (label.text != text) label.text = text;
            if (label.color != color) label.color = color;
        }

#if UNITY_EDITOR
        public bool EnsureUniqueEditorId(HashSet<string> usedIds)
        {
            if (!string.IsNullOrEmpty(levelId) && usedIds.Add(levelId)) return false;
            levelId = Guid.NewGuid().ToString("N");
            usedIds.Add(levelId);
            UnityEditor.EditorUtility.SetDirty(this);
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this))
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            return true;
        }
#endif
    }
}
