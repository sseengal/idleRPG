using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Sim;

namespace IdleRPG.Combat
{
    /// <summary>
    /// Runtime state of the enemy being fought. Pure C#; stats are derived from <see cref="EnemyData"/>
    /// through the shared stage-scaling formulas and stored in a <see cref="StatBlock"/> (Step 7a) so the
    /// upcoming effect pipeline can read/write any stat without changing this class.
    /// </summary>
    public sealed class EnemyCombatant
    {
        private double attackTimer;

        private EnemyCombatant()
        {
        }

        /// <summary>Static definition (name, sprite, tint).</summary>
        public EnemyData Data { get; private set; }

        public int Stage { get; private set; }

        public bool IsBoss { get; private set; }

        /// <summary>Derived stats (hp/atk/def today; more stats from Step 12 on).</summary>
        public StatBlock Stats { get; private set; }

        public double MaxHealth => Stats.MaxHealth;

        public double CurrentHealth { get; private set; }

        public double Attack => Stats.Attack;

        public double Defense => Stats.Defense;

        public double AttackIntervalSec { get; private set; }

        /// <summary>Gold dropped on death (already includes boss + prestige multipliers).</summary>
        public double GoldReward { get; private set; }

        public bool IsAlive => CurrentHealth > 0d;

        public double HealthPercent => MaxHealth <= 0d ? 0d : CurrentHealth / MaxHealth;

        public string DisplayName => Data == null ? "Enemy" : Data.EnemyName;

        /// <summary>Legacy entry point: builds the rule snapshot from the MVP scaling struct.</summary>
        public static EnemyCombatant Create(EnemyData data, int stage, bool isBoss, CombatScaling scaling, double externalGoldMultiplier)
        {
            return Create(data, stage, isBoss, SimRulesFactory.FromScaling(scaling), externalGoldMultiplier, scaling);
        }

        /// <summary>
        /// Creates a scaled enemy from a frozen rule snapshot. Returns null when data is missing so the
        /// caller can decide how to fail safely.
        /// </summary>
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

            double interval = data.AttackIntervalSec * rules.PaceMultiplier;   // parity: no extra clamp

            EnemyCombatant enemy = new EnemyCombatant
            {
                Data = data,
                Stage = safeStage,
                IsBoss = isBoss,
                AttackIntervalSec = interval,
                GoldReward = legacyScaling.HasValue
                    ? CombatRewardCalculator.CalculateGold(data, safeStage, isBoss, legacyScaling.Value, externalGoldMultiplier)
                    : CombatRewardCalculator.CalculateGold(data, safeStage, isBoss, rules, externalGoldMultiplier)
            };

            enemy.Stats = new StatBlock(
                FormulaUtility.Sanitize(maxHealth, 1d),
                FormulaUtility.Sanitize(FormulaUtility.EnemyAttack(data.BaseAttack, safeStage, rules.EnemyAttackGrowth)),
                FormulaUtility.Sanitize(defense));
            enemy.Stats.Sanitize();
            enemy.Stats.MaxHealth = enemy.Stats.MaxHealth < 1d ? 1d : enemy.Stats.MaxHealth;

            enemy.CurrentHealth = enemy.Stats.MaxHealth;
            enemy.attackTimer = enemy.AttackIntervalSec * 0.5d;

            return enemy;
        }

        /// <summary>Advances the attack cooldown; true when the enemy swings this step.</summary>
        public bool Tick(double deltaTime)
        {
            if (!IsAlive || deltaTime <= 0d)
            {
                return false;
            }

            attackTimer -= deltaTime;
            if (attackTimer > 0d)
            {
                return false;
            }

            attackTimer += AttackIntervalSec;
            if (attackTimer <= 0d)
            {
                attackTimer = AttackIntervalSec;
            }

            return true;
        }

        /// <summary>Applies damage. Returns the damage actually dealt.</summary>
        public double TakeDamage(double damage)
        {
            if (damage <= 0d || !IsAlive)
            {
                return 0d;
            }

            double applied = damage > CurrentHealth ? CurrentHealth : damage;
            CurrentHealth -= applied;

            if (CurrentHealth < 0d)
            {
                CurrentHealth = 0d;
            }

            return applied;
        }
    }
}
