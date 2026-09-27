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
    /// Monetisation guard: the polite policeman for every price tag.
    /// </summary>
    public static partial class ContentValidator
    {

        // ------------------------------------------------------------------
        // Monetisation guard (B7 S5): the polite policeman for every price tag
        // ------------------------------------------------------------------
        /// <summary>Below this payback (seconds of play) money starts to beat playing: an error.</summary>
        private const double MinPaybackSeconds = 3600d;

        /// <summary>Below this payback the price is still worth a look: a warning.</summary>
        private const double WarnPaybackSeconds = 10800d;

        /// <summary>
        /// Reads every monetisation knob and price tag and refuses the ones that break the promise:
        ///   * a time sink may never pay more than 2x the time its cost buys (the doc's rule),
        ///   * a permanent multiplier's PAYBACK (costSeconds / gain) must be at least an hour of play and must
        ///     GROW with every level - rate-free and horizon-free, so money can never out-earn playing,
        ///   * the IAP shelf may only sell time (gems / offline minutes) or ad removal - never power,
        ///   * a free player must be able to reach the cheapest gem sink from the daily faucet.
        /// </summary>
        private static void CheckMonetisation()
        {
            BalanceConfig balance = BalanceLabMenu.Load<BalanceConfig>("BalanceConfig");

            if (balance == null)
            {
                Add(Severity.Error, "money", "BalanceConfig missing; skipped the monetisation guard.");
                return;
            }

            // The instant-income offer IS the exchange rate: how many seconds of income one gem buys.
            double secondsPerGem = balance.InstantIncomeGemCost > 0d
                ? balance.InstantIncomeSeconds / balance.InstantIncomeGemCost
                : 0d;

            if (secondsPerGem <= 0d)
            {
                Add(Severity.Error, "money", "instantIncomeGemCost must be > 0 - it defines the gems -> time exchange.");
                return;
            }

            CheckTimeSink("instant income", balance.InstantIncomeSeconds,
                balance.InstantIncomeGemCost * secondsPerGem);

            double capValue = System.Math.Min(balance.OfflineCapExtensionSeconds, balance.OfflineMaxEquivalentSeconds)
                              * balance.OfflineEfficiency;
            CheckTimeSink("offline cap extension", capValue,
                balance.OfflineCapExtensionGemCost * secondsPerGem);

        // Permanent (multiplier) sinks from the spec: payback must be long and must grow.
            UpgradeSpecFile upgrades = ContentSpecIO.Load<UpgradeSpecFile>(ContentSpecIO.TracksPath);
            int gemSinks = 0;
            double cheapestGemSink = double.MaxValue;
            string summary = "";

            if (upgrades != null)
            {
                for (int i = 0; i < upgrades.prestigeUpgrades.Count; i++)
                {
                    PrestigeUpgradeSpec entry = upgrades.prestigeUpgrades[i];
                    string currency = (entry.costCurrency ?? "tokens").Trim().ToLowerInvariant();

                    if (currency != "gems")
                    {
                        continue;
                    }

                    gemSinks++;
                    cheapestGemSink = System.Math.Min(cheapestGemSink, entry.baseCostTokens);

                    if (entry.effectPerLevel <= 0f)
                    {
                        Add(Severity.Error, "money", $"{entry.id}: a gem sink must have a positive per-level gain.");
                        continue;
                    }

                    double payback = entry.baseCostTokens * secondsPerGem / entry.effectPerLevel;

                    if (payback < MinPaybackSeconds)
                    {
                        Add(Severity.Error, "money",
                            $"{entry.id}: pays back in {payback / 3600d:0.00}h of play (floor {MinPaybackSeconds / 3600d:0}h) - money would beat playing.");
                    }
                    else if (payback < WarnPaybackSeconds)
                    {
                        Add(Severity.Warning, "money",
                            $"{entry.id}: payback {payback / 3600d:0.0}h is short (warn below {WarnPaybackSeconds / 3600d:0}h).");
                    }

                    if (entry.costGrowth <= 1f)
                    {
                        Add(Severity.Error, "money",
                            $"{entry.id}: costGrowth must be > 1 so the payback grows with every level.");
                    }

                    summary += $"{entry.id} payback {payback / 3600d:0.0}h | ";
                }
            }

            // The IAP shelf may sell time or ad removal - never power.
            bool sellsNoAds = false;

            for (int i = 0; i < IapCatalog.All.Count; i++)
            {
                IapProduct product = IapCatalog.All[i];
                bool sellsTime = product.GemsGranted > 0d || product.OfflineCapBonusMinutes > 0d;

                if (product.RemovesAds)
                {
                    sellsNoAds = true;
                }

                if (!sellsTime && !product.RemovesAds)
                {
                    Add(Severity.Error, "money",
                        $"{product.Sku}: sells neither time nor ad removal - power must never be sold.");
                }
            }

            if (!sellsNoAds)
            {
                Add(Severity.Error, "money", "No 'No Ads' product found in IapCatalog - the swap seam needs it.");
            }

            // Free reachability: the cheapest gem sink must be within a few days of the daily faucet.
            int[] streak = balance.DailyStreakGems;
            double threeDaysOfStreak = 0d;

            for (int i = 0; i < streak.Length && i < 3; i++)
            {
                threeDaysOfStreak += streak[i];
            }

            if (gemSinks > 0 && threeDaysOfStreak > 0d && cheapestGemSink > threeDaysOfStreak * 3d)
            {
                Add(Severity.Warning, "money",
                    $"the cheapest gem sink costs {cheapestGemSink:0} gems = {(cheapestGemSink / threeDaysOfStreak):0.0} streak days; a free player may never reach it.");
            }

            if (balance.GemsPerMilestone <= 0 && streak.Length == 0)
            {
                Add(Severity.Error, "money", "no free gem faucet at all (milestones AND streak are empty).");
            }

            Add(Severity.Info, "money",
                $"{secondsPerGem:0} s/gem | instant x1.00, cap x{(balance.OfflineCapExtensionGemCost * secondsPerGem > 0d ? capValue / (balance.OfflineCapExtensionGemCost * secondsPerGem) : 0d):0.00} | " +
                $"{gemSinks} gem sink(s), {IapCatalog.All.Count} IAP SKU(s) | {summary}");
        }

        /// <summary>One time-conversion sink: value may never exceed 2x the time its cost buys.</summary>
        private static void CheckTimeSink(string name, double valueSeconds, double costSeconds)
        {
            if (costSeconds <= 0d)
            {
                Add(Severity.Error, "money", $"{name}: cost in time is 0 - the exchange is broken.");
                return;
            }

            double ratio = valueSeconds / costSeconds;

            if (ratio > 2d)
            {
                Add(Severity.Error, "money",
                    $"{name}: pays {valueSeconds:0}s of income for {costSeconds:0}s of gems (x{ratio:0.00}, cap x2).");
                return;
            }

            Add(Severity.Info, "money", $"{name}: x{ratio:0.00} time-value (cap x2).");
        }
    }
}
