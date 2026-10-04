using IdleRPG.Data;

namespace IdleRPG.Equipment
{
    /// <summary>
    /// The three typed gear slots a hero can wear. Each slot type boosts exactly ONE primary stat, which is
    /// what makes "best item" well-defined (max ATK weapon, max DEF armor, max HP trinket) and stops a
    /// degenerate loadout of three ATK sticks.
    /// </summary>
    public enum ItemSlotType
    {
        /// <summary>Lift in your hand. Boosts ATK.</summary>
        Weapon = 0,

        /// <summary>Worn. Boosts DEF.</summary>
        Armor = 1,

        /// <summary>Carried. Boosts HP.</summary>
        Trinket = 2
    }

    public static class ItemSlotTypeExtensions
    {
        /// <summary>The hero stat a full slot of this type contributes to (weapon = attack, etc.).</summary>
        public static HeroStatType PrimaryStat(this ItemSlotType slotType)
        {
            switch (slotType)
            {
                case ItemSlotType.Weapon:
                    return HeroStatType.Attack;
                case ItemSlotType.Armor:
                    return HeroStatType.Defense;
                default:
                    return HeroStatType.Health;
            }
        }

        /// <summary>Short label used on the gear row (W / A / T) and in drop text.</summary>
        public static string DisplayName(this ItemSlotType slotType)
        {
            switch (slotType)
            {
                case ItemSlotType.Weapon:
                    return "Weapon";
                case ItemSlotType.Armor:
                    return "Armor";
                default:
                    return "Trinket";
            }
        }

        /// <summary>Compact stat label shown next to the item's percent (ATK / DEF / HP).</summary>
        public static string PrimaryStatLabel(this ItemSlotType slotType)
        {
            switch (slotType)
            {
                case ItemSlotType.Weapon:
                    return "ATK";
                case ItemSlotType.Armor:
                    return "DEF";
                default:
                    return "HP";
            }
        }
    }
}