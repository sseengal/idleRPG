using System;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Services;
using IdleRPG.Sim;

namespace IdleRPG.Economy
{
    /// <summary>
    /// The one cashier. Every payout in the game goes through here: combat kills, boss gems, offline claims,
    /// ad boosts, debug grants, milestone chests (later), expeditions (later).
    ///
    /// ELI5: before, gold arrived from several places and each place had to remember to apply the prestige bonus,
    /// the ad boost and the speedometer. Now there is a single till with **two buttons**:
    ///   * <see cref="GrantGold"/> - "work out the amount and pay it" (normal income: kills, milestones),
    ///   * <see cref="GrantQuotedGold"/> - "pay exactly what the receipt says" (anything the player was already
    ///     shown a number for: offline claim, ad reward, bounty).
    /// That split matters: without it the multipliers get applied twice (a bug this step briefly shipped and
    /// then made impossible).
    ///
    /// Every payout also writes a receipt into the ledger, so "playing" and "away" income can never drift apart.
    /// </summary>
    public sealed class RewardService
    {
        /// <summary>Where a payout came from (ledger filters, telemetry, debugging).</summary>
        public enum Source
        {
            Combat = 0,
            OfflineClaim = 1,
            AdBoost = 2,
            Milestone = 3,
            Expedition = 4,
            Debug = 5,
            InstantIncome = 6
        }

        private readonly EconomyManager economy;
        private readonly SimLedger ledger;
        private readonly Func<double, double> goldMultiplier;

        public RewardService(EconomyManager economy, SimLedger ledger, Func<double, double> goldMultiplier)
        {
            this.economy = economy;
            this.ledger = ledger;
            this.goldMultiplier = goldMultiplier;

            if (this.economy == null)
            {
                Debug.LogError("[RewardService] Needs an EconomyManager.");
            }
        }

        /// <summary>Total gold granted this session through the till (all sources).</summary>
        public double GrantedGold { get; private set; }

        public int GrantedGoldCount { get; private set; }

        /// <summary>Gold computed and paid: multipliers first, then the wallet, then the ledger.</summary>
        public double GrantGold(double baseGold, Source source = Source.Combat)
        {
            if (baseGold <= 0d)
            {
                return 0d;
            }

            double resolved = goldMultiplier != null ? goldMultiplier(baseGold) : baseGold;
            return PayResolved(resolved, source);
        }

        /// <summary>Pays an amount already quoted to the player (no multipliers applied again).</summary>
        public double GrantQuotedGold(double quotedGold, Source source)
        {
            return PayResolved(quotedGold, source);
        }

        /// <summary>Pays gems (boss kills, milestones, rewarded ads).</summary>
        public double GrantGems(double amount, Source source = Source.Combat)
        {
            if (amount <= 0d || economy == null)
            {
                return 0d;
            }

            economy.AddGems(amount);
            return amount;
        }

        /// <summary>Pays prestige tokens (ascension). Never part of the combat rate.</summary>
        public double GrantTokens(double amount, Source source = Source.Milestone)
        {
            if (amount <= 0d || economy == null)
            {
                return 0d;
            }

            economy.AddTokens(amount);
            return amount;
        }

        /// <summary>Records a kill so the ledger can report kills/second.</summary>
        public void RecordKill()
        {
            ledger?.RecordKill();
        }

        /// <summary>Records a finished stage duration for the "seconds per stage" metric.</summary>
        public void RecordStage(double seconds)
        {
            ledger?.RecordStage(seconds);
        }

        public override string ToString()
        {
            return $"RewardService(granted {GrantedGold:0} gold over {GrantedGoldCount} payouts)";
        }

        private double PayResolved(double amount, Source source)
        {
            if (amount <= 0d || economy == null)
            {
                return 0d;
            }

            economy.AddGold(amount);

            // Combat income feeds the measured rate; every other source is external (excluded from it).
            ledger?.RecordGold(amount, source != Source.Combat);

            GrantedGold += amount;
            GrantedGoldCount++;

            return amount;
        }
    }
}
