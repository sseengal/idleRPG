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
    /// </summary>
    public static class RegressionCheckMenu
    {
        [MenuItem("Tools/Idle RPG/Run All Checks (regression)", priority = 50)]
        public static void RunAllChecks()
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

            // 3) Parity: the golden numbers are printed for eyeball comparison against the recorded baseline
            //    (80s @x1 / 126s @x1.6, 22 kills, 272 gold, 2.16 gold/s).
            BalanceLabMenu.GoldenNumbers();

            Debug.Log(ok
                ? "[Regression] REGRESSION: PASS - save drift, content validation and the golden-number report are in line."
                : "[Regression] REGRESSION: FAIL - see the errors above.");
        }
    }
}
