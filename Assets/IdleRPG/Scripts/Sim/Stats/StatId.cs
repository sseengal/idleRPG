using System;
using System.Collections.Generic;

namespace IdleRPG.Sim
{
    /// <summary>
    /// The stat id vocabulary. Ids are strings because they are the **save keys**
    /// (`SaveData.statLevels[].key`) and the UI/content lookup keys - never enum values.
    /// <see cref="Slot"/> maps an id to a small dense index so runtime reads are array lookups.
    /// </summary>
    public static class StatId
    {
        public const string Hp = "hp";
        public const string Attack = "atk";
        public const string Defense = "def";
        public const string Armor = "armor";
        public const string ArmorPen = "armorPen";
        public const string Crit = "crit";
        public const string CritDamage = "critDmg";
        public const string Haste = "haste";
        public const string Lifesteal = "lifesteal";
        public const string DamageDealt = "dmgDealt";
        public const string DamageTaken = "dmgTaken";
        public const string GoldFind = "goldFind";

        // Dense indices (order is free to change as long as Slot/NameOf stay in sync).
        public const int SlotHp = 0;
        public const int SlotAttack = 1;
        public const int SlotDefense = 2;
        public const int SlotArmor = 3;
        public const int SlotArmorPen = 4;
        public const int SlotCrit = 5;
        public const int SlotCritDamage = 6;
        public const int SlotHaste = 7;
        public const int SlotLifesteal = 8;
        public const int SlotDamageDealt = 9;
        public const int SlotDamageTaken = 10;
        public const int SlotGoldFind = 11;

        public const int Count = 12;

        /// <summary>Dense slot for an id, or -1 when unknown.</summary>
        public static int Slot(string id)
        {
            switch (id)
            {
                case Hp: return SlotHp;
                case Attack: return SlotAttack;
                case Defense: return SlotDefense;
                case Armor: return SlotArmor;
                case ArmorPen: return SlotArmorPen;
                case Crit: return SlotCrit;
                case CritDamage: return SlotCritDamage;
                case Haste: return SlotHaste;
                case Lifesteal: return SlotLifesteal;
                case DamageDealt: return SlotDamageDealt;
                case DamageTaken: return SlotDamageTaken;
                case GoldFind: return SlotGoldFind;
                default: return -1;
            }
        }

        public static string NameOf(int slot)
        {
            switch (slot)
            {
                case SlotHp: return Hp;
                case SlotAttack: return Attack;
                case SlotDefense: return Defense;
                case SlotArmor: return Armor;
                case SlotArmorPen: return ArmorPen;
                case SlotCrit: return Crit;
                case SlotCritDamage: return CritDamage;
                case SlotHaste: return Haste;
                case SlotLifesteal: return Lifesteal;
                case SlotDamageDealt: return DamageDealt;
                case SlotDamageTaken: return DamageTaken;
                case SlotGoldFind: return GoldFind;
                default: return "unknown";
            }
        }

        private static readonly string[] Names =
        {
            Hp, Attack, Defense, Armor, ArmorPen, Crit, CritDamage,
            Haste, Lifesteal, DamageDealt, DamageTaken, GoldFind
        };

        /// <summary>Every known id, in slot order (used by tools and validation).</summary>
        public static IReadOnlyList<string> All => Names;
    }
}
