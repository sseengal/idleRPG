using System;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Progression;

namespace IdleRPG.Combat
{
    /// <summary>
    /// Deterministic, Unity-free auto-battle simulation for one encounter at a time:
    /// three heroes attack one enemy, the enemy attacks back. Only <see cref="Step"/>
    /// advances time, so the exact same code can power live combat, a fast-forward,
    /// or an offline estimate.
    ///
    /// Order inside one step: all living heroes swing first, then the enemy swings.
    /// </summary>
    public sealed class CombatSimulator
    {
        private ICombatStatProvider statProvider;
        private readonly EnemyTargetingMode targetingMode;

        private CombatScaling scaling;
        private Random random;
        private HeroCombatant[] heroes = Array.Empty<HeroCombatant>();
        private int aliveHeroCount;
        private bool partyWipeRaised;

        public CombatSimulator(ICombatStatProvider statProvider, EnemyTargetingMode targetingMode, CombatScaling scaling, int randomSeed)
        {
            this.statProvider = statProvider ?? DefaultStatProvider.Instance;
            this.targetingMode = targetingMode;
            this.scaling = scaling.Sanitized();
            random = new Random(randomSeed);
        }

        // ------------------------------------------------------------------
        // Events (mirrors of the global GameEvents, raised per simulation step)
        // ------------------------------------------------------------------
        public event Action<EnemyDamagedInfo> EnemyDamaged;

        public event Action<double> EnemyKilled;

        public event Action<int, double, double, double> HeroDamaged;

        public event Action<int> HeroDied;

        public event Action PartyWiped;

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------
        public EnemyCombatant Enemy { get; private set; }

        public int HeroCount => heroes.Length;

        public int AliveHeroCount => aliveHeroCount;

        public HeroCombatant[] Heroes => heroes;

        public bool IsEncounterActive => Enemy != null && Enemy.IsAlive && aliveHeroCount > 0;

        public double EnemyHealthPercent => Enemy == null ? 0d : Enemy.HealthPercent;

        /// <summary>Updates the tuning snapshot (called when BalanceConfig changes).</summary>
        public void ApplyScaling(CombatScaling newScaling)
        {
            scaling = newScaling.Sanitized();
        }

        /// <summary>
        /// Swaps the stat source (Step 3 injects the upgrade/prestige aware resolver).
        /// Recomputes party stats immediately so a live fight picks up the new numbers.
        /// </summary>
        public void SetStatProvider(ICombatStatProvider provider)
        {
            statProvider = provider ?? DefaultStatProvider.Instance;
            RefreshHeroStats();
        }

        /// <summary>
        /// Builds the party from a PartyConfig. Returns false (and logs) when the party is
        /// unusable, so the caller can abort the run instead of fighting imaginary heroes.
        /// </summary>
        public bool SetupParty(PartyConfig party)
        {
            if (party == null)
            {
                UnityEngine.Debug.LogError("[CombatSimulator] PartyConfig is null; cannot build a party.");
                return false;
            }

            int count = party.ValidHeroCount;
            if (count == 0)
            {
                UnityEngine.Debug.LogError("[CombatSimulator] PartyConfig has no heroes assigned.");
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
                    statProvider.GetAttackInterval(data, index));

                index++;
            }

            RecountAliveHeroes();
            partyWipeRaised = false;

            return true;
        }

        /// <summary>
        /// Re-reads derived stats from the provider (after an upgrade purchase) while
        /// preserving each hero's current health percentage.
        /// </summary>
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
                    statProvider.GetAttackInterval(hero.Data, hero.Index));
            }
        }

        /// <summary>Heals the whole party to full (stage advance, retry, new run).</summary>
        public void HealParty()
        {
            for (int i = 0; i < heroes.Length; i++)
            {
                heroes[i]?.RestoreFullHealth();
            }

            RecountAliveHeroes();
            partyWipeRaised = false;
        }

        // ------------------------------------------------------------------
        // Encounter lifecycle
        // ------------------------------------------------------------------
        /// <summary>Spawns a fresh enemy. Returns false when the enemy could not be built.</summary>
        public bool StartEncounter(EnemyData enemyData, int stage, bool isBoss, double externalGoldMultiplier = 1d)
        {
            Enemy = EnemyCombatant.Create(enemyData, stage, isBoss, scaling, externalGoldMultiplier);
            partyWipeRaised = false;
            RecountAliveHeroes();

            return Enemy != null;
        }

        /// <summary>Clears the current enemy without raising a kill event.</summary>
        public void AbortEncounter()
        {
            Enemy = null;
        }

        // ------------------------------------------------------------------
        // Simulation
        // ------------------------------------------------------------------
        /// <summary>Advances the fight by <paramref name="deltaTime"/> seconds.</summary>
        public void Step(double deltaTime)
        {
            if (deltaTime <= 0d || Enemy == null)
            {
                return;
            }

            if (!Enemy.IsAlive || aliveHeroCount == 0)
            {
                return;
            }

            if (ResolveHeroAttacks(deltaTime))
            {
                // Enemy died this step: skip its turn entirely.
                return;
            }

            ResolveEnemyAttack(deltaTime);
        }

        /// <summary>Runs every living hero's swing. Returns true when the enemy died.</summary>
        private bool ResolveHeroAttacks(double deltaTime)
        {
            for (int i = 0; i < heroes.Length; i++)
            {
                HeroCombatant hero = heroes[i];
                if (hero == null || !hero.IsAlive || !hero.Tick(deltaTime))
                {
                    continue;
                }

                bool isCritical = scaling.CriticalChance > 0d && random.NextDouble() < scaling.CriticalChance;
                double multiplier = (isCritical ? scaling.CriticalDamageMultiplier : 1d) * statProvider.GlobalDamageMultiplier;
                double damage = FormulaUtility.Damage(hero.Attack, Enemy.Defense, scaling.MinDamageRatio, multiplier);

                if (damage <= 0d)
                {
                    continue;
                }

                Enemy.TakeDamage(damage);
                EnemyDamaged?.Invoke(new EnemyDamagedInfo(damage, Enemy.CurrentHealth, Enemy.MaxHealth, isCritical));

                if (!Enemy.IsAlive)
                {
                    EnemyKilled?.Invoke(Enemy.GoldReward);
                    return true;
                }
            }

            return false;
        }

        /// <summary>Runs the enemy's swing against the selected hero.</summary>
        private void ResolveEnemyAttack(double deltaTime)
        {
            if (!Enemy.Tick(deltaTime))
            {
                return;
            }

            HeroCombatant target = SelectTarget();
            if (target == null)
            {
                return;
            }

            double damage = FormulaUtility.Damage(Enemy.Attack, target.Defense, scaling.MinDamageRatio);
            if (damage <= 0d)
            {
                return;
            }

            double applied = target.TakeDamage(damage);
            HeroDamaged?.Invoke(target.Index, applied, target.CurrentHealth, target.MaxHealth);

            if (target.IsAlive)
            {
                return;
            }

            HeroDied?.Invoke(target.Index);
            RecountAliveHeroes();

            if (aliveHeroCount == 0 && !partyWipeRaised)
            {
                partyWipeRaised = true;
                PartyWiped?.Invoke();
            }
        }

        /// <summary>Picks the hero the enemy attacks, per the configured targeting mode.</summary>
        private HeroCombatant SelectTarget()
        {
            HeroCombatant best = null;

            for (int i = 0; i < heroes.Length; i++)
            {
                HeroCombatant hero = heroes[i];
                if (hero == null || !hero.IsAlive)
                {
                    continue;
                }

                if (best == null)
                {
                    best = hero;

                    if (targetingMode == EnemyTargetingMode.FrontMost)
                    {
                        // Lowest index wins, so the first alive hero is the tank.
                        return best;
                    }

                    continue;
                }

                if (targetingMode == EnemyTargetingMode.LowestHealthPercent && hero.HealthPercent < best.HealthPercent)
                {
                    best = hero;
                }
            }

            if (best == null || targetingMode != EnemyTargetingMode.Random || aliveHeroCount <= 1)
            {
                return best;
            }

            // Deterministic pick among the living.
            int pick = random.Next(aliveHeroCount);
            int seen = 0;

            for (int i = 0; i < heroes.Length; i++)
            {
                HeroCombatant hero = heroes[i];
                if (hero == null || !hero.IsAlive)
                {
                    continue;
                }

                if (seen == pick)
                {
                    return hero;
                }

                seen++;
            }

            return best;
        }

        private void RecountAliveHeroes()
        {
            int alive = 0;

            for (int i = 0; i < heroes.Length; i++)
            {
                if (heroes[i] != null && heroes[i].IsAlive)
                {
                    alive++;
                }
            }

            aliveHeroCount = alive;
        }
    }
}