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
    public sealed class GameManager : MonoBehaviour
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

        /// <summary>Cleared by DefeatState, restored by a manual retry.</summary>
        public bool AutoRetryEnabled { get; private set; } = true;

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
                LogFlow($"Resumed save: stage {CurrentStage} wave {RestoredWave} (auto-retry {AutoRetryEnabled}).");
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
            Upgrade = new UpgradeManager(Economy, Resolver, partyConfig);
            Ascension = new AscensionManager(balanceConfig, Economy, Resolver, prestigeUpgrades);
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
                Formation.PlaceInDefaultSlots(partyConfig.ValidHeroCount, 1);

                // Any swap (the Team screen in Step 10c, or a debug tool) marks the save dirty; Save is null
                // during this first placement, which the null-conditional handles.
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
        private void SubscribeProgression()
        {
            GameEvents.UpgradePurchased += OnUpgradePurchasedForSave;
            GameEvents.AscensionCompleted += OnAscensionCompletedForSave;
        }

        private void UnsubscribeProgression()
        {
            GameEvents.UpgradePurchased -= OnUpgradePurchasedForSave;
            GameEvents.AscensionCompleted -= OnAscensionCompletedForSave;
        }

        private void OnUpgradePurchasedForSave(int heroIndex, HeroStatType statType, int newLevel, double goldCost)
        {
            Save?.MarkDirty("upgrade");
        }

        private void OnAscensionCompletedForSave(double tokens, int highestStage)
        {
            AscensionCount++;
            Save?.MarkDirty("ascension");
        }

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

            double awarded = Rewards != null
                ? Rewards.GrantGold(goldReward, RewardService.Source.Combat)
                : ResolveGoldReward(goldReward);

            Rewards?.RecordKill();

            TotalKills++;
            TotalGoldEarned += awarded;
            Save?.MarkDirty("kill");
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
                if (Rewards != null)
                {
                    Rewards.GrantGems(balanceConfig.GemsPerBossKill, RewardService.Source.Combat);
                }
            }

            SetState(GameState.Boss, GameState.Combat);
            combatManager.SetStage(CurrentStage, healParty: balanceConfig != null && balanceConfig.HealHeroesOnStageAdvance);
            RaiseStageChanged();

            Save?.MarkDirty("stage");
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
        // Save API
        // ------------------------------------------------------------------
        /// <summary>Writes the save right now (F5, menus, tests).</summary>
        public bool SaveNow()
        {
            return Save != null && Save.SaveNow("manual");
        }

        /// <summary>Wipes the save (debug tooling / future reset button).</summary>
        public void DeleteSave()
        {
            Idle?.ClearPending();
            Save?.DeleteSave();
        }

        /// <summary>
        /// The nuclear option: a fresh install. Wipes the save, its backups and PlayerPrefs, then reloads the
        /// scene so nothing survives in memory either.
        ///
        /// ELI5: `DeleteSave` only deletes the file on disk - the game keeps running with everything the player
        /// already earned, and the next autosave writes it all back. This one empties the disk *and* starts the
        /// scene again from scratch, which is what a "start over" button (or a corrupted-state bug report) needs.
        /// </summary>
        public bool ResetGame()
        {
            Idle?.ClearPending();
            Save?.DeleteSave();          // save file, every backup generation, logout timestamp

            // A fresh install has no preferences either (audio volume/mute live here).
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            Scene scene = SceneManager.GetActiveScene();

            if (scene.buildIndex < 0)
            {
                Debug.LogWarning($"[GameManager] Reset wiped disk state, but scene '{scene.name}' is not in Build " +
                                 "Settings, so it cannot be reloaded; stop and start Play mode for a clean run.");
                return false;
            }

            Debug.Log($"[GameManager] Full reset: save, backups and PlayerPrefs wiped; reloading '{scene.name}'.");
            SceneManager.LoadScene(scene.buildIndex, LoadSceneMode.Single);
            return true;
        }

        /// <summary>
        /// Offers the offline reward for the time since the last session ended.
        /// Called once on start; also used by the debug tooling to re-run the calculation.
        /// </summary>
        public OfflineRewardResult EvaluateOffline()
        {
            if (Idle == null)
            {
                return OfflineRewardResult.None;
            }

            double lastLogout = ResolveLastLogoutBinary();

            if (lastLogout <= 0d)
            {
                return OfflineRewardResult.None;
            }

            double savedRate = Ledger != null ? Ledger.GoldPerSecond : 0d;
            OfflineRewardResult result = Idle.Evaluate(lastLogout, CurrentStage, savedRate);

            if (result.HasReward)
            {
                LogFlow($"Offline: {result.RawSeconds:0}s away -> {result.CappedSeconds:0}s paid, " +
                        $"{result.Gold:0.#} gold at {result.GoldPerSecond:0.##}/s ({Idle.LastRateSource})");

                // Consume the window straight away so a kill before claiming cannot pay twice.
                Save?.SaveNow("offline");
                GameEvents.RaiseOfflineRewardsReady(result);
            }

            return result;
        }

        /// <summary>
        /// Most recent of the two logout timestamps (save file and PlayerPrefs). Taking the newer one
        /// means a hard process kill can never inflate the offline window.
        /// </summary>
        private double ResolveLastLogoutBinary()
        {
            double fromSave = Save != null ? Save.LastLogoutBinary : 0d;
            double fromPrefs = 0d;
            bool hasPrefs = SaveManager.TryReadPlayerPrefsLogout(out fromPrefs);

            if (hasPrefs && fromPrefs > fromSave)
            {
                return fromPrefs;
            }

            return fromSave;
        }

        /// <summary>
        /// A board change is real progress: write it on the next autosave **and** re-stamp the live fight, so a
        /// swap always takes effect on the next swing (no caller has to remember to call ApplyFormation).
        /// </summary>
        private void OnFormationChanged()
        {
            Save?.MarkDirty("formation");
            combatManager?.Simulator?.ApplyFormation();
        }

        private void OnIdleRewardPaid(double gold)
        {
            // Both time payouts land here: the offline claim and a bought fast-forward.
            TotalGoldEarned += gold;
            Save?.MarkDirty("idle-income");
        }

        /// <summary>
        /// Gem sink #2: buys <c>instantIncomeSeconds</c> of income outright ("fast-forward").
        /// Quote first, then charge, then pay - so gems are never spent on a payout of zero.
        /// </summary>
        public bool BuyInstantIncome()
        {
            if (Shop == null || Idle == null)
            {
                return false;
            }

            double savedRate = Ledger != null ? Ledger.GoldPerSecond : 0d;
            double quote = Idle.QuoteInstantIncome(CurrentStage, savedRate);

            if (quote <= 0d)
            {
                GameEvents.RaiseToast("No income to fast-forward yet");
                return false;
            }

            if (!Shop.CanAffordInstantIncome)
            {
                GameEvents.RaiseToast($"Needs {Shop.InstantIncomeGemCost:0} gems");
                return false;
            }

            double gold = Idle.TryGrantInstantIncome(CurrentStage, savedRate, Shop.TrySpendInstantIncomeGems);

            if (gold <= 0d)
            {
                return false;
            }

            GameEvents.RaiseToast($"+{NumberFormatter.Format(gold)} gold");
            LogFlow($"Instant income: {Shop.InstantIncomeSeconds / 60d:0} min for {Shop.InstantIncomeGemCost:0} gems" +
                    $" -> {gold:0.#} gold (purchase #{Idle.InstantIncomePurchases})");
            return true;
        }

        /// <summary>Remembers which management tab the player was on.</summary>
        public void SetLastPageIndex(int index)
        {
            LastPageIndex = Mathf.Max(0, index);
            Save?.MarkDirty("page");
        }

        /// <summary>Builds the payload written to disk. Called by <see cref="SaveManager"/>.</summary>
        private SaveData CaptureSnapshot()
        {
            SaveData data = SaveData.CreateDefault();

            data.currentStage = CurrentStage;
            data.currentWave = combatManager != null ? combatManager.CurrentWave : 1;
            data.highestStageReached = HighestStageReached;
            data.autoRetryEnabled = AutoRetryEnabled;

            data.gold = Economy != null ? Economy.Gold : 0d;
            data.gems = Economy != null ? Economy.Gems : 0d;
            data.prestigeTokens = Economy != null ? Economy.PrestigeTokens : 0d;

            Resolver?.WriteToSave(data, partyConfig);
            Boost?.WriteToSave(data);
            if (Ledger != null)
            {
                data.lastGoldPerSecond = Ledger.GoldPerSecond;
            }

            Shop?.WriteToSave(data);

            if (Formation != null)
            {
                data.partySlots = new List<int>(Formation.ToSlotArray());
            }

            data.totalKills = TotalKills;
            data.totalGoldEarned = TotalGoldEarned;
            data.ascensionCount = AscensionCount;
            data.lastPageIndex = LastPageIndex;

            return data;
        }

        /// <summary>Pushes a loaded payload into the live systems (before combat starts).</summary>
        private void ApplySnapshot(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            // Formation first: the board has to be right before the party is built from it.
            if (Formation != null && data.partySlots != null && data.partySlots.Count > 0)
            {
                Formation.ApplySlotArray(data.partySlots.ToArray());
            }

            CurrentStage = Mathf.Max(1, data.currentStage);
            RestoredWave = Mathf.Max(1, data.currentWave);
            HighestStageReached = Mathf.Max(1, data.highestStageReached, CurrentStage);
            AutoRetryEnabled = data.autoRetryEnabled;

            TotalKills = data.totalKills;
            TotalGoldEarned = data.totalGoldEarned;
            AscensionCount = data.ascensionCount;
            LastPageIndex = data.lastPageIndex;

            Economy?.Restore(data.gold, data.gems, data.prestigeTokens);
            Resolver?.FillFromSave(data, partyConfig);
            Boost?.Restore(data.goldBoostActive, data.goldBoostExpiresAtBinary);
            Ledger?.SeedGoldPerSecond(data.lastGoldPerSecond);
            Shop?.Restore(data.offlineEquivalentCapBonusSeconds, data.offlineCapExtensionsPurchased);

            // Give the loaded values to combat (Initialize() would reset the stage to 1).
            combatManager.SetProgress(CurrentStage, RestoredWave, healParty: true);

            GameEvents.RaiseSaveLoaded();
            LogFlow($"Loaded save: stage {CurrentStage} wave {RestoredWave} best {HighestStageReached} | {Economy}");
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
        public void EditorInitializeProgression(List<StatUpgradeData> statTracks, List<PrestigeUpgradeData> permanentUpgrades)
        {
            statUpgrades = statTracks ?? new List<StatUpgradeData>();
            prestigeUpgrades = permanentUpgrades ?? new List<PrestigeUpgradeData>();
        }
#endif
    }
}