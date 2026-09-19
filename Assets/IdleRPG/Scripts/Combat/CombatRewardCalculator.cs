using IdleRPG.Data;
using IdleRPG.Progression;

namespace IdleRPG.Combat
{
    /// <summary>
    /// Pure gold maths for a kill: base drop * stage growth * boss multiplier * prestige bonus.
    /// Kept separate from <see cref="EnemyCombatant"/> so the offline estimator can reuse it.
    /// </summary>
    public static class CombatRewardCalculator
    {
        /// <summary>Gold dropped by an enemy at a stage. Never negative, never NaN.</summary>
        public static double CalculateGold(EnemyData enemy, int stage, bool isBoss, CombatScaling scaling, double externalGoldMultiplier)
        {
            if (enemy == null)
            {
                return 0d;
            }

            double gold = FormulaUtility.EnemyGoldDrop(enemy.BaseGoldDrop, stage, scaling.EnemyGoldGrowth);

            if (isBoss)
            {
                gold *= enemy.BossGoldMultiplier;
            }

            if (externalGoldMultiplier > 0d)
            {
                gold *= externalGoldMultiplier;
            }

            return FormulaUtility.Sanitize(gold);
        }

        /// <summary>
        /// Gem reward for a boss kill. Gems are a design-level currency, so this is an
        /// integer count rather than a scaled value.
        /// </summary>
        public static int CalculateGems(BalanceConfig balanceConfig, bool isBoss)
        {
            if (balanceConfig == null || !isBoss)
            {
                return 0;
            }

            return balanceConfig.GemsPerBossKill;
        }
    }
}