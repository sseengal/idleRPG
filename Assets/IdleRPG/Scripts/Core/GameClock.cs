using System;

namespace IdleRPG.Core
{
    /// <summary>
    /// Single time source for the game. Everything time-based (offline progress, boost
    /// expiry, save timestamps) goes through here so it can be stubbed in tests or
    /// swapped for a server clock later.
    /// </summary>
    public static class GameClock
    {
        /// <summary>Current UTC time.</summary>
        public static DateTime UtcNow => DateTime.UtcNow;

        /// <summary>DateTime.ToBinary() of "now" — the format used in save files and PlayerPrefs.</summary>
        public static double NowBinary => DateTime.UtcNow.ToBinary();

        public static double ToBinary(DateTime utcTime)
        {
            return utcTime.ToBinary();
        }

        public static DateTime FromBinary(double binary)
        {
            try
            {
                return DateTime.FromBinary((long)binary);
            }
            catch (ArgumentException)
            {
                // Corrupt or tampered timestamp: treat it as "now" rather than crashing.
                UnityEngine.Debug.LogWarning($"[GameClock] Invalid timestamp binary value {binary}; falling back to now.");
                return DateTime.UtcNow;
            }
        }

        /// <summary>Whole seconds between two binary timestamps (never negative).</summary>
        public static double SecondsBetween(double earlierBinary, double laterBinary)
        {
            double seconds = (laterBinary - earlierBinary) / TimeSpan.TicksPerSecond;
            return seconds < 0d ? 0d : seconds;
        }
    }
}