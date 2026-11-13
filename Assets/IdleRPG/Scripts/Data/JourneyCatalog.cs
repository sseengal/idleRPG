using System;
using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// The names of the journey, in one asset: the lands and, later, the places inside a land.
    ///
    /// ELI5: the signposts. The battle title asks this asset "where are we?" and gets one word back.
    /// When the chapter is written, the same asset also answers "which part of the land?", and no code
    /// changes.
    ///
    /// The player never sees a stage number. One land covers <see cref="StagesPerLand"/> stages, and a
    /// stage index is translated here, so the fiction and the engine stay separated.
    /// </summary>
    [CreateAssetMenu(fileName = "JourneyCatalog", menuName = "Idle RPG/Data/Journey Catalog", order = 13)]
    public sealed class JourneyCatalog : ScriptableObject
    {
        /// <summary>One named part of a land, covering a range of steps inside a stage.</summary>
        [Serializable]
        public sealed class Place
        {
            public string name;

            [Min(1)] public int fromStep = 1;
            [Min(1)] public int toStep = 1;
        }

        /// <summary>One named land, with an optional list of places.</summary>
        [Serializable]
        public sealed class Land
        {
            public string name;

            [Tooltip("Shown on the boss step, in the title band.")]
            public string bossName;

            [Tooltip("Optional. An empty list means the land name is the only label - the current state.")]
            public Place[] places = Array.Empty<Place>();
        }

        [SerializeField] private Land[] lands = Array.Empty<Land>();

        [SerializeField, Min(1)]
        [Tooltip("How many stages one land covers. A milestone boss ends each land.")]
        private int stagesPerLand = 10;

        public int Count => lands == null ? 0 : lands.Length;

        public int StagesPerLand => Mathf.Max(1, stagesPerLand);

        /// <summary>The land for a 1-based stage, or null when the asset holds no lands.</summary>
        public Land LandFor(int stage)
        {
            if (Count == 0)
            {
                return null;
            }

            int index = (Mathf.Max(1, stage) - 1) / StagesPerLand;
            return lands[index % Count];
        }
    }
}
