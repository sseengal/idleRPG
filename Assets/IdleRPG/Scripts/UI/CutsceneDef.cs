using System;
using UnityEngine;
using IdleRPG.Sim;

namespace IdleRPG.UI
{
    /// <summary>What one step of a cutscene does.</summary>
    public enum CutsceneVerb
    {
        /// <summary>Nothing happens; holds the beat (a pause between lines).</summary>
        Wait = 0,

        /// <summary>The black bars slide in.</summary>
        BarsIn = 1,

        /// <summary>The black bars slide out (the shot is over).</summary>
        BarsOut = 2,

        /// <summary>Sets the title line.</summary>
        Text = 3,

        /// <summary>Sends a unit to a point on screen (0..1, x and y).</summary>
        MoveUnit = 4,

        /// <summary>Plays a unit's action clip.</summary>
        PlayClip = 5
    }

    /// <summary>
    /// One beat of a fixed play: a time, a verb and its arguments. There is no logic here - the runner walks them in
    /// order, which is what makes a cutscene reproducible.
    /// </summary>
    [Serializable]
    public struct CutsceneStep
    {
        [Tooltip("Seconds from the start of the cutscene.")]
        public float time;

        public CutsceneVerb verb;

        [Tooltip("Text for the Text verb.")]
        public string text;

        [Tooltip("Which side the unit is on (MoveUnit / PlayClip).")]
        public CombatantSide side;

        [Tooltip("Party or enemy index (MoveUnit / PlayClip).")]
        public int index;

        [Tooltip("Where to stand, in screen fractions (0..1). MoveUnit only.")]
        public Vector2 spot;
    }

    /// <summary>
    /// A fixed, animation-locked play: bars, a line of text, a unit moving, a clip playing. Nothing here can damage,
    /// heal, grant loot or change the party - a cutscene is a picture, and it is deliberately unable to be anything
    /// else. Fill the steps in an asset later; the test one is built in code.
    /// </summary>
    [CreateAssetMenu(fileName = "CutsceneDef", menuName = "Idle RPG/Cutscene", order = 20)]
    public sealed class CutsceneDef : ScriptableObject
    {
        public string id = "cutscene";

        [Tooltip("Stop combat while this plays. Leave on for anything the player should watch.")]
        public bool pauseBattle = true;

        [Tooltip("A tap (or SPACE) ends it early.")]
        public bool skippable = true;

        [Tooltip("Hard stop: even a broken step can never trap the player longer than this.")]
        public float hardCapSeconds = 8f;

        public CutsceneStep[] steps = Array.Empty<CutsceneStep>();

        /// <summary>
        /// Test play: bars, a title, one enemy walking to the middle. Long enough to judge the feel, short enough to
        /// press again. Not content - a probe with a title on it.
        /// </summary>
        public static CutsceneDef CreateSample()
        {
            CutsceneDef def = CreateInstance<CutsceneDef>();
            def.id = "sample_boss_intro";
            def.hardCapSeconds = 5f;
            def.steps = new[]
            {
                new CutsceneStep { time = 0f, verb = CutsceneVerb.BarsIn },
                new CutsceneStep { time = 0.30f, verb = CutsceneVerb.MoveUnit, side = CombatantSide.Enemy, index = 0, spot = new Vector2(0.62f, 0.55f) },
                new CutsceneStep { time = 0.90f, verb = CutsceneVerb.PlayClip, side = CombatantSide.Enemy, index = 0 },
                new CutsceneStep { time = 1.10f, verb = CutsceneVerb.Text, text = "THE ORC CHIEFTAIN" },
                new CutsceneStep { time = 3.20f, verb = CutsceneVerb.BarsOut }
            };
            return def;
        }
    }
}
