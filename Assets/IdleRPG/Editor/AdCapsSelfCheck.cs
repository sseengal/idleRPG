using System;
using UnityEditor;
using UnityEngine;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Save;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Headless checks for the ad-caps rules (B7 S3). The caps logic is pure C# with an injected clock, so this
    /// runs with NO play mode, no frames and no ad SDK - from the menu or headless:
    ///   Unity -batchmode -quit -projectPath ... -executeMethod IdleRPG.EditorTools.AdCapsSelfCheck.RunAll
    /// Exit code 0 = all green (used by the terminal verification loop).
    /// </summary>
    public static class AdCapsSelfCheck
    {
        private static int failures;

        [MenuItem("Tools/Idle RPG/Debug/Ads/Run Ad-Caps Self Check", priority = 133)]
        public static void RunAll()
        {
            failures = 0;

            BalanceConfig balance = BalanceLabMenu.Load<BalanceConfig>("BalanceConfig");

            if (balance == null)
            {
                Debug.LogError("[AdCapsSelfCheck] BalanceConfig missing; cannot run.");
                Report();
                return;
            }

            DateTime clock = new DateTime(2026, 9, 27, 9, 0, 0);
            AdCapsService caps = new AdCapsService(balance, () => clock);

            AdPlacementDef boost = balance.GetAdPlacement(AdPlacementId.GoldBoost);
            AdPlacementDef doubled = balance.GetAdPlacement(AdPlacementId.DoubleOffline);

            Debug.Log(string.Format("[AdCapsSelfCheck] GoldBoost {0}/day {1}s | DoubleOffline {2}/day {3}s",
                boost.DailyCap, boost.CooldownSec, doubled.DailyCap, doubled.CooldownSec));

            Check("fresh day: full cap available",
                caps.RemainingToday(AdPlacementId.GoldBoost) == boost.DailyCap &&
                caps.CanShow(AdPlacementId.GoldBoost, out _));

            caps.MarkShown(AdPlacementId.GoldBoost);

            Check("one used: remaining drops by one",
                caps.RemainingToday(AdPlacementId.GoldBoost) == boost.DailyCap - 1);

            bool blocked = !caps.CanShow(AdPlacementId.GoldBoost, out double wait) && wait > 0d;
            Check("cooldown blocks the next immediate use", blocked);
            Debug.Log($"[AdCapsSelfCheck] cooldown reported {wait:0}s remaining");

            clock = clock.AddSeconds(boost.CooldownSec + 1d);
            Check("cooldown elapses", caps.CanShow(AdPlacementId.GoldBoost, out _));

            for (int i = 1; i < boost.DailyCap; i++)
            {
                caps.MarkShown(AdPlacementId.GoldBoost);
            }

            Check("cap reached: nothing left", caps.RemainingToday(AdPlacementId.GoldBoost) == 0);
            Check("cap reached: blocked with no cooldown wait",
                !caps.CanShow(AdPlacementId.GoldBoost, out double capWait) && capWait <= 0d);

            clock = clock.AddDays(1);
            Check("next local day resets the cap",
                caps.RemainingToday(AdPlacementId.GoldBoost) == boost.DailyCap &&
                caps.CanShow(AdPlacementId.GoldBoost, out _));

            caps.MarkShown(AdPlacementId.DoubleOffline);
            SaveData data = SaveData.CreateDefault();
            caps.WriteToSave(data);

            AdCapsService restored = new AdCapsService(balance, () => clock);
            restored.Restore(data.adRedemptions);

            Check("save round-trip keeps the counters",
                restored.RemainingToday(AdPlacementId.DoubleOffline) == doubled.DailyCap - 1);

            Report();
        }

        private static void Check(string what, bool ok)
        {
            if (ok)
            {
                Debug.Log($"[AdCapsSelfCheck] OK   {what}");
                return;
            }

            failures++;
            Debug.LogError($"[AdCapsSelfCheck] FAIL {what}");
        }

        private static void Report()
        {
            if (failures == 0)
            {
                Debug.Log("[AdCapsSelfCheck] RESULT: PASS - ad caps, cooldowns, day rollover and the save round-trip are in line.");
            }
            else
            {
                Debug.LogError($"[AdCapsSelfCheck] RESULT: FAIL - {failures} check(s) failed.");
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(failures == 0 ? 0 : 1);
            }
        }
    }
}
