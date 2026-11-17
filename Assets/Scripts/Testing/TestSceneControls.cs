using ContraLike.Campaign;
using ContraLike.Player;
using ContraLike.Combat;
using ContraLike.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ContraLike.Testing
{
    [AddComponentMenu("Contra-Like/Тестовая сцена/Управление тестом")]
    public sealed class TestSceneControls : MonoBehaviour
    {
        [SceneDropdown, InspectorName("Сцена главного меню")]
        public string menuScene = "Assets/Scenes/Main Menu.unity";
        private PlayerShooter shooter;
        private Text weaponLabel;
        private Text healthLabel;
        private Health playerHealth;

        private void Start()
        {
            shooter = FindFirstObjectByType<PlayerShooter>();
            var label = GameObject.Find("Current Weapon");
            if (label != null) weaponLabel = label.GetComponent<Text>();
            var healthObject = GameObject.Find("Player Health");
            if (healthObject != null) healthLabel = healthObject.GetComponent<Text>();
            if (shooter != null) playerHealth = shooter.GetComponent<Health>();
        }

        private void Update()
        {
            if (weaponLabel != null && shooter != null && shooter.CurrentWeapon != null)
                weaponLabel.text = "ОРУЖИЕ: " + shooter.CurrentWeapon.title.ToUpperInvariant();
            if (healthLabel != null && playerHealth != null)
                healthLabel.text = "ЗДОРОВЬЕ: " + Mathf.CeilToInt(playerHealth.CurrentHealth) + " / " + Mathf.CeilToInt(playerHealth.maxHealth);
            var keyboard = Keyboard.current;
            if (keyboard == null || !GameManager.GameplayInputAllowed) return;
            if (keyboard.rKey.wasPressedThisFrame) Restart();
        }

        public void Restart()
        {
            if (GameManager.Instance != null) { GameManager.Instance.RestartLevel(); return; }
            Time.timeScale = 1;
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().path);
        }

        public void ReturnToMenu()
        {
            if (GameManager.Instance != null) { GameManager.Instance.ReturnToMenu(); return; }
            if (!Application.CanStreamedLevelBeLoaded(menuScene)) return;
            Time.timeScale = 1;
            SceneManager.LoadSceneAsync(menuScene);
        }
    }
}
