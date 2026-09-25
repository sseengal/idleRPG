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
        AttackOnly = 1
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
    internal static class ClimbSimulation
    {
        private const int MaxStage = 30;

        /// <summary>30 simulated minutes stuck on one stage counts as "this wall is a bug, not a wall".</summary>
        private const double MaxWallSeconds = 1800d;

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
            public List<ClimbStageRow> ClearedStages;

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

            report.AppendLine($"  party {party.ValidHeroCount} heroes | pace x{pace:0.##} | buys the cheapest affordable upgrade");
            report.AppendLine("  loop: clear -> advance | wipe -> roll back one stage (game rule) and farm it until the frontier falls");
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

                report.AppendLine($"  -- WALL stage {stage}: farming stage {farmStage} --");

                while (!cleared && wallSeconds < MaxWallSeconds && totalSeconds < maxRunSeconds)
                {
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
                    }
                }

                if (!cleared)
                {
                    stuckStage = stage;
                    report.AppendLine(string.Format(
                        "  !! STUCK on stage {0}: {1:0.0} min of farming stage {2}, {3} gold earned, {4} upgrades bought, still wiping",
                        stage, wallSeconds / 60d, farmStage, NumberFormatter.Format(wallGold), wallBuys));
                    AppendWallDiagnosis(report, balance, waves, party, resolver, prestigeUpgrades, stage, lastAttemptSeconds);
                    break;
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

            return result;
        }

        /// <summary>
        /// Median gap between consecutive shopping trips. The mean would be dragged around by one long wall, and the
        /// question we care about is "how long does a typical stretch without a purchase last?".
        /// </summary>
        private static double MedianGap(List<double> stamps)
        {
            if (stamps == null || stamps.Count < 2)
            {
                return -1d;
            }

            List<double> gaps = new List<double>(stamps.Count - 1);
            for (int i = 1; i < stamps.Count; i++)
            {
                gaps.Add(stamps[i] - stamps[i - 1]);
            }

            gaps.Sort();
            int middle = gaps.Count / 2;

            return gaps.Count % 2 == 1
                ? gaps[middle]
                : (gaps[middle - 1] + gaps[middle]) * 0.5d;
        }

        /// <summary>
        /// Spends every affordable coin on the policy's upgrade, cheapest first, until nothing is affordable.
        /// Mirrors a player pressing the buy button: one level at a time, cost from the real resolver.
        /// </summary>
        private static int SpendGold(StatResolver resolver, PartyConfig party, ref double gold, ClimbPolicy policy)
        {
            int bought = 0;

            while (true)
            {
                int bestHero = -1;
                HeroStatType bestStat = HeroStatType.Attack;
                double bestCost = double.MaxValue;

                for (int h = 0; h < party.ValidHeroCount; h++)
                {
                    HeroData hero = party.GetHero(h);
                    if (hero == null)
                    {
                        continue;
                    }

                    for (int s = 0; s < AllStats.Length; s++)
                    {
                        HeroStatType stat = AllStats[s];
                        if (policy == ClimbPolicy.AttackOnly && stat != HeroStatType.Attack)
                        {
                            continue;
                        }

                        int level = resolver.GetHeroLevel(hero, stat);
                        if (resolver.IsAtMaxLevel(stat, level))
                        {
                            continue;
                        }

                        double cost = resolver.GetUpgradeCost(stat, level, 1);
                        if (cost <= gold && cost < bestCost)
                        {
                            bestCost = cost;
                            bestHero = h;
                            bestStat = stat;
                        }
                    }
                }

                if (bestHero < 0)
                {
                    return bought;
                }

                HeroData target = party.GetHero(bestHero);
                gold -= bestCost;
                resolver.SetHeroLevel(target, bestStat, resolver.GetHeroLevel(target, bestStat) + 1);
                bought++;
            }
        }

        private static void AppendStageRow(
            StringBuilder report,
            int stage,
            BalanceLabMenu.StageRun run,
            double elapsedSeconds,
            StatResolver resolver,
            PartyConfig party,
            int buys)
        {
            report.AppendLine(string.Format(
                "  {0,5}  {1,5:0.0}  {2,5}  {3,9}  {4,6:0.00}  {5,6:0.0}m  {6,6:0.00} {7,6:0.00} {8,6:0.00}  {9,5}",
                stage,
                run.Seconds,
                run.Kills,
                NumberFormatter.Format(run.Gold),
                run.Seconds <= 0d ? 0d : run.Gold / run.Seconds,
                elapsedSeconds / 60d,
                PowerRatio(resolver, party, HeroStatType.Attack),
                PowerRatio(resolver, party, HeroStatType.Health),
                PowerRatio(resolver, party, HeroStatType.Defense),
                buys));
        }

        /// <summary>Party stat divided by the same party with no upgrades: 1.00 means untouched base stats.</summary>
        private static double PowerRatio(StatResolver resolver, PartyConfig party, HeroStatType stat)
        {
            double current = 0d;
            double baseValue = 0d;

            for (int i = 0; i < party.ValidHeroCount; i++)
            {
                HeroData hero = party.GetHero(i);
                if (hero == null)
                {
                    continue;
                }

                switch (stat)
                {
                    case HeroStatType.Attack:
                        current += resolver.GetAttack(hero, i);
                        baseValue += hero.BaseAttack;
                        break;
                    case HeroStatType.Health:
                        current += resolver.GetMaxHealth(hero, i);
                        baseValue += hero.BaseHealth;
                        break;
                    default:
                        current += resolver.GetDefense(hero, i);
                        baseValue += hero.BaseDefense;
                        break;
                }
            }

            return baseValue <= 0d ? 0d : current / baseValue;
        }

        /// <summary>Raw party damage per second, before enemy DEF mitigation.</summary>
        private static double PartyDps(StatResolver resolver, PartyConfig party)
        {
            double dps = 0d;

            for (int i = 0; i < party.ValidHeroCount; i++)
            {
                HeroData hero = party.GetHero(i);
                if (hero == null)
                {
                    continue;
                }

                double interval = resolver.GetAttackInterval(hero, i);
                if (interval <= 0d)
                {
                    continue;
                }

                dps += resolver.GetAttack(hero, i) / interval;
            }

            return dps;
        }

        /// <summary>Total enemy HP of a whole stage (every wave + boss), read from the real encounter factory.</summary>
        private static double StageEnemyHealth(BalanceConfig balance, WaveConfig waves, int stage)
        {
            SimRules rules = SimRulesFactory.FromBalance(balance);
            double total = 0d;

            for (int wave = 1; wave <= balance.NormalWavesPerStage + 1; wave++)
            {
                bool isBoss = WaveConfig.IsBossWave(wave, balance.NormalWavesPerStage);
                EnemyCombatant[] team = EncounterFactory.Build(waves, balance, stage, wave, isBoss, rules);

                if (team == null)
                {
                    continue;
                }

                for (int i = 0; i < team.Length; i++)
                {
                    total += team[i].MaxHealth;
                }
            }

            return total;
        }

        /// <summary>Why the wall is a wall, and whether cashing in a prestige would actually break it.</summary>
        private static void AppendWallDiagnosis(
            StringBuilder report,
            BalanceConfig balance,
            WaveConfig waves,
            PartyConfig party,
            StatResolver resolver,
            PrestigeUpgradeData[] prestigeUpgrades,
            int stage,
            double survivedSeconds)
        {
            double stageHealth = StageEnemyHealth(balance, waves, stage);
            double dps = PartyDps(resolver, party);
            double rawSeconds = dps > 0d ? stageHealth / dps : 0d;
            double needFactor = survivedSeconds > 0d ? rawSeconds / survivedSeconds : 0d;

            report.AppendLine(string.Format(
                "  diagnosis stage {0}: {1} enemy HP total | raw party dps {2:0.#} (ignores enemy DEF) -> {3:0}s of damage | party survived {4:0}s => needs about x{5:0.0} more damage",
                stage, NumberFormatter.Format(stageHealth), dps, rawSeconds, survivedSeconds, needFactor));

            double tokens = FormulaUtility.PrestigeTokenReward(stage - 1, balance.PrestigeStageDivisor, balance.PrestigeExponent);
            PrestigeUpgradeData damage = FindPrestigeUpgrade(prestigeUpgrades, PrestigeEffectType.DamagePercent);

            if (damage == null)
            {
                report.AppendLine($"  prestige: stage {stage - 1} would pay {tokens:0} tokens (no Damage upgrade asset found)");
                return;
            }

            int levels = 0;
            double spent = 0d;

            while (levels < damage.MaxLevel)
            {
                double cost = FormulaUtility.StatUpgradeBulkCost(damage.BaseCostTokens, levels, 1, damage.CostGrowth);
                if (spent + cost > tokens)
                {
                    break;
                }

                spent += cost;
                levels++;
            }

            double multiplier = damage.GetMultiplier(levels);

            report.AppendLine(string.Format(
                "  prestige: stage {0} pays {1:0} tokens -> {2} Damage levels ({3:0} tokens) = x{4:0.00} damage. Verdict: {5}",
                stage - 1,
                tokens,
                levels,
                spent,
                multiplier,
                multiplier >= needFactor ? "prestige WOULD break this wall" : "prestige does NOT break this wall"));
        }

        private static PrestigeUpgradeData FindPrestigeUpgrade(PrestigeUpgradeData[] upgrades, PrestigeEffectType effectType)
        {
            if (upgrades == null)
            {
                return null;
            }

            for (int i = 0; i < upgrades.Length; i++)
            {
                if (upgrades[i] != null && upgrades[i].EffectType == effectType)
                {
                    return upgrades[i];
                }
            }

            return null;
        }
    }
}
