using System;
using UnityEngine;
using IdleRPG.Economy;
using IdleRPG.Save;
using IdleRPG.Sim;
using IdleRPG.Utils;

namespace IdleRPG.Core
{
    /// <summary>
    /// The two time payouts a player meets at the door: the offline window ("you were away") and instant income
    /// ("you bought an hour").
    ///
    /// ELI5: the arithmetic for both lives in <see cref="IdleTimeService"/>. This class does the errand around it -
    /// work out how long the player was away, ask for a quote, write the save so the same window cannot pay twice,
    /// and add the gold to the lifetime total.
    ///
    /// Why it is not left in <c>GameManager</c>: the errand needs five collaborators (the idle service, the
    /// speedometer, the save, the shop, and the stage the run is on) and knows nothing about combat or the UI.
    /// Pulling it out keeps the composition root about wiring, and lets the whole offline path be driven from a
    /// tool with a faked logout stamp (<see cref="SaveManager.DebugSetLastLogoutBinary"/>).
    /// </summary>
    public sealed class OfflineCoordinator
    {
        private readonly IdleTimeService idle;
        private readonly SimLedger ledger;
        private readonly SaveManager save;
        private readonly ShopService shop;
        private readonly Func<int> currentStage;
        private readonly Action<double> onGoldEarned;
        private readonly Action<string> log;

        /// <summary>
        /// The coordinator subscribes to <see cref="IdleTimeService.RewardPaid"/> itself, so both time payouts
        /// reach the lifetime total. <paramref name="onGoldEarned"/> is how that total is updated - it belongs to
        /// the manager, not here.
        /// </summary>
        public OfflineCoordinator(IdleTimeService idle, SimLedger ledger, SaveManager save, ShopService shop,
            Func<int> currentStage, Action<double> onGoldEarned, Action<string> log)
        {
            this.idle = idle;
            this.ledger = ledger;
            this.save = save;
            this.shop = shop;
            this.currentStage = currentStage;
            this.onGoldEarned = onGoldEarned;
            this.log = log;

            if (this.idle == null)
            {
                Debug.LogError("[OfflineCoordinator] Needs the IdleTimeService; offline payouts are off.");
                return;
            }

            this.idle.RewardPaid += OnRewardPaid;
        }

        /// <summary>Unhooks from the idle service (the manager is going away).</summary>
        public void Detach()
        {
            if (idle != null)
            {
                idle.RewardPaid -= OnRewardPaid;
            }
        }

        /// <summary>The stage the run is on, read fresh each call - the run advances while the game plays.</summary>
        private int Stage => currentStage != null ? currentStage() : 1;

        /// <summary>
        /// Offers the offline reward for the time since the last session ended.
        /// Called once on start; also used by the debug tooling to re-run the calculation.
        /// </summary>
        public OfflineRewardResult Evaluate()
        {
            if (idle == null)
            {
                return OfflineRewardResult.None;
            }

            double lastLogout = ResolveLastLogout();

            if (lastLogout <= 0d)
            {
                return OfflineRewardResult.None;
            }

            double savedRate = ledger != null ? ledger.GoldPerSecond : 0d;
            OfflineRewardResult result = idle.Evaluate(lastLogout, Stage, savedRate);

            if (result.HasReward)
            {
                log?.Invoke($"Offline: {result.RawSeconds:0}s away -> {result.CappedSeconds:0}s paid, " +
                            $"{result.Gold:0.#} gold at {result.GoldPerSecond:0.##}/s ({idle.LastRateSource})");

                // Consume the window straight away so a kill before claiming cannot pay twice.
                save?.SaveNow("offline");
                GameEvents.RaiseOfflineRewardsReady(result);
            }

            return result;
        }
        /// <summary>
        /// Gem sink #2: buys <c>instantIncomeSeconds</c> of income outright ("fast-forward").
        /// Quote first, then charge, then pay - so gems are never spent on a payout of zero.
        /// </summary>
        public bool BuyInstantIncome()
        {
            if (shop == null || idle == null)
            {
                return false;
            }

            double savedRate = ledger != null ? ledger.GoldPerSecond : 0d;
            double quote = idle.QuoteInstantIncome(Stage, savedRate);

            if (quote <= 0d)
            {
                GameEvents.RaiseToast("No income to fast-forward yet");
                return false;
            }

            if (!shop.CanAffordInstantIncome)
            {
                GameEvents.RaiseToast($"Needs {shop.InstantIncomeGemCost:0} gems");
                return false;
            }

            double gold = idle.TryGrantInstantIncome(Stage, savedRate, shop.TrySpendInstantIncomeGems);

            if (gold <= 0d)
            {
                return false;
            }

            GameEvents.RaiseToast($"+{NumberFormatter.Format(gold)} gold");
            log?.Invoke($"Instant income: {shop.InstantIncomeSeconds / 60d:0} min for {shop.InstantIncomeGemCost:0} gems" +
                        $" -> {gold:0.#} gold (purchase #{idle.InstantIncomePurchases})");
            return true;
        }

        /// <summary>
        /// Most recent of the two logout timestamps (save file and PlayerPrefs). Taking the newer one
        /// means a hard process kill can never inflate the offline window.
        /// </summary>
        private double ResolveLastLogout()
        {
            double fromSave = save != null ? save.LastLogoutBinary : 0d;
            double fromPrefs = 0d;
            bool hasPrefs = SaveManager.TryReadPlayerPrefsLogout(out fromPrefs);

            if (hasPrefs && fromPrefs > fromSave)
            {
                return fromPrefs;
            }

            return fromSave;
        }

        private void OnRewardPaid(double gold)
        {
            // Both time payouts land here: the offline claim and a bought fast-forward.
            onGoldEarned?.Invoke(gold);
            save?.MarkDirty("idle-income");
        }
    }
}
