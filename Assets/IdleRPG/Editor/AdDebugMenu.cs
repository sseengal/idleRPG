using System;
using UnityEditor;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Ad-caps development controls (B7 S3). Exercises the per-day police + cooldowns without watching real ads.
    /// </summary>
    public static class AdDebugMenu
    {
        [MenuItem("Tools/Idle RPG/Debug/Ads/Log Caps (Play)", priority = 130)]
        public static void LogCaps()
        {
            GameManager manager = FindManager();
            if (manager?.AdCaps == null)
            {
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder("[AdCaps] ");
            foreach (AdPlacementId id in Enum.GetValues(typeof(AdPlacementId)))
            {
                bool can = manager.AdCaps.CanShow(id, out double wait);
                sb.Append(string.Format("{0}: left={1} canShow={2}{3} | ",
                    id,
                    manager.AdCaps.RemainingToday(id),
                    can,
                    !can && wait > 0d ? $" (wait {wait:0}s)" : string.Empty));
            }

            Debug.Log(sb.ToString());
        }

        [MenuItem("Tools/Idle RPG/Debug/Ads/Reset Daily Caps (Play)", priority = 131)]
        public static void ResetCaps()
        {
            GameManager manager = FindManager();
            if (manager?.AdCaps == null || manager.Save == null)
            {
                return;
            }

            manager.AdCaps.ClearAll();
            manager.Save.SaveNow("ad-caps-reset");
            Debug.Log("[AdCaps] daily counters cleared and saved.");
        }

        [MenuItem("Tools/Idle RPG/Debug/Ads/Simulate Next Day (Play)", priority = 132)]
        public static void SimulateNextDay()
        {
            GameManager manager = FindManager();
            if (manager == null)
            {
                return;
            }

            manager.ClockOverride = DateTime.Now.Date.AddDays(1);
            manager.AdCaps?.ClearAll();
            LogCaps();

            // The override persists only for this play session; log it so it is never a silent trap.
            Debug.Log("[AdCaps] ClockOverride set to TOMORROW - caps reset. Real dates restored after the next relaunch.");
        }

        private static GameManager FindManager()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[AdDebugMenu] Run in Play mode; the caps only exist in a live game.");
                return null;
            }

            GameManager manager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            if (manager == null)
            {
                Debug.LogWarning("[AdDebugMenu] No GameManager in the scene.");
            }

            return manager;
        }
    }
}