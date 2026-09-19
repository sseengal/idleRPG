using System;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Utils;

namespace IdleRPG.Economy
{
    /// <summary>
    /// Owns every currency balance (Gold / Gems / Prestige Tokens) and publishes changes.
    /// Plain C# class: constructed by the bootstrap, no scene lookup, no Update polling.
    /// All mutations funnel through <see cref="SetAmount"/> so clamping and events can
    /// never be bypassed.
    /// </summary>
    public sealed class EconomyManager
    {
        private readonly BalanceConfig balanceConfig;

        private double gold;
        private double gems;
        private double prestigeTokens;

        public EconomyManager(BalanceConfig balanceConfig)
        {
            this.balanceConfig = balanceConfig;

            if (this.balanceConfig == null)
            {
                Debug.LogError("[EconomyManager] BalanceConfig is null; falling back to 0 starting balances.");
            }
        }

        /// <summary>(currency, newAmount) — mirrors GameEvents.CurrencyChanged for direct wiring.</summary>
        public event Action<CurrencyType, double> CurrencyChanged;

        public double Gold => gold;

        public double Gems => gems;

        public double PrestigeTokens => prestigeTokens;

        // ------------------------------------------------------------------
        // Reads
        // ------------------------------------------------------------------
        public double GetAmount(CurrencyType currencyType)
        {
            switch (currencyType)
            {
                case CurrencyType.Gold:
                    return gold;
                case CurrencyType.Gems:
                    return gems;
                case CurrencyType.PrestigeTokens:
                    return prestigeTokens;
                default:
                    Debug.LogWarning($"[EconomyManager] Unknown currency '{currencyType}'.");
                    return 0d;
            }
        }

        /// <summary>True when the balance covers <paramref name="cost"/>.</summary>
        public bool CanAfford(CurrencyType currencyType, double cost)
        {
            if (cost <= 0d)
            {
                return true;
            }

            return GetAmount(currencyType) >= cost;
        }

        // ------------------------------------------------------------------
        // Writes
        // ------------------------------------------------------------------
        public void AddGold(double amount)
        {
            AddCurrency(CurrencyType.Gold, amount);
        }

        /// <summary>Spends gold. Returns false (and changes nothing) when unaffordable.</summary>
        public bool SpendGold(double amount)
        {
            return SpendCurrency(CurrencyType.Gold, amount);
        }

        public void AddGems(double amount)
        {
            AddCurrency(CurrencyType.Gems, amount);
        }

        public bool SpendGems(double amount)
        {
            return SpendCurrency(CurrencyType.Gems, amount);
        }

        public void AddTokens(double amount)
        {
            AddCurrency(CurrencyType.PrestigeTokens, amount);
        }

        public bool SpendTokens(double amount)
        {
            return SpendCurrency(CurrencyType.PrestigeTokens, amount);
        }

        /// <summary>Adds without an affordability check. Ignores non-positive amounts.</summary>
        public void AddCurrency(CurrencyType currencyType, double amount)
        {
            if (double.IsNaN(amount) || amount <= 0d)
            {
                return;
            }

            SetAmount(currencyType, GetAmount(currencyType) + FormulaUtility.Sanitize(amount));
        }

        /// <summary>
        /// Spends when affordable. Returns true when the balance changed, false when the
        /// player cannot afford it. Zero-cost requests succeed and change nothing.
        /// </summary>
        public bool SpendCurrency(CurrencyType currencyType, double amount)
        {
            if (double.IsNaN(amount))
            {
                return false;
            }

            if (amount <= 0d)
            {
                return true;
            }

            double current = GetAmount(currencyType);
            if (current < amount)
            {
                return false;
            }

            SetAmount(currencyType, current - amount);
            return true;
        }

        /// <summary>
        /// Directly sets a balance. Used by the save system and by prestige resets.
        /// Negative or non-finite values fall back to zero.
        /// </summary>
        public void SetAmount(CurrencyType currencyType, double newAmount)
        {
            double safeAmount = FormulaUtility.Sanitize(newAmount);

            switch (currencyType)
            {
                case CurrencyType.Gold:
                    if (gold == safeAmount)
                    {
                        return;
                    }

                    gold = safeAmount;
                    break;

                case CurrencyType.Gems:
                    if (gems == safeAmount)
                    {
                        return;
                    }

                    gems = safeAmount;
                    break;

                case CurrencyType.PrestigeTokens:
                    if (prestigeTokens == safeAmount)
                    {
                        return;
                    }

                    prestigeTokens = safeAmount;
                    break;

                default:
                    Debug.LogWarning($"[EconomyManager] Cannot set unknown currency '{currencyType}'.");
                    return;
            }

            CurrencyChanged?.Invoke(currencyType, safeAmount);
            GameEvents.RaiseCurrencyChanged(currencyType, safeAmount);
        }

        /// <summary>Applies the starting balances from BalanceConfig (new game).</summary>
        public void ApplyStartingBalances()
        {
            SetAmount(CurrencyType.Gold, balanceConfig != null ? balanceConfig.StartingGold : 0d);
            SetAmount(CurrencyType.Gems, balanceConfig != null ? balanceConfig.StartingGems : 0d);
            SetAmount(CurrencyType.PrestigeTokens, 0d);
        }

        /// <summary>Gold-only reset used by the ascension flow (gems/tokens persist).</summary>
        public void ResetGold()
        {
            SetAmount(CurrencyType.Gold, 0d);
        }

        /// <summary>Restores a full snapshot (save load).</summary>
        public void Restore(double savedGold, double savedGems, double savedTokens)
        {
            SetAmount(CurrencyType.Gold, savedGold);
            SetAmount(CurrencyType.Gems, savedGems);
            SetAmount(CurrencyType.PrestigeTokens, savedTokens);
        }

        /// <summary>Debug-friendly snapshot string.</summary>
        public override string ToString()
        {
            return $"[Economy] Gold={NumberFormatter.Format(gold)} Gems={NumberFormatter.Format(gems)} Tokens={NumberFormatter.Format(prestigeTokens)}";
        }
    }
}