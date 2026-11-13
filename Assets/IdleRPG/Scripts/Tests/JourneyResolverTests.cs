using IdleRPG.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace IdleRPG.Tests
{
    /// <summary>
    /// The journey words and the trail. Pins the contract the battle band depends on: the player is never
    /// shown a stage number, a land resolves for any stage, places are optional, and the trail follows the
    /// stage plan and the camp cadence instead of a fixed count of ten.
    ///
    /// The numbers here are small and round on purpose: a balance pass may change the asset without
    /// rewriting this suite.
    /// </summary>
    public class JourneyResolverTests
    {
        private JourneyCatalog catalog;

        [TearDown]
        public void TearDown()
        {
            if (catalog != null)
            {
                Object.DestroyImmediate(catalog);
                catalog = null;
            }
        }

        /// <summary>Builds a catalog through its serialized fields, the same shape an author edits.</summary>
        private JourneyCatalog BuildCatalog(bool withPlaces)
        {
            catalog = ScriptableObject.CreateInstance<JourneyCatalog>();

            SerializedObject serialized = new SerializedObject(catalog);
            serialized.FindProperty("stagesPerLand").intValue = 10;

            SerializedProperty lands = serialized.FindProperty("lands");
            lands.arraySize = 1;

            SerializedProperty land = lands.GetArrayElementAtIndex(0);
            land.FindPropertyRelative("name").stringValue = "Greenwake";
            land.FindPropertyRelative("bossName").stringValue = "The Slime King";

            SerializedProperty places = land.FindPropertyRelative("places");
            places.arraySize = withPlaces ? 1 : 0;

            if (withPlaces)
            {
                SerializedProperty place = places.GetArrayElementAtIndex(0);
                place.FindPropertyRelative("name").stringValue = "The Wall";
                place.FindPropertyRelative("fromStep").intValue = 5;
                place.FindPropertyRelative("toStep").intValue = 6;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        [Test]
        public void NoCatalog_FallsBackToTheLandName_AndNeverBlanks()
        {
            JourneyLabel label = JourneyResolver.Resolve(null, 7, 3);

            Assert.AreEqual(JourneyResolver.FallbackLandName, label.Land);
            Assert.AreEqual(JourneyResolver.FallbackLandName, label.Display);
            Assert.IsNull(label.Place);
        }

        [Test]
        public void AnyStage_ResolvesToTheLand_BecausePlacesAreOptional()
        {
            JourneyCatalog built = BuildCatalog(withPlaces: false);

            for (int stage = 1; stage <= 40; stage++)
            {
                JourneyLabel label = JourneyResolver.Resolve(built, stage, 1);

                Assert.AreEqual("Greenwake", label.Land);
                Assert.IsNull(label.Place);
                Assert.AreEqual("Greenwake", label.Display);
            }
        }

        [Test]
        public void APlaceCoversItsStepRangeOnly()
        {
            JourneyCatalog built = BuildCatalog(withPlaces: true);

            Assert.IsNull(JourneyResolver.Resolve(built, 1, 4).Place);
            Assert.AreEqual("The Wall", JourneyResolver.Resolve(built, 1, 5).Place);
            Assert.AreEqual("The Wall", JourneyResolver.Resolve(built, 1, 6).Place);
            Assert.IsNull(JourneyResolver.Resolve(built, 1, 7).Place);

            // A place wins the title slot while it applies.
            Assert.AreEqual("The Wall", JourneyResolver.Resolve(built, 1, 5).Display);
        }

        [Test]
        public void BossName_ComesFromTheLand()
        {
            JourneyCatalog built = BuildCatalog(withPlaces: false);

            Assert.AreEqual("The Slime King", JourneyResolver.Resolve(built, 1, 7).Boss);
        }

        [Test]
        public void Trail_NineSteps_HasThreeCampsAndOneBoss()
        {
            var marks = JourneyResolver.BuildTrail(normalSteps: 9, currentStep: 1, campEverySteps: 3);

            Assert.AreEqual(13, marks.Count);
            Assert.AreEqual(9, CountOf(marks, TrailMarkKind.Step));
            Assert.AreEqual(3, CountOf(marks, TrailMarkKind.Camp));
            Assert.AreEqual(1, CountOf(marks, TrailMarkKind.Boss));

            Assert.AreEqual(3, CampAfter(marks, 0).Step);
            Assert.AreEqual(6, CampAfter(marks, 1).Step);
            Assert.AreEqual(9, CampAfter(marks, 2).Step);
            Assert.AreEqual(10, Boss(marks).Step);
        }

        [Test]
        public void Trail_SixSteps_HasTwoCampsAndBossAtSeven()
        {
            var marks = JourneyResolver.BuildTrail(normalSteps: 6, currentStep: 1, campEverySteps: 3);

            Assert.AreEqual(9, marks.Count);
            Assert.AreEqual(6, CountOf(marks, TrailMarkKind.Step));
            Assert.AreEqual(2, CountOf(marks, TrailMarkKind.Camp));
            Assert.AreEqual(7, Boss(marks).Step);
        }

        [Test]
        public void Trail_ClearedAndCurrent_MoveWithTheStep()
        {
            var marks = JourneyResolver.BuildTrail(normalSteps: 9, currentStep: 3, campEverySteps: 3);

            Assert.IsTrue(Step(marks, 1).Cleared);
            Assert.IsTrue(Step(marks, 2).Cleared);
            Assert.IsTrue(Step(marks, 3).Current);
            Assert.IsFalse(Step(marks, 3).Cleared);
            Assert.IsFalse(Step(marks, 4).Cleared);
            Assert.IsFalse(Step(marks, 4).Current);

            // The camp after a cleared step is the one you can take right now.
            Assert.IsTrue(CampAfter(marks, 0).Available);

            // The next camp is not offered yet.
            Assert.IsFalse(CampAfter(marks, 1).Available);
        }

        [Test]
        public void Trail_BossStep_IsCurrent_AndEarlierMarksAreCleared()
        {
            var marks = JourneyResolver.BuildTrail(normalSteps: 6, currentStep: 7, campEverySteps: 3);

            Assert.IsTrue(Boss(marks).Current);
            Assert.IsFalse(Boss(marks).Cleared);
            Assert.IsTrue(Step(marks, 6).Cleared);
        }

        [Test]
        public void Trail_ClampsAFutureStep_SoBadDataCannotBreakTheBand()
        {
            var marks = JourneyResolver.BuildTrail(normalSteps: 6, currentStep: 99, campEverySteps: 3);

            Assert.IsTrue(Boss(marks).Current);
            Assert.AreEqual(9, marks.Count);
        }

        private static int CountOf(System.Collections.Generic.List<TrailMark> marks, TrailMarkKind kind)
        {
            int count = 0;

            foreach (TrailMark mark in marks)
            {
                if (mark.Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private static TrailMark Step(System.Collections.Generic.List<TrailMark> marks, int step)
        {
            foreach (TrailMark mark in marks)
            {
                if (mark.Kind == TrailMarkKind.Step && mark.Step == step)
                {
                    return mark;
                }
            }

            Assert.Fail("No step mark " + step);
            return default;
        }

        private static TrailMark Boss(System.Collections.Generic.List<TrailMark> marks)
        {
            foreach (TrailMark mark in marks)
            {
                if (mark.Kind == TrailMarkKind.Boss)
                {
                    return mark;
                }
            }

            Assert.Fail("No boss mark");
            return default;
        }

        private static TrailMark CampAfter(System.Collections.Generic.List<TrailMark> marks, int index)
        {
            int seen = 0;

            foreach (TrailMark mark in marks)
            {
                if (mark.Kind != TrailMarkKind.Camp)
                {
                    continue;
                }

                if (seen == index)
                {
                    return mark;
                }

                seen++;
            }

            Assert.Fail("No camp at index " + index);
            return default;
        }
    }
}

