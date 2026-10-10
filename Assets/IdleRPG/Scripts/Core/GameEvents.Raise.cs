using System;
using UnityEngine;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Save;
using IdleRPG.Sim;

namespace IdleRPG.Core
{
    /// <summary>
    /// Publishing half of the event bus: every internal Raise* helper and its SafeInvoke wrappers.
    /// </summary>
    public static partial class GameEvents
    {
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

        internal static void RaiseSwingStarted(int attackerIndex, CombatantSide attackerSide, int targetIndex, CombatantSide targetSide)
        {
            SafeInvoke(SwingStarted, attackerIndex, attackerSide, targetIndex, targetSide, nameof(SwingStarted));
        }

        internal static void RaiseEnemyKilled(string enemyName, double goldReward, int enemyIndex)
        {
            SafeInvoke(EnemyKilled, enemyName, goldReward, enemyIndex, nameof(EnemyKilled));
        }

        internal static void RaiseHeroDamaged(int heroIndex, double damage, double currentHealth, double maxHealth, int attackerEnemyIndex, bool isCritical)
        {
            SafeInvoke(HeroDamaged, heroIndex, damage, currentHealth, maxHealth, attackerEnemyIndex, isCritical, nameof(HeroDamaged));
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

        internal static void RaiseDailyStreakClaimed(int day, double gems)
        {
            SafeInvoke(DailyStreakClaimed, day, gems, nameof(DailyStreakClaimed));
        }

        internal static void RaiseOfflineRewardsReady(OfflineRewardResult result)
        {
            SafeInvoke(OfflineRewardsReady, result, nameof(OfflineRewardsReady));
        }

        internal static void RaiseOfflineRewardsClaimed(double gold)
        {
            SafeInvoke(OfflineRewardsClaimed, gold, nameof(OfflineRewardsClaimed));
        }

        internal static void RaiseCombatMessage(LogMessage message)
        {
            SafeInvoke(CombatMessage, message, nameof(CombatMessage));
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

        private static void SafeInvoke<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, string eventName)
        {
            if (handler == null)
            {
                return;
            }

            try
            {
                handler(arg1, arg2, arg3, arg4, arg5);
            }
            catch (Exception exception)
            {
                LogSubscriberException(eventName, exception);
            }
        }

        private static void SafeInvoke<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, string eventName)
        {
            if (handler == null)
            {
                return;
            }

            try
            {
                handler(arg1, arg2, arg3, arg4, arg5, arg6);
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
