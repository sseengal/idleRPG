using System;
using System.Globalization;

namespace IdleRPG.Utils
{
    /// <summary>
    /// Turns big idle-game numbers into compact labels ("1.23M", "4.5aa").
    /// Allocation-light: returns short strings, never uses string.Format in hot paths.
    /// </summary>
    public static class NumberFormatter
    {
        /// <summary>Short suffixes after the trillions range (idle-game convention).</summary>
        private static readonly string[] Suffixes =
        {
            "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc",
            "Ud", "Dd", "Td", "Qad", "Qid", "Sxd", "Spd", "Ocd", "Nod", "Vg"
        };

        /// <summary>Compact label with NO decimal point. See <see cref="Format(double, int)"/>.</summary>
        public static string Format(double value)
        {
            return Format(value, 0);
        }

        /// <summary>
        /// Compact, integer-only label: "7", "234", "1K", "2M". This is the ONE place game numbers become
        /// text, so the whole UI is decimal-free by construction.
        ///
        /// Rules:
        ///  - values under 1000 round to the nearest whole number (a positive value never shows as 0);
        ///  - bigger values round to a whole mantissa and carry tiers (999,600 -> "1M", never "1000K").
        ///
        /// The <paramref name="decimals"/> argument is kept for call-site compatibility and ignored on purpose:
        /// upgrade/multiplier descriptors that genuinely need fractions format themselves.
        /// </summary>
        public static string Format(double value, int decimals)
        {
            if (double.IsNaN(value))
            {
                return "0";
            }

            if (double.IsInfinity(value))
            {
                return "inf";
            }

            bool negative = value < 0d;
            double magnitude = negative ? -value : value;

            if (magnitude < 1000d)
            {
                long small = (long)Math.Round(magnitude, MidpointRounding.AwayFromZero);
                if (small == 0L && magnitude > 0d)
                {
                    small = 1L;   // a live value never reads as zero
                }

                string body = small.ToString("0", CultureInfo.InvariantCulture);
                return negative ? "-" + body : body;
            }

            int tier = 0;
            double scaled = magnitude;

            while (scaled >= 1000d && tier < Suffixes.Length - 1)
            {
                scaled /= 1000d;
                tier++;
            }

            long mantissa = (long)Math.Round(scaled, MidpointRounding.AwayFromZero);
            if (mantissa >= 1000L && tier < Suffixes.Length - 1)
            {
                mantissa /= 1000L;
                tier++;
            }

            string result = mantissa.ToString("0", CultureInfo.InvariantCulture) + Suffixes[tier];
            return negative ? "-" + result : result;
        }

        /// <summary>Whole-number label, e.g. for stage or level counters.</summary>
        public static string FormatWhole(double value)
        {
            return Format(value, 0);
        }

        /// <summary>Adds the per-second suffix, e.g. "12.3K/s".</summary>
        public static string FormatPerSecond(double value)
        {
            return Format(value) + "/s";
        }

        /// <summary>Label for a currency amount, e.g. "1.2M". Never null.</summary>
        public static string FormatCurrency(double value)
        {
            return Format(value);
        }

        /// <summary>
        /// Compact duration label: "45s", "12m 30s", "3h 05m", "2d 4h".
        /// </summary>
        public static string FormatDuration(double seconds)
        {
            if (double.IsNaN(seconds) || seconds <= 0d)
            {
                return "0s";
            }

            long total = (long)Math.Floor(seconds);
            long days = total / 86400;
            long hours = (total % 86400) / 3600;
            long minutes = (total % 3600) / 60;
            long secs = total % 60;

            if (days > 0)
            {
                return days + "d " + hours + "h";
            }

            if (hours > 0)
            {
                return hours + "h " + minutes.ToString("00", CultureInfo.InvariantCulture) + "m";
            }

            if (minutes > 0)
            {
                return minutes + "m " + secs.ToString("00", CultureInfo.InvariantCulture) + "s";
            }

            return secs + "s";
        }

        /// <summary>Clock style label for a running countdown, e.g. "0:29".</summary>
        public static string FormatCountdown(double seconds)
        {
            if (double.IsNaN(seconds) || seconds <= 0d)
            {
                return "0:00";
            }

            long total = (long)Math.Ceiling(seconds);
            long minutes = total / 60;
            long secs = total % 60;
            return minutes + ":" + secs.ToString("00", CultureInfo.InvariantCulture);
        }

        /// <summary>Signed percent label, e.g. "+15%" / "-5%".</summary>
        public static string FormatPercent(float fraction, int decimals = 0)
        {
            return FormatPercent((double)fraction, decimals);
        }

        /// <summary>Signed percent label from a fraction (0.15 -> "+15%").</summary>
        public static string FormatPercent(double fraction, int decimals = 0)
        {
            if (double.IsNaN(fraction) || double.IsInfinity(fraction))
            {
                return "0%";
            }

            double percent = fraction * 100d;
            string body = percent.ToString("0." + new string('#', decimals < 0 ? 0 : decimals), CultureInfo.InvariantCulture);

            return percent > 0d ? "+" + body + "%" : body + "%";
        }
    }
}