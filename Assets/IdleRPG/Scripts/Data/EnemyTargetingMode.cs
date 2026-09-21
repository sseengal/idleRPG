namespace IdleRPG.Data
{
    /// <summary>How the current enemy chooses which hero to attack.</summary>
    public enum EnemyTargetingMode
    {
        /// <summary>Lane order: the first alive hero tanks (Knight -> Archer -> Mage).</summary>
        FrontMost = 0,

        /// <summary>Attacks whichever alive hero has the lowest health percentage.</summary>
        LowestHealthPercent = 1,

        /// <summary>Deterministic pseudo-random pick among alive heroes.</summary>
        Random = 2,

        /// <summary>
        /// Archetype default: use the wave's global rule from <see cref="BalanceConfig.EnemyTargeting"/>.
        /// This is what an enemy that does not care about positioning keeps.
        /// </summary>
        Inherit = 3,

        /// <summary>
        /// Reaches over the front rank and hits the back rank first - the ranged counter to hiding behind a tank
        /// (Step 11). Once the back rank is empty it falls through to the front rank.
        /// </summary>
        BacklineFirst = 4
    }
}