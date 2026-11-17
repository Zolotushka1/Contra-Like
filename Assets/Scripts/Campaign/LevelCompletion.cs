using UnityEngine;
using UnityEngine.Events;

namespace ContraLike.Campaign
{
    [DefaultExecutionOrder(-150), DisallowMultipleComponent]
    [AddComponentMenu("Contra-Like/Завершение уровня")]
    public sealed class LevelCompletion : MonoBehaviour
    {
        [Header("Попытка уровня")]
        [InspectorName("Полный рестарт уровня")]
        [Tooltip("При рестарте клавишей R, смерти или падении вернуть врагов и монеты и вычесть заработанное за текущую попытку.")]
        public bool fullRestart = true;

        [InspectorName("Сброс попытки при выходе")]
        [Tooltip("При выходе на карту или закрытии игры до победы вернуть врагов и монеты при следующем входе и вычесть заработанное за попытку.")]
        public bool resetOnExit = true;

        [Header("События")]
        [InspectorName("После победы")]
        [Tooltip("Например, показать результаты или включить музыку победы. Прогресс сохраняется автоматически перед событием.")]
        public UnityEvent onCompleted = new UnityEvent();

        private bool completed;

        private void Awake() => LevelRun.Register(this);

        public void CompleteLevel()
        {
            if (completed) return;
            completed = true;
            if (string.IsNullOrEmpty(LevelRun.LevelId))
                Debug.LogWarning("Для сохранения победы сначала запустите уровень с карты.", this);
            LevelRun.FinishAttempt(gameObject.scene.path);
            onCompleted.Invoke();
        }
    }
}
