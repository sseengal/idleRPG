using System.Collections.Generic;
using UnityEngine;

namespace IdleRPG.Services
{
    /// <summary>
    /// Development implementation of <see cref="IAudioService"/>: **no audio files at all**. Each cue is a short
    /// tone generated in memory (sine with a fast decay, plus a small pitch sweep), so the whole game can be
    /// wired, mixed and tested before an artist supplies a single clip.
    ///
    /// ELI5: instead of shipping placeholder .wav files nobody wants to hear twice, the game hums the tune
    /// itself. Different cue = different pitch/shape, so "hit" and "crit" are already tellable apart.
    ///
    /// Volume and mute are PlayerPrefs-backed, so the settings survive a relaunch - and the full reset wipes them,
    /// exactly like a fresh install.
    /// </summary>
    public sealed class PlaceholderAudioService : IAudioService
    {
        public const string VolumePrefsKey = "IdleRPG.audioVolume";
        public const string MutedPrefsKey = "IdleRPG.audioMuted";

        private const int SampleRate = 44100;

        /// <summary>Pitch/shape recipe per cue: base note, sweep target, length and how bright it is.</summary>
        private readonly struct CueRecipe
        {
            public readonly float Frequency;
            public readonly float EndFrequency;
            public readonly float Seconds;
            public readonly float Brightness;

            public CueRecipe(float frequency, float endFrequency, float seconds, float brightness)
            {
                Frequency = frequency;
                EndFrequency = endFrequency;
                Seconds = seconds;
                Brightness = brightness;
            }
        }

        private readonly Dictionary<SfxCue, AudioClip> clips = new Dictionary<SfxCue, AudioClip>();
        private readonly AudioSource source;

        private float volume;
        private bool muted;

        public PlaceholderAudioService(MonoBehaviour host, float defaultVolume = 0.5f)
        {
            volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePrefsKey, defaultVolume));
            muted = PlayerPrefs.GetInt(MutedPrefsKey, 0) == 1;

            if (host == null)
            {
                Debug.LogError("[PlaceholderAudioService] A MonoBehaviour host is required for the AudioSource.");
                return;
            }

            GameObject hostObject = new GameObject("PlaceholderAudio");
            hostObject.transform.SetParent(host.transform, false);

            source = hostObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;                       // 2D: UI/combat cues, not positional
            source.volume = EffectiveVolume;
            source.ignoreListenerPause = true;
        }

        public float Volume => volume;

        public bool IsMuted => muted;

        public int PlayedCount { get; private set; }

        /// <summary>Volume the source actually runs at (mute wins).</summary>
        public float EffectiveVolume => muted ? 0f : volume;

        public void Play(SfxCue cue)
        {
            if (source == null || EffectiveVolume <= 0f)
            {
                return;
            }

            AudioClip clip = GetClip(cue);

            if (clip == null)
            {
                return;
            }

            source.PlayOneShot(clip);
            PlayedCount++;
        }

        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
            PlayerPrefs.SetFloat(VolumePrefsKey, volume);
            PlayerPrefs.Save();

            if (source != null)
            {
                source.volume = EffectiveVolume;
            }
        }

        public void SetMuted(bool value)
        {
            muted = value;
            PlayerPrefs.SetInt(MutedPrefsKey, muted ? 1 : 0);
            PlayerPrefs.Save();

            if (source != null)
            {
                source.volume = EffectiveVolume;
            }
        }

        public bool ToggleMuted()
        {
            SetMuted(!muted);
            return muted;
        }

        public string Describe()
        {
            return muted ? "muted" : $"{volume * 100f:0}%";
        }

        /// <summary>Generates a cue clip on first use and caches it (never allocates twice).</summary>
        private AudioClip GetClip(SfxCue cue)
        {
            if (clips.TryGetValue(cue, out AudioClip cached))
            {
                return cached;
            }

            CueRecipe recipe = RecipeFor(cue);
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(recipe.Seconds * SampleRate));
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float seconds = i / (float)SampleRate;
                float progress = i / (float)sampleCount;

                // Pitch sweep keeps two cues of similar length distinguishable.
                float frequency = Mathf.Lerp(recipe.Frequency, recipe.EndFrequency, progress);
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * seconds);

                // Brightness adds a second harmonic: 0 = pure hum, 1 = bright ping.
                if (recipe.Brightness > 0f)
                {
                    float brighter = wave * 0.7f + Mathf.Sin(4f * Mathf.PI * frequency * seconds) * 0.3f;
                    wave = Mathf.Lerp(wave, brighter, recipe.Brightness);
                }

                // Fast exponential decay with a 2ms fade-in, so there is no click at the start.
                float envelope = Mathf.Exp(-6f * progress) * Mathf.Min(1f, i / (SampleRate * 0.002f));
                samples[i] = wave * envelope * 0.35f;
            }

            AudioClip clip = AudioClip.Create($"sfx_{cue}", sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            clips[cue] = clip;
            return clip;
        }

        /// <summary>The placeholder "sound design": higher + shorter = something good happened to the enemy.</summary>
        private static CueRecipe RecipeFor(SfxCue cue)
        {
            switch (cue)
            {
                case SfxCue.Hit:
                    return new CueRecipe(220f, 180f, 0.06f, 0.1f);

                case SfxCue.Crit:
                    return new CueRecipe(660f, 990f, 0.09f, 1f);

                case SfxCue.Kill:
                    return new CueRecipe(330f, 220f, 0.14f, 0.4f);

                case SfxCue.Boss:
                    return new CueRecipe(110f, 82f, 0.45f, 0.15f);

                case SfxCue.LevelUp:
                    return new CueRecipe(520f, 1040f, 0.28f, 0.6f);

                case SfxCue.Claim:
                    return new CueRecipe(440f, 880f, 0.35f, 0.5f);

                case SfxCue.Purchase:
                    return new CueRecipe(740f, 1110f, 0.12f, 0.8f);

                case SfxCue.Ascend:
                    return new CueRecipe(330f, 1320f, 0.7f, 0.5f);

                case SfxCue.Defeat:
                    return new CueRecipe(180f, 70f, 0.55f, 0.05f);

                default:
                    return new CueRecipe(440f, 440f, 0.08f, 0.2f);
            }
        }
    }
}
