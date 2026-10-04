using IdleRPG.Data;

namespace IdleRPG.Equipment
{
    /// <summary>
    /// A single dropped piece of gear. Rolled procedurally at drop time (class + slot + rarity + stage), so the
    /// instance carries everything - combat asks for the summed bonus via <see cref="ItemService"/>, never for
    /// the item itself. Stat bonuses are fractions of the hero's BASE stat (0.03 = +3%).
    /// </summary>
    public sealed class ItemInstance
    {
        public string InstanceId { get; }

        public ItemSlotType SlotType { get; }

        public ItemRarity Rarity { get; }

        /// <summary>Only heroes of this class may equip it.</summary>
        public HeroRole Role { get; }

        /// <summary>The stage it dropped from (drives the roll strength).</summary>
        public int Level { get; }

        /// <summary>Bonus as a fraction of the hero's base stat for this slot's primary stat.</summary>
        public double StatFraction { get; }

        /// <summary>Gold value at drop; salvaging pays a fraction of it.</summary>
        public double Price { get; }

        public ItemInstance(string instanceId, ItemSlotType slotType, ItemRarity rarity, HeroRole role,
            int level, double statFraction, double price)
        {
            InstanceId = string.IsNullOrEmpty(instanceId) ? System.Guid.NewGuid().ToString("N") : instanceId;
            SlotType = slotType;
            Rarity = rarity;
            Role = role;
            Level = level < 1 ? 1 : level;
            StatFraction = statFraction < 0d ? 0d : statFraction;
            Price = price < 0d ? 0d : price;
        }

        /// <summary>"+3.4% ATK" - the number the player compares.</summary>
        public string PercentText => (StatFraction * 100d).ToString("0.#") + "%";

        /// <summary>Short row label: "Common Weapon   +3.4% ATK".</summary>
        public string ShortLabel => Rarity.DisplayName() + " " + SlotType.DisplayName();

        /// <summary>Drop announcement: "Legendary Trinket (+3.6% HP) for Tank".</summary>
        public string DropSummary => ShortLabel + " (+" + PercentText + " " + SlotType.PrimaryStatLabel() + ") for " + Role;
    }
}