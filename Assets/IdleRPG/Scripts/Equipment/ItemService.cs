using System;
using System.Collections.Generic;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Save;
using IdleRPG.Utils;
using UnityEngine;

namespace IdleRPG.Equipment
{
    /// <summary>Flat bonuses a hero's gear grants, as fractions of that hero's base stats.</summary>
    public readonly struct ItemBonuses
    {
        public readonly double Hp;
        public readonly double Atk;
        public readonly double Def;

        public ItemBonuses(double hp, double atk, double def)
        {
            Hp = hp;
            Atk = atk;
            Def = def;
        }
    }

    /// <summary>
    /// Owns everything the party wears and carries: the inventory (dropped, not yet worn), the three typed
    /// slots per hero, and every mutation path (drop / equip / swap / discard / salvage). The combat sim never
    /// sees this class - <see cref="StatResolver"/> reads the summed bonus fractions, so new affix types or
    /// set bonuses later change only this class and the resolver hook.
    ///
    /// Golden rule for drops: an item NEVER vanishes silently. If it must go, it is either salvaged into gold
    /// (upgrade unlocked) or announced as discarded, and every acquisition/equip lands in the battle log.
    /// </summary>
    public sealed partial class ItemService
    {
        public const int SlotCount = 3;

        private readonly BalanceConfig balance;
        private readonly PartyConfig party;
        private readonly EconomyManager economy;
        private readonly Func<PrestigeEffectType, int> utilityLevels;
        private readonly List<ItemInstance> inventory = new List<ItemInstance>();
        private readonly Dictionary<int, ItemInstance[]> equippedByHero = new Dictionary<int, ItemInstance[]>();
        private readonly System.Random rng = new System.Random();

        /// <summary>Raised after any mutation (drop, equip, discard, salvage) - save dirty + UI refresh.</summary>
        public event Action Changed;

        public ItemService(BalanceConfig balance, PartyConfig party, EconomyManager economy, Func<PrestigeEffectType, int> utilityLevels)
        {
            this.balance = balance;
            this.party = party;
            this.economy = economy;
            this.utilityLevels = utilityLevels;
        }

        /// <summary>The unworn items, bag order = drop order (newest last).</summary>
        public IReadOnlyList<ItemInstance> Inventory => inventory;

        public int InventoryCount => inventory.Count;

        public int InventoryCap => balance != null ? Mathf.Max(1, balance.InventoryCap) : 20;

        /// <summary>Major token upgrade: dropped gear auto-equips when it is strictly better.</summary>
        public bool AutoEquipUnlocked => utilityLevels != null && utilityLevels(PrestigeEffectType.AutoEquipGear) >= 1;

        /// <summary>Minor token upgrade: overflowing gear becomes gold instead of being lost.</summary>
        public bool AutoSalvageUnlocked => utilityLevels != null && utilityLevels(PrestigeEffectType.AutoSalvageGear) >= 1;

        public ItemInstance GetEquipped(int heroIndex, ItemSlotType slotType)
        {
            if (!equippedByHero.TryGetValue(heroIndex, out ItemInstance[] wear))
            {
                return null;
            }

            return wear[(int)slotType];
        }

        /// <summary>Every item the hero wears, slot order (weapon, armor, trinket) or null where empty.</summary>
        public ItemInstance[] GetEquippedAll(int heroIndex)
        {
            if (!equippedByHero.TryGetValue(heroIndex, out ItemInstance[] wear))
            {
                return new ItemInstance[SlotCount];
            }

            return wear;
        }

        /// <summary>
        /// Every instance the party owns: worn items first (deduped), then the bag in drop order.
        /// The inventory page renders this list; equipped instances carry the E marker.
        /// </summary>
        public List<ItemInstance> AllInstances()
        {
            List<ItemInstance> all = new List<ItemInstance>();
            HashSet<string> seen = new HashSet<string>();

            foreach (KeyValuePair<int, ItemInstance[]> pair in equippedByHero)
            {
                for (int i = 0; i < pair.Value.Length; i++)
                {
                    ItemInstance item = pair.Value[i];
                    if (item != null && seen.Add(item.InstanceId))
                    {
                        all.Add(item);
                    }
                }
            }

            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i] != null && seen.Add(inventory[i].InstanceId))
                {
                    all.Add(inventory[i]);
                }
            }

            return all;
        }

        /// <summary>
        /// True when an instance is currently worn; reports who wears it and in which slot.
        /// </summary>
        public bool IsEquipped(string instanceId, out int heroIndex, out ItemSlotType slotType)
        {
            heroIndex = -1;
            slotType = default;

            if (string.IsNullOrEmpty(instanceId))
            {
                return false;
            }

            foreach (KeyValuePair<int, ItemInstance[]> pair in equippedByHero)
            {
                for (int i = 0; i < pair.Value.Length; i++)
                {
                    if (pair.Value[i] != null && pair.Value[i].InstanceId == instanceId)
                    {
                        heroIndex = pair.Key;
                        slotType = (ItemSlotType)i;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>The party hero who can wear an item (its class), or -1 when the roster has none.</summary>
        public int HeroFor(ItemInstance item)
        {
            if (item == null || party == null)
            {
                return -1;
            }

            for (int i = 0; i < party.Heroes.Count; i++)
            {
                HeroData hero = party.GetHero(i);
                if (hero != null && hero.Role == item.Role)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Summed bonuses as fractions of the hero's base stats (empty slots contribute 0).</summary>
        public ItemBonuses GetGearBonusFraction(int heroIndex)
        {
            double hp = 0d;
            double atk = 0d;
            double def = 0d;

            if (equippedByHero.TryGetValue(heroIndex, out ItemInstance[] wear))
            {
                for (int i = 0; i < wear.Length && i < SlotCount; i++)
                {
                    ItemInstance item = wear[i];
                    if (item == null)
                    {
                        continue;
                    }

                    switch ((ItemSlotType)i)
                    {
                        case ItemSlotType.Weapon:
                            atk += item.StatFraction;
                            break;
                        case ItemSlotType.Armor:
                            def += item.StatFraction;
                            break;
                        case ItemSlotType.Trinket:
                            hp += item.StatFraction;
                            break;
                    }
                }
            }

            return new ItemBonuses(hp, atk, def);
        }

// ------------------------------------------------------------------
        // Player actions
        // ------------------------------------------------------------------
        /// <summary>Manual equip: item must fit the hero's class and lands in its own slot type.</summary>
        public bool Equip(int heroIndex, string instanceId, out string message)
        {
            message = string.Empty;

            HeroData hero = party != null ? party.GetHero(heroIndex) : null;
            if (hero == null)
            {
                message = "select a hero first";
                return false;
            }

            ItemInstance item = Find(instanceId);
            if (item == null)
            {
                message = "that item is gone";
                return false;
            }

            if (item.Role != hero.Role)
            {
                message = item.Role + " gear only";
                return false;
            }

            ItemInstance[] wear = WearFor(heroIndex);
            ItemInstance previous = wear[(int)item.SlotType];

            inventory.Remove(item);
            wear[(int)item.SlotType] = item;

            if (previous != null)
            {
                PutBackOrLose(previous);
            }

            message = "equipped " + item.ShortLabel;
            Announce(hero.HeroName + " equipped " + item.ShortLabel + " (+" + item.PercentText + " " + item.SlotType.PrimaryStatLabel() + ")", false);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Takes a slot's item back off (goes to the inventory, or salvage/discard when full).</summary>
        public bool Unequip(int heroIndex, ItemSlotType slotType, out string message)
        {
            message = string.Empty;

            if (!equippedByHero.TryGetValue(heroIndex, out ItemInstance[] wear))
            {
                message = "nothing equipped there";
                return false;
            }

            ItemInstance item = wear[(int)slotType];
            if (item == null)
            {
                message = "nothing equipped there";
                return false;
            }

            wear[(int)slotType] = null;
            PutBackOrLose(item);
            Announce(item.Role + " " + item.ShortLabel + " unequipped", false);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Deletes an unworn item. Equipped items must be unequipped first (never silently lose worn gear).</summary>
        public bool Discard(string instanceId, out string message)
        {
            message = string.Empty;

            ItemInstance item = Find(instanceId);
            if (item == null)
            {
                message = "that item is gone";
                return false;
            }

            inventory.Remove(item);
            Announce("discarded " + item.ShortLabel + " (+" + item.PercentText + " " + item.SlotType.PrimaryStatLabel() + ")", false);
            Changed?.Invoke();
            return true;
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------
        /// <summary>Finds an instance in the BAG (not equipped). Used by the UI to gate equip actions.</summary>
        public ItemInstance FindFor(string instanceId)
        {
            return Find(instanceId);
        }

        private ItemInstance Find(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                return null;
            }

            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i] != null && inventory[i].InstanceId == instanceId)
                {
                    return inventory[i];
                }
            }

            return null;
        }

        private ItemInstance[] WearFor(int heroIndex)
        {
            if (!equippedByHero.TryGetValue(heroIndex, out ItemInstance[] wear))
            {
                wear = new ItemInstance[SlotCount];
                equippedByHero[heroIndex] = wear;
            }

            return wear;
        }

        /// <summary>Every drop/equip/swap/discard/salvage lands in the battle log; big moments toast too.</summary>
        private void Announce(string text, bool announceToast)
        {
            GameEvents.RaiseCombatMessage(new LogMessage(text, LogMessageKind.Reward));
            if (announceToast)
            {
                GameEvents.RaiseToast(text);
            }
        }
    }
}