using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Sim;

namespace IdleRPG.Combat
{
    /// <summary>
    /// An enemy in the fight: the shared <see cref="Combatant"/> sheet plus its definition and the
    /// stage-scaling stored on it.
    ///
    /// ELI5: enemies are built from their data asset (sprite, base numbers) and then scaled up by the stage
    /// number. All of that maths happens here, once, before the fight starts.
    /// </summary>
    public sealed class EnemyCombatant : Combatant
    {
        private EnemyCombatant(string id, string displayName, EnemyData data, int stage, StatBlock stats, double attackIntervalSec)
            : base(id, displayName, CombatantSide.Enemy, stats, attackIntervalSec, MinInterval)
        {
            Data = data;
            Stage = stage;
        }

        /// <summary>No extra interval clamp for enemies (their pace scaling may go below the hero floor).</summary>
        private const double MinInterval = 0.01d;

        /// <summary>Static definition (name, sprite, tint).</summary>
        public EnemyData Data { get; private set; }

        public int Stage { get; private set; }

        /// <summary>Legacy entry point: builds the rule snapshot from the MVP scaling struct.</summary>
        public static EnemyCombatant Create(EnemyData data, int stage, bool isBoss, CombatScaling scaling, double externalGoldMultiplier)
        {
            return Create(data, stage, isBoss, SimRulesFactory.FromScaling(scaling), externalGoldMultiplier, scaling);
        }

        /// <summary>Rule-snapshot overload (Step 7a+).</summary>
        public static EnemyCombatant Create(EnemyData data, int stage, bool isBoss, SimRules rules, double externalGoldMultiplier)
        {
            return Create(data, stage, isBoss, rules, externalGoldMultiplier, null);
        }

        private static EnemyCombatant Create(EnemyData data, int stage, bool isBoss, SimRules rules, double externalGoldMultiplier, CombatScaling? legacyScaling)
        {
            if (data == null)
            {
                SimLog.LogError("[EnemyCombatant] Cannot spawn an enemy from a null EnemyData.");
                return null;
            }

            rules = rules.Sanitized();
            int safeStage = stage < 1 ? 1 : stage;

            double maxHealth = FormulaUtility.EnemyMaxHealth(data.BaseHealth, safeStage, rules.EnemyHealthGrowth);
            if (isBoss)
            {
                maxHealth *= data.BossHealthMultiplier;
            }

            double defense = data.BaseDefense;
            if (rules.ScaleEnemyDefenseWithStage)
            {
                defense = FormulaUtility.ScaleByStage(defense, safeStage, rules.EnemyAttackGrowth);
            }

            StatBlock stats = new StatBlock(
                FormulaUtility.Sanitize(maxHealth, 1d),
                FormulaUtility.Sanitize(FormulaUtility.EnemyAttack(data.BaseAttack, safeStage, rules.EnemyAttackGrowth)),
                FormulaUtility.Sanitize(defense));

            double interval = data.AttackIntervalSec * rules.PaceMultiplier;   // parity: no extra clamp

            EnemyCombatant enemy = new EnemyCombatant(data.EnemyID, data.EnemyName, data, safeStage, stats, interval)
            {
                IsBoss = isBoss,
                GoldReward = legacyScaling.HasValue
                    ? CombatRewardCalculator.CalculateGold(data, safeStage, isBoss, legacyScaling.Value, externalGoldMultiplier)
                    : CombatRewardCalculator.CalculateGold(data, safeStage, isBoss, rules, externalGoldMultiplier)
            };

            return enemy;
        }
    }
}
