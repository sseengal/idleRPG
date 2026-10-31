using UnityEngine;
using IdleRPG.Data;

namespace IdleRPG.Core
{
    /// <summary>
    /// Story wiring: the schedule asset and the service that decides when a scene plays.
    /// </summary>
    public sealed partial class GameManager
    {
        [Tooltip("When story scenes play. Empty = no story scenes, combat behaves exactly as before.")]
        [SerializeField] private StoryScheduleDef storySchedule;

        /// <summary>The story scheduler. Null only when wiring failed.</summary>
        public StoryService Story { get; private set; }
    }
}
