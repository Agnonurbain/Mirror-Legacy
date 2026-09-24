using System.Collections.Generic;
using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Events;

namespace MirrorChronicles.Core
{
    public enum MusicTrack
    {
        Domain,
        Combat,
        Breakthrough,
        WorldMap,
        Victory,
        Defeat
    }

    /// <summary>
    /// Manages BGM and SFX. Crossfades between music tracks based on game state.
    /// Assign AudioClips via Inspector or load from Resources.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music")]
        public AudioClip domainMusic;
        public AudioClip combatMusic;
        public AudioClip breakthroughMusic;
        public AudioClip worldMapMusic;
        public AudioClip victoryMusic;
        public AudioClip defeatMusic;

        [Header("SFX")]
        public AudioClip sfxParchmentOpen;
        public AudioClip sfxBronzeHit;
        public AudioClip sfxInkDrop;
        public AudioClip sfxBreakthroughSuccess;
        public AudioClip sfxBreakthroughFail;
        public AudioClip sfxCombatHit;
        public AudioClip sfxQiPulse;

        [Header("Settings")]
        [Range(0f, 1f)] public float musicVolume = 0.6f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        public float crossfadeDuration = 1.5f;

        private AudioSource _musicSourceA;
        private AudioSource _musicSourceB;
        private bool _usingSourceA = true;
        private MusicTrack _currentTrack;

        private readonly List<AudioSource> _sfxSources = new List<AudioSource>();
        private const int MaxSfxSources = 8;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _musicSourceA = gameObject.AddComponent<AudioSource>();
            _musicSourceA.loop = true;
            _musicSourceA.playOnAwake = false;

            _musicSourceB = gameObject.AddComponent<AudioSource>();
            _musicSourceB.loop = true;
            _musicSourceB.playOnAwake = false;

            for (int i = 0; i < MaxSfxSources; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sfxSources.Add(src);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnPhaseChanged += HandlePhaseChanged;
            GameEvents.OnBreakthroughSuccess += HandleBreakthroughSuccess;
            GameEvents.OnBreakthroughFailed += HandleBreakthroughFailed;
        }

        private void OnDisable()
        {
            GameEvents.OnPhaseChanged -= HandlePhaseChanged;
            GameEvents.OnBreakthroughSuccess -= HandleBreakthroughSuccess;
            GameEvents.OnBreakthroughFailed -= HandleBreakthroughFailed;
        }

        private void Start()
        {
            PlayMusic(MusicTrack.Domain);
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Breakthrough)
                PlayMusic(MusicTrack.Breakthrough);
            else if (_currentTrack == MusicTrack.Breakthrough)
                PlayMusic(MusicTrack.Domain);
        }

        private void HandleBreakthroughSuccess(CharacterData c, CultivationRealm r)
        {
            PlaySFX(sfxBreakthroughSuccess);
        }

        private void HandleBreakthroughFailed(CharacterData c)
        {
            PlaySFX(sfxBreakthroughFail);
        }

        public void PlayMusic(MusicTrack track)
        {
            if (track == _currentTrack) return;
            _currentTrack = track;

            AudioClip clip = GetMusicClip(track);
            if (clip == null)
            {
                Debug.Log($"[AudioManager] No clip assigned for {track}, skipping.");
                return;
            }

            StartCoroutine(CrossfadeMusic(clip));
        }

        private AudioClip GetMusicClip(MusicTrack track)
        {
            return track switch
            {
                MusicTrack.Domain => domainMusic,
                MusicTrack.Combat => combatMusic,
                MusicTrack.Breakthrough => breakthroughMusic,
                MusicTrack.WorldMap => worldMapMusic,
                MusicTrack.Victory => victoryMusic,
                MusicTrack.Defeat => defeatMusic,
                _ => null
            };
        }

        private System.Collections.IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            var fadeOut = _usingSourceA ? _musicSourceA : _musicSourceB;
            var fadeIn = _usingSourceA ? _musicSourceB : _musicSourceA;
            _usingSourceA = !_usingSourceA;

            fadeIn.clip = newClip;
            fadeIn.volume = 0f;
            fadeIn.Play();

            float elapsed = 0f;
            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / crossfadeDuration;
                fadeIn.volume = Mathf.Lerp(0f, musicVolume, t);
                fadeOut.volume = Mathf.Lerp(musicVolume, 0f, t);
                yield return null;
            }

            fadeOut.Stop();
            fadeIn.volume = musicVolume;
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;

            foreach (var src in _sfxSources)
            {
                if (!src.isPlaying)
                {
                    src.clip = clip;
                    src.volume = sfxVolume;
                    src.Play();
                    return;
                }
            }

            _sfxSources[0].clip = clip;
            _sfxSources[0].volume = sfxVolume;
            _sfxSources[0].Play();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            var active = _usingSourceA ? _musicSourceA : _musicSourceB;
            active.volume = musicVolume;
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }
    }
}
