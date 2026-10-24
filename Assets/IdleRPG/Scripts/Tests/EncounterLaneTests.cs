using NUnit.Framework;
using IdleRPG.Sim;

namespace IdleRPG.Tests
{
    /// <summary>
    /// Guards the two rules the combat lane is built on: one action in flight at a time, and an ability landing only
    /// after its animation window. The sim is plain C#, so these run with no scene and no Unity clock.
    /// </summary>
    public sealed class EncounterLaneTests
    {
        private static Combatant Hero(string id, double attack = 10d, double interval = 1d)
        {
            return new Combatant(id, id, CombatantSide.Party, new StatBlock(1000d, attack, 0d), interval);
        }

        private static Combatant Enemy(string id, double health = 10000d, double attack = 1d, double interval = 2d)
        {
            return new Combatant(id, id, CombatantSide.Enemy, new StatBlock(health, attack, 0d), interval);
        }

        private static Encounter Build(out Combatant hero, out Combatant enemy)
        {
            var encounter = new Encounter(SimContext.CreateDefault());
            hero = Hero("hero_test");
            enemy = Enemy("enemy_test");

            encounter.SetParty(new[] { hero });
            encounter.SpawnEnemies(new[] { enemy });
            return encounter;
        }

        [Test]
        public void Lane_NeverHasTwoActionsInFlight()
        {
            Encounter encounter = Build(out _, out _);

            int started = 0;
            int landed = 0;

            encounter.SwingStarted += _ => started++;
            encounter.Damaged += _ => landed++;

            for (int step = 0; step < 400; step++)
            {
                encounter.Step(0.05d);

                Assert.LessOrEqual(
                    started - landed,
                    1,
                    $"two actions were in flight after step {step} ({started} announced, {landed} landed)");
            }

            Assert.Greater(started, 2, "the lane must actually run during the test window");
        }

        [Test]
        public void Ability_CarriesItsImpactEffectToTheDamageEvent()
        {
            Encounter encounter = Build(out Combatant hero, out _);

            hero.SetAbility(new SimAbility
            {
                Id = "test_strike",
                DisplayName = "Test Strike",
                ClipSlot = "Ability",
                CooldownSec = 10d,
                Kind = SimAbilityKind.Damage,
                Target = SimAbilityTarget.FrontEnemy,
                DamageMultiplier = 2d,
                WindowSeconds = 0.2d,
                VfxId = "test_fx"
            });

            string seen = null;
            encounter.Damaged += damage => seen = damage.VfxId;

            Assert.IsTrue(encounter.RequestAbility(0), "the ready hero must accept the cast request");

            for (int step = 0; step < 200 && seen == null; step++)
            {
                encounter.Step(0.02d);
            }

            Assert.AreEqual("test_fx", seen, "the damage event must carry the ability's impact effect id");
        }

        [Test]
        public void PlainSwing_CarriesNoImpactEffect()
        {
            Encounter encounter = Build(out _, out _);

            string seen = "sentinel";
            encounter.Damaged += damage => seen = damage.VfxId;

            for (int step = 0; step < 200 && seen == "sentinel"; step++)
            {
                encounter.Step(0.02d);
            }

            Assert.AreEqual(string.Empty, seen ?? string.Empty, "a plain swing must not ask for an impact effect");
        }

        [Test]
        public void Ability_LandsOnlyAfterItsCastWindow()
        {
            Encounter encounter = Build(out Combatant hero, out _);

            hero.SetAbility(new SimAbility
            {
                Id = "test_shield",
                DisplayName = "Test Shield",
                ClipSlot = "Ability",
                CooldownSec = 14d,
                Kind = SimAbilityKind.Shield,
                Target = SimAbilityTarget.Self,
                ShieldFactor = 0.5d,
                ShieldSeconds = 5d,
                WindowSeconds = 0.4d
            });

            double shieldAt = -1d;
            double now = 0d;

            encounter.ShieldApplied += _ =>
            {
                if (shieldAt < 0d)
                {
                    shieldAt = now;
                }
            };

            Assert.IsTrue(encounter.RequestAbility(0), "the ready hero must accept the cast request");
            Assert.AreEqual(-1d, shieldAt, "the effect must not land on the frame the button was pressed");

            for (int step = 0; step < 200 && shieldAt < 0d; step++)
            {
                now += 0.02d;
                encounter.Step(0.02d);
            }

            Assert.Greater(shieldAt, 0.2d, $"the shield landed after {shieldAt:0.00}s - the cast window was skipped");
            Assert.Less(shieldAt, 2.0d, $"the shield landed after {shieldAt:0.00}s - the cast never resolved");
        }
    }
}
