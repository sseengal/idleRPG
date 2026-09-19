using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using IdleRPG.Combat;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Debugging;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// One-click authoring for the headless combat test scene.
    ///
    /// Menu: Tools > Idle RPG > Create Combat Debug Scene
    ///
    /// The scene contains a single "GameManager" object carrying GameManager
    /// (state machine + economy), CombatManager (ticker + waves) and
    /// CombatEventLogger (console mirror), with every data asset reference wired.
    /// </summary>
    public static class CombatDebugSceneBuilder
    {
        private const string ScenePath = "Assets/IdleRPG/Scenes/CombatDebug.unity";
        private const string ConfigFolder = "Assets/IdleRPG/Data/Config";

        [MenuItem("Tools/Idle RPG/Create Combat Debug Scene", priority = 20)]
        public static void CreateDebugScene()
        {
            // Only generate when something is missing: regenerating reimports assets,
            // which would invalidate references loaded in the same tick.
            if (!HasDataAssets())
            {
                DataAssetGenerator.GenerateAll();
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Load AFTER the scene swap so the references stay valid when the scene is saved.
            BalanceConfig balance = Load<BalanceConfig>(ConfigFolder + "/BalanceConfig.asset");
            WaveConfig waveConfig = Load<WaveConfig>(ConfigFolder + "/WaveConfig.asset");
            PartyConfig partyConfig = Load<PartyConfig>(ConfigFolder + "/PartyConfig.asset");

            if (balance == null || waveConfig == null || partyConfig == null)
            {
                Debug.LogError("[CombatDebugSceneBuilder] Data assets missing. Run Tools > Idle RPG > Generate Data Assets first.");
                return;
            }

            GameObject root = new GameObject("GameManager");
            CombatManager combatManager = root.AddComponent<CombatManager>();
            GameManager gameManager = root.AddComponent<GameManager>();
            root.AddComponent<CombatEventLogger>();

            gameManager.EditorInitialize(balance, waveConfig, partyConfig, combatManager);
            EditorUtility.SetDirty(gameManager);

            if (gameManager.Balance == null || gameManager.WaveData == null || gameManager.Combat == null)
            {
                Debug.LogError("[CombatDebugSceneBuilder] Wiring verification failed: references did not apply.");
                return;
            }

            EnsureFolder(System.IO.Path.GetDirectoryName(ScenePath).Replace('\\', '/'));

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"[CombatDebugSceneBuilder] Failed to save scene to {ScenePath}.");
                return;
            }

            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[CombatDebugSceneBuilder] Created {ScenePath} wired to BalanceConfig='{gameManager.Balance.name}', " +
                      $"WaveConfig='{gameManager.WaveData.name}', Combat='{gameManager.Combat.name}'. Press Play to watch the battle in the Console.");
        }

        [MenuItem("Tools/Idle RPG/Open Combat Debug Scene", priority = 21)]
        public static void OpenDebugScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                CreateDebugScene();
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        /// <summary>True when every data asset the debug scene needs already exists.</summary>
        private static bool HasDataAssets()
        {
            return AssetDatabase.LoadAssetAtPath<BalanceConfig>(ConfigFolder + "/BalanceConfig.asset") != null
                && AssetDatabase.LoadAssetAtPath<WaveConfig>(ConfigFolder + "/WaveConfig.asset") != null
                && AssetDatabase.LoadAssetAtPath<PartyConfig>(ConfigFolder + "/PartyConfig.asset") != null;
        }

        private static T Load<T>(string assetPath) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                Debug.LogWarning($"[CombatDebugSceneBuilder] Missing asset: {assetPath}");
            }

            return asset;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
        }

        /// <summary>Adds the scene to the build list (idempotent) and enables it.</summary>
        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    scenes[i].enabled = true;
                    EditorBuildSettings.scenes = scenes.ToArray();
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}