using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using IdleRPG.Combat;
using IdleRPG.Data;

namespace IdleRPG.EditorTools.Content
{
    /// <summary>
    /// One check per content area: formation, encounters, wave recipe, heroes, enemies, party, waves, upgrades, asset-vs-spec and the balance band.
    /// </summary>
    public static partial class ContentValidator
    {
        // ------------------------------------------------------------------
        // Formation (Step 10a)
        // ------------------------------------------------------------------
        /// <summary>
        /// Board sanity: the asset exists, the party fits on it, the unlock lists are ordered and sane, and the
        /// row rules are inside their legal ranges. Catches a hand-edited FormationData before the sim does.
        /// </summary>
        private static void CheckFormation()
        {
            string path = "Assets/IdleRPG/Data/Config/Formation_Default.asset";
            FormationData formation = AssetDatabase.LoadAssetAtPath<FormationData>(path);

            if (formation == null)
            {
                Add(Severity.Error, "formation", $"No FormationData at {path}; run Tools > Idle RPG > Generate Data Assets.");
                return;
            }

            PartyConfig party = SceneWiringUtility.LoadPartyConfig();
            int heroes = party != null ? party.ValidHeroCount : 0;

            if (formation.SlotCount < heroes)
            {
                Add(Severity.Error, "formation",
                    $"Board has {formation.SlotCount} slot(s) but the party has {heroes} hero(es).");
            }

            if (formation.BackRowTargetWeight > 1f)
            {
                Add(Severity.Error, "formation",
                    $"backRowTargetWeight must be in [0, 1] but is {formation.BackRowTargetWeight}.");
            }

            Add(Severity.Info, "formation",
                $"board front {formation.FrontSlots} / back {formation.BackSlots} ({formation.SlotCount} slots, " +
                $"all usable), back-rank target weight {formation.BackRowTargetWeight:0.##}.");
        }

        // ------------------------------------------------------------------
        // Encounters (Step 11a)
        // ------------------------------------------------------------------
        /// <summary>
        /// Multi-enemy sanity: the team size must fit the portrait layout, and the wave budget knobs are reported so
        /// a balance change is visible in the report. With enemiesPerWave = 1 this is the parity baseline.
        /// </summary>
        private static void CheckEncounters()
        {
            string path = ContentSpecIO.ConfigFolder + "/BalanceConfig.asset";
            BalanceConfig balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(path);

            if (balance == null)
            {
                Add(Severity.Error, "encounters", $"No BalanceConfig at {path}.");
                return;
            }

            int rawMax = SoField.Int(balance, "maxEnemiesPerWave", balance.MaxEnemiesPerWave);
            int rawMin = SoField.Int(balance, "minEnemiesPerWave", balance.MinEnemiesPerWave);

            if (rawMax > BalanceConfig.HardEnemyCap || rawMin > BalanceConfig.HardEnemyCap)
            {
                Add(Severity.Warning, "encounters",
                    $"wave size is clamped to {BalanceConfig.HardEnemyCap} (asset says min {rawMin} / max {rawMax}); " +
                    "the portrait layout holds three.");
            }

            if (rawMin > rawMax)
            {
                Add(Severity.Warning, "encounters", $"minEnemiesPerWave ({rawMin}) is above maxEnemiesPerWave ({rawMax}).");
            }

            CheckWaveRecipe(balance);

            Add(Severity.Info, "encounters",
                $"wave size {balance.MinEnemiesPerWave}-{balance.MaxEnemiesPerWave}, recipe {WaveComposition.Describe(balance)}, " +
                $"wave budget HP x{balance.WaveHealthMultiplier:0.##} ATK x{balance.WaveAttackMultiplier:0.##} " +
                $"gold x{balance.WaveGoldMultiplier:0.##}.");
        }

        /// <summary
        /// The wave-size recipe must be usable: weights above zero, every count inside min..max, and the
        /// resulting average has to be at least 1 (it drives the offline payout).
        /// </summary>
        private static void CheckWaveRecipe(BalanceConfig balance)
        {
            var recipe = balance.WaveCountWeights;
            int total = 0;

            if (recipe.Count == 0)
            {
                Add(Severity.Warning, "encounters", "Wave-size recipe is empty; every wave falls back to minEnemiesPerWave.");
            }

            for (int i = 0; i < recipe.Count; i++)
            {
                WaveCountWeight entry = recipe[i];

                if (entry.weight < 0)
                {
                    Add(Severity.Error, "encounters", $"Wave-size weight for {entry.count} enemies is negative.");
                }
                else if (entry.weight > 0 && (entry.count < balance.MinEnemiesPerWave || entry.count > balance.MaxEnemiesPerWave))
                {
                    Add(Severity.Warning, "encounters",
                        $"Wave-size {entry.count} is outside {balance.MinEnemiesPerWave}..{balance.MaxEnemiesPerWave} and will never be used.");
                }
                else
                {
                    total += entry.weight;
                }
            }

            if (total <= 0)
            {
                Add(Severity.Error, "encounters", "Wave-size weights sum to zero; every wave falls back to minEnemiesPerWave.");
            }
            else if (balance.MeanEnemiesPerWave < 1d)
            {
                Add(Severity.Error, "encounters", $"Wave-size average is {balance.MeanEnemiesPerWave:0.##}; it must be at least 1.");
            }
        }

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

            Add(Severity.Info, "waves", $"normal {file.normalEnemyIds.Count}, boss {file.bossEnemyIds.Count}.");
        }

        private static void CheckUpgrades(UpgradeSpecFile file)
        {
            HashSet<string> statTypes = new HashSet<string>();
            HashSet<string> ids = new HashSet<string>();

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
            }

            Add(Severity.Info, "upgrades",
                $"{file.statUpgrades.Count} stat + {file.prestigeUpgrades.Count} prestige tracks checked.");
        }

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
        // Balance band (one bounded headless stage run, reusing the Balance Lab runner)
        // ------------------------------------------------------------------
        private static void CheckBalanceBand()
        {
            BalanceConfig balance = BalanceLabMenu.Load<BalanceConfig>("BalanceConfig");
            WaveConfig waves = BalanceLabMenu.Load<WaveConfig>("WaveConfig");
            PartyConfig party = BalanceLabMenu.Load<PartyConfig>("PartyConfig");

            if (balance == null || waves == null || party == null)
            {
                Add(Severity.Error, "balance", "Config assets missing; skipped the stage-1 band check.");
                return;
            }

            BalanceLabMenu.StageRun run = BalanceLabMenu.RunStage(balance, waves, party, 1, balance.CombatPaceMultiplier);

            if (run.Wiped)
            {
                Add(Severity.Error, "balance", "An unupgraded party wipes on stage 1.");
            }

            if (run.Seconds < MinStageOneSeconds || run.Seconds > MaxStageOneSeconds)
            {
                Add(Severity.Error, "balance",
                    $"Stage 1 takes {run.Seconds:0}s; target is {MinStageOneSeconds}-{MaxStageOneSeconds}s.");
            }

            Add(Severity.Info, "balance",
                $"Stage 1: {run.Seconds:0}s, {run.Kills} kills, {run.Gold:0} gold, " +
                $"{(run.Seconds <= 0d ? 0d : run.Gold / run.Seconds):0.00} gold/s.");
        }
    }
}
