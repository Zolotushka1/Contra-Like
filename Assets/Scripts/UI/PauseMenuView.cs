using ContraLike.Audio;
using ContraLike.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ContraLike.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(Canvas))]
    [AddComponentMenu("Contra-Like/Интерфейс/Меню паузы")]
    public sealed class PauseMenuView : MonoBehaviour
    {
        private GameObject panel;
        private Slider volume;
        private Text volumeLabel;
        private Button resumeButton;
        private GameManager game;
        private MusicManager music;

        private void Awake()
        {
            panel = transform.Find("Pause Panel").gameObject;
            volume = GetComponentInChildren<Slider>(true);
            volumeLabel = panel.transform.Find("Content/Volume Value").GetComponent<Text>();
            resumeButton = panel.transform.Find("Content/Resume Button").GetComponent<Button>();
        }

        private void OnEnable() => Bind();
        private void Start() => Bind();

        private void Bind()
        {
            if (panel == null) return;
            if (game == null && GameManager.Instance != null)
            {
                game = GameManager.Instance;
                game.StateChanged += ShowState;
            }
            if (music == null && MusicManager.Instance != null)
            {
                music = MusicManager.Instance;
                music.VolumeChanged += ShowVolume;
            }
            if (music != null) ShowVolume(music.Volume);
            if (game != null) ShowState(game.State);
        }

        private void OnDisable()
        {
            if (game != null) game.StateChanged -= ShowState;
            if (music != null) music.VolumeChanged -= ShowVolume;
            game = null;
            music = null;
        }

        private void ShowState(GameState state)
        {
            var show = state == GameState.Paused;
            panel.SetActive(show);
            if (EventSystem.current == null) return;
            if (show) EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
            else if (EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
                EventSystem.current.SetSelectedGameObject(null);
        }

        private void ShowVolume(float value)
        {
            volume.SetValueWithoutNotify(value);
            volumeLabel.text = Mathf.RoundToInt(value * 100) + "%";
        }

        public void ResumeGame() => GameManager.Instance?.ResumeGame();
        public void ReturnToMenu() => GameManager.Instance?.ReturnToMenu();
        public void ChangeVolume(float value) => MusicManager.Instance?.SetVolume(value);
    }
}
