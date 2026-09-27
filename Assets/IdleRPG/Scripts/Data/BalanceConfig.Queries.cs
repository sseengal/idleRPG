using System.Collections.Generic;
using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// Derived reads over <see cref="BalanceConfig"/>'s raw fields: clamps, aliases and lookups.
    /// </summary>
    public partial class BalanceConfig : ScriptableObject
    {
        /// <summary>Gem table for day 1..N (never empty; falls back to a sane default).</summary>
        public int[] DailyStreakGems
        {
            get
            {
                if (dailyStreakGems != null && dailyStreakGems.Length > 0 && dailyStreakGems[0] > 0)
                {
                    return dailyStreakGems;
                }

                return new[] { 5, 8, 10, 12, 15, 18, 25 };
            }
        }

        /// <summary>The streak's ceiling: how many consecutive days the table covers.</summary>
        public int DailyStreakCap => DailyStreakGems.Length;

        /// <summary>The streak-cap day also starts a gold boost.</summary>
        public bool DailyStreakDay7Boost => dailyStreakDay7Boost;

        /// <summary>Gems a streak of <paramref name="day"/> days pays (clamped to the cap).</summary>
        public int DailyStreakGemsForDay(int day)
        {
            int[] table = DailyStreakGems;
            int index = day < 1 ? 0 : (day > table.Length ? table.Length - 1 : day - 1);
            return table[index];
        }

        // ------------------------------------------------------------------
        // Ad placements (B7 S3)
        // ------------------------------------------------------------------
        /// <summary>Per-day limits for a placement; falls back to a sane default when unconfigured.</summary>
        public AdPlacementDef GetAdPlacement(AdPlacementId placementId)
        {
            if (adPlacements != null)
            {
                for (int i = 0; i < adPlacements.Count; i++)
                {
                    if (adPlacements[i] != null && adPlacements[i].PlacementId == placementId)
                    {
                        return adPlacements[i];
                    }
                }
            }

            return placementId == AdPlacementId.GoldBoost
                ? new AdPlacementDef(AdPlacementId.GoldBoost, 5, 300f)
                : new AdPlacementDef(AdPlacementId.DoubleOffline, 3, 30f);
        }

        public float WaveTransitionDelaySec => Mathf.Max(0f, waveTransitionDelaySec);

        public double MinDamageRatio => Mathf.Clamp(minDamageRatio, 0f, 1f);

        public float CriticalChance => Mathf.Clamp01(criticalChance);

        public float CriticalDamageMultiplier => Mathf.Max(1f, criticalDamageMultiplier);

        public bool ReviveHeroesEachWave => reviveHeroesEachWave;

        public float UpgradeCostGrowth => Mathf.Max(1f, upgradeCostGrowth);

        public float UpgradeStatGainPerLevel => Mathf.Max(0f, upgradeStatGainPerLevel);

        public float PrestigeStageDivisor => Mathf.Max(1f, prestigeStageDivisor);

        public float PrestigeExponent => Mathf.Max(0f, prestigeExponent);

        public int MinStageToAscend => Mathf.Max(1, minStageToAscend);

        public float OfflineCapSeconds => Mathf.Max(0f, offlineCapSeconds);

        public double OfflineEfficiency => Mathf.Clamp(offlineEfficiency, 0f, 1f);

        public float OfflineMaxEquivalentSeconds => Mathf.Max(0f, offlineMaxEquivalentSeconds);

        public float OfflineEstimatedSecondsPerKill => Mathf.Max(0.1f, offlineEstimatedSecondsPerKill);

        public float MinOfflineSecondsForPopup => Mathf.Max(0f, minOfflineSecondsForPopup);

        public float OfflineCapExtensionSeconds => Mathf.Max(60f, offlineCapExtensionSeconds);

        public double OfflineCapExtensionGemCost => offlineCapExtensionGemCost < 0d ? 0d : offlineCapExtensionGemCost;

        public float OfflineCapExtensionMaxSeconds => Mathf.Max(0f, offlineCapExtensionMaxSeconds);

        public float InstantIncomeSeconds => Mathf.Max(60f, instantIncomeSeconds);

        public double InstantIncomeGemCost => instantIncomeGemCost < 0d ? 0d : instantIncomeGemCost;

        public double StartingGold => startingGold < 0d ? 0d : startingGold;

        public double StartingGems => startingGems < 0d ? 0d : startingGems;

        public float GoldPerSecondSampleWindowSec => Mathf.Max(5f, goldPerSecondSampleWindowSec);

        public double AdGoldBoostMultiplier => adGoldBoostMultiplier < 1d ? 1d : adGoldBoostMultiplier;

        public float AdGoldBoostDurationSec => Mathf.Max(0f, adGoldBoostDurationSec);

        public float AutosaveIntervalSec => Mathf.Max(5f, autosaveIntervalSec);
    }
}
