using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IdleRPG.Combat;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Progression;
using IdleRPG.Services;

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
        [Tooltip("The three hero stat tracks (ATK / HP / DEF).")]
        [SerializeField] private List<StatUpgradeData> statUpgrades = new List<StatUpgradeData>();
        [Tooltip("The permanent upgrade tree (+% Gold, +% Damage, +% HP).")]
        [SerializeField] private List<PrestigeUpgradeData> prestigeUpgrades = new List<PrestigeUpgradeData>();

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
        private Coroutine slowTickRoutine;

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

        /// <summary>Final hero stats (base + levels + prestige).</summary>
        public StatResolver Resolver { get; private set; }

        /// <summary>Hero stat purchases (+1 / +10 ATK, HP, DEF).</summary>
        public UpgradeManager Upgrade { get; private set; }

        /// <summary>Token yield, ascension reset and the permanent upgrade tree.</summary>
        public AscensionManager Ascension { get; private set; }

        /// <summary>Timed gold multiplier from rewarded ads.</summary>
        public BoostManager Boost { get; private set; }

        /// <summary>Rewarded-ad provider (mock until a real SDK is added).</summary>
        public IAdService Ads { get; private set; }

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

            if (Resolver != null)
            {
                Resolver.StatsChanged -= OnStatsChanged;
            }

            if (slowTickRoutine != null)
            {
                StopCoroutine(slowTickRoutine);
                slowTickRoutine = null;
            }
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

            Resolver = new StatResolver(balanceConfig, statUpgrades, prestigeUpgrades);
            Resolver.StatsChanged += OnStatsChanged;
            Upgrade = new UpgradeManager(Economy, Resolver, partyConfig);
            Ascension = new AscensionManager(balanceConfig, Economy, Resolver, prestigeUpgrades);
            Boost = new BoostManager(balanceConfig);
            Ads = new MockAdService(this, 3f, true);

            if (!combatManager.Initialize(balanceConfig, waveConfig, partyConfig))
            {
                Debug.LogError("[GameManager] CombatManager failed to initialise (check PartyConfig heroes).");
                return false;
            }

            combatManager.SetStatProvider(Resolver);
            SubscribeCombat();
            SetState(GameState.Boot, GameState.Combat);

            slowTickRoutine = StartCoroutine(SlowTickLoop());

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

            combatManager.StopRun();
            SetState(State, GameState.Ascension);

            if (!Ascension.TryAscend(HighestStageReached, out double tokens))
            {
                Debug.LogWarning("[GameManager] Ascension requested but the token yield is 0.");
                return false;
            }

            GameEvents.RaiseAscensionCompleted(tokens, HighestStageReached);
            LogFlow($"Ascended for {tokens:0} token(s) | {Ascension.DescribeMultipliers()} | {Economy}");

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

            Economy.AddGold(ResolveGoldReward(goldReward));
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

        // ------------------------------------------------------------------
        // Rewards, boosts and progression hooks
        // ------------------------------------------------------------------
        /// <summary>
        /// Single funnel for gold: raw combat/offline reward x prestige gold% x ad boost.
        /// Step 5's offline calculation uses this too, so live and offline earnings agree.
        /// </summary>
        public double ResolveGoldReward(double rawGold)
        {
            double prestige = Resolver != null ? Resolver.GlobalGoldMultiplier : 1d;
            double boost = Boost != null ? Boost.GoldMultiplier : 1d;
            return rawGold * prestige * boost;
        }

        /// <summary>Shows a rewarded ad, then grants the 2x gold boost on success (Shop panel).</summary>
        public void WatchAdForGoldBoost()
        {
            if (Ads == null || Boost == null)
            {
                return;
            }

            if (!Ads.IsRewardedAdReady)
            {
                GameEvents.RaiseToast("Ad not ready yet.");
                return;
            }

            Ads.ShowRewardedAd(success =>
            {
                if (!success)
                {
                    GameEvents.RaiseToast("Ad skipped - no reward.");
                    return;
                }

                Boost.ActivateFromAd();
                LogFlow($"Ad boost active: x{Boost.GoldMultiplier:0.#} gold for {Boost.RemainingSeconds / 60f:0.#} min.");
            });
        }

        /// <summary>Called by the resolver after any level or multiplier change.</summary>
        private void OnStatsChanged()
        {
            if (combatManager != null && combatManager.Simulator != null)
            {
                combatManager.Simulator.RefreshHeroStats();
            }
        }

        /// <summary>One-second tick for time-based systems (boost expiry; autosave joins in Step 5).</summary>
        private IEnumerator SlowTickLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(1f);

            while (true)
            {
                yield return wait;

                if (Boost != null && Boost.Refresh())
                {
                    LogFlow("Ad gold boost expired.");
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>Editor-only wiring for progression assets (used by the scene builder tools).</summary>
        public void EditorInitializeProgression(List<StatUpgradeData> statTracks, List<PrestigeUpgradeData> permanentUpgrades)
        {
            statUpgrades = statTracks ?? new List<StatUpgradeData>();
            prestigeUpgrades = permanentUpgrades ?? new List<PrestigeUpgradeData>();
        }
#endif
    }
}