using System;

namespace IdleRPG.Progression
{
    /// <summary>
    /// Pure, side-effect free progression maths. No UnityEngine dependency so the
    /// exact same code can run in the editor, in-game, or in offline simulations.
    /// </summary>
    public static class FormulaUtility
    {
        /// <summary>Enemy MaxHP = BaseHP * growth^(stage-1).</summary>
        public static double EnemyMaxHealth(double baseHealth, int stage, double healthGrowth)
        {
            return ScaleByStage(baseHealth, stage, healthGrowth);
        }

        /// <summary>Enemy Gold drop = BaseGold * growth^(stage-1).</summary>
        public static double EnemyGoldDrop(double baseGold, int stage, double goldGrowth)
        {
            return ScaleByStage(baseGold, stage, goldGrowth);
        }

        /// <summary>Enemy attack = BaseAttack * growth^(stage-1).</summary>
        public static double EnemyAttack(double baseAttack, int stage, double attackGrowth)
        {
            return ScaleByStage(baseAttack, stage, attackGrowth);
        }

        /// <summary>
        /// Shared exponential stage scaling: value * growth^(stage - 1).
        /// Stage is clamped to >= 1 so stage 1 returns the base value exactly.
        /// </summary>
        public static double ScaleByStage(double baseValue, int stage, double growthPerStage)
        {
            if (baseValue <= 0d)
            {
                return 0d;
            }

            int safeStage = stage < 1 ? 1 : stage;
            double safeGrowth = growthPerStage < 1d ? 1d : growthPerStage;

            return baseValue * Math.Pow(safeGrowth, safeStage - 1);
        }

        /// <summary>Stat upgrade cost = BaseCost * growth^level.</summary>
        public static double StatUpgradeCost(double baseCost, int level, double costGrowth)
        {
            if (baseCost <= 0d)
            {
                return 0d;
            }

            int safeLevel = level < 0 ? 0 : level;
            double safeGrowth = costGrowth < 1d ? 1d : costGrowth;

            return baseCost * Math.Pow(safeGrowth, safeLevel);
        }

        /// <summary>
        /// Total cost of buying <paramref name="count"/> levels starting at <paramref name="startLevel"/>.
        /// Closed-form geometric series so +10 / +100 purchases stay cheap to compute.
        /// </summary>
        public static double StatUpgradeBulkCost(double baseCost, int startLevel, int count, double costGrowth)
        {
            if (baseCost <= 0d || count <= 0)
            {
                return 0d;
            }

            int safeLevel = startLevel < 0 ? 0 : startLevel;
            double safeGrowth = costGrowth < 1d ? 1d : costGrowth;
            double firstCost = baseCost * Math.Pow(safeGrowth, safeLevel);

            // r == 1 -> simple multiplication, avoids a division by zero.
            if (Math.Abs(safeGrowth - 1d) < double.Epsilon)
            {
                return firstCost * count;
            }

            double sum = firstCost * (Math.Pow(safeGrowth, count) - 1d) / (safeGrowth - 1d);
            return sum <= 0d ? 0d : sum;
        }

        /// <summary>
        /// Final hero stat: base * (1 + level * gainPerLevelFraction) * globalMultiplier.
        /// Additive per level, then scaled by any global prestige multiplier.
        /// </summary>
        public static double HeroStatValue(double baseStat, int level, double gainPerLevelFraction, double globalMultiplier = 1d)
        {
            if (baseStat <= 0d)
            {
                return 0d;
            }

            int safeLevel = level < 0 ? 0 : level;
            double safeGain = gainPerLevelFraction < 0d ? 0d : gainPerLevelFraction;
            double safeMultiplier = globalMultiplier < 0d ? 0d : globalMultiplier;

            return baseStat * (1d + safeLevel * safeGain) * safeMultiplier;
        }

        /// <summary>Prestige tokens = floor((highestStage / divisor)^exponent).</summary>
        public static double PrestigeTokenReward(int highestStage, double stageDivisor, double exponent)
        {
            if (highestStage <= 0)
            {
                return 0d;
            }

            double safeDivisor = stageDivisor <= 0d ? 1d : stageDivisor;
            double safeExponent = exponent < 0d ? 0d : exponent;

            double raw = Math.Pow(highestStage / safeDivisor, safeExponent);
            if (double.IsNaN(raw) || double.IsInfinity(raw))
            {
                return 0d;
            }

            return Math.Floor(raw);
        }

        /// <summary>
        /// Offline Gold = seconds * goldPerSecond * efficiency, with seconds capped.
        /// </summary>
        public static double OfflineGold(double offlineSeconds, double goldPerSecond, double efficiency, double capSeconds)
        {
            if (offlineSeconds <= 0d || goldPerSecond <= 0d)
            {
                return 0d;
            }

            double safeCap = capSeconds < 0d ? 0d : capSeconds;
            double cappedSeconds = offlineSeconds > safeCap ? safeCap : offlineSeconds;
            double safeEfficiency = efficiency < 0d ? 0d : efficiency;

            return cappedSeconds * goldPerSecond * safeEfficiency;
        }

        /// <summary>
        /// Damage after defence: Max(attack - defence, attack * minDamageRatio).
        /// The floor stops high-defence enemies from stalling the run entirely.
        /// </summary>
        public static double Damage(double attack, double defense, double minDamageRatio, double damageMultiplier = 1d)
        {
            if (attack <= 0d)
            {
                return 0d;
            }

            double safeDefense = defense < 0d ? 0d : defense;
            double safeRatio = minDamageRatio < 0d ? 0d : (minDamageRatio > 1d ? 1d : minDamageRatio);
            double safeMultiplier = damageMultiplier < 0d ? 0d : damageMultiplier;

            double mitigated = attack - safeDefense;
            double floor = attack * safeRatio;
            double result = mitigated < floor ? floor : mitigated;

            return result * safeMultiplier;
        }

        /// <summary>
        /// Estimated gold per second at a stage. Used as the offline fallback when no
        /// live sample has been recorded yet.
        /// </summary>
        public static double EstimatedGoldPerSecond(double baseGold, int stage, double goldGrowth, double secondsPerKill, double enemiesPerWave)
        {
            double safeEnemies = enemiesPerWave < 1d ? 1d : enemiesPerWave;
            double safeSeconds = secondsPerKill < 0.1d ? 0.1d : secondsPerKill;
            double goldPerKill = EnemyGoldDrop(baseGold, stage, goldGrowth) * safeEnemies;
            return goldPerKill / safeSeconds;
        }

        /// <summary>Safety net: returns a finite, non-negative value.</summary>
        public static double Sanitize(double value, double fallback = 0d)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return fallback;
            }

            return value < 0d ? 0d : value;
        }
    }
}