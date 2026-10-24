using IdleRPG.Combat;
using IdleRPG.Data;
using IdleRPG.Sim;

namespace IdleRPG.Combat
{
    /// <summary>
    /// Builds the sim's frozen rule snapshot from the Unity-side data (BalanceConfig) or from the legacy
    /// <see cref="CombatScaling"/> struct. Keeping the conversion in the runtime assembly means `Sim/`
    /// never has to know about ScriptableObjects (and the mapping lives in exactly one place).
    /// </summary>
    public static class SimRulesFactory
    {
        /// <summary>Maps the MVP scaling struct onto the new rule snapshot (values are identical).</summary>
        public static SimRules FromScaling(CombatScaling scaling)
        {
            SimRules rules = SimRules.Default;

            rules.EnemyHealthGrowth = scaling.EnemyHealthGrowth;
            rules.EnemyGoldGrowth = scaling.EnemyGoldGrowth;
            rules.EnemyAttackGrowth = scaling.EnemyAttackGrowth;
            rules.ScaleEnemyDefenseWithStage = scaling.ScaleEnemyDefenseWithStage;
            rules.MinDamageRatio = scaling.MinDamageRatio;
            rules.BasicFloorRatio = scaling.MinDamageRatio;
            rules.CriticalChance = scaling.CriticalChance;
            rules.CriticalDamageMultiplier = scaling.CriticalDamageMultiplier;
            rules.PaceMultiplier = scaling.PaceMultiplier;

            return rules.Sanitized();
        }

        /// <summary>Builds the snapshot straight from the balance asset (used by Balance Lab and tools).</summary>
        public static SimRules FromBalance(BalanceConfig balance)
        {
            if (balance == null)
            {
                return SimRules.Default;
            }

            SimRules rules = SimRules.Default;

            rules.EnemyHealthGrowth = balance.EnemyHealthGrowth;
            rules.EnemyGoldGrowth = balance.EnemyGoldGrowth;
            rules.EnemyAttackGrowth = balance.EnemyAttackGrowth;
            rules.ScaleEnemyDefenseWithStage = balance.ScaleEnemyDefenseWithStage;
            rules.MinDamageRatio = balance.MinDamageRatio;
            rules.BasicFloorRatio = balance.MinDamageRatio;
            rules.CriticalChance = balance.CriticalChance;
            rules.CriticalDamageMultiplier = balance.CriticalDamageMultiplier;
            rules.PaceMultiplier = balance.CombatPaceMultiplier;

            return rules.Sanitized();
        }

        /// <summary>The MVP scaling struct rebuilt from a balance asset (kept for existing callers).</summary>
        public static CombatScaling ScalingFromBalance(BalanceConfig balance)
        {
            if (balance == null)
            {
                return CombatScaling.Default;
            }

            CombatScaling scaling = CombatScaling.Default;
            scaling.EnemyHealthGrowth = balance.EnemyHealthGrowth;
            scaling.EnemyGoldGrowth = balance.EnemyGoldGrowth;
            scaling.EnemyAttackGrowth = balance.EnemyAttackGrowth;
            scaling.ScaleEnemyDefenseWithStage = balance.ScaleEnemyDefenseWithStage;
            scaling.MinDamageRatio = balance.MinDamageRatio;
            scaling.CriticalChance = balance.CriticalChance;
            scaling.CriticalDamageMultiplier = balance.CriticalDamageMultiplier;
            scaling.PaceMultiplier = balance.CombatPaceMultiplier;

            return scaling.Sanitized();
        }
    }
}
