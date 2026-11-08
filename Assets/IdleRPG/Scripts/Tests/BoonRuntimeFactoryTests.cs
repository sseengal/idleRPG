using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Sim;
using NUnit.Framework;
using UnityEditor;

namespace IdleRPG.Tests
{
    /// <summary>
    /// The hand-to-fight compiler, tested against the REAL content assets.
    ///
    /// ELI5: this proves two things at once - that a mechanic card reaches the fight with the right numbers, and
    /// that a stat card does not (stats are the resolver's job; putting them in the fight too would double them).
    ///
    /// It reads the shipped assets on purpose: a generator bug that writes the wrong op (a sparse enum written by
    /// ordinal, for example) is exactly the kind of thing that would otherwise be found in play, not here.
    /// </summary>
    public sealed class BoonRuntimeFactoryTests
    {
        private const string PoolPath = "Assets/IdleRPG/Data/Boons/PerkPool.asset";
        private const string PartyPath = "Assets/IdleRPG/Data/Config/PartyConfig.asset";

        private static PartyConfig LoadParty()
        {
            PartyConfig party = AssetDatabase.LoadAssetAtPath<PartyConfig>(PartyPath);
            Assert.IsNotNull(party, "the party config must exist");
            return party;
        }

        private static PerkData LoadBoon(string id)
        {
            PerkPoolDef pool = AssetDatabase.LoadAssetAtPath<PerkPoolDef>(PoolPath);
            Assert.IsNotNull(pool, "the boon pool must exist");

            PerkData boon = pool.Find(id);
            Assert.IsNotNull(boon, $"the pool must contain {id}");
            return boon;
        }

        private static int SlotOf(PartyConfig party, string heroId)
        {
            for (int i = 0; i < party.Heroes.Count; i++)
            {
                if (party.Heroes[i] != null && party.Heroes[i].HeroID == heroId)
                {
                    return i;
                }
            }

            Assert.Fail($"the party has no hero {heroId}");
            return -1;
        }

        private static int CountEffects(PerkRuntime runtime, int slots)
        {
            int total = 0;

            for (int i = 0; i < slots; i++)
            {
                total += runtime.For(i).Length;
            }

            return total;
        }

        [Test]
        public void EmptyHand_CompilesToNothing()
        {
            PartyConfig party = LoadParty();
            PerkRuntime runtime = BoonRuntimeFactory.Build(new PerkLoadout(), party);

            Assert.IsTrue(runtime.IsEmpty, "a hand with no cards must cost the fight nothing");
        }

        [Test]
        public void CleavingBlow_ReachesTheFight_ForTheSoldierOnly()
        {
            PartyConfig party = LoadParty();
            PerkLoadout hand = new PerkLoadout();

            Assert.IsTrue(hand.Add(LoadBoon("boon_cleaving_blow")), "the card must be addable");

            PerkRuntime runtime = BoonRuntimeFactory.Build(hand, party);
            int soldier = SlotOf(party, "hero_soldier");

            Assert.AreEqual(1, CountEffects(runtime, party.Heroes.Count), "one mechanic, one effect");

            PerkEffect[] effects = runtime.For(soldier);
            Assert.AreEqual(1, effects.Length, "the soldier carries it");
            Assert.AreEqual(PerkTrigger.OnHit, effects[0].Trigger);
            Assert.AreEqual(PerkEffectOp.DamageOthers, effects[0].Op);
            Assert.AreEqual(0.5d, effects[0].Amount, 0.0001d);

            int knight = SlotOf(party, "hero_knight");
            int mage = SlotOf(party, "hero_mage");

            Assert.AreEqual(0, runtime.For(knight).Length, "a soldier card must not reach the knight");
            Assert.AreEqual(0, runtime.For(mage).Length, "a soldier card must not reach the mage");
        }

        [Test]
        public void StatCard_DoesNotReachTheFight_BecauseItBelongsToTheResolver()
        {
            PartyConfig party = LoadParty();
            PerkLoadout hand = new PerkLoadout();

            Assert.IsTrue(hand.Add(LoadBoon("boon_sharp_blades")));

            PerkRuntime runtime = BoonRuntimeFactory.Build(hand, party);

            Assert.IsTrue(runtime.IsEmpty, "a pure stat card must not be compiled into the fight as well");
        }
    }
}
