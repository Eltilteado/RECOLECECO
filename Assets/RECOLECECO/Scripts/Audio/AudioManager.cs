using System.Collections.Generic;
using UnityEngine;

namespace RecolecEco
{
    public enum Sfx { Click, Collect, Arm, Splash, Correct, Wrong, Buy, Mission, Milestone, Dock, Bump, Fanfare, Gull }

    /// <summary>
    /// Audio 100 % original generado por síntesis al iniciar (sin recursos externos):
    /// olas, gaviotas, motor, efectos y música relajante.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        static AudioManager _i;
        public static AudioManager I
        {
            get
            {
                if (_i == null)
                {
                    var go = new GameObject("AudioManager");
                    DontDestroyOnLoad(go);
                    _i = go.AddComponent<AudioManager>();
                    _i.Init();
                }
                return _i;
            }
        }

        readonly Dictionary<Sfx, AudioClip> _clips = new Dictionary<Sfx, AudioClip>();
        AudioSource _music, _ambience, _motor;
        readonly List<AudioSource> _sfxPool = new List<AudioSource>();
        float _gullTimer = 4f;
        float _motorTarget, _motorPitch = 0.8f;
        AudioClip _menuMusic, _gameMusic;
        public float MusicVolume = 0.32f;

        void Init()
        {
            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true; _music.volume = MusicVolume; _music.playOnAwake = false;
            _ambience = gameObject.AddComponent<AudioSource>();
            _ambience.loop = true; _ambience.volume = 0.32f;
            _motor = gameObject.AddComponent<AudioSource>();
            _motor.loop = true; _motor.volume = 0f;
            for (int i = 0; i < 8; i++) { var s = gameObject.AddComponent<AudioSource>(); s.playOnAwake = false; _sfxPool.Add(s); }

            _ambience.clip = Synth.Waves(); _ambience.Play();
            _motor.clip = Synth.Motor(); _motor.Play();
            _clips[Sfx.Click] = Synth.Blip(880, 0.06f, 0.25f);
            _clips[Sfx.Collect] = Synth.Arp(new[] { 660f, 880f, 1320f }, 0.07f, 0.35f);
            _clips[Sfx.Arm] = Synth.Sweep(180, 320, 0.3f, 0.25f, true);
            _clips[Sfx.Splash] = Synth.Splash(0.35f, 0.5f);
            _clips[Sfx.Correct] = Synth.Arp(new[] { 784f, 1046f }, 0.09f, 0.4f);
            _clips[Sfx.Wrong] = Synth.Arp(new[] { 300f, 240f }, 0.12f, 0.35f);
            _clips[Sfx.Buy] = Synth.Arp(new[] { 523f, 659f, 784f, 1046f, 1318f }, 0.06f, 0.4f);
            _clips[Sfx.Mission] = Synth.Arp(new[] { 523f, 659f, 784f, 1046f }, 0.12f, 0.45f);
            _clips[Sfx.Milestone] = Synth.Arp(new[] { 392f, 523f, 659f, 784f, 1046f }, 0.1f, 0.45f);
            _clips[Sfx.Dock] = Synth.Thud(0.35f, 0.6f);
            _clips[Sfx.Bump] = Synth.Thud(0.25f, 0.5f);
            _clips[Sfx.Fanfare] = Synth.Arp(new[] { 392f, 523f, 659f, 784f, 659f, 784f, 1046f }, 0.16f, 0.45f);
            _clips[Sfx.Gull] = Synth.Gull();
            _menuMusic = Synth.Music(1);
            _gameMusic = Synth.Music(2);
        }

        public void PlayMenuMusic() { SwitchMusic(_menuMusic); }
        public void PlayGameMusic() { SwitchMusic(_gameMusic); }

        void SwitchMusic(AudioClip c)
        {
            if (_music.clip == c && _music.isPlaying) return;
            _music.clip = c; _music.volume = MusicVolume; _music.Play();
        }

        public void PlaySfx(Sfx s, float volume = 1f, float pitch = 1f)
        {
            if (!_clips.TryGetValue(s, out var clip)) return;
            foreach (var src in _sfxPool)
            {
                if (src.isPlaying) continue;
                src.pitch = pitch * Random.Range(0.97f, 1.03f);
                src.PlayOneShot(clip, volume);
                return;
            }
            _sfxPool[0].PlayOneShot(clip, volume);
        }

        public void SetMotor(float speed01, bool throttle)
        {
            _motorTarget = 0.10f + speed01 * 0.30f + (throttle ? 0.05f : 0f);
            _motorPitch = 0.7f + speed01 * 0.9f;
        }

        public void StopMotor() { _motorTarget = 0f; }

        void Update()
        {
            bool inGame = GameManager.I != null && !GameManager.I.Paused;
            if (!inGame) _motorTarget = 0f;
            _motor.volume = Mathf.MoveTowards(_motor.volume, _motorTarget, Time.unscaledDeltaTime * 0.8f);
            _motor.pitch = Mathf.Lerp(_motor.pitch, _motorPitch, Time.unscaledDeltaTime * 3f);
            _motorTarget = Mathf.MoveTowards(_motorTarget, 0f, Time.unscaledDeltaTime * 0.5f);

            _gullTimer -= Time.unscaledDeltaTime;
            if (_gullTimer <= 0f)
            {
                _gullTimer = Random.Range(5f, 13f);
                PlaySfx(Sfx.Gull, Random.Range(0.25f, 0.5f), Random.Range(0.85f, 1.2f));
            }
        }
    }
}
