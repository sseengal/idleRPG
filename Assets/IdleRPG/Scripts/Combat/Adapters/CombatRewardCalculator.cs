using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Sim;

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

        /// <summary>Rule-snapshot overload (Step 7a): same formula, no dependence on the legacy struct.</summary>
        public static double CalculateGold(EnemyData enemy, int stage, bool isBoss, SimRules rules, double externalGoldMultiplier)
        {
            if (enemy == null)
            {
                return 0d;
            }

            double gold = FormulaUtility.EnemyGoldDrop(enemy.BaseGoldDrop, stage, rules.Sanitized().EnemyGoldGrowth);

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
        /// Gem reward for clearing a stage. Gems are a design-level currency, so this is an integer count.
        ///
        /// The rule: only a FIRST-TIME clear of a milestone stage pays. The player bounces between their ceiling
        /// and the stage below it forever, so paying per boss kill would farm gems; paying on new bests only makes
        /// gems a pure progress reward.
        /// </summary>
        public static int CalculateMilestoneGems(BalanceConfig balanceConfig, int clearedStage, bool isNewBest)
        {
            if (balanceConfig == null || !isNewBest || clearedStage < 1)
            {
                return 0;
            }

            return clearedStage % balanceConfig.MilestoneStageInterval == 0 ? balanceConfig.GemsPerMilestone : 0;
        }
    }
}