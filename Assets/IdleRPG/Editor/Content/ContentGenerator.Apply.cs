using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using IdleRPG.Data;
using IdleRPG.Progression;

namespace IdleRPG.EditorTools.Content
{
    /// <summary>
    /// The Apply (specs -> assets) cook: cards back into the game assets.
    /// </summary>
    public static partial class ContentGenerator
    {

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
                    .Set("costCurrency", (int)ParseCurrency(entry.costCurrency))
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
    }
}
