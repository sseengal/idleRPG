namespace IdleRPG.Sim
{
    /// <summary>
    /// How the simulation is being driven. Gameplay numbers must be identical in every mode;
    /// only the emitted events differ (offline/fast-forward runs do not need per-hit view events).
    /// </summary>
    public enum SimMode
    {
        /// <summary>Real-time play: every event is raised (views, log, damage numbers).</summary>
        Live = 0,

        /// <summary>Speed-up (ad/gem fast-forward): events are coalesced by the director.</summary>
        FastForward = 1,

        /// <summary>Headless estimate (offline payout, Balance Lab): result only, no view events.</summary>
        Offline = 2
    }

    public static class SimModeExtensions
    {
        /// <summary>True when per-hit/cosmetic events should be suppressed.</summary>
        public static bool IsSilent(this SimMode mode)
        {
            return mode != SimMode.Live;
        }
    }
}
