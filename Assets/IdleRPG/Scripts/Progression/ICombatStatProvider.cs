using IdleRPG.Data;

namespace IdleRPG.Progression
{
    /// <summary>
    /// Supplies the *derived* combat stats of a hero. Step 2 ships
    /// <see cref="DefaultStatProvider"/> (base stats only); Step 3 swaps in the
    /// upgrade + prestige aware provider without touching any combat code.
    /// </summary>
    public interface ICombatStatProvider
    {
        /// <summary>Max HP after hero levels and global health multipliers.</summary>
        double GetMaxHealth(HeroData hero, int heroIndex);

        /// <summary>Attack after hero levels and global damage multipliers.</summary>
        double GetAttack(HeroData hero, int heroIndex);

        /// <summary>Defence after hero levels.</summary>
        double GetDefense(HeroData hero, int heroIndex);

        /// <summary>Seconds between attacks.</summary>
        double GetAttackInterval(HeroData hero, int heroIndex);

        /// <summary>Global damage multiplier from permanent upgrades (1 = none).</summary>
        double GlobalDamageMultiplier { get; }

        /// <summary>Global health multiplier from permanent upgrades (1 = none).</summary>
        double GlobalHealthMultiplier { get; }

        /// <summary>Global gold multiplier from permanent upgrades (1 = none).</summary>
        double GlobalGoldMultiplier { get; }
    }
}