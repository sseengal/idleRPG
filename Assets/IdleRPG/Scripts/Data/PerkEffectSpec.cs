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
        /// <summary>Every effect a boon may carry. Each one maps onto a single stat the resolver already owns.</summary>
        public enum Op
        {
            AttackPercent = 0,
            HealthPercent = 1,
            DefensePercent = 2,
            GoldPercent = 3,
            AttackSpeedPercent = 4,
            CritChanceFlat = 5,
            CritDamageFlat = 6
        }

        [SerializeField] private Op operation = Op.AttackPercent;
        [SerializeField] private float amount = 0.1f;

        public Op Operation => operation;

        public double Amount => amount;

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
                default: return value;
            }
        }
    }
}
