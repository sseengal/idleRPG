namespace IdleRPG.Equipment
{
    /// <summary>How rare (and therefore how strong + expensive) a piece of gear is.</summary>
    public enum ItemRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3
    }

    public static class ItemRarityExtensions
    {
        public static string DisplayName(this ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare:
                    return "Rare";
                case ItemRarity.Epic:
                    return "Epic";
                case ItemRarity.Legendary:
                    return "Legendary";
                default:
                    return "Common";
            }
        }

        /// <summary>Multiplies the rolled stat fraction (1x / 1.6x / 2.4x / 3.6x).</summary>
        public static double StatMultiplier(this ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare:
                    return 1.6d;
                case ItemRarity.Epic:
                    return 2.4d;
                case ItemRarity.Legendary:
                    return 3.6d;
                default:
                    return 1d;
            }
        }

        /// <summary>Multiplies the drop's gold price (1x / 2x / 4x / 8x).</summary>
        public static double PriceMultiplier(this ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare:
                    return 2d;
                case ItemRarity.Epic:
                    return 4d;
                case ItemRarity.Legendary:
                    return 8d;
                default:
                    return 1d;
            }
        }
    }
}