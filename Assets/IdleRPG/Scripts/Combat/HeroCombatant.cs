using IdleRPG.Data;

namespace IdleRPG.Combat
{
    /// <summary>
    /// Runtime state of one party member during a fight. Pure C#: no MonoBehaviour,
    /// no scene lookup — the same object can run in the live game or in a simulation.
    /// </summary>
    public sealed class HeroCombatant
    {
        /// <summary>Lane index inside the party (0 = front, tanks first).</summary>
        public readonly int Index;

        /// <summary>Static definition (icon, id, name).</summary>
        public readonly HeroData Data;

        private double attackTimer;

        public HeroCombatant(int index, HeroData data, double maxHealth, double attack, double defense, double attackIntervalSec)
        {
            Index = index;
            Data = data;

            MaxHealth = maxHealth < 1d ? 1d : maxHealth;
            Attack = attack < 0d ? 0d : attack;
            Defense = defense < 0d ? 0d : defense;
            AttackIntervalSec = attackIntervalSec < HeroData.MinAttackIntervalSec ? HeroData.MinAttackIntervalSec : attackIntervalSec;

            CurrentHealth = MaxHealth;
            // Stagger the first hit so all three heroes never swing on the same frame.
            attackTimer = AttackIntervalSec * 0.5d;
        }

        public double MaxHealth { get; private set; }

        public double CurrentHealth { get; private set; }

        public double Attack { get; private set; }

        public double Defense { get; private set; }

        public double AttackIntervalSec { get; private set; }

        public bool IsAlive => CurrentHealth > 0d;

        /// <summary>Remaining health as 0..1 (safe when max health is zero).</summary>
        public double HealthPercent => MaxHealth <= 0d ? 0d : CurrentHealth / MaxHealth;

        public string DisplayName => Data == null ? $"Hero {Index}" : Data.HeroName;

        /// <summary>
        /// Re-applies derived stats (hero levels / prestige upgrades). Keeps the current
        /// health percentage so an upgrade bought mid-fight cannot heal or kill the hero.
        /// </summary>
        public void ApplyStats(double maxHealth, double attack, double defense, double attackIntervalSec)
        {
            double ratio = MaxHealth > 0d ? CurrentHealth / MaxHealth : 1d;

            MaxHealth = maxHealth < 1d ? 1d : maxHealth;
            Attack = attack < 0d ? 0d : attack;
            Defense = defense < 0d ? 0d : defense;
            AttackIntervalSec = attackIntervalSec < HeroData.MinAttackIntervalSec ? HeroData.MinAttackIntervalSec : attackIntervalSec;

            CurrentHealth = IsAlive || ratio > 0d
                ? Clamp(ratio * MaxHealth, MaxHealth)
                : MaxHealth;
        }

        /// <summary>Advances the attack cooldown; true when a swing is ready this step.</summary>
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

            // Carry the overshoot so cadence never drifts, and clamp to avoid death spirals.
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

        /// <summary>Full heal (stage advance, retry, or a fresh run).</summary>
        public void RestoreFullHealth()
        {
            CurrentHealth = MaxHealth;
            attackTimer = AttackIntervalSec * 0.5d;
        }

        private static double Clamp(double value, double maxValue)
        {
            if (value < 0d)
            {
                return 0d;
            }

            return value > maxValue ? maxValue : value;
        }
    }
}