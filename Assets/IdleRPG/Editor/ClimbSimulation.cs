using System.Collections.Generic;
using System.Text;
using UnityEngine;
using IdleRPG.Combat;
using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Sim;
using IdleRPG.Utils;

namespace IdleRPG.EditorTools
{
    /// <summary>Which upgrade a robot player reaches for.</summary>
    internal enum ClimbPolicy
    {
        /// <summary>Classic idle behaviour: buy whatever is cheapest right now, across all three stats.</summary>
        Cheapest = 0,

        /// <summary>Rusher: every coin into ATK, never HP or DEF.</summary>
        AttackOnly = 1,

        /// <summary>
        /// B6 Step 4: a player who bought the automation cards (auto-buy always on) and plays the manual
        /// rebirth loop - buys whenever affordable, and when a wall outlives the rebirth trigger (or beats the
        /// farm budget) with the ascension gate met, ascends, spends the tokens on permanent upgrades and
        /// climbs again. Answers the question the single-run robot cannot: does a rebirth actually extend the
        /// frontier, or is it a coin-sink that never pays for itself?
        /// </summary>
        Rebirth = 2
    }

    /// <summary>
    /// Robot player (B3). Plays the *played* loop instead of a single stage: fight the frontier, and when the party
    /// wipes, roll back one stage (the game's rule), farm it, buy upgrades and push again.
    ///
    /// Answers what a stage sweep cannot: how long a stage feels, when the first wall lands, how long it takes to
    /// break, and whether prestige is ever the right move.
    ///
    /// Everything reuses shipping code — the real Balance Lab stage runner (which drives CombatSimulator), the real
    /// StatResolver for levels, gains and costs, and FormulaUtility for token payouts. No second formula lives here.
    /// </summary>
    internal static partial class ClimbSimulation
    {
        private const int MaxStage = 30;

        /// <summary>30 simulated minutes stuck on one stage counts as "this wall is a bug, not a wall".</summary>
        private const double MaxWallSeconds = 1800d;

        /// <summary>B6 Step 4: a wall that still holds after this long (with the ascension gate met) is when a
        /// manual player ascends instead of farming to the wall timeout.</summary>
        private const double RebirthWallSeconds = 240d;

        /// <summary>B6 Step 4: voluntary-ascent schedule. A real player does not only rebirth at a wall - they
        /// take the yield when it is worth it. The robot ascends at these run-best stages too, so the rebirth
        /// loop is always exercised and measured on clean data, not just when a wall forces it.</summary>
        private static readonly int[] RebirthMilestones = { 20, 40 };

        /// <summary>3 simulated hours for the whole climb — the robot stops there and reports.</summary>
        private const double MaxRunSeconds = 10800d;

        private static readonly HeroStatType[] AllStats =
        {
            HeroStatType.Attack,
            HeroStatType.Health,
            HeroStatType.Defense
        };

        /// <summary>One cleared stage of the climb: what the robot saw and what it cost in wall time.</summary>
        internal struct ClimbStageRow
        {
            public int Stage;
            public double Seconds;
            public int Kills;
            public double Gold;
            public double WallSeconds;
            public bool ClearedAfterFarming;
        }

        /// <summary>
        /// The machine-readable outcome of one robot climb.
        ///
        /// ELI5: the robot used to only tell us a story (a log a human had to read). Now it also hands over a
        /// scorecard, so "did the frontier move?" can fail a build instead of waiting for someone to notice.
        /// </summary>
        internal struct ClimbResult
        {
            public ClimbPolicy Policy;
            public string Report;
            public int ReachedStage;
            /// <summary>Stage the robot was still farming when it gave up; 0 = never stuck.</summary>
            public int StuckStage;
            public int Walls;
            public int Upgrades;
            public double TotalSeconds;
            public double WorstWallSeconds;
            /// <summary>Median sim-time between shopping trips (a trip = a farm cycle that could afford something).</summary>
            public double MedianShoppingGapSeconds;
            /// <summary>Sim-time until the first purchase was affordable (-1 = never).</summary>
            public double FirstPurchaseSeconds;
            /// <summary>True when the climb stopped because the time budget ran out rather than because it finished.</summary>
            public bool BudgetExhausted;
            /// <summary>Fastest cleared stage time - a sustained very low figure is the "power glut" smell (Fix D).</summary>
            public double FastestStageSeconds;
            /// <summary>How many cleared stages took under the glut threshold (10s): content is being one-shot.</summary>
            public int GlutStages;
            public List<ClimbStageRow> ClearedStages;

            /// <summary>Highest stage reached across every rebirth - the honest frontier for the long game (B6 Step 4).</summary>
            public int LifetimeBestStage;
            /// <summary>How many manual rebirths the robot performed.</summary>
            public int Rebirths;
            /// <summary>Permanent-upgrade levels bought with rebirth tokens.</summary>
            public int PrestigeLevels;

            public bool Stuck => StuckStage > 0;
        }

        internal static string Run(
            BalanceConfig balance,
            WaveConfig waves,
            PartyConfig party,
            StatUpgradeData[] statUpgrades,
            PrestigeUpgradeData[] prestigeUpgrades,
            ClimbPolicy policy)
        {
            return Simulate(balance, waves, party, statUpgrades, prestigeUpgrades, policy).Report;
        }

        /// <summary>
        /// Plays the loop once and returns both the printable report and the numbers behind it. Bounded by
        /// <paramref name="maxStage"/> and <paramref name="maxRunSeconds"/> so a validator call cannot loop forever.
        /// </summary>
        internal static ClimbResult Simulate(
            BalanceConfig balance,
            WaveConfig waves,
            PartyConfig party,
            StatUpgradeData[] statUpgrades,
            PrestigeUpgradeData[] prestigeUpgrades,
            ClimbPolicy policy,
            int maxStage = MaxStage,
            double maxRunSeconds = MaxRunSeconds)
        {
            ClimbResult result = new ClimbResult
            {
                Policy = policy,
                ClearedStages = new List<ClimbStageRow>(),
                MedianShoppingGapSeconds = -1d,
                FirstPurchaseSeconds = -1d
            };

            StringBuilder report = new StringBuilder();
            report.AppendLine($"=== Robot player: {policy} ===");

            if (balance == null || waves == null || party == null || party.ValidHeroCount == 0)
            {
                report.AppendLine("  (missing config — nothing to simulate)");
                result.Report = report.ToString();
                return result;
            }

            StatResolver resolver = new StatResolver(balance, statUpgrades, prestigeUpgrades);
            double pace = balance.CombatPaceMultiplier;
            double gold = 0d;
            double totalSeconds = 0d;
            double worstWallSeconds = 0d;
            double lastAttemptSeconds = 0d;
            int buys = 0;
            int walls = 0;
            int reachedStage = 0;
            int stuckStage = 0;
            List<double> shoppingTrips = new List<double>();
            int runBest = 1;
            int lifetimeBest = 0;
            int rebirths = 0;
            int prestigeLevels = 0;
            double tokens = 0d;
            int nextRebirthMilestone = 0;

            report.AppendLine($"  party {party.ValidHeroCount} heroes | pace x{pace:0.##} | buys the cheapest affordable upgrade");
            report.AppendLine("  loop: clear -> advance | wipe -> roll back one stage (game rule) and farm it until the frontier falls");
            if (policy == ClimbPolicy.Rebirth)
            {
                report.AppendLine("  automation: auto-buy always on | manual rebirth when a wall outlives the trigger and the gate is met");
            }
            report.AppendLine("  stage   secs   kills      gold   gold/s  elapsed    ATKx    HPx    DEFx   buys");

            int stage = 1;
            while (stage <= maxStage && totalSeconds < maxRunSeconds)
            {
                BalanceLabMenu.StageRun attempt = BalanceLabMenu.RunStage(balance, waves, party, stage, pace, resolver);
                totalSeconds += attempt.Seconds;
                lastAttemptSeconds = attempt.Seconds;

                if (!attempt.Wiped)
                {
                    gold += attempt.Gold;
                    AppendStageRow(report, stage, attempt, totalSeconds, resolver, party, buys);
                    result.ClearedStages.Add(new ClimbStageRow
                    {
                        Stage = stage,
                        Seconds = attempt.Seconds,
                        Kills = attempt.Kills,
                        Gold = attempt.Gold,
                        WallSeconds = 0d,
                        ClearedAfterFarming = false
                    });
                    reachedStage = stage;

                    if (stage > runBest)
                    {
                        runBest = stage;
                    }

                    if (stage > lifetimeBest)
                    {
                        lifetimeBest = stage;
                    }

                    // B6 Step 4 (auto-buy card): spend whenever affordable, not only at walls.
                    if (policy == ClimbPolicy.Rebirth)
                    {
                        int midBuys = SpendGold(resolver, party, ref gold, policy);
                        if (midBuys > 0)
                        {
                            buys += midBuys;
                            shoppingTrips.Add(totalSeconds);
                        }
                    }

                    // B6 Step 4 (manual rebirth loop): a voluntary ascent at a milestone stage - the "quick
                    // ascend" a real player takes when the yield is worth it - so the rebirth path is always
                    // exercised and measured, not just when a wall forces it.
                    if (policy == ClimbPolicy.Rebirth && nextRebirthMilestone < RebirthMilestones.Length &&
                        runBest >= balance.MinStageToAscend && runBest >= RebirthMilestones[nextRebirthMilestone] &&
                        TryRebirth(resolver, prestigeUpgrades, runBest, balance, ref tokens,
                            out double milestoneYield, out int milestoneLevels))
                    {
                        nextRebirthMilestone++;
                        rebirths++;
                        prestigeLevels += milestoneLevels;
                        report.AppendLine(string.Format(
                            "  -- REBIRTH at stage {0} (milestone {1}, yield {2:0} token(s)): {3} prestige level(s)",
                            stage, runBest, milestoneYield, milestoneLevels));
                        gold = 0d;
                        stage = 1;
                        runBest = 1;
                        continue;
                    }

                    stage++;
                    continue;
                }

                // ---- wall: farm the last cleared stage, spend, retry the frontier ----
                walls++;
                int farmStage = Mathf.Max(1, stage - 1);
                double wallSeconds = attempt.Seconds;
                double wallGold = 0d;
                int wallBuys = 0;
                bool cleared = false;
                bool rebornThisWall = false;

                report.AppendLine($"  -- WALL stage {stage}: farming stage {farmStage} --");

                while (!cleared && wallSeconds < MaxWallSeconds && totalSeconds < maxRunSeconds)
                {
                    // B6 Step 4: a wall that already outlived the rebirth trigger is when a manual player
                    // ascends instead of farming to the wall timeout - take the rebirth now, climb again.
                    if (!rebornThisWall && policy == ClimbPolicy.Rebirth && runBest >= balance.MinStageToAscend &&
                        wallSeconds >= RebirthWallSeconds)
                    {
                        rebornThisWall = TryRebirth(resolver, prestigeUpgrades, runBest, balance, ref tokens,
                            out double proactiveYield, out int proactiveLevels);
                        if (rebornThisWall)
                        {
                            prestigeLevels += proactiveLevels;
                            report.AppendLine(string.Format(
                                "  -- REBIRTH at stage {0} after {1:0.0} min of wall (yield {2:0} token(s)): {3} prestige level(s)",
                                stage, wallSeconds / 60d, proactiveYield, proactiveLevels));
                            break;
                        }
                    }

                    BalanceLabMenu.StageRun farm = BalanceLabMenu.RunStage(balance, waves, party, farmStage, pace, resolver);
                    totalSeconds += farm.Seconds;
                    wallSeconds += farm.Seconds;
                    gold += farm.Gold;
                    wallGold += farm.Gold;

                    int bought = SpendGold(resolver, party, ref gold, policy);
                    buys += bought;
                    wallBuys += bought;

                    // A "shopping trip" is a farm cycle that could actually afford something - the cadence a player
                    // feels. Recorded here so the loop-health check can assert "there is always something to buy".
                    if (bought > 0)
                    {
                        shoppingTrips.Add(totalSeconds);
                    }

                    BalanceLabMenu.StageRun retry = BalanceLabMenu.RunStage(balance, waves, party, stage, pace, resolver);
                    totalSeconds += retry.Seconds;
                    wallSeconds += retry.Seconds;
                    lastAttemptSeconds = retry.Seconds;

                    if (!retry.Wiped)
                    {
                        cleared = true;
                        gold += retry.Gold;
                        AppendStageRow(report, stage, retry, totalSeconds, resolver, party, buys);
                        result.ClearedStages.Add(new ClimbStageRow
                        {
                            Stage = stage,
                            Seconds = retry.Seconds,
                            Kills = retry.Kills,
                            Gold = retry.Gold,
                            WallSeconds = wallSeconds,
                            ClearedAfterFarming = true
                        });
                        reachedStage = stage;

                        if (stage > runBest)
                        {
                            runBest = stage;
                        }

                        if (stage > lifetimeBest)
                        {
                            lifetimeBest = stage;
                        }
                    }
                }

                if (!cleared && !rebornThisWall)
                {
                    // B6 Step 4 fallback: the wall beat the farm budget. If the ascension gate is met, a manual
                    // player would rebirth rather than quit - the permanent multipliers are how the wall breaks.
                    if (policy == ClimbPolicy.Rebirth && runBest >= balance.MinStageToAscend &&
                        TryRebirth(resolver, prestigeUpgrades, runBest, balance, ref tokens, out double fallbackYield, out int fallbackLevels))
                    {
                        rebornThisWall = true;
                        prestigeLevels += fallbackLevels;
                        report.AppendLine(string.Format(
                            "  -- REBIRTH at stage {0} after {1:0.0} min of wall (yield {2:0} token(s)): {3} prestige level(s)",
                            stage, wallSeconds / 60d, fallbackYield, fallbackLevels));
                    }
                    else
                    {
                        stuckStage = stage;
                        report.AppendLine(string.Format(
                            "  !! STUCK on stage {0}: {1:0.0} min of farming stage {2}, {3} gold earned, {4} upgrades bought, still wiping",
                            stage, wallSeconds / 60d, farmStage, NumberFormatter.Format(wallGold), wallBuys));
                        AppendWallDiagnosis(report, balance, waves, party, resolver, prestigeUpgrades, stage, lastAttemptSeconds);
                        break;
                    }
                }

                if (rebornThisWall)
                {
                    // A wall-forced ascent may pass several voluntary milestones (e.g. a wall at runBest 22
                    // already covered the 20-stage milestone) - do not re-take it on the next climb.
                    while (nextRebirthMilestone < RebirthMilestones.Length &&
                           lifetimeBest >= RebirthMilestones[nextRebirthMilestone])
                    {
                        nextRebirthMilestone++;
                    }

                    gold = 0d;
                    stage = 1;
                    runBest = 1;
                    rebirths++;
                    report.AppendLine("  -- climbing again from stage 1 with the permanent multipliers --");
                    continue;
                }

                if (wallSeconds > worstWallSeconds)
                {
                    worstWallSeconds = wallSeconds;
                }

                report.AppendLine(string.Format(
                    "  -- wall {0} broken in {1:0.0} min ({2} upgrades, {3} gold farmed) --",
                    stage, wallSeconds / 60d, wallBuys, NumberFormatter.Format(wallGold)));
                stage++;
            }

            report.AppendLine(string.Format(
                "  reached stage {0} in {1:0.0} min ({2:0.00} h) | {3} upgrades | walls {4} | worst wall {5:0.0} min",
                reachedStage, totalSeconds / 60d, totalSeconds / 3600d, buys, walls, worstWallSeconds / 60d));

            if (policy == ClimbPolicy.Rebirth)
            {
                report.AppendLine(string.Format(
                    "  LONG GAME: lifetime best stage {0} | {1} rebirth(s) | {2} prestige level(s) | {3:0} token(s) held",
                    lifetimeBest, rebirths, prestigeLevels, tokens));
            }

            result.Report = report.ToString();
            result.ReachedStage = reachedStage;
            result.StuckStage = stuckStage;
            result.Walls = walls;
            result.Upgrades = buys;
            result.TotalSeconds = totalSeconds;
            result.WorstWallSeconds = worstWallSeconds;
            result.BudgetExhausted = totalSeconds >= maxRunSeconds;
            result.FirstPurchaseSeconds = shoppingTrips.Count > 0 ? shoppingTrips[0] : -1d;
            result.MedianShoppingGapSeconds = MedianGap(shoppingTrips);
            result.LifetimeBestStage = lifetimeBest;
            result.Rebirths = rebirths;
            result.PrestigeLevels = prestigeLevels;

            result.FastestStageSeconds = double.MaxValue;
            result.GlutStages = 0;
            for (int i = 0; i < result.ClearedStages.Count; i++)
            {
                ClimbStageRow row = result.ClearedStages[i];
                if (row.Seconds < result.FastestStageSeconds)
                {
                    result.FastestStageSeconds = row.Seconds;
                }

                if (row.Seconds < GlutThresholdSeconds)
                {
                    result.GlutStages++;
                }
            }

            return result;
        }

        /// <summary>A stage clearing faster than this (sustained) means player power is far ahead of content (Fix D).</summary>
        private const double GlutThresholdSeconds = 10d;
    }
}
