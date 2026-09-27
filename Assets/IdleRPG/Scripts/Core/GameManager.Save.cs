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
        /// </summary>
        public OfflineRewardResult EvaluateOffline()
        {
            if (Idle == null)
            {
                return OfflineRewardResult.None;
            }

            double lastLogout = ResolveLastLogoutBinary();

            if (lastLogout <= 0d)
            {
                return OfflineRewardResult.None;
            }

            double savedRate = Ledger != null ? Ledger.GoldPerSecond : 0d;
            OfflineRewardResult result = Idle.Evaluate(lastLogout, CurrentStage, savedRate);

            if (result.HasReward)
            {
                LogFlow($"Offline: {result.RawSeconds:0}s away -> {result.CappedSeconds:0}s paid, " +
                        $"{result.Gold:0.#} gold at {result.GoldPerSecond:0.##}/s ({Idle.LastRateSource})");

                // Consume the window straight away so a kill before claiming cannot pay twice.
                Save?.SaveNow("offline");
                GameEvents.RaiseOfflineRewardsReady(result);
            }

            return result;
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
        /// Most recent of the two logout timestamps (save file and PlayerPrefs). Taking the newer one
        /// means a hard process kill can never inflate the offline window.
        /// </summary>
        private double ResolveLastLogoutBinary()
        {
            double fromSave = Save != null ? Save.LastLogoutBinary : 0d;
            double fromPrefs = 0d;
            bool hasPrefs = SaveManager.TryReadPlayerPrefsLogout(out fromPrefs);

            if (hasPrefs && fromPrefs > fromSave)
            {
                return fromPrefs;
            }

            return fromSave;
        }

        private void OnIdleRewardPaid(double gold)
        {
            // Both time payouts land here: the offline claim and a bought fast-forward.
            TotalGoldEarned += gold;
            Save?.MarkDirty("idle-income");
        }

        /// <summary>
        /// Gem sink #2: buys <c>instantIncomeSeconds</c> of income outright ("fast-forward").
        /// Quote first, then charge, then pay - so gems are never spent on a payout of zero.
        /// </summary>
        public bool BuyInstantIncome()
        {
            if (Shop == null || Idle == null)
            {
                return false;
            }

            double savedRate = Ledger != null ? Ledger.GoldPerSecond : 0d;
            double quote = Idle.QuoteInstantIncome(CurrentStage, savedRate);

            if (quote <= 0d)
            {
                GameEvents.RaiseToast("No income to fast-forward yet");
                return false;
            }

            if (!Shop.CanAffordInstantIncome)
            {
                GameEvents.RaiseToast($"Needs {Shop.InstantIncomeGemCost:0} gems");
                return false;
            }

            double gold = Idle.TryGrantInstantIncome(CurrentStage, savedRate, Shop.TrySpendInstantIncomeGems);

            if (gold <= 0d)
            {
                return false;
            }

            GameEvents.RaiseToast($"+{NumberFormatter.Format(gold)} gold");
            LogFlow($"Instant income: {Shop.InstantIncomeSeconds / 60d:0} min for {Shop.InstantIncomeGemCost:0} gems" +
                    $" -> {gold:0.#} gold (purchase #{Idle.InstantIncomePurchases})");
            return true;
        }

        /// <summary>Remembers which management tab the player was on.</summary>
        public void SetLastPageIndex(int index)
        {
            LastPageIndex = Mathf.Max(0, index);
            Save?.MarkDirty("page");
        }

        /// <summary>Builds the payload written to disk. Called by <see cref="SaveManager"/>.</summary>
        private SaveData CaptureSnapshot()
        {
            SaveData data = SaveData.CreateDefault();

            data.currentStage = CurrentStage;
            data.currentWave = combatManager != null ? combatManager.CurrentWave : 1;
            data.highestStageReached = HighestStageReached;
            data.runBestStage = RunBestStage;

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

        /// <summary>True when the player owns the "no ads" purchase (B7 S1): the watch-ad buttons must disappear.</summary>
        public bool AdsDisabledByNoAds => Iap != null && Iap.IsOwned(IapCatalog.SkuNoAds);

        /// <summary>
        /// The ONE gate for every rewarded ad (B7 S3): no-ads -> caps + cooldown -> ready check -> show. On a
        /// completed ad the redemption is recorded and saved before <paramref name="onReward"/> runs, so a whirlwind
        /// tap-spam or crash can never over-pay a placement.
        /// </summary>
        public bool TryShowAdPlacement(AdPlacementId placement, Action onReward)
        {
            if (Ads == null || AdCaps == null)
            {
                return false;
            }

            if (AdsDisabledByNoAds)
            {
                GameEvents.RaiseToast("Ads removed - enjoy the quiet!");
                return false;
            }

            if (!AdCaps.CanShow(placement, out double waitSeconds))
            {
                if (waitSeconds > 0d)
                {
                    GameEvents.RaiseToast(string.Format("Ad recently used - try again in about {0:0} min.", waitSeconds / 60d));
                }
                else
                {
                    GameEvents.RaiseToast("Daily ad limit reached - come back tomorrow.");
                }

                return false;
            }

            if (!Ads.IsRewardedAdReady)
            {
                GameEvents.RaiseToast("Ad not ready yet.");
                return false;
            }

            Ads.ShowRewardedAd(success =>
            {
                if (!success)
                {
                    GameEvents.RaiseToast("Ad skipped - no reward.");
                    return;
                }

                AdCaps.MarkShown(placement);
                Save?.SaveNow("ad");
                onReward?.Invoke();
            });

            return true;
        }

        /// <summary>Shows the GoldBoost ad, then activates the 2x gold boost (Shop CTA + the B hotkey).</summary>
        public bool WatchAdForGoldBoost()
        {
            return TryShowAdPlacement(AdPlacementId.GoldBoost, () =>
            {
                if (Boost != null)
                {
                    Boost.Activate();
                    LogFlow($"Ad boost active: x{Boost.GoldMultiplier:0.#} gold for {Boost.RemainingSeconds / 60f:0.#} min.");
                }
            });
        }

        /// <summary>
        /// The single IAP funnel (B7 S1). The store confirms payment (<see cref="IIapService"/>), then this
        /// grants the product's contents through the normal till (gems) and shop (offline cap) — never directly.
        /// </summary>
        public void PurchaseIap(string sku)
        {
            if (Iap == null || !Iap.IsInitialized)
            {
                GameEvents.RaiseToast("Store is not ready.");
                return;
            }

            IapProduct product = IapCatalog.Find(sku);

            if (product == null)
            {
                GameEvents.RaiseToast("Unknown product.");
                return;
            }

            if (Iap.IsOwned(sku))
            {
                GameEvents.RaiseToast("Already owned.");
                return;
            }

            Iap.Purchase(sku, success =>
            {
                if (!success)
                {
                    GameEvents.RaiseToast("Purchase failed - nothing charged.");
                    return;
                }

                if (product.IsNoAds)
                {
                    LogFlow($"IAP bought: {product.DisplayName} (ads removed).");
                    GameEvents.RaiseToast("Ads removed - thank you!");
                    return;
                }

                if (product.GemsGranted > 0)
                {
                    Rewards?.GrantGems(product.GemsGranted, RewardService.Source.Milestone);
                }

                if (product.OfflineCapBonusMinutes > 0d)
                {
                    Shop?.TryGrantOfflineCapBonus(product.OfflineCapBonusMinutes);
                }

                Save?.MarkDirty("iap");
                LogFlow($"IAP bought: {product.DisplayName} (+{product.GemsGranted:0} gems, +{product.OfflineCapBonusMinutes:0} min offline cap).");
                GameEvents.RaiseToast($"{product.DisplayName} purchased!");
            });
        }
    }
}
