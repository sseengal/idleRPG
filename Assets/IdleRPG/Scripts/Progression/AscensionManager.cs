using System;
using System.Collections.Generic;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Utils;

namespace IdleRPG.Progression
{
    /// <summary>
    /// Prestige flow: token yield, the ascension reset and the permanent upgrade tree.
    /// Deliberately does NOT touch GameManager/CombatManager — it mutates currencies and
    /// progression state, then returns; the caller (GameManager) owns flow and state.
    /// </summary>
    public sealed class AscensionManager
    {
        private readonly BalanceConfig balanceConfig;
        private readonly EconomyManager economy;
        private readonly StatResolver resolver;
        private readonly List<PrestigeUpgradeData> upgrades;

        public AscensionManager(BalanceConfig balanceConfig, EconomyManager economy, StatResolver resolver,
            IEnumerable<PrestigeUpgradeData> prestigeUpgrades)
        {
            this.balanceConfig = balanceConfig;
            this.economy = economy;
            this.resolver = resolver;
            upgrades = new List<PrestigeUpgradeData>(prestigeUpgrades ?? Array.Empty<PrestigeUpgradeData>());

            if (this.balanceConfig == null || this.economy == null || this.resolver == null)
            {
                Debug.LogError("[AscensionManager] Requires BalanceConfig, EconomyManager and StatResolver.");
            }
        }

        /// <summary>Tokens the player would earn for a given highest stage reached.</summary>
        public double GetTokenYield(int highestStageReached)
        {
            if (balanceConfig == null)
            {
                return 0d;
            }

            return FormulaUtility.PrestigeTokenReward(
                highestStageReached,
                balanceConfig.PrestigeStageDivisor,
                balanceConfig.PrestigeExponent);
        }

        public bool CanAscend(int highestStageReached)
        {
            return balanceConfig != null && highestStageReached >= balanceConfig.MinStageToAscend;
        }

        /// <summary>Every permanent upgrade asset, in authoring order (UI list order).</summary>
        public IReadOnlyList<PrestigeUpgradeData> Upgrades => upgrades;

        /// <summary>
        /// Grants tokens, wipes gold and (optionally) hero levels. Returns false when the
        /// player has not reached the minimum stage. Gems and prestige levels always persist.
        /// </summary>
        public bool TryAscend(int highestStageReached, out double tokensEarned)
        {
            tokensEarned = 0d;

            if (!CanAscend(highestStageReached))
            {
                return false;
            }

            tokensEarned = GetTokenYield(highestStageReached);
            if (tokensEarned <= 0d)
            {
                return false;
            }

            economy.AddTokens(tokensEarned);
            economy.ResetGold();

            if (balanceConfig == null || balanceConfig.ResetHeroLevelsOnAscension)
            {
                resolver.ResetHeroLevels();
            }

            return true;
        }

        // ------------------------------------------------------------------
        // Permanent upgrade tree
        // ------------------------------------------------------------------
        public int GetPrestigeLevel(PrestigeUpgradeData upgrade)
        {
            return resolver == null ? 0 : resolver.GetPrestigeLevel(upgrade);
        }

        /// <summary>Token cost of the next <paramref name="levels"/> levels of an upgrade.</summary>
        public double GetPrestigeCost(PrestigeUpgradeData upgrade, int levels = 1)
        {
            if (upgrade == null || levels <= 0)
            {
                return 0d;
            }

            int currentLevel = GetPrestigeLevel(upgrade);
            return FormulaUtility.StatUpgradeBulkCost(upgrade.BaseCostTokens, currentLevel, levels, upgrade.CostGrowth);
        }

        public bool CanAffordPrestige(PrestigeUpgradeData upgrade, int levels = 1)
        {
            return economy != null && economy.CanAfford(CurrencyType.PrestigeTokens, GetPrestigeCost(upgrade, levels));
        }

        public bool IsPrestigeMaxed(PrestigeUpgradeData upgrade)
        {
            return upgrade != null && upgrade.IsAtMaxLevel(GetPrestigeLevel(upgrade));
        }

        /// <summary>Buys permanent upgrade levels with Prestige Tokens.</summary>
        public bool TryBuyPrestigeUpgrade(PrestigeUpgradeData upgrade, int levels = 1)
        {
            if (upgrade == null || levels <= 0 || economy == null || resolver == null)
            {
                return false;
            }

            int currentLevel = GetPrestigeLevel(upgrade);
            if (upgrade.IsAtMaxLevel(currentLevel))
            {
                GameEvents.RaiseToast($"{upgrade.DisplayName} is already maxed.");
                return false;
            }

            int requestedLevels = levels;
            if (upgrade.HasLevelCap)
            {
                int remaining = upgrade.MaxLevel - currentLevel;
                if (requestedLevels > remaining)
                {
                    requestedLevels = remaining;
                }
            }

            double cost = GetPrestigeCost(upgrade, requestedLevels);
            if (!economy.SpendTokens(cost))
            {
                GameEvents.RaiseToast($"Not enough tokens (need {NumberFormatter.Format(cost)}).");
                return false;
            }

            int newLevel = currentLevel + requestedLevels;
            resolver.SetPrestigeLevel(upgrade, newLevel);
            GameEvents.RaiseToast($"{upgrade.DisplayName} -> level {newLevel}");

            return true;
        }

        /// <summary>Summary line for logs, e.g. "Gold x1.20 Damage x1.10 HP x1.00".</summary>
        public string DescribeMultipliers()
        {
            if (resolver == null)
            {
                return "no resolver";
            }

            return $"Gold x{resolver.GlobalGoldMultiplier:0.00} Damage x{resolver.GlobalDamageMultiplier:0.00} " +
                   $"HP x{resolver.GlobalHealthMultiplier:0.00}";
        }
    }
}