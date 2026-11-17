using ContraLike.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ContraLike.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(Canvas))]
    [AddComponentMenu("Contra-Like/Интерфейс/Окно победы")]
    public sealed class LevelVictoryView : MonoBehaviour
    {
        private Button exitButton;

        private void Awake() => exitButton = GetComponentInChildren<Button>(true);

        public void Show()
        {
            GameManager.Instance?.EnterVictory();
            gameObject.SetActive(true);
            if (EventSystem.current != null && exitButton != null)
                EventSystem.current.SetSelectedGameObject(exitButton.gameObject);
        }

        public void ReturnToMenu() => GameManager.Instance?.ReturnToMenu();
    }
}
