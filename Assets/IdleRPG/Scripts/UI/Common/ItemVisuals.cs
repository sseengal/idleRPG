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
        private static readonly Color RarityCommonColor = UiTheme.TextDim;
        private static readonly Color RarityRareColor = UiTheme.ItemBlue;
        private static readonly Color RarityEpicColor = UiTheme.ItemPurple;
        private static readonly Color RarityLegendaryColor = UiTheme.ItemGold;

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

        /// <summary>Gear-slot icon from the skin; null falls back to the slot letter.</summary>
        public static Sprite SlotIcon(ItemSlotType slot)
        {
            UiSkin skin = UiSkin.Current;

            switch (slot)
            {
                case ItemSlotType.Weapon:
                    return skin.weaponIcon;
                case ItemSlotType.Armor:
                    return skin.armorIcon;
                default:
                    return skin.trinketIcon;
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