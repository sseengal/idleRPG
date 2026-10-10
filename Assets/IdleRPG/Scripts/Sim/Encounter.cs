using System;
using IdleRPG.Progression;

namespace IdleRPG.Sim
{
    /// <summary>How a side picks its target (data-driven per archetype/ability from Step 11 on).</summary>
    public enum TargetRule
    {
        /// <summary>Closest to the enemy: lowest row first (front row tanks), then left to right.</summary>
        FrontMost = 0,

        LowestHealthPercent = 1,

        Random = 2,

        /// <summary>Reaches over the front row and hits the back row first - the ranged counter (Step 10).</summary>
        BacklineFirst = 3
    }

    /// <summary>One hit, with explicit indices so views and the log can attribute it.</summary>
    public struct DamageEvent
    {
        public int AttackerIndex;
        public CombatantSide AttackerSide;
        public int TargetIndex;
        public CombatantSide TargetSide;
        public double Damage;
        public double TargetHealth;
        public double TargetMaxHealth;
        public bool IsCritical;
    }

    /// <summary>
    /// An attack was announced: the attacker commits, is arming its next swing, and the blow will land after
    /// <see cref="SimRules.SwingDurationSec"/> sim-seconds. Damage is NOT applied here - the UI uses this to run
    /// the unit in visually; the actual hit (and its <see cref="DamageEvent"/>) arrives at impact.
    /// </summary>
    public struct SwingStartedEvent
    {
        public CombatantSide AttackerSide;
        public int AttackerIndex;
        public CombatantSide TargetSide;
        public int TargetIndex;
    }

    /// <summary>A blow travelling from announcement to impact.</summary>
    internal struct PendingSwing
    {
        public Combatant Attacker;
        public Combatant Target;
        public CombatantSide AttackerSide;
        public double Damage;
        public bool IsCritical;
        public double Remaining;
    }

    /// <summary>A combatant died.</summary>
    public struct DeathEvent
    {
        public int Index;
        public CombatantSide Side;
        public string DisplayName;
        public double GoldReward;
    }

    /// <summary>
    /// One fight: the party versus 1..N enemies (the cap comes from <see cref="SimCaps"/>).
    ///
    /// ELI5: this is the referee. It holds both teams, decides who swings at whom, and shouts the results
    /// ("party:1 hit enemy:2 for 12 damage"). It knows nothing about Unity, and it behaves identically
    /// whether it is stepped 20 times a second on a phone or 12,000 times in a row for an offline estimate.
    /// </summary>
    public sealed partial class Encounter
    {
        private readonly SimContext context;

        private Combatant[] party = Array.Empty<Combatant>();
        private Combatant[] enemies = Array.Empty<Combatant>();
        private bool partyWipeRaised;

        // Single-action flow (ATB-style): exactly one swing may be in flight. `activeSwing` is that blow while
        // it is travelling, then `recoveryRemaining` is the blink-home window before the next attacker may start.
        private bool hasActiveSwing;
        private PendingSwing activeSwing;
        private double recoveryRemaining;

        /// <summary>Which side committed last - the soft alternation preference ("other side goes next").</summary>
        private CombatantSide? lastActorSide;

        public Encounter(SimContext context)
        {
            this.context = context ?? SimContext.CreateDefault();
            PartyTargetRule = TargetRule.FrontMost;
            EnemyTargetRule = TargetRule.FrontMost;
        }

        // --- Events (indices included; the runtime maps them onto GameEvents) ---
        public event Action<DamageEvent> Damaged;
        public event Action<SwingStartedEvent> SwingStarted;
        public event Action<DeathEvent> Died;
        public event Action<int, double> EnemyKilled;
        public event Action PartyWiped;

        // --- Read-only state ---
        public SimContext Context => context;

        public Combatant[] Party => party;

        public Combatant[] Enemies => enemies;

        public int EnemyCount => enemies.Length;

        public int AlivePartyCount => CountAlive(party);

        public int AliveEnemyCount => CountAlive(enemies);

        public bool IsActive => AliveEnemyCount > 0 && AlivePartyCount > 0;

        /// <summary>How the enemies choose a hero to hit.</summary>
        public TargetRule EnemyTargetRule { get; set; }

        /// <summary>How the party chooses an enemy to hit (per-hero overrides arrive in Step 11).</summary>
        public TargetRule PartyTargetRule { get; set; }

        /// <summary>
        /// Row targeting weight, mirrored from <see cref="SimRules.BackRowTargetWeight"/> so a fight reads it the
        /// same way it reads every other tuning value. Position changes *who gets picked*, never how hard they
        /// are hit.
        /// </summary>
        public double BackRowTargetWeight => context.Rules.BackRowTargetWeight;

        public Combatant FirstAliveEnemy => SelectTarget(enemies, PartyTargetRule);

        /// <summary>Combined enemy health as 0..1 - what the single MVP health bar shows today.</summary>
        public double TotalEnemyHealthPercent
        {
            get
            {
                double current = 0d;
                double max = 0d;

                for (int i = 0; i < enemies.Length; i++)
                {
                    Combatant enemy = enemies[i];
                    if (enemy == null)
                    {
                        continue;
                    }

                    current += enemy.CurrentHealth;
                    max += enemy.MaxHealth;
                }

                return max <= 0d ? 0d : current / max;
            }
        }

        /// <summary>Exposed for tools: the snapshot the sim is running under.</summary>
        public override string ToString()
        {
            return $"Encounter(party {AlivePartyCount}/{party.Length}, enemies {AliveEnemyCount}/{enemies.Length})";
        }

        // --- Lifecycle ---
        /// <summary>Installs the party and re-indexes its slots so events always match array positions.</summary>
        public void SetParty(Combatant[] partyMembers)
        {
            party = partyMembers ?? Array.Empty<Combatant>();

            for (int i = 0; i < party.Length; i++)
            {
                if (party[i] != null)
                {
                    party[i].SlotIndex = i;
                }
            }

            partyWipeRaised = false;
        }

        /// <summary>Spawns the enemy team for a new wave (replaces the previous one).</summary>
        public void SpawnEnemies(Combatant[] enemyMembers)
        {
            enemies = enemyMembers ?? Array.Empty<Combatant>();
            partyWipeRaised = false;
            ResetCombatFlow();

            int cap = context.Caps.MaxEnemiesPerEncounter;

            for (int i = 0; i < enemies.Length; i++)
            {
                Combatant enemy = enemies[i];
                if (enemy == null)
                {
                    continue;
                }

                enemy.SlotIndex = i;

                if (i >= cap)
                {
                    // Beyond the cap: park them dead so they cannot act, and say so once.
                    enemy.TakeDamage(enemy.CurrentHealth);
                    SimLog.LogWarning($"[Encounter] Enemy {i} exceeds MaxEnemiesPerEncounter ({cap}); parked.");
                }
            }
        }

        /// <summary>Ends the fight without raising a kill event.</summary>
        public void Clear()
        {
            enemies = Array.Empty<Combatant>();
            ResetCombatFlow();
        }

        /// <summary>Full heal for the whole party (stage advance, retry, new run).</summary>
        public void HealParty()
        {
            for (int i = 0; i < party.Length; i++)
            {
                party[i]?.RestoreFullHealth();
            }

            partyWipeRaised = false;
            ResetCombatFlow();
        }

        /// <summary>Aborts the in-flight action: no swing flying, no recovery, no alternation memory.</summary>
        private void ResetCombatFlow()
        {
            hasActiveSwing = false;
            activeSwing = default;
            recoveryRemaining = 0d;
            lastActorSide = null;
        }

        /// <summary>
        /// Advances the fight one beat - an ATB-style single-action fight: exactly ONE unit is ever running or
        /// hitting at a time. What Step does per beat, in priority order:
        /// <list type="number">
        /// <item>every living combatant's cooldown ticks;</item>
        /// <item>a swing already in flight moves one beat (and its damage lands at impact);</item>
        /// <item>the recovery window after a hit counts down;</item>
        /// <item>only when the arena is otherwise empty does the next ready attacker start its swing.</item>
        /// </list>
        /// Who acts next is whoever has been ready longest, with a soft preference for the side that did NOT act
        /// last - so the fight reads as hero/enemy exchanges instead of a simultaneous mosh pit.
        /// </summary>
        public void Step(double deltaTime)
        {
            if (deltaTime <= 0d || !IsActive)
            {
                return;
            }

            TickAllTimers(deltaTime);

            if (!IsActive)
            {
                return;
            }

            if (hasActiveSwing)
            {
                AdvanceActiveSwing(deltaTime);
                return;
            }

            if (recoveryRemaining > 0d)
            {
                recoveryRemaining -= deltaTime;
                if (recoveryRemaining < 0d)
                {
                    recoveryRemaining = 0d;
                }

                return;
            }

            AnnounceNext();
        }

        private void TickAllTimers(double deltaTime)
        {
            for (int i = 0; i < party.Length; i++)
            {
                party[i]?.Tick(deltaTime);
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i]?.Tick(deltaTime);
            }
        }

        /// <summary>Advances the single in-flight swing; the one that finishes applies its damage now (impact).</summary>
        private void AdvanceActiveSwing(double deltaTime)
        {
            activeSwing.Remaining -= deltaTime;
            if (activeSwing.Remaining > 0d)
            {
                return;
            }

            hasActiveSwing = false;
            PendingSwing landed = activeSwing;
            activeSwing = default;

            ApplySwing(landed);

            if (!IsActive)
            {
                return;
            }

            // The hit landed; the attacker blinks home for a beat before the next unit may start.
            double recoverySec = context.Rules.SwingRecoverySec;
            recoveryRemaining = recoverySec > 0d ? recoverySec : 0d;
        }

        /// <summary>Applies one landed swing: damage, threat, the impact event, and any death that follows.</summary>
        private void ApplySwing(PendingSwing swing)
        {
            Combatant attacker = swing.Attacker;
            Combatant target = swing.Target;

            // No free hits from the grave or on corpses: a flight that ended after either side vanished does nothing
            // (a surviving attacker whose blow was wasted still re-arms, so it is not stuck "ready" forever).
            if (attacker == null || target == null || !attacker.IsAlive || !target.IsAlive)
            {
                if (attacker != null && attacker.IsAlive)
                {
                    attacker.EndSwing();
                }

                return;
            }

            double applied = target.TakeDamage(swing.Damage);
            attacker.Threat += applied;

            Damaged?.Invoke(new DamageEvent
            {
                AttackerIndex = attacker.SlotIndex,
                AttackerSide = swing.AttackerSide,
                TargetIndex = target.SlotIndex,
                TargetSide = target.Side,
                Damage = applied,
                TargetHealth = target.CurrentHealth,
                TargetMaxHealth = target.MaxHealth,
                IsCritical = swing.IsCritical
            });

            // The swing is over: the attacker's cooldown starts counting a full interval from this impact.
            attacker.EndSwing();

            if (target.IsAlive)
            {
                return;
            }

            Died?.Invoke(new DeathEvent
            {
                Index = target.SlotIndex,
                Side = target.Side,
                DisplayName = target.DisplayName,
                GoldReward = target.GoldReward
            });

            if (target.Side == CombatantSide.Enemy)
            {
                EnemyKilled?.Invoke(target.SlotIndex, target.GoldReward);
            }

            if (AlivePartyCount == 0 && !partyWipeRaised)
            {
                partyWipeRaised = true;
                PartyWiped?.Invoke();
            }
        }

        // --- Internals ---
        /// <summary>Starts the next ready attacker's swing: picks who, rolls the hit (crit + damage) and announces it.</summary>
        private void AnnounceNext()
        {
            int maxAttempts = party.Length + enemies.Length + 1;
            int attempts = 0;

            while (attempts++ < maxAttempts)
            {
                Combatant attacker = PickNextReadyAttacker(out CombatantSide side);
                if (attacker == null)
                {
                    return;
                }

                // An archetype with its own preference uses it; everybody else uses the side rule (Step 11a).
                Combatant[] defenders = side == CombatantSide.Party ? enemies : party;
                TargetRule rule = side == CombatantSide.Party ? PartyTargetRule : EnemyTargetRule;
                Combatant target = SelectTarget(defenders, attacker.TargetRule ?? rule);
                if (target == null)
                {
                    attacker.EndSwing();
                    continue;
                }

                // Crit comes from the ATTACKER when it has its own values (heroes); enemies fall back to the
                // rules' global crit, so no enemy asset needs touching.
                double critChance = attacker.CritChance > 0d ? attacker.CritChance : context.Rules.CriticalChance;
                double critDamage = attacker.CritDamageMultiplier > 1d
                    ? attacker.CritDamageMultiplier
                    : context.Rules.CriticalDamageMultiplier;

                bool isCritical = critChance > 0d && context.Rng.NextDouble() < critChance;

                double multiplier = isCritical ? critDamage : 1d;
                double damage = FormulaUtility.Damage(attacker.Attack, target.Defense, context.Rules.MinDamageRatio, multiplier);

                // MVP parity: a turn that would deal no damage is a wasted turn (no swing, no stuck "ready").
                if (damage <= 0d)
                {
                    attacker.EndSwing();
                    continue;
                }

                activeSwing = new PendingSwing
                {
                    Attacker = attacker,
                    Target = target,
                    AttackerSide = side,
                    Damage = damage,
                    IsCritical = isCritical,
                    Remaining = context.Rules.SwingDurationSec
                };
                hasActiveSwing = true;
                lastActorSide = side;

                SwingStarted?.Invoke(new SwingStartedEvent
                {
                    AttackerSide = side,
                    AttackerIndex = attacker.SlotIndex,
                    TargetSide = target.Side,
                    TargetIndex = target.SlotIndex
                });
                return;
            }
        }

        /// <summary>
        /// Who acts next: the ready attacker with the longest-standing timer. Soft alternation - if a unit of the
        /// side that did NOT act last is ready, it is picked first, so the fight reads as hero/enemy exchanges.
        /// The first pick of a wave goes to a hero, matching the MVP "heroes go first" convention.
        /// </summary>
        private Combatant PickNextReadyAttacker(out CombatantSide side)
        {
            CombatantSide preferred = lastActorSide.HasValue
                ? (lastActorSide.Value == CombatantSide.Party ? CombatantSide.Enemy : CombatantSide.Party)
                : CombatantSide.Party;
            CombatantSide fallback = preferred == CombatantSide.Party ? CombatantSide.Enemy : CombatantSide.Party;

            Combatant pick = FindReadyAttacker(preferred);
            if (pick == null)
            {
                pick = FindReadyAttacker(fallback);
            }

            if (pick != null)
            {
                side = pick.Side;
                return pick;
            }

            side = default;
            return null;
        }

        private Combatant FindReadyAttacker(CombatantSide side)
        {
            Combatant[] row = side == CombatantSide.Party ? party : enemies;
            Combatant best = null;

            for (int i = 0; i < row.Length; i++)
            {
                Combatant candidate = row[i];
                if (candidate == null || !candidate.IsAlive || !candidate.IsSwingReady)
                {
                    continue;
                }

                // Most-negative timer = has been ready (waiting) the longest.
                if (best == null || candidate.AttackTimer < best.AttackTimer)
                {
                    best = candidate;
                }
            }

            return best;
        }

        private static int CountAlive(Combatant[] combatants)
        {
            int alive = 0;

            for (int i = 0; i < combatants.Length; i++)
            {
                if (combatants[i] != null && combatants[i].IsAlive)
                {
                    alive++;
                }
            }

            return alive;
        }
    }
}
