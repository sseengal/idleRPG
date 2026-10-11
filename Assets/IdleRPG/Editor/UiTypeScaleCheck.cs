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
    ///
    /// Split by job, the house pattern: this file is the entry points and the tables, <c>.Scene</c> walks the
    /// shipping scenes and prefabs, <c>.Code</c> reads the source, <c>.Report</c> prints the result.
    /// </summary>
    public static partial class UiTypeScaleCheck
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
    }
}
