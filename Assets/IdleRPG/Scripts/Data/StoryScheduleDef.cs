using System;
using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// The moment a story scene is asked to play. A cue maps to one code seam; it is never faked by a timer.
    /// </summary>
    public enum StoryCue
    {
        /// <summary>Just before a wave spawns. Wave 1 is the node intro.</summary>
        BeforeWave = 0,

        /// <summary>After a wave is cleared, before the next one spawns. Same seam as BeforeWave, earlier in time.</summary>
        AfterWave = 1,

        /// <summary>After the boss dies, before the node advances.</summary>
        StageCleared = 2,

        /// <summary>After the party is wiped, before the retry.</summary>
        Defeat = 3,

        /// <summary>A caller asks for the scene. Debug, tutorials, menus.</summary>
        Manual = 4
    }

    /// <summary>Which waves a rule applies to. Random is rolled by the service, because the roll needs ids.</summary>
    public enum WaveMode
    {
        Any = 0,
        Exact = 1,
        EveryN = 2,
        Random = 3
    }

    /// <summary>How often a rule may play.</summary>
    public enum PlayLimit
    {
        EveryTime = 0,
        OncePerStage = 1,
        OncePerRun = 2
    }

    /// <summary>
    /// The wave part of a rule. It answers only the deterministic part: Any, Exact and EveryN. Random is
    /// true here and is rolled by <c>StoryService</c> with a stable hash, so a relaunch never re-rolls.
    /// </summary>
    [Serializable]
    public struct StoryWaveSpec
    {
        public WaveMode mode;

        [Tooltip("Exact: the wave number. 1-based.")]
        public int wave;

        [Tooltip("EveryN: the step. Values below 1 are treated as 1.")]
        public int everyN;

        [Tooltip("EveryN: first wave that can match. 0 and 1 are the same.")]
        public int offset;

        [Range(0f, 1f)]
        [Tooltip("Random: chance per wave, 0..1.")]
        public float chance;

        public StoryWaveSpec(WaveMode mode, int wave = 1, float chance = 0.25f, int everyN = 3, int offset = 0)
        {
            this.mode = mode;
            this.wave = wave;
            this.chance = chance;
            this.everyN = everyN;
            this.offset = offset;
        }

        /// <summary>True when the rule is rolled, not tested. The service owns the roll.</summary>
        public bool IsRandom => mode == WaveMode.Random;

        /// <summary>Deterministic wave match. Random always returns true here.</summary>
        public bool Matches(int waveNumber)
        {
            int number = Mathf.Max(1, waveNumber);

            switch (mode)
            {
                case WaveMode.Exact:
                    return number == Mathf.Max(1, wave);

                case WaveMode.EveryN:
                    int step = Mathf.Max(1, everyN);
                    int start = Mathf.Max(1, offset);
                    return number >= start && (number - start) % step == 0;

                default:
                    return true;
            }
        }
    }

    /// <summary>
    /// One place a scene can play. The same scene may be used by several rules; the rule is the placement,
    /// the scene is the content. This is what lets one scene run before a fight, after a fight or before a
    /// random wave without editing the scene.
    /// </summary>
    [Serializable]
    public struct StoryRule
    {
        public StoryCue cue;

        [Tooltip("What plays. Required.")]
        public StorySceneDef scene;

        [Tooltip("First stage this rule applies to. 0 = no lower bound.")]
        public int minStage;

        [Tooltip("Last stage this rule applies to. 0 = no upper bound.")]
        public int maxStage;

        public StoryWaveSpec wave;

        [Tooltip("Higher plays first when several rules match the same moment.")]
        public int priority;

        public PlayLimit playLimit;

        [Tooltip("Cap per stage. 0 = no cap. Use 1 on Random rules so a scene cannot repeat in one node.")]
        public int maxPerStage;

        /// <summary>True when the stage falls inside the rule's range. Zero means "no bound".</summary>
        public bool MatchesStage(int stage)
        {
            if (minStage > 0 && stage < minStage)
            {
                return false;
            }

            return maxStage <= 0 || stage <= maxStage;
        }
    }

    /// <summary>
    /// The placement sheet for one node (or a whole chapter). It holds rules only: no beats, no text.
    ///
    /// ELI5: the list of "when to play what". Add a row to put a scene before a fight, after a fight, before
    /// a random wave, at the boss, or on a manual call. No code change, no scene edit.
    /// </summary>
    [CreateAssetMenu(fileName = "StorySchedule", menuName = "Idle RPG/Story/Story Schedule", order = 21)]
    public sealed class StoryScheduleDef : ScriptableObject
    {
        [Tooltip("Rules in authoring order. Equal priority plays in this order.")]
        [SerializeField] private StoryRule[] rules = Array.Empty<StoryRule>();

        public StoryRule[] Rules => rules ?? Array.Empty<StoryRule>();

        public int Count => rules == null ? 0 : rules.Length;

#if UNITY_EDITOR
        /// <summary>Editor-only rule write used by the asset seeder. Runtime never calls it.</summary>
        public void EditorSetRules(StoryRule[] newRules)
        {
            rules = newRules ?? Array.Empty<StoryRule>();
        }
#endif

        /// <summary>Keeps the asset valid while it is edited.</summary>
        private void OnValidate()
        {
            if (rules == null)
            {
                return;
            }

            for (int i = 0; i < rules.Length; i++)
            {
                if (rules[i].scene == null)
                {
                    Debug.LogWarning($"[StoryScheduleDef] '{name}' row {i} has no scene. The row is ignored at runtime.");
                }
            }
        }
    }
}
