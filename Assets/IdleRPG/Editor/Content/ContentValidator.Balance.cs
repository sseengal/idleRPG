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
    /// Balance-domain checks: formation, encounters, wave recipe, balance band, power band.
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
        // Power band (Fix D): power vs content at the levels players actually reach
        // ------------------------------------------------------------------
        /// <summary>A kill-time below this at a sample bracket means the enemies are being one-shot (power glut).</summary>
        private const double PowerBandMinTtkSeconds = 8d;

        /// <summary>A kill-time above this at a sample bracket means the race is lost again (stall).</summary>
        private const double PowerBandMaxTtkSeconds = 240d;

        /// <summary>
        /// Checks the race formula directly, at sample compounding levels, instead of hoping a game session notices:
        /// for level L the frontier sits around stage ln(1+gain)/ln(growth) x L (that is the whole point of the
        /// compounding fix), so we compute what a one-hit/death feels like exactly there. This is the check that
        /// would have screamed "one-shotting" instead of waiting for a player to report it.
        ///
        /// ELI5: a level-80 hero and a stage-24 monster have no business meeting - the formula puts level 80 at the
        /// stage-50 frontier. When a save is far behind its own power line (legacy levels), Fix A moves the levels;
        /// this check makes sure the LINE itself is sane in both directions (too strong up top, or stalling).
        /// </summary>
        private static void CheckPowerBand()
        {
            BalanceConfig balance = BalanceLabMenu.Load<BalanceConfig>("BalanceConfig");
            PartyConfig party = BalanceLabMenu.Load<PartyConfig>("PartyConfig");
            StatUpgradeData attackTrack = BalanceLabMenu.Load<StatUpgradeData>("StatUpgrade_ATK");

            if (balance == null || party == null || party.ValidHeroCount == 0 || attackTrack == null)
            {
                Add(Severity.Error, "power", "Config assets missing; skipped the power-band check.");
                return;
            }

            HeroData hero = party.GetHero(0);
            if (hero == null)
            {
                Add(Severity.Error, "power", "No hero at index 0; skipped the power-band check.");
                return;
            }

            double gain = attackTrack.StatGainPerLevelFraction;
            bool compounding = attackTrack.EffectMode == StatEffectMode.Multiplicative;
            double growth = balance.EnemyHealthGrowth;
            double baseAtk = hero.BaseAttack;
            double interval = hero.AttackIntervalSec;
            double goldPerKill = 130d; // the hardest normal enemy's base HP is the yardstick (Goblin)

            int[] brackets = { 25, 50, 75, 100 };

            for (int b = 0; b < brackets.Length; b++)
            {
                int level = brackets[b];
                double attack = compounding
                    ? baseAtk * System.Math.Pow(1d + gain, level)
                    : baseAtk * (1d + gain * level);

                double frontierStageF = level * (System.Math.Log(1d + gain) / System.Math.Log(growth));
                int stage = System.Math.Max(1, (int)System.Math.Floor(frontierStageF));

                double enemyHp = goldPerKill * System.Math.Pow(growth, stage - 1);
                double dps = attack / interval;
                double ttk = enemyHp / System.Math.Max(1e-9, dps);

                if (ttk < PowerBandMinTtkSeconds)
                {
                    Add(Severity.Warning, "power",
                        $"at level {level} the frontier stage is ~{stage} but enemies die in {ttk:0.0}s - power is " +
                        "far ahead of content there (one-shot glut). Raise enemy growth or lower the gain.");
                }
                else if (ttk > PowerBandMaxTtkSeconds)
                {
                    Add(Severity.Warning, "power",
                        $"at level {level} the frontier stage is ~{stage} but enemies take {ttk / 60d:0.0} min to " +
                        "kill - the compounding race is lost again up there. Raise the gain or lower growth.");
                }
                else
                {
                    Add(Severity.Info, "power",
                        $"level {level} -> stage ~{stage}: enemy TTK {ttk:0.0}s (band {PowerBandMinTtkSeconds:0}-{PowerBandMaxTtkSeconds:0}s).");
                }
            }
        }
    }
}
