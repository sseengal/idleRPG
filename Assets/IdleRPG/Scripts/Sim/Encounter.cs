using System;
using IdleRPG.Progression;

namespace IdleRPG.Sim
{
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

        // Enemy cast intent (Ability-Plan). An enemy's clock is its will, but a clock that has run out must not mean
        // "cast on the next beat" - that is what made every fight sound like a metronome. While ready, the unit rolls
        // once every 1-3 s and only a pass becomes a cast, so a move lands from time to time and can catch a player out.
        private const double EnemyCastChance = 0.35d;
        private const double EnemyCastRetryMinSec = 1d;
        private const double EnemyCastRetryMaxSec = 3d;

        public Encounter(SimContext context)
        {
            this.context = context ?? SimContext.CreateDefault();
            PartyTargetRule = TargetRule.FrontMost;
            EnemyTargetRule = TargetRule.FrontMost;
        }

        // --- Events (indices included; the runtime maps them onto GameEvents) ---
        public event Action<DamageEvent> Damaged;
        public event Action<SwingStartedEvent> SwingStarted;

        /// <summary>A unit cast its ability (Ability-Plan). Fired on the fight's own tick, never mid-frame.</summary>
        public event Action<AbilityCastEvent> AbilityCast;

        /// <summary>An ability healed a unit (impact). One call per unit healed.</summary>
        public event Action<HealEvent> Healed;

        /// <summary>An ability raised a guard on a unit (impact). One call per unit guarded.</summary>
        public event Action<ShieldEvent> ShieldApplied;

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

            // A queued press belongs to the fight that was running: a wave change or a reset must not fire it later.
            for (int i = 0; i < party.Length; i++)
            {
                party[i]?.CancelAbilityRequest();
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i]?.CancelAbilityRequest();
            }
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
                Combatant enemy = enemies[i];
                if (enemy == null)
                {
                    continue;
                }

                enemy.Tick(deltaTime);

                // Enemies have no buttons: their clock IS their intent. When it runs out they get a chance to ask for a
                // cast, and the lane announces it exactly like a hero's press (one action at a time, same events, same
                // pictures). A miss is not a wasted turn - the enemy keeps swinging and rolls again shortly.
                if (enemy.IsAbilityReady &&
                    enemy.TickAbilityIntent(deltaTime, context.Rng, EnemyCastChance, EnemyCastRetryMinSec, EnemyCastRetryMaxSec))
                {
                    enemy.RequestAbility();
                }
            }
        }

        /// <summary>
        /// Asks one of the party to cast its ability. Returns false when that seat is empty, the unit is down, its clock
        /// has not run out, or it is already queued - the same answer the button needs to decide whether the press
        /// could do anything. The request is a flag; <see cref="AnnounceNext"/> turns it into an action when the lane
        /// is free, so the cast can never start while somebody else is mid-swing.
        /// </summary>
        public bool RequestAbility(int partyIndex)
        {
            if (party == null || partyIndex < 0 || partyIndex >= party.Length)
            {
                return false;
            }

            return party[partyIndex] != null && party[partyIndex].RequestAbility();
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

        /// <summary>
        /// Applies one landed action: a swing's damage, or an ability's effect on everyone it was aimed at. Deaths (and the
        /// party wipe that follows) resolve in one place, so an ability kill is announced exactly like a swing kill.
        /// </summary>
        private void ApplySwing(PendingSwing swing)
        {
            Combatant attacker = swing.Attacker;

            // No free hits from the grave: a flight that ended after the attacker fell does nothing.
            if (attacker == null || !attacker.IsAlive)
            {
                return;
            }

            if (swing.IsAbility)
            {
                ApplyAbilityImpact(swing);
                attacker.EndSwing();
                return;
            }

            Combatant target = swing.Target;

            // No free hits on corpses: a flight that ended after the victim fell does nothing (the attacker still
            // re-arms, so it is not stuck "ready" forever).
            if (target == null || !target.IsAlive)
            {
                attacker.EndSwing();
                return;
            }

            double applied = target.TakeDamage(swing.Damage);
            attacker.Threat += applied;

            RaiseDamage(attacker, target, swing.AttackerSide, applied, swing.IsCritical);

            // The swing is over: the attacker's cooldown starts counting a full interval from this impact.
            attacker.EndSwing();

            ResolveDeath(target);
        }

        /// <summary>One ability impact. Damage goes through the swing's own path, so numbers / HP / kills are free.</summary>
        private void ApplyAbilityImpact(PendingSwing swing)
        {
            Combatant caster = swing.Attacker;
            SimAbility ability = swing.Ability;
            Combatant[] targets = swing.Targets;

            if (targets == null || targets.Length == 0)
            {
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                Combatant target = targets[i];
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                if (ability.Kind == SimAbilityKind.Damage)
                {
                    double applied = target.TakeDamage(swing.Damage);
                    caster.Threat += applied;
                    RaiseDamage(caster, target, swing.AttackerSide, applied, swing.IsCritical, ability.VfxId);
                    ResolveDeath(target);
                    continue;
                }

                if (ability.Kind == SimAbilityKind.Heal)
                {
                    double healed = target.Heal(target.MaxHealth * ability.HealFraction);
                    if (healed <= 0d)
                    {
                        continue;
                    }

                    Healed?.Invoke(new HealEvent
                    {
                        Side = target.Side,
                        Index = target.SlotIndex,
                        SourceSide = swing.AttackerSide,
                        SourceIndex = caster.SlotIndex,
                        AbilityName = ability.DisplayName,
                        Amount = healed,
                        TargetHealth = target.CurrentHealth,
                        TargetMaxHealth = target.MaxHealth
                    });

                    continue;
                }

                // Shield: a real damage-taken modifier for a few seconds, not a cosmetic glow.
                double factor = ability.ShieldFactor;
                double seconds = ability.ShieldSeconds;

                if (seconds <= 0d)
                {
                    continue;
                }

                target.ApplyGuard(factor, seconds);

                ShieldApplied?.Invoke(new ShieldEvent
                {
                    Side = target.Side,
                    Index = target.SlotIndex,
                    SourceSide = swing.AttackerSide,
                    SourceIndex = caster.SlotIndex,
                    AbilityName = ability.DisplayName,
                    Factor = target.DamageTakenMultiplier,
                    Seconds = seconds
                });
            }
        }

        /// <summary>Raises the impact event for a hit that really removed something (a 0-damage hit shows no number).</summary>
        private void RaiseDamage(Combatant attacker, Combatant target, CombatantSide attackerSide, double applied, bool isCritical, string vfxId = "")
        {
            if (applied <= 0d)
            {
                return;
            }

            Damaged?.Invoke(new DamageEvent
            {
                AttackerIndex = attacker.SlotIndex,
                AttackerSide = attackerSide,
                TargetIndex = target.SlotIndex,
                TargetSide = target.Side,
                Damage = applied,
                TargetHealth = target.CurrentHealth,
                TargetMaxHealth = target.MaxHealth,
                IsCritical = isCritical,
                VfxId = vfxId
            });
        }

        /// <summary>Every consequence of a unit reaching 0 health: its death, the kill reward, and a party wipe.</summary>
        private void ResolveDeath(Combatant target)
        {
            if (target == null || target.IsAlive)
            {
                return;
            }

            target.CancelAbilityRequest();

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

        /// <summary>
        /// How long this attacker's swing travels before its damage lands.
        ///
        /// The attacker's own art decides it: <see cref="Combatant.SwingSeconds"/> covers that unit's attack strip up
        /// to and including its strike frame (measured from the pixels - the sword at full reach, the bow released),
        /// so the damage lands exactly when the swing connects. A unit with no art (a placeholder) has 0 there and
        /// uses the rules' window instead. Clamped to the same 0.1-2 s range the rules use, so a broken clip can
        /// never stall the fight.
        /// </summary>
        private double SwingWindow(Combatant attacker)
        {
            double window = attacker != null && attacker.SwingSeconds > 0d
                ? attacker.SwingSeconds
                : context.Rules.SwingDurationSec;

            if (window < 0.1d)
            {
                return 0.1d;
            }

            return window > 2d ? 2d : window;
        }

        // --- Internals ---
        /// <summary>Starts the next ready attacker's swing: picks who, rolls the hit (crit + damage) and announces it.</summary>
        private void AnnounceNext()
        {
            // A queued press has priority: the player asked for it, so it is announced like any other action - one
            // actor, one target, one travel window - before the ordinary attack pick runs.
            if (AnnounceAbility())
            {
                return;
            }

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
                    Remaining = SwingWindow(attacker)
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
        /// Turns a queued press into the next action, or returns false when nobody asked to cast.
        ///
        /// A cast is announced exactly like a swing: the ability's own target rule picks who it lands on, the travel
        /// window is measured from that ability's art strip, and the effect resolves at impact. The button's press is
        /// therefore visible on the same beat a basic attack would be - the only difference is which clip plays and
        /// what happens when it connects.
        /// </summary>
        private bool AnnounceAbility()
        {
            Combatant caster = PickPendingCaster(out CombatantSide side);
            if (caster == null)
            {
                return false;
            }

            SimAbility ability = caster.Ability;
            Combatant[] defenders = side == CombatantSide.Party ? enemies : party;
            Combatant[] friends = side == CombatantSide.Party ? party : enemies;

            Combatant[] targets = ResolveAbilityTargets(ability.Target, caster, defenders, friends);
            if (targets.Length == 0)
            {
                // Nothing to aim at: the press is dropped instead of sticking until a target appears.
                caster.EndSwing();
                return false;
            }

            Combatant anchor = targets[0];
            double damage = 0d;
            bool isCritical = false;

            if (ability.Kind == SimAbilityKind.Damage)
            {
                // Same crit and defence path as a swing, so an ability crit reads and resolves exactly like a swing crit.
                double critChance = caster.CritChance > 0d ? caster.CritChance : context.Rules.CriticalChance;
                double critDamage = caster.CritDamageMultiplier > 1d
                    ? caster.CritDamageMultiplier
                    : context.Rules.CriticalDamageMultiplier;

                isCritical = critChance > 0d && context.Rng.NextDouble() < critChance;

                double multiplier = ability.DamageMultiplier * (isCritical ? critDamage : 1d);
                damage = FormulaUtility.Damage(caster.Attack, anchor.Defense, context.Rules.MinDamageRatio, multiplier);
            }

            activeSwing = new PendingSwing
            {
                Attacker = caster,
                Target = anchor,
                AttackerSide = side,
                Damage = damage,
                IsCritical = isCritical,
                Remaining = AbilityWindow(caster),
                IsAbility = true,
                Ability = ability,
                Targets = targets
            };

            hasActiveSwing = true;
            lastActorSide = side;

            // The clock starts at the ANNOUNCE: the time spent queued for the lane is not charged to the player.
            caster.StartAbilityCooldown();

            AbilityCast?.Invoke(new AbilityCastEvent
            {
                Side = side,
                Index = caster.SlotIndex,
                DisplayName = caster.DisplayName,
                AbilityId = ability.Id,
                AbilityName = ability.DisplayName,
                ClipSlot = ability.ClipSlot,
                CooldownSeconds = ability.CooldownSec,
                Kind = ability.Kind,
                Target = ability.Target,
                TargetSide = anchor.Side,
                TargetIndex = anchor.SlotIndex
            });

            // The view entry point is the SAME event a basic attack uses. An explicit clip slot is the only extra:
            // nothing in the UI has to know that this is an ability.
            SwingStarted?.Invoke(new SwingStartedEvent
            {
                AttackerSide = side,
                AttackerIndex = caster.SlotIndex,
                TargetSide = anchor.Side,
                TargetIndex = anchor.SlotIndex,
                ClipSlot = ability.ClipSlot,
                IsAbility = true
            });

            return true;
        }

        /// <summary>The first unit (party first, then enemies) with a valid queued cast. Taking it clears the request.</summary>
        private Combatant PickPendingCaster(out CombatantSide side)
        {
            Combatant pick = FindPendingCaster(party);
            if (pick != null)
            {
                side = CombatantSide.Party;
                return pick;
            }

            pick = FindPendingCaster(enemies);
            side = CombatantSide.Enemy;
            return pick;
        }

        private static Combatant FindPendingCaster(Combatant[] row)
        {
            if (row == null)
            {
                return null;
            }

            for (int i = 0; i < row.Length; i++)
            {
                Combatant candidate = row[i];
                if (candidate == null || !candidate.IsAlive || !candidate.HasCastRequest)
                {
                    continue;
                }

                // A request that can no longer start is thrown away, not left to fire later.
                if (candidate.TryTakeAbilityRequest())
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// Who the ability lands on. The caster's own side is never guessed: "FrontEnemy"/"FrontHero" always mean the
        /// side opposite the caster, so a hero ability can never heal an enemy by mistake.
        /// </summary>
        private Combatant[] ResolveAbilityTargets(SimAbilityTarget target, Combatant caster, Combatant[] defenders, Combatant[] friends)
        {
            switch (target)
            {
                case SimAbilityTarget.Self:
                    return new[] { caster };

                case SimAbilityTarget.Party:
                    return LivingMembers(friends);

                case SimAbilityTarget.LowestAlly:
                    return SingleTarget(LowestHealth(friends));

                case SimAbilityTarget.FrontEnemy:
                case SimAbilityTarget.FrontHero:
                    return SingleTarget(SelectTarget(defenders, caster.Side == CombatantSide.Party ? PartyTargetRule : EnemyTargetRule));

                default:
                    return SingleTarget(SelectTarget(defenders, caster.Side == CombatantSide.Party ? PartyTargetRule : EnemyTargetRule));
            }
        }

        private static Combatant[] SingleTarget(Combatant target)
        {
            return target == null ? Array.Empty<Combatant>() : new[] { target };
        }

        private static Combatant LowestHealth(Combatant[] row)
        {
            if (row == null)
            {
                return null;
            }

            Combatant best = null;

            for (int i = 0; i < row.Length; i++)
            {
                Combatant candidate = row[i];
                if (candidate == null || !candidate.IsAlive)
                {
                    continue;
                }

                if (best == null || candidate.HealthPercent < best.HealthPercent)
                {
                    best = candidate;
                }
            }

            return best;
        }

        private static Combatant[] LivingMembers(Combatant[] row)
        {
            if (row == null)
            {
                return Array.Empty<Combatant>();
            }

            int alive = 0;
            for (int i = 0; i < row.Length; i++)
            {
                if (row[i] != null && row[i].IsAlive)
                {
                    alive++;
                }
            }

            if (alive == 0)
            {
                return Array.Empty<Combatant>();
            }

            Combatant[] members = new Combatant[alive];
            int index = 0;

            for (int i = 0; i < row.Length; i++)
            {
                if (row[i] != null && row[i].IsAlive)
                {
                    members[index++] = row[i];
                }
            }

            return members;
        }

        /// <summary>
        /// How long a cast travels before its effect lands: measured from the ability's own art strip up to its strike
        /// frame, clamped to the same 0.1-2 s range as a swing so one broken clip can never stall the fight.
        /// No measured strip = the caster's ordinary swing window.
        /// </summary>
        private double AbilityWindow(Combatant caster)
        {
            double window = caster != null && caster.Ability.WindowSeconds > 0d
                ? caster.Ability.WindowSeconds
                : SwingWindow(caster);

            if (window < 0.1d)
            {
                return 0.1d;
            }

            return window > 2d ? 2d : window;
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