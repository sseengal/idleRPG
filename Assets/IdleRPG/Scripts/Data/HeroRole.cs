namespace IdleRPG.Data
{
    /// <summary>
    /// What a hero is for. Auto-arrange reads it (tanks to the front row first) and the Team screen shows it as
    /// a tag; the sim itself never branches on it - it reads stats and the target rule.
    /// </summary>
    public enum HeroRole
    {
        Tank = 0,
        Damage = 1,
        Support = 2
    }
}
