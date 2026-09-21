using System;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Sim;
using IdleRPG.Progression;

namespace IdleRPG.Save
{
    /// <summary>
    /// The one owner of **time-based payouts** - every "the player traded time for gold" grant lives here.
    /// Two exist today:
    ///   * the **offline window**: time the player was away (wall-capped, equivalent-capped, discount, claim-gated),
    ///   * **instant income**: time bought outright with gems (gem sink #2).
    ///
    /// ELI5: the game can pay you for time you did not play. Two flavours: "you were away" (capped and
    /// discounted, because sleeping should not beat playing) and "you bought an hour" (paid in full, because you
    /// spent gems). Both need the same three answers - what is gold/sec, how much discount, and how does this
    /// reach the wallet without fooling the speedometer - so they live in one class instead of two copies.
    /// Expeditions and bounties (Step 17) will plug in here as a third flavour rather than re-deriving the maths.
    ///
    /// Two caps apply to the offline window and both stay in force:
    ///   * <c>offlineCapSeconds</c>           - the 8h spec limit on wall-clock time away,
    ///   * <c>offlineMaxEquivalentSeconds</c> - pays at most N seconds of battle income (+ shop purchases),
    ///     because the equivalent cap is what actually limits a payout.
    ///
    /// Gold = paidSeconds * goldPerSecond * efficiency, then the player's gold multiplier. Every payout goes
    /// through the reward funnel flagged **external**, so time income never inflates the measured rate that the
    /// next payout is built from.
    /// </summary>
    public sealed class IdleTimeService
    {
        private readonly BalanceConfig balanceConfig;
        private readonly WaveConfig waveConfig;
        private readonly RewardService rewards;
        private readonly SimLedger ledger;
        private readonly Func<double, double> goldResolver;

        /// <summary>Extra offline equivalent cap bought in the shop (seconds); set by GameManager after a load.</summary>
        public double BonusEquivalentCapSeconds { get; set; }

        public IdleTimeService(
            BalanceConfig balanceConfig,
            WaveConfig waveConfig,
            RewardService rewards,
            SimLedger ledger,
            Func<double, double> goldResolver)
        {
            this.balanceConfig = balanceConfig;
            this.waveConfig = waveConfig;
            this.rewards = rewards;
            this.ledger = ledger;
            this.goldResolver = goldResolver;

            if (this.balanceConfig == null || this.rewards == null)
            {
                Debug.LogError("[IdleTimeService] Needs a BalanceConfig and a RewardService.");
            }
        }

        /// <summary>Raised after a time-based payout is paid, with the gold actually granted.</summary>
        public event Action<double> RewardPaid;

        /// <summary>Unclaimed offline reward, if any. Doubles as the pending-claim guard.</summary>
        public OfflineRewardResult Pending { get; private set; }

        public bool HasPending => Pending.HasReward;

        /// <summary>Rate the last evaluation used (gold per second, before efficiency).</summary>
        public double LastRate { get; private set; }

        /// <summary>"measured", "saved" or "estimated" - which rate source won.</summary>
        public string LastRateSource { get; private set; } = "none";

        /// <summary>Instant-income payouts this session (telemetry only; never part of the save).</summary>
        public int InstantIncomePurchases { get; private set; }

        public void Attach()
        {
            GameEvents.OfflineRewardsClaimed += OnOfflineRewardsClaimed;
        }

        public void Detach()
        {
            GameEvents.OfflineRewardsClaimed -= OnOfflineRewardsClaimed;
        }

        /// <summary>Drops any pending reward without paying it (used when a save is wiped).</summary>
        public void ClearPending()
        {
            Pending = OfflineRewardResult.None;
        }

        // ------------------------------------------------------------------
        // Instant income (gem sink #2)
        // ------------------------------------------------------------------

        /// <summary>
        /// What one fast-forward would pay right now, in resolved gold (0 = not worth offering).
        /// No side effects beyond the rate probe, so the shop can label a button with it.
        /// </summary>
        public double QuoteInstantIncome(int stage, double savedGoldPerSecond)
        {
            double seconds = balanceConfig != null ? balanceConfig.InstantIncomeSeconds : 0d;
            double rate = ResolveRate(stage, savedGoldPerSecond);

            return seconds > 0d ? TimeGold(seconds, rate) : 0d;
        }

        /// <summary>
        /// Buys <c>instantIncomeSeconds</c> of income outright. The gems are charged only after the payout is
        /// known to be non-zero, so a purchase can never cost gems and pay nothing.
        /// </summary>
        /// <param name="chargeGems">Returns false when the player cannot afford it (nothing is paid then).</param>
        /// <returns>Gold granted, or 0 when the purchase was refused.</returns>
        public double TryGrantInstantIncome(int stage, double savedGoldPerSecond, Func<bool> chargeGems)
        {
            double quote = QuoteInstantIncome(stage, savedGoldPerSecond);

            if (quote <= 0d)
            {
                return 0d;
            }

            if (chargeGems != null && !chargeGems())
            {
                return 0d;
            }

            double paid = PayExternal(quote, RewardService.Source.InstantIncome);

            if (paid > 0d)
            {
                InstantIncomePurchases++;
                Debug.Log($"[IdleTimeService] Instant income paid {paid:0.#} gold " +
                          $"({balanceConfig.InstantIncomeSeconds:0}s at {LastRate:0.##}/s, {LastRateSource}).");
            }

            return paid;
        }

        // ------------------------------------------------------------------
        // Offline window
        // ------------------------------------------------------------------

        private double ResolveRate(int stage, double savedGoldPerSecond)
        {
            if (ledger != null && ledger.HasEnoughSamples && ledger.GoldPerSecond > 0d)
            {
                LastRate = ledger.GoldPerSecond;
                LastRateSource = "measured";
            }
            else if (savedGoldPerSecond > 0d)
            {
                LastRate = savedGoldPerSecond;
                LastRateSource = "saved";
            }
            else
            {
                LastRate = EstimateStageGoldPerSecond(stage);
                LastRateSource = "estimated";
            }

            return LastRate;
        }

        /// <summary>Formula estimate of gold/sec at this manager's current stage.</summary>
        public double EstimateStageGoldPerSecond(int stage)
        {
            return EstimateGoldPerSecond(balanceConfig, waveConfig, stage);
        }

        /// <summary>
        /// Formula estimate of gold/sec at a stage, used before any live measurement exists.
        /// The stage's first normal enemy is the reference (boss waves are ignored).
        /// Static so the editor tools can print a payout table without a live game.
        /// </summary>
        public static double EstimateGoldPerSecond(BalanceConfig balanceConfig, WaveConfig waveConfig, int stage)
        {
            if (balanceConfig == null || waveConfig == null)
            {
                return 0d;
            }

            EnemyData enemy = waveConfig.GetEnemyFor(Mathf.Max(1, stage), 1, balanceConfig.NormalWavesPerStage);

            if (enemy == null)
            {
                return 0d;
            }

            return FormulaUtility.EstimatedGoldPerSecond(
                enemy.BaseGoldDrop,
                stage,
                balanceConfig.EnemyGoldGrowth,
                balanceConfig.OfflineEstimatedSecondsPerKill,
                balanceConfig.MeanEnemiesPerWave);
        }

        /// <summary>
        /// Builds the reward for a logout timestamp. Returns <see cref="OfflineRewardResult.None"/> when
        /// there is nothing worth showing (too short, clock went backwards, or no income).
        /// </summary>
        /// <param name="lastLogoutBinary">Timestamp of the previous session end.</param>
        /// <param name="stage">Stage to estimate income for when no measured rate exists.</param>
        /// <param name="savedGoldPerSecond">Rate stored in the save file (may be 0).</param>
        public OfflineRewardResult Evaluate(double lastLogoutBinary, int stage, double savedGoldPerSecond)
        {
            Pending = OfflineRewardResult.None;

            // SecondsBetween clamps at 0, so a rewound clock (tamper) pays nothing.
            double awaySeconds = GameClock.SecondsBetween(lastLogoutBinary, GameClock.NowBinary);

            if (awaySeconds <= 0d)
            {
                Debug.LogWarning("[IdleTimeService] Non-positive offline delta; paying nothing.");
                return OfflineRewardResult.None;
            }

            double cap = balanceConfig != null ? balanceConfig.OfflineCapSeconds : awaySeconds;
            double wallSeconds = Math.Min(awaySeconds, cap);

            // Shop purchases raise the equivalent cap: that is the cap that actually limits a payout.
            double equivalentCap = (balanceConfig != null ? balanceConfig.OfflineMaxEquivalentSeconds : wallSeconds)
                                   + (BonusEquivalentCapSeconds < 0d ? 0d : BonusEquivalentCapSeconds);
            double paidSeconds = Math.Min(wallSeconds, equivalentCap);

            double rate = ResolveRate(stage, savedGoldPerSecond);
            double minSeconds = balanceConfig != null ? balanceConfig.MinOfflineSecondsForPopup : 30f;

            if (paidSeconds < minSeconds || rate <= 0d)
            {
                return OfflineRewardResult.None;
            }

            // Efficiency is applied once; the cap argument is the already-capped duration.
            double gold = TimeGold(paidSeconds, rate);

            Pending = new OfflineRewardResult(awaySeconds, paidSeconds, rate, gold, awaySeconds > paidSeconds);
            return Pending;
        }

        private void OnOfflineRewardsClaimed(double gold)
        {
            if (!Pending.HasReward)
            {
                // Already claimed (or nothing was offered): ignore a second click.
                Debug.LogWarning("[IdleTimeService] Ignored a claim with no pending reward.");
                return;
            }

            OfflineRewardResult claimed = Pending;
            Pending = OfflineRewardResult.None;

            double payable = claimed.Gold > 0d ? claimed.Gold : gold;

            // Through the till, flagged external: paid in full, excluded from the measured rate.
            PayExternal(payable, RewardService.Source.OfflineClaim);

            Debug.Log($"[IdleTimeService] Paid {payable:0.#} gold for {claimed.CappedSeconds:0}s away " +
                      $"(rate {claimed.GoldPerSecond:0.##}/s, source {LastRateSource}).");
        }

        // ------------------------------------------------------------------
        // Shared maths for every time payout
        // ------------------------------------------------------------------

        /// <summary>
        /// Time -> gold, the single formula both flavours use: seconds * rate * efficiency, then the player's
        /// gold multiplier (applied here so the number quoted in UI is the number paid).
        /// </summary>
        private double TimeGold(double seconds, double rate)
        {
            if (seconds <= 0d || rate <= 0d)
            {
                return 0d;
            }

            double efficiency = balanceConfig != null ? balanceConfig.OfflineEfficiency : 1d;
            double baseGold = FormulaUtility.TimeBasedGold(seconds, rate, efficiency, seconds);

            // Prestige/boost multipliers are applied here so the popup shows exactly what is paid.
            double multiplier = goldResolver != null ? Math.Max(0d, goldResolver(1d)) : 1d;
            return baseGold * multiplier;
        }

        /// <summary>Pays a quoted amount as external income and reports it to listeners.</summary>
        private double PayExternal(double gold, RewardService.Source source)
        {
            if (gold <= 0d)
            {
                return 0d;
            }

            // Quoted: the UI already showed the resolved amount, so pay it verbatim.
            double paid = rewards != null ? rewards.GrantQuotedGold(gold, source) : gold;
            RewardPaid?.Invoke(paid);
            return paid;
        }
    }
}
