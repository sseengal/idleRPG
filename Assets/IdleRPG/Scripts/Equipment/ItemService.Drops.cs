using System;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Utils;

namespace IdleRPG.Equipment
{
    /// <summary>Drop pipeline (partial of <see cref="ItemService"/>): boss drops, auto-equip,
    /// bag overflow, salvage. Everything lands in the battle log / toasts via Announce.</summary>
    public sealed partial class ItemService
    {
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
    }
}
