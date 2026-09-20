namespace IdleRPG.Sim
{
    /// <summary>
    /// Hard limits for the simulation. They exist so late-game content (many enemies, many statuses,
    /// chained triggers) can never turn one frame into a meltdown, and so offline estimates stay bounded.
    /// </summary>
    public struct SimCaps
    {
        /// <summary>Maximum enemies in one encounter (portrait layout allows 3 now, 5 later).</summary>
        public int MaxEnemiesPerEncounter;

        /// <summary>Maximum simultaneous statuses on one combatant.</summary>
        public int MaxStatusesPerCombatant;

        /// <summary>How deep on-hit/on-kill trigger chains may recurse.</summary>
        public int MaxTriggerDepth;

        /// <summary>How many fixed steps one frame may run while catching up.</summary>
        public int MaxStepsPerFrame;

        /// <summary>Upper bound on simulated seconds for an offline estimate (extrapolate beyond this).</summary>
        public double MaxOfflineSimSeconds;

        public static SimCaps Default
        {
            get
            {
                return new SimCaps
                {
                    MaxEnemiesPerEncounter = 3,
                    MaxStatusesPerCombatant = 8,
                    MaxTriggerDepth = 3,
                    MaxStepsPerFrame = 12,
                    MaxOfflineSimSeconds = 600d
                };
            }
        }

        /// <summary>Clamps every cap into a usable range.</summary>
        public SimCaps Sanitized()
        {
            SimCaps safe = this;

            if (safe.MaxEnemiesPerEncounter < 1)
            {
                safe.MaxEnemiesPerEncounter = 1;
            }

            if (safe.MaxStatusesPerCombatant < 0)
            {
                safe.MaxStatusesPerCombatant = 0;
            }

            if (safe.MaxTriggerDepth < 0)
            {
                safe.MaxTriggerDepth = 0;
            }

            if (safe.MaxStepsPerFrame < 1)
            {
                safe.MaxStepsPerFrame = 1;
            }

            if (safe.MaxOfflineSimSeconds < 1d)
            {
                safe.MaxOfflineSimSeconds = 1d;
            }

            return safe;
        }
    }
}
