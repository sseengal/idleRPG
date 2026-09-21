using System;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Sim;

namespace IdleRPG.Combat
{
    /// <summary>
    /// The scene's combat component: owns the simulator plus the <see cref="CombatDirector"/> and translates
    /// sim results into <see cref="GameEvents"/>.
    ///
    /// ELI5: this used to be the fight engine AND the wave choreographer AND a coroutine that slept between
    /// waves. The engine now lives in `Sim/` (`Encounter`), the choreography in <see cref="CombatDirector"/>,
    /// and this class is just the plug. Scene hierarchy and public API are unchanged.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatManager : MonoBehaviour
    {
        [Header("Debug")]
        [Tooltip("Also mirror every combat event into the Unity console.")]
        [SerializeField] private bool logCombatEvents = true;

        private BalanceConfig balanceConfig;
        private WaveConfig waveConfig;
        private PartyConfig partyConfig;

        private CombatSimulator simulator;
        private CombatDirector director;
        private ICombatStatProvider statProvider = DefaultStatProvider.Instance;

        // --- Events consumed by GameManager ---
        public event Action<double> EnemyKilled;
        public event Action<int, int> WaveCleared;
        public event Action<int> StageCleared;
        public event Action PartyWiped;
        public event Action BossFailed;
        public event Action<int, int, bool> WaveStarted;

        // --- State ---
        public CombatSimulator Simulator => simulator;

        public CombatDirector Director => director;

        public int CurrentStage => director == null ? 1 : director.CurrentStage;

        public int CurrentWave => director == null ? 1 : director.CurrentWave;

        public int WavesPerStage => director == null ? 11 : director.WavesPerStage;

        public bool IsBossWave => director != null && director.IsBossWave;

        public bool IsRunning => director != null && director.IsRunning;

        /// <summary>Wave label for the HUD: one name, or "Goblin +2" when the wave holds several enemies.</summary>
        public string CurrentEnemyName
        {
            get
            {
                if (simulator == null || simulator.Enemies == null || simulator.Enemies.Length == 0)
                {
                    return "-";
                }

                string name = simulator.Enemies[0] == null ? "-" : simulator.Enemies[0].DisplayName;
                int extra = simulator.Enemies.Length - 1;
                return extra > 0 ? $"{name} +{extra}" : name;
            }
        }

        public double CurrentEnemyHealthPercent => simulator == null ? 0d : simulator.EnemyHealthPercent;

        /// <summary>Who stands where (formation). Null = the legacy fixed lanes.</summary>
        public Formation Formation => simulator != null ? simulator.Formation : null;

        /// <summary>Wires the data assets, builds the party and prepares the director.</summary>
        public bool Initialize(BalanceConfig balance, WaveConfig waves, PartyConfig party, Formation formation = null)
        {
            if (balance == null || waves == null || party == null)
            {
                Debug.LogError("[CombatManager] Initialize needs BalanceConfig, WaveConfig and PartyConfig.");
                return false;
            }

            balanceConfig = balance;
            waveConfig = waves;
            partyConfig = party;

            simulator = new CombatSimulator(statProvider, balanceConfig.EnemyTargeting, SimContext.CreateDefault(), 20240919);
            simulator.ApplyScaling(SimRulesFactory.ScalingFromBalance(balanceConfig));
            simulator.EnemyDamaged += OnSimEnemyDamaged;
            simulator.EnemyKilled += OnSimEnemyKilled;
            simulator.HeroDamaged += OnSimHeroDamaged;
            simulator.HeroDied += OnSimHeroDied;
            simulator.PartyWiped += OnSimPartyWiped;

            if (!simulator.SetupParty(partyConfig, formation))
            {
                return false;
            }

            director = new CombatDirector();
            director.WaveStarted += OnDirectorWaveStarted;
            director.WaveCleared += (stage, wave) => WaveCleared?.Invoke(stage, wave);
            director.StageCleared += stage => StageCleared?.Invoke(stage);
            director.EnemyKilled += gold => EnemyKilled?.Invoke(gold);
            director.PartyWiped += () => PartyWiped?.Invoke();
            director.BossFailed += () => BossFailed?.Invoke();

            return director.Initialize(balanceConfig, waveConfig, simulator);
        }

        /// <summary>Re-applies the balance asset to the running simulation.</summary>
        public void ApplyBalance()
        {
            if (simulator != null && balanceConfig != null)
            {
                simulator.ApplyScaling(SimRulesFactory.ScalingFromBalance(balanceConfig));
            }
        }

        public void SetStatProvider(ICombatStatProvider provider)
        {
            statProvider = provider ?? DefaultStatProvider.Instance;

            if (simulator != null)
            {
                simulator.SetStatProvider(statProvider);
            }
        }

        /// <summary>Restores saved progress (stage + wave).</summary>
        public void SetProgress(int stage, int wave, bool healParty)
        {
            director?.SetProgress(stage, wave, healParty);
        }

        /// <summary>Points the run at a stage and optionally heals the party.</summary>
        public void SetStage(int stage, bool healParty)
        {
            director?.SetStage(stage, healParty);
        }

        public void StartRun()
        {
            if (simulator == null || director == null)
            {
                Debug.LogError("[CombatManager] StartRun called before Initialize.");
                return;
            }

            director.Start();
        }

        public void StopRun()
        {
            director?.Stop();
        }

        /// <summary>Driven by <see cref="RunController"/> - the only place combat advances.</summary>
        public void Tick(double deltaTime)
        {
            director?.Tick(deltaTime);
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

        // ------------------------------------------------------------------
        // Simulation event mirrors
        // ------------------------------------------------------------------
        private void OnDirectorWaveStarted(int stage, int wave, bool isBoss)
        {
            WaveStarted?.Invoke(stage, wave, isBoss);

            if (simulator == null || simulator.Enemies == null || simulator.Enemies.Length == 0)
            {
                return;
            }

            // One event per enemy so each of the 1-3 slots on the battle page can bind itself.
            for (int i = 0; i < simulator.Enemies.Length; i++)
            {
                EnemyCombatant enemy = simulator.Enemies[i];

                if (enemy != null)
                {
                    GameEvents.RaiseEnemySpawned(enemy.DisplayName, enemy.MaxHealth, enemy.IsBoss, i);
                }
            }

            LogCombat($"Stage {stage} | Wave {wave}/{WavesPerStage}{(isBoss ? " (BOSS)" : string.Empty)} -> " +
                      $"{EncounterFactory.Describe(simulator.Enemies)}");
        }

        private void OnSimEnemyDamaged(EnemyDamagedInfo info)
        {
            GameEvents.RaiseEnemyDamaged(info);

            if (logCombatEvents)
            {
                Debug.Log($"[Combat] {EnemyLabel(info.EnemyIndex)} took {info.Damage:0.#}" +
                          $"{(info.IsCritical ? " CRIT" : string.Empty)} -> {info.CurrentHealth:0}/{info.MaxHealth:0}");
            }
        }

        private void OnSimEnemyKilled(int enemyIndex, double goldReward)
        {
            string enemyName = EnemyLabel(enemyIndex);

            GameEvents.RaiseEnemyKilled(enemyName, goldReward, enemyIndex);
            director?.NotifyEnemyKilled(enemyIndex, goldReward);

            int left = simulator != null ? simulator.AliveEnemyCount : 0;
            LogCombat($"{enemyName} killed -> +{goldReward:0} gold ({left} left in wave)");
        }

        /// <summary>Name of one enemy in the wave, for logs and events.</summary>
        private string EnemyLabel(int enemyIndex)
        {
            if (simulator == null || simulator.Enemies == null || enemyIndex < 0 || enemyIndex >= simulator.Enemies.Length)
            {
                return "Enemy";
            }

            EnemyCombatant enemy = simulator.Enemies[enemyIndex];
            return enemy == null ? "Enemy" : enemy.DisplayName;
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
            GameEvents.RaisePartyWiped();
            director?.NotifyPartyWiped();

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
