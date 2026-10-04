using System;
using System.Collections.Generic;
using IdleRPG.Data;
using IdleRPG.Save;

namespace IdleRPG.Equipment
{
    /// <summary>Persistence (partial of <see cref="ItemService"/>): schema v6 write/read. Worn
    /// instances travel in the same saved list as the bag; equipped records reference ids only.</summary>
    public sealed partial class ItemService
    {
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
