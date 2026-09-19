using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Save;

namespace IdleRPG.Economy
{
    /// <summary>
    /// Timed gold multiplier (the "watch an ad" reward). Pure C# with the clock injected,
    /// so nothing here needs Update() and the expiry survives an app kill via SaveData.
    /// </summary>
    public sealed class BoostManager
    {
        private readonly BalanceConfig balanceConfig;

        private bool isActive;
        private double expiresAtBinary;

        public BoostManager(BalanceConfig balanceConfig)
        {
            this.balanceConfig = balanceConfig;

            if (this.balanceConfig == null)
            {
                Debug.LogError("[BoostManager] BalanceConfig is null; boost falls back to x1.");
            }
        }

        /// <summary>Raised when the boost turns on or off (active, remainingSeconds, multiplier).</summary>
        public event System.Action<bool, float, double> BoostChanged;

        public bool IsActive => isActive;

        /// <summary>Multiplier applied to gold rewards (1 when inactive).</summary>
        public double GoldMultiplier => isActive && balanceConfig != null ? balanceConfig.AdGoldBoostMultiplier : 1d;

        /// <summary>Seconds left on the current boost (0 when inactive).</summary>
        public float RemainingSeconds
        {
            get
            {
                if (!isActive)
                {
                    return 0f;
                }

                double seconds = GameClock.SecondsBetween(GameClock.NowBinary, expiresAtBinary);
                return seconds <= 0d ? 0f : (float)seconds;
            }
        }

        // ------------------------------------------------------------------
        // Control
        // ------------------------------------------------------------------
        /// <summary>
        /// Starts or extends the boost (back-to-back ads stack their durations).
        /// Returns false only when the BalanceConfig is missing.
        /// </summary>
        public bool ActivateFromAd()
        {
            if (balanceConfig == null)
            {
                return false;
            }

            double now = GameClock.NowBinary;
            double durationTicks = balanceConfig.AdGoldBoostDurationSec * System.TimeSpan.TicksPerSecond;

            // Extend rather than overwrite so back-to-back ads are never wasted.
            double baseTicks = isActive && expiresAtBinary > now ? expiresAtBinary : now;
            expiresAtBinary = baseTicks + durationTicks;

            isActive = true;
            RaiseChanged();

            return true;
        }

        /// <summary>
        /// Re-evaluates expiry. Call once per second (or on resume). Returns true when the
        /// active state changed, so callers can refresh UI without polling.
        /// </summary>
        public bool Refresh()
        {
            if (!isActive)
            {
                return false;
            }

            if (GameClock.SecondsBetween(GameClock.NowBinary, expiresAtBinary) > 0d)
            {
                return false;
            }

            isActive = false;
            expiresAtBinary = 0d;
            RaiseChanged();

            return true;
        }

        /// <summary>Stops the boost immediately (debug/testing).</summary>
        public void Clear()
        {
            if (!isActive)
            {
                return;
            }

            isActive = false;
            expiresAtBinary = 0d;
            RaiseChanged();
        }

        // ------------------------------------------------------------------
        // Persistence
        // ------------------------------------------------------------------
        public void Restore(bool active, double savedExpiresAtBinary)
        {
            isActive = active;
            expiresAtBinary = savedExpiresAtBinary;
            Refresh();
            RaiseChanged();
        }

        public void WriteToSave(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            data.goldBoostActive = isActive;
            data.goldBoostExpiresAtBinary = expiresAtBinary;
        }

        private void RaiseChanged()
        {
            double multiplier = GoldMultiplier;
            float remaining = RemainingSeconds;

            BoostChanged?.Invoke(isActive, remaining, multiplier);
            GameEvents.RaiseGoldBoostChanged(isActive, remaining, multiplier);
        }
    }
}