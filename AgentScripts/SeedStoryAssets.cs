using System.IO;
using IdleRPG.Data;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-off authoring helper for the story slice. Lives outside Assets/ so it never becomes a project asset.
/// Run from the pipeline: run_script AgentScripts/SeedStoryAssets.cs SeedStoryAssets.Run
/// </summary>
public static class SeedStoryAssets
{
    private const string Folder = "Assets/IdleRPG/Data/Story/Stage01";
    private const string ScenePath = Folder + "/S01_Intro.asset";
    private const string SchedulePath = Folder + "/Stage01_Schedule.asset";

    public static void Run()
    {
        EnsureFolder(Folder);

        CharacterArtSet knight = Load<CharacterArtSet>("Assets/IdleRPG/Data/Characters/Knight_Art.asset");
        CharacterArtSet mage = Load<CharacterArtSet>("Assets/IdleRPG/Data/Characters/Wizard_Art.asset");
        CharacterArtSet soldier = Load<CharacterArtSet>("Assets/IdleRPG/Data/Characters/Soldier_Art.asset");
        CharacterArtSet slime = Load<CharacterArtSet>("Assets/IdleRPG/Data/Characters/Slime_Art.asset");

        if (knight == null || mage == null || soldier == null || slime == null)
        {
            Debug.LogError("[SeedStoryAssets] Missing one or more character art sets. Aborted.");
            return;
        }

        StorySceneDef scene = CreateScene(knight, mage, soldier, slime);
        StoryScheduleDef schedule = CreateSchedule(scene);

        EditorUtility.SetDirty(scene);
        EditorUtility.SetDirty(schedule);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SeedStoryAssets] Created {ScenePath} ({scene.Actors.Length} actors, {scene.Beats.Length} beats) " +
                  $"and {SchedulePath} ({schedule.Count} rule).");
    }

    private static StorySceneDef CreateScene(CharacterArtSet knight, CharacterArtSet mage,
        CharacterArtSet soldier, CharacterArtSet slime)
    {
        AssetDatabase.DeleteAsset(ScenePath);

        StorySceneDef scene = ScriptableObject.CreateInstance<StorySceneDef>();
        AssetDatabase.CreateAsset(scene, ScenePath);

        StoryActorDef[] actors =
        {
            new StoryActorDef("knight", "Knight", knight, false),
            new StoryActorDef("mage", "Mage", mage, false),
            new StoryActorDef("soldier", "Soldier", soldier, false),
            new StoryActorDef("slime", "Slime", slime, true)
        };

        StoryBeat[] beats =
        {
            new StoryBeat(0.5f, StoryVerb.Enter, "knight", new Vector2(0.20f, 0.40f), 1.2f),
            new StoryBeat(1.0f, StoryVerb.Enter, "mage", new Vector2(0.31f, 0.33f), 1.2f),
            new StoryBeat(1.5f, StoryVerb.Enter, "soldier", new Vector2(0.42f, 0.27f), 1.2f),
            new StoryBeat(4.2f, StoryVerb.Bubble, "soldier", default, 0f, string.Empty, BubbleKind.Question),
            new StoryBeat(4.8f, StoryVerb.Say, "soldier", default, 0f,
                "Something's wrong here. There's not a single bird, not even an insect."),
            new StoryBeat(9.0f, StoryVerb.Bubble, "knight", default, 0f, string.Empty, BubbleKind.Bang),
            new StoryBeat(9.6f, StoryVerb.Say, "knight", default, 0f,
                "Stay alert. I don't like how these trees seem to be watching us."),
            new StoryBeat(13.8f, StoryVerb.Bubble, "mage", default, 0f, string.Empty, BubbleKind.Dots),
            new StoryBeat(14.4f, StoryVerb.Say, "mage", default, 0f,
                "That's because they are... This forest is alive."),
            new StoryBeat(18.0f, StoryVerb.Bubble, "soldier", default, 0f, string.Empty, BubbleKind.None),
            new StoryBeat(18.6f, StoryVerb.Say, "soldier", default, 0f,
                "You could've mentioned that before we walked in!"),
            new StoryBeat(22.0f, StoryVerb.Bubble, "mage", default, 0f, string.Empty, BubbleKind.Bang),
            new StoryBeat(22.5f, StoryVerb.Say, "mage", default, 0f, "Quiet. Something's moving."),
            new StoryBeat(25.3f, StoryVerb.WalkTo, "knight", new Vector2(0.24f, 0.40f), 1.0f),
            new StoryBeat(26.5f, StoryVerb.WalkTo, "knight", new Vector2(0.20f, 0.40f), 1.0f),
            new StoryBeat(28.3f, StoryVerb.Enter, "slime", new Vector2(0.80f, 0.40f), 2.8f),
            new StoryBeat(31.7f, StoryVerb.Bubble, "slime", default, 0f, string.Empty, BubbleKind.Dots),
            new StoryBeat(32.3f, StoryVerb.Bubble, "knight", default, 0f, string.Empty, BubbleKind.Bang),
            new StoryBeat(32.9f, StoryVerb.Say, "knight", default, 0f, "What in the blazes is that?"),
            new StoryBeat(36.3f, StoryVerb.Bubble, "mage", default, 0f, string.Empty, BubbleKind.None),
            new StoryBeat(36.9f, StoryVerb.Say, "mage", default, 0f,
                "A living slime. And it's heading straight for us."),
            new StoryBeat(41.1f, StoryVerb.Say, "soldier", default, 0f,
                "Well, let's give it a warm welcome!"),
            new StoryBeat(44.3f, StoryVerb.Bubble, "knight", default, 0f, string.Empty, BubbleKind.Bang),
            new StoryBeat(44.8f, StoryVerb.Say, "knight", default, 0f, "Weapons ready!"),
            new StoryBeat(47.6f, StoryVerb.PlayClip, "knight", default, 0f, string.Empty, BubbleKind.None, "Attack01"),
            new StoryBeat(47.8f, StoryVerb.PlayClip, "soldier", default, 0f, string.Empty, BubbleKind.None, "Attack01"),
            new StoryBeat(48.4f, StoryVerb.End)
        };

        scene.EditorSetContent("s01_intro", actors, beats, 54f);

        EditorUtility.SetDirty(scene);
        AssetDatabase.SaveAssets();
        return scene;
    }

    private static StoryScheduleDef CreateSchedule(StorySceneDef scene)
    {
        AssetDatabase.DeleteAsset(SchedulePath);

        StoryScheduleDef schedule = ScriptableObject.CreateInstance<StoryScheduleDef>();
        AssetDatabase.CreateAsset(schedule, SchedulePath);

        StoryRule rule = new StoryRule
        {
            cue = StoryCue.BeforeWave,
            scene = scene,
            minStage = 1,
            maxStage = 1,
            wave = new StoryWaveSpec(WaveMode.Exact, 1),
            priority = 10,
            playLimit = PlayLimit.OncePerStage,
            maxPerStage = 1
        };

        schedule.EditorSetRules(new[] { rule });

        EditorUtility.SetDirty(schedule);
        AssetDatabase.SaveAssets();
        return schedule;
    }

    private static void SetActor(SerializedProperty actor, string id, string displayName,
        CharacterArtSet art, bool facesLeft)
    {
        actor.FindPropertyRelative("id").stringValue = id;
        actor.FindPropertyRelative("displayName").stringValue = displayName;
        actor.FindPropertyRelative("art").objectReferenceValue = art;
        actor.FindPropertyRelative("facesLeft").boolValue = facesLeft;
        actor.FindPropertyRelative("scale").floatValue = 1f;
    }

    private static void Enter(SerializedProperty beat, float time, string actorId, float x, float y, float duration)
    {
        SetBeat(beat, time, StoryVerb.Enter, actorId);
        SetSpot(beat, x, y);
        beat.FindPropertyRelative("duration").floatValue = duration;
    }

    private static void WalkTo(SerializedProperty beat, float time, string actorId, float x, float y, float duration)
    {
        SetBeat(beat, time, StoryVerb.WalkTo, actorId);
        SetSpot(beat, x, y);
        beat.FindPropertyRelative("duration").floatValue = duration;
    }

    private static void HideBubble(SerializedProperty beat, float time, string actorId)
    {
        SetBeat(beat, time, StoryVerb.Bubble, actorId);
    }

    private static void PlayClip(SerializedProperty beat, float time, string actorId, string slot)
    {
        SetBeat(beat, time, StoryVerb.PlayClip, actorId);
        beat.FindPropertyRelative("clip").stringValue = slot;
    }

    private static void Bubble(SerializedProperty beat, float time, string actorId, BubbleKind kind)
    {
        SetBeat(beat, time, StoryVerb.Bubble, actorId);
        beat.FindPropertyRelative("bubble").enumValueIndex = (int)kind;
    }

    private static void Say(SerializedProperty beat, float time, string actorId, string text)
    {
        SetBeat(beat, time, StoryVerb.Say, actorId);
        beat.FindPropertyRelative("text").stringValue = text;
    }

    private static void End(SerializedProperty beat, float time)
    {
        SetBeat(beat, time, StoryVerb.End, string.Empty);
    }

    private static void SetBeat(SerializedProperty beat, float time, StoryVerb verb, string actorId)
    {
        beat.FindPropertyRelative("time").floatValue = time;
        beat.FindPropertyRelative("verb").enumValueIndex = (int)verb;
        beat.FindPropertyRelative("actorId").stringValue = actorId;
        beat.FindPropertyRelative("text").stringValue = string.Empty;
        beat.FindPropertyRelative("clip").stringValue = string.Empty;
        beat.FindPropertyRelative("bubble").enumValueIndex = (int)BubbleKind.None;
        beat.FindPropertyRelative("duration").floatValue = 0f;
        SetSpot(beat, 0f, 0f);
    }

    private static void SetSpot(SerializedProperty beat, float x, float y)
    {
        SerializedProperty spot = beat.FindPropertyRelative("spot");
        spot.FindPropertyRelative("x").floatValue = x;
        spot.FindPropertyRelative("y").floatValue = y;
    }

    private static T Load<T>(string path) where T : Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), path));
        AssetDatabase.Refresh();
    }
}
