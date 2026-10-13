using System.Collections.Generic;
using UnityEngine;
using IdleRPG.Abilities;
using IdleRPG.Combat;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Equipment;
using IdleRPG.Progression;
using IdleRPG.Save;
using IdleRPG.Sim;
using IdleRPG.Utils;

namespace IdleRPG.Core
{
    /// <summary>
    /// The save contract in one place: live systems -> <see cref="SaveData"/>, and <see cref="SaveData"/> -> live
    /// systems.
    ///
    /// ELI5: the form the game fills in when it writes the player's progress, and reads back when it starts. Every
    /// field on that form is named here and nowhere else.
    ///
    /// Why it is its own class: the save is the one surface that must never silently regress, and while this lived
    /// inside <c>GameManager</c> it could only be exercised by booting a whole scene. Now the mapping can be driven
    /// from a test or a tool with a bundle of services and a payload.
    ///
    /// Why a <see cref="Services"/> bundle and not the <c>GameContext</c>: the context is filled LAST during wiring
    /// (it needs every service, Save and Idle included) and the backup-recovery path saves from inside
    /// <c>WireUp()</c>, so reading the context here would hit a half-built box. The caller gathers the references it
    /// has at the moment of the call.
    /// </summary>
    public static class SaveSnapshotMapper
    {
        /// <summary>The live systems the save contract touches, gathered by the caller at the moment of the call.</summary>
        public struct Services
        {
            public CombatManager Combat;
            public EconomyManager Economy;
            public StatResolver Resolver;
            public PartyConfig Party;
            public Formation Formation;
            public TrackService Tracks;
            public AutomationService Automation;
            public BoostManager Boost;
            public SimLedger Ledger;
            public ShopService Shop;
            public ItemService Gear;
            public DailyStreakService DailyStreak;
            public AdCapsService AdCaps;
            public FirstRunTips FirstRunTips;

            /// <summary>Hero abilities: the unlock levels and which hero wears which.</summary>
            public AbilityService Abilities;
        }

        /// <summary>The run and lifetime numbers the save needs that no service owns.</summary>
        public struct RunState
        {
            public int CurrentStage;

            /// <summary>Wave to resume on (the manager's <c>RestoredWave</c>).</summary>
            public int RestoredWave;

            public int HighestStageReached;
            public int RunBestStage;
            public double RunGoldEarned;
            public int RunKills;
            public int RunStagesCleared;
            public double RunStartBinary;
            public int TotalKills;
            public double TotalGoldEarned;
            public int AscensionCount;
            public int LastPageIndex;
        }
        /// <summary>Builds the payload written to disk.</summary>
        public static SaveData Capture(Services services, RunState run)
        {
            SaveData data = SaveData.CreateDefault();

            data.currentStage = run.CurrentStage;
            data.currentWave = services.Combat != null ? services.Combat.CurrentWave : 1;
            data.highestStageReached = run.HighestStageReached;
            data.runBestStage = run.RunBestStage;
            data.runGoldEarned = run.RunGoldEarned;
            data.runKills = run.RunKills;
            data.runStagesCleared = run.RunStagesCleared;
            data.runStartBinary = run.RunStartBinary;

            data.gold = services.Economy != null ? services.Economy.Gold : 0d;
            data.gems = services.Economy != null ? services.Economy.Gems : 0d;
            data.prestigeTokens = services.Economy != null ? services.Economy.PrestigeTokens : 0d;

            services.Resolver?.WriteToSave(data, services.Party);
            services.Tracks?.WriteToSave(data);
            services.Automation?.WriteToSave(data);
            services.Boost?.WriteToSave(data);
            if (services.Ledger != null)
            {
                // B6 Step 3 honesty guard: the measured rate is normalised to base tempo, so the Speed Button
                // (or any future boost) can never inflate the offline / instant-income quotes.
                double tempo = services.Automation != null ? services.Automation.SpeedMultiplier : 1d;
                data.lastGoldPerSecond = tempo > 1d ? services.Ledger.GoldPerSecond / tempo : services.Ledger.GoldPerSecond;
            }

            services.Shop?.WriteToSave(data);
            services.Gear?.WriteToSave(data);
            services.Abilities?.WriteToSave(data);

            if (services.Formation != null)
            {
                data.partySlots = new List<int>(services.Formation.ToSlotArray());
            }

            data.totalKills = run.TotalKills;
            data.totalGoldEarned = run.TotalGoldEarned;
            data.ascensionCount = run.AscensionCount;
            data.lastPageIndex = run.LastPageIndex;
            data.dailyStreakLastDate = services.DailyStreak?.LastClaimDate ?? "";
            data.dailyStreakCount = services.DailyStreak?.StreakCount ?? 0;
            services.AdCaps?.WriteToSave(data);
            if (services.FirstRunTips != null)
            {
                data.SetLevel(SaveData.SaveKeys.TipMask(), services.FirstRunTips.Mask);
            }

            return data;
        }

        /// <summary>
        /// Pushes a loaded payload into the live systems (before combat starts) and returns the run state it
        /// produced, clamped so a damaged or older payload cannot put the game in an impossible state.
        /// </summary>
        public static RunState Apply(SaveData data, Services services, RunState current)
        {
            RunState run = current;

            if (data == null)
            {
                return run;
            }

            // Formation first: the board has to be right before the party is built from it. The hero count is the
            // roster size, so a hero missing from an older save lands in a free slot instead of vanishing.
            if (services.Formation != null)
            {
                int rosterSize = services.Party != null ? services.Party.Heroes.Count : 0;
                services.Formation.ApplySlotArray(data.partySlots != null ? data.partySlots.ToArray() : null, rosterSize);
            }

            run.CurrentStage = Mathf.Max(1, data.currentStage);
            run.RestoredWave = Mathf.Max(1, data.currentWave);
            run.HighestStageReached = Mathf.Max(1, data.highestStageReached, run.CurrentStage);
            run.RunBestStage = Mathf.Clamp(Mathf.Max(1, data.runBestStage), 1, run.HighestStageReached);

            run.RunGoldEarned = data.runGoldEarned < 0d ? 0d : data.runGoldEarned;
            run.RunKills = Mathf.Max(0, data.runKills);
            run.RunStagesCleared = Mathf.Max(0, data.runStagesCleared);
            run.RunStartBinary = data.runStartBinary > 0d ? data.runStartBinary : GameClock.NowBinary;

            run.TotalKills = data.totalKills;
            run.TotalGoldEarned = data.totalGoldEarned;
            run.AscensionCount = data.ascensionCount;
            run.LastPageIndex = data.lastPageIndex;

            services.Economy?.Restore(data.gold, data.gems, data.prestigeTokens);
            services.Resolver?.FillFromSave(data, services.Party);
            services.Tracks?.FillFromSave(data);
            services.Automation?.FillFromSave(data);
            services.Boost?.Restore(data.goldBoostActive, data.goldBoostExpiresAtBinary);
            services.Ledger?.SeedGoldPerSecond(data.lastGoldPerSecond);
            services.Shop?.Restore(data.offlineEquivalentCapBonusSeconds, data.offlineCapExtensionsPurchased);
            services.Gear?.FillFromSave(data);
            services.DailyStreak?.Restore(data.dailyStreakLastDate, data.dailyStreakCount);
            services.AdCaps?.Restore(data.adRedemptions);
            services.FirstRunTips?.Restore(data.GetLevel(SaveData.SaveKeys.TipMask()));
            services.Abilities?.LoadFromSave(data);

            return run;
        }
    }
}
