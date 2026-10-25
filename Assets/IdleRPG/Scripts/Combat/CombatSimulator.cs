using System;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Sim;

namespace IdleRPG.Combat
{
    /// <summary>
    /// The runtime's window onto a fight.
    ///
    /// ELI5: the referee (<see cref="Encounter"/>) does all the fighting; this class is the shop counter
    /// between the referee and the rest of the game. It builds the two teams from your data assets, passes
    /// your upgrades in, and translates the referee's shouts into the game events the UI already listens to.
    /// Nothing else in the game had to change.
    /// </summary>
    public sealed class CombatSimulator
    {
        // A wave's first enemy cast roll waits this long (random inside the range), so a fight does not open with a move.
        private const double EnemyFirstCastMinSec = 2d;
        private const double EnemyFirstCastMaxSec = 6d;

        private ICombatStatProvider statProvider;
        private readonly EnemyTargetingMode targetingMode;
        private Formation formation;

        private SimContext context;
        private Encounter encounter;
        private HeroCombatant[] heroes = Array.Empty<HeroCombatant>();
        private EnemyCombatant[] enemies = Array.Empty<EnemyCombatant>();

        // --- Constructors ---
        /// <summary>Legacy entry point (Step 2-6 callers): converts the scaling snapshot into a context.</summary>
        public CombatSimulator(ICombatStatProvider statProvider, EnemyTargetingMode targetingMode, CombatScaling scaling, int randomSeed)
            : this(statProvider, targetingMode, SimContext.CreateDefault(), randomSeed)
        {
            ApplyScaling(scaling);
        }

        /// <summary>Preferred constructor: the caller owns the frozen rules, the mode and the caps.</summary>
        public CombatSimulator(ICombatStatProvider statProvider, EnemyTargetingMode targetingMode, SimContext context, int randomSeed)
        {
            this.statProvider = statProvider ?? DefaultStatProvider.Instance;
            this.targetingMode = targetingMode;
            this.context = context ?? SimContext.CreateDefault();

            // The seed lives in the context's rng so live, fast-forward and offline runs share one sequence.
            if (!(this.context.Rng is DeterministicRng))
            {
                this.context = new SimContext(this.context.Rules, randomSeed, this.context.Mode);
            }

            encounter = new Encounter(this.context) { EnemyTargetRule = MapTargeting(targetingMode) };
            WireEncounterEvents();
        }

        // --- Events (mirrors the old API so UI/log keep working) ---
        public event Action<EnemyDamagedInfo> EnemyDamaged;

        /// <summary>An attack or ability was announced (attacker committed; effects land after the travel window).</summary>
        public event Action<int, CombatantSide, int, CombatantSide, string, int, bool> SwingStarted;

        /// <summary>(enemyIndex, goldReward) - one call per enemy, so the UI can attribute a 1-3 enemy wave.</summary>
        public event Action<int, double> EnemyKilled;

        /// <summary>
        /// (heroIndex, damage, currentHealth, maxHealth, attackerEnemyIndex, isCritical).
        /// The attacker index is what lets the battle log and the models say *which* enemy swung - with 1-3
        /// enemies a line like "Werewolf A hits Knight" must never be guesswork.
        /// </summary>
        public event Action<int, double, double, double, int, bool, bool> HeroDamaged;

        public event Action<int> HeroDied;

        public event Action PartyWiped;

        /// <summary>A hero or enemy cast its ability (Ability-Plan). Forwarded from the fight's own tick.</summary>
        public event Action<AbilityCastEvent> AbilityCast;

        /// <summary>Raised when a cast carries an on-target impact effect, with the lead time to its start.</summary>
        public event Action<ImpactVfxEvent> ImpactVfx;

        /// <summary>An ability healed a unit (forwarded at impact).</summary>
        public event Action<HealEvent> Healed;

        /// <summary>An ability raised a guard on a unit (forwarded at impact).</summary>
        public event Action<ShieldEvent> ShieldApplied;

        /// <summary>
        /// Supplies the ability a hero has equipped, by PARTY INDEX. Injected by the game wiring, so the sim never has
        /// to know about unlocks or the save file. Null (tests, tools) means no hero is carrying an ability.
        /// </summary>
        public Func<int, AbilityDef> AbilityFor { get; set; }

        // --- State ---
        public SimContext Context => context;

        public Encounter CurrentEncounter => encounter;

        /// <summary>The enemy being fought (index 0). Multi-enemy waves expose <see cref="Enemies"/>.</summary>
        public EnemyCombatant Enemy => enemies.Length > 0 ? enemies[0] : null;

        /// <summary>Every enemy in the current wave (1 today; up to the cap from Step 11 on).</summary>
        public EnemyCombatant[] Enemies => enemies;

        public int EnemyCount => enemies.Length;

        public int HeroCount => heroes.Length;

        public int AliveHeroCount => encounter.AlivePartyCount;

        /// <summary>Living enemies in the current wave - the director ends a wave only when this hits 0.</summary>
        public int AliveEnemyCount => encounter.AliveEnemyCount;

        public HeroCombatant[] Heroes => heroes;

        /// <summary>Who stands where (null until a formation is assigned).</summary>
        public Formation Formation => formation;

        public bool IsEncounterActive => AliveHeroCount > 0 && encounter.AliveEnemyCount > 0;

        public double EnemyHealthPercent => encounter.TotalEnemyHealthPercent;

        /// <summary>Updates the tuning snapshot (called when BalanceConfig changes).</summary>
        public void ApplyScaling(CombatScaling newScaling)
        {
            // The formation owns the row rule, so it is re-stamped on top of every rules refresh.
            SimRules rules = SimRulesFactory.FromScaling(newScaling);
            rules.BackRowTargetWeight = formation != null ? formation.Data.BackRowTargetWeight : rules.BackRowTargetWeight;
            context.ApplyRules(rules);
        }

        /// <summary>Replaces the whole context (rules + rng + mode + caps) - fast-forward and offline runs.</summary>
        public void ApplyContext(SimContext newContext)
        {
            if (newContext == null)
            {
                return;
            }

            context = newContext;
            encounter = new Encounter(context) { EnemyTargetRule = MapTargeting(targetingMode) };
            WireEncounterEvents();

            if (heroes.Length > 0)
            {
                encounter.SetParty(heroes);
            }
        }

        /// <summary>
        /// Swaps the stat source (the upgrade/prestige resolver). Recomputes party stats immediately so a
        /// live fight picks up the new numbers.
        /// </summary>
        public void SetStatProvider(ICombatStatProvider provider)
        {
            statProvider = provider ?? DefaultStatProvider.Instance;
            RefreshHeroStats();
        }

        // --- Party ---
        /// <summary>
        /// Builds the party from a PartyConfig. Returns false (and logs) when the party is unusable, so the
        /// caller can abort the run instead of fighting imaginary heroes.
        /// </summary>
        public bool SetupParty(PartyConfig party)
        {
            return SetupParty(party, null);
        }

        /// <summary>
        /// Builds the party and stamps each hero's formation position. The array stays in **PartyConfig order**
        /// (hero 0, 1, 2...) because that index is the hero's identity for stat levels; the row/column carries the
        /// position the sim's targeting rules read.
        /// </summary>
        public bool SetupParty(PartyConfig party, Formation assignedFormation)
        {
            if (party == null)
            {
                SimLog.LogError("[CombatSimulator] PartyConfig is null; cannot build a party.");
                return false;
            }

            int count = party.ValidHeroCount;
            if (count == 0)
            {
                SimLog.LogError("[CombatSimulator] PartyConfig has no heroes assigned.");
                return false;
            }

            heroes = new HeroCombatant[count];
            int index = 0;

            for (int i = 0; i < party.Heroes.Count && index < count; i++)
            {
                HeroData data = party.GetHero(i);
                if (data == null)
                {
                    continue;
                }

                heroes[index] = new HeroCombatant(
                    index,
                    data,
                    statProvider.GetMaxHealth(data, index),
                    statProvider.GetAttack(data, index),
                    statProvider.GetDefense(data, index),
                    ScaledInterval(statProvider.GetAttackInterval(data, index)))
                {
                    CritChance = statProvider.GetCritChance(data, index),
                    CritDamageMultiplier = statProvider.GetCritDamage(data, index)
                };

                index++;
            }

            // Arm every hero from the provider. Done once here, and again at the start of every wave, so an ability
            // bought or swapped between waves reaches the fight without restarting the game.
            ArmPartyAbilities();

            formation = assignedFormation;
            StampFormationRules();
            encounter.SetParty(heroes);
            ApplyFormation();
            return true;
        }

        /// <summary>
        /// Re-stamps each hero's rank/position from the board. Called after a swap so the next swing already
        /// respects the new layout (no rebuild, no lost health).
        /// </summary>
        public void ApplyFormation()
        {
            if (formation == null)
            {
                return;
            }

            for (int i = 0; i < heroes.Length; i++)
            {
                HeroCombatant hero = heroes[i];
                if (hero == null)
                {
                    continue;
                }

                hero.Row = formation.RankOfHero(hero.SlotIndex);
                hero.Column = formation.PositionOfHero(hero.SlotIndex);
            }
        }

        /// <summary>
        /// Copies the board's row rule into the frozen rules snapshot. Done here (not by the caller) so a fight can
        /// never run with a stale weighting because somebody forgot a call.
        /// </summary>
        private void StampFormationRules()
        {
            if (formation == null)
            {
                return;
            }

            SimRules rules = context.Rules;
            rules.BackRowTargetWeight = formation.Data.BackRowTargetWeight;
            context.ApplyRules(rules);
        }

        /// <summary>Re-reads derived stats from the provider while preserving each hero's health percentage.</summary>
        public void RefreshHeroStats()
        {
            for (int i = 0; i < heroes.Length; i++)
            {
                HeroCombatant hero = heroes[i];
                if (hero == null || hero.Data == null)
                {
                    continue;
                }

                hero.ApplyStats(
                    statProvider.GetMaxHealth(hero.Data, hero.Index),
                    statProvider.GetAttack(hero.Data, hero.Index),
                    statProvider.GetDefense(hero.Data, hero.Index),
                    ScaledInterval(statProvider.GetAttackInterval(hero.Data, hero.Index)));

                // Crit levels can change too (a purchase refreshes the fight's unit, not just the UI).
                hero.CritChance = statProvider.GetCritChance(hero.Data, hero.Index);
                hero.CritDamageMultiplier = statProvider.GetCritDamage(hero.Data, hero.Index);
            }
        }

        /// <summary>Heals the whole party to full (stage advance, retry, new run).</summary>
        public void HealParty()
        {
            encounter.HealParty();
        }

        /// <summary>Camp heal: a fraction of max HP, plus a fraction revive for the fallen.</summary>
        public void HealPartyFraction(double healFraction, double reviveFraction)
        {
            encounter.HealPartyFraction(healFraction, reviveFraction);
        }

        // --- Encounter lifecycle ---
        /// <summary>Spawns a fresh enemy. Returns false when the enemy could not be built.</summary>
        public bool StartEncounter(EnemyData enemyData, int stage, bool isBoss, double externalGoldMultiplier = 1d)
        {
            EnemyCombatant enemy = EnemyCombatant.Create(enemyData, stage, isBoss, context.Rules, externalGoldMultiplier);

            if (enemy == null)
            {
                return false;
            }

            enemies = new[] { enemy };
            ArmPartyAbilities();
            ArmEnemyAbilities();
            encounter.SpawnEnemies(enemies);
            encounter.PartyTargetRule = TargetRule.FrontMost;
            return true;
        }

        /// <summary>Spawns a whole enemy team (1..N enemies; composition comes from <see cref="EncounterFactory"/>).</summary>
        public bool StartEncounter(EnemyCombatant[] team)
        {
            if (team == null || team.Length == 0)
            {
                return false;
            }

            enemies = team;
            ArmPartyAbilities();
            ArmEnemyAbilities();
            encounter.SpawnEnemies(enemies);
            encounter.PartyTargetRule = TargetRule.FrontMost;
            return true;
        }

        /// <summary>Clears the current enemy without raising a kill event.</summary>
        public void AbortEncounter()
        {
            encounter.Clear();
            enemies = Array.Empty<EnemyCombatant>();
        }

        /// <summary>Advances the fight by <paramref name="deltaTime"/> seconds.</summary>
        public void Step(double deltaTime)
        {
            encounter.Step(deltaTime);
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------
        /// <summary>
        /// Reads each hero's equipped ability from the injected provider and gives it to the fight (Ability-Plan).
        /// Called when the party is built and at the start of every wave, so an ability bought or swapped between
        /// waves is live in the next one - no restart, and no debug-only path. The provider is the game wiring's
        /// job, so the sim never has to know about unlocks or the save file.
        /// </summary>
        private void ArmPartyAbilities()
        {
            if (heroes == null)
            {
                return;
            }

            for (int i = 0; i < heroes.Length; i++)
            {
                HeroCombatant hero = heroes[i];
                if (hero == null)
                {
                    continue;
                }

                AbilityDef equipped = AbilityFor != null ? AbilityFor(i) : null;

                // One mapping point: everything the fight needs about an ability is copied here, art window included.
                hero.SetAbility(ToSimAbility(equipped, hero.Data != null ? hero.Data.ArtSet : null));
            }
        }

        /// <summary>
        /// Gives every enemy its ability (data-driven from the enemy asset, never from an unlock or the save file).
        /// Enemies are armed at the start of each wave, which is also why their clock begins ready: a wave opens with
        /// the enemy able to use its move, exactly like a hero at full clock.
        /// </summary>
        private void ArmEnemyAbilities()
        {
            if (enemies == null)
            {
                return;
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyCombatant enemy = enemies[i];
                if (enemy == null || enemy.Data == null)
                {
                    continue;
                }

                AbilityDef[] owned = enemy.Data.Abilities;
                AbilityDef ability = owned != null && owned.Length > 0 ? owned[0] : null;

                enemy.SetAbility(ToSimAbility(ability, enemy.Data.ResolveArt(enemy.Stage)));

                // A wave must not open with an enemy move on its very first beat: the first roll waits 2-6 s, then the
                // normal chance decides. Seeded from the fight's own generator, so a replay stays in step.
                if (enemy.HasAbility)
                {
                    double wait = EnemyFirstCastMinSec +
                                  (context.Rng != null ? context.Rng.NextDouble() * (EnemyFirstCastMaxSec - EnemyFirstCastMinSec) : 0d);
                    enemy.HoldAbility(wait);
                }
            }
        }

        /// <summary>
        /// The single place a data ability becomes a fight ability. A new ability needs a spec row and an asset - no view
        /// code, no switch in the UI, no event of its own.
        /// </summary>
        private static SimAbility ToSimAbility(AbilityDef definition, CharacterArtSet art)
        {
            if (definition == null)
            {
                return SimAbility.None;
            }

            string slot = definition.ArtSlot;

            return new SimAbility
            {
                Id = definition.AbilityID,
                DisplayName = definition.DisplayName,
                ClipSlot = slot,
                CooldownSec = definition.CooldownSec,
                Kind = MapKind(definition.Kind),
                Target = MapTarget(definition.Target),
                ShieldFactor = definition.ShieldFactor,
                ShieldSeconds = definition.ShieldSeconds,
                HealFraction = definition.HealFraction,
                DamageMultiplier = definition.DamageMultiplier,
                VfxId = definition.ImpactVfx,
                ImpactDelaySeconds = definition.ImpactDelaySec,
                VfxScale = definition.ImpactVfxScale,
                GuardHits = definition.GuardHits,
                StayPlanted = definition.CastInPlace,

                // The art owns the travel window: the effect lands on the frame that strip reaches full reach.
                WindowSeconds = art != null ? art.SecondsFor(slot) : 0f
            };
        }

        private static SimAbilityKind MapKind(AbilityKind kind)
        {
            switch (kind)
            {
                case AbilityKind.Heal: return SimAbilityKind.Heal;
                case AbilityKind.Shield: return SimAbilityKind.Shield;
                default: return SimAbilityKind.Damage;
            }
        }

        private static SimAbilityTarget MapTarget(AbilityTarget target)
        {
            switch (target)
            {
                case AbilityTarget.Self: return SimAbilityTarget.Self;
                case AbilityTarget.Party: return SimAbilityTarget.Party;
                case AbilityTarget.LowestAlly: return SimAbilityTarget.LowestAlly;
                case AbilityTarget.FrontHero: return SimAbilityTarget.FrontHero;
                default: return SimAbilityTarget.FrontEnemy;
            }
        }

        private void WireEncounterEvents()
        {
            encounter.Damaged += OnEncounterDamaged;
            encounter.SwingStarted += OnEncounterSwingStarted;
            encounter.Died += OnEncounterDied;
            encounter.EnemyKilled += (index, gold) => EnemyKilled?.Invoke(index, gold);
            encounter.PartyWiped += () => PartyWiped?.Invoke();
            encounter.AbilityCast += cast => AbilityCast?.Invoke(cast);
            encounter.ImpactVfx += vfx => ImpactVfx?.Invoke(vfx);
            encounter.Healed += heal => Healed?.Invoke(heal);
            encounter.ShieldApplied += shield => ShieldApplied?.Invoke(shield);
        }

        private void OnEncounterSwingStarted(SwingStartedEvent swing)
        {
            SwingStarted?.Invoke(swing.AttackerIndex, swing.AttackerSide, swing.TargetIndex, swing.TargetSide, swing.ClipSlot, swing.CoveredAllyIndex, swing.StayPlanted);
        }

        private void OnEncounterDamaged(DamageEvent damage)
        {
            if (damage.TargetSide == CombatantSide.Enemy)
            {
                EnemyDamaged?.Invoke(new EnemyDamagedInfo(
                    damage.Damage, damage.TargetHealth, damage.TargetMaxHealth, damage.IsCritical,
                    damage.AttackerIndex, damage.TargetIndex, damage.VfxId, damage.AbilityName));
                return;
            }

            HeroDamaged?.Invoke(damage.TargetIndex, damage.Damage, damage.TargetHealth, damage.TargetMaxHealth, damage.AttackerIndex, damage.IsCritical, damage.IsGuarded);
        }

        private void OnEncounterDied(DeathEvent death)
        {
            if (death.Side == CombatantSide.Party)
            {
                HeroDied?.Invoke(death.Index);
            }
        }

        /// <summary>Applies the game-pace multiplier to an attack interval.</summary>
        private double ScaledInterval(double baseIntervalSec)
        {
            double scaled = baseIntervalSec * context.Rules.PaceMultiplier;
            return scaled < 0.1d ? 0.1d : scaled;
        }

        /// <summary>Maps the data-layer targeting enum onto the sim's target rules (one table, in EncounterFactory).</summary>
        private static TargetRule MapTargeting(EnemyTargetingMode mode)
        {
            return EncounterFactory.MapRule(mode) ?? TargetRule.FrontMost;
        }
    }
}
