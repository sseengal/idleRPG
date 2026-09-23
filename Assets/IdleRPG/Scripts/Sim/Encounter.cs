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

        public Encounter(SimContext context)
        {
            this.context = context ?? SimContext.CreateDefault();
            PartyTargetRule = TargetRule.FrontMost;
            EnemyTargetRule = TargetRule.FrontMost;
        }

        // --- Events (indices included; the runtime maps them onto GameEvents) ---
        public event Action<DamageEvent> Damaged;
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
        }

        /// <summary>Full heal for the whole party (stage advance, retry, new run).</summary>
        public void HealParty()
        {
            for (int i = 0; i < party.Length; i++)
            {
                party[i]?.RestoreFullHealth();
            }

            partyWipeRaised = false;
        }

        /// <summary>
        /// Advances the fight. Order preserved from the MVP: every living hero swings first, then the enemies.
        /// If the last enemy dies during the hero phase, the enemy phase is skipped entirely.
        /// </summary>
        public void Step(double deltaTime)
        {
            if (deltaTime <= 0d || !IsActive)
            {
                return;
            }

            ResolvePhase(party, enemies, CombatantSide.Party, PartyTargetRule, deltaTime);

            if (AliveEnemyCount == 0 || AlivePartyCount == 0)
            {
                return;
            }

            ResolvePhase(enemies, party, CombatantSide.Enemy, EnemyTargetRule, deltaTime);

            if (AlivePartyCount == 0 && !partyWipeRaised)
            {
                partyWipeRaised = true;
                PartyWiped?.Invoke();
            }
        }

        // --- Internals ---
        private void ResolvePhase(Combatant[] attackers, Combatant[] defenders, CombatantSide attackerSide, TargetRule rule, double deltaTime)
        {
            for (int i = 0; i < attackers.Length; i++)
            {
                Combatant attacker = attackers[i];
                if (attacker == null || !attacker.IsAlive || !attacker.Tick(deltaTime))
                {
                    continue;
                }

                // An archetype with its own preference uses it; everybody else uses the side rule (Step 11a).
                Combatant target = SelectTarget(defenders, attacker.TargetRule ?? rule);
                if (target == null)
                {
                    return;
                }

                bool isCritical = context.Rules.CriticalChance > 0d
                                  && context.Rng.NextDouble() < context.Rules.CriticalChance;

                double multiplier = isCritical ? context.Rules.CriticalDamageMultiplier : 1d;
                double damage = FormulaUtility.Damage(attacker.Attack, target.Defense, context.Rules.MinDamageRatio, multiplier);

                if (damage <= 0d)
                {
                    continue;
                }

                double applied = target.TakeDamage(damage);
                attacker.Threat += applied;

                Damaged?.Invoke(new DamageEvent
                {
                    AttackerIndex = attacker.SlotIndex,
                    AttackerSide = attackerSide,
                    TargetIndex = target.SlotIndex,
                    TargetSide = target.Side,
                    Damage = applied,
                    TargetHealth = target.CurrentHealth,
                    TargetMaxHealth = target.MaxHealth,
                    IsCritical = isCritical
                });

                if (target.IsAlive)
                {
                    continue;
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

                    // MVP parity: the target died inside this phase, so if nothing is left alive the rest of
                    // the phase (and the enemy phase) never happens.
                    if (AliveEnemyCount == 0)
                    {
                        return;
                    }
                }
            }
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
