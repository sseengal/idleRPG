using System;
using System.Collections.Generic;
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
    /// picture. Author one with <see cref="CutsceneBuilder"/> or fill the steps in an asset.
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

    /// <summary>
    /// Writes a play in one place: one line per beat. This is the structure content is authored with - it cannot
    /// express anything except where a unit stands and which clip it plays, so a scene can never quietly grow game
    /// rules and the whole shot reads as a list.
    /// </summary>
    public sealed class CutsceneBuilder
    {
        private readonly CutsceneDef def;
        private readonly List<CutsceneStep> steps = new List<CutsceneStep>();

        public CutsceneBuilder(string id)
        {
            def = ScriptableObject.CreateInstance<CutsceneDef>();
            def.id = id;
        }

        public CutsceneBuilder PauseBattle(bool paused)
        {
            def.pauseBattle = paused;
            return this;
        }

        public CutsceneBuilder Skippable(bool skippable)
        {
            def.skippable = skippable;
            return this;
        }

        public CutsceneBuilder HardCap(float seconds)
        {
            def.hardCapSeconds = seconds;
            return this;
        }

        /// <summary>`2.10 s: the enemy walks to the middle of the screen.`</summary>
        public CutsceneBuilder Move(float time, CombatantSide side, int index, Vector2 spot)
        {
            steps.Add(new CutsceneStep
            {
                time = time,
                verb = CutsceneVerb.MoveUnit,
                side = side,
                index = index,
                spot = spot
            });

            return this;
        }

        /// <summary>`0.90 s: the enemy plays its attack strip.`</summary>
        public CutsceneBuilder PlayClip(float time, CombatantSide side, int index)
        {
            steps.Add(new CutsceneStep
            {
                time = time,
                verb = CutsceneVerb.PlayClip,
                side = side,
                index = index
            });

            return this;
        }

        public CutsceneDef Build()
        {
            steps.Sort((a, b) => a.time.CompareTo(b.time));
            def.steps = steps.ToArray();

            return def;
        }
    }
}
