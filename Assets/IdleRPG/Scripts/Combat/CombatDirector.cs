using System;
using IdleRPG.Data;
using IdleRPG.Sim;

namespace IdleRPG.Combat
{
    /// <summary>
    /// Drives the wave/encounter choreography: catch-up stepping, the pause between waves, and the
    /// hand-off when a stage boss dies.
    ///
    /// ELI5: the old code *went to sleep* between waves (`WaitForSeconds`), so a replay could never reproduce
    /// a live fight exactly. This metronome counts the pause down itself instead, which means the very same
    /// code runs at 60fps on a phone, at x2 in fast-forward, and 12,000 steps at once for an offline
    /// estimate - and all three give the same answer.
    ///
    /// Plain C#: the MonoBehaviour wrapper (<see cref="CombatManager"/>) forwards <see cref="Tick"/>.
    /// </summary>
    public sealed class CombatDirector
    {
        /// <summary>Hard cap on catch-up steps per frame so a stall can never spiral.</summary>
        public const int MaxStepsPerFrame = 12;

        private BalanceConfig balanceConfig;
        private WaveConfig waveConfig;
        private CombatSimulator simulator;

        private double stepAccumulator;
        private double transitionRemaining;
        private bool transitionPending;
        private bool running;

        // --- Events (the wrapper re-raises the ones GameManager listens to) ---
        public event Action<double> EnemyKilled;
        public event Action<int, int> WaveCleared;
        public event Action<int> StageCleared;
        public event Action PartyWiped;
        public event Action BossFailed;
        public event Action<int, int, bool> WaveStarted;

        // --- State ---
        public int CurrentStage { get; private set; } = 1;

        public int CurrentWave { get; private set; } = 1;

        public bool IsBossWave { get; private set; }

        public bool IsRunning => running;

        public int WavesPerStage => balanceConfig == null ? 11 : balanceConfig.NormalWavesPerStage + 1;

        public double TransitionRemaining => transitionRemaining;

        /// <summary>Wires the data assets and the simulator. Returns false on bad data.</summary>
        public bool Initialize(BalanceConfig balance, WaveConfig waves, CombatSimulator combatSimulator)
        {
            if (balance == null || waves == null || combatSimulator == null)
            {
                SimLog.LogError("[CombatDirector] Initialize needs BalanceConfig, WaveConfig and a simulator.");
                return false;
            }

            balanceConfig = balance;
            waveConfig = waves;
            simulator = combatSimulator;

            CurrentStage = 1;
            CurrentWave = 1;
            IsBossWave = false;

            return true;
        }

        /// <summary>Starts the run at the current stage/wave.</summary>
        public void Start()
        {
            if (simulator == null)
            {
                SimLog.LogError("[CombatDirector] Start called before Initialize.");
                return;
            }

            if (running)
            {
                return;
            }

            running = true;
            stepAccumulator = 0d;
            transitionPending = false;
            transitionRemaining = 0d;

            BeginWave();
        }

        public void Stop()
        {
            running = false;
            transitionPending = false;
            transitionRemaining = 0d;
            simulator?.AbortEncounter();
        }

        /// <summary>
        /// Restores saved progress: a stage *and* the wave inside it, so a player who saved mid-stage resumes
        /// there instead of replaying the stage from wave 1.
        /// </summary>
        public void SetProgress(int stage, int wave, bool healParty)
        {
            CurrentStage = stage < 1 ? 1 : stage;
            CurrentWave = Clamp(wave, 1, WavesPerStage);
            IsBossWave = WaveConfig.IsBossWave(CurrentWave, NormalWaves());

            if (healParty)
            {
                simulator?.HealParty();
            }

            if (running)
            {
                BeginWave();
            }
        }

        /// <summary>Points the run at a stage and optionally heals the party.</summary>
        public void SetStage(int stage, bool healParty)
        {
            CurrentStage = stage < 1 ? 1 : stage;
            CurrentWave = 1;
            IsBossWave = false;

            if (healParty)
            {
                simulator?.HealParty();
            }

            if (running)
            {
                BeginWave();
            }
        }

        /// <summary>
        /// The single gameplay tick. Catch-up steps are capped so a frame hitch can never spiral, and a wave
        /// that finished starts its inter-wave pause (counted in simulated time, so fast-forward works).
        /// </summary>
        public void Tick(double deltaTime)
        {
            if (!running || simulator == null || balanceConfig == null || deltaTime <= 0d)
            {
                return;
            }

            if (transitionPending)
            {
                transitionRemaining -= deltaTime;

                if (transitionRemaining > 0d)
                {
                    return;
                }

                transitionPending = false;
                transitionRemaining = 0d;
                HandleWaveResolved();
                stepAccumulator = 0d;
                return;
            }

            stepAccumulator += deltaTime;

            double tick = balanceConfig.CombatTickIntervalSec;
            int steps = 0;

            while (running && !transitionPending && stepAccumulator >= tick && steps < MaxStepsPerFrame)
            {
                stepAccumulator -= tick;
                simulator.Step(tick);
                steps++;
            }

            if (steps >= MaxStepsPerFrame && stepAccumulator > tick)
            {
                // Dropped time (a stall or a huge chunk): never build a backlog of steps.
                stepAccumulator = 0d;
            }
        }

        /// <summary>Called by the wrapper when the sim reports a kill: starts the inter-wave pause.</summary>
        public void NotifyEnemyKilled(double goldReward)
        {
            transitionPending = true;
            transitionRemaining = balanceConfig != null ? balanceConfig.WaveTransitionDelaySec : 0d;
            stepAccumulator = 0d;

            EnemyKilled?.Invoke(goldReward);
        }

        /// <summary>Called by the wrapper when the sim reports a party wipe.</summary>
        public void NotifyPartyWiped()
        {
            running = false;
            transitionPending = false;

            PartyWiped?.Invoke();

            if (IsBossWave)
            {
                BossFailed?.Invoke();
            }
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------
        private void BeginWave()
        {
            if (simulator == null || waveConfig == null || balanceConfig == null)
            {
                return;
            }

            IsBossWave = WaveConfig.IsBossWave(CurrentWave, NormalWaves());

            // The factory owns composition, ranks and the per-wave budget (Step 11a); with one enemy per wave it
            // returns exactly what the old single-enemy path did.
            EnemyCombatant[] team = EncounterFactory.Build(
                waveConfig, balanceConfig, CurrentStage, CurrentWave, IsBossWave, simulator.Context.Rules);

            if (team == null || team.Length == 0 || !simulator.StartEncounter(team))
            {
                SimLog.LogError($"[CombatDirector] No enemy for stage {CurrentStage} wave {CurrentWave}; stopping.");
                running = false;
                return;
            }

            transitionPending = false;
            WaveStarted?.Invoke(CurrentStage, CurrentWave, IsBossWave);
        }

        /// <summary>Advances to the next wave, or hands the cleared stage back to the run owner.</summary>
        private void HandleWaveResolved()
        {
            if (!running)
            {
                return;
            }

            if (IsBossWave)
            {
                // Stage progression belongs to the caller (GameManager), which calls SetStage() back in.
                StageCleared?.Invoke(CurrentStage);
                return;
            }

            int clearedWave = CurrentWave;
            WaveCleared?.Invoke(CurrentStage, clearedWave);

            CurrentWave++;
            BeginWave();
        }

        private int NormalWaves()
        {
            return balanceConfig != null ? balanceConfig.NormalWavesPerStage : 10;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}

