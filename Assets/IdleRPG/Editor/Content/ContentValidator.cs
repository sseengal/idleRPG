using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using IdleRPG.Data;

namespace IdleRPG.EditorTools.Content
{
    /// <summary>
    /// Reads every spec + asset and shouts about anything broken, missing or unbalanced.
    ///
    /// ELI5: the recipe cards are only useful if someone checks them. This is the kitchen inspector: it reads
    /// all the cards, compares them with what is actually in the game, and prints a PASS/FAIL list (plus a CSV)
    /// before a broken card can ever reach the player.
    ///
    /// Menu: Tools > Idle RPG > Content > Validate Content
    /// </summary>
    public static class ContentValidator
    {
        private const int MinStageOneSeconds = 90;
        private const int MaxStageOneSeconds = 180;

        /// <summary>Formation fields an enemy spec may carry (Step 11a).</summary>
        private static readonly System.Collections.Generic.HashSet<string> KnownTargetRules =
            new System.Collections.Generic.HashSet<string> { "inherit", "frontmost", "lowesthealthpercent", "random", "backlinefirst" };

        public enum Severity
        {
            Error = 0,
            Warning = 1,
            Info = 2
        }

        public struct Issue
        {
            public Severity Severity;
            public string Area;
            public string Message;
        }

        private static readonly List<Issue> Issues = new List<Issue>();

        [MenuItem("Tools/Idle RPG/Content/Validate Content", priority = 102)]
        public static void ValidateMenu()
        {
            Validate();
        }

        /// <summary>Runs every check. Returns the collected issues (also logged + written to CSV).</summary>
        public static List<Issue> Validate()
        {
            Issues.Clear();

            HeroSpecFile heroes = ContentSpecIO.Load<HeroSpecFile>(ContentSpecIO.HeroesPath);
            EnemySpecFile enemies = ContentSpecIO.Load<EnemySpecFile>(ContentSpecIO.EnemiesPath);
            PartySpecFile party = ContentSpecIO.Load<PartySpecFile>(ContentSpecIO.PartyPath);
            WaveSpecFile waves = ContentSpecIO.Load<WaveSpecFile>(ContentSpecIO.WavesPath);
            UpgradeSpecFile upgrades = ContentSpecIO.Load<UpgradeSpecFile>(ContentSpecIO.UpgradesPath);

            CheckHeroes(heroes);
            CheckEnemies(enemies);
            CheckParty(party, heroes);
            CheckWaves(waves, enemies);
            CheckUpgrades(upgrades);
            CheckAssetsAgainstSpecs(heroes, enemies);
            CheckFormation();
            CheckEncounters();
            CheckBalanceBand();

            WriteCsv();
            LogSummary();
            return Issues;
        }

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

            int raw = SoField.Int(balance, "enemiesPerWave", balance.EnemiesPerWave);

            if (raw > BalanceConfig.MaxEnemiesPerWave)
            {
                Add(Severity.Warning, "encounters",
                    $"enemiesPerWave is clamped to {BalanceConfig.MaxEnemiesPerWave} (the asset says {raw}); " +
                    "the portrait layout holds three.");
            }

            Add(Severity.Info, "encounters",
                $"enemiesPerWave {balance.EnemiesPerWave} (1 = single enemy; 2-3 stack on the battle page), wave budget " +
                $"HP x{balance.WaveHealthMultiplier:0.##} ATK x{balance.WaveAttackMultiplier:0.##} " +
                $"gold x{balance.WaveGoldMultiplier:0.##}.");
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

        // ------------------------------------------------------------------
        // Helpers + reporting
        // ------------------------------------------------------------------
        private static void RequireId(string id, string area, string assetName, HashSet<string> ids, HashSet<string> assets)
        {
            if (string.IsNullOrEmpty(id))
            {
                Add(Severity.Error, area, "An entry has an empty id.");
                return;
            }

            if (!ids.Add(id))
            {
                Add(Severity.Error, area, $"Duplicate id '{id}'.");
            }

            if (string.IsNullOrEmpty(assetName))
            {
                Add(Severity.Error, area, $"{id}: no asset file name.");
                return;
            }

            if (!assets.Add(assetName))
            {
                Add(Severity.Error, area, $"Duplicate asset name '{assetName}' (each id needs its own asset).");
            }
        }

        private static void RequirePool(string area, List<string> ids, HashSet<string> known)
        {
            if (ids == null || ids.Count == 0)
            {
                Add(Severity.Error, area, "Pool is empty.");
                return;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                if (!known.Contains(ids[i]))
                {
                    Add(Severity.Error, area, $"Unknown enemy id '{ids[i]}'.");
                }
            }
        }

        private static void Add(Severity severity, string area, string message)
        {
            Issues.Add(new Issue { Severity = severity, Area = area, Message = message });
        }

        private static void WriteCsv()
        {
            StringBuilder csv = new StringBuilder("severity,area,message\n");

            for (int i = 0; i < Issues.Count; i++)
            {
                Issue issue = Issues[i];
                csv.Append(issue.Severity).Append(',')
                   .Append('"').Append(issue.Area).Append('"').Append(',')
                   .Append('"').Append(issue.Message.Replace("\"", "'")).Append('"').Append('\n');
            }

            try
            {
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/content-report.csv"));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, csv.ToString());
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"[ContentValidator] Could not write the CSV: {exception.Message}");
            }
        }

        private static void LogSummary()
        {
            int errors = 0;
            int warnings = 0;
            StringBuilder report = new StringBuilder();

            report.AppendLine("=== Content validation ===");

            for (int i = 0; i < Issues.Count; i++)
            {
                Issue issue = Issues[i];

                if (issue.Severity == Severity.Error)
                {
                    errors++;
                }
                else if (issue.Severity == Severity.Warning)
                {
                    warnings++;
                }

                string marker = issue.Severity == Severity.Error ? "--" : (issue.Severity == Severity.Warning ? "??" : "ok");
                report.AppendLine($"  {marker} [{issue.Area}] {issue.Message}");
            }

            report.AppendLine(errors == 0
                ? $"RESULT: clean ({warnings} warning(s)). CSV: Temp/content-report.csv"
                : $"RESULT: {errors} error(s), {warnings} warning(s). CSV: Temp/content-report.csv");

            if (errors == 0)
            {
                Debug.Log(report.ToString());
            }
            else
            {
                Debug.LogError(report.ToString());
            }
        }
    }
}
