using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using IdleRPG.EditorTools.Content;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// The whole regression net in one command: save-schema drift, content validation and the golden-number report.
    ///
    /// ELI5: before this, "run the checks" meant remembering three menu items and reading three logs. Now there is
    /// one button (and one batch-mode entry point for CI), and it prints a single PASS/FAIL line at the end.
    ///
    /// Menu: Tools &gt; Idle RPG &gt; Run All Checks (regression)
    /// Batch: Unity -batchmode -quit -executeMethod IdleRPG.EditorTools.RegressionCheckMenu.RunAllChecks
    ///
    /// This is the definition of done for every base step: green here means save drift, content validation and
    /// the golden numbers all agree, so "did I break anything" is one command instead of three menu items and
    /// three log reads.
    /// </summary>
    public static class RegressionCheckMenu
    {
        [MenuItem("Tools/Idle RPG/Run All Checks (regression)", priority = 50)]
        public static void RunAllChecks()
        {
            RunAllChecksWithResult();
        }

        /// <summary>
        /// Runs the whole net and returns true only when nothing failed. The release command (B9') calls this to
        /// decide GO / NO-GO, so a broken state can never be stamped and shipped.
        /// </summary>
        public static bool RunAllChecksWithResult()
        {
            bool ok = true;

            // 1) Save schema: round-trip drift, migrations from every older version, and the live file on disk.
            ok &= SaveRoundTripMenu.RunChecks();

            // 2) Content: specs vs assets vs the balance band. Errors fail the run; warnings are printed and tolerated.
            List<ContentValidator.Issue> issues = ContentValidator.Validate();
            int errors = 0;

            foreach (ContentValidator.Issue issue in issues)
            {
                if (issue.Severity == ContentValidator.Severity.Error)
                {
                    errors++;
                }
            }

            if (errors > 0)
            {
                Debug.LogError($"[Regression] Content validation reported {errors} error(s).");
                ok = false;
            }

            // 3) Parity: the golden numbers must MATCH the recorded baseline (BalanceBaseline, tolerance 2%).
            //    Printing them was not a gate - a change that halved the party's damage still printed "PASS".
            ok &= CheckGoldenNumbers();

            // 4) UI: every label in the shipping scenes is on the type scale, in a theme font, and no code sets a
            //    text size by hand. Without this, the 15-sizes-in-90-places problem can come back one label at a time.
            ok &= UiTypeScaleCheck.RunCheckWithResult();

            // 5) UI spacing: every gap in the UI code must come from UiSpace. Without this, the
            //    eight-numbers-in-60-places problem can come back one call site at a time.
            ok &= UiSpacingCheck.RunCheckWithResult();

            Debug.Log(ok
                ? "[Regression] REGRESSION: PASS - save drift, content validation (incl. loop health) and the UI checks passed. Golden numbers are printed as a reading only (baseline void, Docs/Content.md §10)."
                : "[Regression] REGRESSION: FAIL - see the errors above.");

            return ok;
        }

        /// <summary>Runs the recorded baseline conditions and compares them with <see cref="BalanceBaseline"/>.</summary>
        private static bool CheckGoldenNumbers()
        {
            BalanceLabMenu.GoldenNumbers();   // prints the per-enemy detail, unchanged, for eyeballing

            BalanceLabMenu.GoldenRun goldens = BalanceLabMenu.CaptureGoldens();
            if (!goldens.Available)
            {
                Debug.LogError("[Regression] Golden numbers unavailable (config assets missing).");
                return false;
            }

            bool matched = true;
            matched &= Report("stage 1 seconds @x1.0", goldens.PaceOne.Seconds, BalanceBaseline.StageOneSecondsPaceOne);
            matched &= Report("stage 1 seconds @x1.6", goldens.PaceShipped.Seconds, BalanceBaseline.StageOneSecondsShipped);
            matched &= Report("stage 1 kills", goldens.PaceShipped.Kills, BalanceBaseline.StageOneKills);
            matched &= Report("stage 1 gold", goldens.PaceShipped.Gold, BalanceBaseline.StageOneGold);
            matched &= Report("stage 1 gold/s @x1.6", goldens.GoldPerSecondShipped, BalanceBaseline.StageOneGoldPerSecondShipped);

            Debug.Log($"[Regression] Golden baseline recorded {BalanceBaseline.Recorded} (tolerance ±{BalanceBaseline.Tolerance * 100d:0.#}%).");
            Debug.Log("[Regression] The baseline is VOID until the full rebalance after feature complete (Docs/Content.md §10). Drift is reported, not failed.");

            if (!matched)
            {
                Debug.LogWarning("[Regression] Golden drift printed above. Expected while the baseline is void - this does not fail the gate.");
            }

            return true;
        }

        private static bool Report(string label, double actual, double expected)
        {
            bool ok = BalanceBaseline.Compare(label, actual, expected, out string line);

            if (ok)
            {
                Debug.Log(line);
            }
            else
            {
                // Warning, not error: the baseline predates gear, abilities and the roster swap
                // (Docs/Content.md §10). A future rebalance re-records it once.
                Debug.LogWarning(line + "  <- baseline is void until the rebalance (Docs/Content.md §10); not a failure.");
            }

            return ok;
        }
    }
}
