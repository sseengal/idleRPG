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
            }
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
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Save?.SaveNow("pause");
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

        private bool WireUp()
        {
            if (formationConfig == null)
            {
                Debug.LogError("[GameManager] No FormationData assigned; the party falls back to fixed lanes.");
            }

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

            // Step 14a (B5 session 1): one checkout for every progression row. StatResolver still owns the levels.
            float fallbackGrowth = balanceConfig != null ? balanceConfig.UpgradeCostGrowth : 1.07f;
            Tracks = new TrackService(Economy, Resolver, statUpgrades, prestigeUpgrades, automationDefs, fallbackGrowth);

            Upgrade = new UpgradeManager(Economy, Resolver, partyConfig, Tracks);
            Ascension = new AscensionManager(balanceConfig, Economy, Resolver, prestigeUpgrades, Tracks);

            // B6: the automation engine. Cards are bought with tokens; the auto-buy machine runs on the 1s tick.
            Automation = new AutomationService(Tracks, Economy, partyConfig, automationDefs);
            Automation.Changed += OnAutomationChanged;
            Boost = new BoostManager(balanceConfig);
            Ads = new MockAdService(this, 3f, true);

            // Step 9b-3: placeholder SFX (procedural tones) driven by the event bus.
            Audio = new PlaceholderAudioService(this);
            AudioDirector.Create(gameObject, Audio);

            Ledger = new SimLedger(balanceConfig != null ? balanceConfig.GoldPerSecondSampleWindowSec : 60f);
            Rewards = new RewardService(Economy, Ledger, ResolveGoldReward);
            Shop = new ShopService(balanceConfig, Economy);

            // Step 10a/10b: the board the party stands on. Created **before** the load so a saved layout can be
            // restored into it; with no saved layout the default placement (the MVP's fixed lanes) is used, so a
            // fresh game - and an upgraded v2 save - behaves exactly as before.
            if (formationConfig != null)
            {
                Formation = new Formation(formationConfig);
                Formation.PlaceInDefaultSlots(partyConfig.ValidHeroCount);

                // Any swap (the Party screen, or a debug tool) marks the save dirty and re-stamps the fight; Save
                // is null during this first placement, which the null-conditional handles.
                Formation.Changed += OnFormationChanged;
            }

            Save = new SaveManager(new SaveSystem(), balanceConfig, CaptureSnapshot);
            LoadedFromSave = Save.TryLoad(out SaveData loaded);

            if (LoadedFromSave)
            {
                ApplySnapshot(loaded);

                if (Save.RecoveredFromBackup)
                {
                    // Rewrite straight away so the damaged file is replaced by good data.
                    Save.SaveNow("recovery");
                }
            }

            if (!combatManager.Initialize(balanceConfig, waveConfig, partyConfig, Formation))
            {
                Debug.LogError("[GameManager] CombatManager failed to initialise (check PartyConfig heroes).");
                return false;
            }

            combatManager.SetStatProvider(Resolver);

            if (LoadedFromSave)
            {
                // Resume exactly where the player left off, healing the party like a retry would.
                combatManager.SetProgress(CurrentStage, RestoredWave, healParty: true);
            }
            else
            {
                combatManager.SetStage(CurrentStage, healParty: true);
            }

            // Step 9b-2: one owner for time-based payouts (offline window + instant income).
            Idle = new IdleTimeService(balanceConfig, waveConfig, Rewards, Ledger, ResolveGoldReward)
            {
                BonusEquivalentCapSeconds = Shop != null ? Shop.OfflineCapBonusSeconds : 0d
            };
            Idle.Attach();
            Idle.RewardPaid += OnIdleRewardPaid;

            SubscribeCombat();
            SubscribeProgression();
            SetState(GameState.Boot, GameState.Combat);

            Debug.Log($"[GameManager] Save load: {Save.LastLoadSource} | stage {CurrentStage} wave {RestoredWave} | {Economy}");

            BuildContext();

            return true;
        }

#if UNITY_EDITOR

        /// <summary>
        /// Editor-only wiring hook used by the authoring tools. Keeps the serialized
        /// fields private while avoiding reflection/SerializedObject in scene builders.
        /// </summary>
        public void EditorInitialize(BalanceConfig balance, WaveConfig waves, PartyConfig party, CombatManager combat,
            FormationData formation = null)
        {
            balanceConfig = balance;
            waveConfig = waves;
            partyConfig = party;
            combatManager = combat;

            if (formation != null)
            {
                formationConfig = formation;
            }
        }
#endif

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

        /// <summary>One-second tick for time-based systems (boost expiry; autosave joins in Step 5).</summary>
        /// <summary>
        /// Fills the <see cref="GameContext"/> and starts the single heartbeat. Everything exists by this
        /// point, so the box is complete the moment anything can read it.
        /// </summary>
        private void BuildContext()
        {
            Context = new GameContext
            {
                Balance = balanceConfig,
                Waves = waveConfig,
                Party = partyConfig,
                Formation = Formation,
                Combat = combatManager,
                Economy = Economy,
                Resolver = Resolver,
                Upgrade = Upgrade,
                Ascension = Ascension,
                Automation = Automation,
                Boost = Boost,
                Ads = Ads,
                Audio = Audio,
                Ledger = Ledger,
                Rewards = Rewards,
                Shop = Shop,
                Save = Save,
                Idle = Idle
            };

            if (runner == null)
            {
                runner = gameObject.GetComponent<RunController>();

                if (runner == null)
                {
                    runner = gameObject.AddComponent<RunController>();
                }
            }

            Telemetry = new TelemetryFeed();
            Telemetry.Attach();

            runner.Attach(Context, true);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Dev-only dashboard (F3); never compiled into a release build.
            DevOverlay.Create(gameObject, Context, Telemetry);
#endif

            LogFlow($"Run controller attached | {Context}");
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
