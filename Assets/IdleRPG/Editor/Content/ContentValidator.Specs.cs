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
    /// Spec checks: heroes, enemies, party, waves and upgrades read from the recipe cards.
    /// </summary>
    public static partial class ContentValidator
    {

        // ------------------------------------------------------------------
        // Spec checks
        // ------------------------------------------------------------------
        private static void CheckHeroes(HeroSpecFile file)
        {
            if (file.heroes.Count == 0)
            {
                Add(Severity.Error, "heroes", "No heroes in the spec.");
                return;
            }

            HashSet<string> ids = new HashSet<string>();
            HashSet<string> assets = new HashSet<string>();

            for (int i = 0; i < file.heroes.Count; i++)
            {
                HeroSpec hero = file.heroes[i];
                RequireId(hero.id, "heroes", hero.asset, ids, assets);

                if (hero.baseHealth <= 0f)
                {
                    Add(Severity.Error, "heroes", $"{hero.id}: baseHealth must be > 0 (is {hero.baseHealth}).");
                }

                if (hero.baseAttack <= 0f)
                {
                    Add(Severity.Error, "heroes", $"{hero.id}: baseAttack must be > 0 (is {hero.baseAttack}).");
                }

                if (hero.attackIntervalSec < 0.1f)
                {
                    Add(Severity.Error, "heroes", $"{hero.id}: attackIntervalSec must be >= 0.1 (is {hero.attackIntervalSec}).");
                }

                if (ContentSpecIO.FromHex(hero.tint, Color.clear).a <= 0f)
                {
                    Add(Severity.Warning, "heroes", $"{hero.id}: tint '{hero.tint}' is not a valid colour.");
                }
            }

            Add(Severity.Info, "heroes", $"{file.heroes.Count} heroes checked.");
        }

        private static void CheckEnemies(EnemySpecFile file)
        {
            if (file.enemies.Count == 0)
            {
                Add(Severity.Error, "enemies", "No enemies in the spec.");
                return;
            }

            HashSet<string> ids = new HashSet<string>();
            HashSet<string> assets = new HashSet<string>();
            int bosses = 0;

            for (int i = 0; i < file.enemies.Count; i++)
            {
                EnemySpec enemy = file.enemies[i];
                RequireId(enemy.id, "enemies", enemy.asset, ids, assets);

                if (enemy.isBoss)
                {
                    bosses++;
                }

                if (enemy.baseHealth <= 0f)
                {
                    Add(Severity.Error, "enemies", $"{enemy.id}: baseHealth must be > 0 (is {enemy.baseHealth}).");
                }

                if (enemy.attackIntervalSec < 0.1f)
                {
                    Add(Severity.Error, "enemies", $"{enemy.id}: attackIntervalSec must be >= 0.1 (is {enemy.attackIntervalSec}).");
                }

                if (enemy.baseGoldDrop < 0f)
                {
                    Add(Severity.Error, "enemies", $"{enemy.id}: baseGoldDrop must be >= 0 (is {enemy.baseGoldDrop}).");
                }

                if (enemy.isBoss && (enemy.bossHealthMultiplier < 1f || enemy.bossGoldMultiplier < 1f))
                {
                    Add(Severity.Warning, "enemies", $"{enemy.id}: boss multipliers below 1 make a boss weaker than a normal enemy.");
                }

                // Step 11a: the two formation fields. A typo silently falls back to the default, so warn.
                if (!KnownTargetRules.Contains((enemy.targetRule ?? "").Trim().ToLowerInvariant()))
                {
                    Add(Severity.Warning, "enemies",
                        $"{enemy.id}: unknown targetRule '{enemy.targetRule}' (falls back to inherit). " +
                        $"Known: {string.Join(", ", KnownTargetRules)}.");
                }

            }

            if (bosses == 0)
            {
                Add(Severity.Error, "enemies", "No boss enemy defined; boss waves have nothing to spawn.");
            }

            Add(Severity.Info, "enemies", $"{file.enemies.Count} enemies checked ({bosses} boss).");
        }

        private static void CheckParty(PartySpecFile file, HeroSpecFile heroes)
        {
            HashSet<string> known = new HashSet<string>();
            for (int i = 0; i < heroes.heroes.Count; i++)
            {
                known.Add(heroes.heroes[i].id);
            }

            if (file.heroIds.Count == 0)
            {
                Add(Severity.Error, "party", "Party is empty.");
                return;
            }

            // The board (Step 10) decides how many heroes fit - not a hardcoded party size.
            FormationData formation = AssetDatabase.LoadAssetAtPath<FormationData>("Assets/IdleRPG/Data/Config/Formation_Default.asset");
            int boardSlots = formation != null ? formation.SlotCount : PartyConfig.FallbackPartySize;


            if (file.heroIds.Count > boardSlots)
            {
                Add(Severity.Warning, "party",
                    $"Party has {file.heroIds.Count} heroes; the formation board has {boardSlots} slot(s).");
            }

            HashSet<string> seen = new HashSet<string>();

            for (int i = 0; i < file.heroIds.Count; i++)
            {
                string id = file.heroIds[i];

                if (!known.Contains(id))
                {
                    Add(Severity.Error, "party", $"Lane {i} references unknown hero '{id}'.");
                }

                if (!seen.Add(id))
                {
                    Add(Severity.Error, "party", $"Hero '{id}' appears in the party twice.");
                }
            }

            // The other direction (B8' tool 2): a hero card that is not in the party exists but never fights.
            for (int i = 0; i < heroes.heroes.Count; i++)
            {
                string id = heroes.heroes[i].id;

                if (!string.IsNullOrEmpty(id) && !file.heroIds.Contains(id))
                {
                    Add(Severity.Warning, "party",
                        $"{id}: this hero is not in the party, so it never fights. Add it to party.json to make it playable.");
                }
            }

            Add(Severity.Info, "party", $"{file.heroIds.Count} lanes checked.");
        }

        private static void CheckWaves(WaveSpecFile file, EnemySpecFile enemies)
        {
            HashSet<string> known = new HashSet<string>();
            for (int i = 0; i < enemies.enemies.Count; i++)
            {
                known.Add(enemies.enemies[i].id);
            }

            RequirePool("waves.normal", file.normalEnemyIds, known);
            RequirePool("waves.boss", file.bossEnemyIds, known);

            // The other direction (B8' tool 2): a card that is in no pool exists but never appears in the game.
            for (int i = 0; i < enemies.enemies.Count; i++)
            {
                string id = enemies.enemies[i].id;

                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (!file.normalEnemyIds.Contains(id) && !file.bossEnemyIds.Contains(id))
                {
                    Add(Severity.Warning, "waves",
                        $"{id}: this monster is in no fight pool, so it will never appear in the game. " +
                        $"Add it to normalEnemyIds (or bossEnemyIds) to make it reachable.");
                }
            }

            Add(Severity.Info, "waves", $"normal {file.normalEnemyIds.Count}, boss {file.bossEnemyIds.Count}.");
        }

        private static void CheckUpgrades(UpgradeSpecFile file)
        {
            HashSet<string> statTypes = new HashSet<string>();
            HashSet<string> ids = new HashSet<string>();
            int compoundingTracks = 0;

            for (int i = 0; i < file.statUpgrades.Count; i++)
            {
                StatUpgradeSpec upgrade = file.statUpgrades[i];
                statTypes.Add((upgrade.statType ?? "").ToLowerInvariant());

                if (!ids.Add(upgrade.id))
                {
                    Add(Severity.Error, "upgrades", $"Duplicate stat upgrade id '{upgrade.id}'.");
                }

                if (upgrade.baseCost <= 0d)
                {
                    Add(Severity.Error, "upgrades", $"{upgrade.id}: baseCost must be > 0 (is {upgrade.baseCost}).");
                }

                if (upgrade.costGrowth < 1f)
                {
                    Add(Severity.Error, "upgrades", $"{upgrade.id}: costGrowth must be >= 1 (is {upgrade.costGrowth}).");
                }

                if (upgrade.statGainPerLevelFraction < 0f)
                {
                    Add(Severity.Error, "upgrades", $"{upgrade.id}: statGainPerLevelFraction must be >= 0.");
                }

                // B3d: the mode decides whether power is linear (loses the race) or compounding (races it).
                // A silent fall back to additive is a balance change, so an unknown/missing value is reported
                // instead of being swallowed.
                string mode = (upgrade.effectMode ?? "").Trim().ToLowerInvariant();
                bool compounding = mode == "multiplicative" || mode == "compounding";

                if (mode.Length == 0)
                {
                    Add(Severity.Warning, "upgrades",
                        $"{upgrade.id}: spec has no effectMode (treated as additive). Re-run Export Specs From Assets.");
                }
                else if (!compounding && mode != "additive")
                {
                    Add(Severity.Error, "upgrades",
                        $"{upgrade.id}: unknown effectMode '{upgrade.effectMode}' (use \"additive\" or \"multiplicative\").");
                }
                else if (compounding && upgrade.statGainPerLevelFraction <= 0f)
                {
                    Add(Severity.Error, "upgrades",
                        $"{upgrade.id}: compounding with statGainPerLevelFraction 0 leaves the track dead.");
                }
                else if (compounding)
                {
                    compoundingTracks++;
                }
            }

            string[] required = { "attack", "health", "defense" };
            for (int i = 0; i < required.Length; i++)
            {
                if (!statTypes.Contains(required[i]))
                {
                    Add(Severity.Error, "upgrades", $"Missing the '{required[i]}' stat upgrade track.");
                }
            }

            HashSet<string> prestigeIds = new HashSet<string>();

            for (int i = 0; i < file.prestigeUpgrades.Count; i++)
            {
                PrestigeUpgradeSpec upgrade = file.prestigeUpgrades[i];

                if (!prestigeIds.Add(upgrade.id))
                {
                    Add(Severity.Error, "upgrades", $"Duplicate prestige upgrade id '{upgrade.id}'.");
                }

                if (upgrade.baseCostTokens <= 0d)
                {
                    Add(Severity.Error, "upgrades", $"{upgrade.id}: baseCostTokens must be > 0.");
                }

                if (upgrade.effectPerLevel <= 0f)
                {
                    Add(Severity.Error, "upgrades", $"{upgrade.id}: effectPerLevel must be > 0.");
                }

                // B7 S4: costCurrency must be a known value, and a gems-priced track must be capped - an
                // uncapped permanent multiplier is unlimited power for a premium currency.
                string currency = (upgrade.costCurrency ?? "tokens").Trim().ToLowerInvariant();

                if (currency != "tokens" && currency != "prestigetokens" && currency != "gems" && currency != "gold")
                {
                    Add(Severity.Error, "upgrades", $"{upgrade.id}: unknown costCurrency '{upgrade.costCurrency}' (use tokens|gems).");
                }

                if ((currency == "gems" || currency == "gold") && upgrade.maxLevel <= 0)
                {
                    Add(Severity.Error, "upgrades",
                        $"{upgrade.id}: a {currency}-priced track must have a level cap (maxLevel > 0).");
                }
            }

            Add(Severity.Info, "upgrades",
                $"{file.statUpgrades.Count} stat + {file.prestigeUpgrades.Count} prestige tracks checked " +
                $"({compoundingTracks} compounding).");
        }
    }
}
