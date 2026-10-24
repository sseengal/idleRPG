namespace IdleRPG.Combat
{
    /// <summary>
    /// Unity-free snapshot of the tuning constants the combat simulation needs.
    /// Built once by <see cref="CombatManager"/> from BalanceConfig so the simulator
    /// (and any future offline/fast-forward run) never touches a ScriptableObject.
    /// </summary>
    public struct CombatScaling
    {
        public double EnemyHealthGrowth;
        public double EnemyGoldGrowth;
        public double EnemyAttackGrowth;
        public bool ScaleEnemyDefenseWithStage;
        public double MinDamageRatio;
        public double CriticalChance;
        public double CriticalDamageMultiplier;

        /// <summary>Multiplies every attack interval. Wall-clock pacing only (1 = fastest).</summary>
        public double PaceMultiplier;

        /// <summary>Safe fallback matching the BalanceConfig defaults.</summary>
        public static CombatScaling Default
        {
            get
            {
                return new CombatScaling
                {
                    EnemyHealthGrowth = 1.15d,
                    EnemyGoldGrowth = 1.12d,
                    EnemyAttackGrowth = 1.08d,
                    ScaleEnemyDefenseWithStage = false,
                    MinDamageRatio = 0.15d,
                    CriticalChance = 0.05d,
                    CriticalDamageMultiplier = 2d,
                    PaceMultiplier = 1d
                };
            }
        }

        /// <summary>Clamps every value into a usable range.</summary>
        public CombatScaling Sanitized()
        {
            CombatScaling safe = this;

            if (safe.EnemyHealthGrowth < 1d)
            {
                safe.EnemyHealthGrowth = 1d;
            }

            if (safe.EnemyGoldGrowth < 1d)
            {
                safe.EnemyGoldGrowth = 1d;
            }

            if (safe.EnemyAttackGrowth < 1d)
            {
                safe.EnemyAttackGrowth = 1d;
            }

            if (safe.MinDamageRatio < 0d)
            {
                safe.MinDamageRatio = 0d;
            }
            else if (safe.MinDamageRatio > 1d)
            {
                safe.MinDamageRatio = 1d;
            }

            if (safe.CriticalChance < 0d)
            {
                safe.CriticalChance = 0d;
            }
            else if (safe.CriticalChance > 1d)
            {
                safe.CriticalChance = 1d;
            }

            if (safe.CriticalDamageMultiplier < 1d)
            {
                safe.CriticalDamageMultiplier = 1d;
            }

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
    }
}