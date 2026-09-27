using System;
using System.Globalization;
using IdleRPG.Data;

namespace IdleRPG.Economy
{
    /// <summary>
    /// The every-morning gift (B7 S2). Pure C# with an injected clock, so the debug menu can fake dates and the
    /// whole rule set is exercisable without waiting a real day (or touching <c>DateTime.Now</c>).
    ///
    /// ELI5: a calendar on the wall. Open the app each morning -> the calendar hands over some gems. Come back
    /// every day and the gift grows; miss a day and the calendar starts over. Turning the clock BACK cannot fool it.
    ///
    /// Design (tenured-team review 2026-09-27):
    ///  - PERSIST-FIRST: <see cref="TryClaim"/> writes today's date into state BEFORE the caller's SaveNow, so a
    ///    crash between paying and saving can never double-pay a day.
    ///  - BOUNDARY: the calendar flips at the DEVICE'S local midnight (G13 locked) - UTC never matters to a player.
    ///  - TAMPER: a saved date in the future (clock rolled back) refuses the claim and changes nothing.
    /// </summary>
    public sealed class DailyStreakService
    {
        /// <summary>One claim attempt: whether anything was paid and what tomorrow promises.</summary>
        public struct ClaimResult
        {
            public bool Claimed;
            public int Day;         // streak length claimed (1..cap)
            public int Gems;        // gems paid for this day
            public bool Day7Boost;  // the cap day also granted a gold boost
            public int NextDay;     // the day tomorrow's claim would reach (capped)
            public int NextGems;    // what tomorrow would pay
        }

        private readonly BalanceConfig balance;
        private readonly Func<DateTime> now;

        public DailyStreakService(BalanceConfig balance, Func<DateTime> now)
        {
            this.balance = balance;
            this.now = now ?? (() => DateTime.Now);
        }

        /// <summary>Local date ("yyyy-MM-dd") of the last successful claim; "" = never claimed.</summary>
        public string LastClaimDate { get; private set; } = "";

        /// <summary>Consecutive-day count at the last claim (1..cap).</summary>
        public int StreakCount { get; private set; }

        /// <summary>
        /// Restores from a save. An unparseable/absent date is treated as "never claimed" - it must never crash.
        /// </summary>
        public void Restore(string lastClaimDate, int streakCount)
        {
            LastClaimDate = ValidateDate(lastClaimDate) ? lastClaimDate : "";
            StreakCount = streakCount < 0 ? 0 : streakCount;
        }

        /// <summary>
        /// The streak day the player is currently on, as of the injected clock: 0 = no streak yet today (nothing
        /// claimable or a fresh gap), N = they have already reached day N. Used only for display.
        /// </summary>
        public int CurrentDay
        {
            get
            {
                DateTime today = now().Date;
                string todayStr = Format(today);

                if (string.IsNullOrEmpty(LastClaimDate) || !ValidateDate(LastClaimDate))
                {
                    return 0;
                }

                DateTime last = Parse(LastClaimDate);

                if (LastClaimDate == todayStr)
                {
                    return Math.Max(1, StreakCount);
                }

                if (last == today.AddDays(-1))
                {
                    return StreakCount >= balance.DailyStreakCap ? balance.DailyStreakCap : StreakCount + 1;
                }

                return 1; // a gap: the next claim starts fresh at day 1
            }
        }
        /// <summary>
        /// Attempts the daily claim. Sets the persisted date FIRST (persist-first), so the caller can save right
        /// after and a crash can never pay the same day twice. Refuses - changing nothing - when the day was
        /// already claimed or the saved date is in the future (clock roll-back).
        /// </summary>
        public ClaimResult TryClaim()
        {
            ClaimResult result = default;

            DateTime today = now().Date;
            string todayStr = Format(today);

            bool hasLast = !string.IsNullOrEmpty(LastClaimDate) && ValidateDate(LastClaimDate);

            if (hasLast)
            {
                DateTime last = Parse(LastClaimDate);

                if (last > today)
                {
                    result.Claimed = false;   // clock rolled back
                    return result;
                }

                if (LastClaimDate == todayStr)
                {
                    result.Claimed = false;   // already claimed today
                    return result;
                }
            }

            int newStreak;

            if (!hasLast)
            {
                newStreak = 1;
            }
            else if (Parse(LastClaimDate) == today.AddDays(-1))
            {
                newStreak = Math.Min(StreakCount + 1, balance.DailyStreakCap);
                newStreak = Math.Max(1, newStreak);
            }
            else
            {
                newStreak = 1; // missed at least one full day
            }

            int gems = balance.DailyStreakGemsForDay(newStreak);
            bool boost = balance.DailyStreakDay7Boost && newStreak >= balance.DailyStreakCap;

            // Persist-first: the caller saves immediately after this, locking the day.
            LastClaimDate = todayStr;
            StreakCount = newStreak;

            int nextDay = Math.Min(newStreak + 1, balance.DailyStreakCap);

            result.Claimed = true;
            result.Day = newStreak;
            result.Gems = gems;
            result.Day7Boost = boost;
            result.NextDay = nextDay;
            result.NextGems = balance.DailyStreakGemsForDay(nextDay);
            return result;
        }

        /// <summary>Display string for the shop header, e.g. "Day 3/7 - next +10".</summary>
        public string Describe()
        {
            int day = CurrentDay;
            int cap = balance.DailyStreakCap;

            if (day <= 0)
            {
                return string.Format("today +{0} gems", balance.DailyStreakGemsForDay(1));
            }

            return string.Format("Day {0}/{1} - next +{2}", day, cap, balance.DailyStreakGemsForDay(day == cap ? cap : day + 1));
        }

        private static string Format(DateTime date)
        {
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static DateTime Parse(string value)
        {
            return DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static bool ValidateDate(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }
    }
}
