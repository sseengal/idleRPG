using System.Text;
using UnityEditor;
using UnityEngine;
using IdleRPG.Combat;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Sim;
using IdleRPG.Utils;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Headless balance tooling (Step 7a). Runs the real pure simulation - live, fast-forward and offline
    /// share this code, so what the tool prints is exactly what the player gets.
    ///
    /// Menu: Tools > Idle RPG > Balance Lab > ...
    /// Every action is a single bounded run (no background loops) and prints to the console.
    /// </summary>
    public static class BalanceLabMenu
    {
        private const string ConfigFolder = "Assets/IdleRPG/Data/Config";
        private const double StepSeconds = 0.05d;
        private const double WaveTimeoutSeconds = 600d;

        [MenuItem("Tools/Idle RPG/Balance Lab/Golden Numbers (parity check)", priority = 90)]
        public static void GoldenNumbers()
        {
            SimLogBridge.EnsureInstalled();

            BalanceConfig balance = Load<BalanceConfig>("BalanceConfig");
            WaveConfig waves = Load<WaveConfig>("WaveConfig");
            PartyConfig party = Load<PartyConfig>("PartyConfig");

            if (balance == null || waves == null || party == null)
            {
                return;
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine("=== Balance Lab: golden numbers (MVP parity) ===");
            report.AppendLine($"seed 12345, DefaultStatProvider (no upgrades), enemiesPerWave {balance.EnemiesPerWave}");

            foreach (double pace in new[] { 1d, (double)balance.CombatPaceMultiplier })
            {
                AppendPaceRun(report, balance, waves, party, pace);
            }

            Debug.Log(report.ToString());
        }

        [MenuItem("Tools/Idle RPG/Balance Lab/Encounter Factory Probe", priority = 92)]
        public static void EncounterFactoryProbe()
        {
            SimLogBridge.EnsureInstalled();

            BalanceConfig balance = Load<BalanceConfig>("BalanceConfig");
            WaveConfig waves = Load<WaveConfig>("WaveConfig");

            if (balance == null || waves == null)
            {
                return;
            }

            SimRules rules = SimRulesFactory.FromBalance(balance);
            StringBuilder report = new StringBuilder();
            report.AppendLine("=== Balance Lab: encounter factory (Step 11a) ===");
            report.AppendLine($"seed-free, stage 1, rules from BalanceConfig (pace x{rules.PaceMultiplier:0.##})");

            foreach (int count in new[] { 1, BalanceConfig.MaxEnemiesPerWave })
            {
                for (int wave = 1; wave <= 3; wave++)
                {
                    EnemyCombatant[] team = EncounterFactory.Build(waves, balance, 1, wave, false, rules, count);

                    if (team == null || team.Length == 0)
                    {
                        report.AppendLine($"count {count} wave {wave}: (nothing to spawn)");
                        continue;
                    }

                    double health = 0d;
                    double attack = 0d;
                    double gold = 0d;

                    for (int i = 0; i < team.Length; i++)
                    {
                        health += team[i].MaxHealth;
                        attack += team[i].Attack;
                        gold += team[i].GoldReward;
                    }

                    report.AppendLine(
                        $"count {count} wave {wave}: {team.Length} -> {EncounterFactory.Describe(team)} " +
                        $"| total {health:0.#}hp {attack:0.#}atk {gold:0.#}g");
                }
            }

            Debug.Log(report.ToString());
        }

        [MenuItem("Tools/Idle RPG/Balance Lab/Sweep Stages 1-10", priority = 93)]
        public static void SweepStages()
        {
            SimLogBridge.EnsureInstalled();

            BalanceConfig balance = Load<BalanceConfig>("BalanceConfig");
            WaveConfig waves = Load<WaveConfig>("WaveConfig");
            PartyConfig party = Load<PartyConfig>("PartyConfig");

            if (balance == null || waves == null || party == null)
            {
                return;
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine("=== Balance Lab: stage sweep (pace " + balance.CombatPaceMultiplier + ") ===");
            report.AppendLine("  stage   seconds  kills      gold   gold/s  wiped");

            for (int stage = 1; stage <= 10; stage++)
            {
                StageRun run = RunStage(balance, waves, party, stage, balance.CombatPaceMultiplier);
                report.AppendLine(string.Format("  {0,5}  {1,7:0.0}  {2,5}  {3,8}  {4,6:0.00}  {5}",
                    stage, run.Seconds, run.Kills, NumberFormatter.Format(run.Gold),
                    run.Seconds <= 0d ? 0d : run.Gold / run.Seconds, run.Wiped ? "yes" : "no"));
            }

            Debug.Log(report.ToString());
        }

        private static void AppendPaceRun(StringBuilder report, BalanceConfig balance, WaveConfig waves, PartyConfig party, double pace)
        {
            SimRules rules = SimRulesFactory.FromBalance(balance);
            rules.PaceMultiplier = pace;

            report.AppendLine($"-- pace x{pace:0.##} --");

            double normalWaves = balance.NormalWavesPerStage;

            for (int wave = 1; wave <= balance.NormalWavesPerStage + 1; wave++)
            {
                EnemyData data = waves.GetEnemyFor(1, wave, balance.NormalWavesPerStage);
                if (data == null)
                {
                    continue;
                }

                bool isBoss = WaveConfig.IsBossWave(wave, balance.NormalWavesPerStage);
                WaveRun waveRun = RunWave(balance, party, data, 1, isBoss, rules);

                report.AppendLine(string.Format("  {0,-18} {1,7:0} HP  TTK {2,6:0.0}s {3}{4}",
                    data.EnemyName,
                    isBoss ? EnemyHealth(data, 1, true, rules) : EnemyHealth(data, 1, false, rules),
                    waveRun.Seconds,
                    isBoss ? "(boss)" : "wave " + wave + "/" + normalWaves,
                    waveRun.Cleared ? string.Empty : "  [WIPED]"));
            }

            StageRun stageRun = RunStage(balance, waves, party, 1, pace);

            report.AppendLine(string.Format("  full stage: {0:0}s ({1:0.0} min) | kills {2} | gold {3} -> {4:0.00} gold/s",
                stageRun.Seconds, stageRun.Seconds / 60d, stageRun.Kills,
                NumberFormatter.Format(stageRun.Gold),
                stageRun.Seconds <= 0d ? 0d : stageRun.Gold / stageRun.Seconds));

            StageRun second = RunStage(balance, waves, party, 2, pace);
            report.AppendLine(string.Format("  stage 2: {0:0}s | wiped {1} (baseline: wiped by stage 2)", second.Seconds, second.Wiped ? "yes" : "no"));
        }

        private static double EnemyHealth(EnemyData data, int stage, bool isBoss, SimRules rules)
        {
            double hp = FormulaUtility.EnemyMaxHealth(data.BaseHealth, stage, rules.EnemyHealthGrowth);
            return isBoss ? hp * data.BossHealthMultiplier : hp;
        }

        /// <summary>Runs one wave headless and returns how long it took.</summary>
        private static WaveRun RunWave(BalanceConfig balance, PartyConfig party, EnemyData enemy, int stage, bool isBoss, SimRules rules)
        {
            SimContext context = new SimContext(rules, 12345, SimMode.Offline);
            CombatSimulator simulator = new CombatSimulator(DefaultStatProvider.Instance, balance.EnemyTargeting, context, 12345);

            if (!simulator.SetupParty(party) || !simulator.StartEncounter(enemy, stage, isBoss))
            {
                return new WaveRun { Seconds = 0d };
            }

            double elapsed = 0d;
            while (simulator.IsEncounterActive && elapsed < WaveTimeoutSeconds)
            {
                simulator.Step(StepSeconds);
                elapsed += StepSeconds;
            }

            return new WaveRun { Seconds = elapsed, Cleared = !simulator.IsEncounterActive };
        }

        /// <summary>Runs a full stage (all waves + boss) headless, healing the party at the start like the game does.</summary>
        internal static StageRun RunStage(BalanceConfig balance, WaveConfig waves, PartyConfig party, int stage, double pace)
        {
            SimRules rules = SimRulesFactory.FromBalance(balance);
            rules.PaceMultiplier = pace;

            SimContext context = new SimContext(rules, 12345, SimMode.Offline);
            CombatSimulator simulator = new CombatSimulator(DefaultStatProvider.Instance, balance.EnemyTargeting, context, 12345);

            if (!simulator.SetupParty(party))
            {
                return new StageRun();
            }

            double lastKillGold = 0d;
            System.Action<double> onKill = gold => lastKillGold = gold;
            simulator.EnemyKilled += onKill;

            StageRun run = new StageRun();
            int lastWave = balance.NormalWavesPerStage + 1;

            for (int wave = 1; wave <= lastWave; wave++)
            {
                EnemyData data = waves.GetEnemyFor(stage, wave, balance.NormalWavesPerStage);
                if (data == null)
                {
                    continue;
                }

                bool isBoss = WaveConfig.IsBossWave(wave, balance.NormalWavesPerStage);

                if (!simulator.StartEncounter(data, stage, isBoss))
                {
                    break;
                }

                double waveSeconds = 0d;
                while (simulator.IsEncounterActive && waveSeconds < WaveTimeoutSeconds)
                {
                    simulator.Step(StepSeconds);
                    waveSeconds += StepSeconds;
                }

                run.Seconds += waveSeconds;

                if (simulator.IsEncounterActive)
                {
                    // Timed out: treat as a wipe so the sweep cannot run away.
                    run.Wiped = true;
                    break;
                }

                run.Kills++;
                run.Gold += lastKillGold;

                if (simulator.AliveHeroCount == 0)
                {
                    run.Wiped = true;
                    break;
                }
            }

            simulator.EnemyKilled -= onKill;
            return run;
        }

        private struct WaveRun
        {
            public double Seconds;
            public bool Cleared;
        }

        internal struct StageRun
        {
            public double Seconds;
            public int Kills;
            public double Gold;
            public bool Wiped;
        }

        internal static T Load<T>(string fileName) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>($"{ConfigFolder}/{fileName}.asset");
            if (asset == null)
            {
                Debug.LogError($"[BalanceLab] Missing asset {ConfigFolder}/{fileName}.asset");
            }

            return asset;
        }
    }
}
