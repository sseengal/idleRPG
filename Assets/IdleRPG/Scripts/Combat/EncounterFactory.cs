using System.Collections.Generic;
using IdleRPG.Data;
using IdleRPG.Sim;

namespace IdleRPG.Combat
{
    /// <summary>
    /// Builds the enemy side of one wave: how many enemies, which archetypes, which ranks, and how the wave's
    /// HP / attack / gold budget is split between them.
    ///
    /// ELI5: one enemy per wave is easy - just scale its numbers by the stage. A *wave* is harder: three goblins
    /// must not mean three times the health, three times the damage and three times the gold, or the idle rate
    /// falls apart. So this is the shopper: it takes the wave's enemies, keeps their individual flavour, and
    /// ratios the group back to the budget a single-enemy wave would have had (`BalanceConfig.Wave*Multiplier`).
    ///
    /// With one enemy the ratios are exactly 1 and nothing is rescaled, which is what keeps the MVP numbers
    /// byte-identical while the multi-enemy feature is built.
    /// </summary>
    public static class EncounterFactory
    {
        /// <summary>Ceiling on one wave's team size (the portrait layout and the sim cap agree on three).</summary>
        public const int MaxEnemiesPerWave = BalanceConfig.MaxEnemiesPerWave;

        /// <summary>Builds the enemy team for a wave. Returns null when the wave has no enemy to spawn.</summary>
        /// <param name="stage">Stage the enemies are scaled to.</param>
        /// <param name="wave">Wave inside the stage (1-based).</param>
        /// <param name="isBoss">Boss waves are always a single enemy (design: bosses are one big fight).</param>
        /// <param name="rules">Frozen tuning snapshot - passed in so the factory never reads a ScriptableObject
        /// the fight is not already using.</param>
        /// <param name="countOverride">Force a team size (probes/tools). 0 or less = use
        /// <see cref="BalanceConfig.EnemiesPerWave"/>.</param>
        public static EnemyCombatant[] Build(
            WaveConfig waves, BalanceConfig balance, int stage, int wave, bool isBoss, SimRules rules, int countOverride = 0)
        {
            if (waves == null)
            {
                SimLog.LogError("[EncounterFactory] No WaveConfig; cannot build a wave.");
                return null;
            }

            int normalWaves = balance != null ? balance.NormalWavesPerStage : 10;
            int wanted = countOverride > 0 ? countOverride : (balance != null ? balance.EnemiesPerWave : 1);

            if (isBoss)
            {
                wanted = 1;
            }

            wanted = Clamp(wanted, 1, MaxEnemiesPerWave);

            List<EnemyData> picks = waves.GetEnemiesFor(stage, wave, normalWaves, wanted);

            if (picks == null || picks.Count == 0)
            {
                SimLog.LogError($"[EncounterFactory] Stage {stage} wave {wave} has no enemy to spawn.");
                return null;
            }

            EnemyCombatant[] team = new EnemyCombatant[picks.Count];

            for (int i = 0; i < picks.Count; i++)
            {
                EnemyCombatant enemy = EnemyCombatant.Create(picks[i], stage, isBoss, rules, 1d);

                if (enemy == null)
                {
                    SimLog.LogError($"[EncounterFactory] Enemy '{picks[i].EnemyID}' failed to build.");
                    return null;
                }

                enemy.TargetRule = MapRule(picks[i].TargetRule);
                team[i] = enemy;
            }

            ApplyWaveBudget(team, balance);

            return team;
        }

        /// <summary>
        /// Maps a data-layer targeting mode onto the sim's rule. Returns null for
        /// <see cref="EnemyTargetingMode.Inherit"/>, which means "use the wave's global rule".
        /// </summary>
        public static TargetRule? MapRule(EnemyTargetingMode mode)
        {
            switch (mode)
            {
                case EnemyTargetingMode.FrontMost:
                    return TargetRule.FrontMost;
                case EnemyTargetingMode.LowestHealthPercent:
                    return TargetRule.LowestHealthPercent;
                case EnemyTargetingMode.Random:
                    return TargetRule.Random;
                case EnemyTargetingMode.BacklineFirst:
                    return TargetRule.BacklineFirst;
                default:
                    return null;
            }
        }

        /// <summary>One readable line for tools and logs ("Goblin#0 60hp 12atk 18g | Bat#1 ...").</summary>
        public static string Describe(EnemyCombatant[] team)
        {
            if (team == null || team.Length == 0)
            {
                return "(no enemies)";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            for (int i = 0; i < team.Length; i++)
            {
                EnemyCombatant enemy = team[i];

                if (enemy == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append($"{enemy.DisplayName}#{i} ")
                       .Append($"{enemy.MaxHealth:0.#}hp {enemy.Attack:0.#}atk {enemy.GoldReward:0.#}g");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Ratios the team's totals back to a one-enemy wave's budget, keeping the relative beefiness of each
        /// archetype (a goblin stays chunkier than a slime). Defence is deliberately left alone: armour is part of
        /// an archetype's identity, and the HP multiplier is the knob that compensates for it.
        /// </summary>
        private static void ApplyWaveBudget(EnemyCombatant[] team, BalanceConfig balance)
        {
            if (balance == null || team.Length <= 1)
            {
                return;   // a single enemy *is* the budget - and the reason the MVP numbers do not move
            }

            double healthSum = 0d;
            double attackSum = 0d;
            double goldSum = 0d;

            for (int i = 0; i < team.Length; i++)
            {
                healthSum += team[i].MaxHealth;
                attackSum += team[i].Attack;
                goldSum += team[i].GoldReward;
            }

            if (healthSum <= 0d || attackSum <= 0d)
            {
                SimLog.LogWarning("[EncounterFactory] Enemy team has no health or attack; skipping the budget split.");
                return;
            }

            // Primary = the enemy this wave would have spawned on its own, so "the budget" has one definition.
            double healthFactor = team[0].MaxHealth * balance.WaveHealthMultiplier / healthSum;
            double attackFactor = team[0].Attack * balance.WaveAttackMultiplier / attackSum;
            double goldFactor = goldSum > 0d ? team[0].GoldReward * balance.WaveGoldMultiplier / goldSum : 0d;

            for (int i = 0; i < team.Length; i++)
            {
                EnemyCombatant enemy = team[i];

                if (healthFactor != 1d || attackFactor != 1d)
                {
                    enemy.ApplyStats(
                        new StatBlock(enemy.MaxHealth * healthFactor, enemy.Attack * attackFactor, enemy.Defense),
                        enemy.AttackIntervalSec);
                }

                if (goldFactor != 0d)
                {
                    enemy.GoldReward = enemy.GoldReward * goldFactor;
                }
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
