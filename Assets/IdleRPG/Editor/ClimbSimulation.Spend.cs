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
    /// <summary>
    /// Robot-player spending strategies: the rebirth trigger and the gold purchase policy.
    /// </summary>
    internal static partial class ClimbSimulation
    {

        /// <summary>
        /// B6 Step 4: performs one manual rebirth exactly like the game — tokens priced on the run best
        /// (<c>FormulaUtility.PrestigeTokenReward</c>, the same call <c>AscensionManager.GetTokenYield</c> makes),
        /// hero levels reset per config, and the whole yield spent on permanent upgrades at their real cost
        /// (cheapest-first, mirroring the auto-buy card; the real checkout is <c>TrackService.TryBuy</c>, this
        /// reuses its cost curve so no second formula could drift in). Returns false when the gate is not met
        /// or the yield is 0, leaving everything untouched.
        /// </summary>
        private static bool TryRebirth(StatResolver resolver, PrestigeUpgradeData[] prestigeUpgrades,
            int runBestStage, BalanceConfig balance, ref double tokens, out double yield, out int prestigeLevels)
        {
            yield = 0d;
            prestigeLevels = 0;

            if (resolver == null || prestigeUpgrades == null || runBestStage < 1 || balance == null)
            {
                return false;
            }

            double earned = FormulaUtility.PrestigeTokenReward(
                runBestStage, balance.PrestigeStageDivisor, balance.PrestigeExponent);
            if (earned <= 0d)
            {
                return false;
            }

            yield = earned;
            tokens += earned;

            if (balance.ResetHeroLevelsOnAscension)
            {
                resolver.ResetHeroLevels();
            }

            double remaining = tokens;

            bool boughtAny;
            do
            {
                boughtAny = false;

                for (int i = 0; i < prestigeUpgrades.Length; i++)
                {
                    PrestigeUpgradeData upgrade = prestigeUpgrades[i];
                    if (upgrade == null)
                    {
                        continue;
                    }

                    int level = resolver.GetPrestigeLevel(upgrade);
                    if (upgrade.IsAtMaxLevel(level))
                    {
                        continue;
                    }

                    double cost = FormulaUtility.StatUpgradeBulkCost(upgrade.BaseCostTokens, level, 1, upgrade.CostGrowth);
                    if (cost <= remaining)
                    {
                        resolver.SetPrestigeLevel(upgrade, level + 1);
                        remaining -= cost;
                        prestigeLevels++;
                        boughtAny = true;
                    }
                }
            }
            while (boughtAny);

            tokens = remaining;
            return true;
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
    }
}
