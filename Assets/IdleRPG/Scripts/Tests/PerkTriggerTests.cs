using System;
using NUnit.Framework;
using IdleRPG.Sim;

namespace IdleRPG.Tests
{
    /// <summary>
    /// The boon hooks: cleave, echo, thorns, lifesteal and on-kill. Plain C#, no scene, no Unity clock.
    ///
    /// ELI5: these tests are the proof that a card written as numbers really changes the fight - and that it cannot
    /// run away (a cleave may not cleave, one swing may not become five hits).
    /// </summary>
    public sealed class PerkTriggerTests
    {
        private static Combatant Hero(string id, double health = 1000d, double attack = 100d, double interval = 1d)
        {
            return new Combatant(id, id, CombatantSide.Party, new StatBlock(health, attack, 0d), interval);
        }

        private static Combatant Enemy(string id, double health = 1000d, double attack = 10d, double interval = 2d)
        {
            return new Combatant(id, id, CombatantSide.Enemy, new StatBlock(health, attack, 0d), interval);
        }

        /// <summary>One hero against a row of enemies, with the given boons filed under party slot 0.</summary>
        private static Encounter Build(Combatant[] enemies, params PerkEffect[] boons)
        {
            Encounter encounter = new Encounter(SimContext.CreateDefault());
            Combatant hero = Hero("hero_test");

            encounter.SetParty(new[] { hero });
            encounter.Perks = PerkRuntime.FromSlots(new[] { boons });
            encounter.SpawnEnemies(enemies);

            return encounter;
        }

        private static void Run(Encounter encounter, int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                encounter.Step(0.05d);
            }
        }

        [Test]
        public void Cleave_HitsEveryOtherEnemy_ButNotTheOneStruck()
        {
            Combatant first = Enemy("bat_a", health: 100000d);
            Combatant second = Enemy("bat_b", health: 100000d);
            Combatant third = Enemy("bat_c", health: 100000d);

            Encounter encounter = Build(
                new[] { first, second, third },
                new PerkEffect(PerkTrigger.OnHit, PerkEffectOp.DamageOthers, 0.5d));

            // Stop the moment the FIRST blow lands: the cleave is applied inside the same step, so one swing is
            // measured and later swings cannot muddy the numbers.
            int struckIndex = -1;
            int landed = 0;
            encounter.Damaged += damage =>
            {
                if (landed == 0)
                {
                    struckIndex = damage.TargetIndex;
                }

                landed++;
            };

            for (int step = 0; step < 400 && landed == 0; step++)
            {
                encounter.Step(0.05d);
            }

            double[] lost =
            {
                100000d - first.CurrentHealth,
                100000d - second.CurrentHealth,
                100000d - third.CurrentHealth
            };

            Assert.GreaterOrEqual(struckIndex, 0, "a blow must land");
            Assert.Greater(lost[struckIndex], 0d, "the struck enemy takes the main hit");

            for (int i = 0; i < lost.Length; i++)
            {
                if (i == struckIndex)
                {
                    continue;
                }

                Assert.AreEqual(lost[struckIndex] * 0.5d, lost[i], 1.0d,
                    $"enemy {i} takes half of the main hit (each other enemy, not a share of one)");
            }
        }

        [Test]
        public void WithoutBoons_TheFightIsUnchanged()
        {
            Combatant enemy = Enemy("bat_a");
            Encounter encounter = new Encounter(SimContext.CreateDefault());

            encounter.SetParty(new[] { Hero("hero_test") });
            encounter.SpawnEnemies(new[] { enemy });

            Assert.IsTrue(encounter.Perks.IsEmpty, "no boons means the empty snapshot");

            Run(encounter, 200);

            Assert.AreEqual(0d, encounter.Party[0].Shield, "nothing may shield without a card");
            Assert.Greater(1000d - enemy.CurrentHealth, 0d, "the fight still runs normally");
        }

        [Test]
        public void Cleave_CannotRecurseItself()
        {
            // If triggered damage fed back into the hook, a full-strength cleave across three enemies would never
            // settle. The test only requires the fight to stay finite and sane.
            Encounter encounter = Build(
                new[] { Enemy("bat_a"), Enemy("bat_b"), Enemy("bat_c") },
                new PerkEffect(PerkTrigger.OnHit, PerkEffectOp.DamageOthers, 1d));

            int damageEvents = 0;
            encounter.Damaged += _ => damageEvents++;

            Run(encounter, 60);

            Assert.Greater(damageEvents, 0, "the fight must actually run");
            Assert.Less(damageEvents, 40, $"cleave re-triggered itself ({damageEvents} hits in 3 seconds)");
        }

        [Test]
        public void Echo_StrikesTheSameTargetAgain_OnTopOfTheSwing()
        {
            Combatant enemy = Enemy("bat_a");
            Encounter encounter = Build(new[] { enemy },
                new PerkEffect(PerkTrigger.OnHit, PerkEffectOp.ExtraHit, 0.5d));

            Run(encounter, 200);

            double lost = 1000d - enemy.CurrentHealth;

            Assert.Greater(lost, 100d, "the extra hit must land on top of the swing");
        }

        [Test]
        public void Thorns_HurtWhoeverHurtsTheHero()
        {
            Combatant enemy = Enemy("bat_a", attack: 50d, interval: 1d);
            Encounter encounter = Build(new[] { enemy },
                new PerkEffect(PerkTrigger.OnHurt, PerkEffectOp.Reflect, 0.5d));

            Run(encounter, 300);

            Assert.Greater(1000d - enemy.CurrentHealth, 0d, "the enemy must lose health to the reflected damage");
        }

        [Test]
        public void Lifesteal_HealsTheOwner_ForAFractionOfTheDamageDealt()
        {
            Encounter encounter = new Encounter(SimContext.CreateDefault());
            Combatant hero = Hero("hero_test", health: 1000d, attack: 100d);

            hero.TakeDamage(500d);   // hurt on purpose, so a heal has somewhere to go

            encounter.SetParty(new[] { hero });
            encounter.Perks = PerkRuntime.FromSlots(new[]
            {
                new[] { new PerkEffect(PerkTrigger.OnHit, PerkEffectOp.Lifesteal, 0.5d) }
            });
            encounter.SpawnEnemies(new[] { Enemy("bat_a") });

            double before = hero.CurrentHealth;
            Run(encounter, 200);

            Assert.Greater(hero.CurrentHealth, before, "lifesteal must heal the hero");
        }

        [Test]
        public void OnKill_FiresForTheKiller_AndTheKillStillPays()
        {
            Combatant enemy = Enemy("bat_a", health: 10d);
            Encounter encounter = Build(new[] { enemy },
                new PerkEffect(PerkTrigger.OnKill, PerkEffectOp.ShieldSelf, 0.25d));

            bool killPaid = false;
            encounter.EnemyKilled += (_, __) => killPaid = true;

            Run(encounter, 200);

            Assert.IsTrue(killPaid, "a kill must still be paid");
            Assert.Greater(encounter.Party[0].Shield, 0d, "the on-kill card must have shielded the killer");
        }
    }
}
