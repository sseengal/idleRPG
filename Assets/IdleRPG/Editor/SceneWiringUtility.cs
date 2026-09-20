using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using IdleRPG.Combat;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Debugging;

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

        public static List<StatUpgradeData> LoadStatTracks()
        {
            return LoadOrdered<StatUpgradeData>(ConfigFolder, "StatUpgrade_ATK", "StatUpgrade_HP", "StatUpgrade_DEF");
        }

        public static List<PrestigeUpgradeData> LoadPrestigeUpgrades()
        {
            return LoadOrdered<PrestigeUpgradeData>(ConfigFolder, "Prestige_Gold", "Prestige_Damage", "Prestige_Health");
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
            gameManager.EditorInitializeProgression(statTracks, prestigeUpgrades);
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