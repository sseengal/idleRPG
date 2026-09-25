using System;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Utils;

namespace IdleRPG.Progression
{
    /// <summary>
    /// Buys hero stat levels with Gold. All cost maths lives in <see cref="FormulaUtility"/> and the purchase
    /// itself runs through <see cref="TrackService"/> - the single checkout for every progression row. This class
    /// keeps its public surface (the Step 4 UI reads it) and only adds the gold/token naming and events on top.
    /// </summary>
    public sealed class UpgradeManager
    {
        private readonly EconomyManager economy;
        private readonly StatResolver resolver;
        private readonly PartyConfig party;
        private readonly TrackService trackService;

        public UpgradeManager(EconomyManager economy, StatResolver resolver, PartyConfig party, TrackService trackService)
        {
            this.economy = economy;
            this.resolver = resolver;
            this.party = party;
            this.trackService = trackService;

            if (this.economy == null || this.resolver == null || this.party == null || this.trackService == null)
            {
                Debug.LogError("[UpgradeManager] Requires EconomyManager, StatResolver, PartyConfig and TrackService.");
            }
        }

        /// <summary>(heroIndex, statType, newLevel, goldCost) after a successful purchase.</summary>
        public event Action<int, HeroStatType, int, double> UpgradePurchased;

        /// <summary>Gold spent on hero levels this session (debug/telemetry).</summary>
        public double TotalSpentGold { get; private set; }

        // ------------------------------------------------------------------
        // Queries (used by the Step 4 UI)
        // ------------------------------------------------------------------
        public bool IsValidHero(int heroIndex)
        {
            return party != null && party.GetHero(heroIndex) != null;
        }

        public int GetLevel(int heroIndex, HeroStatType statType)
        {
            HeroData hero = party == null ? null : party.GetHero(heroIndex);
            return resolver == null ? 0 : resolver.GetHeroLevel(hero, statType);
        }

        /// <summary>Cost of the next <paramref name="levels"/> levels for one hero.</summary>
        public double GetCost(int heroIndex, HeroStatType statType, int levels = 1)
        {
            HeroData hero = party == null ? null : party.GetHero(heroIndex);
            if (hero == null || resolver == null || levels <= 0)
            {
                return 0d;
            }

            return resolver.GetUpgradeCost(statType, resolver.GetHeroLevel(hero, statType), levels);
        }

        public bool CanAfford(int heroIndex, HeroStatType statType, int levels = 1)
        {
            double cost = GetCost(heroIndex, statType, levels);
            return economy != null && economy.CanAfford(CurrencyType.Gold, cost);
        }

        public bool IsAtMaxLevel(int heroIndex, HeroStatType statType)
        {
            return resolver != null && resolver.IsAtMaxLevel(statType, GetLevel(heroIndex, statType));
        }

        /// <summary>
        /// How many levels the current gold can buy, in closed form (no purchase loop).
        /// Capped by the stat's max level and <paramref name="hardCap"/>.
        /// </summary>
        public int MaxAffordableLevels(int heroIndex, HeroStatType statType, int hardCap = 1000)
        {
            HeroData hero = party == null ? null : party.GetHero(heroIndex);
            if (hero == null || resolver == null || economy == null)
            {
                return 0;
            }

            int currentLevel = resolver.GetHeroLevel(hero, statType);
            if (resolver.IsAtMaxLevel(statType, currentLevel))
            {
                return 0;
            }

            double gold = economy.Gold;
            double baseCost = resolver.GetBaseUpgradeCost(statType);
            double growth = resolver.GetUpgradeCostGrowth(statType);
            double firstCost = baseCost * Math.Pow(growth, currentLevel);

            if (firstCost <= 0d)
            {
                return 0;
            }

            int affordable;

            if (Math.Abs(growth - 1d) < double.Epsilon)
            {
                affordable = (int)Math.Floor(gold / firstCost);
            }
            else
            {
                double ratio = 1d + gold * (growth - 1d) / firstCost;
                affordable = ratio <= 1d ? 0 : (int)Math.Floor(Math.Log(ratio, growth));
            }

            if (affordable < 0)
            {
                affordable = 0;
            }

            StatUpgradeData data = resolver.GetUpgradeData(statType);
            if (data != null && data.HasLevelCap)
            {
                int remaining = data.MaxLevel - currentLevel;
                if (affordable > remaining)
                {
                    affordable = remaining;
                }
            }

            return affordable > hardCap ? hardCap : affordable;
        }

        /// <summary>Human-readable cost for the UI, e.g. "1.23K".</summary>
        public string GetCostLabel(int heroIndex, HeroStatType statType, int levels = 1)
        {
            return NumberFormatter.Format(GetCost(heroIndex, statType, levels));
        }

        // ------------------------------------------------------------------
        // Purchases
        // ------------------------------------------------------------------
        /// <summary>
        /// Buys <paramref name="levels"/> levels of one stat for one hero.
        /// Spends nothing and returns false when capped or unaffordable.
        /// </summary>
        public bool TryUpgrade(int heroIndex, HeroStatType statType, int levels = 1)
        {
            if (levels <= 0 || economy == null || resolver == null)
            {
                return false;
            }

            HeroData hero = party == null ? null : party.GetHero(heroIndex);
            if (hero == null)
            {
                Debug.LogWarning($"[UpgradeManager] No hero at index {heroIndex}.");
                return false;
            }

            int currentLevel = resolver.GetHeroLevel(hero, statType);

            if (!trackService.TryGetHeroTrack(statType, out ProgressionTrack track))
            {
                Debug.LogWarning($"[UpgradeManager] No upgrade track for stat {statType}.");
                return false;
            }

            if (track.IsAtMaxLevel(currentLevel))
            {
                GameEvents.RaiseToast($"{statType.ToDisplayName()} is already at max level.");
                return false;
            }

            // The single checkout: clamps to the cap, prices it, spends gold, writes the level.
            PurchaseResult result = trackService.TryBuy(track, hero, levels, out double cost, out int newLevel);

            if (result == PurchaseResult.Maxed)
            {
                GameEvents.RaiseToast($"{statType.ToDisplayName()} is already at max level.");
                return false;
            }

            if (result == PurchaseResult.CannotAfford)
            {
                double quoted = trackService.Cost(track, hero, track.ClampLevels(currentLevel, levels));
                GameEvents.RaiseToast($"Not enough gold (need {NumberFormatter.Format(quoted)}).");
                return false;
            }

            if (result != PurchaseResult.Bought)
            {
                return false;
            }

            TotalSpentGold += cost;

            UpgradePurchased?.Invoke(heroIndex, statType, newLevel, cost);
            GameEvents.RaiseHeroLevelChanged(heroIndex, statType, newLevel);
            GameEvents.RaiseUpgradePurchased(heroIndex, statType, newLevel, cost);

            return true;
        }

        /// <summary>
        /// Buys the same upgrade for every hero that can afford it. Returns how many
        /// purchases succeeded — useful for a "buys for all" button later.
        /// </summary>
        public int TryUpgradeAll(HeroStatType statType, int levels = 1)
        {
            if (party == null)
            {
                return 0;
            }

            int successes = 0;

            for (int i = 0; i < party.Heroes.Count; i++)
            {
                if (party.GetHero(i) == null)
                {
                    continue;
                }

                if (TryUpgrade(i, statType, levels))
                {
                    successes++;
                }
            }

            return successes;
        }

        /// <summary>Levels the current gold can buy for every hero (UI badge helper).</summary>
        public int GetMaxAffordableForAll(HeroStatType statType, int hardCap = 100)
        {
            if (party == null)
            {
                return 0;
            }

            int lowest = int.MaxValue;

            for (int i = 0; i < party.Heroes.Count; i++)
            {
                if (party.GetHero(i) == null)
                {
                    continue;
                }

                int affordable = MaxAffordableLevels(i, statType, hardCap);
                if (affordable < lowest)
                {
                    lowest = affordable;
                }
            }

            return lowest == int.MaxValue ? 0 : lowest;
        }
    }
}