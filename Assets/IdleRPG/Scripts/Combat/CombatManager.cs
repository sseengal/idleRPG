using System;
using System.Collections;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Progression;

namespace IdleRPG.Combat
{
    /// <summary>
    /// Drives the auto-battle: owns the ticker, the wave index inside a stage and the
    /// boss encounter trigger. Stage progression and the game state machine live in
    /// <see cref="GameManager"/>, which reacts to the events raised here.
    ///
    /// No <c>Update()</c> polling and no per-entity scripts: one coroutine advances a
    /// pure <see cref="CombatSimulator"/> in fixed steps and mirrors results into
    /// <see cref="GameEvents"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatManager : MonoBehaviour
    {
        /// <summary>Hard cap on simulation steps per frame to avoid a death spiral.</summary>
        private const int MaxStepsPerFrame = 12;

        [Header("Debug")]
        [Tooltip("Also mirror every combat event into the Unity console.")]
        [SerializeField] private bool logCombatEvents = true;

        private BalanceConfig balanceConfig;
        private WaveConfig waveConfig;
        private PartyConfig partyConfig;

        private CombatSimulator simulator;
        private Coroutine tickerRoutine;
        private ICombatStatProvider statProvider = DefaultStatProvider.Instance;

        private double stepAccumulator;
        private bool running;
        private bool waveAdvancePending;
        private int currentStage = 1;
        private int currentWave = 1;

        // ------------------------------------------------------------------
        // Events consumed by GameManager
        // ------------------------------------------------------------------
        /// <summary>Enemy died, carrying its gold reward.</summary>
        public event Action<double> EnemyKilled;

        /// <summary>(stage, wave) after a non-boss wave is cleared.</summary>
        public event Action<int, int> WaveCleared;

        /// <summary>(stage) after the stage boss dies.</summary>
        public event Action<int> StageCleared;

        /// <summary>All heroes are dead.</summary>
        public event Action PartyWiped;

        /// <summary>Raised when the wipe happened during a boss encounter.</summary>
        public event Action BossFailed;

        /// <summary>(stage, wave, isBossWave) whenever a new enemy is spawned.</summary>
        public event Action<int, int, bool> WaveStarted;

        // ------------------------------------------------------------------
        // Read-only state
        // ------------------------------------------------------------------
        public bool IsRunning => running;

        public bool IsInitialised => simulator != null;

        public int CurrentStage => currentStage;

        public int CurrentWave => currentWave;

        /// <summary>Total waves in a stage: N normal waves + 1 boss wave.</summary>
        public int WavesPerStage => balanceConfig == null ? 11 : balanceConfig.NormalWavesPerStage + 1;

        public bool IsBossWave { get; private set; }

        public CombatSimulator Simulator => simulator;

        public ICombatStatProvider StatProvider => statProvider;

        public string CurrentEnemyName => simulator == null || simulator.Enemy == null ? "-" : simulator.Enemy.DisplayName;

        public double CurrentEnemyHealthPercent => simulator == null ? 0d : simulator.EnemyHealthPercent;

        /// <summary>Swaps the stat source (Step 3 injects the upgrade/prestige provider).</summary>
        public void SetStatProvider(ICombatStatProvider provider)
        {
            statProvider = provider ?? DefaultStatProvider.Instance;

            if (simulator != null)
            {
                simulator.RefreshHeroStats();
            }
        }

        // ------------------------------------------------------------------
        // Setup
        // ------------------------------------------------------------------
        /// <summary>Wires the data assets and builds the party. Returns false on bad data.</summary>
        public bool Initialize(BalanceConfig balance, WaveConfig waves, PartyConfig party)
        {
            if (balance == null || waves == null || party == null)
            {
                Debug.LogError("[CombatManager] Initialize needs BalanceConfig, WaveConfig and PartyConfig.");
                return false;
            }

            balanceConfig = balance;
            waveConfig = waves;
            partyConfig = party;

            simulator = new CombatSimulator(statProvider, balanceConfig.EnemyTargeting, BuildScaling(), 20240919);
            simulator.EnemyDamaged += OnSimEnemyDamaged;
            simulator.EnemyKilled += OnSimEnemyKilled;
            simulator.HeroDamaged += OnSimHeroDamaged;
            simulator.HeroDied += OnSimHeroDied;
            simulator.PartyWiped += OnSimPartyWiped;

            if (!simulator.SetupParty(partyConfig))
            {
                return false;
            }

            currentStage = 1;
            currentWave = 1;
            IsBossWave = false;

            return true;
        }

        /// <summary>Points the run at a stage and optionally heals the party.</summary>
        public void SetStage(int stage, bool healParty)
        {
            currentStage = Mathf.Max(1, stage);
            currentWave = 1;
            IsBossWave = false;

            if (healParty && simulator != null)
            {
                simulator.HealParty();
            }

            if (running)
            {
                BeginWave();
            }
        }

        public void StartRun()
        {
            if (simulator == null)
            {
                Debug.LogError("[CombatManager] StartRun called before Initialize.");
                return;
            }

            if (running)
            {
                return;
            }

            running = true;
            stepAccumulator = 0d;
            BeginWave();
            tickerRoutine = StartCoroutine(TickerLoop());
        }

        public void StopRun()
        {
            running = false;
            waveAdvancePending = false;

            if (tickerRoutine != null)
            {
                StopCoroutine(tickerRoutine);
                tickerRoutine = null;
            }

            if (simulator != null)
            {
                simulator.AbortEncounter();
            }
        }

        private void OnDestroy()
        {
            if (simulator == null)
            {
                return;
            }

            simulator.EnemyDamaged -= OnSimEnemyDamaged;
            simulator.EnemyKilled -= OnSimEnemyKilled;
            simulator.HeroDamaged -= OnSimHeroDamaged;
            simulator.HeroDied -= OnSimHeroDied;
            simulator.PartyWiped -= OnSimPartyWiped;
        }

        private CombatScaling BuildScaling()
        {
            CombatScaling scaling = CombatScaling.Default;
            scaling.EnemyHealthGrowth = balanceConfig.EnemyHealthGrowth;
            scaling.EnemyGoldGrowth = balanceConfig.EnemyGoldGrowth;
            scaling.EnemyAttackGrowth = balanceConfig.EnemyAttackGrowth;
            scaling.ScaleEnemyDefenseWithStage = balanceConfig.ScaleEnemyDefenseWithStage;
            scaling.MinDamageRatio = balanceConfig.MinDamageRatio;
            scaling.CriticalChance = balanceConfig.CriticalChance;
            scaling.CriticalDamageMultiplier = balanceConfig.CriticalDamageMultiplier;

            return scaling.Sanitized();
        }

        // ------------------------------------------------------------------
        // Wave flow
        // ------------------------------------------------------------------
        /// <summary>Spawns the enemy for the current stage/wave and announces it.</summary>
        private void BeginWave()
        {
            if (simulator == null || waveConfig == null || balanceConfig == null)
            {
                return;
            }

            IsBossWave = WaveConfig.IsBossWave(currentWave, balanceConfig.NormalWavesPerStage);

            EnemyData enemyData = waveConfig.GetEnemyFor(currentStage, currentWave, balanceConfig.NormalWavesPerStage);
            if (enemyData == null || !simulator.StartEncounter(enemyData, currentStage, IsBossWave))
            {
                Debug.LogError($"[CombatManager] No enemy available for stage {currentStage} wave {currentWave}; stopping the run.");
                StopRun();
                return;
            }

            waveAdvancePending = false;

            GameEvents.RaiseEnemySpawned(simulator.Enemy.DisplayName, simulator.Enemy.MaxHealth, IsBossWave);
            WaveStarted?.Invoke(currentStage, currentWave, IsBossWave);

            LogCombat($"Stage {currentStage} | Wave {currentWave}/{WavesPerStage}{(IsBossWave ? " (BOSS)" : string.Empty)} -> " +
                      $"{simulator.Enemy.DisplayName} HP {simulator.Enemy.MaxHealth:0} ATK {simulator.Enemy.Attack:0} " +
                      $"gold {simulator.Enemy.GoldReward:0}");
        }

        /// <summary>Fixed-step ticker: one coroutine, no per-entity Update().</summary>
        private IEnumerator TickerLoop()
        {
            while (running)
            {
                stepAccumulator += Time.deltaTime;

                double tick = balanceConfig.CombatTickIntervalSec;
                int steps = 0;

                while (running && !waveAdvancePending && stepAccumulator >= tick && steps < MaxStepsPerFrame)
                {
                    stepAccumulator -= tick;
                    simulator.Step(tick);
                    steps++;
                }

                if (waveAdvancePending)
                {
                    waveAdvancePending = false;
                    yield return new WaitForSeconds(balanceConfig.WaveTransitionDelaySec);
                    HandleWaveResolved();
                    stepAccumulator = 0d;
                    continue;
                }

                yield return null;
            }
        }

        /// <summary>Advances to the next wave, or hands the cleared stage back to GameManager.</summary>
        private void HandleWaveResolved()
        {
            if (!running)
            {
                return;
            }

            if (IsBossWave)
            {
                // GameManager owns stage progression and calls SetStage() back into this class.
                StageCleared?.Invoke(currentStage);
                return;
            }

            int clearedWave = currentWave;
            WaveCleared?.Invoke(currentStage, clearedWave);
            GameEvents.RaiseWaveCompleted(currentStage, clearedWave);

            currentWave++;
            BeginWave();
        }

        // ------------------------------------------------------------------
        // Simulation event mirrors
        // ------------------------------------------------------------------
        private void OnSimEnemyDamaged(EnemyDamagedInfo info)
        {
            GameEvents.RaiseEnemyDamaged(info);

            if (logCombatEvents)
            {
                Debug.Log($"[Combat] {simulator.Enemy?.DisplayName} took {info.Damage:0.#}" +
                          $"{(info.IsCritical ? " CRIT" : string.Empty)} -> {info.CurrentHealth:0}/{info.MaxHealth:0}");
            }
        }

        private void OnSimEnemyKilled(double goldReward)
        {
            waveAdvancePending = true;
            string enemyName = simulator.Enemy == null ? "Enemy" : simulator.Enemy.DisplayName;

            EnemyKilled?.Invoke(goldReward);
            GameEvents.RaiseEnemyKilled(enemyName, goldReward);

            LogCombat($"{enemyName} killed -> +{goldReward:0} gold");
        }

        private void OnSimHeroDamaged(int heroIndex, double damage, double currentHealth, double maxHealth)
        {
            GameEvents.RaiseHeroDamaged(heroIndex, damage, currentHealth, maxHealth);

            if (!logCombatEvents)
            {
                return;
            }

            string heroName = simulator.Heroes.Length > heroIndex && simulator.Heroes[heroIndex] != null
                ? simulator.Heroes[heroIndex].DisplayName
                : $"Hero {heroIndex}";

            Debug.Log($"[Combat] {heroName} took {damage:0.#} -> {currentHealth:0}/{maxHealth:0}");
        }

        private void OnSimHeroDied(int heroIndex)
        {
            string heroName = simulator.Heroes.Length > heroIndex && simulator.Heroes[heroIndex] != null
                ? simulator.Heroes[heroIndex].DisplayName
                : $"Hero {heroIndex}";

            GameEvents.RaiseHeroDied(heroIndex);
            LogCombat($"{heroName} died");
        }

        private void OnSimPartyWiped()
        {
            running = false;

            GameEvents.RaisePartyWiped();
            PartyWiped?.Invoke();

            if (IsBossWave)
            {
                GameEvents.RaiseBossFailed();
                BossFailed?.Invoke();
            }

            LogCombat("PARTY WIPED");
        }

        private void LogCombat(string message)
        {
            if (logCombatEvents)
            {
                Debug.Log($"[CombatManager] {message}");
            }
        }
    }
}