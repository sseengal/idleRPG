using System;
using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>How good a boon looks. Drives the tag colour and the draw weight, nothing else.</summary>
    public enum PerkRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3
    }

    /// <summary>
    /// One thing a boon does. Kept as a small record (op + amount) rather than a subclass per effect, so a new
    /// boon is a data asset and not a code change - the same rule the ability catalogue follows.
    ///
    /// Amounts are always "the size of the bonus": 0.12 means +12% for a percent op, and +12 points for the two
    /// flat ops (crit chance is a 0..1 fraction, crit damage is a multiplier above 1).
    /// </summary>
    [Serializable]
    public sealed class PerkEffectSpec
    {
        /// <summary>
        /// Every effect a boon may carry.
        ///
        /// Layer A (a stat the resolver already owns): Attack ... CritDamage, the first seven. They need no fight
        /// code at all.
        /// Layer B (a mechanic the fight fires): from CleaveToOthers on. These are compiled into the sim's own
        /// vocabulary by <see cref="IdleRPG.Progression.BoonRuntimeFactory"/> and need <see cref="Trigger"/>.
        /// </summary>
        public enum Op
        {
            AttackPercent = 0,
            HealthPercent = 1,
            DefensePercent = 2,
            GoldPercent = 3,
            AttackSpeedPercent = 4,
            CritChanceFlat = 5,
            CritDamageFlat = 6,

            // --- Layer B: mechanics ---
            CleaveToOthers = 10,
            ExtraStrike = 11,
            ReflectDamage = 12,
            Lifesteal = 13,
            HealSelf = 14,
            ShieldSelf = 15
        }

        /// <summary>True when this op is a mechanic the fight has to fire (layer B), not a stat.</summary>
        public static bool IsMechanic(Op operation)
        {
            return (int)operation >= 10;
        }

        [SerializeField] private Op operation = Op.AttackPercent;
        [SerializeField] private float amount = 0.1f;

        [Tooltip("When a mechanic fires. Ignored by stat ops.")]
        [SerializeField] private PerkTriggerKind trigger = PerkTriggerKind.OnHit;

        [Tooltip("Chance for a mechanic to fire, 1 = always. Ignored by stat ops.")]
        [Range(0f, 1f)]
        [SerializeField] private float chance = 1f;

        public Op Operation => operation;

        public double Amount => amount;

        /// <summary>"On hit: ", "On kill: " - the trigger word a mechanic's description starts with.</summary>
        private string When()
        {
            switch (trigger)
            {
                case PerkTriggerKind.OnKill: return "On kill: ";
                case PerkTriggerKind.OnHurt: return "When hit: ";
                case PerkTriggerKind.OnWaveStart: return "Each wave: ";
                default: return "On hit: ";
            }
        }

        public PerkTriggerKind Trigger => trigger;

        public double Chance => chance;

        /// <summary>True when this effect is a mechanic, so the fight needs it.</summary>
        public bool IsTrigger => IsMechanic(operation);

        /// <summary>"+12% attack", "+5% crit chance" - the shape every boon description uses.</summary>
        public string ToDisplayString()
        {
            double percent = amount * 100d;
            string value = (percent > 0d ? "+" : string.Empty) + percent.ToString("0.#") + "%";

            switch (operation)
            {
                case Op.AttackPercent: return value + " attack";
                case Op.HealthPercent: return value + " health";
                case Op.DefensePercent: return value + " defense";
                case Op.GoldPercent: return value + " gold";
                case Op.AttackSpeedPercent: return value + " attack speed";
                case Op.CritChanceFlat: return value + " crit chance";
                case Op.CritDamageFlat: return value + " crit damage";

                // Mechanics read as when-then, the way a card should: "On hit: 50% of the hit..." .
                case Op.CleaveToOthers: return When() + value + " of the hit to the other enemies";
                case Op.ExtraStrike: return When() + value + " attack strike again";
                case Op.ReflectDamage: return When() + value + " of damage taken reflected";
                case Op.Lifesteal: return When() + "heal " + value + " of damage dealt";
                case Op.HealSelf: return When() + "heal " + value + " of max health";
                case Op.ShieldSelf: return When() + "shield " + value + " of max health";
                default: return value;
            }
        }
    }
}
