using IdleRPG.Data;
using IdleRPG.Sim;

namespace IdleRPG.Combat
{
    /// <summary>
    /// Runtime state of one party member during a fight. Pure C#: no MonoBehaviour, no scene lookup -
    /// the same object runs in the live game, a fast-forward or an offline estimate.
    ///
    /// Stats live in a <see cref="StatBlock"/> (Step 7a) so new stats can be added without touching this
    /// class. The legacy properties are thin aliases kept for existing callers (UI, combat log, probes).
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

            Stats = new StatBlock(ClampHealth(maxHealth), ClampStat(attack), ClampStat(defense));
            Stats.Sanitize();
            AttackIntervalSec = ClampInterval(attackIntervalSec);

            CurrentHealth = Stats.MaxHealth;
            // Stagger the first hit so all three heroes never swing on the same frame.
            attackTimer = AttackIntervalSec * 0.5d;
        }

        /// <summary>Derived stats (hp/atk/def today; more stats from Step 12 on).</summary>
        public StatBlock Stats { get; private set; }

        public double MaxHealth => Stats.MaxHealth;

        public double CurrentHealth { get; private set; }

        public double Attack => Stats.Attack;

        public double Defense => Stats.Defense;

        public double AttackIntervalSec { get; private set; }

        public bool IsAlive => CurrentHealth > 0d;

        /// <summary>Remaining health as 0..1 (safe when max health is zero).</summary>
        public double HealthPercent => MaxHealth <= 0d ? 0d : CurrentHealth / MaxHealth;

        public string DisplayName => Data == null ? "Hero " + Index : Data.HeroName;

        /// <summary>
        /// Re-applies derived stats (hero levels / prestige upgrades). Keeps the current health percentage
        /// so an upgrade bought mid-fight cannot heal or kill the hero.
        /// </summary>
        public void ApplyStats(double maxHealth, double attack, double defense, double attackIntervalSec)
        {
            StatBlock stats = new StatBlock(ClampHealth(maxHealth), ClampStat(attack), ClampStat(defense));
            stats.Sanitize();
            ApplyStats(stats, attackIntervalSec);
        }

        /// <summary>Stat-block overload: the pipeline may hand in any stat set.</summary>
        public void ApplyStats(StatBlock stats, double attackIntervalSec)
        {
            if (stats == null)
            {
                return;
            }

            double ratio = MaxHealth > 0d ? CurrentHealth / MaxHealth : 1d;

            Stats.CopyFrom(stats);
            Stats.Sanitize();
            Stats.MaxHealth = ClampHealth(Stats.MaxHealth);
            AttackIntervalSec = ClampInterval(attackIntervalSec);

            // Behaviour preserved from the MVP: a living hero keeps their health percentage, and a hero at
            // exactly 0% is restored to full. (Step 7a is a structure-only refactor - the revive-on-upgrade
            // quirk is logged in Checklist.md and gets a proper decision in Step 12.)
            CurrentHealth = IsAlive || ratio > 0d
                ? ClampTo(ratio * Stats.MaxHealth, Stats.MaxHealth)
                : Stats.MaxHealth;
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
            CurrentHealth = Stats.MaxHealth;
            attackTimer = AttackIntervalSec * 0.5d;
        }

        private static double ClampHealth(double value)
        {
            return value < 1d ? 1d : value;
        }

        private static double ClampStat(double value)
        {
            return value < 0d ? 0d : value;
        }

        private static double ClampInterval(double value)
        {
            return value < HeroData.MinAttackIntervalSec ? HeroData.MinAttackIntervalSec : value;
        }

        private static double ClampTo(double value, double maxValue)
        {
            if (value < 0d)
            {
                return 0d;
            }

            return value > maxValue ? maxValue : value;
        }
    }
}
