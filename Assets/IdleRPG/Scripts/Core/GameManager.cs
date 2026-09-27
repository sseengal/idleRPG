using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using IdleRPG.Combat;
using IdleRPG.Data;
using IdleRPG.DebugTools;
using IdleRPG.Economy;
using IdleRPG.Sim;
using IdleRPG.Progression;
using IdleRPG.Save;
using IdleRPG.Services;
using IdleRPG.Utils;

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
    public sealed partial class GameManager : MonoBehaviour
    {
        [Header("Data Assets")]
        [SerializeField] private BalanceConfig balanceConfig;
        [SerializeField] private WaveConfig waveConfig;
        [SerializeField] private PartyConfig partyConfig;

        [Tooltip("Formation board for the party (rows/columns, slot unlocks, row rules). Step 10.")]
        [SerializeField] private FormationData formationConfig;
        [Tooltip("The three hero stat tracks (ATK / HP / DEF).")]
        [SerializeField] private List<StatUpgradeData> statUpgrades = new List<StatUpgradeData>();
        [Tooltip("The permanent upgrade tree (+% Gold, +% Damage, +% HP).")]
        [SerializeField] private List<PrestigeUpgradeData> prestigeUpgrades = new List<PrestigeUpgradeData>();

        [Tooltip("The automation cards (auto-buy manager, speed button). B6.")]
        [SerializeField] private List<AutomationDef> automationDefs = new List<AutomationDef>();

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
        private RunController runner;

        /// <summary>(previous, next) state transitions for interested systems/UI.</summary>
        public event Action<GameState, GameState> StateChanged;

        /// <summary>Gold/gems/tokens owner. Created here, never looked up.</summary>
        public EconomyManager Economy { get; private set; }

        public GameState State { get; private set; } = GameState.Boot;

        public int CurrentStage { get; private set; } = 1;

        public int HighestStageReached { get; private set; } = 1;

        /// <summary>
        /// Best stage of the CURRENT run. Ascension is gated and priced on this, so the button cannot be pressed
        /// again until the player has climbed back up. Resets to 1 on ascension; <see cref="HighestStageReached"/>
        /// stays as the lifetime record.
        /// </summary>
        public int RunBestStage { get; private set; } = 1;

        /// <summary>True while the short post-wipe beat is running (the loop resumes when it ends).</summary>
        private bool defeatBeatActive;

        /// <summary>Seconds left in the post-wipe beat.</summary>
        private float defeatBeatRemaining;

        public CombatManager Combat => combatManager;

        public WaveConfig WaveData => waveConfig;

        public PartyConfig Party => partyConfig;

        /// <summary>Who stands where. Built from <see cref="formationConfig"/> on start.</summary>
        public Formation Formation { get; private set; }

        public BalanceConfig Balance => balanceConfig;

        /// <summary>Final hero stats (base + levels + prestige).</summary>
        public StatResolver Resolver { get; private set; }

        /// <summary>Hero stat purchases (+1 / +10 ATK, HP, DEF).</summary>
        public UpgradeManager Upgrade { get; private set; }

        /// <summary>Token yield, ascension reset and the permanent upgrade tree.</summary>
        public AscensionManager Ascension { get; private set; }

        /// <summary>
        /// The one checkout for every progression row (B5 session 1). Hero stat purchases and the prestige tree both
        /// spend through here, so the two can never price or pay differently again.
        /// </summary>
        public TrackService Tracks { get; private set; }

        /// <summary>The automation engine (auto-buy; the speed button lands in Step 3). B6.</summary>
        public AutomationService Automation { get; private set; }

        /// <summary>Timed gold multiplier from rewarded ads.</summary>
        public BoostManager Boost { get; private set; }

        /// <summary>Rewarded-ad provider (mock until a real SDK is added).</summary>
        public IAdService Ads { get; private set; }

        /// <summary>Store cash register: mock today, Unity IAP (Apple/Google) at launch. B7 S1.</summary>
        public IIapService Iap { get; private set; }

        /// <summary>Every-morning gift calendar (B7 S2). Reads the save, pays the daily gems.</summary>
        public Economy.DailyStreakService DailyStreak { get; private set; }

        /// <summary>Per-day ad police (B7 S3): caps + cooldowns per placement, local-midnight rollover.</summary>
        public Economy.AdCapsService AdCaps { get; private set; }

        /// <summary>Three first-run hints, shown once each (B8').</summary>
        public Core.FirstRunTips FirstRunTips { get; private set; }

        /// <summary>
        /// Debug/testing hook: replace the shared game clock's "today" (e.g. yesterday/tomorrow) without touching
        /// the device clock. Null = real local date. Used by the daily streak AND the ad caps.
        /// </summary>
        public System.DateTime? ClockOverride { get; set; }

        private System.DateTime ResolveClockNow()
        {
            return ClockOverride ?? System.DateTime.Now;
        }

        /// <summary>Tokens the player would receive by ascending right now (priced on THIS run's best stage).</summary>
        public double PrestigeTokenYield
        {
            get
            {
                if (balanceConfig == null)
                {
                    return 0d;
                }

                return FormulaUtility.PrestigeTokenReward(
                    RunBestStage,
                    balanceConfig.PrestigeStageDivisor,
                    balanceConfig.PrestigeExponent);
            }
        }

        /// <summary>
        /// True once THIS run has reached the minimum stage. Deliberately not <see cref="HighestStageReached"/>:
        /// that never goes down, which left the ascend button live forever and made tokens farmable.
        /// </summary>
        public bool CanAscend => balanceConfig != null && RunBestStage >= balanceConfig.MinStageToAscend;

        /// <summary>Every wired system in one box (Step 7c). Nothing hunts the scene any more.</summary>
        public GameContext Context { get; private set; }

        /// <summary>The single heartbeat: combat ticks + the 1s slow chores.</summary>
        public RunController Runner => runner;

        /// <summary>Local session diary (in-memory ring buffer; never leaves the device).</summary>
        public TelemetryFeed Telemetry { get; private set; }

        /// <summary>Save orchestration (autosave cadence, lifecycle hooks, manual saves).</summary>
        public SaveManager Save { get; private set; }

        /// <summary>Measures live income (gold/s, kills/s, seconds/stage) - the one rate source.</summary>
        public SimLedger Ledger { get; private set; }

        /// <summary>The single payout till: multipliers + wallet + ledger receipt.</summary>
        public RewardService Rewards { get; private set; }

        /// <summary>Gem sinks (Step 9b: the offline income cap extension + instant income).</summary>
        public ShopService Shop { get; private set; }

        /// <summary>SFX abstraction. Placeholder tones today, authored clips in Step 20.</summary>
        public IAudioService Audio { get; private set; }

        /// <summary>Every time-based payout: the offline window and the instant-income gem sink.</summary>
        public IdleTimeService Idle { get; private set; }

        /// <summary>True when the current run was restored from disk.</summary>
        public bool LoadedFromSave { get; private set; }

        /// <summary>Wave restored from the save (used to resume mid-stage).</summary>
        public int RestoredWave { get; private set; } = 1;

        // Lifetime stats (persisted; no UI yet).
        public int TotalKills { get; private set; }

        public double TotalGoldEarned { get; private set; }

        public int AscensionCount { get; private set; }

        public int LastPageIndex { get; private set; }

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------
        private void Awake()
        {
            isWired = WireUp();

            if (!isWired)
            {
                enabled = false;
                return;
            }

            // Android back = save, then leave (B9'). No-op everywhere else.
            AndroidBackButton.Create(this);
        }

        private void Start()
        {
            if (!isWired)
            {
                return;
            }

            if (LoadedFromSave)
            {
                // Resume: the stage/wave were restored in WireUp, so just start the ticker.
                combatManager.StartRun();
                SetState(State, combatManager.IsBossWave ? GameState.Boss : GameState.Combat);
                RaiseStageChanged();
                LogFlow($"Resumed save: stage {CurrentStage} wave {RestoredWave}.");
            }
            else if (autoStartRun)
            {
                StartRun(startingStage);
                Save?.SaveNow("first-run");
            }

            // Offer offline earnings (no-op on a fresh install or a very short absence).
            EvaluateOffline();

            // Every-morning gift: auto-claim silently when there is a streak to pay.
            TryClaimDailyStreak();

            // Tip 1 of the first-run hints: "tap UPGRADE to hit harder" (once per install).
            Context?.FirstRunTips?.OnSessionStart();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Save?.SaveNow("pause");
            }
            else
            {
                // An idle game can sit in the background across midnight - catch the new day on the way back in.
                TryClaimDailyStreak();
            }
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                Save?.SaveNow("blur");
            }
        }

        private void OnApplicationQuit()
        {
            Save?.SaveNow("quit");
        }

        private void OnDestroy()
        {
            UnsubscribeCombat();
            UnsubscribeProgression();

            if (Automation != null)
            {
                Automation.Changed -= OnAutomationChanged;
            }

            if (Ledger != null)
            {
                Ledger.ResetSession();
            }

            if (Idle != null)
            {
                Idle.RewardPaid -= OnIdleRewardPaid;
                Idle.Detach();
            }

            if (Formation != null)
            {
                Formation.Changed -= OnFormationChanged;
            }

            if (Resolver != null)
            {
                Resolver.StatsChanged -= OnStatsChanged;
            }

            if (runner != null)
            {
                runner.Detach();
            }

            if (Telemetry != null)
            {
                Telemetry.Detach();
            }
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

#if UNITY_EDITOR

        /// <summary>Editor-only wiring for progression assets (used by the scene builder tools).</summary>
        public void EditorInitializeProgression(List<StatUpgradeData> statTracks,
            List<PrestigeUpgradeData> permanentUpgrades, List<AutomationDef> automationCards = null)
        {
            statUpgrades = statTracks ?? new List<StatUpgradeData>();
            prestigeUpgrades = permanentUpgrades ?? new List<PrestigeUpgradeData>();
            automationDefs = automationCards ?? new List<AutomationDef>();
        }
#endif
    }
}
