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

        [Tooltip("Countdown for the boss encounter, in seconds.")]
        [SerializeField] private float bossTimeLimitSec = 30f;

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
        [SerializeField] private float minDamageRatio = 0.1f;

        [Tooltip("Critical hit chance for heroes (0 = disabled, MVP default).")]
        [Range(0f, 1f)]
        [SerializeField] private float criticalChance = 0f;

        [SerializeField] private float criticalDamageMultiplier = 1.5f;

        [Tooltip("Heroes revive at full HP when a wave starts.")]
        [SerializeField] private bool reviveHeroesEachWave = true;

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

        [Tooltip("Seconds to ignore. Prevents instant popups after a quick app switch.")]
        [SerializeField] private float minOfflineSecondsForPopup = 30f;

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

        public int NormalWavesPerStage => Mathf.Max(1, normalWavesPerStage);

        public float BossTimeLimitSec => Mathf.Max(1f, bossTimeLimitSec);

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

        public float MinOfflineSecondsForPopup => Mathf.Max(0f, minOfflineSecondsForPopup);

        public double StartingGold => startingGold < 0d ? 0d : startingGold;

        public double StartingGems => startingGems < 0d ? 0d : startingGems;

        public float GoldPerSecondSampleWindowSec => Mathf.Max(5f, goldPerSecondSampleWindowSec);

        public double AdGoldBoostMultiplier => adGoldBoostMultiplier < 1d ? 1d : adGoldBoostMultiplier;

        public float AdGoldBoostDurationSec => Mathf.Max(0f, adGoldBoostDurationSec);

        public float AutosaveIntervalSec => Mathf.Max(5f, autosaveIntervalSec);
    }
}