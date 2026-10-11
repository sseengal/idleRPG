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
    /// The asset half of the check: open every scene the build actually ships (additively, so it also works in
    /// batch mode), walk every prefab, and judge each label - a theme font, a size on the scale, and the font that
    /// size's role uses.
    /// </summary>
    public static partial class UiTypeScaleCheck
    {
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

    }
}
