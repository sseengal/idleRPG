using System;

namespace IdleRPG.Progression
{
    /// <summary>
    /// How a per-level stat gain composes.
    ///
    /// <para>AdditiveBase - value = base * (1 + level * gain). Linear in level (the shipped MVP model).</para>
    /// <para>Multiplicative - value = base * (1 + gain)^level. Exponential in level.</para>
    ///
    /// <para>Why the mode exists (B3d): enemy HP grows x1.15 per stage and affordable levels grow only
    /// logarithmically with gold, so additive power grows at most linearly in stage while content grows
    /// exponentially - the frontier then has a hard ceiling that no cost tuning can move. Compounding is the
    /// only model that races it.</para>
    ///
    /// <para>The compounding gain is <b>derived, not chosen</b>. Power per stage works out as
    /// goldGrowth^(ln(1 + gain) / ln(costGrowth)), so the gain that makes affordable power grow at exactly the
    /// content rate is <b>exp(ln(contentGrowth) x ln(costGrowth) / ln(goldGrowth)) - 1</b>; for the shipped
    /// 1.15 / 1.07 / 1.12 that is <b>0.087</b> (x1.087). The data uses 0.09 (x1.09) on purpose - about 0.5% per
    /// stage ahead of content, which is the margin that keeps prestige and wave luck from re-stalling the climb.
    /// 1.08 stalls, 1.10 runs away. Change any of the three inputs and this number must be recomputed
    /// (<see cref="FormulaUtility.CompoundingGainFor"/>).</para>
    /// </summary>
    public enum StatEffectMode
    {
        /// <summary>base * (1 + level * gain) - linear. Default, keeps old saves/numbers identical.</summary>
        AdditiveBase = 0,

        /// <summary>base * (1 + gain)^level - compounding.</summary>
        Multiplicative = 1
    }

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
        /// Final hero stat, additive model (shipped): base * (1 + level * gainPerLevelFraction) * globalMultiplier.
        /// Kept as the default entry point so existing callers and numbers are untouched.
        /// </summary>
        public static double HeroStatValue(double baseStat, int level, double gainPerLevelFraction, double globalMultiplier = 1d)
        {
            return HeroStatValue(baseStat, level, gainPerLevelFraction, globalMultiplier, StatEffectMode.AdditiveBase);
        }

        /// <summary>
        /// Final hero stat: base * perLevel(level, gain) * globalMultiplier, where perLevel is
        /// (1 + level * gain) for <see cref="StatEffectMode.AdditiveBase"/> and (1 + gain)^level for
        /// <see cref="StatEffectMode.Multiplicative"/>. Level 0 returns the base stat in both modes.
        /// </summary>
        public static double HeroStatValue(double baseStat, int level, double gainPerLevelFraction,
            double globalMultiplier, StatEffectMode mode)
        {
            if (baseStat <= 0d)
            {
                return 0d;
            }

            int safeLevel = level < 0 ? 0 : level;
            double safeGain = gainPerLevelFraction < 0d ? 0d : gainPerLevelFraction;
            double safeMultiplier = globalMultiplier < 0d ? 0d : globalMultiplier;

            double byLevel = mode == StatEffectMode.Multiplicative
                ? Math.Pow(1d + safeGain, safeLevel)
                : 1d + safeLevel * safeGain;

            return baseStat * byLevel * safeMultiplier;
        }

        /// <summary>
        /// The per-level compounding gain that makes affordable power grow at exactly the content rate.
        ///
        /// Derivation: levels affordable grow like log_1.07(gold), so power grows like
        /// gold^(ln(1 + gain) / ln(costGrowth)). Gold grows `goldGrowthPerStage` per stage, so power per stage is
        /// goldGrowth^(ln(1 + gain) / ln(costGrowth)). Setting that equal to the content rate and solving gives
        /// the fraction returned here: exp(ln(contentGrowth) x ln(costGrowth) / ln(goldGrowth)) - 1.
        ///
        /// With the shipped 1.15 / 1.07 / 1.12 this returns 0.087, so a track should use ~0.087-0.09 - the small
        /// excess over the exact value is deliberate design margin, not slop.
        /// </summary>
        public static double CompoundingGainFor(double contentGrowthPerStage, double costGrowthPerLevel,
            double goldGrowthPerStage)
        {
            if (contentGrowthPerStage <= 1d || costGrowthPerLevel <= 1d || goldGrowthPerStage <= 1d)
            {
                return 0d;
            }

            double gain = Math.Exp(Math.Log(contentGrowthPerStage) * Math.Log(costGrowthPerLevel) /
                                   Math.Log(goldGrowthPerStage)) - 1d;

            return gain <= 0d ? 0d : gain;
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
        /// <summary>
        /// Time-based gold, shared by every time payout in the game: seconds * goldPerSecond * efficiency,
        /// with the seconds clamped to a cap. The offline window and instant income both call this, so the two
        /// can never disagree about the discount.
        /// </summary>
        public static double TimeBasedGold(double seconds, double goldPerSecond, double efficiency, double capSeconds)
        {
            if (seconds <= 0d || goldPerSecond <= 0d)
            {
                return 0d;
            }

            double safeCap = capSeconds < 0d ? 0d : capSeconds;
            double cappedSeconds = seconds > safeCap ? safeCap : seconds;
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