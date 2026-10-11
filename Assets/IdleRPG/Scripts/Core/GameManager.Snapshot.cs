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
            return SaveSnapshotMapper.Capture(LiveServices(), RunStateNow());
        }

        /// <summary>Pushes a loaded payload into the live systems (before combat starts).</summary>
        private void ApplySnapshot(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            ApplyRunState(SaveSnapshotMapper.Apply(data, LiveServices(), RunStateNow()));

            // Give the loaded values to combat (Initialize() would reset the stage to 1).
            combatManager.SetProgress(CurrentStage, RestoredWave, healParty: true);

            GameEvents.RaiseSaveLoaded();
            LogFlow($"Loaded save: stage {CurrentStage} wave {RestoredWave} best {HighestStageReached} | {Economy}");
        }

        /// <summary>
        /// The live systems the save contract touches, gathered at the moment of the call. Not the
        /// <see cref="Context"/>: it is filled last during wiring, and the backup-recovery path saves from
        /// inside <see cref="WireUp"/>.
        /// </summary>
        private SaveSnapshotMapper.Services LiveServices()
        {
            return new SaveSnapshotMapper.Services
            {
                Combat = combatManager,
                Economy = Economy,
                Resolver = Resolver,
                Party = partyConfig,
                Formation = Formation,
                Tracks = Tracks,
                Automation = Automation,
                Boost = Boost,
                Ledger = Ledger,
                Shop = Shop,
                Gear = Gear,
                DailyStreak = DailyStreak,
                AdCaps = AdCaps,
                FirstRunTips = FirstRunTips
            };
        }

        /// <summary>The run and lifetime numbers the save needs, gathered from the manager's own state.</summary>
        private SaveSnapshotMapper.RunState RunStateNow()
        {
            return new SaveSnapshotMapper.RunState
            {
                CurrentStage = CurrentStage,
                RestoredWave = RestoredWave,
                HighestStageReached = HighestStageReached,
                RunBestStage = RunBestStage,
                RunGoldEarned = RunGoldEarned,
                RunKills = RunKills,
                RunStagesCleared = RunStagesCleared,
                RunStartBinary = runStartBinary,
                TotalKills = TotalKills,
                TotalGoldEarned = TotalGoldEarned,
                AscensionCount = AscensionCount,
                LastPageIndex = LastPageIndex
            };
        }

        /// <summary>Writes the run state the mapper produced back onto the manager.</summary>
        private void ApplyRunState(SaveSnapshotMapper.RunState run)
        {
            CurrentStage = run.CurrentStage;
            RestoredWave = run.RestoredWave;
            HighestStageReached = run.HighestStageReached;
            RunBestStage = run.RunBestStage;
            RunGoldEarned = run.RunGoldEarned;
            RunKills = run.RunKills;
            RunStagesCleared = run.RunStagesCleared;
            runStartBinary = run.RunStartBinary;
            TotalKills = run.TotalKills;
            TotalGoldEarned = run.TotalGoldEarned;
            AscensionCount = run.AscensionCount;
            LastPageIndex = run.LastPageIndex;
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
