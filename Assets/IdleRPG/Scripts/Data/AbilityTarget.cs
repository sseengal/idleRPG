namespace IdleRPG.Data
{
    /// <summary>
    /// Who an ability's effect lands on. The caster's side is never asked for: a hero ability picks an enemy target
    /// and an enemy ability picks a hero target, which is what the "enemy" / "hero" cases mean.
    /// </summary>
    public enum AbilityTarget
    {
        /// <summary>The caster itself (Shield Wall, the enemies' Block).</summary>
        Self = 0,

        /// <summary>Every living member of the caster's side (Templar Bulwark).</summary>
        Party = 1,

        /// <summary>The friendly unit with the lowest health fraction (Priest Heal).</summary>
        LowestAlly = 2,

        /// <summary>The front-most living enemy - what the heroes normally attack anyway.</summary>
        FrontEnemy = 3,

        /// <summary>The front-most living hero (Beam, Spike).</summary>
        FrontHero = 4
    }
}
