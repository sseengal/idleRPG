namespace IdleRPG.Sim
{
    /// <summary>What a boon DOES when it fires. The whole fight-side vocabulary, in one enum.</summary>
    public enum PerkEffectOp
    {
        /// <summary>Deal a fraction of the hit to the other enemies in the wave (Cleave).</summary>
        DamageOthers = 0,

        /// <summary>Strike the same target again for a fraction of the owner's attack (Battle Echo).</summary>
        ExtraHit = 1,

        /// <summary>Deal a fraction of the damage just taken back to whoever dealt it (Thorned Mail).</summary>
        Reflect = 2,

        /// <summary>Heal the owner for a fraction of the damage just dealt (Vampiric Edge).</summary>
        Lifesteal = 3,

        /// <summary>Heal the owner for a fraction of its own maximum health (Last Stand).</summary>
        HealSelf = 4,

        /// <summary>Shield the owner for a fraction of its own maximum health (Soul Ward).</summary>
        ShieldSelf = 5
    }

    /// <summary>Who the effect lands on, when it is not the obvious target.</summary>
    public enum PerkTargets
    {
        /// <summary>The unit the trigger named (the victim, or the attacker for a reflect).</summary>
        Trigger = 0,

        /// <summary>The other living enemies in the same wave (cleave).</summary>
        OtherEnemies = 1,

        /// <summary>The owner itself (heals, shields).</summary>
        Self = 2
    }

    /// <summary>
    /// One compiled boon effect, as the fight sees it: no ScriptableObject, no strings, no Unity.
    ///
    /// ELI5: "when this happens, do that, this strongly, this often". The card's words live in the Unity layer; this
    /// is the same thing written as numbers so the fight can act on it.
    /// </summary>
    public struct PerkEffect
    {
        public PerkTrigger Trigger;

        public PerkEffectOp Op;

        /// <summary>Size of the effect: a fraction of the hit, of the attack, or of max health.</summary>
        public double Amount;

        /// <summary>Chance to fire, 0..1. 1 = always.</summary>
        public double Chance;

        public PerkTargets Targets;

        public PerkEffect(PerkTrigger trigger, PerkEffectOp op, double amount, double chance = 1d,
            PerkTargets targets = PerkTargets.Trigger)
        {
            Trigger = trigger;
            Op = op;
            Amount = amount;
            Chance = chance;
            Targets = targets;
        }

        public double SafeAmount => Amount < 0d ? 0d : (Amount > 10d ? 10d : Amount);

        public double SafeChance => Chance < 0d ? 0d : (Chance > 1d ? 1d : Chance);
    }
}
