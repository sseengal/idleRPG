namespace IdleRPG.Sim
{
    /// <summary>
    /// When a boon fires. Part of the sim's vocabulary because the fight decides when something happens, and the
    /// Unity side only describes the boon - it never watches the fight.
    /// </summary>
    public enum PerkTrigger
    {
        /// <summary>The owner's attack landed: "on hit".</summary>
        OnHit = 0,

        /// <summary>The owner's attack killed an enemy: "on kill".</summary>
        OnKill = 1,

        /// <summary>The owner was hurt: "when hit". Thorns, second wind.</summary>
        OnHurt = 2,

        /// <summary>A wave started: "at the start of every wave".</summary>
        OnWaveStart = 3
    }
}
