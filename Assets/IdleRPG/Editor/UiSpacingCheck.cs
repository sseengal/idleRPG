using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using IdleRPG.UI;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Checks that no UI code writes a gap by hand: every spacing number must come from <see cref="UiSpace"/>.
    ///
    /// ELI5: the ruler inspector. It reads the UI code and points at any line that passes a raw number where a gap
    /// belongs. Before the ladder existed, eight different numbers (0, 2, 4, 6, 8, 10, 12, 14) were spread over
    /// about 60 call sites, and two panels doing the same job padded 14/6 and 10/10, so nothing lined up.
    ///
    /// Menu: Tools &gt; Idle RPG &gt; UI &gt; Check Spacing
    /// Batch: Unity -batchmode -quit -executeMethod IdleRPG.EditorTools.UiSpacingCheck.Check
    ///
    /// The ladder lives in <see cref="UiSpace"/>; this file only measures against it and never repeats a number.
    /// Two rules keep it honest:
    ///  * a ZERO inset is allowed - "no gap" is not a spacing choice, so writing 0f is not a magic number;
    ///  * the design labs are out of scope on purpose, because a lab compares looks and may pick a gap by hand.
    ///
    /// It is registered in <c>Run All Checks (regression)</c>, so a hand-written gap fails the run instead of
    /// shipping quietly.
    /// </summary>
    public static class UiSpacingCheck
    {
        public enum Severity
        {
            Error = 0,
            Warning = 1,
            Info = 2
        }

        public struct Issue
        {
            public Severity Severity;
            public string Area;
            public string Message;
        }

        /// <summary>Folders scanned for UI construction code.</summary>
        private static readonly string[] Roots =
        {
            "Assets/IdleRPG/Editor",
            "Assets/IdleRPG/Scripts/UI",
            "Assets/IdleRPG/Scripts/Debug"
        };

        /// <summary>Files allowed to write a gap by hand, each with the reason - so the list stays honest.</summary>
        private static readonly Dictionary<string, string> CodeAllowList = new Dictionary<string, string>
        {
            { "UiSpace.cs", "the ladder itself - the one place a gap number is written down" },
            { "UiLabSceneBuilder.cs", "the design lab compares looks, so it may pick a gap by hand" },
            { "DevOverlay.cs", "the F3 debug overlay - developer UI, not player UI" },
            { "InventoryTabUI.Grid.cs", "56 and 18 are layout offsets and -5 is the selection ring's overhang; not gaps" }
        };

        // The four-number inset tail of Anchor/Stretch: (rect, min, max, left, bottom, right, top).
        private static readonly Regex InsetTail = new Regex(
            @"\b(?:Anchor|Stretch)\s*\([^;]*?, *(-?\d+(?:\.\d+)?)f, *(-?\d+(?:\.\d+)?)f, *(-?\d+(?:\.\d+)?)f, *(-?\d+(?:\.\d+)?)f\)");

        // new RectOffset(left, right, top, bottom) - a hand-built padding.
        private static readonly Regex RectOffsetLiteral = new Regex(
            @"new\s+RectOffset\s*\(\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*\)");

        // CreateScrollView(parent, name, spacing, padding, ...) and VerticalStack(target, spacing, padding).
        private static readonly Regex ScrollSpacing = new Regex(
            @"CreateScrollView\s*\(\s*[^,]+,\s*[^,]+,\s*(-?\d+(?:\.\d+)?)f\s*,");

        private static readonly Regex StackSpacing = new Regex(
            @"VerticalStack\s*\(\s*[^,]+,\s*(-?\d+(?:\.\d+)?)f\s*,");

        private static readonly List<Issue> Issues = new List<Issue>();
        private static int fileCount;
        private static int callCount;

        [MenuItem("Tools/Idle RPG/UI/Check Spacing", priority = 111)]
        public static void Check()
        {
            Validate();
        }

        /// <summary>Runs the check and prints a PASS/FAIL summary. Returns the issues found.</summary>
        public static List<Issue> Validate()
        {
            Issues.Clear();
            fileCount = 0;
            callCount = 0;

            foreach (string root in Roots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    ScanFile(file.Replace('\\', '/'));
                }
            }

            LogSummary();
            return Issues;
        }

        /// <summary>
        /// Runs the check and returns true only when nothing failed. The regression net calls this, so a
        /// hand-written gap fails the run instead of shipping quietly.
        /// </summary>
        public static bool RunCheckWithResult()
        {
            foreach (Issue issue in Validate())
            {
                if (issue.Severity == Severity.Error)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Reads one file and reports every gap number that is not a role.</summary>
        private static void ScanFile(string path)
        {
            string fileName = Path.GetFileName(path);

            if (CodeAllowList.ContainsKey(fileName))
            {
                return;
            }

            fileCount++;
            string[] lines = File.ReadAllLines(path);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                ReportIfNonZero(InsetTail.Match(line), path, i + 1, line, "inset");
                ReportIfNonZero(RectOffsetLiteral.Match(line), path, i + 1, line, "RectOffset padding");
                ReportIfNonZero(ScrollSpacing.Match(line), path, i + 1, line, "scroll spacing");
                ReportIfNonZero(StackSpacing.Match(line), path, i + 1, line, "stack spacing");
            }
        }

        /// <summary>
        /// Reports a match when any captured number is not zero. A zero is allowed: "no gap" is not a spacing
        /// choice, so it does not belong on the ladder.
        /// </summary>
        private static void ReportIfNonZero(Match match, string path, int lineNumber, string line, string what)
        {
            if (!match.Success)
            {
                return;
            }

            callCount++;
            List<string> offenders = new List<string>();

            for (int g = 1; g < match.Groups.Count; g++)
            {
                if (match.Groups[g].Success && match.Groups[g].Value.TrimStart('-', '.', '0') != string.Empty)
                {
                    offenders.Add(match.Groups[g].Value);
                }
            }

            if (offenders.Count == 0)
            {
                return;
            }

            Issues.Add(new Issue
            {
                Severity = Severity.Error,
                Area = path,
                Message = $"{path}:{lineNumber} writes the {what} {string.Join(", ", offenders)} by hand. " +
                          $"Use a UiSpace role. Line: {line.Trim()}"
            });
        }

        private static void LogSummary()
        {
            foreach (Issue issue in Issues)
            {
                Debug.LogError($"[UiSpace] {issue.Message}");
            }

            Debug.Log($"[UiSpace] Checked {callCount} layout call(s) in {fileCount} file(s). " +
                      $"The ladder: {UiSpace.Hairline}/{UiSpace.Tight}/{UiSpace.Snug}/{UiSpace.Small}/" +
                      $"{UiSpace.Base}/{UiSpace.Medium}/{UiSpace.Gutter}/{UiSpace.Large}, " +
                      $"screen margin {UiSpace.ScreenMargin}.");

            bool ok = true;

            foreach (Issue issue in Issues)
            {
                if (issue.Severity == Severity.Error)
                {
                    ok = false;
                }
            }

            Debug.Log(ok
                ? "[UiSpace] SPACING: PASS - every gap in the UI code comes from UiSpace."
                : $"[UiSpace] SPACING: FAIL - {Issues.Count} hand-written gap(s) found.");
        }
    }
}
