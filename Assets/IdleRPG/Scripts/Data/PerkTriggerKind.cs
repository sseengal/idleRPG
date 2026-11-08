namespace IdleRPG.Data
{
    /// <summary>
    /// When a mechanic fires, in the content's own words.
    ///
    /// ELI5: a card says "on hit" or "on kill". This enum is that choice, and it is separate from the sim's
    /// <c>PerkTrigger</c> only because content must not depend on the simulation - the factory translates between
    /// the two, in one place.
    /// </summary>
    public enum PerkTriggerKind
    {
        OnHit = 0,
        OnKill = 1,
        OnHurt = 2,
        OnWaveStart = 3
    }
}
