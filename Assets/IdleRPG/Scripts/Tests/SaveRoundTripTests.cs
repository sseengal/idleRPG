using System.Collections.Generic;
using System.Linq;
using IdleRPG.Data;
using IdleRPG.Equipment;
using IdleRPG.Save;
using NUnit.Framework;

namespace IdleRPG.Tests
{
    /// <summary>
    /// THE save-sanity suite: whatever gear layout exists must survive WriteToSave -> a fresh service ->
    /// FillFromSave with worn ids, bag contents and counts intact, and no duplicate instance ids on disk.
    /// This is the player-loses-their-save safety net - the one surface that must never silently regress.
    /// </summary>
    public class SaveRoundTripTests
    {
        [Test]
        public void RoundTrip_PreservesWornAndBag_AndWritesNoDuplicateIds()
        {
            BalanceConfig balance = TestGearFixture.MakeBalance(dropChance: 1f, cap: 30);
            PartyConfig party = TestGearFixture.MakeParty(HeroRole.Tank, HeroRole.Damage);
            ItemService source = TestGearFixture.MakeService(balance, party, autoEquip: false, autoSalvage: false);

            for (int i = 0; i < 25; i++)
            {
                source.TryBossDrop(5);
            }

            // Equip the first bag item that matches hero 0's class (Tank) - arbitrary slot, any rarity.
            ItemInstance wore = source.Inventory.FirstOrDefault(item => item.Role == HeroRole.Tank);
            if (wore == null)
            {
                Assert.Ignore("no Tank-class drop rolled; suite still exercises the bag path");
                return;
            }

            Assert.IsTrue(source.Equip(0, wore.InstanceId, out string equipMessage), equipMessage);

            int wornCount = source.AllInstances().Count - source.InventoryCount;
            ItemInstance worn = source.GetEquipped(0, wore.SlotType);

            SaveData data = new SaveData();
            source.WriteToSave(data);

            // Dedupe rule: every instance travels exactly once in the shared saved list.
            List<string> savedIds = data.inventory.Select(record => record.instanceId).ToList();
            Assert.AreEqual(savedIds.Count, savedIds.Distinct().Count(), "duplicate instance id written to save");

            EquippedGearRecord wornRecord = data.equippedGear.FirstOrDefault(record => record.heroIndex == 0);
            Assert.IsNotNull(wornRecord, "hero 0 has no equipped record in the save");

            // A fresh service built from the same data must reproduce the exact loadout.
            ItemService reloaded = TestGearFixture.MakeService(balance, party, autoEquip: false, autoSalvage: false);
            reloaded.FillFromSave(data);

            Assert.AreEqual(TestGearFixture.CountAll(source), TestGearFixture.CountAll(reloaded), "owned count changed across round-trip");
            Assert.AreEqual(source.InventoryCount, reloaded.InventoryCount, "bag count changed across round-trip");

            ItemInstance wornAgain = reloaded.GetEquipped(0, wore.SlotType);
            Assert.IsNotNull(wornAgain, "worn gear vanished on load");
            Assert.AreEqual(worn.InstanceId, wornAgain.InstanceId, "worn id changed across round-trip");

            HashSet<string> bagA = new HashSet<string>(source.Inventory.Select(item => item.InstanceId));
            HashSet<string> bagB = new HashSet<string>(reloaded.Inventory.Select(item => item.InstanceId));
            Assert.IsTrue(bagA.SetEquals(bagB), "bag contents changed across round-trip");
            Assert.AreEqual(1, wornCount, "exactly one worn item for this setup");
        }

        [Test]
        public void Save_OverflowingBag_TrimsToCap_ButKeepsWorn()
        {
            BalanceConfig balance = TestGearFixture.MakeBalance(dropChance: 1f, cap: 3);
            PartyConfig party = TestGearFixture.MakeParty(HeroRole.Tank, HeroRole.Support);
            ItemService source = TestGearFixture.MakeService(balance, party, autoEquip: false, autoSalvage: false);

            for (int i = 0; i < 10; i++)
            {
                source.TryBossDrop(4);
            }

            ItemInstance wore = source.Inventory.FirstOrDefault(item => item.Role == HeroRole.Tank);
            if (wore != null)
            {
                source.Equip(0, wore.InstanceId, out _);
            }

            SaveData data = new SaveData();
            source.WriteToSave(data);

            ItemService reloaded = TestGearFixture.MakeService(balance, party, autoEquip: false, autoSalvage: false);
            reloaded.FillFromSave(data);

            Assert.LessOrEqual(reloaded.InventoryCount, 3, "bag must never exceed its cap after load");
            ItemInstance wornAgain = reloaded.GetEquipped(0, wore != null ? wore.SlotType : ItemSlotType.Weapon);
            if (wore != null)
            {
                Assert.AreEqual(wore.InstanceId, wornAgain.InstanceId, "worn gear must survive a trim");
            }
        }
    }
}
