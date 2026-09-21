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

        public event Action<double> EnemyKilled;

        public event Action<int, double, double, double> HeroDamaged;

        public event Action<int> HeroDied;

        public event Action PartyWiped;

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

        public HeroCombatant[] Heroes => heroes;

        /// <summary>Who stands where (null until a formation is assigned).</summary>
        public Formation Formation => formation;

        public bool IsEncounterActive => Enemy != null && Enemy.IsAlive && AliveHeroCount > 0;

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
                    ScaledInterval(statProvider.GetAttackInterval(data, index)));

                index++;
            }

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
            }
        }

        /// <summary>Heals the whole party to full (stage advance, retry, new run).</summary>
        public void HealParty()
        {
            encounter.HealParty();
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
            encounter.SpawnEnemies(enemies);
            encounter.PartyTargetRule = TargetRule.FrontMost;
            return true;
        }

        /// <summary>Spawns a whole enemy team (multi-enemy waves arrive in Step 11).</summary>
        public bool StartEncounter(EnemyCombatant[] team)
        {
            if (team == null || team.Length == 0)
            {
                return false;
            }

            enemies = team;
            encounter.SpawnEnemies(enemies);
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
        private void WireEncounterEvents()
        {
            encounter.Damaged += OnEncounterDamaged;
            encounter.Died += OnEncounterDied;
            encounter.EnemyKilled += gold => EnemyKilled?.Invoke(gold);
            encounter.PartyWiped += () => PartyWiped?.Invoke();
        }

        private void OnEncounterDamaged(DamageEvent damage)
        {
            if (damage.TargetSide == CombatantSide.Enemy)
            {
                EnemyDamaged?.Invoke(new EnemyDamagedInfo(
                    damage.Damage, damage.TargetHealth, damage.TargetMaxHealth, damage.IsCritical, damage.AttackerIndex));
                return;
            }

            HeroDamaged?.Invoke(damage.TargetIndex, damage.Damage, damage.TargetHealth, damage.TargetMaxHealth);
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

        /// <summary>Maps the data-layer targeting enum onto the sim's target rules (decision A2).</summary>
        private static TargetRule MapTargeting(EnemyTargetingMode mode)
        {
            switch (mode)
            {
                case EnemyTargetingMode.LowestHealthPercent:
                    return TargetRule.LowestHealthPercent;
                case EnemyTargetingMode.Random:
                    return TargetRule.Random;
                default:
                    return TargetRule.FrontMost;
            }
        }
    }
}
