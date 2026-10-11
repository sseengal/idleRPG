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
    /// The source half of the check: read every .cs under Assets/IdleRPG for a text size written by hand, and the
    /// two small helpers that scan line by line.
    /// </summary>
    public static partial class UiTypeScaleCheck
    {
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
