using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Debugging;
using IdleRPG.Progression;
using IdleRPG.Save;
using IdleRPG.Utils;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Editor helpers for balance work and quick Play-mode pokes.
    /// Menu: Tools > Idle RPG > Debug > ...
    /// </summary>
    public static class ProgressionDebugMenu
    {
        private const string ConfigFolder = "Assets/IdleRPG/Data/Config";
        private const string HeroFolder = "Assets/IdleRPG/Data/Heroes";
        private const string EnemyFolder = "Assets/IdleRPG/Data/Enemies";

        [MenuItem("Tools/Idle RPG/Debug/Log Balance Summary", priority = 40)]
        public static void LogBalanceSummary()
        {
            BalanceConfig balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ConfigFolder + "/BalanceConfig.asset");
            if (balance == null)
            {
                Debug.LogError("[ProgressionDebugMenu] BalanceConfig.asset not found.");
                return;
            }

            StatUpgradeData atk = AssetDatabase.LoadAssetAtPath<StatUpgradeData>(ConfigFolder + "/StatUpgrade_ATK.asset");
            StatUpgradeData hp = AssetDatabase.LoadAssetAtPath<StatUpgradeData>(ConfigFolder + "/StatUpgrade_HP.asset");
            StatUpgradeData def = AssetDatabase.LoadAssetAtPath<StatUpgradeData>(ConfigFolder + "/StatUpgrade_DEF.asset");
            PrestigeUpgradeData goldPrestige = AssetDatabase.LoadAssetAtPath<PrestigeUpgradeData>(ConfigFolder + "/Prestige_Gold.asset");

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("=== Idle RPG balance summary ===");
            builder.AppendLine($"Enemy HP x{balance.EnemyHealthGrowth} | gold x{balance.EnemyGoldGrowth} | attack x{balance.EnemyAttackGrowth}");
            builder.AppendLine($"Crit {balance.CriticalChance:P0} x{balance.CriticalDamageMultiplier} | min damage ratio {balance.MinDamageRatio:P0}");
            builder.AppendLine($"Waves/stage {balance.NormalWavesPerStage} (+1 boss) | rollback {balance.StageRollbackOnDefeat} stage(s)");
            builder.AppendLine("-- Enemy HP / gold by stage (Slime base 60 HP, 10 gold) --");

            for (int stage = 1; stage <= 60; stage += stage < 10 ? 3 : 10)
            {
                double hpAtStage = FormulaUtility.EnemyMaxHealth(60d, stage, balance.EnemyHealthGrowth);
                double goldAtStage = FormulaUtility.EnemyGoldDrop(10d, stage, balance.EnemyGoldGrowth);
                builder.AppendLine($"  stage {stage,3}: HP {NumberFormatter.Format(hpAtStage),8} | gold/kill {NumberFormatter.Format(goldAtStage),8}");
            }

            builder.AppendLine("-- ATK upgrade cost for +10 levels (from level 0/10/20/30) --");

            if (atk != null)
            {
                float growth = atk.GetCostGrowth(balance.UpgradeCostGrowth);
                builder.AppendLine($"  {NumberFormatter.Format(FormulaUtility.StatUpgradeBulkCost(atk.BaseCost, 0, 10, growth))} / " +
                                   $"{NumberFormatter.Format(FormulaUtility.StatUpgradeBulkCost(atk.BaseCost, 10, 10, growth))} / " +
                                   $"{NumberFormatter.Format(FormulaUtility.StatUpgradeBulkCost(atk.BaseCost, 20, 10, growth))} / " +
                                   $"{NumberFormatter.Format(FormulaUtility.StatUpgradeBulkCost(atk.BaseCost, 30, 10, growth))}");
            }

            if (hp != null && def != null)
            {
                builder.AppendLine($"  HP base cost {hp.BaseCost}, +{hp.StatGainPerLevelFraction:P0} of base per level | " +
                                   $"DEF base cost {def.BaseCost}, +{def.StatGainPerLevelFraction:P0} per level");
            }

            builder.AppendLine("-- Prestige --");

            if (goldPrestige != null)
            {
                double total = 0d;
                for (int level = 0; level < goldPrestige.MaxLevel; level++)
                {
                    total += FormulaUtility.StatUpgradeCost(goldPrestige.BaseCostTokens, level, goldPrestige.CostGrowth);
                }

                builder.AppendLine($"  {goldPrestige.DisplayName}: +{goldPrestige.EffectPerLevel:P0}/level, {goldPrestige.MaxLevel} levels, " +
                                   $"~{NumberFormatter.Format(total)} tokens for x{goldPrestige.GetMultiplier(goldPrestige.MaxLevel):0.00}");
            }

            builder.AppendLine("-- Pace (knob: BalanceConfig.combatPaceMultiplier) --");
            AppendPaceSummary(builder, balance);
            AppendOfflineSummary(builder, balance);

            builder.Append("  Token yield by highest stage: ");

            for (int stage = 10; stage <= 100; stage += 10)
            {
                builder.Append($"{stage}->{FormulaUtility.PrestigeTokenReward(stage, balance.PrestigeStageDivisor, balance.PrestigeExponent):0} ");
            }

            builder.AppendLine();
            Debug.Log(builder.ToString());
        }

        /// <summary>
        /// Estimates a full stage at the current pace. This is the number to watch while tuning:
        /// pace changes wall-clock time only, damage/health/gold ratios stay untouched.
        /// </summary>
        /// <summary>
        /// Prints the offline payout model: both caps, the rate per stage and what 1h / 2h / 8h / 9h
        /// of absence is worth. Stage length is assumed constant so the numbers stay comparable.
        /// </summary>
        private static void AppendOfflineSummary(StringBuilder builder, BalanceConfig balance)
        {
            WaveConfig waves = AssetDatabase.LoadAssetAtPath<WaveConfig>(ConfigFolder + "/WaveConfig.asset");

            if (waves == null)
            {
                builder.AppendLine("  wave config missing");
                return;
            }

            double wallCap = balance.OfflineCapSeconds;
            double equivalentCap = balance.OfflineMaxEquivalentSeconds;
            double efficiency = balance.OfflineEfficiency;
            builder.AppendLine("");
            builder.AppendLine(string.Format(
                "-- Offline earnings (caps: wall {0:0}h, equivalent {1:0}h | efficiency {2:0}%) --",
                wallCap / 3600d, equivalentCap / 3600d, efficiency * 100d));

            double[] awayList = { 3600d, 7200d, 28800d, 32400d };
            int[] stageList = { 1, 10, 30 };

            foreach (int stage in stageList)
            {
                double rate = IdleTimeService.EstimateGoldPerSecond(balance, waves, stage);
                builder.AppendLine(string.Format("  stage {0,-3} rate {1,8:0.00} gold/s  (formula fallback)", stage, rate));

                foreach (double away in awayList)
                {
                    double wall = Math.Min(away, wallCap);
                    double paid = Math.Min(wall, equivalentCap);
                    double gold = paid * rate * efficiency;
                    double goldPerStage = EstimateStageGold(balance, waves, stage);
                    double stageIncomes = gold / Mathf.Max(1f, (float)goldPerStage);

                    builder.AppendLine(string.Format(
                        "    away {0,4:0.#}h -> paid {1,5:0.0}h -> {2,10} gold  (~{3:0.#} stage-incomes){4}",
                        away / 3600d, paid / 3600d, NumberFormatter.Format(gold), stageIncomes,
                        away > paid + 0.5d ? "  [capped]" : string.Empty));
                }
            }
        }

        /// <summary>
        /// Gold for one clear of a stage: the rotating normal pool plus the boss,
        /// scaled to that stage. Used for the offline "stage-incomes" column.
        /// </summary>
        private static double EstimateStageGold(BalanceConfig balance, WaveConfig waves, int stage)
        {
            int normalWaves = balance.NormalWavesPerStage;
            int poolSize = Mathf.Max(1, waves.NormalEnemyCount);
            double wavesPerEnemy = normalWaves / (double)poolSize;
            double gold = 0d;

            for (int i = 0; i < poolSize; i++)
            {
                EnemyData enemy = waves.GetEnemyFor(stage, i + 1, normalWaves);
                if (enemy != null)
                {
                    gold += FormulaUtility.EnemyGoldDrop(enemy.BaseGoldDrop, stage, balance.EnemyGoldGrowth) * wavesPerEnemy;
                }
            }

            EnemyData boss = waves.GetEnemyFor(stage, normalWaves + 1, normalWaves);
            if (boss != null)
            {
                gold += FormulaUtility.EnemyGoldDrop(boss.BaseGoldDrop, stage, balance.EnemyGoldGrowth) * boss.BossGoldMultiplier;
            }

            return gold;
        }

        private static void AppendPaceSummary(StringBuilder builder, BalanceConfig balance)
        {
            PartyConfig party = AssetDatabase.LoadAssetAtPath<PartyConfig>(ConfigFolder + "/PartyConfig.asset");
            WaveConfig waves = AssetDatabase.LoadAssetAtPath<WaveConfig>(ConfigFolder + "/WaveConfig.asset");

            if (party == null || waves == null)
            {
                builder.AppendLine("  party/wave config missing");
                return;
            }

            double pace = balance.CombatPaceMultiplier;
            double critFactor = 1d + balance.CriticalChance * (balance.CriticalDamageMultiplier - 1d);
            int normalWaves = balance.NormalWavesPerStage;
            int poolSize = Mathf.Max(1, waves.NormalEnemyCount);
            double wavesPerEnemy = normalWaves / (double)poolSize;

            builder.AppendLine(string.Format("  pace x{0:0.##} -> attacks per hero are {1:0}% slower | crit factor x{2:0.###}",
                pace, (pace - 1f) * 100f, critFactor));

            double stageSeconds = 0d;

            for (int i = 0; i < poolSize; i++)
            {
                EnemyData enemy = waves.GetEnemyFor(1, i + 1, normalWaves);
                if (enemy == null)
                {
                    continue;
                }

                double dps = PartyDpsAgainst(enemy.BaseDefense, party, balance) * critFactor;
                double health = FormulaUtility.EnemyMaxHealth(enemy.BaseHealth, 1, balance.EnemyHealthGrowth);
                double ttk = dps <= 0d ? 0d : health / dps;
                stageSeconds += ttk * wavesPerEnemy;

                builder.AppendLine(string.Format("  {0,-16} {1,7:0} HP  TTK {2,6:0.0}s  x{3:0.#} waves",
                    enemy.EnemyName, health, ttk, wavesPerEnemy));
            }

            EnemyData boss = waves.GetEnemyFor(1, normalWaves + 1, normalWaves);
            if (boss != null)
            {
                double bossDps = PartyDpsAgainst(boss.BaseDefense, party, balance) * critFactor;
                double bossHealth = FormulaUtility.EnemyMaxHealth(boss.BaseHealth, 1, balance.EnemyHealthGrowth) * boss.BossHealthMultiplier;
                double bossTtk = bossDps <= 0d ? 0d : bossHealth / bossDps;
                stageSeconds += bossTtk;
                builder.AppendLine(string.Format("  {0,-16} {1,7:0} HP  TTK {2,6:0.0}s  (boss)",
                    boss.EnemyName, bossHealth, bossTtk));
            }

            stageSeconds += (normalWaves + 1) * balance.WaveTransitionDelaySec;
            double seconds = Mathf.Max(1f, (float)stageSeconds);

            builder.AppendLine(string.Format("  full stage estimate: {0:0}s ({1:0.0} min)", stageSeconds, stageSeconds / 60d));

            double goldPerStage = 0d;
            for (int i = 0; i < poolSize; i++)
            {
                EnemyData enemy = waves.GetEnemyFor(1, i + 1, normalWaves);
                if (enemy != null)
                {
                    goldPerStage += FormulaUtility.EnemyGoldDrop(enemy.BaseGoldDrop, 1, balance.EnemyGoldGrowth) * wavesPerEnemy;
                }
            }

            if (boss != null)
            {
                goldPerStage += FormulaUtility.EnemyGoldDrop(boss.BaseGoldDrop, 1, balance.EnemyGoldGrowth) * boss.BossGoldMultiplier;
            }

            double goldPerSecond = goldPerStage / seconds;
            double firstUpgrade = FormulaUtility.StatUpgradeCost(10d, 0, balance.UpgradeCostGrowth);

            builder.AppendLine(string.Format("  gold/stage {0} -> {1:0.0} gold/s -> first ATK level every {2:0.0}s",
                NumberFormatter.Format(goldPerStage), goldPerSecond, firstUpgrade / Mathf.Max(0.01f, (float)goldPerSecond)));
        }

        private static double PartyDpsAgainst(double enemyDefense, PartyConfig party, BalanceConfig balance)
        {
            double pace = balance.CombatPaceMultiplier;
            double dps = 0d;

            int lanes = party != null ? party.ValidHeroCount : 0;

            for (int i = 0; i < lanes; i++)
            {
                HeroData hero = party.GetHero(i);
                if (hero == null)
                {
                    continue;
                }

                double interval = Mathf.Max(0.1f, hero.AttackIntervalSec * (float)pace);
                dps += FormulaUtility.Damage(hero.BaseAttack, enemyDefense, balance.MinDamageRatio) / interval;
            }

            return dps;
        }

        [MenuItem("Tools/Idle RPG/Debug/Grant 100K Gold (Play Mode)", priority = 41)]
        public static void GrantGold()
        {
            GameManager manager = FindRunningGameManager();
            if (manager != null)
            {
                manager.Economy.AddGold(100000d);
                Debug.Log($"[ProgressionDebugMenu] Granted 100K gold | {manager.Economy}");
            }
        }

        [MenuItem("Tools/Idle RPG/Debug/Log Hotkeys", priority = 45)]
        public static void LogHotkeys()
        {
            // Same catalogue the F3 overlay renders, so the console and the screen can never disagree.
            Debug.Log("[ProgressionDebugMenu]\n" + DebugHotkeyCatalog.BuildLegend());
        }

        [MenuItem("Tools/Idle RPG/Debug/Grant 25 Tokens (Play Mode)", priority = 42)]
        public static void GrantTokens()
        {
            GameManager manager = FindRunningGameManager();
            if (manager != null)
            {
                manager.Economy.AddTokens(25d);
                Debug.Log($"[ProgressionDebugMenu] Granted 25 tokens | {manager.Economy}");
            }
        }

        [MenuItem("Tools/Idle RPG/Debug/Jump To Stage 10 (Play Mode)", priority = 43)]
        public static void JumpToStageTen()
        {
            GameManager manager = FindRunningGameManager();
            if (manager != null)
            {
                manager.StartRun(10);
                Debug.Log($"[ProgressionDebugMenu] Jumped to stage {manager.CurrentStage}. Press A in game to test ascension.");
            }
        }

        [MenuItem("Tools/Idle RPG/Debug/Log Live State (Play Mode)", priority = 44)]
        public static void LogLiveState()
        {
            GameManager manager = FindRunningGameManager();
            if (manager == null)
            {
                return;
            }

            StringBuilder prestige = new StringBuilder();

            foreach (PrestigeUpgradeData upgrade in manager.Ascension.Upgrades)
            {
                if (upgrade == null)
                {
                    continue;
                }

                prestige.Append($"{upgrade.DisplayName} L{manager.Ascension.GetPrestigeLevel(upgrade)} ");
            }

            Debug.Log($"[ProgressionDebugMenu] state={manager.State} stage={manager.CurrentStage}/{manager.HighestStageReached} " +
                      $"wave={manager.Combat.CurrentWave} | {manager.Economy} | {manager.Ascension.DescribeMultipliers()} | " +
                      $"prestige: {prestige} | boost={(manager.Boost.IsActive ? manager.Boost.RemainingSeconds + "s" : "off")}");
        }

        private static GameManager FindRunningGameManager()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[ProgressionDebugMenu] Enter Play Mode first.");
                return null;
            }

            GameManager manager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            if (manager == null)
            {
                Debug.LogWarning("[ProgressionDebugMenu] No GameManager found in the loaded scene.");
            }

            return manager;
        }
    }
}