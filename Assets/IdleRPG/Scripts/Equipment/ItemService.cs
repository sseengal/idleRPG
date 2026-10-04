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
    public sealed class ItemService
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
        // Drops
        // ------------------------------------------------------------------
        /// <summary>Boss downed: roll the chance and route the item (auto-equip -> inventory -> salvage/discard).</summary>
        public void TryBossDrop(int stage)
        {
            double chance = balance != null ? balance.GearDropChance : 0.35d;
            if (rng.NextDouble() >= chance)
            {
                return;
            }

            ItemInstance item = ItemFactory.Create(rng, balance, stage);
            Announce(item.DropSummary, announceToast: item.Rarity == ItemRarity.Legendary);

            if (!TryAutoEquip(item))
            {
                AddToInventory(item);
            }
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
        // Drop routing
        // ------------------------------------------------------------------
        private bool TryAutoEquip(ItemInstance item)
        {
            if (!AutoEquipUnlocked)
            {
                return false;
            }

            // Best candidate: a hero of the right class whose CURRENT item in that slot is the weakest -
            // the drop lifts the loadout where it is most behind.
            int bestHero = -1;
            double weakest = double.MaxValue;
            int heroCount = party != null ? party.Heroes.Count : 0;

            for (int i = 0; i < heroCount; i++)
            {
                HeroData hero = party != null ? party.GetHero(i) : null;
                if (hero == null || hero.Role != item.Role)
                {
                    continue;
                }

                double current = GetEquipped(i, item.SlotType) != null ? GetEquipped(i, item.SlotType).StatFraction : 0d;
                if (current < item.StatFraction && current < weakest)
                {
                    weakest = current;
                    bestHero = i;
                }
            }

            if (bestHero < 0)
            {
                return false;
            }

            ItemInstance[] wear = WearFor(bestHero);
            ItemInstance previous = wear[(int)item.SlotType];
            wear[(int)item.SlotType] = item;

            if (previous != null)
            {
                PutBackOrLose(previous);
            }

            HeroData equippedHero = party.GetHero(bestHero);
            Announce(equippedHero.HeroName + " auto-equipped " + item.ShortLabel + " (+" + item.PercentText + " " + item.SlotType.PrimaryStatLabel() + ")", true);
            Changed?.Invoke();
            return true;
        }

        private void AddToInventory(ItemInstance item)
        {
            if (inventory.Count < InventoryCap)
            {
                inventory.Add(item);
                Announce("+ " + item.ShortLabel + " to inventory (" + inventory.Count + "/" + InventoryCap + ")", false);
                Changed?.Invoke();
                return;
            }

            if (AutoSalvageUnlocked)
            {
                Salvage(item);
                return;
            }

            Announce("inventory full - " + item.ShortLabel + " was lost", true);
            Changed?.Invoke();
        }

        /// <summary>The inventory is the only home for unequipped gear; full means salvage (upgrade) or loss.</summary>
        private void PutBackOrLose(ItemInstance item)
        {
            if (inventory.Count < InventoryCap)
            {
                inventory.Add(item);
                return;
            }

            if (AutoSalvageUnlocked)
            {
                Salvage(item);
                return;
            }

            Announce("inventory full - swapped " + item.ShortLabel + " was lost", true);
        }

        private void Salvage(ItemInstance item)
        {
            double gold = item.Price * (balance != null ? balance.GearSalvageFraction : 0.4d);
            economy?.AddGold(gold);
            Announce("salvaged " + item.ShortLabel + " for " + NumberFormatter.Format(gold) + " gold", true);
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------
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
// ------------------------------------------------------------------
        // Save (schema v6, additive)
        // ------------------------------------------------------------------
        public void WriteToSave(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            data.inventory = new List<ItemSaveRecord>();

            // Every instance travels in the one saved list - worn gear first (deduped), then the bag -
            // because the equipped records only reference instance ids. Without this, a worn item had no
            // record on disk and vanished on the next load.
            HashSet<string> seen = new HashSet<string>();
            foreach (KeyValuePair<int, ItemInstance[]> pair in equippedByHero)
            {
                for (int i = 0; i < pair.Value.Length; i++)
                {
                    ItemInstance item = pair.Value[i];
                    if (item != null && seen.Add(item.InstanceId))
                    {
                        data.inventory.Add(ToRecord(item));
                    }
                }
            }

            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i] != null && seen.Add(inventory[i].InstanceId))
                {
                    data.inventory.Add(ToRecord(inventory[i]));
                }
            }

            data.equippedGear = new List<EquippedGearRecord>(equippedByHero.Count);
            foreach (KeyValuePair<int, ItemInstance[]> pair in equippedByHero)
            {
                EquippedGearRecord record = new EquippedGearRecord(pair.Key);
                record.weaponId = pair.Value[(int)ItemSlotType.Weapon] != null ? pair.Value[(int)ItemSlotType.Weapon].InstanceId : string.Empty;
                record.armorId = pair.Value[(int)ItemSlotType.Armor] != null ? pair.Value[(int)ItemSlotType.Armor].InstanceId : string.Empty;
                record.trinketId = pair.Value[(int)ItemSlotType.Trinket] != null ? pair.Value[(int)ItemSlotType.Trinket].InstanceId : string.Empty;
                data.equippedGear.Add(record);
            }
        }

        public void FillFromSave(SaveData data)
        {
            inventory.Clear();
            equippedByHero.Clear();

            if (data == null)
            {
                return;
            }

            // Worn items travel in the same saved "inventory" list as the bag; the equipped records only
            // reference them by instance id. All instances are restored, then split into worn + bag.
            List<ItemInstance> all = new List<ItemInstance>();
            if (data.inventory != null)
            {
                for (int i = 0; i < data.inventory.Count; i++)
                {
                    ItemInstance item = FromRecord(data.inventory[i]);
                    if (item != null && FindById(all, item.InstanceId) == null)
                    {
                        all.Add(item);
                        if (all.Count >= InventoryCap * 2)
                        {
                            break; // safety: never unbounded on a corrupted file
                        }
                    }
                }
            }

            Dictionary<string, ItemInstance> byId = new Dictionary<string, ItemInstance>();
            for (int i = 0; i < all.Count; i++)
            {
                byId[all[i].InstanceId] = all[i];
            }

            // Worn first (they are the player's loadout; never lose them), the rest trimmed to the cap.
            if (data.equippedGear != null)
            {
                for (int i = 0; i < data.equippedGear.Count; i++)
                {
                    EquippedGearRecord record = data.equippedGear[i];
                    if (record == null || record.heroIndex < 0)
                    {
                        continue;
                    }

                    ItemInstance[] wear = WearFor(record.heroIndex);
                    ItemInstance weapon = Take(byId, record.weaponId);
                    ItemInstance armor = Take(byId, record.armorId);
                    ItemInstance trinket = Take(byId, record.trinketId);

                    if (weapon != null)
                    {
                        wear[(int)ItemSlotType.Weapon] = weapon;
                    }

                    if (armor != null)
                    {
                        wear[(int)ItemSlotType.Armor] = armor;
                    }

                    if (trinket != null)
                    {
                        wear[(int)ItemSlotType.Trinket] = trinket;
                    }
                }
            }

            // Drop order preserved; the bag stops at the cap.
            foreach (KeyValuePair<string, ItemInstance> pair in byId)
            {
                if (pair.Value == null || inventory.Count >= InventoryCap)
                {
                    continue;
                }

                inventory.Add(pair.Value);
            }
        }

        private static ItemInstance Take(Dictionary<string, ItemInstance> byId, string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId) || !byId.TryGetValue(instanceId, out ItemInstance item))
            {
                return null;
            }

            byId[instanceId] = null;
            return item;
        }

        private static ItemInstance FindById(List<ItemInstance> list, string instanceId)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].InstanceId == instanceId)
                {
                    return list[i];
                }
            }

            return null;
        }

        private static ItemSaveRecord ToRecord(ItemInstance item)
        {
            return new ItemSaveRecord(
                item.InstanceId,
                (int)item.SlotType,
                (int)item.Rarity,
                (int)item.Role,
                item.Level,
                item.StatFraction,
                item.Price);
        }

        private static ItemInstance FromRecord(ItemSaveRecord record)
        {
            if (record == null || string.IsNullOrEmpty(record.instanceId))
            {
                return null;
            }

            try
            {
                return new ItemInstance(
                    record.instanceId,
                    (ItemSlotType)record.slotType,
                    (ItemRarity)record.rarity,
                    (HeroRole)record.role,
                    record.level,
                    record.statValue,
                    record.price);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
