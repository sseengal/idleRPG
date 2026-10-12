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
    public static partial class ContentGenerator
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

            // Any card that has no picture yet gets one (never overwrites existing art). Kept outside the
            // asset-editing batch because each picture is its own import.
            int drawn = PlaceholderSpriteGenerator.GenerateCardUnits();

            Debug.Log($"[ContentGenerator] Applied specs (heroes {heroes.heroes.Count}, enemies {enemies.enemies.Count}, " +
                      $"stat upgrades {upgrades.statUpgrades.Count}, prestige {upgrades.prestigeUpgrades.Count}, " +
                      $"new card pictures {drawn}).");
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
                    art = ArtCharacterName(SoField.Reference(hero, "artSet")),
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
            // The numbers and the band table are OWNED BY THE SPEC, not by the assets: the generator derives them,
            // so writing the derived values back would turn every derived row into a hand-tuned one and the band
            // table would stop reaching it. Export therefore carries the card's own numbers (and the whole band
            // table) forward, and writes only what the assets really own - identity plus what wiring the assets add.
            EnemySpecFile previous = ContentSpecIO.Load<EnemySpecFile>(ContentSpecIO.EnemiesPath);
            file.bands = previous.bands ?? new List<EnemyBandSpec>();

            var cardById = new Dictionary<string, EnemySpec>();
            for (int i = 0; i < previous.enemies.Count; i++)
            {
                cardById[previous.enemies[i].id] = previous.enemies[i];
            }

            List<EnemyData> assets = ContentSpecIO.LoadAll<EnemyData>(ContentSpecIO.EnemyFolder);

            for (int i = 0; i < assets.Count; i++)
            {
                EnemyData enemy = assets[i];
                cardById.TryGetValue(SoField.Text(enemy, "enemyID", enemy.name), out EnemySpec card);

                file.enemies.Add(new EnemySpec
                {
                    id = SoField.Text(enemy, "enemyID", enemy.name),
                    asset = enemy.name,
                    displayName = SoField.Text(enemy, "enemyName", enemy.name),
                    sprite = SoField.AssetName(SoField.Reference(enemy, "enemySprite")),
                    art = ArtCharacterName(SoField.Reference(enemy, "artSet")),
                    firstStage = SoField.Int(enemy, "firstStage", 1),
                    lastStage = SoField.Int(enemy, "lastStage", 0),

                    // Numbers and archetype ride the card forward (see the note at the top of this method).
                    archetype = card != null ? card.archetype : string.Empty,
                    baseHealth = card != null ? card.baseHealth : 0f,
                    baseAttack = card != null ? card.baseAttack : 0f,
                    baseDefense = card != null ? card.baseDefense : 0f,
                    baseGoldDrop = card != null ? card.baseGoldDrop : 0f,
                    attackIntervalSec = card != null ? card.attackIntervalSec : 0f,

                    stageAlternates = AlternatesOf(enemy),
                    isBoss = SoField.Bool(enemy, "isBoss"),
                    bossHealthMultiplier = SoField.Float(enemy, "bossHealthMultiplier", 1f),
                    bossGoldMultiplier = SoField.Float(enemy, "bossGoldMultiplier", 1f),
                    bossSizeScale = SoField.Float(enemy, "bossSizeScale", 1.35f),
                    tint = ContentSpecIO.ToHex(SoField.Color(enemy, "placeholderTint", Color.white)),
                    targetRule = ((EnemyTargetingMode)SoField.Int(enemy, "targetRule", (int)EnemyTargetingMode.Inherit))
                        .ToString().ToLowerInvariant()
                });
            }

            file.enemies.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            ContentSpecIO.Save(ContentSpecIO.EnemiesPath, file);
        }

        /// <summary>
        /// The alternate art names wired on an enemy asset, in order. This is S6b: the generator writes the
        /// alternates onto the asset and Export reads them back, so a regeneration round-trips them instead of
        /// quietly dropping the skins (the Goblin's Demon / Blood Monster sheets).
        /// </summary>
        private static List<string> AlternatesOf(EnemyData enemy)
        {
            var names = new List<string>();
            int count = SoField.Count(enemy, "stageAlternates");

            for (int i = 0; i < count; i++)
            {
                string name = ArtCharacterName(SoField.ElementAt(enemy, "stageAlternates", i));

                if (!string.IsNullOrEmpty(name))
                {
                    names.Add(name);
                }
            }

            return names;
        }

        /// <summary>
        /// "Knight_Art" -> "Knight": the character folder name behind a resolved art set, so an Export followed by a
        /// Generate keeps the art wiring instead of quietly dropping it.
        /// </summary>
        private static string ArtCharacterName(Object reference)
        {
            string assetName = SoField.AssetName(reference);

            if (string.IsNullOrEmpty(assetName))
            {
                return "";
            }

            const string suffix = "_Art";
            return assetName.EndsWith(suffix, System.StringComparison.Ordinal)
                ? assetName.Substring(0, assetName.Length - suffix.Length)
                : assetName;
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
                    costCurrency = ((IdleRPG.Economy.CurrencyType)SoField.Int(upgrade, "costCurrency",
                        (int)IdleRPG.Economy.CurrencyType.PrestigeTokens)).ToString().ToLowerInvariant(),
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
    }
}
