using System;

namespace IdleRPG.Save
{
    /// <summary>
    /// Result of an offline-progress calculation, handed to the offline rewards popup.
    /// Immutable value type — safe to raise through events.
    /// </summary>
    [Serializable]
    public struct OfflineRewardResult
    {
        /// <summary>Real seconds the player was away.</summary>
        public double RawSeconds;

        /// <summary>Seconds actually paid out (capped, per BalanceConfig).</summary>
        public double CappedSeconds;

        /// <summary>Gold per second used for the estimate.</summary>
        public double GoldPerSecond;

        /// <summary>Gold awarded.</summary>
        public double Gold;

        /// <summary>True when the raw time exceeded the cap.</summary>
        public bool WasCapped;

        /// <summary>True when the result is worth showing to the player.</summary>
        public bool HasReward => Gold > 0d && CappedSeconds > 0d;

        public OfflineRewardResult(double rawSeconds, double cappedSeconds, double goldPerSecond, double gold, bool wasCapped)
        {
            RawSeconds = rawSeconds;
            CappedSeconds = cappedSeconds;
            GoldPerSecond = goldPerSecond;
            Gold = gold;
            WasCapped = wasCapped;
        }

        /// <summary>Result representing "nothing to claim".</summary>
        public static OfflineRewardResult None => new OfflineRewardResult(0d, 0d, 0d, 0d, false);

        /// <summary>True when the struct carries meaningful data.</summary>
        public bool IsValid => RawSeconds > 0d;
    }
}