using IdleRPG.Data;

namespace IdleRPG.Progression
{
    /// <summary>
    /// Baseline stat provider: hero base stats, no upgrades, no prestige bonuses.
    /// Used until <see cref="ICombatStatProvider"/> is replaced in Step 3.
    /// </summary>
    public sealed class DefaultStatProvider : ICombatStatProvider
    {
        /// <summary>Shared instance — the class is stateless.</summary>
        public static readonly DefaultStatProvider Instance = new DefaultStatProvider();

        public double GetMaxHealth(HeroData hero, int heroIndex)
        {
            return hero == null ? 0d : hero.BaseHealth;
        }

        public double GetAttack(HeroData hero, int heroIndex)
        {
            return hero == null ? 0d : hero.BaseAttack;
        }

        public double GetDefense(HeroData hero, int heroIndex)
        {
            return hero == null ? 0d : hero.BaseDefense;
        }

        public double GetAttackInterval(HeroData hero, int heroIndex)
        {
            return hero == null ? HeroData.MinAttackIntervalSec : hero.AttackIntervalSec;
        }

        public double GlobalDamageMultiplier => 1d;

        public double GlobalHealthMultiplier => 1d;

        public double GlobalGoldMultiplier => 1d;
    }
}