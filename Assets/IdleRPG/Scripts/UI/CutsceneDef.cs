using System;
using UnityEngine;
using IdleRPG.Sim;

namespace IdleRPG.UI
{
    /// <summary>
    /// What one step of a fixed play does. Only two verbs exist, because a play is a picture: it can put a unit
    /// somewhere, or play that unit's clip. Nothing here can damage, heal, grant loot or move the player's formation.
    /// </summary>
    public enum CutsceneVerb
    {
        /// <summary>Sends a unit to a point on screen (0..1, x and y).</summary>
        MoveUnit = 0,

        /// <summary>Plays a unit's action clip.</summary>
        PlayClip = 1
    }

    /// <summary>
    /// One beat of a fixed play: a time, a verb and its arguments. No logic lives here - the runner walks the beats in
    /// time order, which is what makes a play reproducible from run to run.
    /// </summary>
    [Serializable]
    public struct CutsceneStep
    {
        [Tooltip("Seconds from the start of the play.")]
        public float time;

        public CutsceneVerb verb;

        [Tooltip("Which side the unit is on.")]
        public CombatantSide side;

        [Tooltip("Party or enemy index.")]
        public int index;

        [Tooltip("Where to stand, in screen fractions (0..1). MoveUnit only.")]
        public Vector2 spot;
    }

    /// <summary>
    /// A fixed, animation-locked play. It holds no game state and has no way to change the fight: a cutscene is a
    /// picture. Fill the steps in an asset.
    /// </summary>
    [CreateAssetMenu(fileName = "CutsceneDef", menuName = "Idle RPG/Cutscene", order = 20)]
    public sealed class CutsceneDef : ScriptableObject
    {
        public string id = "cutscene";

        [Tooltip("Stop combat while this plays. Leave on for anything the player should watch.")]
        public bool pauseBattle = true;

        [Tooltip("A tap (or SPACE) ends it early.")]
        public bool skippable = true;

        [Tooltip("Hard stop: even a broken beat can never trap the player longer than this.")]
        public float hardCapSeconds = 8f;

        public CutsceneStep[] steps = Array.Empty<CutsceneStep>();
    }
}
