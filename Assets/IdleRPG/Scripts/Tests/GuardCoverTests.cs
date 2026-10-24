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
        public void Guard_DirectHitIsHalved_ButDoesNotSpendACharge()
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
            Assert.AreEqual(2, knight.CoverGuardHitsRemaining, "a direct hit must not spend an interception");
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
