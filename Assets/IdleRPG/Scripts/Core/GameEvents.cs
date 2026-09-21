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
    public static class GameEvents
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

        /// <summary>(heroIndex, damage, currentHealth, maxHealth).</summary>
        public static event Action<int, double, double, double> HeroDamaged;

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
        }

        // ------------------------------------------------------------------
        // Raise helpers (internal: only game systems may publish)
        // ------------------------------------------------------------------
        internal static void RaiseCurrencyChanged(CurrencyType currency, double newAmount)
        {
            SafeInvoke(CurrencyChanged, currency, newAmount, nameof(CurrencyChanged));
        }

        internal static void RaiseGoldBoostChanged(bool active, float remainingSeconds, double multiplier)
        {
            SafeInvoke(GoldBoostChanged, active, remainingSeconds, multiplier, nameof(GoldBoostChanged));
        }

        internal static void RaiseToast(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            SafeInvoke(ToastRequested, message, nameof(ToastRequested));
        }

        internal static void RaiseGameStateChanged(GameState previous, GameState next)
        {
            SafeInvoke(GameStateChanged, previous, next, nameof(GameStateChanged));
        }

        internal static void RaiseStageChanged(int stage, int wave, bool isBossWave)
        {
            SafeInvoke(StageChanged, stage, wave, isBossWave, nameof(StageChanged));
        }

        internal static void RaiseBossFailed()
        {
            SafeInvoke(BossFailed, nameof(BossFailed));
        }

        internal static void RaisePartyWiped()
        {
            SafeInvoke(PartyWiped, nameof(PartyWiped));
        }

        internal static void RaiseWaveCompleted(int stage, int wave)
        {
            SafeInvoke(WaveCompleted, stage, wave, nameof(WaveCompleted));
        }

        internal static void RaiseEnemySpawned(string enemyName, double maxHealth, bool isBoss, int enemyIndex)
        {
            SafeInvoke(EnemySpawned, enemyName, maxHealth, isBoss, enemyIndex, nameof(EnemySpawned));
        }

        internal static void RaiseEnemyDamaged(EnemyDamagedInfo info)
        {
            SafeInvoke(EnemyDamaged, info, nameof(EnemyDamaged));
        }

        internal static void RaiseEnemyKilled(string enemyName, double goldReward, int enemyIndex)
        {
            SafeInvoke(EnemyKilled, enemyName, goldReward, enemyIndex, nameof(EnemyKilled));
        }

        internal static void RaiseHeroDamaged(int heroIndex, double damage, double currentHealth, double maxHealth)
        {
            SafeInvoke(HeroDamaged, heroIndex, damage, currentHealth, maxHealth, nameof(HeroDamaged));
        }

        internal static void RaiseHeroDied(int heroIndex)
        {
            SafeInvoke(HeroDied, heroIndex, nameof(HeroDied));
        }

        internal static void RaiseHeroStatsChanged(int heroIndex)
        {
            SafeInvoke(HeroStatsChanged, heroIndex, nameof(HeroStatsChanged));
        }

        internal static void RaiseHeroLevelChanged(int heroIndex, HeroStatType statType, int newLevel)
        {
            SafeInvoke(HeroLevelChanged, heroIndex, statType, newLevel, nameof(HeroLevelChanged));
        }

        internal static void RaiseUpgradePurchased(int heroIndex, HeroStatType statType, int newLevel, double goldCost)
        {
            SafeInvoke(UpgradePurchased, heroIndex, statType, newLevel, goldCost, nameof(UpgradePurchased));
        }

        internal static void RaisePrestigeYieldChanged(double tokenYield)
        {
            SafeInvoke(PrestigeYieldChanged, tokenYield, nameof(PrestigeYieldChanged));
        }

        internal static void RaiseAscensionCompleted(double tokensEarned, int newHighestStage)
        {
            SafeInvoke(AscensionCompleted, tokensEarned, newHighestStage, nameof(AscensionCompleted));
        }

        internal static void RaiseOfflineRewardsReady(OfflineRewardResult result)
        {
            SafeInvoke(OfflineRewardsReady, result, nameof(OfflineRewardsReady));
        }

        internal static void RaiseOfflineRewardsClaimed(double gold)
        {
            SafeInvoke(OfflineRewardsClaimed, gold, nameof(OfflineRewardsClaimed));
        }

        internal static void RaiseSaveLoaded()
        {
            SafeInvoke(SaveLoaded, nameof(SaveLoaded));
        }

        internal static void RaiseSaveWritten()
        {
            SafeInvoke(SaveWritten, nameof(SaveWritten));
        }

        // ------------------------------------------------------------------
        // Safe invocation
        // ------------------------------------------------------------------
        private static void SafeInvoke(Action handler, string eventName)
        {
            if (handler == null)
            {
                return;
            }

            try
            {
                handler();
            }
            catch (Exception exception)
            {
                LogSubscriberException(eventName, exception);
            }
        }

        private static void SafeInvoke<T1>(Action<T1> handler, T1 arg1, string eventName)
        {
            if (handler == null)
            {
                return;
            }

            try
            {
                handler(arg1);
            }
            catch (Exception exception)
            {
                LogSubscriberException(eventName, exception);
            }
        }

        private static void SafeInvoke<T1, T2>(Action<T1, T2> handler, T1 arg1, T2 arg2, string eventName)
        {
            if (handler == null)
            {
                return;
            }

            try
            {
                handler(arg1, arg2);
            }
            catch (Exception exception)
            {
                LogSubscriberException(eventName, exception);
            }
        }

        private static void SafeInvoke<T1, T2, T3>(Action<T1, T2, T3> handler, T1 arg1, T2 arg2, T3 arg3, string eventName)
        {
            if (handler == null)
            {
                return;
            }

            try
            {
                handler(arg1, arg2, arg3);
            }
            catch (Exception exception)
            {
                LogSubscriberException(eventName, exception);
            }
        }

        private static void SafeInvoke<T1, T2, T3, T4>(Action<T1, T2, T3, T4> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, string eventName)
        {
            if (handler == null)
            {
                return;
            }

            try
            {
                handler(arg1, arg2, arg3, arg4);
            }
            catch (Exception exception)
            {
                LogSubscriberException(eventName, exception);
            }
        }

        private static void LogSubscriberException(string eventName, Exception exception)
        {
            Debug.LogError($"[GameEvents] A subscriber of '{eventName}' threw: {exception}");
        }
    }
}