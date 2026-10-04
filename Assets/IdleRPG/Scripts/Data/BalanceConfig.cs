using System.Collections.Generic;
using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// Single source of truth for every tuning constant in the game.
    /// Nothing is hard-coded in code: designers edit this asset.
    /// </summary>
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "Idle RPG/Data/Balance Config", order = 10)]
    public partial class BalanceConfig : ScriptableObject
    {
        // ------------------------------------------------------------------
        // Enemy scaling
        // ------------------------------------------------------------------
        [Header("Enemy Scaling (per stage)")]
        [Tooltip("MaxHP = BaseHP * growth^(stage-1). Spec default 1.15")]
        [SerializeField] private float enemyHealthGrowth = 1.15f;

        [Tooltip("Gold = BaseGold * growth^(stage-1). Spec default 1.12")]
        [SerializeField] private float enemyGoldGrowth = 1.12f;

        [Tooltip("Enemy attack scaling. Spec did not define this.")]
        [SerializeField] private float enemyAttackGrowth = 1.08f;

        // ------------------------------------------------------------------
        // Waves & boss
        // ------------------------------------------------------------------
        [Header("Waves & Boss")]
        [Tooltip("Normal waves before a boss wave (design: 10 -> 1).")]
        [SerializeField] private int normalWavesPerStage = 10;

        [Tooltip("Smallest wave size. 1 = a lone enemy is possible. Boss waves always ignore this (always 1).")]
        [SerializeField] private int minEnemiesPerWave = 1;

        [Tooltip("Largest wave size. The battle page holds three stacked enemies, so it clamps to 3.")]
        [SerializeField] private int maxEnemiesPerWave = 3;

        [Tooltip("How often each wave size shows up. Recipe A: 1 x25, 2 x50, 3 x25 -> out of 20 fights " +
                 "5 singles / 10 doubles / 5 triples (average 2). Weights are relative; counts outside " +
                 "min..max are ignored.")]
        [SerializeField] private List<WaveCountWeight> waveCountWeights = new List<WaveCountWeight>
        {
            new WaveCountWeight(1, 25),
            new WaveCountWeight(2, 50),
            new WaveCountWeight(3, 25)
        };

        [Tooltip("Multi-enemy budget: total wave HP compared to a one-enemy wave. 1 = same clear time, " +
                 "each enemy gets 1/count of the pool.")]
        [SerializeField] private float waveHealthMultiplier = 1f;

        [Tooltip("Multi-enemy budget: total enemy attack compared to a one-enemy wave. 1 = same incoming DPS. " +
                 "Per-hit defence makes split attacks weaker, so >1 is the compensation knob.")]
        [SerializeField] private float waveAttackMultiplier = 1f;

        [Tooltip("Multi-enemy budget: total gold compared to a one-enemy wave. 1 = same gold per second.")]
        [SerializeField] private float waveGoldMultiplier = 1f;

        [Tooltip("Gems paid for the FIRST clear of a milestone stage (see milestoneStageInterval). " +
                 "Only a new-best stage pays, so bouncing between stages can never farm gems.")]
        [SerializeField] private int gemsPerMilestone = 5;

        [Tooltip("Every Nth stage pays gems on its first clear only. 5 = stages 5, 10, 15, 20 ...")]
        [SerializeField] private int milestoneStageInterval = 5;

        // ------------------------------------------
        // Daily streak (B7 S2)
        // ------------------------------------------
        [Header("Daily Streak (B7 S2)")]
        [Tooltip("Gems rewarded for each consecutive day, index 0 = day 1. The last entry is the cap: " +
                 "while the streak holds, every later day pays the cap amount.")]
        [SerializeField] private int[] dailyStreakGems = { 5, 8, 10, 12, 15, 18, 25 };

        [Tooltip("The streak-cap day (day 7 with the shipped table) also starts a short gold boost.")]
        [SerializeField] private bool dailyStreakDay7Boost = true;
// ------------------------------------------
        // Ad placements (B7 S3)
        // ------------------------------------------
        [Header("Ad Placements (B7 S3)")]
        [Tooltip("Per-day limits for each rewarded-ad placement: max redemptions per local day + minimum gap.")]
        [SerializeField] private List<AdPlacementDef> adPlacements = new List<AdPlacementDef>
        {
            new AdPlacementDef(AdPlacementId.GoldBoost, 5, 300f),
            new AdPlacementDef(AdPlacementId.DoubleOffline, 3, 30f)
        };

        [Tooltip("Delay between clearing a wave and spawning the next, in seconds.")]
        [SerializeField] private float waveTransitionDelaySec = 0.5f;

        // ------------------------------------------------------------------
        // Combat maths
        // ------------------------------------------------------------------
        [Header("Combat Maths")]
        [Tooltip("Damage = Max(attack - defense, attack * this). Prevents '0 damage' stalemates.")]
        [Range(0f, 1f)]
        [SerializeField] private float minDamageRatio = 0.15f;

        [Tooltip("Critical hit chance for hero attacks. MVP: 5%.")]
        [Range(0f, 1f)]
        [SerializeField] private float criticalChance = 0.05f;

        [Tooltip("Damage multiplier applied on a critical hit. MVP: x2.")]
        [SerializeField] private float criticalDamageMultiplier = 2f;

        [Tooltip("Heal heroes to full HP whenever a new stage begins.")]
        [SerializeField] private bool healHeroesOnStageAdvance = true;

        [Tooltip("Also heal heroes at the start of every wave. Off: damage carries across the stage.")]
        [SerializeField] private bool reviveHeroesEachWave = false;

        // ------------------------------------------------------------------
        // Encounter flow
        // ------------------------------------------------------------------
        [Header("Encounter Flow")]
        [Tooltip("Which hero the current enemy attacks.")]
        [SerializeField] private EnemyTargetingMode enemyTargeting = EnemyTargetingMode.FrontMost;

        [Tooltip("Fixed simulation step of the combat ticker, in seconds.")]
        [SerializeField] private float combatTickIntervalSec = 0.05f;

        [Tooltip("Stages lost when the party wipes. Design spec: drop back 1 stage.")]
        [SerializeField] private int stageRollbackOnDefeat = 1;

        [Tooltip("Beat between a wipe and the automatic fallback fight, in seconds. " +
                 "The party always keeps fighting - the loop never waits for input.")]
        [SerializeField] private float defeatPauseSeconds = 0.75f;

        [Tooltip("Also scale enemy defence with the stage.")]
        [SerializeField] private bool scaleEnemyDefenseWithStage = false;

        [Tooltip("Print combat and wave events to the console.")]
        [SerializeField] private bool logCombatToConsole = true;

        [Tooltip("GAME PACE. Multiplies every unit's attack interval (heroes AND enemies). " +
                 "1 = shipped speeds, 1.6 = ~60% slower. Wall-clock only: damage, health, gold and " +
                 "upgrade ratios are untouched, so raising difficulty never needs a rebalance.")]
        [Range(0.25f, 10f)]
        [SerializeField] private float combatPaceMultiplier = 1.6f;

        // ------------------------------------------------------------------
        // Ascension
        // ------------------------------------------------------------------
        [Tooltip("Classic prestige loop: hero ATK/HP/DEF levels reset to 0 on ascension. " +
                 "Off = hero levels are permanent (tokens become a pure bonus layer).")]
        [SerializeField] private bool resetHeroLevelsOnAscension = true;

        // ------------------------------------------------------------------
        // Stat upgrades
        // ------------------------------------------------------------------
        [Header("Stat Upgrades")]
        [Tooltip("Fallback cost growth when a StatUpgradeData asset leaves it unset. Spec default 1.07")]
        [SerializeField] private float upgradeCostGrowth = 1.07f;

        [Tooltip("Fallback bonus per level (fraction of the hero base stat). 0.1 = +10% of base per level.")]
        [SerializeField] private float upgradeStatGainPerLevel = 0.1f;

        // ------------------------------------------------------------------
        // Prestige / ascension
        // ------------------------------------------------------------------
        [Header("Prestige")]
        [Tooltip("Tokens = floor((HighestStage / divisor)^exponent). Spec default 10 / 1.5")]
        [SerializeField] private float prestigeStageDivisor = 10f;

        [SerializeField] private float prestigeExponent = 1.5f;

        [Tooltip("Ascension is only allowed once the highest stage reaches this value.")]
        [SerializeField] private int minStageToAscend = 10;

        // ------------------------------------------------------------------
        // Offline progress
        // ------------------------------------------------------------------
        [Header("Offline Progress")]
        [Tooltip("Offline earnings are capped at 8 hours per spec.")]
        [SerializeField] private float offlineCapSeconds = 28800f;

        [Tooltip("Offline Gold = seconds * goldPerSecond * efficiency. Spec default 0.7")]
        [Range(0f, 1f)]
        [SerializeField] private float offlineEfficiency = 0.7f;

        [Tooltip("Economy cap: offline pays for at most this many seconds of battle income.")]
        [SerializeField] private float offlineMaxEquivalentSeconds = 7200f;

        [Tooltip("Fallback kill time (seconds) used to estimate gold/sec before any live sample exists.")]
        [SerializeField] private float offlineEstimatedSecondsPerKill = 3.6f;

        [Tooltip("Seconds to ignore. Prevents instant popups after a quick app switch.")]
        [SerializeField] private float minOfflineSecondsForPopup = 30f;

        [Tooltip("Gem sink: seconds of extra offline income cap per purchase (3600 = +1h).")]
        [SerializeField] private float offlineCapExtensionSeconds = 3600f;

        [Tooltip("Gem sink: how much extra cap one purchase costs.")]
        [SerializeField] private double offlineCapExtensionGemCost = 50d;

        [Tooltip("Gem sink: ceiling on the total extra cap (10800 = +3h).")]
        [SerializeField] private float offlineCapExtensionMaxSeconds = 10800f;

        [Tooltip("Gem sink: seconds of income bought outright by one fast-forward purchase.")]
        [SerializeField] private float instantIncomeSeconds = 3600f;

        [Tooltip("Gem sink: gems charged per fast-forward purchase (repeatable, no cap).")]
        [SerializeField] private double instantIncomeGemCost = 30d;

        // ------------------------------------------------------------------
        // Economy & monetisation
        // ------------------------------------------------------------------
        [Header("Economy")]
        [SerializeField] private double startingGold = 0d;

        [SerializeField] private double startingGems = 0d;

        [Tooltip("Rolling window (seconds) used to measure live gold/sec for offline estimates.")]
        [SerializeField] private float goldPerSecondSampleWindowSec = 60f;

        [Header("Ad Boost")]
        [SerializeField] private double adGoldBoostMultiplier = 2d;

        [SerializeField] private float adGoldBoostDurationSec = 3600f;

        // ------------------------------------------------------------------
        // Equipment (gear from boss drops)
        // ------------------------------------------------------------------
        [Header("Equipment")]
        [Tooltip("Chance each cleared boss drops a piece of gear (0.35 = 35%).")]
        [SerializeField] private float bossGearDropChance = 0.35f;

        [Tooltip("How many unequipped items the bag can hold before drops start being overwritten.")]
        [SerializeField] private int inventoryCap = 20;

        [Tooltip("Gear stat formula (a fraction of the hero BASE stat): baseFraction + perStage * itemLevel, capped."
                 + " 0.015 = +1.5% at level 0.")]
        [SerializeField] private float gearStatBaseFraction = 0.015f;

        [Tooltip("Adds this fraction of a percent per item level to the base stat formula (0.0012 = +0.12%/level).")]
        [SerializeField] private float gearStatPerStage = 0.0012f;

        [Tooltip("Upper bound on one item's stat bonus before rarity (0.25 = +25% of base).")]
        [SerializeField] private float gearStatMaxFraction = 0.25f;

        [Tooltip("Rarity weights for a drop (Common / Rare / Epic / Legendary). Relative, must stay in order.")]
        [SerializeField] private float[] gearRarityWeights = { 55f, 28f, 12f, 5f };

        [Tooltip("Gold price of a Common item at stage 1 (2 + 0.9 * level, times rarity multiplier).")]
        [SerializeField] private double gearPriceBase = 2d;

        [Tooltip("Price grows this much per item level (0.9 = +0.9 gold/level).")]
        [SerializeField] private double gearPricePerStage = 0.9d;

        [Tooltip("Fraction of the item's price paid by auto-salvage (0.4 = 40%).")]
        [SerializeField] private float gearSalvageFraction = 0.4f;

        // ------------------------------------------------------------------
        // Save
        // ------------------------------------------------------------------
        [Header("Save")]
        [Tooltip("Seconds between automatic saves while playing.")]
        [SerializeField] private float autosaveIntervalSec = 15f;

        // ------------------------------------------------------------------
        // Accessors (clamped so a bad inspector value can never break the sim)
        // ------------------------------------------------------------------
        public float GearDropChance => Mathf.Clamp01(bossGearDropChance);

        public int InventoryCap => Mathf.Max(1, inventoryCap);

        public float GearStatBaseFraction => Mathf.Max(0f, gearStatBaseFraction);

        public float GearStatPerStage => Mathf.Max(0f, gearStatPerStage);

        public float GearStatMaxFraction => Mathf.Max(0f, gearStatMaxFraction);

        public float[] GearRarityWeights => gearRarityWeights != null ? gearRarityWeights : System.Array.Empty<float>();

        public double GearPriceBase => gearPriceBase < 0d ? 0d : gearPriceBase;

        public double GearPricePerStage => gearPricePerStage < 0d ? 0d : gearPricePerStage;

        public float GearSalvageFraction => Mathf.Clamp01(gearSalvageFraction);

        public float EnemyHealthGrowth => Mathf.Max(1f, enemyHealthGrowth);

        public float EnemyGoldGrowth => Mathf.Max(1f, enemyGoldGrowth);

        public float EnemyAttackGrowth => Mathf.Max(1f, enemyAttackGrowth);

        public EnemyTargetingMode EnemyTargeting => enemyTargeting;

        public float CombatTickIntervalSec => Mathf.Clamp(combatTickIntervalSec, 0.01f, 0.5f);

        public int StageRollbackOnDefeat => Mathf.Max(0, stageRollbackOnDefeat);

        /// <summary>Seconds between a wipe and the automatic fallback fight (0 = instant).</summary>
        public float DefeatPauseSeconds => Mathf.Max(0f, defeatPauseSeconds);

        public bool ScaleEnemyDefenseWithStage => scaleEnemyDefenseWithStage;

        public bool LogCombatToConsole => logCombatToConsole;

        /// <summary>Wall-clock speed of combat. 1 = fastest, higher = slower.</summary>
        public float CombatPaceMultiplier => Mathf.Clamp(combatPaceMultiplier, 0.25f, 10f);

        public bool HealHeroesOnStageAdvance => healHeroesOnStageAdvance;

        public bool ResetHeroLevelsOnAscension => resetHeroLevelsOnAscension;

        public int NormalWavesPerStage => Mathf.Max(1, normalWavesPerStage);

        /// <summary>Hard ceiling on enemies in one wave: the battle page stacks three slots and the sim agrees.</summary>
        public const int HardEnemyCap = 3;

        /// <summary>Smallest wave size (1 = a lone enemy is possible).</summary>
        public int MinEnemiesPerWave => Mathf.Clamp(minEnemiesPerWave, 1, HardEnemyCap);

        /// <summary>Largest wave size.</summary>
        public int MaxEnemiesPerWave => Mathf.Clamp(maxEnemiesPerWave, MinEnemiesPerWave, HardEnemyCap);

        /// <summary>The wave-size recipe (see <see cref="WaveCountWeight"/>). Never null.</summary>
        public List<WaveCountWeight> WaveCountWeights => waveCountWeights != null ? waveCountWeights : new List<WaveCountWeight>();

        /// <summary>
        /// Average wave size implied by the recipe. The offline estimator uses this (not the max), so a varied
        /// wave size cannot inflate the away-time payout.
        /// </summary>
        public double MeanEnemiesPerWave
        {
            get
            {
                double weighted = 0d;
                double total = 0d;

                List<WaveCountWeight> recipe = WaveCountWeights;

                for (int i = 0; i < recipe.Count; i++)
                {
                    WaveCountWeight entry = recipe[i];

                    if (entry.weight <= 0 || entry.count < MinEnemiesPerWave || entry.count > MaxEnemiesPerWave)
                    {
                        continue;
                    }

                    weighted += entry.count * (double)entry.weight;
                    total += entry.weight;
                }

                if (total <= 0d)
                {
                    return (MinEnemiesPerWave + MaxEnemiesPerWave) * 0.5d;
                }

                return weighted / total;
            }
        }

        /// <summary>Editor-only: writes the recipe and the bounds (used by the data generator).</summary>
        public void EditorSetWaveCounts(int min, int max, params WaveCountWeight[] recipe)
        {
            minEnemiesPerWave = min;
            maxEnemiesPerWave = max;

            if (recipe != null && recipe.Length > 0)
            {
                waveCountWeights = new List<WaveCountWeight>(recipe);
            }
        }

        /// <summary>Total wave HP versus a one-enemy wave (1 = idle parity).</summary>
        public float WaveHealthMultiplier => Mathf.Max(0.05f, waveHealthMultiplier);

        /// <summary>Total wave attack versus a one-enemy wave (1 = same incoming DPS).</summary>
        public float WaveAttackMultiplier => Mathf.Max(0.05f, waveAttackMultiplier);

        /// <summary>Total wave gold versus a one-enemy wave (1 = same gold per second).</summary>
        public float WaveGoldMultiplier => Mathf.Max(0.05f, waveGoldMultiplier);

        public int GemsPerMilestone => Mathf.Max(0, gemsPerMilestone);

        /// <summary>Stages between gem milestones (5 = stages 5, 10, 15 ...). Never below 1.</summary>
        public int MilestoneStageInterval => Mathf.Max(1, milestoneStageInterval);

        // ------------------------------------------------------------------
        // Daily streak (B7 S2)
        // ------------------------------------------------------------------
    }
}
