using System;
using UnityEngine;
using IdleRPG.Combat;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Progression;

namespace IdleRPG.Core
{
    /// <summary>
    /// Central state machine and composition root for the MVP.
    ///
    /// Owns:
    ///   * the <see cref="GameState"/> flow (Boot -> Combat -> Boss -> Defeat -> Ascension),
    ///   * stage progression (current stage, highest stage reached, auto-retry flag),
    ///   * the <see cref="EconomyManager"/> instance and reward application.
    ///
    /// Wave-level flow lives in <see cref="CombatManager"/>; this class only reacts to it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Data Assets")]
        [SerializeField] private BalanceConfig balanceConfig;
        [SerializeField] private WaveConfig waveConfig;
        [SerializeField] private PartyConfig partyConfig;

        [Header("Scene References")]
        [Tooltip("Wave/encounter driver living on the same GameObject.")]
        [SerializeField] private CombatManager combatManager;

        [Header("Run Settings")]
        [Tooltip("Start the auto-battle automatically when the scene starts.")]
        [SerializeField] private bool autoStartRun = true;

        [Tooltip("Stage a brand new game begins at.")]
        [SerializeField] private int startingStage = 1;

        [Tooltip("Print stage/state changes to the console.")]
        [SerializeField] private bool logFlowToConsole = true;

        private bool isWired;

        /// <summary>(previous, next) state transitions for interested systems/UI.</summary>
        public event Action<GameState, GameState> StateChanged;

        /// <summary>Gold/gems/tokens owner. Created here, never looked up.</summary>
        public EconomyManager Economy { get; private set; }

        public GameState State { get; private set; } = GameState.Boot;

        public int CurrentStage { get; private set; } = 1;

        public int HighestStageReached { get; private set; } = 1;

        /// <summary>Cleared by DefeatState, restored by a manual retry.</summary>
        public bool AutoRetryEnabled { get; private set; } = true;

        public CombatManager Combat => combatManager;

        public WaveConfig WaveData => waveConfig;

        public PartyConfig Party => partyConfig;

        public BalanceConfig Balance => balanceConfig;

        /// <summary>Tokens the player would receive by ascending right now.</summary>
        public double PrestigeTokenYield
        {
            get
            {
                if (balanceConfig == null)
                {
                    return 0d;
                }

                return FormulaUtility.PrestigeTokenReward(
                    HighestStageReached,
                    balanceConfig.PrestigeStageDivisor,
                    balanceConfig.PrestigeExponent);
            }
        }

        /// <summary>True once the player has reached the minimum stage for an ascension.</summary>
        public bool CanAscend => balanceConfig != null && HighestStageReached >= balanceConfig.MinStageToAscend;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------
        private void Awake()
        {
            isWired = WireUp();

            if (!isWired)
            {
                enabled = false;
            }
        }

        private void Start()
        {
            if (!isWired)
            {
                return;
            }

            if (autoStartRun)
            {
                StartRun(startingStage);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeCombat();
        }

        private bool WireUp()
        {
            if (balanceConfig == null || waveConfig == null || partyConfig == null)
            {
                Debug.LogError("[GameManager] BalanceConfig / WaveConfig / PartyConfig must be assigned in the inspector.");
                return false;
            }

            if (combatManager == null)
            {
                Debug.LogError("[GameManager] CombatManager reference is missing.");
                return false;
            }

            Economy = new EconomyManager(balanceConfig);
            Economy.ApplyStartingBalances();

            if (!combatManager.Initialize(balanceConfig, waveConfig, partyConfig))
            {
                Debug.LogError("[GameManager] CombatManager failed to initialise (check PartyConfig heroes).");
                return false;
            }

            SubscribeCombat();
            SetState(GameState.Boot, GameState.Combat);

            return true;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only wiring hook used by the authoring tools. Keeps the serialized
        /// fields private while avoiding reflection/SerializedObject in scene builders.
        /// </summary>
        public void EditorInitialize(BalanceConfig balance, WaveConfig waves, PartyConfig party, CombatManager combat)
        {
            balanceConfig = balance;
            waveConfig = waves;
            partyConfig = party;
            combatManager = combat;
        }
#endif

        // ------------------------------------------------------------------
        // Run control
        // ------------------------------------------------------------------
        /// <summary>Starts (or restarts) the run from <paramref name="stage"/>.</summary>
        public void StartRun(int stage)
        {
            if (!isWired)
            {
                Debug.LogWarning("[GameManager] StartRun ignored: manager is not wired.");
                return;
            }

            CurrentStage = Mathf.Max(1, stage);
            if (CurrentStage > HighestStageReached)
            {
                HighestStageReached = CurrentStage;
            }

            AutoRetryEnabled = true;
            combatManager.SetStage(CurrentStage, healParty: true);
            combatManager.StartRun();
            SetState(State == GameState.Boot ? GameState.Boot : State, combatManager.IsBossWave ? GameState.Boss : GameState.Combat);
            RaiseStageChanged();
        }

        /// <summary>Manual retry after a defeat: re-fights the current stage from wave 1.</summary>
        public void RetryAfterDefeat()
        {
            if (!isWired)
            {
                return;
            }

            if (State != GameState.Defeat)
            {
                Debug.LogWarning($"[GameManager] Retry ignored: state is {State}, not Defeat.");
                return;
            }

            StartRun(CurrentStage);
            LogFlow($"Retry accepted: stage {CurrentStage}, wave 1.");
        }

        /// <summary>
        /// Enters the ascension state. Reward grant + world reset are implemented in Step 3;
        /// the state transition itself is real so UI and combat pausing already work.
        /// </summary>
        public bool RequestAscension()
        {
            if (!isWired)
            {
                return false;
            }

            if (!CanAscend)
            {
                GameEvents.RaiseToast($"Reach stage {balanceConfig.MinStageToAscend} to ascend.");
                return false;
            }

            double tokens = PrestigeTokenYield;
            combatManager.StopRun();
            SetState(State, GameState.Ascension);

            if (tokens <= 0d)
            {
                Debug.LogWarning("[GameManager] Ascension requested but the token yield is 0.");
                return false;
            }

            // Step 3 adds permanent-upgrade spending; the reset itself is final here.
            Economy.AddTokens(tokens);
            Economy.ResetGold();
            GameEvents.RaiseAscensionCompleted(tokens, HighestStageReached);
            LogFlow($"Ascended for {tokens:0} token(s); highest stage kept at {HighestStageReached}.");

            ResetProgressForAscension(HighestStageReached);
            return true;
        }

        /// <summary>Called by the ascension flow: gold, stage and hero levels go back to zero.</summary>
        public void ResetProgressForAscension(int highestStageToKeep)
        {
            CurrentStage = 1;
            HighestStageReached = Mathf.Max(1, highestStageToKeep);
            AutoRetryEnabled = true;

            SetState(State, GameState.Combat);
            combatManager.SetStage(CurrentStage, healParty: true);
            combatManager.StartRun();
            RaiseStageChanged();
        }

        // ------------------------------------------------------------------
        // Combat event handling
        // ------------------------------------------------------------------
        private void SubscribeCombat()
        {
            combatManager.EnemyKilled += OnEnemyKilled;
            combatManager.WaveCleared += OnWaveCleared;
            combatManager.StageCleared += OnStageCleared;
            combatManager.PartyWiped += OnPartyWiped;
            combatManager.BossFailed += OnBossFailed;
            combatManager.WaveStarted += OnWaveStarted;
        }

        private void UnsubscribeCombat()
        {
            if (combatManager == null)
            {
                return;
            }

            combatManager.EnemyKilled -= OnEnemyKilled;
            combatManager.WaveCleared -= OnWaveCleared;
            combatManager.StageCleared -= OnStageCleared;
            combatManager.PartyWiped -= OnPartyWiped;
            combatManager.BossFailed -= OnBossFailed;
            combatManager.WaveStarted -= OnWaveStarted;
        }

        /// <summary>Applies prestige gold bonuses on top of the simulation's own reward.</summary>
        private void OnEnemyKilled(double goldReward)
        {
            if (Economy == null)
            {
                return;
            }

            double multiplier = combatManager.StatProvider != null ? combatManager.StatProvider.GlobalGoldMultiplier : 1d;
            Economy.AddGold(goldReward * multiplier);
        }

        private void OnWaveCleared(int stage, int wave)
        {
            if (combatManager.IsBossWave)
            {
                return;
            }

            LogFlow($"Wave {wave} cleared (stage {stage}).");
        }

        /// <summary>Boss died: advance the stage, grant gems, keep auto-retry behaviour.</summary>
        private void OnStageCleared(int stage)
        {
            int nextStage = stage + 1;
            CurrentStage = nextStage;

            if (nextStage > HighestStageReached)
            {
                HighestStageReached = nextStage;
                GameEvents.RaisePrestigeYieldChanged(PrestigeTokenYield);
            }

            if (Economy != null && balanceConfig != null && balanceConfig.GemsPerBossKill > 0)
            {
                Economy.AddGems(balanceConfig.GemsPerBossKill);
            }

            SetState(GameState.Boss, GameState.Combat);
            combatManager.SetStage(CurrentStage, healParty: balanceConfig != null && balanceConfig.HealHeroesOnStageAdvance);
            RaiseStageChanged();

            LogFlow($"Stage {stage} complete -> now stage {CurrentStage} | {Economy}");
        }

        /// <summary>Party wipe: drop back, disable auto-retry, wait for a manual retry.</summary>
        private void OnPartyWiped()
        {
            int rollback = balanceConfig != null ? balanceConfig.StageRollbackOnDefeat : 1;
            int defeatedStage = CurrentStage;

            CurrentStage = Mathf.Max(1, CurrentStage - rollback);

            if (balanceConfig != null && balanceConfig.ResetAutoRetryOnDefeat)
            {
                AutoRetryEnabled = false;
            }

            combatManager.StopRun();
            SetState(GameState.Boss, GameState.Defeat);
            RaiseStageChanged();

            GameEvents.RaisePartyWiped();

            LogFlow($"DEFEAT on stage {defeatedStage}. Rolled back to stage {CurrentStage}, wave 1. " +
                    $"Auto-retry={AutoRetryEnabled}. Call RetryAfterDefeat() to resume.");

            if (AutoRetryEnabled)
            {
                StartRun(CurrentStage);
            }
        }

        private void OnBossFailed()
        {
            LogFlow("Boss encounter failed.");
        }

        /// <summary>Reflects the encounter type in the state machine (Combat vs Boss).</summary>
        private void OnWaveStarted(int stage, int wave, bool isBoss)
        {
            SetState(State, isBoss ? GameState.Boss : GameState.Combat);
            RaiseStageChanged();
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        /// <summary>Moves to a new state, publishing internal and global events.</summary>
        private void SetState(GameState previous, GameState next)
        {
            if (State == next && previous == next)
            {
                return;
            }

            State = next;
            StateChanged?.Invoke(previous, next);
            GameEvents.RaiseGameStateChanged(previous, next);
        }

        private void RaiseStageChanged()
        {
            bool isBoss = combatManager != null && combatManager.IsBossWave;
            GameEvents.RaiseStageChanged(CurrentStage, combatManager == null ? 1 : combatManager.CurrentWave, isBoss);
            GameEvents.RaisePrestigeYieldChanged(PrestigeTokenYield);
        }

        private void LogFlow(string message)
        {
            if (logFlowToConsole)
            {
                Debug.Log($"[GameManager] {message}");
            }
        }
    }
}