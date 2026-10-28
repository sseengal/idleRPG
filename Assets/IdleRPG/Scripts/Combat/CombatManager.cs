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

        private Func<int, AbilityDef> abilityProvider;

        /// <summary>
        /// Who supplies each hero's equipped ability, by party index. Set once by the game wiring. Writing it also
        /// pushes it to the simulator, which may already exist - and the sim asks it every time it builds the party,
        /// so an ability swapped in the Party tab is live from the next wave without a restart.
        /// </summary>
        public Func<int, AbilityDef> AbilityProvider
        {
            get => abilityProvider;
            set
            {
                abilityProvider = value;

                if (simulator != null)
                {
                    simulator.AbilityFor = value;
                }
            }
        }

        /// <summary>
        /// Asks a hero to cast its equipped ability. False when it owns nothing, is down, or is still cooling, so the
        /// button can tell "not yet" apart from "nothing happened". The cast is announced as soon as the action lane is
        /// free (one actor, one target, one travel window), and its cooldown starts at that announce.
        /// </summary>
        public bool RequestAbility(int heroIndex)
        {
            return simulator != null &&
                   simulator.CurrentEncounter != null &&
                   simulator.CurrentEncounter.RequestAbility(heroIndex);
        }

        public bool IsRunning => director != null && director.IsRunning;

        /// <summary>Wave label for the HUD: one name, or "Werewolf +2" when the wave holds several enemies.</summary>
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
            simulator.SwingStarted += OnSimSwingStarted;
            simulator.EnemyKilled += OnSimEnemyKilled;
            simulator.HeroDamaged += OnSimHeroDamaged;
            simulator.AbilityCast += OnSimAbilityCast;
            simulator.ImpactVfx += OnSimImpactVfx;
            simulator.Healed += OnSimHealed;
            simulator.ShieldApplied += OnSimShieldApplied;
            simulator.AbilityFor = abilityProvider;
            simulator.HeroDied += OnSimHeroDied;
            simulator.PartyWiped += OnSimPartyWiped;

            if (!simulator.SetupParty(partyConfig, formation))
            {
                return false;
            }

            director = new CombatDirector();
            director.WaveStarted += OnDirectorWaveStarted;
            director.WaveCleared += (stage, wave) =>
            {
                WaveCleared?.Invoke(stage, wave);
                GameEvents.RaiseWaveCompleted(stage, wave);
                GameEvents.RaiseCombatMessage(new LogMessage(string.Format("Stage {0} wave {1} cleared", stage, wave), LogMessageKind.Event));
            };
            director.StageCleared += stage => StageCleared?.Invoke(stage);
            director.EnemyKilled += gold => EnemyKilled?.Invoke(gold);
            director.PartyWiped += () => PartyWiped?.Invoke();
            director.BossFailed += () =>
            {
                BossFailed?.Invoke();
                GameEvents.RaiseBossFailed();
                GameEvents.RaiseCombatMessage(new LogMessage("Boss encounter failed", LogMessageKind.Defeat));
            };

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

        /// <summary>Camp heal: a fraction of max HP, plus a fraction revive for the fallen.</summary>
        public void HealPartyFraction(double healFraction, double reviveFraction)
        {
            simulator?.HealPartyFraction(healFraction, reviveFraction);
        }

        /// <summary>
        /// The camp asks the wave flow to stop before a spawn. One hook, used by intro/mid/boss checkpoints.
        /// </summary>
        public void SetWaveGate(Func<int, int, bool> gate)
        {
            director?.SetWaveGate(gate);
        }

        /// <summary>Called by the camp after a choice; the held wave spawns now.</summary>
        public void ResumeAfterCamp()
        {
            director?.ResumeAfterGate();
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

        /// <summary>
        /// True while a cutscene owns the screen. Combat does not advance, so no numbers move behind the picture.
        /// Set by <see cref="IdleRPG.UI.CutsceneRunner"/> only; a cutscene can never change anything else.
        /// </summary>
        public bool Paused { get; set; }

        /// <summary>Driven by <see cref="RunController"/> - the only place combat advances.</summary>
        public void Tick(double deltaTime)
        {
            if (Paused)
            {
                return;
            }

            director?.Tick(deltaTime);
        }

        private void OnDestroy()
        {
            if (simulator == null)
            {
                return;
            }

            simulator.EnemyDamaged -= OnSimEnemyDamaged;
            simulator.SwingStarted -= OnSimSwingStarted;
            simulator.EnemyKilled -= OnSimEnemyKilled;
            simulator.HeroDamaged -= OnSimHeroDamaged;
            simulator.AbilityCast -= OnSimAbilityCast;
            simulator.ImpactVfx -= OnSimImpactVfx;
            simulator.Healed -= OnSimHealed;
            simulator.ShieldApplied -= OnSimShieldApplied;
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

            GameEvents.RaiseCombatMessage(new LogMessage(string.Format("-- Wave {0}: {1}{2} --", wave,
                EncounterFactory.Describe(simulator.Enemies), isBoss ? " (BOSS)" : string.Empty), LogMessageKind.Event));

            LogCombat($"Stage {stage} | Wave {wave}/{WavesPerStage}{(isBoss ? " (BOSS)" : string.Empty)} -> " +
                      $"{EncounterFactory.Describe(simulator.Enemies)}");
        }

        /// <summary>
        /// A cast was announced: it has a target and a travel window now. Logged so the player can read what happened,
        /// then raised so the panel restarts the correct clock. The picture and the effect need nothing else from here -
        /// the clip rides on <see cref="OnSimSwingStarted"/> and the damage / heal / guard arrives at impact.
        /// </summary>
        private void OnSimImpactVfx(ImpactVfxEvent vfx)
        {
            GameEvents.RaiseImpactVfx(vfx);
        }

        private void OnSimAbilityCast(AbilityCastEvent cast)
        {
            LogCombat($"{cast.DisplayName} casts {cast.AbilityName} at {TargetLabel(cast.TargetSide, cast.TargetIndex)}");
            GameEvents.RaiseAbilityCast(cast.Side, cast.Index, cast.AbilityId, cast.AbilityName, cast.ClipSlot, cast.CooldownSeconds);
        }

        private void OnSimHealed(HealEvent heal)
        {
            GameEvents.RaiseHealed(heal);
            LogCombat($"{TargetLabel(heal.Side, heal.Index)} healed for {heal.Amount:0.#} by {heal.AbilityName}");
        }

        private void OnSimShieldApplied(ShieldEvent shield)
        {
            GameEvents.RaiseShieldApplied(shield);
            LogCombat($"{TargetLabel(shield.Side, shield.Index)} guarded by {shield.AbilityName} " +
                      $"(x{shield.Factor:0.##} for {shield.Seconds:0.#}s)");
        }

        /// <summary>Name of one unit on either side, for a log line.</summary>
        private string TargetLabel(CombatantSide side, int index)
        {
            if (side == CombatantSide.Enemy)
            {
                return EnemyLabel(index);
            }

            return simulator != null && simulator.Heroes != null && index >= 0 && index < simulator.Heroes.Length &&
                   simulator.Heroes[index] != null
                ? simulator.Heroes[index].DisplayName
                : $"Hero {index}";
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

        private void OnSimSwingStarted(int attackerIndex, CombatantSide attackerSide, int targetIndex, CombatantSide targetSide, string clipSlot, int coveredAllyIndex, bool stayPlanted)
        {
            GameEvents.RaiseSwingStarted(attackerIndex, attackerSide, targetIndex, targetSide, clipSlot, coveredAllyIndex, stayPlanted);
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

        private void OnSimHeroDamaged(int heroIndex, double damage, double currentHealth, double maxHealth, int attackerEnemyIndex, bool isCritical, bool isGuarded)
        {
            GameEvents.RaiseHeroDamaged(heroIndex, damage, currentHealth, maxHealth, attackerEnemyIndex, isCritical, isGuarded);

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
            GameEvents.RaiseCombatMessage(new LogMessage("Party wiped - falling back a stage", LogMessageKind.Defeat));
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
