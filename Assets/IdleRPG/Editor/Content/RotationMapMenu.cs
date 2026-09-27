using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using IdleRPG.Data;

namespace IdleRPG.EditorTools.Content
{
    /// <summary>
    /// The map (B8' tool 3).
    ///
    /// Plain words: a menu that prints which monster appears at every stage and wave, and shows in numbers what
    /// happens if you add one more card - because the fight rotation wraps around the pool size, so adding a card
    /// re-shuffles which monster shows up at every stage. Read it BEFORE a card is committed, so the difficulty
    /// re-record happens on purpose instead of by surprise.
    ///
    /// Every pick comes from <see cref="WaveConfig.ResolvePoolIndex"/> - the exact same maths the shipped game runs,
    /// so the map and the game can never disagree.
    /// </summary>
    public static class RotationMapMenu
    {
        private const int StagesShown = 10;
        private const int StagesForCounts = 20;
        private const string ReportPath = "Temp/rotation-map.txt";

        [MenuItem("Tools/Idle RPG/Content/Print Rotation Map (who appears where)", priority = 105)]
        public static void Print()
        {
            WaveSpecFile waves = ContentSpecIO.Load<WaveSpecFile>(ContentSpecIO.WavesPath);
            EnemySpecFile enemies = ContentSpecIO.Load<EnemySpecFile>(ContentSpecIO.EnemiesPath);
            BalanceConfig balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(
                ContentSpecIO.ConfigFolder + "/BalanceConfig.asset");

            if (waves == null)
            {
                Debug.LogError("[RotationMap] No waves.json to read.");
                return;
            }

            int normalWaves = balance != null ? balance.NormalWavesPerStage : 10;

            StringBuilder text = new StringBuilder();
            text.AppendLine("Rotation map (who appears where)");
            text.AppendLine();
            text.AppendLine($"Normal waves per stage: {normalWaves} (+1 boss wave)");
            text.AppendLine($"Pools: normal {waves.normalEnemyIds.Count} [{ListNames(waves.normalEnemyIds, enemies)}] | " +
                            $"boss {waves.bossEnemyIds.Count} [{ListNames(waves.bossEnemyIds, enemies)}]");
            text.AppendLine();
            text.AppendLine("READ THIS FIRST: the pick wraps around the pool size, so ADDING A CARD RE-SHUFFLES");
            text.AppendLine("WHICH MONSTER APPEARS AT EVERY STAGE. Adding a monster is a balance change, not just a card.");
            text.AppendLine();

            for (int stage = 1; stage <= StagesShown; stage++)
            {
                StringBuilder row = new StringBuilder();
                row.Append($"S{stage,2}");
                row.Append(" :");

                for (int wave = 1; wave <= normalWaves + 1; wave++)
                {
                    bool bossWave = wave >= normalWaves + 1;
                    string pick = PickName(waves, enemies, stage, wave, bossWave);
                    row.Append(bossWave ? $"  [BOSS] {pick}" : $"  {pick}");
                }

                text.AppendLine(row.ToString());
            }

            text.AppendLine();
            text.AppendLine($"Lead monster over stages 1-{StagesForCounts} (the first enemy of each stage):");

            Dictionary<string, int> leadCounts = CountLeads(waves, enemies, normalWaves, StagesForCounts);
            foreach (KeyValuePair<string, int> pair in leadCounts)
            {
                text.AppendLine($"  {pair.Key,20} x{pair.Value}");
            }

            text.AppendLine();
            text.AppendLine("What changes if you append ONE more card:");

            int totalPicks = StagesForCounts * (normalWaves + 1);
            int normalChanged = CountPicksThatChange(waves.normalEnemyIds.Count, normalWaves, StagesForCounts);
            text.AppendLine($"  normal pool {waves.normalEnemyIds.Count} -> {waves.normalEnemyIds.Count + 1}: " +
                            $"{normalChanged} of the first {totalPicks} stage/wave picks become a DIFFERENT monster.");

            int bossChanged = CountPicksThatChange(waves.bossEnemyIds.Count, 1, StagesForCounts);
            text.AppendLine($"  boss pool {waves.bossEnemyIds.Count} -> {waves.bossEnemyIds.Count + 1}: " +
                            $"{bossChanged} of {StagesForCounts} boss waves pick a DIFFERENT boss.");

            text.AppendLine();
            text.AppendLine("The plan: add the card -> press \"Generate Assets From Specs\" -> print this map again ->");
            text.AppendLine("re-record the golden numbers on purpose (see Content.md recipe).");
            text.AppendLine();
            text.AppendLine(AssetDriftLine(waves));

            WriteReport(text.ToString());
            Debug.Log(text.ToString());
        }
// ------------------------------------------------------------------
        // Reading + maths
        // ------------------------------------------------------------------

        private static string ListNames(List<string> ids, EnemySpecFile enemies)
        {
            StringBuilder name = new StringBuilder();
            for (int i = 0; i < ids.Count; i++)
            {
                string label = NameFor(ids[i], enemies);
                name.Append(name.Length > 0 ? ", " : string.Empty);
                name.Append(label);
            }

            return name.ToString();
        }

        private static string NameFor(string id, EnemySpecFile enemies)
        {
            if (enemies != null)
            {
                for (int i = 0; i < enemies.enemies.Count; i++)
                {
                    if (enemies.enemies[i].id == id)
                    {
                        return string.IsNullOrEmpty(enemies.enemies[i].displayName) ? id : enemies.enemies[i].displayName;
                    }
                }
            }

            return id;
        }

        private static string PickName(WaveSpecFile waves, EnemySpecFile enemies, int stage, int wave, bool bossWave)
        {
            List<string> pool = bossWave ? waves.bossEnemyIds : waves.normalEnemyIds;
            if (pool == null || pool.Count == 0)
            {
                return "(nothing configured)";
            }

            return NameFor(pool[WaveConfig.ResolvePoolIndex(stage, wave, pool.Count)], enemies);
        }

        private static Dictionary<string, int> CountLeads(WaveSpecFile waves, EnemySpecFile enemies, int normalWaves, int stages)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            for (int stage = 1; stage <= stages; stage++)
            {
                string lead = PickName(waves, enemies, stage, 1, false);
                counts[lead] = counts.ContainsKey(lead) ? counts[lead] + 1 : 1;
            }

            return counts;
        }

        /// <summary>How many stage/wave picks land on a different index when a pool grows from size to size+1.</summary>
        private static int CountPicksThatChange(int poolSize, int wavesPerStage, int stages)
        {
            if (poolSize <= 0)
            {
                return 0;
            }

            int changed = 0;

            for (int stage = 1; stage <= stages; stage++)
            {
                for (int wave = 1; wave <= wavesPerStage; wave++)
                {
                    if (WaveConfig.ResolvePoolIndex(stage, wave, poolSize) !=
                        WaveConfig.ResolvePoolIndex(stage, wave, poolSize + 1))
                    {
                        changed++;
                    }
                }
            }

            return changed;
        }

        /// <summary>
        /// One truth check: the map reads the cards (the spec), the shipped game reads the generated asset. When those
        /// two disagree (someone edited the asset in the inspector and did not regenerate), say so loudly.
        /// </summary>
        private static string AssetDriftLine(WaveSpecFile waves)
        {
            WaveConfig config = AssetDatabase.LoadAssetAtPath<WaveConfig>(ContentSpecIO.ConfigFolder + "/WaveConfig.asset");
            if (config == null)
            {
                return "Generated asset: NOT FOUND - press \"Generate Assets From Specs\" first.";
            }

            List<string> normal = ReadPoolIds(config, "normalEnemies");
            List<string> boss = ReadPoolIds(config, "bossEnemies");

            bool same = normal.SequenceEqual(waves.normalEnemyIds) && boss.SequenceEqual(waves.bossEnemyIds);
            return same
                ? "Generated asset matches the cards (same pools, same order)."
                : "WARNING: the generated asset does NOT match the cards - press \"Generate Assets From Specs\" before trusting this map.";
        }

        private static List<string> ReadPoolIds(WaveConfig config, string poolField)
        {
            List<string> ids = new List<string>();
            int count = SoField.Count(config, poolField);
            for (int i = 0; i < count; i++)
            {
                EnemyData enemy = SoField.ElementAt(config, poolField, i) as EnemyData;
                if (enemy != null)
                {
                    ids.Add(SoField.Text(enemy, "enemyID", enemy.name));
                }
            }

            return ids;
        }

        private static void WriteReport(string text)
        {
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "../" + ReportPath));
            File.WriteAllText(full, text);
            Debug.Log($"[RotationMap] Wrote {ReportPath}");
}
}
        }