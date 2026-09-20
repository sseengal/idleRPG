namespace IdleRPG.Sim
{
    /// <summary>
    /// Everything the simulation is allowed to know: frozen rules, the randomness source, the mode it is
    /// being driven in and the hard caps. One instance is built per encounter by the runtime and handed to
    /// the simulator - the sim never reads a ScriptableObject, a service or the clock.
    /// </summary>
    public sealed class SimContext
    {
        public SimContext(SimRules rules, IRng rng, SimMode mode, SimCaps caps)
        {
            Rules = rules.Sanitized();
            Rng = rng;
            Mode = mode;
            Caps = caps.Sanitized();
        }

        public SimContext(SimRules rules, int randomSeed, SimMode mode)
            : this(rules, new SystemRng(randomSeed), mode, SimCaps.Default)
        {
        }

        /// <summary>Default context used by tooling and as a fallback: live, default rules, seed 0.</summary>
        public static SimContext CreateDefault()
        {
            return new SimContext(SimRules.Default, 0, SimMode.Live);
        }

        public SimRules Rules { get; private set; }

        public IRng Rng { get; private set; }

        public SimMode Mode { get; private set; }

        public SimCaps Caps { get; private set; }

        /// <summary>True when per-hit/cosmetic events should be suppressed (offline, fast-forward).</summary>
        public bool IsSilent => Mode.IsSilent();

        /// <summary>Replaces the rules in place (a balance change while an encounter is idle).</summary>
        public void ApplyRules(SimRules rules)
        {
            Rules = rules.Sanitized();
        }

        public void SetMode(SimMode mode)
        {
            Mode = mode;
        }

        public override string ToString()
        {
            return $"SimContext({Mode}, pace x{Rules.PaceMultiplier:0.##}, enemies<={Caps.MaxEnemiesPerEncounter})";
        }
    }
}
