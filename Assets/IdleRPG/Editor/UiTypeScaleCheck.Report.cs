using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using IdleRPG.UI;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// What the check prints: how much was checked, the size and font histograms, every issue at its own severity,
    /// and one PASS/FAIL line. The small lookups the rules share live here too.
    /// </summary>
    public static partial class UiTypeScaleCheck
    {
        // ------------------------------------------------------------------
        // Reporting + small helpers
        // ------------------------------------------------------------------
        /// <summary>Prints the counts, the histograms, every issue, and one PASS/FAIL line.</summary>
        private static void LogSummary()
        {
            int errors = 0;
            int warnings = 0;
            int infos = 0;

            foreach (Issue issue in Issues)
            {
                switch (issue.Severity)
                {
                    case Severity.Error:
                        errors++;
                        Debug.LogError("[UiType] " + issue.Area + " -> " + issue.Message);
                        break;
                    case Severity.Warning:
                        warnings++;
                        Debug.LogWarning("[UiType] " + issue.Area + " -> " + issue.Message);
                        break;
                    default:
                        infos++;
                        Debug.Log("[UiType] " + issue.Area + " -> " + issue.Message);
                        break;
                }
            }

            Debug.Log("[UiType] Checked " + labelCount + " labels in " + sceneCount + " shipping scene(s) and " +
                prefabCount + " prefab(s).");
            Debug.Log("[UiType] Sizes in use: " + Histogram(SizeCounts) + "   (the scale: " + ScaleList() + ")");
            Debug.Log("[UiType] Fonts in use: " + Histogram(FontCounts));
            Debug.Log("[UiType] Allowed to set a size by hand: " + AllowListSummary());

            Debug.Log(errors == 0
                ? "[UiType] TYPE SCALE: PASS - every label is on the scale, in a theme font, and nothing sets a size by hand." +
                  (warnings > 0 || infos > 0 ? " (" + warnings + " warning(s), " + infos + " note(s) worth a look)" : string.Empty)
                : "[UiType] TYPE SCALE: FAIL - " + errors + " error(s), " + warnings + " warning(s).");
        }

        private static void Add(Severity severity, string area, string message)
        {
            Issues.Add(new Issue { Severity = severity, Area = area, Message = message });
        }

        private static void Count(Dictionary<string, int> counts, string key)
        {
            counts.TryGetValue(key, out int current);
            counts[key] = current + 1;
        }

        private static bool IsOnScale(float size)
        {
            foreach (float candidate in SizeToFont.Keys)
            {
                if (Mathf.Abs(candidate - size) < 0.01f)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The role whose size this is, or "unknown".</summary>
        private static string RoleOf(float size)
        {
            foreach (UiType.Role role in Roles)
            {
                if (Mathf.Abs(UiType.Size(role) - size) < 0.01f)
                {
                    return role.ToString();
                }
            }

            return "unknown";
        }

        /// <summary>The closest scale size, written as "40 (Section)" so a fix is obvious.</summary>
        private static string Nearest(float size)
        {
            float best = 0f;
            float bestGap = float.MaxValue;

            foreach (float candidate in SizeToFont.Keys)
            {
                float gap = Mathf.Abs(candidate - size);

                if (gap < bestGap)
                {
                    bestGap = gap;
                    best = candidate;
                }
            }

            return best.ToString("0.#") + " (" + RoleOf(best) + ")";
        }
        private static string ScaleList()
        {
            StringBuilder text = new StringBuilder();

            foreach (UiType.Role role in Roles)
            {
                if (text.Length > 0)
                {
                    text.Append(", ");
                }

                text.Append(role).Append(' ').Append(UiType.Size(role).ToString("0"));
            }

            return text.ToString();
        }

        private static string Histogram(Dictionary<string, int> counts)
        {
            if (counts.Count == 0)
            {
                return "(none)";
            }

            List<string> keys = new List<string>(counts.Keys);
            keys.Sort();
            StringBuilder text = new StringBuilder();

            foreach (string key in keys)
            {
                if (text.Length > 0)
                {
                    text.Append("  ");
                }

                text.Append(key).Append('=').Append(counts[key]);
            }

            return text.ToString();
        }

        private static string AllowListSummary()
        {
            List<string> names = new List<string>(CodeAllowList.Keys);
            names.Sort();
            return string.Join(", ", names);
        }

        /// <summary>The label's path in the scene, so the offender can be found by hand.</summary>
        private static string PathOf(Transform node)
        {
            List<string> parts = new List<string>();
            Transform current = node;

            while (current != null)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

    }
}
