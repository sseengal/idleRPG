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
    /// Reads every spec + asset and shouts about anything broken, missing or unbalanced.
    ///
    /// ELI5: the recipe cards are only useful if someone checks them. This is the kitchen inspector: it reads
    /// all the cards, compares them with what is actually in the game, and prints a PASS/FAIL list (plus a CSV)
    /// before a broken card can ever reach the player.
    ///
    /// Menu: Tools > Idle RPG > Content > Validate Content
    /// </summary>
    public static partial class ContentValidator
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
            UpgradeSpecFile upgrades = ContentSpecIO.Load<UpgradeSpecFile>(ContentSpecIO.TracksPath);

            CheckHeroes(heroes);
            CheckEnemies(enemies);
            CheckParty(party, heroes);
            CheckWaves(waves, enemies);
            CheckUpgrades(upgrades);
            CheckAssetsAgainstSpecs(heroes, enemies);
            CheckAssetReferences(enemies, heroes);
            CheckFormation();
            CheckEncounters();
            CheckBalanceBand();
            CheckLoopHealth();
            CheckPowerBand();

            WriteCsv();
            LogSummary();
            return Issues;
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
