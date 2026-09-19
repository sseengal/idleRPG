using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Utils;

namespace IdleRPG.Debugging
{
    /// <summary>
    /// Development tool: mirrors <see cref="GameEvents"/> into the console so the
    /// auto-battle can be validated headlessly (no UI needed). Toggle it off in the
    /// inspector when the real HUD exists.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatEventLogger : MonoBehaviour
    {
        [Header("Verbosity")]
        [SerializeField] private bool logCurrency = false;
        [SerializeField] private bool logHeroDamage = false;
        [SerializeField] private bool logEnemyDamage = false;
        [SerializeField] private bool logStateChanges = true;
        [SerializeField] private bool logWavesAndKills = true;

        private void OnEnable()
        {
            GameEvents.CurrencyChanged += OnCurrencyChanged;
            GameEvents.GameStateChanged += OnGameStateChanged;
            GameEvents.StageChanged += OnStageChanged;
            GameEvents.EnemySpawned += OnEnemySpawned;
            GameEvents.EnemyDamaged += OnEnemyDamaged;
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.HeroDamaged += OnHeroDamaged;
            GameEvents.HeroDied += OnHeroDied;
            GameEvents.WaveCompleted += OnWaveCompleted;
            GameEvents.PartyWiped += OnPartyWiped;
            GameEvents.BossFailed += OnBossFailed;
            GameEvents.PrestigeYieldChanged += OnPrestigeYieldChanged;
            GameEvents.AscensionCompleted += OnAscensionCompleted;
            GameEvents.ToastRequested += OnToastRequested;
            GameEvents.OfflineRewardsReady += OnOfflineRewardsReady;
        }

        private void OnDisable()
        {
            GameEvents.CurrencyChanged -= OnCurrencyChanged;
            GameEvents.GameStateChanged -= OnGameStateChanged;
            GameEvents.StageChanged -= OnStageChanged;
            GameEvents.EnemySpawned -= OnEnemySpawned;
            GameEvents.EnemyDamaged -= OnEnemyDamaged;
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.HeroDamaged -= OnHeroDamaged;
            GameEvents.HeroDied -= OnHeroDied;
            GameEvents.WaveCompleted -= OnWaveCompleted;
            GameEvents.PartyWiped -= OnPartyWiped;
            GameEvents.BossFailed -= OnBossFailed;
            GameEvents.PrestigeYieldChanged -= OnPrestigeYieldChanged;
            GameEvents.AscensionCompleted -= OnAscensionCompleted;
            GameEvents.ToastRequested -= OnToastRequested;
            GameEvents.OfflineRewardsReady -= OnOfflineRewardsReady;
        }

        private void OnCurrencyChanged(CurrencyType currency, double newAmount)
        {
            if (logCurrency)
            {
                Debug.Log($"[Economy] {currency} = {NumberFormatter.Format(newAmount)}");
            }
        }

        private void OnGameStateChanged(GameState previous, GameState next)
        {
            if (logStateChanges)
            {
                Debug.Log($"[State] {previous} -> {next}");
            }
        }

        private void OnStageChanged(int stage, int wave, bool isBossWave)
        {
            if (logWavesAndKills && isBossWave)
            {
                Debug.Log($"[Stage] {stage} wave {wave} - BOSS ENCOUNTER");
            }
        }

        private void OnEnemySpawned(string enemyName, double maxHealth, bool isBoss)
        {
            if (logWavesAndKills)
            {
                Debug.Log($"[Spawn] {enemyName} ({(isBoss ? "BOSS" : "normal")}) HP {NumberFormatter.Format(maxHealth)}");
            }
        }

        private void OnEnemyDamaged(EnemyDamagedInfo info)
        {
            if (logEnemyDamage)
            {
                Debug.Log($"[Hit] {NumberFormatter.Format(info.Damage)}{(info.IsCritical ? " CRIT" : string.Empty)} " +
                          $"({info.NormalizedHealth:P0} left)");
            }
        }

        private void OnEnemyKilled(string enemyName, double goldReward)
        {
            if (logWavesAndKills)
            {
                Debug.Log($"[Kill] {enemyName} -> +{NumberFormatter.Format(goldReward)} gold");
            }
        }

        private void OnHeroDamaged(int heroIndex, double damage, double currentHealth, double maxHealth)
        {
            if (logHeroDamage)
            {
                Debug.Log($"[Hero {heroIndex}] -{damage:0.#} -> {currentHealth:0}/{maxHealth:0}");
            }
        }

        private void OnHeroDied(int heroIndex)
        {
            if (logHeroDamage)
            {
                Debug.Log($"[Hero {heroIndex}] DIED");
            }
        }

        private void OnWaveCompleted(int stage, int wave)
        {
            if (logWavesAndKills)
            {
                Debug.Log($"[Wave] cleared stage {stage} wave {wave}");
            }
        }

        private void OnPartyWiped()
        {
            Debug.Log("[Party] wiped");
        }

        private void OnBossFailed()
        {
            Debug.Log("[Boss] failed");
        }

        private void OnPrestigeYieldChanged(double tokenYield)
        {
            Debug.Log($"[Prestige] token yield now {NumberFormatter.Format(tokenYield)}");
        }

        private void OnAscensionCompleted(double tokensEarned, int newHighestStage)
        {
            Debug.Log($"[Ascension] +{NumberFormatter.Format(tokensEarned)} tokens (best stage {newHighestStage})");
        }

        private void OnToastRequested(string message)
        {
            Debug.Log($"[Toast] {message}");
        }

        private void OnOfflineRewardsReady(Save.OfflineRewardResult result)
        {
            Debug.Log($"[Offline] {NumberFormatter.FormatDuration(result.CappedSeconds)} away -> " +
                      $"{NumberFormatter.Format(result.Gold)} gold");
        }
    }
}