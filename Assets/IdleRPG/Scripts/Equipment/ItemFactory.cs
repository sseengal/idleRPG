using System;
using IdleRPG.Data;

namespace IdleRPG.Equipment
{
    /// <summary>
    /// Rolls a new piece of equipment from a boss kill. Everything about the roll (chance, weights, curve) is
    /// data-driven from <see cref="BalanceConfig"/> so the balance pass is a renumber, never a rewrite.
    ///
    /// MVP model: one stat per item (the slot's primary). The instance carries a single value on purpose -
    /// this is the extension point for affixes/set bonuses later (the resolver only reads the summed fraction,
    /// so combat never changes shape).
    /// </summary>
    public static class ItemFactory
    {
        private static readonly float[] DefaultRarityWeights = { 55f, 28f, 12f, 5f };

        /// <summary>Creates one random but deterministic-drop item for the given stage.</summary>
        public static ItemInstance Create(System.Random rng, BalanceConfig balance, int stage)
        {
            if (rng == null)
            {
                rng = new System.Random();
            }

            int slotIndex = rng.Next(0, 3);                       // Weapon / Armor / Trinket
            int roleIndex = rng.Next(0, 3);                       // Tank / Damage / Support
            ItemRarity rarity = RollRarity(rng, balance);

            int level = stage < 1 ? 1 : stage;
            double statFraction = StatFraction(balance, level) * rarity.StatMultiplier();
            double price = Price(balance, level) * rarity.PriceMultiplier();

            return new ItemInstance(
                Guid.NewGuid().ToString("N"),
                (ItemSlotType)slotIndex,
                rarity,
                (HeroRole)roleIndex,
                level,
                statFraction,
                price);
        }

        /// <summary>Base bonus fraction at an item level, before rarity: base + perStage * level, capped.</summary>
        public static double StatFraction(BalanceConfig balance, int level)
        {
            double baseValue = balance != null ? balance.GearStatBaseFraction : 0.015d;
            double perStage = balance != null ? balance.GearStatPerStage : 0.0012d;
            double cap = balance != null ? balance.GearStatMaxFraction : 0.25d;

            double raw = baseValue + perStage * (level < 1 ? 1 : level);
            return raw > cap ? cap : raw;
        }

        /// <summary>Gold price of a Common item at this level, before rarity.</summary>
        public static double Price(BalanceConfig balance, int level)
        {
            double baseValue = balance != null ? balance.GearPriceBase : 2d;
            double perStage = balance != null ? balance.GearPricePerStage : 0.9d;
            return baseValue + perStage * (level < 1 ? 1 : level);
        }

        private static ItemRarity RollRarity(System.Random rng, BalanceConfig balance)
        {
            float[] weights = balance != null ? balance.GearRarityWeights : null;
            if (weights == null || weights.Length == 0)
            {
                weights = DefaultRarityWeights;
            }

            double total = 0d;
            for (int i = 0; i < weights.Length; i++)
            {
                total += weights[i] < 0f ? 0f : weights[i];
            }

            double roll = rng.NextDouble() * (total > 0d ? total : 100d);
            for (int i = 0; i < weights.Length; i++)
            {
                roll -= weights[i];
                if (roll <= 0d)
                {
                    return (ItemRarity)i;
                }
            }

            return (ItemRarity)(weights.Length - 1);
        }
    }
}