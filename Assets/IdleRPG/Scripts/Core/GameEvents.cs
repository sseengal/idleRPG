using System;
using UnityEngine;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Save;

namespace IdleRPG.Core
{
    /// <summary>
    /// Global, static event bus. UI and systems subscribe here instead of polling,
    /// which keeps MonoBehaviours decoupled (no FindObjectOfType, no Update() ticks).
    ///
    /// All events are raised through the internal Raise* helpers, which null-check the
    /// handler and isolate each subscriber in a try/catch so one broken listener cannot
    /// break the rest of the chain. Statics are cleared on every play-mode start through
    /// <see cref="ResetAll"/> (domain-reload disabled safety).
    /// </summary>
    public static partial class GameEvents
    {
        // ------------------------------------------------------------------
        // Currency & economy
        // ------------------------------------------------------------------
        /// <summary>(currency, newAmount) after any balance change.</summary>
        public static event Action<CurrencyType, double> CurrencyChanged;

        /// <summary>(active, remainingSeconds, multiplier) for the ad gold boost.</summary>
        public static event Action<bool, float, double> GoldBoostChanged;

        /// <summary>(message) short player-facing message for a toast/snackbar.</summary>
        public static event Action<string> ToastRequested;

        // ------------------------------------------------------------------
        // Battle feed
        // ------------------------------------------------------------------
        /// <summary>(message) one-sentence news for the battle log; rendered verbatim by CombatLogUI.</summary>
        public static event Action<LogMessage> CombatMessage;

        /// <summary>(day, gems) when the daily-streak calendar pays out (B7 S2).</summary>
        public static event Action<int, double> DailyStreakClaimed;

        // ------------------------------------------------------------------
        // Game flow
        // ------------------------------------------------------------------
        /// <summary>(previousState, newState).</summary>
        public static event Action<GameState, GameState> GameStateChanged;

        /// <summary>(stage, wave, isBossWave).</summary>
        public static event Action<int, int, bool> StageChanged;

        /// <summary>Raised when the party wipes or the boss timer expires.</summary>
        public static event Action BossFailed;

        /// <summary>Raised when all heroes are dead.</summary>
        public static event Action PartyWiped;

        /// <summary>(stage, wave) after a wave is cleared.</summary>
        public static event Action<int, int> WaveCompleted;

        // ------------------------------------------------------------------
        // Combat
        // ------------------------------------------------------------------
        /// <summary>(enemyName, maxHealth, isBoss, enemyIndex). Raised once per enemy in the wave.</summary>
        public static event Action<string, double, bool, int> EnemySpawned;

        /// <summary>Per-hit damage detail (carries the enemy index).</summary>
        public static event Action<EnemyDamagedInfo> EnemyDamaged;

        /// <summary>(enemyName, goldReward, enemyIndex). One call per enemy.</summary>
        public static event Action<string, double, int> EnemyKilled;

        /// <summary>(heroIndex, damage, currentHealth, maxHealth, attackerEnemyIndex). See <see cref="HeroDamaged"/> above.</summary>
        public static event Action<int, double, double, double, int> HeroDamaged;

        /// <summary>(heroIndex).</summary>
        public static event Action<int> HeroDied;

        // ------------------------------------------------------------------
        // Progression
        // ------------------------------------------------------------------
        /// <summary>(heroIndex) whenever a hero's derived stats change.</summary>
        public static event Action<int> HeroStatsChanged;

        /// <summary>(heroIndex, statType, newLevel).</summary>
        public static event Action<int, HeroStatType, int> HeroLevelChanged;

        /// <summary>(heroIndex, statType, newLevel, goldCost).</summary>
        public static event Action<int, HeroStatType, int, double> UpgradePurchased;

        /// <summary>(tokenYield) recalculated when the highest stage changes.</summary>
        public static event Action<double> PrestigeYieldChanged;

        /// <summary>(tokensEarned, newHighestStage).</summary>
        public static event Action<double, int> AscensionCompleted;

        // ------------------------------------------------------------------
        // Offline progress & save
        // ------------------------------------------------------------------
        /// <summary>Offline earnings ready — the popup should open.</summary>
        public static event Action<OfflineRewardResult> OfflineRewardsReady;

        /// <summary>(goldClaimed).</summary>
        public static event Action<double> OfflineRewardsClaimed;

        /// <summary>Save file loaded into memory.</summary>
        public static event Action SaveLoaded;

        /// <summary>Save file written to disk.</summary>
        public static event Action SaveWritten;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------
        /// <summary>
        /// Clears every subscription when entering play mode. Required because the
        /// project can run with domain reload disabled, which would otherwise keep
        /// stale delegates from the previous session alive.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAll()
        {
            CurrencyChanged = null;
            GoldBoostChanged = null;
            ToastRequested = null;
            GameStateChanged = null;
            StageChanged = null;
            BossFailed = null;
            PartyWiped = null;
            WaveCompleted = null;
            EnemySpawned = null;
            EnemyDamaged = null;
            EnemyKilled = null;
            HeroDamaged = null;
            HeroDied = null;
            HeroStatsChanged = null;
            HeroLevelChanged = null;
            UpgradePurchased = null;
            PrestigeYieldChanged = null;
            AscensionCompleted = null;
            OfflineRewardsReady = null;
            OfflineRewardsClaimed = null;
            SaveLoaded = null;
            SaveWritten = null;
            CombatMessage = null;
        }

    }
}
