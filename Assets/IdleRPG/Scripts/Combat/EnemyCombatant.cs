using IdleRPG.Data;
using IdleRPG.Progression;

namespace IdleRPG.Combat
{
    /// <summary>
    /// Runtime state of the enemy currently being fought. Pure C#; stats are derived
    /// from <see cref="EnemyData"/> through the shared stage-scaling formulas.
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

        public double MaxHealth { get; private set; }

        public double CurrentHealth { get; private set; }

        public double Attack { get; private set; }

        public double Defense { get; private set; }

        public double AttackIntervalSec { get; private set; }

        /// <summary>Gold dropped on death (already includes boss + prestige multipliers).</summary>
        public double GoldReward { get; private set; }

        public bool IsAlive => CurrentHealth > 0d;

        public double HealthPercent => MaxHealth <= 0d ? 0d : CurrentHealth / MaxHealth;

        public string DisplayName => Data == null ? "Enemy" : Data.EnemyName;

        /// <summary>
        /// Creates a scaled enemy for a stage. Returns null when data is missing so the
        /// caller can decide how to fail safely.
        /// </summary>
        public static EnemyCombatant Create(EnemyData data, int stage, bool isBoss, CombatScaling scaling, double externalGoldMultiplier)
        {
            if (data == null)
            {
                UnityEngine.Debug.LogError("[EnemyCombatant] Cannot spawn an enemy from a null EnemyData.");
                return null;
            }

            int safeStage = stage < 1 ? 1 : stage;

            double maxHealth = FormulaUtility.EnemyMaxHealth(data.BaseHealth, safeStage, scaling.EnemyHealthGrowth);
            if (isBoss)
            {
                maxHealth *= data.BossHealthMultiplier;
            }

            double defense = data.BaseDefense;
            if (scaling.ScaleEnemyDefenseWithStage)
            {
                defense = FormulaUtility.ScaleByStage(defense, safeStage, scaling.EnemyAttackGrowth);
            }

            EnemyCombatant enemy = new EnemyCombatant
            {
                Data = data,
                Stage = safeStage,
                IsBoss = isBoss,
                MaxHealth = FormulaUtility.Sanitize(maxHealth, 1d),
                Attack = FormulaUtility.Sanitize(FormulaUtility.EnemyAttack(data.BaseAttack, safeStage, scaling.EnemyAttackGrowth)),
                Defense = FormulaUtility.Sanitize(defense),
                AttackIntervalSec = data.AttackIntervalSec * scaling.PaceMultiplier,
                GoldReward = CombatRewardCalculator.CalculateGold(data, safeStage, isBoss, scaling, externalGoldMultiplier)
            };

            if (enemy.MaxHealth < 1d)
            {
                enemy.MaxHealth = 1d;
            }

            enemy.CurrentHealth = enemy.MaxHealth;
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