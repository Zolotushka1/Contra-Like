using ContraLike.Campaign;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ContraLike.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
    [AddComponentMenu("Contra-Like/Интерфейс/Окно уровня")]
    public sealed class LevelDetailsView : MonoBehaviour
    {
        private GameObject modal;
        private Transform window;
        private Text titleLabel;
        private Text descriptionLabel;
        private Text statusLabel;
        private RawImage preview;
        private AspectRatioFitter previewAspect;
        private ScrollRect descriptionScroll;
        private Button playButton;
        private Button closeButton;
        private Toggle doubleJumpToggle;
        private Text doubleJumpHint;
        private LevelManager owner;
        private CanvasGroup mapGroup;
        private bool mapWasInteractable;
        private GameObject previousSelection;

        public bool IsOpen => modal != null && modal.activeSelf;
        public LevelNode SelectedLevel { get; private set; }

        private void Awake()
        {
            modal = transform.Find("Modal").gameObject;
            window = modal.transform.Find("Window");
            titleLabel = window.Find("Title").GetComponent<Text>();
            descriptionLabel = window.Find("Description/Viewport/Content").GetComponent<Text>();
            statusLabel = window.Find("Status").GetComponent<Text>();
            preview = window.Find("Preview/Image").GetComponent<RawImage>();
            previewAspect = preview.GetComponent<AspectRatioFitter>();
            descriptionScroll = window.Find("Description").GetComponent<ScrollRect>();
            playButton = window.Find("Play Button").GetComponent<Button>();
            closeButton = window.Find("Close Button").GetComponent<Button>();
            doubleJumpToggle = window.Find("Double Jump")?.GetComponent<Toggle>();
            if (doubleJumpToggle != null)
                doubleJumpHint = doubleJumpToggle.transform.Find("Hint").GetComponent<Text>();
        }

        public void Show(LevelNode level)
        {
            if (level == null || level.Manager == null || level.Manager.IsLoading) return;
            if (IsOpen) Close();
            SelectedLevel = level;
            owner = level.Manager;
            mapGroup = owner.GetComponent<CanvasGroup>();
            if (mapGroup != null)
            {
                mapWasInteractable = mapGroup.interactable;
                mapGroup.interactable = false;
            }
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (previousSelection == null) previousSelection = level.gameObject;
            titleLabel.text = level.title;
            descriptionLabel.text = level.description ?? "";
            preview.texture = level.previewImage;
            preview.enabled = level.previewImage != null;
            previewAspect.aspectRatio = level.previewImage != null ? (float)level.previewImage.width / level.previewImage.height : 16f / 9;
            modal.SetActive(true);
            RefreshAvailability();
            Canvas.ForceUpdateCanvases();
            descriptionScroll.StopMovement();
            descriptionScroll.verticalNormalizedPosition = 1;
            FocusWindow();
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (SelectedLevel == null || owner == null) { Close(); return; }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { Close(); return; }
            RefreshAvailability();
        }

        private void LateUpdate()
        {
            if (!IsOpen || EventSystem.current == null) return;
            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null || !selected.transform.IsChildOf(window)) FocusWindow();
        }

        private bool CanPlay => SelectedLevel != null && owner != null && !owner.IsLoading && owner.IsAvailable(SelectedLevel)
            && !string.IsNullOrEmpty(SelectedLevel.scenePath) && Application.CanStreamedLevelBeLoaded(SelectedLevel.scenePath);

        private void RefreshAvailability()
        {
            playButton.interactable = CanPlay;
            var available = owner.IsAvailable(SelectedLevel);
            var ready = !string.IsNullOrEmpty(SelectedLevel.scenePath) && Application.CanStreamedLevelBeLoaded(SelectedLevel.scenePath);
            var text = !ready ? "УРОВЕНЬ ГОТОВИТСЯ" : !available ? "УРОВЕНЬ ЗАКРЫТ" : owner.IsCompleted(SelectedLevel) ? "ПРОЙДЕН" : "ДОСТУПЕН";
            if (statusLabel.text != text) statusLabel.text = text;
            statusLabel.color = available && ready ? owner.availableColor : owner.lockedColor;
            if (doubleJumpToggle != null)
            {
                var unlocked = owner.IsDoubleJumpUnlocked;
                doubleJumpToggle.interactable = unlocked;
                doubleJumpToggle.SetIsOnWithoutNotify(unlocked && LevelProgress.Current.DoubleJumpEnabled);
                var hint = unlocked ? "Нажмите Space ещё раз в воздухе" : "После прохождения уровня 1";
                if (doubleJumpHint.text != hint) doubleJumpHint.text = hint;
            }
        }

        public void SetDoubleJumpEnabled(bool enabled)
        {
            if (IsOpen && owner != null && owner.IsDoubleJumpUnlocked)
                LevelProgress.Current.SetDoubleJumpEnabled(enabled);
        }

        private void FocusWindow()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(playButton.interactable ? playButton.gameObject : closeButton.gameObject);
        }

        public void PlaySelectedLevel()
        {
            if (!CanPlay) return;
            SelectedLevel.OpenLevel();
            if (owner != null && owner.IsLoading) Close();
        }

        public void Close()
        {
            if (!IsOpen) return;
            modal.SetActive(false);
            RestoreMap();
            if (EventSystem.current != null)
            {
                var selectable = previousSelection != null ? previousSelection.GetComponent<Selectable>() : null;
                EventSystem.current.SetSelectedGameObject(selectable != null && selectable.IsActive() && selectable.IsInteractable() ? previousSelection : null);
            }
            previousSelection = null;
            SelectedLevel = null;
            owner = null;
        }

        private void RestoreMap()
        {
            if (mapGroup != null) mapGroup.interactable = mapWasInteractable;
            mapGroup = null;
        }

        private void OnDisable()
        {
            RestoreMap();
            if (modal != null) modal.SetActive(false);
            SelectedLevel = null;
            owner = null;
            previousSelection = null;
        }
    }
}
