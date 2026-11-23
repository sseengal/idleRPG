using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using IdleRPG.UI;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Moves the labels that are SAVED INSIDE scenes and prefabs onto the current theme fonts.
    ///
    /// ELI5: a token only repaints text the code builds while the game runs. Text saved in a scene keeps its own
    /// copy of the font and the size in the file, so those copies must be edited once. This is that one edit.
    ///
    /// It matches on the PAIR (old font, old size), never the size alone: 48 is both Title (Silkscreen) and
    /// Dialogue (VT323), so the number by itself would put a heading in the sentence face.
    ///
    /// Three tables, one per face that has been retired: Silkscreen, VT323, and - since 2026-11-23 - Abaddon Bold,
    /// which Lilita One replaced for every role except the battle log.
    ///
    /// Menu: Tools &gt; Idle RPG &gt; Art &gt; Migrate Theme Fonts
    /// Run once, after the Abaddon assets exist. A second run reports 0 changed, because no label still uses an
    /// old font. Nothing is guessed: a label in an old font at a size this table does not know is reported, not
    /// rewritten.
    /// </summary>
    public static class ThemeFontMigration
    {
        private const string OldDisplayPath = "Assets/IdleRPG/Resources/Fonts/Silkscreen Pixel.asset";
        private const string OldSentencePath = "Assets/IdleRPG/Resources/Fonts/VT323 Pixel.asset";

        /// <summary>The chrome face up to 2026-11-23, when Lilita One replaced it.</summary>
        private const string OldChromePath = "Assets/IdleRPG/Resources/Fonts/Abaddon Bold Pixel.asset";

        /// <summary>Set by <see cref="Migrate"/> so the label walk can recognise the old chrome face.</summary>
        private static TMP_FontAsset oldChrome;

        /// <summary>
        /// Lilita One as it was used before the ladder pass (2026-11-23): 55/48/40/29/30/28/32. Frozen for the
        /// same reason as the tables below - <see cref="UiType"/> already holds the NEW sizes, so it cannot tell us
        /// what an old saved label meant. 29 became 34 (Sentence), 28 became 26 (Micro), 55 became 56 (Display).
        /// 44 is deliberately absent: that size belongs to the log, which is Abaddon Light, not this face.
        /// </summary>
        private static readonly Dictionary<float, UiType.Role> OldLoudRoles = new Dictionary<float, UiType.Role>
        {
            { 55f, UiType.Role.Display },
            { 48f, UiType.Role.Title },
            { 40f, UiType.Role.Section },
            { 29f, UiType.Role.Sentence },
            { 30f, UiType.Role.Small },
            { 28f, UiType.Role.Micro },
            { 32f, UiType.Role.Damage },
        };

        /// <summary>
        /// Abaddon Bold as it was used up to 2026-11-23, at the sizes <see cref="UiType"/> held before the
        /// Lilita One move (62/54/45/36/34/29). Frozen here for the same reason as the tables below.
        /// </summary>
        private static readonly Dictionary<float, UiType.Role> OldChromeRoles = new Dictionary<float, UiType.Role>
        {
            { 62f, UiType.Role.Display },
            { 54f, UiType.Role.Title },
            { 45f, UiType.Role.Section },
            { 36f, UiType.Role.Damage },
            { 34f, UiType.Role.Small },
            { 29f, UiType.Role.Micro },
        };

        /// <summary>
        /// The scale as it stood before the font change (2026-11-15), frozen here on purpose: <see cref="UiType"/>
        /// holds the NEW sizes, so it cannot tell us what an old saved label meant.
        /// </summary>
        private static readonly Dictionary<float, UiType.Role> OldDisplayRoles = new Dictionary<float, UiType.Role>
        {
            { 56f, UiType.Role.Display },
            { 48f, UiType.Role.Title },
            { 40f, UiType.Role.Section },
            { 32f, UiType.Role.Damage },
            { 30f, UiType.Role.Small },
            { 26f, UiType.Role.Micro },
        };

        private static readonly Dictionary<float, UiType.Role> OldSentenceRoles = new Dictionary<float, UiType.Role>
        {
            { 48f, UiType.Role.Dialogue },
            { 36f, UiType.Role.Sentence },
        };

        [MenuItem("Tools/Idle RPG/Art/Migrate Theme Fonts", priority = 23)]
        public static void Migrate()
        {
            TMP_FontAsset oldDisplay = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OldDisplayPath);
            TMP_FontAsset oldSentence = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OldSentencePath);
            oldChrome = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OldChromePath);

            if (oldDisplay == null || oldSentence == null)
            {
                Debug.LogError("[ThemeFontMigration] An old font asset is missing, so nothing can be matched. " +
                               "Expected: " + OldDisplayPath);
                return;
            }

            if (UiTheme.Loud == null || UiTheme.Sentence == null)
            {
                Debug.LogError("[ThemeFontMigration] The theme fonts are not loaded. " +
                               "Run Tools > Idle RPG > Art > Generate Pixel Fonts first.");
                return;
            }

            int changed = 0;
            int skipped = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                changed += MigratePrefab(AssetDatabase.GUIDToAssetPath(guid), oldDisplay, oldSentence, ref skipped);
            }

            foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
            {
                if (entry == null || !entry.enabled)
                {
                    continue;
                }

                changed += MigrateScene(entry.path, oldDisplay, oldSentence, ref skipped);
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"[ThemeFontMigration] {changed} label(s) moved to the theme fonts, {skipped} left alone.");
        }

        /// <summary>Opens a prefab, rewrites its labels, saves it back - nested prefabs stay intact.</summary>
        private static int MigratePrefab(string path, TMP_FontAsset oldDisplay, TMP_FontAsset oldSentence,
            ref int skipped)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            int changed = 0;

            try
            {
                changed = MigrateLabels(contents.transform, path, oldDisplay, oldSentence, ref skipped);

                if (changed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    Debug.Log($"[ThemeFontMigration] {path}: {changed} label(s).");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return changed;
        }

        /// <summary>Opens a shipping scene if needed, rewrites its labels, saves it, then closes it again.</summary>
        private static int MigrateScene(string path, TMP_FontAsset oldDisplay, TMP_FontAsset oldSentence,
            ref int skipped)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool openedHere = !scene.isLoaded;

            if (openedHere)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            if (!scene.isLoaded)
            {
                Debug.LogWarning($"[ThemeFontMigration] {path} could not be opened, so it was not migrated.");
                return 0;
            }

            int changed = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                changed += MigrateLabels(root.transform, path, oldDisplay, oldSentence, ref skipped);
            }

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[ThemeFontMigration] {path}: {changed} label(s).");
            }

            if (openedHere)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            return changed;
        }

        /// <summary>One object and everything under it. Returns how many labels were rewritten.</summary>
        private static int MigrateLabels(Transform root, string area, TMP_FontAsset oldDisplay,
            TMP_FontAsset oldSentence, ref int skipped)
        {
            int changed = 0;

            foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                // An auto-sized label stores the last size it resolved to in fontSize and its role's size in
                // fontSizeMax - the role is the max, exactly as the type-scale check reads it.
                float size = label.enableAutoSizing ? label.fontSizeMax : label.fontSize;
                UiType.Role role;

                if (label.font == oldDisplay)
                {
                    if (!OldDisplayRoles.TryGetValue(size, out role))
                    {
                        Report(area, label, size);
                        skipped++;
                        continue;
                    }
                }
                else if (label.font == oldSentence)
                {
                    if (!OldSentenceRoles.TryGetValue(size, out role))
                    {
                        Report(area, label, size);
                        skipped++;
                        continue;
                    }
                }
                else if (label.font == oldChrome)
                {
                    if (!OldChromeRoles.TryGetValue(size, out role))
                    {
                        Report(area, label, size);
                        skipped++;
                        continue;
                    }
                }
                else if (label.font == UiTheme.Loud && OldLoudRoles.TryGetValue(size, out role))
                {
                    // A label already in the chrome face at a size the ladder no longer uses (55, 29, 28 ...).
                    // Labels already on the new ladder do not match and are left alone.
                }
                else
                {
                    // Already a theme label, or a lab / dev label: not ours to touch.
                    continue;
                }

                Apply(label, role);
                changed++;
            }

            return changed;
        }

        /// <summary>Writes the role's font and size, keeping an auto-sized label's shrink range valid.</summary>
        private static void Apply(TMP_Text label, UiType.Role role)
        {
            TMP_FontAsset font = UiType.Font(role);
            float size = UiType.Size(role);

            label.font = font;
            label.fontSharedMaterial = font.material;
            label.fontSize = size;

            if (label.enableAutoSizing)
            {
                label.fontSizeMin = UiType.MinSize;
                label.fontSizeMax = size;
            }

            EditorUtility.SetDirty(label);
        }

        private static void Report(string area, TMP_Text label, float size)
        {
            Debug.LogWarning($"[ThemeFontMigration] {area}: '{PathOf(label.transform)}' uses an old font at size " +
                             $"{size:0.#}, which is not on the old table. Left as it was - check it by hand.");
        }

        /// <summary>The object path, so a warning points at one label and not at a whole scene.</summary>
        private static string PathOf(Transform transform)
        {
            string path = transform.name;

            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }
}
