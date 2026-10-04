using IdleRPG.Data;
using IdleRPG.Equipment;
using NUnit.Framework;

namespace IdleRPG.Tests
{
    /// <summary>
    /// Drop-routing invariants that hold for ANY random draw (no seeding needed): items never exceed
    /// their homes, every item stays class-owned, worn gear always matches the wearer's class, and the
    /// bag cap is the hard ceiling. Salvage-on means nothing is lost silently.
    /// </summary>
    public class DropRoutingTests
    {
        [Test]
        public void BagNeverExceedsCap_AndNothingVacuouslyDuplicates()
        {
            BalanceConfig balance = TestGearFixture.MakeBalance(dropChance: 1f, cap: 5);
            PartyConfig party = TestGearFixture.MakeParty(HeroRole.Tank, HeroRole.Damage);
            ItemService service = TestGearFixture.MakeService(balance, party, autoEquip: false, autoSalvage: false);

            for (int i = 0; i < 60; i++)
            {
                service.TryBossDrop(3);
                Assert.LessOrEqual(service.InventoryCount, 5, "bag exceeded its cap mid-drop");
            }

            Assert.AreEqual(5, service.InventoryCount, "bag should sit exactly at its cap");
            Assert.AreEqual(0, service.AllInstances().Count - service.InventoryCount, "no worn gear with auto-equip off");
        }

        [Test]
        public void EveryItem_HasAClassOwnerInTheParty()
        {
            BalanceConfig balance = TestGearFixture.MakeBalance(dropChance: 1f, cap: 20);
            PartyConfig party = TestGearFixture.MakeParty(HeroRole.Tank, HeroRole.Damage, HeroRole.Support);
            ItemService service = TestGearFixture.MakeService(balance, party, autoEquip: true, autoSalvage: true);

            for (int i = 0; i < 30; i++)
            {
                service.TryBossDrop(2);
            }

            foreach (ItemInstance item in service.AllInstances())
            {
                Assert.IsTrue(HasClassInParty(party, item.Role),
                    item.Role + " item has no matching class hero in the party");
            }
        }

        [Test]
        public void WornGear_AlwaysMatchesWearerClass()
        {
            BalanceConfig balance = TestGearFixture.MakeBalance(dropChance: 1f, cap: 20);
            PartyConfig party = TestGearFixture.MakeParty(HeroRole.Tank, HeroRole.Support);
            ItemService service = TestGearFixture.MakeService(balance, party, autoEquip: true, autoSalvage: true);

            for (int i = 0; i < 40; i++)
            {
                service.TryBossDrop(6);
            }

            for (int hero = 0; hero < 2; hero++)
            {
                HeroData owner = party.GetHero(hero);
                foreach (ItemInstance item in service.GetEquippedAll(hero))
                {
                    if (item != null)
                    {
                        Assert.AreEqual(owner.Role, item.Role,
                            owner.HeroName + " wears " + item.SlotType + " for another class");
                    }
                }
            }
        }

        private static bool HasClassInParty(PartyConfig party, HeroRole role)
        {
            for (int i = 0; i < party.Heroes.Count; i++)
            {
                if (party.GetHero(i) != null && party.GetHero(i).Role == role)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
