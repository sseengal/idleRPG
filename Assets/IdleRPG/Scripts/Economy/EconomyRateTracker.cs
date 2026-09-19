using System.Collections.Generic;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Save;

namespace IdleRPG.Economy
{
    /// <summary>
    /// Measures real gold income over a rolling window so offline progress pays out at the rate
    /// the player actually earns. Offline payouts are excluded (via <see cref="BeginExternalGrant"/>)
    /// so the measurement can never inflate itself.
    /// </summary>
    public sealed class EconomyRateTracker
    {
        private readonly BalanceConfig balanceConfig;
        private readonly Queue<Sample> samples = new Queue<Sample>();

        private double lastKnownGold;
        private double windowTotal;
        private double firstSampleTime = -1d;
        private int externalGrantDepth;

        public EconomyRateTracker(BalanceConfig balanceConfig)
        {
            this.balanceConfig = balanceConfig;

            if (this.balanceConfig == null)
            {
                Debug.LogError("[EconomyRateTracker] BalanceConfig is null; using a 60s window.");
            }
        }

        /// <summary>Measured (or seeded) gold per second.</summary>
        public double GoldPerSecond { get; private set; }

        /// <summary>Seconds of usable samples collected so far.</summary>
        public double SampledSeconds => firstSampleTime < 0d ? 0d : Time.unscaledTime - firstSampleTime;

        /// <summary>True once there is enough data to trust the measurement.</summary>
        public bool HasEnoughSamples => SampledSeconds >= 10d && GoldPerSecond > 0d;

        private double WindowSeconds => balanceConfig != null ? balanceConfig.GoldPerSecondSampleWindowSec : 60f;

        /// <summary>Starts tracking; seeds the last known balance so the first delta is correct.</summary>
        public void Attach(EconomyManager economy)
        {
            if (economy == null)
            {
                return;
            }

            lastKnownGold = economy.Gold;
            GameEvents.CurrencyChanged += OnCurrencyChanged;
        }

        public void Detach()
        {
            GameEvents.CurrencyChanged -= OnCurrencyChanged;
        }

        /// <summary>
        /// Marks gold that does not come from combat (offline claim, ad boost, debug grants) so it is
        /// not counted as income. Always pair with <see cref="EndExternalGrant"/>.
        /// </summary>
        public void BeginExternalGrant()
        {
            externalGrantDepth++;
        }

        public void EndExternalGrant()
        {
            externalGrantDepth = Mathf.Max(0, externalGrantDepth - 1);
        }

        /// <summary>Seeds the rate from a save file when there is not enough live data yet.</summary>
        public void SeedFromSave(double savedGoldPerSecond)
        {
            if (savedGoldPerSecond > 0d && GoldPerSecond <= 0d)
            {
                GoldPerSecond = savedGoldPerSecond;
            }
        }

        public void WriteToSave(SaveData data)
        {
            if (data != null)
            {
                data.lastGoldPerSecond = GoldPerSecond;
            }
        }

        private void OnCurrencyChanged(CurrencyType currencyType, double newAmount)
        {
            if (currencyType != CurrencyType.Gold)
            {
                return;
            }

            double delta = newAmount - lastKnownGold;
            lastKnownGold = newAmount;

            if (delta <= 0d || externalGrantDepth > 0)
            {
                return;
            }

            PushSample(delta);
        }

        private void PushSample(double amount)
        {
            double now = Time.unscaledTime;

            if (firstSampleTime < 0d)
            {
                firstSampleTime = now;
            }

            samples.Enqueue(new Sample(now, amount));
            windowTotal += amount;

            TrimAndCompute(now);
        }

        private void TrimAndCompute(double now)
        {
            double cutoff = now - WindowSeconds;

            while (samples.Count > 0 && samples.Peek().Time < cutoff)
            {
                windowTotal -= samples.Dequeue().Amount;
            }

            windowTotal = windowTotal < 0d ? 0d : windowTotal;

            // Average over the sampled span (not the full window) so short sessions still measure.
            double span = Mathf.Min((float)WindowSeconds, Mathf.Max(1f, (float)(now - firstSampleTime)));
            GoldPerSecond = windowTotal / span;
        }

        private readonly struct Sample
        {
            public readonly double Time;
            public readonly double Amount;

            public Sample(double time, double amount)
            {
                Time = time;
                Amount = amount;
            }
        }
    }
}