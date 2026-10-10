using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using IdleRPG.Combat;
using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Services;

namespace IdleRPG.EditorTools.Content
{
    /// <summary>
    /// Spec <-> asset agreement and asset references: hand-edited assets and orphan files are caught here.
    /// </summary>
    public static partial class ContentValidator
    {

        // ------------------------------------------------------------------
        // Spec <-> asset agreement (catches hand-edited assets and orphan files)
        // ------------------------------------------------------------------
        private static void CheckAssetsAgainstSpecs(HeroSpecFile heroes, EnemySpecFile enemies)
        {
            HashSet<string> specHeroIds = new HashSet<string>();
            for (int i = 0; i < heroes.heroes.Count; i++)
            {
                specHeroIds.Add(heroes.heroes[i].id);
            }

            List<HeroData> heroAssets = ContentSpecIO.LoadAll<HeroData>(ContentSpecIO.HeroFolder);
            for (int i = 0; i < heroAssets.Count; i++)
            {
                string id = SoField.Text(heroAssets[i], "heroID", heroAssets[i].name);

                if (!specHeroIds.Contains(id))
                {
                    Add(Severity.Warning, "assets",
                        $"Hero asset '{heroAssets[i].name}' (id '{id}') has no spec entry; Generate would ignore it.");
                }
            }

            HashSet<string> specEnemyIds = new HashSet<string>();
            for (int i = 0; i < enemies.enemies.Count; i++)
            {
                specEnemyIds.Add(enemies.enemies[i].id);
            }

            List<EnemyData> enemyAssets = ContentSpecIO.LoadAll<EnemyData>(ContentSpecIO.EnemyFolder);
            for (int i = 0; i < enemyAssets.Count; i++)
            {
                string id = SoField.Text(enemyAssets[i], "enemyID", enemyAssets[i].name);

                if (!specEnemyIds.Contains(id))
                {
                    Add(Severity.Warning, "assets",
                        $"Enemy asset '{enemyAssets[i].name}' (id '{id}') has no spec entry; Generate would ignore it.");
                }
            }

            Add(Severity.Info, "assets", $"{heroAssets.Count} hero + {enemyAssets.Count} enemy assets scanned.");
        }

        // ------------------------------------------------------------------
        // Asset references (B4a): every pool element must resolve
        // ------------------------------------------------------------------
        /// <summary>
        /// Catches the "recipe deleted, asset left pointing at it" bug.
        ///
        /// ELI5: runtime code is polite - if a wave's list holds a hole, it quietly spawns fewer monsters instead of
        /// crashing, so the only symptom is a smaller number (this is how a deleted enemy once turned 22 kills into 18).
        /// The inspector is not polite: a hole here is an error with a name and an index.
        /// </summary>
        private static void CheckAssetReferences(EnemySpecFile enemies, HeroSpecFile heroes)
        {
            HashSet<string> specEnemyIds = new HashSet<string>();
            for (int i = 0; i < enemies.enemies.Count; i++)
            {
                specEnemyIds.Add(enemies.enemies[i].id);
            }

            HashSet<string> specHeroIds = new HashSet<string>();
            for (int i = 0; i < heroes.heroes.Count; i++)
            {
                specHeroIds.Add(heroes.heroes[i].id);
            }

            WaveConfig waves = BalanceLabMenu.Load<WaveConfig>("WaveConfig");
            if (waves == null)
            {
                Add(Severity.Error, "refs", "No WaveConfig asset; run Tools > Idle RPG > Generate Data Assets.");
            }
            else
            {
                CheckEnemyPool("WaveConfig.normalEnemies", waves, "normalEnemies", specEnemyIds, true);
                CheckEnemyPool("WaveConfig.bossEnemies", waves, "bossEnemies", specEnemyIds, true);
            }

            PartyConfig party = BalanceLabMenu.Load<PartyConfig>("PartyConfig");
            if (party == null)
            {
                Add(Severity.Error, "refs", "No PartyConfig asset; run Tools > Idle RPG > Generate Data Assets.");
            }
            else
            {
                int slots = SoField.Count(party, "heroes");
                for (int i = 0; i < slots; i++)
                {
                    Object element = SoField.ElementAt(party, "heroes", i);
                    if (element == null)
                    {
                        Add(Severity.Error, "refs",
                            $"PartyConfig.heroes[{i}] is empty - a deleted hero asset leaves a hole; re-run Generate.");
                        continue;
                    }

                    HeroData hero = element as HeroData;
                    string id = hero != null ? SoField.Text(hero, "heroID", hero.name) : element.name;

                    if (!specHeroIds.Contains(id))
                    {
                        Add(Severity.Error, "refs",
                            $"PartyConfig.heroes[{i}] references '{id}', which is not in heroes.json (spec and assets out of sync).");
                    }
                }
            }
        }

        /// <summary>One enemy pool: no holes, every id known, no duplicates.</summary>
        private static void CheckEnemyPool(string label, Object owner, string field, HashSet<string> knownIds, bool requireNonEmpty)
        {
            int count = SoField.Count(owner, field);
            if (count == 0)
            {
                Add(requireNonEmpty ? Severity.Error : Severity.Warning, "refs", $"{label} is empty.");
                return;
            }

            HashSet<string> seen = new HashSet<string>();

            for (int i = 0; i < count; i++)
            {
                Object element = SoField.ElementAt(owner, field, i);
                if (element == null)
                {
                    Add(Severity.Error, "refs",
                        $"{label}[{i}] is empty - a deleted enemy asset leaves a hole here (waves then spawn fewer enemies). " +
                        "Re-run Tools > Idle RPG > Content > Generate Assets From Specs.");
                    continue;
                }

                EnemyData enemy = element as EnemyData;
                string id = enemy != null ? enemy.EnemyID : element.name;

                if (string.IsNullOrEmpty(id))
                {
                    Add(Severity.Error, "refs", $"{label}[{i}] ('{element.name}') has no enemy id.");
                    continue;
                }

                if (!knownIds.Contains(id))
                {
                    Add(Severity.Error, "refs", $"{label}[{i}] references '{id}', which is not in enemies.json.");
                }

                if (!seen.Add(id))
                {
                    Add(Severity.Warning, "refs", $"{label} lists '{id}' more than once (weights that enemy twice).");
                }
            }
        }

        /// <summary>
        /// Every stat track owns one stat, buys something, the spec and asset agree, and the prestige tree exists.
        ///
        /// ELI5: the recipe card says what an upgrade should be; the asset is what the game uses. This check makes sure
        /// neither drifted - a hand-edited asset that disagrees with its card is caught here, not discovered by a player.
        /// </summary>
        private static void CheckUpgradeAssets()
        {
            UpgradeSpecFile spec = ContentSpecIO.Load<UpgradeSpecFile>(ContentSpecIO.TracksPath);
            Dictionary<string, StatUpgradeSpec> specByAsset = new Dictionary<string, StatUpgradeSpec>();

            if (spec != null)
            {
                for (int i = 0; i < spec.statUpgrades.Count; i++)
                {
                    StatUpgradeSpec entry = spec.statUpgrades[i];
                    string assetName = string.IsNullOrEmpty(entry.asset) ? entry.id : entry.asset;
                    specByAsset[assetName] = entry;
                }
            }

            List<StatUpgradeData> statAssets = ContentSpecIO.LoadAll<StatUpgradeData>(ContentSpecIO.ConfigFolder);
            HashSet<string> statTypes = new HashSet<string>();

            for (int i = 0; i < statAssets.Count; i++)
            {
                StatUpgradeData upgrade = statAssets[i];
                string statType = upgrade.StatType.ToString().ToLowerInvariant();

                if (!statTypes.Add(statType))
                {
                    Add(Severity.Error, "refs",
                        $"Two stat upgrade assets both cover '{statType}' ({upgrade.name}); the resolver would use one and ignore the other.");
                }

                if (upgrade.StatGainPerLevelFraction <= 0f)
                {
                    Add(Severity.Error, "refs", $"{upgrade.name}: statGainPerLevelFraction is 0, so the track buys nothing.");
                }

                if (!specByAsset.TryGetValue(upgrade.name, out StatUpgradeSpec entry))
                {
                    Add(Severity.Error, "refs",
                        $"Stat upgrade asset '{upgrade.name}' has no entry in upgrades.json; Generate Assets From Specs would rewrite it silently.");
                    continue;
                }

                StatEffectMode specMode = ParseSpecEffectMode(entry.effectMode);
                StatEffectMode assetMode = upgrade.EffectMode;

                if (specMode != assetMode)
                {
                    Add(Severity.Error, "refs",
                        $"{upgrade.name}: effectMode is {assetMode} in the asset but " +
                        $"\"{(string.IsNullOrEmpty(entry.effectMode) ? "additive" : entry.effectMode)}\" in upgrades.json. " +
                        "Re-run Export/Generate so the card and the asset tell the same story.");
                }

                if (System.Math.Abs(upgrade.StatGainPerLevelFraction - entry.statGainPerLevelFraction) > 0.001f)
                {
                    Add(Severity.Warning, "refs",
                        $"{upgrade.name}: gain is {upgrade.StatGainPerLevelFraction:0.###} in the asset but {entry.statGainPerLevelFraction:0.###} in upgrades.json.");
                }
            }

            List<PrestigeUpgradeData> prestigeAssets = ContentSpecIO.LoadAll<PrestigeUpgradeData>(ContentSpecIO.ConfigFolder);
            if (prestigeAssets.Count == 0)
            {
                Add(Severity.Warning, "refs",
                    "No prestige upgrade assets found; ascension would pay tokens with nothing to spend them on.");
            }

            List<AutomationDef> automationAssets = ContentSpecIO.LoadAll<AutomationDef>(ContentSpecIO.ConfigFolder);
            HashSet<string> autoIds = new HashSet<string>();
            bool autoSpecEmpty = spec == null || spec.automationUpgrades.Count == 0;

            for (int i = 0; i < automationAssets.Count; i++)
            {
                AutomationDef card = automationAssets[i];
                string id = card.AutomationID;

                if (!autoIds.Add(id))
                {
                    Add(Severity.Error, "refs", $"Duplicate automation card id '{id}'.");
                }

                if (card.BaseCostTokens <= 0d)
                {
                    Add(Severity.Error, "refs", $"{card.name}: baseCostTokens must be > 0.");
                }

                if (!autoSpecEmpty && !SpecHasAutomation(spec, card.name))
                {
                    Add(Severity.Error, "refs",
                        $"Automation card '{card.name}' has no entry in tracks.json; Generate would rebuild it silently.");
                }
            }

            if (autoSpecEmpty && automationAssets.Count > 0)
            {
                Add(Severity.Warning, "refs",
                    "Automation cards exist but tracks.json lists none; re-run Export Specs From Assets.");
            }
        }

        private static bool SpecHasAutomation(UpgradeSpecFile spec, string assetName)
        {
            for (int i = 0; i < spec.automationUpgrades.Count; i++)
            {
                AutomationSpec entry = spec.automationUpgrades[i];
                if (entry == null)
                {
                    continue;
                }

                string candidate = string.IsNullOrEmpty(entry.asset) ? entry.id : entry.asset;
                if (candidate == assetName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Spec spelling -> effect mode. Mirrors ContentGenerator.ParseEffectMode (single source of truth).</summary>
        private static StatEffectMode ParseSpecEffectMode(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "multiplicative":
                case "compounding":
                    return StatEffectMode.Multiplicative;

                case "flat":
                case "flatadditive":
                    return StatEffectMode.FlatAdditive;

                default:
                    return StatEffectMode.AdditiveBase;
            }
        }
    }
}
