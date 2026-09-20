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
    /// Turns "the player was away for N seconds" into a claimable gold reward.
    ///
    /// Two caps apply and both stay in force:
    ///   * <c>offlineCapSeconds</c>           - the 8h spec limit on wall-clock time away,
    ///   * <c>offlineMaxEquivalentSeconds</c> - pays at most N seconds of battle income.
    ///
    /// Gold = paidSeconds * goldPerSecond * offlineEfficiency, where goldPerSecond is the measured
    /// live rate (save-seeded when the session is young) or a formula estimate as a last resort.
    /// </summary>
    public sealed class OfflineProgressManager
    {
        private readonly BalanceConfig balanceConfig;
        private readonly WaveConfig waveConfig;
        private readonly RewardService rewards;
        private readonly SimLedger ledger;
        private readonly Func<double, double> goldResolver;

        /// <summary>Extra offline cap bought in the shop (seconds); set by GameManager after a load.</summary>
        public double BonusEquivalentCapSeconds { get; set; }

        /// <summary>
        /// Step 9a: pays through the reward funnel (so the claim is booked as external and never pollutes the
        /// earning rate) and reads its rate from the ledger.
        /// </summary>
        public OfflineProgressManager(
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
                Debug.LogError("[OfflineProgressManager] Needs a BalanceConfig and a RewardService.");
            }
        }

        /// <summary>Raised after a claim is paid, with the gold actually granted.</summary>
        public event Action<double> RewardPaid;

        /// <summary>Unclaimed reward, if any. Doubles as the pending-claim guard.</summary>
        public OfflineRewardResult Pending { get; private set; }

        public bool HasPending => Pending.HasReward;

        /// <summary>Rate the last evaluation used (gold per second, before efficiency).</summary>
        public double LastRate { get; private set; }

        /// <summary>"measured", "saved" or "estimated" - which rate source won.</summary>
        public string LastRateSource { get; private set; } = "none";

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
                balanceConfig.EnemiesPerWave);
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
                Debug.LogWarning("[OfflineProgressManager] Non-positive offline delta; paying nothing.");
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
            double baseGold = FormulaUtility.OfflineGold(paidSeconds, rate, balanceConfig.OfflineEfficiency, paidSeconds);

            // Prestige/boost multipliers are applied here so the popup shows exactly what is paid.
            double multiplier = goldResolver != null ? Math.Max(0d, goldResolver(1d)) : 1d;
            double gold = baseGold * multiplier;

            Pending = new OfflineRewardResult(awaySeconds, paidSeconds, rate, gold, awaySeconds > paidSeconds);
            return Pending;
        }

        private void OnOfflineRewardsClaimed(double gold)
        {
            if (!Pending.HasReward)
            {
                // Already claimed (or nothing was offered): ignore a second click.
                Debug.LogWarning("[OfflineProgressManager] Ignored a claim with no pending reward.");
                return;
            }

            OfflineRewardResult claimed = Pending;
            Pending = OfflineRewardResult.None;

            double payable = claimed.Gold > 0d ? claimed.Gold : gold;

            // Through the till, flagged external: paid in full, excluded from the measured rate.
            if (rewards != null)
            {
                // Quoted amount: the popup already showed the resolved gold, so pay it verbatim.
                rewards.GrantQuotedGold(payable, RewardService.Source.OfflineClaim);
            }

            RewardPaid?.Invoke(payable);
            Debug.Log($"[OfflineProgressManager] Paid {payable:0.#} gold for {claimed.CappedSeconds:0}s away " +
                      $"(rate {claimed.GoldPerSecond:0.##}/s, source {LastRateSource}).");
        }
    }
}