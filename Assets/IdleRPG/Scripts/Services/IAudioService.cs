namespace IdleRPG.Services
{
    /// <summary>
    /// Game events that have a sound. One cue per *moment the player cares about*, not per code path, so the
    /// real audio pass (Step 20) only has to swap clips behind these names.
    /// </summary>
    public enum SfxCue
    {
        /// <summary>A normal hit lands.</summary>
        Hit = 0,

        /// <summary>A critical hit lands.</summary>
        Crit = 1,

        /// <summary>An enemy dies.</summary>
        Kill = 2,

        /// <summary>A boss wave starts.</summary>
        Boss = 3,

        /// <summary>A hero upgrade is bought / a stat level goes up.</summary>
        LevelUp = 4,

        /// <summary>An offline payout is claimed.</summary>
        Claim = 5,

        /// <summary>Gems are spent (offline cap or instant income).</summary>
        Purchase = 6,

        /// <summary>An ascension completes.</summary>
        Ascend = 7,

        /// <summary>The party wipes or the boss timer expires.</summary>
        Defeat = 8
    }

    /// <summary>
    /// Audio abstraction. The MVP ships <see cref="PlaceholderAudioService"/> (procedural tones, no audio files);
    /// the real pass (Step 20) implements the same interface with authored clips, mixer groups and music.
    ///
    /// ELI5: the game needs a doorbell, but we have no doorbell yet. This interface is the button; the placeholder
    /// makes a beep so everything can be wired and tested now, and Step 20 replaces the beep with a real sound
    /// without touching a single caller.
    /// </summary>
    public interface IAudioService
    {
        /// <summary>Master SFX volume, 0..1 (persisted).</summary>
        float Volume { get; }

        /// <summary>True when the player muted everything.</summary>
        bool IsMuted { get; }

        /// <summary>Cues played this session (dev overlay + tests).</summary>
        int PlayedCount { get; }

        /// <summary>Plays one cue. Safe to call when muted, without a source, or from a null event.</summary>
        void Play(SfxCue cue);

        /// <summary>Sets the volume (persisted) so it survives a relaunch.</summary>
        void SetVolume(float volume);

        /// <summary>Mutes or unmutes (persisted).</summary>
        void SetMuted(bool muted);

        /// <summary>Flips mute and returns the new state (hotkey + UI button).</summary>
        bool ToggleMuted();

        /// <summary>Short "50% | 12 cues | on" style summary for the dev overlay.</summary>
        string Describe();
    }
}
