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
    /// Checks that every piece of text in the game is on the type scale, and that no code sets a size by hand.
    ///
    /// ELI5: this is the ruler that lives in the workshop. It walks the shipping scenes and every prefab, reads the
    /// size and the font of every label, and names the ones that do not match the list. It also reads the code and
    /// points at any line that sets a size directly - which is how the game got 15 different sizes in about 90
    /// places, half of them in Unity's smooth default font at half the size a phone needs.
    ///
    /// Menu: Tools &gt; Idle RPG &gt; UI &gt; Check Type Scale
    /// Batch: Unity -batchmode -quit -executeMethod IdleRPG.EditorTools.UiTypeScaleCheck.Check
    ///
    /// The scale itself lives in <see cref="UiType"/>; this file only measures against it and never repeats a number.
    /// The design labs (`UiLab.unity`, `ArtLab.unity`) are out of scope on purpose: a lab compares looks, so it is
    /// allowed to pick a size by hand through `UiFactory.RawText`.
    /// </summary>
    public static class UiTypeScaleCheck
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

        /// <summary>The roles, in scale order. Used to build the lookup tables below.</summary>
        private static readonly UiType.Role[] Roles =
        {
            UiType.Role.Display, UiType.Role.Title, UiType.Role.Section, UiType.Role.Sentence,
            UiType.Role.Small, UiType.Role.Micro, UiType.Role.Damage
        };

        /// <summary>Files allowed to touch fontSize directly, each with the reason - so the list stays honest.</summary>
        private static readonly Dictionary<string, string> CodeAllowList = new Dictionary<string, string>
        {
            { "UiType.cs", "the scale itself - the one place a size is written down" },
            { "UiTheme.cs", "the low-level applier every role goes through" },
            { "FloatingDamageTextView.cs", "crit text = the damage role x UiType.CritScale (intended)" },
            { "EnemyUnitView.cs", "a boss name = the small role x a boss scale (intended)" },
            { "DevOverlay.cs", "the F3 debug overlay - developer UI, not player UI" },
            { "UiFactory.cs", "RawText, the design labs' escape hatch (labs are out of scope)" },
        };

        private static readonly Regex FontSizeAssignment = new Regex(@"fontSize(Min|Max)?\s*=[^=]");

        private static readonly List<Issue> Issues = new List<Issue>();
        private static readonly Dictionary<string, int> SizeCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> FontCounts = new Dictionary<string, int>();
        private static int labelCount;
        private static int sceneCount;
        private static int prefabCount;

        /// <summary>Sizes that are on the scale, and the font that size's role uses.</summary>
        private static readonly Dictionary<float, TMP_FontAsset> SizeToFont = new Dictionary<float, TMP_FontAsset>();

        [MenuItem("Tools/Idle RPG/UI/Check Type Scale", priority = 110)]
        public static void Check()
        {
            Validate();
        }

        /// <summary>Runs every check and prints a PASS/FAIL summary. Returns the issues found.</summary>
        public static List<Issue> Validate()
        {
            Issues.Clear();
            SizeCounts.Clear();
            FontCounts.Clear();
            labelCount = 0;
            sceneCount = 0;
            prefabCount = 0;

            BuildSizeTable();
            CheckBuildScenes();
            CheckPrefabs();
            CheckCode();
            LogSummary();

            return Issues;
        }

        /// <summary>
        /// Runs every check and returns true only when nothing failed. The regression net calls this, so a label
        /// off the scale - or a new hand-written size - fails the run instead of shipping quietly.
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

        /// <summary>Fills the size to font lookup from the roles, so no number is repeated in this file.</summary>
        private static void BuildSizeTable()
        {
            SizeToFont.Clear();

            foreach (UiType.Role role in Roles)
            {
                SizeToFont[UiType.Size(role)] = UiType.Font(role);
            }
        }
        // ------------------------------------------------------------------
        // What gets checked
        // ------------------------------------------------------------------
        /// <summary>
        /// Every scene the build actually ships (the enabled entries in Build Settings). A scene that is not open is
        /// opened additively, checked and closed again, so the check works in batch mode and does not disturb the
        /// scene the developer is working in.
        /// </summary>
        private static void CheckBuildScenes()
        {
            foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
            {
                if (entry == null || !entry.enabled)
                {
                    continue;
                }

                Scene scene = SceneManager.GetSceneByPath(entry.path);
                bool openedHere = !scene.isLoaded;

                if (openedHere)
                {
                    scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Additive);
                }

                if (!scene.isLoaded)
                {
                    Add(Severity.Warning, entry.path, "The scene could not be opened, so it was not checked.");
                    continue;
                }

                sceneCount++;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    CheckObject(root.transform, entry.path);
                }

                if (openedHere)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        /// <summary>Every prefab in the project: a prefab can carry labels that no open scene shows.</summary>
        private static void CheckPrefabs()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null)
                {
                    continue;
                }

                prefabCount++;
                CheckObject(prefab.transform, path);
            }
        }

        /// <summary>Checks one object and everything under it.</summary>
        private static void CheckObject(Transform root, string area)
        {
            foreach (TextMeshProUGUI label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                CheckLabel(label, area);
            }
        }
        /// <summary>
        /// The rules for one label: a theme font, a size on the scale, and the font that size's role uses. An
        /// auto-sized label may shrink, but only down to the floor and only up to its own size.
        /// </summary>
        private static void CheckLabel(TextMeshProUGUI label, string area)
        {
            labelCount++;

            string path = PathOf(label.transform);
            bool auto = label.enableAutoSizing;

            // An auto-sized label stores the last size it resolved to in fontSize, and the role's size in
            // fontSizeMax - so its ROLE is fontSizeMax. Reading fontSize here would flag every shrunken label
            // (a nav tab that shrank to 26 is still a Small label, not a Micro one).
            float roleSize = auto ? label.fontSizeMax : label.fontSize;
            string sizeKey = roleSize.ToString("0.#");
            string fontName = label.font != null ? label.font.name : "(none)";

            Count(SizeCounts, sizeKey);
            Count(FontCounts, fontName);

            if (label.font == null)
            {
                Add(Severity.Error, area, path + ": no font assigned (it would draw in Unity's default font).");
                return;
            }

            bool themeFont = label.font == UiTheme.Display || label.font == UiTheme.Sentence;

            if (!themeFont)
            {
                Add(Severity.Error, area, path + ": font is '" + fontName + "', not a theme font (Silkscreen or VT323).");
            }

            if (!IsOnScale(roleSize))
            {
                // The one intended off-scale size: crit damage text is the damage role times CritScale.
                bool isCrit = Mathf.Abs(roleSize - UiType.Damage * UiType.CritScale) < 0.05f;
                Add(isCrit ? Severity.Info : Severity.Error, area,
                    path + ": size " + sizeKey + " is not on the scale. Nearest: " + Nearest(roleSize) + ".");
                return;
            }

            if (themeFont && SizeToFont.TryGetValue(roleSize, out TMP_FontAsset expected) && label.font != expected)
            {
                Add(Severity.Warning, area,
                    path + ": size " + sizeKey + " is the " + RoleOf(roleSize) + " role, which uses '" + expected.name +
                    "', but this label uses '" + fontName + "'.");
            }

            if (auto)
            {
                if (label.fontSizeMin < UiType.MinSize - 0.01f)
                {
                    Add(Severity.Warning, area, path + ": shrinks to " + label.fontSizeMin.ToString("0.#") +
                        ", below the floor of " + UiType.MinSize.ToString("0") + ".");
                }

                if (label.fontSize < label.fontSizeMin - 0.01f || label.fontSize > label.fontSizeMax + 0.01f)
                {
                    Add(Severity.Warning, area, path + ": its resolved size (" + label.fontSize.ToString("0.#") +
                        ") is outside its own range of " + label.fontSizeMin.ToString("0.#") + "-" +
                        label.fontSizeMax.ToString("0.#") + ".");
                }
            }
        }

        /// <summary>
        /// Reads the code for hand-written sizes. The scene builders are checked on purpose: after the role
        /// migration they speak roles, so a new raw number in one of them is a regression, not a style choice.
        /// </summary>
        private static void CheckCode()
        {
            string root = Path.Combine(Application.dataPath, "IdleRPG");

            if (!Directory.Exists(root))
            {
                Add(Severity.Warning, "code", "Assets/IdleRPG was not found, so the code was not checked.");
                return;
            }

            foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (CodeAllowList.ContainsKey(Path.GetFileName(file)))
                {
                    continue;
                }

                string[] lines = File.ReadAllLines(file);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (!FontSizeAssignment.IsMatch(StripComment(lines[i])))
                    {
                        continue;
                    }

                    Add(Severity.Error, ShortPath(file),
                        "line " + (i + 1) + ": sets a text size by hand - use a UiType.Role instead. " + lines[i].Trim());
                }
            }
        }
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

        /// <summary>Drops a trailing "// comment" so a commented-out line is not reported.</summary>
        private static string StripComment(string line)
        {
            int comment = line.IndexOf("//", System.StringComparison.Ordinal);
            return comment >= 0 ? line.Substring(0, comment) : line;
        }

        private static string ShortPath(string absolutePath)
        {
            string normalized = absolutePath.Replace('\\', '/');
            int index = normalized.IndexOf("Assets/", System.StringComparison.Ordinal);
            return index >= 0 ? normalized.Substring(index) : normalized;
        }
    }
}
