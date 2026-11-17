using ContraLike.Campaign;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ContraLike.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Contra-Like/Интерфейс/Сброс прогресса")]
    public sealed class ProgressResetView : MonoBehaviour
    {
        private GameObject modal;
        private GameObject cancelButton;
        private LevelManager map;
        private CanvasGroup mapGroup;
        private GameObject previousSelection;

        private void Awake()
        {
            modal = transform.Find("Modal").gameObject;
            cancelButton = modal.transform.Find("Window/Cancel").gameObject;
            map = GetComponentInParent<Canvas>().GetComponentInChildren<LevelManager>();
            mapGroup = map.GetComponent<CanvasGroup>();
        }

        public void Open()
        {
            if (modal.activeSelf || map.IsLoading || !mapGroup.interactable) return;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            mapGroup.interactable = false;
            modal.SetActive(true);
            FocusCancel();
        }

        public void Cancel()
        {
            if (!modal.activeSelf) return;
            modal.SetActive(false);
            mapGroup.interactable = true;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection);
            previousSelection = null;
        }

        public void Confirm()
        {
            if (!modal.activeSelf) return;
            LevelRun.Reset();
            LevelProgress.Current.ResetProgress();
            map.RefreshMap();
            Cancel();
        }

        private void Update()
        {
            if (modal.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Cancel();
        }

        private void LateUpdate()
        {
            if (!modal.activeSelf || EventSystem.current == null) return;
            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null || !selected.transform.IsChildOf(modal.transform)) FocusCancel();
        }

        private void OnDisable()
        {
            if (modal != null && modal.activeSelf) Cancel();
        }

        private void FocusCancel()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(cancelButton);
        }
    }
}
