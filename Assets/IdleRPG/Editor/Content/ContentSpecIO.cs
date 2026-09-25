using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace IdleRPG.EditorTools.Content
{
    /// <summary>
    /// Reads/writes the JSON spec files and owns their paths.
    ///
    /// ELI5: the shelf where the recipe cards live. Cards are plain text so git can show exactly what changed
    /// when a hero gets stronger.
    /// </summary>
    public static class ContentSpecIO
    {
        public const string SpecRoot = "Assets/IdleRPG/Content/Specs";

        public const string HeroesPath = SpecRoot + "/heroes.json";
        public const string EnemiesPath = SpecRoot + "/enemies.json";
        public const string PartyPath = SpecRoot + "/party.json";
        public const string WavesPath = SpecRoot + "/waves.json";
        public const string TracksPath = SpecRoot + "/tracks.json";

        public const string DataRoot = "Assets/IdleRPG/Data";
        public const string HeroFolder = DataRoot + "/Heroes";
        public const string EnemyFolder = DataRoot + "/Enemies";
        public const string ConfigFolder = DataRoot + "/Config";

        /// <summary>Loads a spec file, or returns a fresh default when it is missing/unreadable.</summary>
        public static T Load<T>(string path) where T : class, new()
        {
            if (!File.Exists(path))
            {
                return new T();
            }

            string json = File.ReadAllText(path);
            T parsed = JsonUtility.FromJson<T>(json);

            if (parsed == null)
            {
                Debug.LogWarning($"[ContentSpecIO] {path} is unreadable; using defaults.");
                return new T();
            }

            return parsed;
        }

        /// <summary>Writes a spec file (pretty-printed so diffs are readable).</summary>
        public static void Save<T>(string path, T value)
        {
            EnsureFolder(SpecRoot);
            File.WriteAllText(path, JsonUtility.ToJson(value, true) + "\n");
            AssetDatabase.ImportAsset(path);
        }

        public static string ToHex(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(color);
        }

        public static Color FromHex(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex))
            {
                return fallback;
            }

            return ColorUtility.TryParseHtmlString(hex, out Color parsed) ? parsed : fallback;
        }

        /// <summary>Our own "CreateOrLoad": reuses the asset when present so GUIDs stay stable.</summary>
        public static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset != null)
            {
                return asset;
            }

            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
        }

        /// <summary>All assets of a type inside a folder, sorted by asset name for stable output.</summary>
        public static List<T> LoadAll<T>(string folder) where T : Object
        {
            List<T> results = new List<T>();
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder });

            for (int i = 0; i < guids.Length; i++)
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (asset != null)
                {
                    results.Add(asset);
                }
            }

            results.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return results;
        }
    }
}
