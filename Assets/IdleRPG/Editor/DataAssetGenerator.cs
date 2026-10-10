using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using IdleRPG.Data;
using IdleRPG.Progression;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Generates every ScriptableObject asset the game needs (heroes, enemies, wave/party
    /// config, stat upgrades, prestige upgrades). Idempotent: existing assets are reused and
    /// only their fields are refreshed, so it is safe to re-run at any time.
    ///
    /// Menu: Tools > Idle RPG > Generate Data Assets
    ///
    /// Field assignment goes through the shared <see cref="Editable"/> helper.
    /// </summary>
    public static class DataAssetGenerator
    {
        private const string DataRoot = "Assets/IdleRPG/Data";
        private const string ConfigFolder = DataRoot + "/Config";
        private const string HeroFolder = DataRoot + "/Heroes";
        private const string EnemyFolder = DataRoot + "/Enemies";
        private const string ArtFolder = PlaceholderSpriteGenerator.ArtFolder;

        // Test-pass stand-in art (2026-09-30): real sprites for the layout test, static idle frames.
        // Licence for both packs still to be sorted before release - see Assets/ThirdParty/PROVENANCE.md.
        // Slime + Bat keep procedural art until two PixelLab generations are spent (token = Temp/pixellab.token).
        private const string SoldierIdleSheet = "Assets/ThirdParty/TinyRPG_01_SoldierOrc/Characters(100x100 split)/Soldier/Soldier/Soldier_Idle.png";
        private const string SoldierAttack01Sheet = "Assets/ThirdParty/TinyRPG_01_SoldierOrc/Characters(100x100 split)/Soldier/Soldier/Soldier_Attack01.png";
        private const string OrcIdleSheet = "Assets/ThirdParty/TinyRPG_01_SoldierOrc/Characters(100x100 split)/Orc/Orc/Orc_Idle.png";
        private const string GoblinIdleSheet = "Assets/ThirdParty/PixelLab/goblin/Goblin_Idle_anim.png";

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
                CreateCurrencies();
                CreateFormation();

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
            // Wave-size recipe A: 5 singles / 10 doubles / 5 triples out of 20 fights (Step 11d).
            balance.EditorSetWaveCounts(1, 3, new WaveCountWeight(1, 25), new WaveCountWeight(2, 50), new WaveCountWeight(3, 25));

            new Editable(balance)
                .Set("enemyHealthGrowth", 1.15f)
                .Set("enemyGoldGrowth", 1.12f)
                .Set("enemyAttackGrowth", 1.08f)
                .Set("normalWavesPerStage", 10)
                .Set("minEnemiesPerWave", 1)
                .Set("maxEnemiesPerWave", 3)
                .Set("gemsPerMilestone", 5)
                .Set("milestoneStageInterval", 5)
                .Set("waveTransitionDelaySec", 0.6f)
                .Set("minDamageRatio", 0.15f)
                .Set("criticalChance", 0.05f)
                .Set("criticalDamageMultiplier", 2f)
                .Set("healHeroesOnStageAdvance", true)
                .Set("reviveHeroesEachWave", false)
                .Set("enemyTargeting", (int)EnemyTargetingMode.FrontMost)
                .Set("combatTickIntervalSec", 0.05f)
                .Set("stageRollbackOnDefeat", 1)
                .Set("defeatPauseSeconds", 0.75f)
                .Set("scaleEnemyDefenseWithStage", false)
                .Set("logCombatToConsole", true)
                .Set("combatPaceMultiplier", 1.6f)
                .Set("upgradeCostGrowth", 1.07f)
                .Set("upgradeStatGainPerLevel", 0.1f)
                .Set("prestigeStageDivisor", 10f)
                .Set("prestigeExponent", 1.5f)
                .Set("minStageToAscend", 10)
                .Set("resetHeroLevelsOnAscension", true)
                .Set("offlineCapSeconds", 28800f)
                .Set("offlineEfficiency", 0.7f)
                .Set("minOfflineSecondsForPopup", 30f)
                .Set("offlineCapExtensionSeconds", 3600f)
                .Set("offlineCapExtensionGemCost", 50d)
                .Set("offlineCapExtensionMaxSeconds", 10800f)
                .Set("instantIncomeSeconds", 3600f)
                .Set("instantIncomeGemCost", 30d)
                .Set("offlineMaxEquivalentSeconds", 7200f)
                .Set("offlineEstimatedSecondsPerKill", 3.6f)
                .Set("startingGold", 0d)
                .Set("startingGems", 0d)
                .Set("goldPerSecondSampleWindowSec", 60f)
                .Set("adGoldBoostMultiplier", 2d)
                .Set("adGoldBoostDurationSec", 1800f)
                .Set("autosaveIntervalSec", 15f)
                .Apply();

                // B7 S2: the daily streak calendar (reviewed values, day 1..7 then plateau on day 7).
                new Editable(balance)
                    .SetIntArray("dailyStreakGems", new[] { 5, 8, 10, 12, 15, 18, 25 })
                    .Set("dailyStreakDay7Boost", true)
                    .Apply();

                // B7 S3: the rewarded-ad placements (caps + cooldowns).
                var placementSo = new SerializedObject(balance);
                SerializedProperty placements = placementSo.FindProperty("adPlacements");
                if (placements != null && placements.isArray)
                {
                    placements.arraySize = 2;

                    SerializedProperty boost = placements.GetArrayElementAtIndex(0);
                    boost.FindPropertyRelative("PlacementId").intValue = (int)AdPlacementId.GoldBoost;
                    boost.FindPropertyRelative("DailyCap").intValue = 5;
                    boost.FindPropertyRelative("CooldownSec").floatValue = 300f;

                    SerializedProperty doubleOffline = placements.GetArrayElementAtIndex(1);
                    doubleOffline.FindPropertyRelative("PlacementId").intValue = (int)AdPlacementId.DoubleOffline;
                    doubleOffline.FindPropertyRelative("DailyCap").intValue = 3;
                    doubleOffline.FindPropertyRelative("CooldownSec").floatValue = 30f;

                    placementSo.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(balance);
                }
        }

        // ------------------------------------------------------------------
        // Content definitions
        // ------------------------------------------------------------------
        /// <summary>
        /// Currency rows (Step 9b). Gold/gems/tokens are live; the rest are labelled placeholders so later
        /// steps only flip a flag rather than inventing a new concept.
        /// </summary>
        private static void CreateCurrencies()
        {
            string folder = DataRoot + "/Currencies";
            EnsureFolder(folder);

            CreateCurrency(folder, "Currency_Gold", "gold", "Gold", false, true, "ui_icon_gold",
                "every kill", "hero stat levels");
            CreateCurrency(folder, "Currency_Gems", "gems", "Gems", true, true, "ui_icon_gem",
                "boss kills, milestones", "offline income cap, instant income, expeditions (Step 19)");
            CreateCurrency(folder, "Currency_Tokens", "tokens", "Prestige Tokens", false, true, "ui_icon_token",
                "ascension", "permanent prestige tracks");
            CreateCurrency(folder, "Currency_Shards", "shards", "Hero Shards", false, false, "ui_icon_shard",
                "duplicates, boss chests, bounties (Step 17)", "star-ups and hero unlocks");
            CreateCurrency(folder, "Currency_Materials", "materials", "Materials", false, false, "ui_icon_material",
                "zone-tier stage drops (Step 18)", "relic upgrades");
            CreateCurrency(folder, "Currency_Essence", "essence", "Essence", false, false, "ui_icon_essence",
                "transcendence (Step 18)", "L2 permanent tracks");
            CreateCurrency(folder, "Currency_Scrolls", "scrolls", "Ability Scrolls", false, false, "ui_icon_scroll",
                "bosses and expeditions (Step 13)", "ability levels");
        }

        /// <summary>
        /// Step 10a: the party board. 2 rows x 3 columns, the first three slots open from the start so the MVP's
        /// fixed lanes are reproduced exactly; later slots open on stage milestones (zone-based unlocks arrive
        /// with ZoneData in Step 15).
        /// </summary>
        private static void CreateFormation()
        {
            string folder = DataRoot + "/Config";
            EnsureFolder(folder);

            FormationData formation = CreateOrLoad<FormationData>(folder + "/Formation_Default.asset");

            new Editable(formation)
                .Set("frontSlots", 3)
                .Set("backSlots", 3)
                .Set("backRowTargetWeight", 0.35f)
                .Apply();
        }

        private static void CreateCurrency(string folder, string fileName, string id, string displayName,
            bool isPremium, bool isImplemented, string spriteName, string earnSource, string sink)
        {
            CurrencyDef currency = CreateOrLoad<CurrencyDef>(folder + "/" + fileName + ".asset");

            new Editable(currency)
                .Set("currencyID", id)
                .Set("displayName", displayName)
                .Set("isPremium", isPremium)
                .Set("isImplemented", isImplemented)
                .Set("earnSource", earnSource)
                .Set("sinkDescription", sink)
                .SetSprite("icon", spriteName)
                .Apply();
        }

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
                .SetEnum("role", IdleRPG.Data.HeroRole.Tank)
                .SetSpriteRef("heroIcon", LoadFrame(SoldierIdleSheet, "Soldier_Idle_0"))
                .SetColor("placeholderTint", Color.white)
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
                .SetEnum("role", IdleRPG.Data.HeroRole.Damage)
                .SetSpriteRef("heroIcon", LoadFrame(SoldierIdleSheet, "Soldier_Idle_3"))
                .SetColor("placeholderTint", new Color(0.62f, 0.76f, 1f, 1f))
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
                // Support, NOT Damage: the Tank/Damage/Support trinity maps 1:1 to Knight/Archer/Mage, and this
                // generator runs on every MVP scene rebuild - a Damage Mage here silently re-breaks Support item
                // drops (they would have no hero to equip to).
                .SetEnum("role", IdleRPG.Data.HeroRole.Support)
                .SetSpriteRef("heroIcon", LoadFrame(SoldierAttack01Sheet, "Soldier_Attack01_2"))
                .SetColor("placeholderTint", new Color(0.78f, 0.62f, 1f, 1f))
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
            enemies.Add(CreateEnemy("Enemy_Goblin", "Goblin", 130f, 12f, 3f, 18f, 1.8f, false, 1f, 1f, Color.white, LoadFrame(GoblinIdleSheet, "Goblin_Idle_anim_0")));

            // Stage-1 boss: 100 x4 = 400 HP (about a third of the stage) and ~150 gold.
            enemies.Add(CreateEnemy("Boss_Ogre", "Ogre Chieftain", 100f, 20f, 5f, 25f, 2.5f, true, 4f, 6f, Color.white, LoadFrame(OrcIdleSheet, "Orc_Idle_0")));

            return enemies;
        }

        private static EnemyData CreateEnemy(string fileName, string displayName, float health, float attack,
            float defense, float gold, float interval, bool isBoss, float bossHealthMultiplier,
            float bossGoldMultiplier, Color tint, Sprite spriteOverride = null)
        {
            EnemyData enemy = CreateOrLoad<EnemyData>(EnemyFolder + "/" + fileName + ".asset");
            string spriteName = fileName.ToLowerInvariant();

            Editable editable = new Editable(enemy);
            editable
                .Set("enemyName", displayName)
                .Set("baseHealth", health)
                .Set("baseAttack", attack)
                .Set("baseDefense", defense)
                .Set("baseGoldDrop", gold)
                .Set("attackIntervalSec", interval)
                .Set("isBoss", isBoss)
                .Set("bossHealthMultiplier", bossHealthMultiplier)
                .Set("bossGoldMultiplier", bossGoldMultiplier);

            if (spriteOverride != null)
            {
                editable.SetSpriteRef("enemySprite", spriteOverride);
            }
            else
            {
                editable.SetSprite("enemySprite", spriteName);
            }

            editable
                .SetColor("placeholderTint", tint)
                .Apply();

            return enemy;
        }

        /// <summary>Finds one frame sub-sprite by name inside an imported sheet (ArtPresetApplier sliced frames).</summary>
        private static Sprite LoadFrame(string sheetPath, string frameName)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(sheetPath))
            {
                if (asset is Sprite sprite && sprite.name == frameName)
                {
                    return sprite;
                }
            }

            Debug.LogWarning($"[DataAssetGenerator] Frame '{frameName}' not found in '{sheetPath}'.");
            return null;
        }

        private static void CreateStatUpgrades()
        {
            // B3d: the per-level effect compounds. Additive power is a straight line against an exponential
            // content curve (enemy HP x1.15 per stage), so the frontier hits a ceiling no cost tuning can move.
            //
            // The gain is derived, not chosen: FormulaUtility.CompoundingGainFor(contentGrowth, 1.07, 1.12).
            //   ATK / HP race enemy HP  (1.15) -> 0.087 exact, data uses 0.09 (about +0.5%/stage of margin)
            //   DEF races enemy ATK     (1.08) -> 0.047 exact, data uses 0.05 (same margin; damage is ATK - DEF,
            //                                   so defence only has to keep up with 1.08^stage, not 1.15^stage)
            // Recompute both if BalanceConfig health/attack/gold growth or the 1.07 cost curve ever change.
            CreateStatUpgrade("StatUpgrade_ATK", HeroStatType.Attack, 10d, 0.09f, StatEffectMode.Multiplicative, "ATK x1.09 per level (compounds).");
            CreateStatUpgrade("StatUpgrade_HP", HeroStatType.Health, 12d, 0.09f, StatEffectMode.Multiplicative, "HP x1.09 per level (compounds).");
            CreateStatUpgrade("StatUpgrade_DEF", HeroStatType.Defense, 15d, 0.05f, StatEffectMode.Multiplicative, "DEF x1.05 per level (compounds).");

            // Crit stats are FLAT points, not fraction-of-base: +0.5% chance and +0.05x damage per level.
            // The resolver caps them (75% / x6) whatever the level says.
            CreateStatUpgrade("StatUpgrade_CritRate", HeroStatType.CritRate, 20d, 0.005f, StatEffectMode.FlatAdditive,
                "+0.5% critical chance per level. Capped at 75%.");
            CreateStatUpgrade("StatUpgrade_CritDamage", HeroStatType.CritDamage, 25d, 0.05f, StatEffectMode.FlatAdditive,
                "+0.05 crit damage multiplier per level. Capped at x6.");
        }

        private static void CreateStatUpgrade(string fileName, HeroStatType statType, double baseCost,
            float gainPerLevelFraction, StatEffectMode effectMode, string description)
        {
            StatUpgradeData upgrade = CreateOrLoad<StatUpgradeData>(ConfigFolder + "/" + fileName + ".asset");
            new Editable(upgrade)
                .Set("statType", (int)statType)
                .Set("displayName", statType.ToDisplayName())
                .Set("description", description)
                .Set("baseCost", baseCost)
                .Set("costGrowthMultiplier", 1.07f)
                .Set("effectMode", (int)effectMode)
                .Set("statGainPerLevelFraction", gainPerLevelFraction)
                .Set("maxLevel", 0)
                .Apply();
        }

        private static void CreatePrestigeUpgrades()
        {
            // +20% per level, 25 levels = +500% per track. Cost growth 1.4 -> ~600 tokens to max one tree, and the
            // first ascension (3 tokens at stage 25) is worth +60% damage: felt, but nowhere near free.
            CreatePrestigeUpgrade("Prestige_Gold", "Gold Mastery", PrestigeEffectType.GoldPercent, "+20% Gold per level.");
            CreatePrestigeUpgrade("Prestige_Damage", "War Mastery", PrestigeEffectType.DamagePercent, "+20% Damage per level.");
            CreatePrestigeUpgrade("Prestige_Health", "Vitality Mastery", PrestigeEffectType.HealthPercent, "+20% HP per level.");
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
                .Set("costGrowth", 1.4f)
                .Set("maxLevel", 25)
                .Set("effectPerLevel", 0.20f)
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
    }
}
