using System;
using System.Collections.Generic;
using ContraLike.Campaign;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ContraLike.Audio
{
    [Serializable]
    public sealed class SceneMusic
    {
        [SceneDropdown, InspectorName("Сцена")]
        public string scenePath;
        [InspectorName("Основная тема")]
        public AudioClip music;
        [InspectorName("Тема босса")]
        public AudioClip bossMusic;
    }

    [DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("Contra-Like/Звук/Music Manager")]
    public sealed class MusicManager : MonoBehaviour
    {
        [InspectorName("Музыка сцен"), Tooltip("Добавьте сцену и её музыку. При загрузке сцены основная тема включается автоматически.")]
        public List<SceneMusic> scenes = new List<SceneMusic>();
        [InspectorName("Музыка титров")]
        public AudioClip creditsMusic;
        [Range(0, 1), InspectorName("Начальная общая громкость"), Tooltip("Громкость музыки и звуков при первом запуске. В дальнейшем используется сохранённое значение ползунка паузы.")]
        public float initialVolume = 0.7f;

        private const string VolumeKey = "ContraLike.Audio.MasterVolume";
        private AudioSource source;
        private bool settingsDirty;
        private float saveAfter;
        public static MusicManager Instance { get; private set; }
        public float Volume { get; private set; }
        public AudioClip CurrentTrack => source != null ? source.clip : null;
        public event Action<float> VolumeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0;
            source.ignoreListenerPause = true;
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, initialVolume));
            AudioListener.volume = Volume;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            if (Instance == this) PlayLevelMusic();
        }

        private void Update()
        {
            if (Instance == this && settingsDirty && Time.unscaledTime >= saveAfter) SaveSettings();
        }

        public void SetVolume(float volume)
        {
            Volume = Mathf.Clamp01(volume);
            AudioListener.volume = Volume;
            PlayerPrefs.SetFloat(VolumeKey, Volume);
            settingsDirty = true;
            saveAfter = Time.unscaledTime + 0.3f;
            VolumeChanged?.Invoke(Volume);
        }

        public void SaveSettings()
        {
            if (!settingsDirty) return;
            PlayerPrefs.Save();
            settingsDirty = false;
        }

        public void PlayLevelMusic() => PlayMusic(FindSceneMusic()?.music);
        public void PlayBossMusic()
        {
            var track = FindSceneMusic()?.bossMusic;
            if (track != null) PlayMusic(track);
        }
        public void PlayCreditsMusic() => PlayMusic(creditsMusic);

        public void PlayMusic(AudioClip clip)
        {
            if (source == null || source.clip == clip && source.isPlaying) return;
            source.clip = clip;
            if (clip == null) source.Stop();
            else source.Play();
        }

        private SceneMusic FindSceneMusic()
        {
            var scenePath = SceneManager.GetActiveScene().path;
            return scenes.Find(entry => entry != null && entry.scenePath == scenePath);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) PlayLevelMusic();
        }

        private void OnApplicationPause(bool paused) { if (paused && Instance == this) SaveSettings(); }
        private void OnApplicationQuit() { if (Instance == this) SaveSettings(); }
        private void OnDestroy()
        {
            if (Instance != this) return;
            SaveSettings();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }
}
