using System;
using System.Collections.Generic;
using UnityEngine;
using IdleRPG.Data;
using IdleRPG.Equipment;
using IdleRPG.Save;

namespace IdleRPG.Progression
{
    /// <summary>
    /// Owns every persistent progression number: hero ATK/HP/DEF levels and permanent
    /// (prestige) upgrade levels. Implements <see cref="ICombatStatProvider"/> so combat
    /// reads final stats without ever knowing upgrades exist.
    /// </summary>
    public sealed partial class StatResolver : ICombatStatProvider
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

        /// <summary>
        /// Optional per-hero gear reader (set by the composition root). Returns flat
        /// bonuses as fractions of the hero's base stats; null means no gear yet.
        /// </summary>
        public Func<int, ItemBonuses> GearBonusReader { get; set; }

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
        /// <summary>Equipment changed: combat and the party sheet re-read final stats.</summary>
        public void NotifyGearChanged()
        {
            StatsChanged?.Invoke();
        }

        /// <summary>
        /// Total level of all utility (unlock) upgrades of one type. 0 = not owned;
        /// these carry no multiplier - they are gates the equipment layer queries.
        /// </summary>
        public int GetUtilityLevel(PrestigeEffectType effectType)
        {
            int level = 0;

            for (int i = 0; i < prestigeUpgrades.Count; i++)
            {
                PrestigeUpgradeData upgrade = prestigeUpgrades[i];
                if (upgrade != null && upgrade.EffectType == effectType)
                {
                    level += GetPrestigeLevel(upgrade);
                }
            }

            return level;
        }

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

        /// <summary>
        /// How a stat's per-level gain composes. Additive unless the track asset opts into compounding (B3d):
        /// compounding is what lets affordable power keep pace with the exponential content curve.
        /// </summary>
        public StatEffectMode GetEffectMode(HeroStatType statType)
        {
            StatUpgradeData data = GetUpgradeData(statType);
            return data != null ? data.EffectMode : StatEffectMode.AdditiveBase;
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
                hero.BaseHealth * (1d + Math.Max(0d, GearFraction(b => b.Hp, heroIndex))),
                GetHeroLevel(hero, HeroStatType.Health),
                GetStatGainFraction(HeroStatType.Health),
                GlobalHealthMultiplier,
                GetEffectMode(HeroStatType.Health));
        }

        public double GetAttack(HeroData hero, int heroIndex)
        {
            if (hero == null)
            {
                return 0d;
            }

            return FormulaUtility.HeroStatValue(
                hero.BaseAttack * (1d + Math.Max(0d, GearFraction(b => b.Atk, heroIndex))),
                GetHeroLevel(hero, HeroStatType.Attack),
                GetStatGainFraction(HeroStatType.Attack),
                GlobalDamageMultiplier,
                GetEffectMode(HeroStatType.Attack));
        }

        public double GetDefense(HeroData hero, int heroIndex)
        {
            if (hero == null)
            {
                return 0d;
            }

            return FormulaUtility.HeroStatValue(
                hero.BaseDefense * (1d + Math.Max(0d, GearFraction(b => b.Def, heroIndex))),
                GetHeroLevel(hero, HeroStatType.Defense),
                GetStatGainFraction(HeroStatType.Defense),
                1d,
                GetEffectMode(HeroStatType.Defense));
        }

        /// <summary>Gear bonus fraction for one stat, safe against a missing/rolling reader.</summary>
        private double GearFraction(System.Func<ItemBonuses, double> read, int heroIndex)
        {
            if (GearBonusReader == null)
            {
                return 0d;
            }

            try
            {
                return read(GearBonusReader(heroIndex));
            }
            catch (System.Exception)
            {
                return 0d;
            }
        }

        public double GetAttackInterval(HeroData hero, int heroIndex)
        {
            return hero == null ? HeroData.MinAttackIntervalSec : hero.AttackIntervalSec;
        }

        /// <summary>Crit caps: past these the maths stops being a "lucky hit" and becomes the baseline.</summary>
        public const double MaxCritChance = 0.75d;

        public const double MaxCritDamage = 6d;

        public double GetCritChance(HeroData hero, int heroIndex)
        {
            if (hero == null)
            {
                return 0d;
            }

            double value = FormulaUtility.HeroStatValue(
                hero.BaseCritChance,
                GetHeroLevel(hero, HeroStatType.CritRate),
                GetStatGainFraction(HeroStatType.CritRate),
                1d,
                GetEffectMode(HeroStatType.CritRate));

            return value < 0d ? 0d : (value > MaxCritChance ? MaxCritChance : value);
        }

        public double GetCritDamage(HeroData hero, int heroIndex)
        {
            if (hero == null)
            {
                return 1d;
            }

            double value = FormulaUtility.HeroStatValue(
                hero.BaseCritDamage,
                GetHeroLevel(hero, HeroStatType.CritDamage),
                GetStatGainFraction(HeroStatType.CritDamage),
                1d,
                GetEffectMode(HeroStatType.CritDamage));

            return value < 1d ? 1d : (value > MaxCritDamage ? MaxCritDamage : value);
        }

        public double GlobalDamageMultiplier => GetEffectMultiplier(PrestigeEffectType.DamagePercent);

        public double GlobalHealthMultiplier => GetEffectMultiplier(PrestigeEffectType.HealthPercent);

        public double GlobalGoldMultiplier => GetEffectMultiplier(PrestigeEffectType.GoldPercent);

        private static string Key(HeroData hero, HeroStatType statType)
        {
            return hero.HeroID + ":" + (int)statType;
        }
    }
}
