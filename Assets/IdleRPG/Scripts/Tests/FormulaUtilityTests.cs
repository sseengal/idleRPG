using IdleRPG.Progression;
using IdleRPG.Utils;
using NUnit.Framework;

namespace IdleRPG.Tests
{
    /// <summary>
    /// Balance maths + number-labelling invariants. The exact curves are deliberate design; this suite pins
    /// their SHAPE (stage-1 base, monotonic growth, cap behaviour of the label tiers) so a balance pass can
    /// renumber without accidentally rewriting the model.
    /// </summary>
    public class FormulaUtilityTests
    {
        [Test]
        public void StageScaling_ReturnsBase_AtStageOne_AndGrowsExponentially()
        {
            Assert.AreEqual(100d, FormulaUtility.EnemyMaxHealth(100d, 1, 1.2d), 0.0001d);
            Assert.AreEqual(144d, FormulaUtility.EnemyMaxHealth(100d, 3, 1.2d), 0.0001d);
            Assert.AreEqual(100d, FormulaUtility.ScaleByStage(100d, 0, 1.2d), 0.0001d, "stage clamps to 1");
            Assert.AreEqual(0d, FormulaUtility.ScaleByStage(-5d, 3, 1.2d), 0.0001d, "non-positive base -> 0");
        }

        [Test]
        public void StatUpgradeCost_IsMonotonic_AndLevelZeroIsBase()
        {
            Assert.AreEqual(10d, FormulaUtility.StatUpgradeCost(10d, 0, 1.1d), 0.0001d);
            double prev = 0d;
            for (int level = 1; level <= 12; level++)
            {
                double cost = FormulaUtility.StatUpgradeCost(10d, level, 1.15d);
                Assert.Greater(cost, prev, "cost must keep rising");
                prev = cost;
            }
        }

        [Test]
        public void BulkCost_MatchesRepeatedSingles()
        {
            double singles = 0d;
            for (int level = 0; level < 5; level++)
            {
                singles += FormulaUtility.StatUpgradeCost(10d, level, 1.1d);
            }

            Assert.AreEqual(singles, FormulaUtility.StatUpgradeBulkCost(10d, 0, 5, 1.1d), 0.0001d);
        }

        [Test]
        public void HeroStatValue_LevelZeroIsBase_AndAdditiveModelIsLinear()
        {
            Assert.AreEqual(100d, FormulaUtility.HeroStatValue(100d, 0, 0.1d), 0.0001d);
            Assert.AreEqual(150d, FormulaUtility.HeroStatValue(100d, 5, 0.1d), 0.0001d);
            Assert.AreEqual(150d, FormulaUtility.HeroStatValue(100d, 5, 0.1d,
                1d, StatEffectMode.AdditiveBase), 0.0001d);
        }

        [Test]
        public void CompoundingGain_ShippedDefaults_LandInTheDesignedBand()
        {
            double gain = FormulaUtility.CompoundingGainFor(1.15d, 1.07d, 1.12d);
            Assert.Greater(gain, 0.079d);
            Assert.Less(gain, 0.095d);
        }

        [Test]
        public void NumberFormatter_TiersAndRoundings()
        {
            Assert.AreEqual("0", NumberFormatter.Format(0d));
            Assert.AreEqual("1", NumberFormatter.Format(0.4d), "a live value never reads as zero");
            Assert.AreEqual("999", NumberFormatter.Format(999d));
            Assert.AreEqual("1K", NumberFormatter.Format(1000d));
            Assert.AreEqual("2K", NumberFormatter.Format(1500d));
            Assert.AreEqual("1M", NumberFormatter.Format(999600d), "never 1000K");
            Assert.AreEqual("1B", NumberFormatter.Format(1234567890d));
            Assert.AreEqual("-2K", NumberFormatter.Format(-1500d));
        }

        [Test]
        public void NumberFormatter_Durations()
        {
            Assert.AreEqual("45s", NumberFormatter.FormatDuration(45d));
            Assert.AreEqual("12m 30s", NumberFormatter.FormatDuration(750d));
            Assert.AreEqual("1d 4h", NumberFormatter.FormatDuration(100800d));
            Assert.AreEqual("0s", NumberFormatter.FormatDuration(-5d));
        }
    }
}
