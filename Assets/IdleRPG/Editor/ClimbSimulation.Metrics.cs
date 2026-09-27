using System.Collections.Generic;
using System.Text;
using UnityEngine;
using IdleRPG.Combat;
using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Sim;
using IdleRPG.Utils;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Robot-player measurement helpers: median gaps, power/dps/enemy-HP projections and wall diagnosis.
    /// </summary>
    internal static partial class ClimbSimulation
    {

        /// <summary>
        /// Median gap between consecutive shopping trips. The mean would be dragged around by one long wall, and the
        /// question we care about is "how long does a typical stretch without a purchase last?".
        /// </summary>
        private static double MedianGap(List<double> stamps)
        {
            if (stamps == null || stamps.Count < 2)
            {
                return -1d;
            }

            List<double> gaps = new List<double>(stamps.Count - 1);
            for (int i = 1; i < stamps.Count; i++)
            {
                gaps.Add(stamps[i] - stamps[i - 1]);
            }

            gaps.Sort();
            int middle = gaps.Count / 2;

            return gaps.Count % 2 == 1
                ? gaps[middle]
                : (gaps[middle - 1] + gaps[middle]) * 0.5d;
        }

        private static void AppendStageRow(
            StringBuilder report,
            int stage,
            BalanceLabMenu.StageRun run,
            double elapsedSeconds,
            StatResolver resolver,
            PartyConfig party,
            int buys)
        {
            report.AppendLine(string.Format(
                "  {0,5}  {1,5:0.0}  {2,5}  {3,9}  {4,6:0.00}  {5,6:0.0}m  {6,6:0.00} {7,6:0.00} {8,6:0.00}  {9,5}",
                stage,
                run.Seconds,
                run.Kills,
                NumberFormatter.Format(run.Gold),
                run.Seconds <= 0d ? 0d : run.Gold / run.Seconds,
                elapsedSeconds / 60d,
                PowerRatio(resolver, party, HeroStatType.Attack),
                PowerRatio(resolver, party, HeroStatType.Health),
                PowerRatio(resolver, party, HeroStatType.Defense),
                buys));
        }

        /// <summary>Party stat divided by the same party with no upgrades: 1.00 means untouched base stats.</summary>
        private static double PowerRatio(StatResolver resolver, PartyConfig party, HeroStatType stat)
        {
            double current = 0d;
            double baseValue = 0d;

            for (int i = 0; i < party.ValidHeroCount; i++)
            {
                HeroData hero = party.GetHero(i);
                if (hero == null)
                {
                    continue;
                }

                switch (stat)
                {
                    case HeroStatType.Attack:
                        current += resolver.GetAttack(hero, i);
                        baseValue += hero.BaseAttack;
                        break;
                    case HeroStatType.Health:
                        current += resolver.GetMaxHealth(hero, i);
                        baseValue += hero.BaseHealth;
                        break;
                    default:
                        current += resolver.GetDefense(hero, i);
                        baseValue += hero.BaseDefense;
                        break;
                }
            }

            return baseValue <= 0d ? 0d : current / baseValue;
        }

        /// <summary>Raw party damage per second, before enemy DEF mitigation.</summary>
        private static double PartyDps(StatResolver resolver, PartyConfig party)
        {
            double dps = 0d;

            for (int i = 0; i < party.ValidHeroCount; i++)
            {
                HeroData hero = party.GetHero(i);
                if (hero == null)
                {
                    continue;
                }

                double interval = resolver.GetAttackInterval(hero, i);
                if (interval <= 0d)
                {
                    continue;
                }

                dps += resolver.GetAttack(hero, i) / interval;
            }

            return dps;
        }

        /// <summary>Total enemy HP of a whole stage (every wave + boss), read from the real encounter factory.</summary>
        private static double StageEnemyHealth(BalanceConfig balance, WaveConfig waves, int stage)
        {
            SimRules rules = SimRulesFactory.FromBalance(balance);
            double total = 0d;

            for (int wave = 1; wave <= balance.NormalWavesPerStage + 1; wave++)
            {
                bool isBoss = WaveConfig.IsBossWave(wave, balance.NormalWavesPerStage);
                EnemyCombatant[] team = EncounterFactory.Build(waves, balance, stage, wave, isBoss, rules);

                if (team == null)
                {
                    continue;
                }

                for (int i = 0; i < team.Length; i++)
                {
                    total += team[i].MaxHealth;
                }
            }

            return total;
        }

        /// <summary>Why the wall is a wall, and whether cashing in a prestige would actually break it.</summary>
        private static void AppendWallDiagnosis(
            StringBuilder report,
            BalanceConfig balance,
            WaveConfig waves,
            PartyConfig party,
            StatResolver resolver,
            PrestigeUpgradeData[] prestigeUpgrades,
            int stage,
            double survivedSeconds)
        {
            double stageHealth = StageEnemyHealth(balance, waves, stage);
            double dps = PartyDps(resolver, party);
            double rawSeconds = dps > 0d ? stageHealth / dps : 0d;
            double needFactor = survivedSeconds > 0d ? rawSeconds / survivedSeconds : 0d;

            report.AppendLine(string.Format(
                "  diagnosis stage {0}: {1} enemy HP total | raw party dps {2:0.#} (ignores enemy DEF) -> {3:0}s of damage | party survived {4:0}s => needs about x{5:0.0} more damage",
                stage, NumberFormatter.Format(stageHealth), dps, rawSeconds, survivedSeconds, needFactor));

            double tokens = FormulaUtility.PrestigeTokenReward(stage - 1, balance.PrestigeStageDivisor, balance.PrestigeExponent);
            PrestigeUpgradeData damage = FindPrestigeUpgrade(prestigeUpgrades, PrestigeEffectType.DamagePercent);

            if (damage == null)
            {
                report.AppendLine($"  prestige: stage {stage - 1} would pay {tokens:0} tokens (no Damage upgrade asset found)");
                return;
            }

            int levels = 0;
            double spent = 0d;

            while (levels < damage.MaxLevel)
            {
                double cost = FormulaUtility.StatUpgradeBulkCost(damage.BaseCostTokens, levels, 1, damage.CostGrowth);
                if (spent + cost > tokens)
                {
                    break;
                }

                spent += cost;
                levels++;
            }

            double multiplier = damage.GetMultiplier(levels);

            report.AppendLine(string.Format(
                "  prestige: stage {0} pays {1:0} tokens -> {2} Damage levels ({3:0} tokens) = x{4:0.00} damage. Verdict: {5}",
                stage - 1,
                tokens,
                levels,
                spent,
                multiplier,
                multiplier >= needFactor ? "prestige WOULD break this wall" : "prestige does NOT break this wall"));
        }

        private static PrestigeUpgradeData FindPrestigeUpgrade(PrestigeUpgradeData[] upgrades, PrestigeEffectType effectType)
        {
            if (upgrades == null)
            {
                return null;
            }

            for (int i = 0; i < upgrades.Length; i++)
            {
                if (upgrades[i] != null && upgrades[i].EffectType == effectType)
                {
                    return upgrades[i];
                }
            }

            return null;
        }
    }
}
