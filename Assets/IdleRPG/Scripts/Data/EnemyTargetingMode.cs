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
        Random = 2
    }
}