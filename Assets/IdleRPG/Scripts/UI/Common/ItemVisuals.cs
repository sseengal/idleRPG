using IdleRPG.Data;
using IdleRPG.Equipment;
using UnityEngine;

namespace IdleRPG.UI
{
    /// <summary>
    /// One home for how items look everywhere (Roster preview, Inventory grid, sheets):
    /// rarity -> colour, slot -> letter/name/stat label, class -> letter. Keeping this in a single place
    /// means new inventory surfaces paint the same vocabulary without copying it again.
    /// </summary>
    public static class ItemVisuals
    {
        private static readonly Color RarityCommonColor = new Color(0.60f, 0.63f, 0.68f, 1f);
        private static readonly Color RarityRareColor = new Color(0.35f, 0.68f, 0.95f, 1f);
        private static readonly Color RarityEpicColor = new Color(0.72f, 0.52f, 0.92f, 1f);
        private static readonly Color RarityLegendaryColor = new Color(0.95f, 0.72f, 0.30f, 1f);

        /// <summary>Rarity tint used for tiles, rings and preview slots.</summary>
        public static Color RarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare:
                    return RarityRareColor;
                case ItemRarity.Epic:
                    return RarityEpicColor;
                case ItemRarity.Legendary:
                    return RarityLegendaryColor;
                default:
                    return RarityCommonColor;
            }
        }

        /// <summary>Compact placeholder tile letter.</summary>
        public static string SlotLetter(ItemSlotType slot)
        {
            switch (slot)
            {
                case ItemSlotType.Weapon:
                    return "W";
                case ItemSlotType.Armor:
                    return "A";
                default:
                    return "T";
            }
        }

        /// <summary>Compact stat label for the slot's primary stat (ATK / DEF / HP).</summary>
        public static string StatLabel(ItemSlotType slot)
        {
            return ItemSlotTypeExtensions.PrimaryStatLabel(slot);
        }

        /// <summary>One-letter class tag (T / D / S).</summary>
        public static char RoleLetter(HeroRole role)
        {
            return role.ToString()[0];
        }
    }
}