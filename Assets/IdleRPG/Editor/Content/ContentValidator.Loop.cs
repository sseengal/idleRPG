using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using IdleRPG.Combat;
using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Services;

namespace IdleRPG.EditorTools.Content
{
    /// <summary>
    /// Loop health: play the loop, do not just look at one stage.
    /// </summary>
    public static partial class ContentValidator
    {

        // ------------------------------------------------------------------
        // Loop health (B4c): play the loop, do not just look at one stage
        // ------------------------------------------------------------------
        /// <summary>The frontier stage the robot must reach inside the time budget. A floor, not a ceiling - raise it as the base grows.</summary>
        private const int LoopHealthTargetStage = 25;

        /// <summary>How far the robot may play before we stop paying for the check (~1.3x the stage-30 cost measured in B3d).</summary>
        private const int LoopHealthMaxStage = 40;

        /// <summary>A wall that takes longer than this to break is a warning (a tuning signal), not an error.</summary>
        private const double LoopHealthWorstWallMinutes = 12d;

        /// <summary>
        /// Plays the loop (B4c) and asserts the frontier still moves.
        ///
        /// ELI5: the stage-1 band check cannot see a stall - an unupgraded party wipes at stage 4 whatever the balance is.
        /// This check lets the robot play: fight, wipe, fall back, farm, buy, push. If it cannot reach the target stage
        /// inside the budget, the balance has a ceiling that no amount of playing can fix, and the build fails.
        ///
        /// Honest limits: the robot only shops after a wipe (a live player buys mid-climb), so the cadence numbers are
        /// wall-phase numbers. They are reported, never asserted.
        /// </summary>
        private static void CheckLoopHealth()
        {
            BalanceConfig balance = BalanceLabMenu.Load<BalanceConfig>("BalanceConfig");
            WaveConfig waves = BalanceLabMenu.Load<WaveConfig>("WaveConfig");
            PartyConfig party = BalanceLabMenu.Load<PartyConfig>("PartyConfig");

            if (balance == null || waves == null || party == null)
            {
                Add(Severity.Error, "loop", "Config assets missing; skipped the loop-health check.");
                return;
            }

            StatUpgradeData[] statUpgrades =
            {
                BalanceLabMenu.Load<StatUpgradeData>("StatUpgrade_ATK"),
                BalanceLabMenu.Load<StatUpgradeData>("StatUpgrade_HP"),
                BalanceLabMenu.Load<StatUpgradeData>("StatUpgrade_DEF")
            };

            PrestigeUpgradeData[] prestigeUpgrades =
            {
                BalanceLabMenu.Load<PrestigeUpgradeData>("Prestige_Gold"),
                BalanceLabMenu.Load<PrestigeUpgradeData>("Prestige_Damage"),
                BalanceLabMenu.Load<PrestigeUpgradeData>("Prestige_Health")
            };

            for (int i = 0; i < statUpgrades.Length; i++)
            {
                if (statUpgrades[i] == null)
                {
                    Add(Severity.Error, "loop", "A stat upgrade asset is missing; the robot cannot buy power.");
                    return;
                }
            }

            ClimbSimulation.ClimbResult climb = ClimbSimulation.Simulate(
                balance, waves, party, statUpgrades, prestigeUpgrades, ClimbPolicy.Cheapest, LoopHealthMaxStage);

            // Fix D: a sustained batch of sub-10s stage clears is the "player power far ahead of content" smell.
            if (climb.GlutStages >= 3)
            {
                Add(Severity.Warning, "loop",
                    $"the robot cleared {climb.GlutStages} stage(s) in under 10s - player power is far ahead of content " +
                    "at those levels (the one-shot power glut). Re-tune enemy growth or the compounding gain.");
            }

            if (climb.Stuck)
            {
                Add(Severity.Error, "loop",
                    $"the robot is STUCK on stage {climb.StuckStage} after {climb.TotalSeconds / 60d:0.0} min " +
                    $"({climb.Upgrades} upgrades bought, {climb.WorstWallSeconds / 60d:0.0} min on the worst wall). " +
                    "Income cannot out-grow the content curve - check each track's effectMode and gain.");
            }
            else if (climb.ReachedStage < LoopHealthTargetStage)
            {
                Add(Severity.Error, "loop",
                    $"the robot only reached stage {climb.ReachedStage} in {climb.TotalSeconds / 60d:0.0} min; " +
                    $"the loop needs stage {LoopHealthTargetStage} to stay healthy.");
            }
            else if (climb.BudgetExhausted)
            {
                Add(Severity.Warning, "loop",
                    $"the robot reached stage {climb.ReachedStage} but used the whole time budget while still climbing; " +
                    "the climb is slowing down.");
            }

            if (climb.WorstWallSeconds / 60d > LoopHealthWorstWallMinutes)
            {
                Add(Severity.Warning, "loop",
                    $"the worst wall took {climb.WorstWallSeconds / 60d:0.0} min to break (target < {LoopHealthWorstWallMinutes:0} min).");
            }

            // B6 Step 4: the long game - the same loop played with the automation cards (auto-buy mid-climb)
            // and manual rebirths. This is the check that would catch a token/prestige sink that never pays for
            // itself: even with the permanent multipliers, the lifetime frontier must keep moving.
            ClimbSimulation.ClimbResult longGame = ClimbSimulation.Simulate(
                balance, waves, party, statUpgrades, prestigeUpgrades, ClimbPolicy.Rebirth, LoopHealthMaxStage);

            if (longGame.Stuck)
            {
                Add(Severity.Error, "loop",
                    $"the rebirth robot is STUCK on stage {longGame.StuckStage} after {longGame.TotalSeconds / 60d:0.0} min " +
                    $"({longGame.Rebirths} rebirth(s), {longGame.PrestigeLevels} prestige level(s)). " +
                    "Even manual rebirths cannot move the frontier - the permanent upgrades do not pay for themselves.");
            }
            else if (longGame.LifetimeBestStage < LoopHealthTargetStage)
            {
                Add(Severity.Error, "loop",
                    $"the rebirth robot's lifetime frontier only reached stage {longGame.LifetimeBestStage} " +
                    $"in {longGame.TotalSeconds / 60d:0.0} min ({longGame.Rebirths} rebirth(s)); " +
                    $"the long game needs lifetime stage {LoopHealthTargetStage} to stay healthy.");
            }
            else if (longGame.BudgetExhausted)
            {
                Add(Severity.Warning, "loop",
                    $"the rebirth robot reached lifetime stage {longGame.LifetimeBestStage} but used the whole time budget; " +
                    "the long game is slowing down across rebirths.");
            }

            Add(Severity.Info, "loop", string.Format(
                "robot+rebirth: lifetime best stage {0} (run {1}) in {2:0.0} min | {3} rebirth(s), {4} prestige level(s)",
                longGame.LifetimeBestStage, longGame.ReachedStage, longGame.TotalSeconds / 60d,
                longGame.Rebirths, longGame.PrestigeLevels));

            double peakSeconds = 0d;
            int peakStage = 0;
            for (int i = 0; i < climb.ClearedStages.Count; i++)
            {
                if (climb.ClearedStages[i].Seconds > peakSeconds)
                {
                    peakSeconds = climb.ClearedStages[i].Seconds;
                    peakStage = climb.ClearedStages[i].Stage;
                }
            }

            Add(Severity.Info, "loop", string.Format(
                "robot (cheapest-buy) reached stage {0} in {1:0.0} min | walls {2}, worst {3:0.0} min | peak stage time {4:0}s (stage {5}) | fastest {6:0.0}s | {7} upgrade(s) | median shopping gap {8}",
                climb.ReachedStage,
                climb.TotalSeconds / 60d,
                climb.Walls,
                climb.WorstWallSeconds / 60d,
                peakSeconds,
                peakStage,
                climb.FastestStageSeconds >= double.MaxValue ? -1d : climb.FastestStageSeconds,
                climb.Upgrades,
                climb.MedianShoppingGapSeconds < 0d
                    ? "n/a"
                    : string.Format("{0:0.0} min (wall phase only)", climb.MedianShoppingGapSeconds / 60d)));
        }
    }
}
