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
    /// Persistence and time payouts: snapshot capture/apply, save/delete/reset, offline evaluation, instant income and the ad boost.
    /// </summary>
    public sealed partial class GameManager
    {
        // ------------------------------------------------------------------
        // Save API
        // ------------------------------------------------------------------
        /// <summary>Writes the save right now (F5, menus, tests).</summary>
        public bool SaveNow()
        {
            return Save != null && Save.SaveNow("manual");
        }

        /// <summary>Wipes the save (debug tooling / future reset button).</summary>
        public void DeleteSave()
        {
            Idle?.ClearPending();
            Save?.DeleteSave();
        }

        /// <summary>
        /// The nuclear option: a fresh install. Wipes the save, its backups and PlayerPrefs, then reloads the
        /// scene so nothing survives in memory either.
        ///
        /// ELI5: `DeleteSave` only deletes the file on disk - the game keeps running with everything the player
        /// already earned, and the next autosave writes it all back. This one empties the disk *and* starts the
        /// scene again from scratch, which is what a "start over" button (or a corrupted-state bug report) needs.
        /// </summary>
        public bool ResetGame()
        {
            Idle?.ClearPending();
            Save?.DeleteSave();          // save file, every backup generation, logout timestamp

            // A fresh install has no preferences either (audio volume/mute live here).
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            Scene scene = SceneManager.GetActiveScene();

            if (scene.buildIndex < 0)
            {
                Debug.LogWarning($"[GameManager] Reset wiped disk state, but scene '{scene.name}' is not in Build " +
                                 "Settings, so it cannot be reloaded; stop and start Play mode for a clean run.");
                return false;
            }

            Debug.Log($"[GameManager] Full reset: save, backups and PlayerPrefs wiped; reloading '{scene.name}'.");
            SceneManager.LoadScene(scene.buildIndex, LoadSceneMode.Single);
            return true;
        }

        /// <summary>
        /// Offers the offline reward for the time since the last session ended.
        /// Called once on start; also used by the debug tooling to re-run the calculation.
        /// The errand itself lives in <see cref="OfflineCoordinator"/>.
        /// </summary>
        public OfflineRewardResult EvaluateOffline()
        {
            return offline != null ? offline.Evaluate() : OfflineRewardResult.None;
        }

        /// <summary>
        /// The every-morning gift (B7 S2). Called on launch and on resume from background - an idle game can sit
        /// in the background over midnight, so Start alone would miss days. Persist-first: the calendar already
        /// wrote today's date into the service, so this SaveNow locks the day before anything else could re-claim.
        /// </summary>
        public void TryClaimDailyStreak()
        {
            if (DailyStreak == null || Rewards == null)
            {
                return;
            }

            Economy.DailyStreakService.ClaimResult result = DailyStreak.TryClaim();

            if (!result.Claimed)
            {
                return;
            }

            Rewards.GrantGems(result.Gems, RewardService.Source.DailyStreak);

            if (result.Day7Boost && Boost != null)
            {
                Boost.Activate();
            }

            Save?.SaveNow("streak");
            GameEvents.RaiseDailyStreakClaimed(result.Day, result.Gems);

            GameEvents.RaiseToast(result.Day7Boost
                ? $"Day {result.Day} gift: +{result.Gems:0} gems + a gold boost!"
                : $"Day {result.Day} gift: +{result.Gems:0} gems (tomorrow +{result.NextGems:0})");

            LogFlow($"Daily streak: day {result.Day}, +{result.Gems:0} gems" +
                    (result.Day7Boost ? " + gold boost" : string.Empty));
        }

        /// <summary>
        /// A time payout was paid (the offline claim, or a bought fast-forward). The manager owns the lifetime
        /// total, so <see cref="OfflineCoordinator"/> reports the amount back here.
        /// </summary>
        private void OnIdleGoldEarned(double gold)
        {
            TotalGoldEarned += gold;
        }

        /// <summary>
        /// Gem sink #2: buys <c>instantIncomeSeconds</c> of income outright ("fast-forward").
        /// Quote first, then charge, then pay - so gems are never spent on a payout of zero.
        /// The errand lives in <see cref="OfflineCoordinator"/>.
        /// </summary>
        public bool BuyInstantIncome()
        {
            return offline != null && offline.BuyInstantIncome();
        }

        /// <summary>Remembers which management tab the player was on.</summary>
        public void SetLastPageIndex(int index)
        {
            LastPageIndex = Mathf.Max(0, index);
            Save?.MarkDirty("page");
        }

        /// <summary>True when the player owns the "no ads" purchase (B7 S1): the watch-ad buttons must disappear.</summary>
    }
}
