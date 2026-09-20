using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// Single source of truth for every tuning constant in the game.
    /// Nothing is hard-coded in code: designers edit this asset.
    /// </summary>
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "Idle RPG/Data/Balance Config", order = 10)]
    public class BalanceConfig : ScriptableObject
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

        [Tooltip("Number of enemies spawned per normal wave. MVP uses 1 for lane clarity.")]
        [SerializeField] private int enemiesPerWave = 1;

        [Tooltip("Gem reward for killing a stage boss. Spec left the gem source undefined.")]
        [SerializeField] private int gemsPerBossKill = 1;

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

        [Tooltip("Turn auto-retry off after a defeat (design spec).")]
        [SerializeField] private bool resetAutoRetryOnDefeat = true;

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
        // Save
        // ------------------------------------------------------------------
        [Header("Save")]
        [Tooltip("Seconds between automatic saves while playing.")]
        [SerializeField] private float autosaveIntervalSec = 15f;

        // ------------------------------------------------------------------
        // Accessors (clamped so a bad inspector value can never break the sim)
        // ------------------------------------------------------------------
        public float EnemyHealthGrowth => Mathf.Max(1f, enemyHealthGrowth);

        public float EnemyGoldGrowth => Mathf.Max(1f, enemyGoldGrowth);

        public float EnemyAttackGrowth => Mathf.Max(1f, enemyAttackGrowth);

        public EnemyTargetingMode EnemyTargeting => enemyTargeting;

        public float CombatTickIntervalSec => Mathf.Clamp(combatTickIntervalSec, 0.01f, 0.5f);

        public int StageRollbackOnDefeat => Mathf.Max(0, stageRollbackOnDefeat);

        public bool ResetAutoRetryOnDefeat => resetAutoRetryOnDefeat;

        public bool ScaleEnemyDefenseWithStage => scaleEnemyDefenseWithStage;

        public bool LogCombatToConsole => logCombatToConsole;

        /// <summary>Wall-clock speed of combat. 1 = fastest, higher = slower.</summary>
        public float CombatPaceMultiplier => Mathf.Clamp(combatPaceMultiplier, 0.25f, 10f);

        public bool HealHeroesOnStageAdvance => healHeroesOnStageAdvance;

        public bool ResetHeroLevelsOnAscension => resetHeroLevelsOnAscension;

        public int NormalWavesPerStage => Mathf.Max(1, normalWavesPerStage);

        public int EnemiesPerWave => Mathf.Max(1, enemiesPerWave);

        public int GemsPerBossKill => Mathf.Max(0, gemsPerBossKill);

        public float WaveTransitionDelaySec => Mathf.Max(0f, waveTransitionDelaySec);

        public double MinDamageRatio => Mathf.Clamp(minDamageRatio, 0f, 1f);

        public float CriticalChance => Mathf.Clamp01(criticalChance);

        public float CriticalDamageMultiplier => Mathf.Max(1f, criticalDamageMultiplier);

        public bool ReviveHeroesEachWave => reviveHeroesEachWave;

        public float UpgradeCostGrowth => Mathf.Max(1f, upgradeCostGrowth);

        public float UpgradeStatGainPerLevel => Mathf.Max(0f, upgradeStatGainPerLevel);

        public float PrestigeStageDivisor => Mathf.Max(1f, prestigeStageDivisor);

        public float PrestigeExponent => Mathf.Max(0f, prestigeExponent);

        public int MinStageToAscend => Mathf.Max(1, minStageToAscend);

        public float OfflineCapSeconds => Mathf.Max(0f, offlineCapSeconds);

        public double OfflineEfficiency => Mathf.Clamp(offlineEfficiency, 0f, 1f);

        public float OfflineMaxEquivalentSeconds => Mathf.Max(0f, offlineMaxEquivalentSeconds);

        public float OfflineEstimatedSecondsPerKill => Mathf.Max(0.1f, offlineEstimatedSecondsPerKill);

        public float MinOfflineSecondsForPopup => Mathf.Max(0f, minOfflineSecondsForPopup);

        public float OfflineCapExtensionSeconds => Mathf.Max(60f, offlineCapExtensionSeconds);

        public double OfflineCapExtensionGemCost => offlineCapExtensionGemCost < 0d ? 0d : offlineCapExtensionGemCost;

        public float OfflineCapExtensionMaxSeconds => Mathf.Max(0f, offlineCapExtensionMaxSeconds);

        public double StartingGold => startingGold < 0d ? 0d : startingGold;

        public double StartingGems => startingGems < 0d ? 0d : startingGems;

        public float GoldPerSecondSampleWindowSec => Mathf.Max(5f, goldPerSecondSampleWindowSec);

        public double AdGoldBoostMultiplier => adGoldBoostMultiplier < 1d ? 1d : adGoldBoostMultiplier;

        public float AdGoldBoostDurationSec => Mathf.Max(0f, adGoldBoostDurationSec);

        public float AutosaveIntervalSec => Mathf.Max(5f, autosaveIntervalSec);
    }
}