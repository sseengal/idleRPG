using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Save;

namespace IdleRPG.Economy
{
    /// <summary>
    /// Gem purchases - the sink that stops gems being a dead currency.
    ///
    /// ELI5: gems come out of bosses but had nothing to buy, so they piled up like arcade tokens with no
    /// machine. This is the machine: it trades gems for permanent quality-of-life. Step 9b ships one offer
    /// (extra offline income cap); Step 19 adds the rest (expedition slot, automation slot, fast-forward).
    ///
    /// Why the *offline income cap* and not the 8h wall clock: the equivalent cap (2h of battle income) is what
    /// actually limits a payout, so raising the 8h limit would change nothing. Buyers must feel the difference.
    /// </summary>
    public sealed class ShopService
    {
        private readonly BalanceConfig balanceConfig;
        private readonly EconomyManager economy;

        public ShopService(BalanceConfig balanceConfig, EconomyManager economy)
        {
            this.balanceConfig = balanceConfig;
            this.economy = economy;

            if (this.balanceConfig == null || this.economy == null)
            {
                Debug.LogError("[ShopService] Needs a BalanceConfig and an EconomyManager.");
            }
        }

        /// <summary>Extra offline cap bought so far, in seconds.</summary>
        public double OfflineCapBonusSeconds { get; private set; }

        public int Purchases { get; private set; }

        /// <summary>Max extra cap the shop allows (e.g. 3 purchases of +1h).</summary>
        public double MaxOfflineCapBonusSeconds => balanceConfig != null ? balanceConfig.OfflineCapExtensionMaxSeconds : 0d;

        /// <summary>Cost of the next extension (gems), or 0 when maxed out.</summary>
        public double NextOfflineCapExtensionCost
        {
            get
            {
                if (balanceConfig == null || IsOfflineCapMaxed)
                {
                    return 0d;
                }

                return balanceConfig.OfflineCapExtensionGemCost;
            }
        }

        public bool IsOfflineCapMaxed => OfflineCapBonusSeconds >= MaxOfflineCapBonusSeconds - 0.5d;

        public bool CanAffordOfflineCapExtension => !IsOfflineCapMaxed && economy != null && economy.Gems >= NextOfflineCapExtensionCost;

        /// <summary>
        /// Buys the next +1h of offline income cap. Returns false (and does nothing) when maxed or too poor.
        /// </summary>
        public bool TryBuyOfflineCapExtension()
        {
            if (balanceConfig == null || economy == null || IsOfflineCapMaxed)
            {
                return false;
            }

            double cost = NextOfflineCapExtensionCost;

            if (!economy.SpendGems(cost))
            {
                return false;
            }

            OfflineCapBonusSeconds = System.Math.Min(
                OfflineCapBonusSeconds + balanceConfig.OfflineCapExtensionSeconds,
                MaxOfflineCapBonusSeconds);

            Purchases++;

            GameEvents.RaiseToast($"Offline income cap +{balanceConfig.OfflineCapExtensionSeconds / 60d:0} min");
            return true;
        }

        // ------------------------------------------------------------------
        // Offer 2: instant income (fast-forward)
        // ------------------------------------------------------------------

        /// <summary>Seconds of income one fast-forward buys.</summary>
        public double InstantIncomeSeconds => balanceConfig != null ? balanceConfig.InstantIncomeSeconds : 0d;

        /// <summary>Gems charged per fast-forward purchase.</summary>
        public double InstantIncomeGemCost => balanceConfig != null ? balanceConfig.InstantIncomeGemCost : 0d;

        /// <summary>Repeatable offer: no cap, but it costs gems every time.</summary>
        public bool CanAffordInstantIncome => economy != null && InstantIncomeGemCost > 0d && economy.Gems >= InstantIncomeGemCost;

        /// <summary>
        /// Charges the gems for a fast-forward. Called by <see cref="IdleTimeService"/> only after it knows the
        /// payout is non-zero, so gems are never spent on an empty purchase.
        /// </summary>
        public bool TrySpendInstantIncomeGems()
        {
            return economy != null && InstantIncomeGemCost > 0d && economy.SpendGems(InstantIncomeGemCost);
        }

        public string DescribeInstantIncomeOffer()
        {
            double minutes = InstantIncomeSeconds / 60d;
            return $"Fast-forward {minutes:0} min - {InstantIncomeGemCost:0} gems";
        }

        /// <summary>Restores purchases from a save.</summary>
        public void Restore(double bonusSeconds, int purchases)
        {
            OfflineCapBonusSeconds = bonusSeconds < 0d ? 0d : System.Math.Min(bonusSeconds, MaxOfflineCapBonusSeconds);
            Purchases = purchases < 0 ? 0 : purchases;
        }

        public void WriteToSave(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            data.offlineEquivalentCapBonusSeconds = OfflineCapBonusSeconds;
            data.offlineCapExtensionsPurchased = Purchases;
        }

        public string DescribeOfflineCapOffer()
        {
            if (IsOfflineCapMaxed)
            {
                return "Offline cap is maxed";
            }

            double minutes = balanceConfig != null ? balanceConfig.OfflineCapExtensionSeconds / 60d : 60d;
            return $"+{minutes:0} min offline income - {NextOfflineCapExtensionCost:0} gems";
        }
    }
}
