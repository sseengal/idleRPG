namespace IdleRPG.Sim
{
    /// <summary>
    /// What an ability does when it lands. Three shapes cover the whole catalogue, and the fight has one case per
    /// shape - so a new ability is new data, never a new branch inside a view.
    /// </summary>
    public enum SimAbilityKind
    {
        /// <summary>Deals damage, scaled off the caster's attack.</summary>
        Damage = 0,

        /// <summary>Restores a fraction of the target's maximum health.</summary>
        Heal = 1,

        /// <summary>Reduces incoming damage for a few seconds.</summary>
        Shield = 2
    }

    /// <summary>Who an ability lands on. "Enemy"/"hero" always means the side opposite the caster.</summary>
    public enum SimAbilityTarget
    {
        /// <summary>The caster itself.</summary>
        Self = 0,

        /// <summary>Every living member of the caster's side.</summary>
        Party = 1,

        /// <summary>The friendly unit with the lowest health fraction.</summary>
        LowestAlly = 2,

        /// <summary>The front-most living enemy.</summary>
        FrontEnemy = 3,

        /// <summary>The front-most living hero.</summary>
        FrontHero = 4
    }

    /// <summary>
    /// One ability as the fight sees it: plain values only.
    ///
    /// ELI5: the referee (the fight code) cannot open the game's data files, so the shop counter
    /// (`CombatSimulator`) copies everything the referee needs onto a small card: what the move does, who it lands on,
    /// how long the animation runs before the hit, and the numbers. A new ability only changes this card.
    /// </summary>
    public struct SimAbility
    {
        public string Id;
        public string DisplayName;

        /// <summary>Art slot that plays the cast ("Ability", "Attack02"). Empty = no clip.</summary>
        public string ClipSlot;

        public double CooldownSec;
        public SimAbilityKind Kind;
        public SimAbilityTarget Target;

        /// <summary>Damage taken is multiplied by this while the guard is up (0.5 = half damage).</summary>
        public double ShieldFactor;

        public double ShieldSeconds;

        /// <summary>Fraction of the target's MAXIMUM health restored.</summary>
        public double HealFraction;

        /// <summary>Sprite sheet id under Resources/Vfx (e.g. "ice_shatter"); empty = no impact effect.</summary>
        public string VfxId;

        /// <summary>How long before the damage the effect should start (0 = at impact).</summary>
        public double ImpactDelaySeconds;

        /// <summary>Size of the effect relative to the shared base (1 = standard).</summary>
        public float VfxScale;

        /// <summary>Multiplier applied to the caster's attack for a Damage ability.</summary>
        public double DamageMultiplier;

        /// <summary>
        /// Travel time from the announce to the impact, measured from the art up to that clip's strike frame.
        /// 0 = no measured clip, so the fight uses the caster's own swing window.
        /// </summary>
        public double WindowSeconds;

        /// <summary>An empty card: this unit has nothing to cast.</summary>
        public static SimAbility None => default;

        public bool IsValid => !string.IsNullOrEmpty(Id);
    }
}
