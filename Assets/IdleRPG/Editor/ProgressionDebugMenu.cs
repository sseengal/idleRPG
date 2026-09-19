using System.Text;
using UnityEditor;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Progression;
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

            builder.Append("  Token yield by highest stage: ");

            for (int stage = 10; stage <= 100; stage += 10)
            {
                builder.Append($"{stage}->{FormulaUtility.PrestigeTokenReward(stage, balance.PrestigeStageDivisor, balance.PrestigeExponent):0} ");
            }

            builder.AppendLine();
            Debug.Log(builder.ToString());
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

            GameManager manager = Object.FindAnyObjectByType<GameManager>();
            if (manager == null)
            {
                Debug.LogWarning("[ProgressionDebugMenu] No GameManager found in the loaded scene.");
            }

            return manager;
        }
    }
}