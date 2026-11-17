using System;
using ContraLike.Audio;
using ContraLike.Campaign;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ContraLike.Core
{
    public enum GameState { Menu, Playing, Paused, Loading, Victory }

    [DefaultExecutionOrder(-200), DisallowMultipleComponent]
    [AddComponentMenu("Contra-Like/Игра/Game Manager")]
    public sealed class GameManager : MonoBehaviour
    {
        [SceneDropdown, InspectorName("Сцена главного меню")]
        public string menuScene = "Assets/Scenes/Main Menu.unity";
        [Header("События")]
        [InspectorName("При постановке на паузу")]
        public UnityEvent onPaused = new UnityEvent();
        [InspectorName("При продолжении игры")]
        public UnityEvent onResumed = new UnityEvent();

        public static GameManager Instance { get; private set; }
        public GameState State { get; private set; }
        public bool IsPaused => State == GameState.Paused;
        public static bool GameplayInputAllowed => Time.timeScale > 0 &&
            (Instance == null || Instance.State == GameState.Playing && Time.frameCount > Instance.blockInputThroughFrame);
        public event Action<GameState> StateChanged;

        private float previousTimeScale = 1;
        private int blockInputThroughFrame = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RestoreClock();
            State = SceneManager.GetActiveScene().path == menuScene ? GameState.Menu : GameState.Playing;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) TogglePause();
        }

        public void TogglePause()
        {
            if (IsPaused) ResumeGame();
            else PauseGame();
        }

        public void PauseGame()
        {
            if (State != GameState.Playing) return;
            previousTimeScale = Time.timeScale > 0 ? Time.timeScale : 1;
            Time.timeScale = 0;
            AudioListener.pause = true;
            ChangeState(GameState.Paused);
            onPaused.Invoke();
        }

        public void ResumeGame()
        {
            if (!IsPaused) return;
            Time.timeScale = previousTimeScale;
            AudioListener.pause = false;
            blockInputThroughFrame = Time.frameCount + 1;
            ChangeState(GameState.Playing);
            MusicManager.Instance?.SaveSettings();
            onResumed.Invoke();
        }

        internal void EnterVictory()
        {
            if (State != GameState.Playing && State != GameState.Paused) return;
            Time.timeScale = 0;
            AudioListener.pause = false;
            ChangeState(GameState.Victory);
        }

        public void ReturnToMenu() => LoadLevel(menuScene);
        public void RestartLevel() => TryRestartLevel();

        internal bool TryRestartLevel() => LoadLevel(SceneManager.GetActiveScene().path, restart: true);

        public bool LoadLevel(string scenePath) => LoadLevel(scenePath, restart: false);

        private bool LoadLevel(string scenePath, bool restart)
        {
            if (State == GameState.Loading || string.IsNullOrEmpty(scenePath) || !Application.CanStreamedLevelBeLoaded(scenePath)) return false;
            var outgoing = SceneManager.GetActiveScene().path;
            var wasVictory = State == GameState.Victory;
            RestoreClock();
            MusicManager.Instance?.SaveSettings();
            ChangeState(GameState.Loading);
            var operation = SceneManager.LoadSceneAsync(scenePath);
            if (operation != null)
            {
                if (restart || outgoing == scenePath) LevelRun.RestartAttempt(outgoing);
                else LevelRun.LeaveAttempt(outgoing);
                return true;
            }
            if (wasVictory)
            {
                Time.timeScale = 0;
                ChangeState(GameState.Victory);
            }
            else ChangeState(SceneManager.GetActiveScene().path == menuScene ? GameState.Menu : GameState.Playing);
            return false;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            RestoreClock();
            blockInputThroughFrame = Time.frameCount + 1;
            ChangeState(scene.path == menuScene ? GameState.Menu : GameState.Playing);
        }

        private void OnApplicationQuit() => LevelRun.LeaveAttempt(SceneManager.GetActiveScene().path);

        private void ChangeState(GameState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }

        private static void RestoreClock()
        {
            Time.timeScale = 1;
            AudioListener.pause = false;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestoreClock();
            Instance = null;
        }
    }
}
