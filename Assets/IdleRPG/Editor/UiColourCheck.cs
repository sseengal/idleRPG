using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Checks that no UI code writes a colour by hand: every constant colour must come from
    /// <see cref="IdleRPG.UI.UiTheme"/>.
    ///
    /// ELI5: the paint inspector. It reads the UI code and points at any line that writes a raw colour number
    /// instead of a named paint. Before the palette was named (2026-11-14), 114 different colours were spread
    /// over ~35 files - six of them near-identical golds - so no two screens matched and a tweak meant a hunt.
    ///
    /// Menu: Tools &gt; Idle RPG &gt; UI &gt; Check Colours
    /// Batch: Unity -batchmode -quit -executeMethod IdleRPG.EditorTools.UiColourCheck.Check
    ///
    /// One rule keeps it honest: a colour with a COMPUTED channel (the procedural glow, coin and vignette
    /// textures write <c>new Color(1f, 1f, 1f, a)</c>) is not a design decision, so it is not flagged. Only
    /// all-constant colours are.
    ///
    /// It is registered in <c>Run All Checks (regression)</c>, so a hand-written colour fails the run instead of
    /// shipping quietly.
    /// </summary>
    public static class UiColourCheck
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
            "Assets/IdleRPG/Scripts/UI"
        };

        /// <summary>Files allowed to write a colour by hand, each with the reason - so the list stays honest.</summary>
        private static readonly Dictionary<string, string> CodeAllowList = new Dictionary<string, string>
        {
            { "UiTheme.cs", "the palette itself - the one place a colour number is written down" }
        };

        // new Color(r, g, b) or new Color(r, g, b, a) where EVERY argument is a number. A computed channel
        // (new Color(1f, 1f, 1f, a)) does not match, which is what lets the procedural textures through.
        private static readonly Regex ConstantColour = new Regex(
            @"\bnew\s+Color(?:32)?\s*\(\s*-?\d+(?:\.\d+)?f?\s*,\s*-?\d+(?:\.\d+)?f?\s*,\s*-?\d+(?:\.\d+)?f?(?:\s*,\s*-?\d+(?:\.\d+)?f?\s*)?\)");

        private static readonly List<Issue> Issues = new List<Issue>();
        private static int fileCount;
        private static int colourCount;

        [MenuItem("Tools/Idle RPG/UI/Check Colours", priority = 112)]
        public static void Check()
        {
            Validate();
        }

        /// <summary>Runs the check and prints a PASS/FAIL summary. Returns the issues found.</summary>
        public static List<Issue> Validate()
        {
            Issues.Clear();
            fileCount = 0;
            colourCount = 0;

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
        /// hand-written colour fails the run instead of shipping quietly.
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

        /// <summary>Reads one file and reports every all-constant colour that is not a palette reference.</summary>
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

                if (line.TrimStart().StartsWith("//"))
                {
                    continue;
                }

                Match match = ConstantColour.Match(line);
                if (!match.Success)
                {
                    continue;
                }

                colourCount++;
                Issues.Add(new Issue
                {
                    Severity = Severity.Error,
                    Area = path,
                    Message = $"{path}:{i + 1} writes the colour {match.Value} by hand. Use a UiTheme token. " +
                              $"Line: {line.Trim()}"
                });
            }
        }

        private static void LogSummary()
        {
            foreach (Issue issue in Issues)
            {
                Debug.LogError($"[UiColour] {issue.Message}");
            }

            Debug.Log($"[UiColour] Checked {fileCount} file(s); {colourCount} hand-written colour(s) found.");

            bool ok = Issues.Count == 0;

            Debug.Log(ok
                ? "[UiColour] COLOURS: PASS - every constant colour in the UI code comes from UiTheme."
                : $"[UiColour] COLOURS: FAIL - {Issues.Count} hand-written colour(s) found.");
        }
    }
}
