using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace TheLostRobotStory.Core
{
    // Call AudioManager.Play("jump") from anywhere.
    // Sound effects: Content/sfx/<name>.wav   Music: Content/Music/<name>.ogg
    // (must also be added in Content.mgcb). Missing files are skipped silently.
    public static class AudioManager
    {
        // Folder names exactly as in Content.mgcb (case matters on Linux/macOS)
        private const string SfxFolder = "sfx/";
        private const string MusicFolder = "Music/";

        // =====================================================
        // MIX - per-sound volume (0..1). The .wav files are already levelled,
        // so this is only for taste. Sounds not listed play at 1.0.
        // =====================================================
        private static readonly Dictionary<string, float> Mix = new()
        {
            ["jump"] = 0.7f,
            ["double_jump"] = 0.7f,
            ["attack"] = 0.8f,
            ["shoot"] = 0.6f,         // fired a lot - keep it light
            ["enemy_hit"] = 0.8f,
            ["collect_crystal"] = 0.8f,
            ["ui_move"] = 0.5f,
            ["ui_select"] = 0.7f,
            ["text_blip"] = 0.6f,
            ["nexus_blip"] = 0.7f,
            ["boss_slam"] = 1.0f,
            ["boss_die"] = 1.0f,
            ["evolve"] = 1.0f,
        };

        // If a sound is missing, try this one instead
        private static readonly Dictionary<string, string> Fallbacks = new()
        {
            ["boss_die"] = "enemy_die",
            ["boss_hit"] = "enemy_hit",
            ["double_jump"] = "jump",
            ["nexus_blip"] = "text_blip",

            // new story sounds - add a .wav with this name to replace the stand-in
            ["block"] = "boss_hit",          // tank shield / adaptive shield
            ["adapt"] = "nexus_blip",        // PROTOTYPE G-0 adapts
            ["crusher"] = "boss_slam",
            ["switch"] = "terminal",
            ["fragment"] = "health_up",
            ["laser_beam"] = "boss_dash",
            ["alarm"] = "boss_dash",
            ["surge"] = "evolve",
            ["relay_destroyed"] = "enemy_die",
            ["m4_talk"] = "terminal",
            ["crumble"] = "block",
            ["flame"] = "laser_beam",
        };

        // Music that falls back to another track until its own file exists
        private static readonly Dictionary<string, string> MusicFallbacks = new()
        {
            ["level5"] = "level2",
            ["level6"] = "level4",
            ["boss_guardian"] = "boss",
            ["boss_warden"] = "boss",
            ["boss_warden_cameo"] = "boss",
            ["boss_core"] = "boss",
        };

        private static ContentManager _content;

        private static readonly Dictionary<string, SoundEffect> _sfx = new();
        private static readonly Dictionary<string, Song> _songs = new();
        private static readonly Dictionary<string, double> _lastPlayed = new();

        private static readonly Stopwatch _clock = Stopwatch.StartNew();
        private static readonly Random _random = new();

        // Same sound can't retrigger faster than this (stops ear-splitting stacking)
        private const double MinRepeatSeconds = 0.04;

        public static float MusicVolume { get; private set; } = 0.6f;
        public static float SfxVolume { get; private set; } = 0.8f;

        // =====================================================
        // MUSIC STATE (fade out -> switch -> fade in, plus ducking)
        // =====================================================
        private const float MusicFadeSpeed = 2f;   // full fade in 0.5s
        private const float DuckedLevel = 0.35f;

        private static string _currentSong;
        private static bool _loopSong = true;
        private static bool _switchPending;
        private static float _fade = 1f;
        private static float _duck = 1f;
        private static float _duckTarget = 1f;

        public static void Initialize(ContentManager content)
        {
            _content = content;
        }

        // Every sound effect the game uses (must match the files in Content/sfx)
        public static readonly string[] SoundNames =
        {
            "jump", "double_jump", "dash", "shoot", "attack", "hurt", "death",
            "enemy_hit", "enemy_die", "boss_hit", "boss_dash", "boss_slam", "boss_die",
            "collect_crystal", "health_up", "evolve", "checkpoint", "door_open", "terminal",
            "ui_move", "ui_select", "text_blip", "nexus_blip",
            "block", "adapt", "crusher", "switch", "fragment", "laser_beam",
            "alarm", "surge", "relay_destroyed", "m4_talk", "crumble", "flame"
        };

        // Loads every sound once at startup so the first jump doesn't hitch
        public static void Preload()
        {
            foreach (string name in SoundNames)
                GetSfx(name);
        }

        public static void SetVolumes(float music, float sfx)
        {
            MusicVolume = Math.Clamp(music, 0f, 1f);
            SfxVolume = Math.Clamp(sfx, 0f, 1f);
            ApplyMusicVolume();
        }

        // =====================================================
        // SOUND EFFECTS
        // =====================================================

        // pitchVariance: small random pitch shift so repeated sounds don't feel robotic
        public static void Play(string name, float volume = 1f, float pitchVariance = 0.08f)
        {
            double now = _clock.Elapsed.TotalSeconds;

            if (_lastPlayed.TryGetValue(name, out double last) &&
                now - last < MinRepeatSeconds)
                return;

            SoundEffect sound = GetSfx(name);

            if (sound == null &&
                Fallbacks.TryGetValue(name, out string fallback))
            {
                sound = GetSfx(fallback);
            }

            if (sound == null)
                return;

            _lastPlayed[name] = now;

            float mix = Mix.TryGetValue(name, out float m) ? m : 1f;
            float pitch = (float)(_random.NextDouble() * 2 - 1) * pitchVariance;

            sound.Play(
                Math.Clamp(volume * mix * SfxVolume, 0f, 1f),
                Math.Clamp(pitch, -1f, 1f),
                0f);
        }

        // =====================================================
        // MUSIC
        // =====================================================

        public static void PlayMusic(string name, bool loop = true)
        {
            if (_currentSong == name)
                return;

            _currentSong = name;
            _loopSong = loop;

            if (MediaPlayer.State == MediaState.Playing && _fade > 0.01f)
                _switchPending = true;  // fade the old track out first
            else
                StartSong();
        }

        public static void StopMusic()
        {
            _currentSong = null;
            _switchPending = true;      // fades out, then stops
        }

        // Lower the music while a menu is open
        public static void SetDucked(bool ducked)
        {
            _duckTarget = ducked ? DuckedLevel : 1f;
        }

        // Call once per frame (Game1.Update)
        public static void Update(float dt)
        {
            if (_switchPending)
            {
                _fade -= MusicFadeSpeed * dt;

                if (_fade <= 0f)
                {
                    _fade = 0f;

                    if (_currentSong == null)
                    {
                        _switchPending = false;
                        MediaPlayer.Stop();
                    }
                    else
                    {
                        StartSong();
                    }
                }
            }
            else if (_fade < 1f)
            {
                _fade = Math.Min(1f, _fade + MusicFadeSpeed * dt);
            }

            if (_duck < _duckTarget)
                _duck = Math.Min(_duckTarget, _duck + 3f * dt);
            else if (_duck > _duckTarget)
                _duck = Math.Max(_duckTarget, _duck - 3f * dt);

            ApplyMusicVolume();
        }

        private static void StartSong()
        {
            _switchPending = false;
            _fade = 0f;

            Song song = GetSong(_currentSong);

            if (song == null && MusicFallbacks.TryGetValue(_currentSong, out string fallback))
                song = GetSong(fallback);

            if (song == null)
            {
                MediaPlayer.Stop();
                return;
            }

            MediaPlayer.IsRepeating = _loopSong;
            ApplyMusicVolume();
            MediaPlayer.Play(song);
        }

        private static void ApplyMusicVolume()
        {
            MediaPlayer.Volume = Math.Clamp(MusicVolume * _fade * _duck, 0f, 1f);
        }

        // =====================================================
        // LOADING
        // =====================================================

        private static SoundEffect GetSfx(string name)
        {
            if (_sfx.TryGetValue(name, out SoundEffect cached))
                return cached; // may be null = known missing

            SoundEffect loaded = TryLoad<SoundEffect>(SfxFolder + name);
            _sfx[name] = loaded;
            return loaded;
        }

        private static Song GetSong(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            if (_songs.TryGetValue(name, out Song cached))
                return cached;

            Song loaded = TryLoad<Song>(MusicFolder + name);
            _songs[name] = loaded;
            return loaded;
        }

        private static T TryLoad<T>(string asset) where T : class
        {
            if (_content == null)
                return null;

            try
            {
                return _content.Load<T>(asset);
            }
            catch (Exception)
            {
                Debug.WriteLine($"[Audio] missing asset: {asset}");
                return null;
            }
        }
    }
}
