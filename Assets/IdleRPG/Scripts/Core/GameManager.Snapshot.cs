using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using IdleRPG.Combat;
using IdleRPG.Data;
using IdleRPG.DebugTools;
using IdleRPG.Economy;
using IdleRPG.Sim;
using IdleRPG.Progression;
using IdleRPG.Save;
using IdleRPG.Services;
using IdleRPG.Utils;

namespace IdleRPG.Core
{
    /// <summary>
    /// Persistence plumbing: snapshot capture/apply (the save contract <-> live systems) and the one gold funnel.
    /// </summary>
    public sealed partial class GameManager : MonoBehaviour
    {

        /// <summary>Builds the payload written to disk. Called by <see cref="SaveManager"/>.</summary>
        private SaveData CaptureSnapshot()
        {
            SaveData data = SaveData.CreateDefault();

            data.currentStage = CurrentStage;
            data.currentWave = combatManager != null ? combatManager.CurrentWave : 1;
            data.highestStageReached = HighestStageReached;
            data.runBestStage = RunBestStage;
            data.runGoldEarned = RunGoldEarned;
            data.runKills = RunKills;
            data.runStagesCleared = RunStagesCleared;
            data.runStartBinary = runStartBinary;

            data.gold = Economy != null ? Economy.Gold : 0d;
            data.gems = Economy != null ? Economy.Gems : 0d;
            data.prestigeTokens = Economy != null ? Economy.PrestigeTokens : 0d;

            Resolver?.WriteToSave(data, partyConfig);
            Tracks?.WriteToSave(data);
            Automation?.WriteToSave(data);
            Boost?.WriteToSave(data);
            if (Ledger != null)
            {
                // B6 Step 3 honesty guard: the measured rate is normalised to base tempo, so the Speed Button
                // (or any future boost) can never inflate the offline / instant-income quotes.
                double tempo = Automation != null ? Automation.SpeedMultiplier : 1d;
                data.lastGoldPerSecond = tempo > 1d ? Ledger.GoldPerSecond / tempo : Ledger.GoldPerSecond;
            }

            Shop?.WriteToSave(data);
            Gear?.WriteToSave(data);

            if (Formation != null)
            {
                data.partySlots = new List<int>(Formation.ToSlotArray());
            }

            data.totalKills = TotalKills;
            data.totalGoldEarned = TotalGoldEarned;
            data.ascensionCount = AscensionCount;
            data.lastPageIndex = LastPageIndex;
            data.dailyStreakLastDate = DailyStreak?.LastClaimDate ?? "";
            data.dailyStreakCount = DailyStreak?.StreakCount ?? 0;
            AdCaps?.WriteToSave(data);
            if (FirstRunTips != null)
            {
                data.SetLevel(SaveData.SaveKeys.TipMask(), FirstRunTips.Mask);
            }

            return data;
        }

        /// <summary>Pushes a loaded payload into the live systems (before combat starts).</summary>
        private void ApplySnapshot(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            // Formation first: the board has to be right before the party is built from it. The hero count is the
            // roster size, so a hero missing from an older save lands in a free slot instead of vanishing.
            if (Formation != null)
            {
                int rosterSize = partyConfig != null ? partyConfig.Heroes.Count : 0;
                Formation.ApplySlotArray(data.partySlots != null ? data.partySlots.ToArray() : null, rosterSize);
            }

            CurrentStage = Mathf.Max(1, data.currentStage);
            RestoredWave = Mathf.Max(1, data.currentWave);
            HighestStageReached = Mathf.Max(1, data.highestStageReached, CurrentStage);
            RunBestStage = Mathf.Clamp(Mathf.Max(1, data.runBestStage), 1, HighestStageReached);

            RunGoldEarned = data.runGoldEarned < 0d ? 0d : data.runGoldEarned;
            RunKills = Mathf.Max(0, data.runKills);
            RunStagesCleared = Mathf.Max(0, data.runStagesCleared);
            runStartBinary = data.runStartBinary > 0d ? data.runStartBinary : GameClock.NowBinary;

            TotalKills = data.totalKills;
            TotalGoldEarned = data.totalGoldEarned;
            AscensionCount = data.ascensionCount;
            LastPageIndex = data.lastPageIndex;

            Economy?.Restore(data.gold, data.gems, data.prestigeTokens);
            Resolver?.FillFromSave(data, partyConfig);
            Tracks?.FillFromSave(data);
            Automation?.FillFromSave(data);
            Boost?.Restore(data.goldBoostActive, data.goldBoostExpiresAtBinary);
            Ledger?.SeedGoldPerSecond(data.lastGoldPerSecond);
            Shop?.Restore(data.offlineEquivalentCapBonusSeconds, data.offlineCapExtensionsPurchased);
            Gear?.FillFromSave(data);
            DailyStreak?.Restore(data.dailyStreakLastDate, data.dailyStreakCount);
            AdCaps?.Restore(data.adRedemptions);
            FirstRunTips?.Restore(data.GetLevel(SaveData.SaveKeys.TipMask()));

            // Give the loaded values to combat (Initialize() would reset the stage to 1).
            combatManager.SetProgress(CurrentStage, RestoredWave, healParty: true);

            GameEvents.RaiseSaveLoaded();
            LogFlow($"Loaded save: stage {CurrentStage} wave {RestoredWave} best {HighestStageReached} | {Economy}");
        }

        // ------------------------------------------------------------------
        // Rewards, boosts and progression hooks
        // ------------------------------------------------------------------
        /// <summary>
        /// Single funnel for gold: raw combat/offline reward x prestige gold% x ad boost.
        /// Step 5's offline calculation uses this too, so live and offline earnings agree.
        /// </summary>
        public double ResolveGoldReward(double rawGold)
        {
            double prestige = Resolver != null ? Resolver.GlobalGoldMultiplier : 1d;
            double boost = Boost != null ? Boost.GoldMultiplier : 1d;
            return rawGold * prestige * boost;
        }
    }
}
