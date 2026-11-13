using System.Collections.Generic;
using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>What the title band reads, resolved from a land and a step.</summary>
    public readonly struct JourneyLabel
    {
        public readonly string Land;
        public readonly string Place;
        public readonly string Boss;

        public JourneyLabel(string land, string place, string boss)
        {
            Land = land;
            Place = place;
            Boss = boss;
        }

        /// <summary>The line for the title band: the place when it exists, otherwise the land.</summary>
        public string Display => string.IsNullOrEmpty(Place) ? Land : Place;
    }

    public enum TrailMarkKind
    {
        Step,
        Camp,
        Boss
    }

    /// <summary>One mark on the trail under the land name.</summary>
    public readonly struct TrailMark
    {
        public readonly TrailMarkKind Kind;
        public readonly int Step;
        public readonly bool Cleared;
        public readonly bool Current;
        public readonly bool Available;

        public TrailMark(TrailMarkKind kind, int step, bool cleared, bool current, bool available)
        {
            Kind = kind;
            Step = step;
            Cleared = cleared;
            Current = current;
            Available = available;
        }
    }

    /// <summary>
    /// Turns a stage and a step into the words and marks the battle band shows.
    ///
    /// ELI5: a translator. The fight speaks in stage and wave numbers; the band speaks in places and
    /// marks. This file is the only place that translates, so every screen says the same thing.
    ///
    /// Pure: no scene, no manager, no time. That is what makes it testable and safe to call every time
    /// a step changes.
    /// </summary>
    public static class JourneyResolver
    {
        /// <summary>
        /// Shown when no catalog is present. Temporary: a blank band would hide a wiring fault, and a
        /// wrong word is easier to see than no word.
        /// </summary>
        public const string FallbackLandName = "Greenwake";

        public static JourneyLabel Resolve(JourneyCatalog catalog, int stage, int step)
        {
            JourneyCatalog.Land land = catalog != null ? catalog.LandFor(stage) : null;

            string landName = land != null && !string.IsNullOrEmpty(land.name)
                ? land.name
                : FallbackLandName;

            string bossName = land != null ? land.bossName : null;
            string placeName = null;

            if (land != null && land.places != null)
            {
                int safeStep = Mathf.Max(1, step);

                foreach (JourneyCatalog.Place place in land.places)
                {
                    if (place == null || string.IsNullOrEmpty(place.name))
                    {
                        continue;
                    }

                    if (safeStep >= place.fromStep && safeStep <= place.toStep)
                    {
                        placeName = place.name;
                        break;
                    }
                }
            }

            return new JourneyLabel(landName, placeName, bossName);
        }

        /// <summary>
        /// The trail for one stage. Steps in order; a camp after every <paramref name="campEverySteps"/>
        /// steps (including the last normal step, which is the pre-boss rest); one boss mark at the end.
        /// </summary>
        public static List<TrailMark> BuildTrail(int normalSteps, int currentStep, int campEverySteps)
        {
            int steps = Mathf.Max(1, normalSteps);
            int campEvery = Mathf.Max(0, campEverySteps);
            int current = Mathf.Clamp(currentStep, 1, steps + 1);

            List<TrailMark> marks = new List<TrailMark>(steps + 5);

            for (int step = 1; step <= steps; step++)
            {
                bool cleared = step < current;
                marks.Add(new TrailMark(TrailMarkKind.Step, step, cleared, step == current, false));

                if (campEvery > 0 && step % campEvery == 0)
                {
                    bool campAvailable = step == current;
                    marks.Add(new TrailMark(TrailMarkKind.Camp, step, cleared && !campAvailable, false,
                        campAvailable));
                }
            }

            int bossStep = steps + 1;
            marks.Add(new TrailMark(TrailMarkKind.Boss, bossStep, current > bossStep, current == bossStep, false));

            return marks;
        }
    }
}
