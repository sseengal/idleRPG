using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using IdleRPG.Combat;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Debugging;
using IdleRPG.EditorTools.Content;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Shared authoring helpers for the scene builders: data-asset loading and reliable
    /// assignment of private [SerializeField] references on freshly created components.
    ///
    /// Reflection is used deliberately: it keeps runtime views free of editor-only setters,
    /// and it is more robust than SerializedObject when wiring many fields in one pass.
    /// Missing fields are logged loudly instead of failing silently.
    /// </summary>
    public static class SceneWiringUtility
    {
        public const string ConfigFolder = "Assets/IdleRPG/Data/Config";
        public const string HeroFolder = "Assets/IdleRPG/Data/Heroes";
        public const string EnemyFolder = "Assets/IdleRPG/Data/Enemies";
        public const string ArtFolder = PlaceholderSpriteGenerator.ArtFolder;

        // ------------------------------------------------------------------
        // Data assets
        // ------------------------------------------------------------------
        public static T LoadAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset == null)
            {
                Debug.LogWarning($"[SceneWiringUtility] Missing asset: {path}");
            }

            return asset;
        }

        public static BalanceConfig LoadBalance() => LoadAsset<BalanceConfig>(ConfigFolder + "/BalanceConfig.asset");

        public static WaveConfig LoadWaveConfig() => LoadAsset<WaveConfig>(ConfigFolder + "/WaveConfig.asset");

        public static PartyConfig LoadPartyConfig() => LoadAsset<PartyConfig>(ConfigFolder + "/PartyConfig.asset");

        /// <summary>Step 10: the party board (rows/columns, unlocks, row rules).</summary>
        public static FormationData LoadFormationConfig() =>
            LoadAsset<FormationData>(ConfigFolder + "/Formation_Default.asset");

        /// <summary>
        /// The gold-bought hero stat tracks, in spec order. Loading from the spec (not a hardcoded list) is what makes
        /// "a new track = one spec row + generate + scene rebuild" true: a new entry here is picked up automatically.
        /// </summary>
        public static List<StatUpgradeData> LoadStatTracks()
        {
            List<StatUpgradeData> tracks = new List<StatUpgradeData>();
            UpgradeSpecFile spec = ContentSpecIO.Load<UpgradeSpecFile>(ContentSpecIO.TracksPath);

            if (spec == null)
            {
                return tracks;
            }

            for (int i = 0; i < spec.statUpgrades.Count; i++)
            {
                StatUpgradeSpec entry = spec.statUpgrades[i];
                if (entry == null)
                {
                    continue;
                }

                string assetName = string.IsNullOrEmpty(entry.asset) ? entry.id : entry.asset;
                StatUpgradeData asset = LoadAsset<StatUpgradeData>(ConfigFolder + "/" + assetName + ".asset");

                if (asset != null)
                {
                    tracks.Add(asset);
                }
            }

            return tracks;
        }

        /// <summary>The token-bought permanent tracks, in spec order (same "new row is picked up" rule).</summary>
        public static List<PrestigeUpgradeData> LoadPrestigeUpgrades()
        {
            List<PrestigeUpgradeData> tracks = new List<PrestigeUpgradeData>();
            UpgradeSpecFile spec = ContentSpecIO.Load<UpgradeSpecFile>(ContentSpecIO.TracksPath);

            if (spec == null)
            {
                return tracks;
            }

            for (int i = 0; i < spec.prestigeUpgrades.Count; i++)
            {
                PrestigeUpgradeSpec entry = spec.prestigeUpgrades[i];
                if (entry == null)
                {
                    continue;
                }

                string assetName = string.IsNullOrEmpty(entry.asset) ? entry.id : entry.asset;
                PrestigeUpgradeData asset = LoadAsset<PrestigeUpgradeData>(ConfigFolder + "/" + assetName + ".asset");

                if (asset != null)
                {
                    tracks.Add(asset);
                }
            }

            return tracks;
        }

        /// <summary>The automation cards (auto-buy manager, speed button), in spec order (B6).</summary>
        public static List<AutomationDef> LoadAutomationDefs()
        {
            List<AutomationDef> defs = new List<AutomationDef>();
            UpgradeSpecFile spec = ContentSpecIO.Load<UpgradeSpecFile>(ContentSpecIO.TracksPath);

            if (spec == null)
            {
                return defs;
            }

            for (int i = 0; i < spec.automationUpgrades.Count; i++)
            {
                AutomationSpec entry = spec.automationUpgrades[i];
                if (entry == null)
                {
                    continue;
                }

                string assetName = string.IsNullOrEmpty(entry.asset) ? entry.id : entry.asset;
                AutomationDef def = LoadAsset<AutomationDef>(ConfigFolder + "/" + assetName + ".asset");

                if (def != null)
                {
                    defs.Add(def);
                }
            }

            return defs;
        }

        /// <summary>Loads assets by file name, preserving order and skipping nulls.</summary>
        public static List<T> LoadOrdered<T>(string folder, params string[] fileNames) where T : Object
        {
            List<T> assets = new List<T>();

            foreach (string fileName in fileNames)
            {
                T asset = LoadAsset<T>(folder + "/" + fileName + ".asset");
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            return assets;
        }

        // ------------------------------------------------------------------
        // Field assignment
        // ------------------------------------------------------------------
        /// <summary>
        /// Assigns a private serialized field by name and marks the object dirty.
        /// Logs an error when the field does not exist (renamed or removed).
        /// </summary>
        public static bool SetField(Object target, string fieldName, object value, bool required = true)
        {
            if (target == null)
            {
                Debug.LogError($"[SceneWiringUtility] Cannot set '{fieldName}': target is null.");
                return false;
            }

            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (field == null)
            {
                string message = $"[SceneWiringUtility] Field '{fieldName}' not found on {target.GetType().Name}.";

                if (required)
                {
                    Debug.LogError(message);
                }
                else
                {
                    Debug.LogWarning(message);
                }

                return false;
            }

            field.SetValue(target, value);
            EditorUtility.SetDirty(target);
            return true;
        }

        /// <summary>Assigns several fields in one pass.</summary>
        public static void SetFields(Object target, params (string field, object value)[] assignments)
        {
            foreach ((string field, object value) in assignments)
            {
                SetField(target, field, value);
            }
        }

        // ------------------------------------------------------------------
        // Game systems object
        // ------------------------------------------------------------------
        /// <summary>
        /// Creates the "GameManager" object with the runtime systems attached and wired:
        /// CombatManager (waves), GameManager (FSM + economy + progression), the console
        /// mirror and the debug hotkeys.
        /// </summary>
        public static GameManager CreateGameManagerObject(BalanceConfig balance, WaveConfig waveConfig,
            PartyConfig partyConfig, List<StatUpgradeData> statTracks, List<PrestigeUpgradeData> prestigeUpgrades,
            bool enableDebugLogger, bool enableHotkeys, FormationData formationConfig = null)
        {
            GameObject root = new GameObject("GameManager");

            CombatManager combatManager = root.AddComponent<CombatManager>();
            GameManager gameManager = root.AddComponent<GameManager>();
            CombatEventLogger logger = root.AddComponent<CombatEventLogger>();
            DebugHotkeys hotkeys = root.AddComponent<DebugHotkeys>();

            gameManager.EditorInitialize(balance, waveConfig, partyConfig, combatManager, formationConfig);
            gameManager.EditorInitializeProgression(statTracks, prestigeUpgrades, LoadAutomationDefs());
            hotkeys.EditorInitialize(gameManager);

            SetField(combatManager, "logCombatEvents", enableDebugLogger);
            logger.enabled = enableDebugLogger;
            hotkeys.enabled = enableHotkeys;

            EditorUtility.SetDirty(gameManager);
            EditorUtility.SetDirty(hotkeys);

            if (gameManager.Balance == null || gameManager.Combat == null || gameManager.Party == null)
            {
                Debug.LogError("[SceneWiringUtility] GameManager wiring verification failed.");
            }

            return gameManager;
        }
    }
}