using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using IdleRPG.Core;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Daily-streak development controls (B7 S2). The calendar uses an injected clock, so these menu items fake
    /// "today" and then run the REAL claim path - proving the rule set without waiting for actual midnights.
    /// </summary>
    public static class DailyStreakDebugMenu
    {
        [MenuItem("Tools/Idle RPG/Debug/Daily Streak/Log State (Play)", priority = 130)]
        public static void LogState()
        {
            GameManager manager = FindManager();
            if (manager?.DailyStreak == null)
            {
                return;
            }

            Debug.Log($"[DailyStreak] lastDate='{manager.DailyStreak.LastClaimDate}' streak={manager.DailyStreak.StreakCount} " +
                      $"today={manager.DailyStreak.Describe()} override={(manager.ClockOverride.HasValue ? manager.ClockOverride.Value.ToString("yyyy-MM-dd") : "none")}");
        }

        [MenuItem("Tools/Idle RPG/Debug/Daily Streak/Simulate Next-Day Claim (Play)", priority = 131)]
        public static void SimulateNextDay()
        {
            GameManager manager = FindManager();
            if (manager?.DailyStreak == null)
            {
                return;
            }

            DateTime next = NextDayFrom(manager);
            RunClaim(manager, next, $"simulating the claim as if today were {next:yyyy-MM-dd}");
        }

        [MenuItem("Tools/Idle RPG/Debug/Daily Streak/Simulate Missed Day (Play)", priority = 132)]
        public static void SimulateMissedDay()
        {
            GameManager manager = FindManager();
            if (manager?.DailyStreak == null)
            {
                return;
            }

            DateTime next = NextDayFrom(manager).AddDays(1);
            RunClaim(manager, next, $"simulating a MISSED day (claim as if today were {next:yyyy-MM-dd})");
        }

        [MenuItem("Tools/Idle RPG/Debug/Daily Streak/Simulate Clock Rollback (Play)", priority = 133)]
        public static void SimulateClockRollback()
        {
            GameManager manager = FindManager();
            if (manager?.DailyStreak == null)
            {
                return;
            }

            DateTime today = DateTime.Now.Date;
            string last = manager.DailyStreak.LastClaimDate;
            DateTime target = today.AddDays(-1);   // earlier than usual "today"
            if (DateTime.TryParseExact(last, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed) && parsed > today)
            {
                target = parsed.AddDays(-1);
            }

            RunClaim(manager, target, $"simulating a clock ROLLBACK (claim as if today were {target:yyyy-MM-dd})");
        }

        [MenuItem("Tools/Idle RPG/Debug/Daily Streak/Clear Override (Play)", priority = 134)]
        public static void ClearOverride()
        {
            GameManager manager = FindManager();
            if (manager == null)
            {
                return;
            }

            manager.ClockOverride = null;
            Debug.Log("[DailyStreak] override cleared - claims now use the real device date.");
        }

        [MenuItem("Tools/Idle RPG/Debug/Daily Streak/Reset Streak (Play)", priority = 135)]
        public static void ResetStreak()
        {
            GameManager manager = FindManager();
            if (manager?.DailyStreak == null || manager.Save == null)
            {
                return;
            }

            // Wash out any simulated date: lastDate = real today, streak 0 => no claim until tomorrow.
            manager.ClockOverride = null;
            manager.DailyStreak.Restore(DateTime.Now.Date.ToString("yyyy-MM-dd"), 0);
            manager.Save.SaveNow("streak-reset");
            Debug.Log("[DailyStreak] state reset to a clean 'claimed today, streak 0' - next claim is tomorrow (day 1).");
        }

        [MenuItem("Tools/Idle RPG/Debug/Daily Streak/Simulate Day-7 Boost (Play)", priority = 136)]
        public static void SimulateDay7Boost()
        {
            GameManager manager = FindManager();
            if (manager?.DailyStreak == null)
            {
                return;
            }

            // Plant a day-6 streak ending yesterday, then claim: reaches the cap day and grants the boost.
            manager.DailyStreak.Restore(DateTime.Now.Date.AddDays(-1).ToString("yyyy-MM-dd"), manager.Balance.DailyStreakCap - 1);
            manager.TryClaimDailyStreak();
        }

        private static DateTime NextDayFrom(GameManager manager)
        {
            string last = manager.DailyStreak.LastClaimDate;

            if (DateTime.TryParseExact(last, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
            {
                return parsed.AddDays(1);
            }

            return DateTime.Now.Date;
        }

        private static void RunClaim(GameManager manager, DateTime target, string note)
        {
            manager.ClockOverride = target;
            double gemsBefore = manager.Economy.Gems;
            manager.TryClaimDailyStreak();
            double gained = manager.Economy.Gems - gemsBefore;
            Debug.Log($"[DailyStreak] {note} -> gained {gained:0} gems, " +
                      $"streak={manager.DailyStreak.StreakCount} lastDate='{manager.DailyStreak.LastClaimDate}'");
        }

        private static GameManager FindManager()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DailyStreakDebugMenu] Run in Play mode; the streak only exists in a live game.");
                return null;
            }

            GameManager manager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            if (manager == null)
            {
                Debug.LogWarning("[DailyStreakDebugMenu] No GameManager in the scene.");
            }

            return manager;
        }
    }
}