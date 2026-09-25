using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using IdleRPG.Data;
using IdleRPG.Progression;

namespace IdleRPG.EditorTools.Content
{
    /// <summary>
    /// The content pipeline: assets &lt;-&gt; JSON specs.
    ///
    /// ELI5: `Export` copies the current game values onto recipe cards; `Generate` cooks the cards back into
    /// the game. Export first (so the cards tell the truth), edit cards, then generate. Export -> generate ->
    /// export must produce identical files - that is how we prove nothing drifts.
    ///
    /// Menu: Tools > Idle RPG > Content > ...
    /// </summary>
    public static class ContentGenerator
    {
        [MenuItem("Tools/Idle RPG/Content/Export Specs From Assets", priority = 100)]
        public static void ExportSpecs()
        {
            ExportHeroes();
            ExportEnemies();
            ExportParty();
            ExportWaves();
            ExportUpgrades();

            AssetDatabase.Refresh();
            Debug.Log("[ContentGenerator] Specs exported to " + ContentSpecIO.SpecRoot);
        }

        [MenuItem("Tools/Idle RPG/Content/Generate Assets From Specs", priority = 101)]
        public static void GenerateAssets()
        {
            HeroSpecFile heroes = ContentSpecIO.Load<HeroSpecFile>(ContentSpecIO.HeroesPath);
            EnemySpecFile enemies = ContentSpecIO.Load<EnemySpecFile>(ContentSpecIO.EnemiesPath);
            PartySpecFile party = ContentSpecIO.Load<PartySpecFile>(ContentSpecIO.PartyPath);
            WaveSpecFile waves = ContentSpecIO.Load<WaveSpecFile>(ContentSpecIO.WavesPath);
            UpgradeSpecFile upgrades = ContentSpecIO.Load<UpgradeSpecFile>(ContentSpecIO.TracksPath);

            AssetDatabase.StartAssetEditing();
            try
            {
                Dictionary<string, HeroData> heroById = ApplyHeroes(heroes);
                Dictionary<string, EnemyData> enemyById = ApplyEnemies(enemies);
                ApplyParty(party, heroById);
                ApplyWaves(waves, enemyById);
                ApplyUpgrades(upgrades);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[ContentGenerator] Applied specs (heroes {heroes.heroes.Count}, enemies {enemies.enemies.Count}, " +
                      $"stat upgrades {upgrades.statUpgrades.Count}, prestige {upgrades.prestigeUpgrades.Count}).");
        }

        // ------------------------------------------------------------------
        // Export (assets -> specs)
        // ------------------------------------------------------------------
        private static void ExportHeroes()
        {
            HeroSpecFile file = new HeroSpecFile();
            List<HeroData> assets = ContentSpecIO.LoadAll<HeroData>(ContentSpecIO.HeroFolder);

            for (int i = 0; i < assets.Count; i++)
            {
                HeroData hero = assets[i];

                file.heroes.Add(new HeroSpec
                {
                    id = SoField.Text(hero, "heroID", hero.name),
                    asset = hero.name,
                    displayName = SoField.Text(hero, "heroName", hero.name),
                    icon = SoField.AssetName(SoField.Reference(hero, "heroIcon")),
                    baseHealth = SoField.Float(hero, "baseHealth", 100f),
                    baseAttack = SoField.Float(hero, "baseAttack", 10f),
                    baseDefense = SoField.Float(hero, "baseDefense", 5f),
                    attackIntervalSec = SoField.Float(hero, "attackIntervalSec", 1.5f),
                    tint = ContentSpecIO.ToHex(SoField.Color(hero, "placeholderTint", Color.white))
                });
            }

            file.heroes.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            ContentSpecIO.Save(ContentSpecIO.HeroesPath, file);
        }

        private static void ExportEnemies()
        {
            EnemySpecFile file = new EnemySpecFile();
            List<EnemyData> assets = ContentSpecIO.LoadAll<EnemyData>(ContentSpecIO.EnemyFolder);

            for (int i = 0; i < assets.Count; i++)
            {
                EnemyData enemy = assets[i];

                file.enemies.Add(new EnemySpec
                {
                    id = SoField.Text(enemy, "enemyID", enemy.name),
                    asset = enemy.name,
                    displayName = SoField.Text(enemy, "enemyName", enemy.name),
                    sprite = SoField.AssetName(SoField.Reference(enemy, "enemySprite")),
                    baseHealth = SoField.Float(enemy, "baseHealth", 60f),
                    baseAttack = SoField.Float(enemy, "baseAttack", 9f),
                    baseDefense = SoField.Float(enemy, "baseDefense", 0f),
                    baseGoldDrop = SoField.Float(enemy, "baseGoldDrop", 8f),
                    attackIntervalSec = SoField.Float(enemy, "attackIntervalSec", 2f),
                    isBoss = SoField.Bool(enemy, "isBoss"),
                    bossHealthMultiplier = SoField.Float(enemy, "bossHealthMultiplier", 1f),
                    bossGoldMultiplier = SoField.Float(enemy, "bossGoldMultiplier", 1f),
                    tint = ContentSpecIO.ToHex(SoField.Color(enemy, "placeholderTint", Color.white)),
                    targetRule = ((EnemyTargetingMode)SoField.Int(enemy, "targetRule", (int)EnemyTargetingMode.Inherit))
                        .ToString().ToLowerInvariant()
                });
            }

            file.enemies.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            ContentSpecIO.Save(ContentSpecIO.EnemiesPath, file);
        }

        private static void ExportParty()
        {
            PartyConfig config = AssetDatabase.LoadAssetAtPath<PartyConfig>(ContentSpecIO.ConfigFolder + "/PartyConfig.asset");
            PartySpecFile file = new PartySpecFile();

            if (config != null)
            {
                int count = SoField.Count(config, "heroes");

                for (int i = 0; i < count; i++)
                {
                    HeroData hero = SoField.ElementAt(config, "heroes", i) as HeroData;
                    if (hero != null)
                    {
                        file.heroIds.Add(SoField.Text(hero, "heroID", hero.name));
                    }
                }
            }

            ContentSpecIO.Save(ContentSpecIO.PartyPath, file);
        }

        private static void ExportWaves()
        {
            WaveConfig config = AssetDatabase.LoadAssetAtPath<WaveConfig>(ContentSpecIO.ConfigFolder + "/WaveConfig.asset");
            WaveSpecFile file = new WaveSpecFile();

            if (config != null)
            {
                int normal = SoField.Count(config, "normalEnemies");
                for (int i = 0; i < normal; i++)
                {
                    EnemyData enemy = SoField.ElementAt(config, "normalEnemies", i) as EnemyData;
                    if (enemy != null)
                    {
                        file.normalEnemyIds.Add(SoField.Text(enemy, "enemyID", enemy.name));
                    }
                }

                int bosses = SoField.Count(config, "bossEnemies");
                for (int i = 0; i < bosses; i++)
                {
                    EnemyData enemy = SoField.ElementAt(config, "bossEnemies", i) as EnemyData;
                    if (enemy != null)
                    {
                        file.bossEnemyIds.Add(SoField.Text(enemy, "enemyID", enemy.name));
                    }
                }
            }

            ContentSpecIO.Save(ContentSpecIO.WavesPath, file);
        }

        private static void ExportUpgrades()
        {
            UpgradeSpecFile file = new UpgradeSpecFile();

            List<StatUpgradeData> stats = ContentSpecIO.LoadAll<StatUpgradeData>(ContentSpecIO.ConfigFolder);
            for (int i = 0; i < stats.Count; i++)
            {
                StatUpgradeData upgrade = stats[i];

                file.statUpgrades.Add(new StatUpgradeSpec
                {
                    id = upgrade.name,
                    asset = upgrade.name,
                    statType = ((HeroStatType)SoField.Int(upgrade, "statType")).ToString().ToLowerInvariant(),
                    displayName = SoField.Text(upgrade, "displayName", upgrade.name),
                    description = SoField.Text(upgrade, "description"),
                    baseCost = SoField.Double(upgrade, "baseCost", 10d),
                    costGrowth = SoField.Float(upgrade, "costGrowthMultiplier", 1.07f),
                    statGainPerLevelFraction = SoField.Float(upgrade, "statGainPerLevelFraction", 0.1f),
                    effectMode = ((StatEffectMode)SoField.Int(upgrade, "effectMode")).ToString().ToLowerInvariant(),
                    maxLevel = SoField.Int(upgrade, "maxLevel")
                });
            }

            List<PrestigeUpgradeData> prestige = ContentSpecIO.LoadAll<PrestigeUpgradeData>(ContentSpecIO.ConfigFolder);
            for (int i = 0; i < prestige.Count; i++)
            {
                PrestigeUpgradeData upgrade = prestige[i];

                file.prestigeUpgrades.Add(new PrestigeUpgradeSpec
                {
                    id = SoField.Text(upgrade, "upgradeID", upgrade.name),
                    asset = upgrade.name,
                    effectType = ((PrestigeEffectType)SoField.Int(upgrade, "effectType")).ToString().ToLowerInvariant(),
                    displayName = SoField.Text(upgrade, "displayName", upgrade.name),
                    description = SoField.Text(upgrade, "description"),
                    baseCostTokens = SoField.Double(upgrade, "baseCostTokens", 1d),
                    costGrowth = SoField.Float(upgrade, "costGrowth", 1.4f),
                    effectPerLevel = SoField.Float(upgrade, "effectPerLevel", 0.1f),
                    maxLevel = SoField.Int(upgrade, "maxLevel", 10)
                });
            }

            ExportAutomation(file);

            ContentSpecIO.Save(ContentSpecIO.TracksPath, file);
        }

        /// <summary>Exports the automation cards (B6) from their assets into tracks.json.</summary>
        private static void ExportAutomation(UpgradeSpecFile file)
        {
            List<AutomationDef> cards = ContentSpecIO.LoadAll<AutomationDef>(ContentSpecIO.ConfigFolder);

            for (int i = 0; i < cards.Count; i++)
            {
                AutomationDef card = cards[i];
                file.automationUpgrades.Add(new AutomationSpec
                {
                    id = card.name,
                    asset = card.name,
                    automationId = SoField.Text(card, "automationID", card.name),
                    displayName = SoField.Text(card, "displayName", card.name),
                    description = SoField.Text(card, "description"),
                    baseCostTokens = SoField.Double(card, "baseCostTokens", 4d),
                    minAscensions = SoField.Int(card, "minAscensions")
                });
            }
        }

        // ------------------------------------------------------------------
        // Apply (specs -> assets)
        // ------------------------------------------------------------------
        private static Dictionary<string, HeroData> ApplyHeroes(HeroSpecFile file)
        {
            Dictionary<string, HeroData> byId = new Dictionary<string, HeroData>();

            for (int i = 0; i < file.heroes.Count; i++)
            {
                HeroSpec spec = file.heroes[i];
                string assetName = string.IsNullOrEmpty(spec.asset) ? DeriveAssetName(spec.id) : spec.asset;
                HeroData hero = ContentSpecIO.CreateOrLoad<HeroData>(ContentSpecIO.HeroFolder + "/" + assetName + ".asset");

                new Editable(hero)
                    .Set("heroID", spec.id)
                    .Set("heroName", spec.displayName)
                    .Set("baseHealth", spec.baseHealth)
                    .Set("baseAttack", spec.baseAttack)
                    .Set("baseDefense", spec.baseDefense)
                    .Set("attackIntervalSec", spec.attackIntervalSec)
                    .SetColor("placeholderTint", ContentSpecIO.FromHex(spec.tint, Color.white))
                    .SetSprite("heroIcon", spec.icon)
                    .Apply();

                byId[spec.id] = hero;
            }

            return byId;
        }

        private static Dictionary<string, EnemyData> ApplyEnemies(EnemySpecFile file)
        {
            Dictionary<string, EnemyData> byId = new Dictionary<string, EnemyData>();

            for (int i = 0; i < file.enemies.Count; i++)
            {
                EnemySpec spec = file.enemies[i];
                string assetName = string.IsNullOrEmpty(spec.asset) ? DeriveAssetName(spec.id) : spec.asset;
                EnemyData enemy = ContentSpecIO.CreateOrLoad<EnemyData>(ContentSpecIO.EnemyFolder + "/" + assetName + ".asset");

                new Editable(enemy)
                    .Set("enemyID", spec.id)
                    .Set("enemyName", spec.displayName)
                    .Set("baseHealth", spec.baseHealth)
                    .Set("baseAttack", spec.baseAttack)
                    .Set("baseDefense", spec.baseDefense)
                    .Set("baseGoldDrop", spec.baseGoldDrop)
                    .Set("attackIntervalSec", spec.attackIntervalSec)
                    .Set("isBoss", spec.isBoss)
                    .Set("bossHealthMultiplier", spec.bossHealthMultiplier)
                    .Set("bossGoldMultiplier", spec.bossGoldMultiplier)
                    .Set("targetRule", (int)ParseTargetRule(spec.targetRule))
                    .SetColor("placeholderTint", ContentSpecIO.FromHex(spec.tint, Color.white))
                    .SetSprite("enemySprite", spec.sprite)
                    .Apply();

                byId[spec.id] = enemy;
            }

            return byId;
        }

        /// <summary>"hero_knight" -> "Hero_Knight" (fallback when a spec has no explicit asset name).</summary>
        private static string DeriveAssetName(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return "Unnamed";
            }

            string[] parts = id.Split('_');
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Length == 0)
                {
                    continue;
                }

                builder.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                {
                    builder.Append(part.Substring(1));
                }
            }

            return builder.ToString();
        }

        private static void ApplyParty(PartySpecFile spec, Dictionary<string, HeroData> heroById)
        {
            PartyConfig config = ContentSpecIO.CreateOrLoad<PartyConfig>(ContentSpecIO.ConfigFolder + "/PartyConfig.asset");
            List<HeroData> picked = new List<HeroData>();

            for (int i = 0; i < spec.heroIds.Count; i++)
            {
                if (heroById.TryGetValue(spec.heroIds[i], out HeroData hero))
                {
                    picked.Add(hero);
                }
                else
                {
                    Debug.LogWarning($"[ContentGenerator] Party references unknown hero '{spec.heroIds[i]}'.");
                }
            }

            if (picked.Count != spec.heroIds.Count)
            {
                Debug.LogError($"[ContentGenerator] Party has unresolved heroes ({picked.Count}/{spec.heroIds.Count}); refusing to write.");
                return;
            }

            new Editable(config).SetObjectList("heroes", picked).Apply();
        }

        private static void ApplyWaves(WaveSpecFile spec, Dictionary<string, EnemyData> enemyById)
        {
            WaveConfig config = ContentSpecIO.CreateOrLoad<WaveConfig>(ContentSpecIO.ConfigFolder + "/WaveConfig.asset");

            List<EnemyData> normal = ResolvePool(spec.normalEnemyIds, enemyById);
            List<EnemyData> bosses = ResolvePool(spec.bossEnemyIds, enemyById);

            if (normal == null || bosses == null || normal.Count == 0)
            {
                Debug.LogError("[ContentGenerator] Wave pools incomplete; leaving WaveConfig untouched.");
                return;
            }

            new Editable(config)
                .SetObjectList("normalEnemies", normal)
                .SetObjectList("bossEnemies", bosses)
                .Apply();
        }

        /// <summary>
        /// Resolves a pool of enemy ids. Returns null when ANY id is unknown: writing a partial (or empty)
        /// pool would silently delete content, so the caller skips the write instead.
        /// </summary>
        private static List<EnemyData> ResolvePool(List<string> ids, Dictionary<string, EnemyData> enemyById)
        {
            List<EnemyData> picked = new List<EnemyData>();
            bool allResolved = true;

            for (int i = 0; i < ids.Count; i++)
            {
                if (enemyById.TryGetValue(ids[i], out EnemyData enemy))
                {
                    picked.Add(enemy);
                    continue;
                }

                allResolved = false;
                Debug.LogError($"[ContentGenerator] Wave pool references unknown enemy '{ids[i]}'; refusing to write the pool.");
            }

            return allResolved ? picked : null;
        }

        private static void ApplyUpgrades(UpgradeSpecFile spec)
        {
            for (int i = 0; i < spec.statUpgrades.Count; i++)
            {
                StatUpgradeSpec entry = spec.statUpgrades[i];
                string assetName = string.IsNullOrEmpty(entry.asset) ? DeriveAssetName(entry.id) : entry.asset;
                StatUpgradeData upgrade = ContentSpecIO.CreateOrLoad<StatUpgradeData>(ContentSpecIO.ConfigFolder + "/" + assetName + ".asset");

                new Editable(upgrade)
                    .Set("statType", (int)ParseStatType(entry.statType))
                    .Set("displayName", entry.displayName)
                    .Set("description", entry.description)
                    .Set("baseCost", entry.baseCost)
                    .Set("costGrowthMultiplier", entry.costGrowth)
                    .Set("statGainPerLevelFraction", entry.statGainPerLevelFraction)
                    .Set("effectMode", (int)ParseEffectMode(entry.effectMode))
                    .Set("maxLevel", entry.maxLevel)
                    .Apply();
            }

            for (int i = 0; i < spec.prestigeUpgrades.Count; i++)
            {
                PrestigeUpgradeSpec entry = spec.prestigeUpgrades[i];
                string assetName = string.IsNullOrEmpty(entry.asset) ? DeriveAssetName(entry.id) : entry.asset;
                PrestigeUpgradeData upgrade = ContentSpecIO.CreateOrLoad<PrestigeUpgradeData>(ContentSpecIO.ConfigFolder + "/" + assetName + ".asset");

                new Editable(upgrade)
                    .Set("upgradeID", entry.id)
                    .Set("displayName", entry.displayName)
                    .Set("description", entry.description)
                    .Set("effectType", (int)ParseEffectType(entry.effectType))
                    .Set("baseCostTokens", entry.baseCostTokens)
                    .Set("costGrowth", entry.costGrowth)
                    .Set("effectPerLevel", entry.effectPerLevel)
                    .Set("maxLevel", entry.maxLevel)
                    .Apply();
            }

            for (int i = 0; i < spec.automationUpgrades.Count; i++)
            {
                AutomationSpec entry = spec.automationUpgrades[i];
                string assetName = string.IsNullOrEmpty(entry.asset) ? DeriveAssetName(entry.id) : entry.asset;
                AutomationDef card = ContentSpecIO.CreateOrLoad<AutomationDef>(ContentSpecIO.ConfigFolder + "/" + assetName + ".asset");

                string automationId = string.IsNullOrEmpty(entry.automationId) ? assetName : entry.automationId;

                new Editable(card)
                    .Set("automationID", automationId)
                    .Set("displayName", entry.displayName)
                    .Set("description", entry.description)
                    .Set("baseCostTokens", entry.baseCostTokens)
                    .Set("minAscensions", entry.minAscensions)
                    .Apply();
            }
        }

        /// <summary>"frontmost" | "lowesthealthpercent" | "random" | "backlinefirst" | anything else = inherit.</summary>
        private static EnemyTargetingMode ParseTargetRule(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "frontmost":
                case "front":
                    return EnemyTargetingMode.FrontMost;
                case "lowesthealthpercent":
                    return EnemyTargetingMode.LowestHealthPercent;
                case "random":
                    return EnemyTargetingMode.Random;
                case "backlinefirst":
                    return EnemyTargetingMode.BacklineFirst;
                default:
                    return EnemyTargetingMode.Inherit;
            }
        }

        private static HeroStatType ParseStatType(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "health":
                case "healthpercent":
                    return HeroStatType.Health;
                case "defense":
                case "defence":
                case "defensepercent":
                    return HeroStatType.Defense;
                default:
                    return HeroStatType.Attack;
            }
        }

        /// <summary>"multiplicative" | "compounding" | anything else = additive (the shipped model).</summary>
        private static StatEffectMode ParseEffectMode(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "multiplicative":
                case "compounding":
                    return StatEffectMode.Multiplicative;
                default:
                    return StatEffectMode.AdditiveBase;
            }
        }

        private static PrestigeEffectType ParseEffectType(string value)
        {
            // Accepts both "damage" and the exported "damagepercent" spelling; anything unknown is gold.
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "damage":
                case "damagepercent":
                    return PrestigeEffectType.DamagePercent;
                case "health":
                case "healthpercent":
                    return PrestigeEffectType.HealthPercent;
                default:
                    return PrestigeEffectType.GoldPercent;
            }
        }
    }
}
