using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using IdleRPG.Data;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Generates every ScriptableObject asset the game needs (heroes, enemies, wave/party
    /// config, stat upgrades, prestige upgrades). Idempotent: existing assets are reused and
    /// only their fields are refreshed, so it is safe to re-run at any time.
    ///
    /// Menu: Tools > Idle RPG > Generate Data Assets
    /// </summary>
    public static class DataAssetGenerator
    {
        private const string DataRoot = "Assets/IdleRPG/Data";
        private const string ConfigFolder = DataRoot + "/Config";
        private const string HeroFolder = DataRoot + "/Heroes";
        private const string EnemyFolder = DataRoot + "/Enemies";

        [MenuItem("Tools/Idle RPG/Generate Data Assets")]
        public static void GenerateAll()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                EnsureFolder(DataRoot);
                EnsureFolder(ConfigFolder);
                EnsureFolder(HeroFolder);
                EnsureFolder(EnemyFolder);

                BalanceConfig balance = CreateOrLoad<BalanceConfig>(ConfigFolder + "/BalanceConfig.asset");

                List<HeroData> heroes = CreateHeroes();
                List<EnemyData> enemies = CreateEnemies();

                WaveConfig waveConfig = CreateOrLoad<WaveConfig>(ConfigFolder + "/WaveConfig.asset");
                new Editable(waveConfig)
                    .SetObjectList("normalEnemies", enemies.GetRange(0, 3))
                    .SetObjectList("bossEnemies", new List<EnemyData> { enemies[3] })
                    .Apply();

                PartyConfig partyConfig = CreateOrLoad<PartyConfig>(ConfigFolder + "/PartyConfig.asset");
                new Editable(partyConfig)
                    .SetObjectList("heroes", new List<Object> { heroes[0], heroes[1], heroes[2] })
                    .Apply();

                ApplyBalance(balance);
                CreateStatUpgrades();
                CreatePrestigeUpgrades();

                Debug.Log($"[DataAssetGenerator] Data assets ready (balance={balance.name}, heroes={heroes.Count}, enemies={enemies.Count}).");
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[DataAssetGenerator] Generation failed: {exception}");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        /// <summary>Explicitly writes the reviewed MVP balance values into BalanceConfig.</summary>
        private static void ApplyBalance(BalanceConfig balance)
        {
            new Editable(balance)
                .Set("enemyHealthGrowth", 1.15f)
                .Set("enemyGoldGrowth", 1.12f)
                .Set("enemyAttackGrowth", 1.08f)
                .Set("normalWavesPerStage", 10)
                .Set("enemiesPerWave", 1)
                .Set("gemsPerBossKill", 1)
                .Set("waveTransitionDelaySec", 0.35f)
                .Set("minDamageRatio", 0.15f)
                .Set("criticalChance", 0.05f)
                .Set("criticalDamageMultiplier", 2f)
                .Set("healHeroesOnStageAdvance", true)
                .Set("reviveHeroesEachWave", false)
                .Set("enemyTargeting", (int)EnemyTargetingMode.FrontMost)
                .Set("combatTickIntervalSec", 0.05f)
                .Set("stageRollbackOnDefeat", 1)
                .Set("resetAutoRetryOnDefeat", true)
                .Set("scaleEnemyDefenseWithStage", false)
                .Set("logCombatToConsole", true)
                .Set("upgradeCostGrowth", 1.07f)
                .Set("upgradeStatGainPerLevel", 0.1f)
                .Set("prestigeStageDivisor", 10f)
                .Set("prestigeExponent", 1.5f)
                .Set("minStageToAscend", 10)
                .Set("offlineCapSeconds", 28800f)
                .Set("offlineEfficiency", 0.7f)
                .Set("minOfflineSecondsForPopup", 30f)
                .Set("startingGold", 0d)
                .Set("startingGems", 0d)
                .Set("goldPerSecondSampleWindowSec", 60f)
                .Set("adGoldBoostMultiplier", 2d)
                .Set("adGoldBoostDurationSec", 3600f)
                .Set("autosaveIntervalSec", 15f)
                .Apply();
        }

        // ------------------------------------------------------------------
        // Content definitions
        // ------------------------------------------------------------------
        private static List<HeroData> CreateHeroes()
        {
            List<HeroData> heroes = new List<HeroData>();

            HeroData knight = CreateOrLoad<HeroData>(HeroFolder + "/Hero_Knight.asset");
            new Editable(knight)
                .Set("heroID", "hero_knight")
                .Set("heroName", "Knight")
                .Set("baseHealth", 240f)
                .Set("baseAttack", 12f)
                .Set("baseDefense", 12f)
                .Set("attackIntervalSec", 1.5f)
                .SetColor("placeholderTint", new Color(0.35f, 0.55f, 0.95f, 1f))
                .Apply();
            heroes.Add(knight);

            HeroData archer = CreateOrLoad<HeroData>(HeroFolder + "/Hero_Archer.asset");
            new Editable(archer)
                .Set("heroID", "hero_archer")
                .Set("heroName", "Archer")
                .Set("baseHealth", 110f)
                .Set("baseAttack", 8f)
                .Set("baseDefense", 4f)
                .Set("attackIntervalSec", 1f)
                .SetColor("placeholderTint", new Color(0.35f, 0.85f, 0.45f, 1f))
                .Apply();
            heroes.Add(archer);

            HeroData mage = CreateOrLoad<HeroData>(HeroFolder + "/Hero_Mage.asset");
            new Editable(mage)
                .Set("heroID", "hero_mage")
                .Set("heroName", "Mage")
                .Set("baseHealth", 130f)
                .Set("baseAttack", 16f)
                .Set("baseDefense", 5f)
                .Set("attackIntervalSec", 2f)
                .SetColor("placeholderTint", new Color(0.75f, 0.4f, 0.95f, 1f))
                .Apply();
            heroes.Add(mage);

            return heroes;
        }

        private static List<EnemyData> CreateEnemies()
        {
            List<EnemyData> enemies = new List<EnemyData>();

            // Baseline: a fresh party (8 DPS) clears a wave in ~8s and a stage in ~90s.
            enemies.Add(CreateEnemy("Enemy_Slime", "Slime", 60f, 6f, 0f, 8f, 2f, false, 1f, 1f, new Color(0.5f, 0.9f, 0.4f, 1f)));
            enemies.Add(CreateEnemy("Enemy_Bat", "Bat", 90f, 9f, 1f, 12f, 1.6f, false, 1f, 1f, new Color(0.6f, 0.4f, 0.3f, 1f)));
            enemies.Add(CreateEnemy("Enemy_Goblin", "Goblin", 130f, 12f, 3f, 18f, 1.8f, false, 1f, 1f, new Color(0.4f, 0.75f, 0.35f, 1f)));

            // Stage-1 boss: 120 x5 = 600 HP (~25s) and ~150 gold (about half a stage income).
            enemies.Add(CreateEnemy("Boss_Ogre", "Ogre Chieftain", 120f, 20f, 5f, 25f, 2.5f, true, 5f, 6f, new Color(0.85f, 0.25f, 0.2f, 1f)));

            return enemies;
        }

        private static EnemyData CreateEnemy(string fileName, string displayName, float health, float attack,
            float defense, float gold, float interval, bool isBoss, float bossHealthMultiplier,
            float bossGoldMultiplier, Color tint)
        {
            EnemyData enemy = CreateOrLoad<EnemyData>(EnemyFolder + "/" + fileName + ".asset");
            new Editable(enemy)
                .Set("enemyName", displayName)
                .Set("baseHealth", health)
                .Set("baseAttack", attack)
                .Set("baseDefense", defense)
                .Set("baseGoldDrop", gold)
                .Set("attackIntervalSec", interval)
                .Set("isBoss", isBoss)
                .Set("bossHealthMultiplier", bossHealthMultiplier)
                .Set("bossGoldMultiplier", bossGoldMultiplier)
                .SetColor("placeholderTint", tint)
                .Apply();

            return enemy;
        }

        private static void CreateStatUpgrades()
        {
            CreateStatUpgrade("StatUpgrade_ATK", HeroStatType.Attack, 10d, "+10% of base ATK per level.");
            CreateStatUpgrade("StatUpgrade_HP", HeroStatType.Health, 12d, "+10% of base HP per level.");
            CreateStatUpgrade("StatUpgrade_DEF", HeroStatType.Defense, 15d, "+10% of base DEF per level.");
        }

        private static void CreateStatUpgrade(string fileName, HeroStatType statType, double baseCost, string description)
        {
            StatUpgradeData upgrade = CreateOrLoad<StatUpgradeData>(ConfigFolder + "/" + fileName + ".asset");
            new Editable(upgrade)
                .Set("statType", (int)statType)
                .Set("displayName", statType.ToDisplayName())
                .Set("description", description)
                .Set("baseCost", baseCost)
                .Set("costGrowthMultiplier", 1.07f)
                .Set("statGainPerLevelFraction", 0.1f)
                .Set("maxLevel", 0)
                .Apply();
        }

        private static void CreatePrestigeUpgrades()
        {
            CreatePrestigeUpgrade("Prestige_Gold", "Gold Mastery", PrestigeEffectType.GoldPercent, "+5% Gold per level.");
            CreatePrestigeUpgrade("Prestige_Damage", "War Mastery", PrestigeEffectType.DamagePercent, "+5% Damage per level.");
            CreatePrestigeUpgrade("Prestige_Health", "Vitality Mastery", PrestigeEffectType.HealthPercent, "+5% HP per level.");
        }

        private static void CreatePrestigeUpgrade(string fileName, string displayName, PrestigeEffectType effectType, string description)
        {
            PrestigeUpgradeData upgrade = CreateOrLoad<PrestigeUpgradeData>(ConfigFolder + "/" + fileName + ".asset");
            new Editable(upgrade)
                .Set("upgradeID", fileName)
                .Set("displayName", displayName)
                .Set("description", description)
                .Set("effectType", (int)effectType)
                .Set("baseCostTokens", 1d)
                .Set("costGrowth", 1.5f)
                .Set("maxLevel", 10)
                .Set("effectPerLevel", 0.05f)
                .Apply();
        }

        // ------------------------------------------------------------------
        // Asset plumbing
        // ------------------------------------------------------------------
        private static T CreateOrLoad<T>(string assetPath) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));
            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, assetPath);
            return created;
        }

        /// <summary>Creates a folder and every missing parent folder.</summary>
        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string leaf = Path.GetFileName(folderPath);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>
        /// Fluent <see cref="SerializedObject"/> wrapper so private [SerializeField] fields
        /// can be authored without widening their access.
        /// </summary>
        private sealed class Editable
        {
            private readonly SerializedObject serializedObject;

            public Editable(Object target)
            {
                serializedObject = new SerializedObject(target);
            }

            public Editable Set(string fieldName, float value)
            {
                SerializedProperty property = Find(fieldName);
                if (property != null)
                {
                    property.floatValue = value;
                }

                return this;
            }

            public Editable Set(string fieldName, int value)
            {
                SerializedProperty property = Find(fieldName);
                if (property != null)
                {
                    property.intValue = value;
                }

                return this;
            }

            public Editable Set(string fieldName, double value)
            {
                SerializedProperty property = Find(fieldName);
                if (property != null)
                {
                    property.doubleValue = value;
                }

                return this;
            }

            public Editable Set(string fieldName, bool value)
            {
                SerializedProperty property = Find(fieldName);
                if (property != null)
                {
                    property.boolValue = value;
                }

                return this;
            }

            public Editable Set(string fieldName, string value)
            {
                SerializedProperty property = Find(fieldName);
                if (property != null)
                {
                    property.stringValue = value;
                }

                return this;
            }

            public Editable SetColor(string fieldName, Color value)
            {
                SerializedProperty property = Find(fieldName);
                if (property != null)
                {
                    property.colorValue = value;
                }

                return this;
            }

            /// <summary>Replaces a List&lt;T&gt; of asset references.</summary>
            public Editable SetObjectList<T>(string fieldName, List<T> values) where T : Object
            {
                SerializedProperty property = Find(fieldName);
                if (property == null)
                {
                    return this;
                }

                property.ClearArray();
                int count = values == null ? 0 : values.Count;
                property.arraySize = count;

                for (int i = 0; i < count; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }

                return this;
            }

            public void Apply()
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(serializedObject.targetObject);
            }

            private SerializedProperty Find(string fieldName)
            {
                SerializedProperty property = serializedObject.FindProperty(fieldName);
                if (property == null)
                {
                    Debug.LogError($"[DataAssetGenerator] Field '{fieldName}' not found on " +
                                   $"{serializedObject.targetObject.GetType().Name} ({serializedObject.targetObject.name}).");
                }

                return property;
            }
        }
    }
}