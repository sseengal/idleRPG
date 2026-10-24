using NUnit.Framework;
using IdleRPG.Sim;

namespace IdleRPG.Tests
{
    /// <summary>
    /// Shield Wall as a party cover stance: the Knight intercepts hits aimed at any ally, takes half of them, and has
    /// a fixed number of interceptions. Hits aimed at him directly are halved but do not spend a charge.
    /// </summary>
    public sealed class GuardCoverTests
    {
        private static Combatant Member(string id, double health = 1000d, double attack = 10d)
        {
            return new Combatant(id, id, CombatantSide.Party, new StatBlock(health, attack, 0d), 5d);
        }

        private static Combatant Enemy(double attack = 100d)
        {
            // Crit is rolled from the rules by default; a near-zero chance keeps the damage exact in a test.
            return new Combatant("enemy", "Enemy", CombatantSide.Enemy, new StatBlock(10000d, attack, 0d), 1d)
            {
                CritChance = 0.0001d
            };
        }

        private static Encounter Build(Combatant[] party, out Combatant enemy)
        {
            var encounter = new Encounter(SimContext.CreateDefault());
            enemy = Enemy();
            encounter.SetParty(party);
            encounter.SpawnEnemies(new[] { enemy });
            return encounter;
        }

        [Test]
        public void Guard_InterceptsAHitAimedAtAnAlly_AndHalvesIt()
        {
            Combatant ally = Member("ally");
            Combatant knight = Member("knight");
            Encounter encounter = Build(new[] { ally, knight }, out _);

            knight.BeginCoverGuard(2, 0.5d);

            int hitIndex = -1;
            encounter.Damaged += damage =>
            {
                if (damage.TargetSide == CombatantSide.Party && hitIndex < 0)
                {
                    hitIndex = damage.TargetIndex;
                }
            };

            for (int step = 0; step < 400 && hitIndex < 0; step++)
            {
                encounter.Step(0.02d);
            }

            Assert.AreEqual(1, hitIndex, "the hit aimed at the ally must land on the knight");
            Assert.AreEqual(950d, knight.CurrentHealth, 1d, "the knight must take half of a 100-damage hit");
            Assert.AreEqual(1000d, ally.CurrentHealth, 0.001d, "the covered ally must take nothing");
        }

        [Test]
        public void Guard_DirectHitIsHalved_AndSpendsACharge()
        {
            Combatant knight = Member("knight");
            Combatant ally = Member("ally");
            Encounter encounter = Build(new[] { knight, ally }, out _);

            knight.BeginCoverGuard(2, 0.5d);

            for (int step = 0; step < 400 && knight.CurrentHealth >= 1000d; step++)
            {
                encounter.Step(0.02d);
            }

            Assert.AreEqual(950d, knight.CurrentHealth, 1d, "a hit aimed at the knight is halved while guarding");
            Assert.AreEqual(1, knight.CoverGuardHitsRemaining, "any hit that lands while guarding spends a charge");
        }

        [Test]
        public void Guard_EndsAfterItsInterceptions()
        {
            Combatant ally = Member("ally");
            Combatant knight = Member("knight");
            Encounter encounter = Build(new[] { ally, knight }, out _);

            knight.BeginCoverGuard(2, 0.5d);

            var partyHits = new System.Collections.Generic.List<int>();
            encounter.Damaged += damage =>
            {
                if (damage.TargetSide == CombatantSide.Party)
                {
                    partyHits.Add(damage.TargetIndex);
                }
            };

            for (int step = 0; step < 1200 && partyHits.Count < 3; step++)
            {
                encounter.Step(0.02d);
            }

            Assert.GreaterOrEqual(partyHits.Count, 3, "three party hits must resolve");
            Assert.AreEqual(1, partyHits[0], "first hit is intercepted");
            Assert.AreEqual(1, partyHits[1], "second hit is intercepted");
            Assert.AreEqual(0, partyHits[2], "third hit goes back to the ally");
            Assert.AreEqual(0, knight.CoverGuardHitsRemaining);
        }

        [Test]
        public void Guard_ClearsOnWaveChange()
        {
            Combatant ally = Member("ally");
            Combatant knight = Member("knight");
            Encounter encounter = Build(new[] { ally, knight }, out _);

            knight.BeginCoverGuard(2, 0.5d);
            Assert.AreEqual(2, knight.CoverGuardHitsRemaining);

            encounter.SpawnEnemies(new[] { Enemy() });

            Assert.AreEqual(0, knight.CoverGuardHitsRemaining, "a new wave must clear the stance");
        }

        [Test]
        public void Guard_InterceptsAnEnemyAbility()
        {
            Combatant ally = Member("ally");
            Combatant knight = Member("knight");
            Encounter encounter = Build(new[] { ally, knight }, out Combatant enemy);

            enemy.SetAbility(new SimAbility
            {
                Id = "test_spike",
                DisplayName = "Spike",
                ClipSlot = "Ability",
                CooldownSec = 0.5d,
                Kind = SimAbilityKind.Damage,
                Target = SimAbilityTarget.FrontHero,
                DamageMultiplier = 2d,
                WindowSeconds = 0.1d
            });

            knight.BeginCoverGuard(2, 0.5d);

            int hitIndex = -1;
            double hitDamage = 0d;

            encounter.Damaged += damage =>
            {
                if (damage.TargetSide == CombatantSide.Party && hitIndex < 0)
                {
                    hitIndex = damage.TargetIndex;
                    hitDamage = damage.Damage;
                }
            };

            // Enemy casts are gated by a random patience roll, so give the fight plenty of sim time.
            for (int step = 0; step < 4000 && hitIndex < 0; step++)
            {
                encounter.Step(0.02d);
            }

            Assert.AreEqual(1, hitIndex, "an enemy ability aimed at the ally must land on the guarding knight");
            Assert.AreEqual(1000d, ally.CurrentHealth, 0.001d, "the covered ally must take nothing from the ability");
            Assert.Less(knight.CurrentHealth, 1000d, "the knight takes the ability hit");
            Assert.Less(hitDamage, 100d, "the intercepted ability hit is halved");
            Assert.AreEqual(1, knight.CoverGuardHitsRemaining, "an intercepted ability spends one charge");
        }

        [Test]
        public void TimedShield_StillHurts_AndDoesNotIntercept()
        {
            Combatant ally = Member("ally");
            Combatant knight = Member("knight");
            Encounter encounter = Build(new[] { ally, knight }, out _);

            knight.ApplyGuard(0.5d, 4d);

            Assert.AreEqual(0.5d, knight.DamageTakenMultiplier, 0.0001d, "the old timed shield still halves damage");
            Assert.IsFalse(knight.IsCoverGuardActive, "a timed shield is not a cover stance");
            Assert.AreEqual(0, knight.CoverGuardHitsRemaining);
        }
    }
}
