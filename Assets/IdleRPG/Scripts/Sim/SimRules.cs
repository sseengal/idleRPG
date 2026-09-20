namespace IdleRPG.Sim
{
    /// <summary>
    /// Snapshot of every tuning constant the simulation reads. Built once per encounter from the
    /// data assets (BalanceConfig / ZoneData / affixes) so the sim never touches a ScriptableObject
    /// and a mid-fight balance change cannot leak in.
    ///
    /// 7a carries the MVP's values verbatim (from `CombatScaling`); the difficulty/floors fields are
    /// already here so Steps 12 and 15 only have to fill them in.
    /// </summary>
    public struct SimRules
    {
        // --- Stage scaling ---
        public double EnemyHealthGrowth;
        public double EnemyGoldGrowth;
        public double EnemyAttackGrowth;
        public bool ScaleEnemyDefenseWithStage;

        // --- Damage model ---
        /// <summary>Global floor as a fraction of attack (MVP behaviour; Step 12 makes it per attack type).</summary>
        public double MinDamageRatio;

        /// <summary>Per-attack-type floors (§8 of Sim-Core): basic, heavy, dot. 0 = fall back to MinDamageRatio.</summary>
        public double BasicFloorRatio;
        public double HeavyFloorRatio;
        public double DotFloorRatio;

        /// <summary>Upper bound on armor mitigation once armor% exists (Step 12).</summary>
        public double ArmorCap;

        // --- Crit (global in 7a; becomes per-entity in Step 12) ---
        public double CriticalChance;
        public double CriticalDamageMultiplier;

        // --- Pacing ---
        /// <summary>Multiplies every attack interval. Wall-clock feel only, never balance.</summary>
        public double PaceMultiplier;

        // --- Formation (used from Step 10) ---
        /// <summary>Damage the back row takes while the front row still has a living member.</summary>
        public double BackRowDamageTakenMultiplier;

        public static SimRules Default
        {
            get
            {
                return new SimRules
                {
                    EnemyHealthGrowth = 1.15d,
                    EnemyGoldGrowth = 1.12d,
                    EnemyAttackGrowth = 1.08d,
                    ScaleEnemyDefenseWithStage = false,
                    MinDamageRatio = 0.15d,
                    BasicFloorRatio = 0.15d,
                    HeavyFloorRatio = 0.08d,
                    DotFloorRatio = 0.05d,
                    ArmorCap = 0.75d,
                    CriticalChance = 0.05d,
                    CriticalDamageMultiplier = 2d,
                    PaceMultiplier = 1d,
                    BackRowDamageTakenMultiplier = 0.75d
                };
            }
        }

        /// <summary>Clamps every value into a usable range.</summary>
        public SimRules Sanitized()
        {
            SimRules safe = this;

            safe.EnemyHealthGrowth = AtLeastOne(safe.EnemyHealthGrowth);
            safe.EnemyGoldGrowth = AtLeastOne(safe.EnemyGoldGrowth);
            safe.EnemyAttackGrowth = AtLeastOne(safe.EnemyAttackGrowth);
            safe.MinDamageRatio = Clamp01(safe.MinDamageRatio);
            safe.BasicFloorRatio = Clamp01(safe.BasicFloorRatio);
            safe.HeavyFloorRatio = Clamp01(safe.HeavyFloorRatio);
            safe.DotFloorRatio = Clamp01(safe.DotFloorRatio);
            safe.ArmorCap = Clamp01(safe.ArmorCap);
            safe.CriticalChance = Clamp01(safe.CriticalChance);
            safe.CriticalDamageMultiplier = safe.CriticalDamageMultiplier < 1d ? 1d : safe.CriticalDamageMultiplier;
            safe.BackRowDamageTakenMultiplier = Clamp01(safe.BackRowDamageTakenMultiplier);

            if (safe.PaceMultiplier < 0.25d)
            {
                safe.PaceMultiplier = 0.25d;
            }
            else if (safe.PaceMultiplier > 10d)
            {
                safe.PaceMultiplier = 10d;
            }

            return safe;
        }

        private static double AtLeastOne(double value)
        {
            if (double.IsNaN(value) || value < 1d)
            {
                return 1d;
            }

            return value;
        }

        private static double Clamp01(double value)
        {
            if (double.IsNaN(value) || value < 0d)
            {
                return 0d;
            }

            return value > 1d ? 1d : value;
        }
    }
}
