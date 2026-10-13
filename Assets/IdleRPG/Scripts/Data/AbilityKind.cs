namespace IdleRPG.Data
{
    /// <summary>
    /// What an ability does when it fires (see Docs/Ability-Plan.md). Kept to three shapes for v1: the three cover
    /// every hero and enemy ability in the catalogue, and a new shape is a new case in the simulator's effect step.
    /// </summary>
    public enum AbilityKind
    {
        /// <summary>Reduces incoming damage for a while (Knight Shield Wall, Bulwark, the enemies' Block).</summary>
        Shield = 0,

        /// <summary>Restores health to a friendly target (Priest Heal).</summary>
        Heal = 1,

        /// <summary>Deals damage, scaled off the caster's attack (Volley, Fireball, Impale, Beam, Spike).</summary>
        Damage = 2
    }
}
