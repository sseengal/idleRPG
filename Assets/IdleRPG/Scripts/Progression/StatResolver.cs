using System;
using System.Collections.Generic;
using UnityEngine;
using IdleRPG.Data;
using IdleRPG.Save;

namespace IdleRPG.Progression
{
    /// <summary>
    /// Owns every persistent progression number: hero ATK/HP/DEF levels and permanent
    /// (prestige) upgrade levels. Implements <see cref="ICombatStatProvider"/> so combat
    /// reads final stats without ever knowing upgrades exist.
    /// </summary>
    public sealed class StatResolver : ICombatStatProvider
    {
        private readonly BalanceConfig balanceConfig;
        private readonly List<StatUpgradeData> statUpgrades;
        private readonly List<PrestigeUpgradeData> prestigeUpgrades;

        private readonly Dictionary<string, int> heroLevels = new Dictionary<string, int>();
        private readonly Dictionary<string, int> prestigeLevels = new Dictionary<string, int>();

        public StatResolver(BalanceConfig balanceConfig, IEnumerable<StatUpgradeData> statUpgrades, IEnumerable<PrestigeUpgradeData> prestigeUpgrades)
        {
            this.balanceConfig = balanceConfig;
            this.statUpgrades = new List<StatUpgradeData>(statUpgrades ?? Array.Empty<StatUpgradeData>());
            this.prestigeUpgrades = new List<PrestigeUpgradeData>(prestigeUpgrades ?? Array.Empty<PrestigeUpgradeData>());

            if (this.balanceConfig == null)
            {
                Debug.LogError("[StatResolver] BalanceConfig is null; falling back to defaults.");
            }
        }

        /// <summary>Raised whenever any level or multiplier changes (combat should refresh).</summary>
        public event Action StatsChanged;

        // ------------------------------------------------------------------
        // Hero levels
        // ------------------------------------------------------------------
        public int GetHeroLevel(HeroData hero, HeroStatType statType)
        {
            if (hero == null)
            {
                return 0;
            }

            return heroLevels.TryGetValue(Key(hero, statType), out int level) ? level : 0;
        }

        public void SetHeroLevel(HeroData hero, HeroStatType statType, int level)
        {
            if (hero == null)
            {
                return;
            }

            int safeLevel = level < 0 ? 0 : level;
            string key = Key(hero, statType);

            // No-op guard: avoids pointless combat refreshes and event spam.
            if (heroLevels.TryGetValue(key, out int existing) && existing == safeLevel)
            {
                return;
            }

            heroLevels[key] = safeLevel;
            StatsChanged?.Invoke();
        }

        /// <summary>Wipes every hero level (ascension reset).</summary>
        public void ResetHeroLevels()
        {
            heroLevels.Clear();
            StatsChanged?.Invoke();
        }

        /// <summary>Total levels invested across all heroes — handy for logs and UI.</summary>
        public int TotalHeroLevels
        {
            get
            {
                int total = 0;

                foreach (KeyValuePair<string, int> pair in heroLevels)
                {
                    total += pair.Value;
                }

                return total;
            }
        }

        // ------------------------------------------------------------------
        // Permanent upgrades
        // ------------------------------------------------------------------
        public int GetPrestigeLevel(PrestigeUpgradeData upgrade)
        {
            if (upgrade == null)
            {
                return 0;
            }

            return prestigeLevels.TryGetValue(upgrade.UpgradeID, out int level) ? level : 0;
        }

        public void SetPrestigeLevel(PrestigeUpgradeData upgrade, int level)
        {
            if (upgrade == null)
            {
                return;
            }

            prestigeLevels[upgrade.UpgradeID] = level < 0 ? 0 : level;
            StatsChanged?.Invoke();
        }

        /// <summary>Global multiplier for one effect type, e.g. 1.15 for +15% gold.</summary>
        public double GetEffectMultiplier(PrestigeEffectType effectType)
        {
            double bonus = 0d;

            for (int i = 0; i < prestigeUpgrades.Count; i++)
            {
                PrestigeUpgradeData upgrade = prestigeUpgrades[i];
                if (upgrade == null || upgrade.EffectType != effectType)
                {
                    continue;
                }

                bonus += upgrade.GetTotalBonus(GetPrestigeLevel(upgrade));
            }

            return 1d + bonus;
        }

        // ------------------------------------------------------------------
        // Upgrade metadata (cost / gain) — the single source used by UI and purchases
        // ------------------------------------------------------------------
        /// <summary>The StatUpgradeData asset driving a stat, or null when none is configured.</summary>
        public StatUpgradeData GetUpgradeData(HeroStatType statType)
        {
            for (int i = 0; i < statUpgrades.Count; i++)
            {
                StatUpgradeData data = statUpgrades[i];
                if (data != null && data.StatType == statType)
                {
                    return data;
                }
            }

            return null;
        }

        public double GetStatGainFraction(HeroStatType statType)
        {
            StatUpgradeData data = GetUpgradeData(statType);
            if (data != null)
            {
                return data.StatGainPerLevelFraction;
            }

            return balanceConfig != null ? balanceConfig.UpgradeStatGainPerLevel : 0.1d;
        }

        /// <summary>Cost of the next <paramref name="levels"/> levels of a stat for one hero.</summary>
        public double GetUpgradeCost(HeroStatType statType, int currentLevel, int levels)
        {
            StatUpgradeData data = GetUpgradeData(statType);
            double baseCost = data != null ? data.BaseCost : 10d;
            double growth = data != null
                ? data.GetCostGrowth(balanceConfig != null ? balanceConfig.UpgradeCostGrowth : 1.07f)
                : 1.07d;

            return FormulaUtility.StatUpgradeBulkCost(baseCost, currentLevel, levels, growth);
        }

        /// <summary>Level-0 cost of a stat track (used for closed-form affordability maths).</summary>
        public double GetBaseUpgradeCost(HeroStatType statType)
        {
            StatUpgradeData data = GetUpgradeData(statType);
            return data != null ? data.BaseCost : 10d;
        }

        /// <summary>Cost growth per level of a stat track.</summary>
        public double GetUpgradeCostGrowth(HeroStatType statType)
        {
            StatUpgradeData data = GetUpgradeData(statType);
            double fallback = balanceConfig != null ? balanceConfig.UpgradeCostGrowth : 1.07d;
            return data != null ? data.GetCostGrowth((float)fallback) : fallback;
        }

        /// <summary>True when the stat has a level cap and it has been reached.</summary>
        public bool IsAtMaxLevel(HeroStatType statType, int currentLevel)
        {
            StatUpgradeData data = GetUpgradeData(statType);
            return data != null && data.IsAtMaxLevel(currentLevel);
        }

        // ------------------------------------------------------------------
        // ICombatStatProvider — final stats the simulation consumes
        // ------------------------------------------------------------------
        public double GetMaxHealth(HeroData hero, int heroIndex)
        {
            if (hero == null)
            {
                return 0d;
            }

            return FormulaUtility.HeroStatValue(
                hero.BaseHealth,
                GetHeroLevel(hero, HeroStatType.Health),
                GetStatGainFraction(HeroStatType.Health),
                GlobalHealthMultiplier);
        }

        public double GetAttack(HeroData hero, int heroIndex)
        {
            if (hero == null)
            {
                return 0d;
            }

            return FormulaUtility.HeroStatValue(
                hero.BaseAttack,
                GetHeroLevel(hero, HeroStatType.Attack),
                GetStatGainFraction(HeroStatType.Attack),
                GlobalDamageMultiplier);
        }

        public double GetDefense(HeroData hero, int heroIndex)
        {
            if (hero == null)
            {
                return 0d;
            }

            return FormulaUtility.HeroStatValue(
                hero.BaseDefense,
                GetHeroLevel(hero, HeroStatType.Defense),
                GetStatGainFraction(HeroStatType.Defense));
        }

        public double GetAttackInterval(HeroData hero, int heroIndex)
        {
            return hero == null ? HeroData.MinAttackIntervalSec : hero.AttackIntervalSec;
        }

        public double GlobalDamageMultiplier => GetEffectMultiplier(PrestigeEffectType.DamagePercent);

        public double GlobalHealthMultiplier => GetEffectMultiplier(PrestigeEffectType.HealthPercent);

        public double GlobalGoldMultiplier => GetEffectMultiplier(PrestigeEffectType.GoldPercent);

        // ------------------------------------------------------------------
        // Save integration (Step 5 wires the file I/O; nothing here needs rework then)
        // ------------------------------------------------------------------
        public void FillFromSave(SaveData data, PartyConfig party)
        {
            if (data == null)
            {
                return;
            }

            heroLevels.Clear();

            if (party != null)
            {
                for (int i = 0; i < party.Heroes.Count; i++)
                {
                    HeroData hero = party.GetHero(i);
                    if (hero == null)
                    {
                        continue;
                    }

                    HeroProgressRecord record = data.GetOrCreateHero(hero.HeroID);
                    SetHeroLevel(hero, HeroStatType.Attack, record.attackLevel);
                    SetHeroLevel(hero, HeroStatType.Health, record.healthLevel);
                    SetHeroLevel(hero, HeroStatType.Defense, record.defenseLevel);
                }
            }

            prestigeLevels.Clear();

            for (int i = 0; i < prestigeUpgrades.Count; i++)
            {
                PrestigeUpgradeData upgrade = prestigeUpgrades[i];
                if (upgrade != null)
                {
                    prestigeLevels[upgrade.UpgradeID] = data.GetPrestigeLevel(upgrade.UpgradeID);
                }
            }

            StatsChanged?.Invoke();
        }

        public void WriteToSave(SaveData data, PartyConfig party)
        {
            if (data == null)
            {
                return;
            }

            if (party != null)
            {
                for (int i = 0; i < party.Heroes.Count; i++)
                {
                    HeroData hero = party.GetHero(i);
                    if (hero == null)
                    {
                        continue;
                    }

                    HeroProgressRecord record = data.GetOrCreateHero(hero.HeroID);
                    record.attackLevel = GetHeroLevel(hero, HeroStatType.Attack);
                    record.healthLevel = GetHeroLevel(hero, HeroStatType.Health);
                    record.defenseLevel = GetHeroLevel(hero, HeroStatType.Defense);
                }
            }

            for (int i = 0; i < prestigeUpgrades.Count; i++)
            {
                PrestigeUpgradeData upgrade = prestigeUpgrades[i];
                if (upgrade == null)
                {
                    continue;
                }

                data.GetOrCreatePrestigeUpgrade(upgrade.UpgradeID).level = GetPrestigeLevel(upgrade);
            }
        }

        private static string Key(HeroData hero, HeroStatType statType)
        {
            return hero.HeroID + ":" + (int)statType;
        }
    }
}