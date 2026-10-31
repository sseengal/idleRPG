using System;
using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>What one beat of a story scene does. A scene is a picture, so every verb only changes the picture.</summary>
    public enum StoryVerb
    {
        /// <summary>Walk in from off-screen to <see cref="StoryBeat.spot"/>.</summary>
        Enter = 0,

        /// <summary>Walk to <see cref="StoryBeat.spot"/> from the current spot.</summary>
        WalkTo = 1,

        /// <summary>Play a named clip from the actor's art set.</summary>
        PlayClip = 2,

        /// <summary>Show a symbol (or a short word) above the actor.</summary>
        Bubble = 3,

        /// <summary>Write one line to the battle log as <c>Name: text</c>.</summary>
        Say = 4,

        /// <summary>Hide the actor.</summary>
        Hide = 5,

        /// <summary>End the scene early. The hard cap is the safety net.</summary>
        End = 6
    }

    /// <summary>The feeling a speech bubble shows. Words are the exception, not the rule.</summary>
    public enum BubbleKind
    {
        None = 0,
        Dots = 1,
        Bang = 2,
        Question = 3,

        /// <summary>Use <see cref="StoryBeat.text"/>. Keep it to 1-3 words.</summary>
        Word = 4
    }

    /// <summary>
    /// One actor a scene can address. Actors are local to the scene: the same art set may be used by two
    /// different ids in two different scenes without sharing any state.
    /// </summary>
    [Serializable]
    public struct StoryActorDef
    {
        [Tooltip("Id used by the beats in this scene, e.g. \"knight\". Only has to be unique inside this scene.")]
        public string id;

        [Tooltip("Name shown in the battle log, e.g. \"Knight\".")]
        public string displayName;

        [Tooltip("Frames for the actor. Uses the same art sets as the battle units.")]
        public CharacterArtSet art;

        [Tooltip("Face the other way. Heroes face right by default; enemies face left.")]
        public bool facesLeft;

        [Tooltip("Size dial. 0 or 1 = the layer's default slot size.")]
        public float scale;

        public StoryActorDef(string id, string displayName, CharacterArtSet art, bool facesLeft, float scale = 1f)
        {
            this.id = id;
            this.displayName = displayName;
            this.art = art;
            this.facesLeft = facesLeft;
            this.scale = scale;
        }

        /// <summary>Scale with a safe default, so a new actor does not render at zero size.</summary>
        public float SafeScale => scale <= 0f ? 1f : scale;
    }

    /// <summary>One thing at one time. Beats are ordered by <see cref="time"/> when the asset is validated.</summary>
    [Serializable]
    public struct StoryBeat
    {
        [Tooltip("Seconds from the start of the scene.")]
        public float time;

        public StoryVerb verb;

        [Tooltip("Which actor this beat addresses. Empty for End.")]
        public string actorId;

        [Tooltip("Where the actor stands, as a fraction of the battle view (0..1).")]
        public Vector2 spot;

        [Tooltip("Walk time in seconds for Enter / WalkTo. 0 = snap into place.")]
        public float duration;

        [Tooltip("Say: the line. Bubble: the word, when bubble = Word.")]
        public string text;

        [Tooltip("Which symbol the bubble shows. Bubble verb only.")]
        public BubbleKind bubble;

        [Tooltip("Clip slot for PlayClip: Idle, Walk, Attack01, Hurt, Death, Ability.")]
        public string clip;

        public StoryBeat(float time, StoryVerb verb, string actorId = "", Vector2 spot = default,
            float duration = 0f, string text = "", BubbleKind bubble = BubbleKind.None, string clip = "")
        {
            this.time = time;
            this.verb = verb;
            this.actorId = actorId;
            this.spot = spot;
            this.duration = duration;
            this.text = text;
            this.bubble = bubble;
            this.clip = clip;
        }
    }

    /// <summary>
    /// One story scene: actors plus timed beats. It holds no game state and cannot change the fight.
    ///
    /// ELI5: a short play. You say who walks in, when a bubble appears, and what the log says. The
    /// schedule asset decides where the play runs. This asset never knows the stage or the wave.
    /// </summary>
    [CreateAssetMenu(fileName = "StoryScene", menuName = "Idle RPG/Story/Story Scene", order = 20)]
    public sealed class StorySceneDef : ScriptableObject
    {
        [Tooltip("Stable id. Used by the save and by random-wave rolls. Never rename after ship.")]
        [SerializeField] private string id = "";

        [Tooltip("Stop the fight for the whole scene. Leave on for anything the player should watch.")]
        [SerializeField] private bool pauseBattle = true;

        [Tooltip("A tap (or Space) ends the scene early. Leave on.")]
        [SerializeField] private bool skippable = true;

        [Tooltip("Hard stop in real seconds. Even a broken beat can never trap the player.")]
        [SerializeField] private float hardCapSeconds = 14f;

        [SerializeField] private StoryActorDef[] actors = Array.Empty<StoryActorDef>();

        [SerializeField] private StoryBeat[] beats = Array.Empty<StoryBeat>();

        /// <summary>The stable id, falling back to the asset name so an unset field still works.</summary>
        public string Id => string.IsNullOrEmpty(id) ? name : id;

        public bool PauseBattle => pauseBattle;

        public bool Skippable => skippable;

        /// <summary>Never below 1 second, so a zero cannot disable the safety net.</summary>
        public float HardCapSeconds => Mathf.Max(1f, hardCapSeconds);

        public StoryActorDef[] Actors => actors ?? Array.Empty<StoryActorDef>();

        public StoryBeat[] Beats => beats ?? Array.Empty<StoryBeat>();

        /// <summary>Index of an actor id, or -1. The layer uses it to address its views.</summary>
        public int IndexOfActor(string actorId)
        {
            if (string.IsNullOrEmpty(actorId) || actors == null)
            {
                return -1;
            }

            for (int i = 0; i < actors.Length; i++)
            {
                if (string.Equals(actors[i].id, actorId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>True when the id exists, so the player can reject a bad beat instead of guessing.</summary>
        public bool TryGetActor(string actorId, out StoryActorDef actor)
        {
            int index = IndexOfActor(actorId);

            if (index < 0)
            {
                actor = default;
                return false;
            }

            actor = actors[index];
            return true;
        }

#if UNITY_EDITOR
        /// <summary>Editor-only content write used by the asset seeder. Runtime never calls it.</summary>
        public void EditorSetContent(string sceneId, StoryActorDef[] newActors, StoryBeat[] newBeats, float capSeconds)
        {
            id = sceneId ?? string.Empty;
            actors = newActors ?? Array.Empty<StoryActorDef>();
            beats = newBeats ?? Array.Empty<StoryBeat>();
            hardCapSeconds = Mathf.Max(1f, capSeconds);
        }
#endif

        /// <summary>Keeps the beats in time order and the cap sane while the asset is edited.</summary>
        private void OnValidate()
        {
            hardCapSeconds = Mathf.Max(1f, hardCapSeconds);

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[StorySceneDef] '{name}' has no id. The asset name is used at runtime; set a stable id before ship.");
            }

            if (beats == null || beats.Length < 2)
            {
                return;
            }

            Array.Sort(beats, (a, b) => a.time.CompareTo(b.time));
        }
    }
}
