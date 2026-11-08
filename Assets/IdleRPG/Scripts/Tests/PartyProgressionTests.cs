using IdleRPG.Progression;
using NUnit.Framework;

namespace IdleRPG.Tests
{
    /// <summary>
    /// Party XP and levels. Pins the contract the boon card depends on: one level pays one pick, a surplus carries
    /// over instead of being lost, the run ceiling stops the bar, and a reset really clears everything.
    ///
    /// The exact curve is design, not law - the numbers here are small and round on purpose so a balance pass can
    /// change the asset without rewriting this suite.
    /// </summary>
    public class PartyProgressionTests
    {
        /// <summary>100 XP for level 2, then x2: 100, 200, 400...</summary>
        private static PartyProgression Build(int maxLevel = 0)
        {
            return new PartyProgression(xpBase: 100d, xpGrowth: 2d, xpPerKill: 10d,
                xpPerKillStageGrowth: 2d, xpBossMultiplier: 5d, maxLevel: maxLevel);
        }

        [Test]
        public void FreshRun_StartsAtLevelOne_WithAnEmptyBar()
        {
            PartyProgression progression = Build();

            Assert.AreEqual(1, progression.Level);
            Assert.AreEqual(0d, progression.Xp, 0.0001d);
            Assert.AreEqual(0, progression.PendingPicks);
            Assert.AreEqual(100d, progression.RequiredXp, 0.0001d);
            Assert.AreEqual(0d, progression.Progress, 0.0001d);
        }

        [Test]
        public void AddingXP_UnderTheBar_LevelsNothing()
        {
            PartyProgression progression = Build();

            Assert.AreEqual(0, progression.AddXp(99d));
            Assert.AreEqual(1, progression.Level);
            Assert.AreEqual(99d, progression.Xp, 0.0001d);
            Assert.AreEqual(0, progression.PendingPicks);
        }

        [Test]
        public void FillingTheBar_GainsOneLevel_AndPaysOnePick()
        {
            PartyProgression progression = Build();

            Assert.AreEqual(1, progression.AddXp(100d));
            Assert.AreEqual(2, progression.Level);
            Assert.AreEqual(1, progression.PendingPicks);
            Assert.AreEqual(200d, progression.RequiredXp, 0.0001d, "the next level costs more");
        }

        [Test]
        public void AStartCarriesOver_AndQueuesEveryLevelItCrosses()
        {
            PartyProgression progression = Build();

            // 100 (->2) + 200 (->3) + 250 spare towards level 4.
            int gained = progression.AddXp(550d);

            Assert.AreEqual(2, gained);
            Assert.AreEqual(3, progression.Level);
            Assert.AreEqual(2, progression.PendingPicks);
            Assert.AreEqual(250d, progression.Xp, 0.0001d, "the surplus is kept, not thrown away");
        }

        [Test]
        public void TheRunCeiling_StopsTheBar_AndKeepsTheLevel()
        {
            PartyProgression progression = Build(maxLevel: 3);

            progression.AddXp(10000d);

            Assert.AreEqual(3, progression.Level);
            Assert.IsTrue(progression.AtMaxLevel);
            Assert.AreEqual(0d, progression.RequiredXp, 0.0001d);
            Assert.AreEqual(1d, progression.Progress, 0.0001d);
            Assert.AreEqual(0, progression.AddXp(500d), "XP at the ceiling changes nothing");
        }

        [Test]
        public void SpendingAPick_IsRefused_WhenNoneAreQueued()
        {
            PartyProgression progression = Build();

            Assert.IsFalse(progression.TrySpendPick());

            progression.AddXp(100d);

            Assert.IsTrue(progression.TrySpendPick());
            Assert.AreEqual(0, progression.PendingPicks);
            Assert.IsFalse(progression.TrySpendPick(), "a pick can only be spent once");
        }

        [Test]
        public void KillXP_GrowsWithTheStage_AndBossesPayMore()
        {
            PartyProgression progression = Build();

            Assert.AreEqual(10d, progression.XpForKill(1, false), 0.0001d);
            Assert.AreEqual(20d, progression.XpForKill(2, false), 0.0001d);
            Assert.AreEqual(50d, progression.XpForKill(1, true), 0.0001d);
            Assert.AreEqual(10d, progression.XpForKill(0, false), 0.0001d, "stage clamps to 1");
        }

        [Test]
        public void ResetForNewRun_ClearsLevelXpAndPicks()
        {
            PartyProgression progression = Build();

            progression.AddXp(350d);
            progression.ResetForNewRun();

            Assert.AreEqual(1, progression.Level);
            Assert.AreEqual(0d, progression.Xp, 0.0001d);
            Assert.AreEqual(0, progression.PendingPicks);
            Assert.AreEqual(0, progression.LevelsGained);
        }
    }
}
