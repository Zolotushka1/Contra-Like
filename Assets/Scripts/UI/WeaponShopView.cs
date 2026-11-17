using System.Collections.Generic;
using ContraLike.Campaign;
using ContraLike.Combat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ContraLike.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Contra-Like/Интерфейс/Оружейная")]
    public sealed class WeaponShopView : MonoBehaviour
    {
        [SerializeField, HideInInspector] private WeaponCatalog catalog;
        [SerializeField, HideInInspector] private GameObject rowPrefab;

        private sealed class Row
        {
            public WeaponSettings weapon;
            public Button button;
            public Text caption;
        }

        private readonly List<Row> rows = new List<Row>();
        private GameObject modal;
        private Transform window;
        private Button closeButton;
        private ScrollRect scroll;
        private Text balanceLabel;
        private Text pendingLabel;
        private LevelManager map;
        private CanvasGroup mapGroup;
        private LevelProgress progress;
        private GameObject previousSelection;

        private bool IsOpen => modal != null && modal.activeSelf;

        private void Awake()
        {
            modal = transform.Find("Modal").gameObject;
            window = modal.transform.Find("Window");
            closeButton = window.Find("Close").GetComponent<Button>();
            scroll = window.Find("Weapons").GetComponent<ScrollRect>();
            balanceLabel = window.Find("Balance").GetComponent<Text>();
            pendingLabel = window.Find("Pending").GetComponent<Text>();
            map = GetComponentInParent<Canvas>().GetComponentInChildren<LevelManager>();
            mapGroup = map.GetComponent<CanvasGroup>();
        }

        public void Open()
        {
            if (IsOpen || catalog == null || rowPrefab == null || map.IsLoading || !mapGroup.interactable) return;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            mapGroup.interactable = false;
            modal.SetActive(true);
            progress = LevelProgress.Current;
            progress.Changed += Refresh;
            BuildRows();
            Refresh();
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1;
            FocusClose();
        }

        public void Close()
        {
            if (!IsOpen) return;
            progress.Changed -= Refresh;
            modal.SetActive(false);
            mapGroup.interactable = true;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection);
            previousSelection = null;
        }

        private void OnDisable() => Close();

        private void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        private void LateUpdate()
        {
            if (!IsOpen || EventSystem.current == null) return;
            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null || !selected.transform.IsChildOf(window)) FocusClose();
        }

        private void FocusClose()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }

        private void BuildRows()
        {
            foreach (var row in rows)
            {
                var root = row.button.transform.parent.gameObject;
                root.SetActive(false);
                Destroy(root);
            }
            rows.Clear();

            foreach (var weapon in catalog.UsableWeapons())
            {
                var entry = Instantiate(rowPrefab, scroll.content).transform;
                var title = entry.Find("Title").GetComponent<Text>();
                var slot = rows.Count + 1;
                title.text = (slot <= 9 ? slot + " · " : "") + weapon.title;
                var specs = entry.Find("Specs").GetComponent<Text>();
                var mode = weapon.automatic ? "автоматическое" : "одиночное";
                var damage = weapon.projectilesPerShot > 1 ? weapon.damage.ToString("0.#") + " × " + weapon.projectilesPerShot : weapon.damage.ToString("0.#");
                specs.text = "Урон " + damage + "  ·  " + mode;
                var button = entry.Find("Purchase").GetComponent<Button>();
                button.onClick.AddListener(() => catalog.TryPurchase(weapon, progress));
                rows.Add(new Row { weapon = weapon, button = button, caption = button.GetComponentInChildren<Text>() });
            }
        }

        private void Refresh()
        {
            var available = progress.SpendableCoins;
            balanceLabel.text = "Монеты для покупки: " + available;
            var pending = progress.Coins - available;
            pendingLabel.text = pending > 0 ? "До победы в незавершённых попытках: " + pending : "";
            var next = catalog.NextPurchase(progress);
            foreach (var row in rows)
            {
                var unlocked = catalog.IsUnlocked(row.weapon, progress);
                row.button.interactable = !unlocked && row.weapon == next && !string.IsNullOrEmpty(row.weapon.UnlockId)
                    && row.weapon.price >= 0 && available >= row.weapon.price;
                if (unlocked)
                    row.caption.text = row.weapon == catalog.StarterWeapon ? "Стартовое" : catalog.requirePurchases ? "Куплено" : "Доступно";
                else
                    row.caption.text = row.weapon == next ? "Купить · " + row.weapon.price : "Сначала предыдущее";
            }
        }
    }
}
