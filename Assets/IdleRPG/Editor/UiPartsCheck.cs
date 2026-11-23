using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Reports every Image that does not use a UiSkin part. This is the guard for "everything is mapped to one
    /// component": a screen that invents its own sprite shows up here instead of quietly drifting.
    ///
    /// Menu: Tools > Idle RPG > UI > Check UI Parts
    /// </summary>
    public static class UiPartsCheck
    {
        [MenuItem("Tools/Idle RPG/UI/Check UI Parts", priority = 40)]
        public static void Check()
        {
            var allowed = new HashSet<Sprite>();
            IdleRPG.UI.UiSkin skin = AssetDatabase.LoadAssetAtPath<IdleRPG.UI.UiSkin>(
                "Assets/IdleRPG/Resources/UiSkin.asset");

            if (skin != null)
            {
                Add(allowed, skin.frameSprite);
                Add(allowed, skin.containerSprite);
                Add(allowed, skin.panelSprite);
                Add(allowed, skin.buttonSprite);
                Add(allowed, skin.primaryButtonSprite);
                Add(allowed, skin.dangerButtonSprite);
                Add(allowed, skin.neutralButtonSprite);
                Add(allowed, skin.bannerSprite);
            }

            int checkedCount = 0;
            var unmapped = new List<string>();

            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Count(root, allowed, ref checkedCount, unmapped);
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/IdleRPG" });

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    Count(prefab, allowed, ref checkedCount, unmapped, path);
                }
            }

            if (unmapped.Count == 0)
            {
                Debug.Log($"[UiPartsCheck] PASS - {checkedCount} image(s) checked, every one uses a skin part.");
                return;
            }

            System.IO.Directory.CreateDirectory("Temp");
            System.IO.File.WriteAllLines("Temp/ui-parts-report.txt", unmapped);

            Debug.LogWarning($"[UiPartsCheck] {unmapped.Count} image(s) are outside UiParts ({checkedCount} checked). " +
                             "Full list: Temp/ui-parts-report.txt");
        }

        private static void Count(GameObject root, HashSet<Sprite> allowed, ref int checkedCount, List<string> unmapped,
            string area = null)
        {
            Image[] images = root.GetComponentsInChildren<Image>(true);

            foreach (Image image in images)
            {
                checkedCount++;

                if (image.sprite == null || allowed.Contains(image.sprite))
                {
                    continue;
                }

                // Icons and backdrop art are pictures, not surfaces: they are allowed to be their own sprites.
                if (image.sprite.name.StartsWith("ui_icon") || image.sprite.name.Contains(" bg"))
                {
                    continue;
                }

                string path = PathOf(image.transform);
                unmapped.Add((area != null ? area : "SCENE") + " :: " + path + " -> " + image.sprite.name);
            }
        }

        private static void Add(HashSet<Sprite> set, Sprite sprite)
        {
            if (sprite != null)
            {
                set.Add(sprite);
            }
        }

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
