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
        StorySceneDef scene = AssetDatabase.LoadAssetAtPath<StorySceneDef>(ScenePath);

        if (scene == null)
        {
            scene = ScriptableObject.CreateInstance<StorySceneDef>();
            AssetDatabase.CreateAsset(scene, ScenePath);
        }

        StoryActorDef[] actors =
        {
            new StoryActorDef("knight", "Knight", knight, false),
            new StoryActorDef("mage", "Mage", mage, false),
            new StoryActorDef("soldier", "Soldier", soldier, false),
            new StoryActorDef("slime", "Slime", slime, false)
        };

        StoryBeat[] beats =
        {
            new StoryBeat(0.5f, StoryVerb.Enter, "soldier", new Vector2(0.20f, 0.40f), 1.2f),
            new StoryBeat(1.0f, StoryVerb.Enter, "mage", new Vector2(0.31f, 0.33f), 1.2f),
            new StoryBeat(1.5f, StoryVerb.Enter, "knight", new Vector2(0.42f, 0.27f), 1.2f),
            new StoryBeat(4.0f, StoryVerb.Say, "soldier", default, 0f,
                "We've been walking for hours now. The markers are gone."),
            new StoryBeat(8.5f, StoryVerb.Say, "knight", default, 0f,
                "We must keep moving."),
            new StoryBeat(11.5f, StoryVerb.Bubble, "mage", default, 0f, string.Empty, BubbleKind.Question),
            new StoryBeat(12.0f, StoryVerb.Say, "mage", default, 0f,
                "Wait. Do you hear that?"),
            new StoryBeat(12.2f, StoryVerb.Face, "soldier", faceLeft: true),
            new StoryBeat(12.2f, StoryVerb.Face, "mage", faceLeft: true),
            new StoryBeat(12.2f, StoryVerb.Face, "knight", faceLeft: true),
            new StoryBeat(14.0f, StoryVerb.Bubble, "knight", default, 0f, string.Empty, BubbleKind.Bang),
            new StoryBeat(14.5f, StoryVerb.Say, "knight", default, 0f, "Move aside!"),
            new StoryBeat(16.0f, StoryVerb.WalkTo, "soldier", new Vector2(0.12f, 0.58f), 0.8f),
            new StoryBeat(16.2f, StoryVerb.WalkTo, "mage", new Vector2(0.34f, 0.58f), 0.8f),
            new StoryBeat(16.4f, StoryVerb.WalkTo, "knight", new Vector2(0.50f, 0.15f), 0.8f),
            new StoryBeat(17.8f, StoryVerb.Say, "soldier", default, 0f, "Weapons ready!"),
            new StoryBeat(18.0f, StoryVerb.PlayClip, "soldier", default, 0f, string.Empty, BubbleKind.None, "Attack01"),
            new StoryBeat(19.0f, StoryVerb.Bubble, "mage", default, 0f, string.Empty, BubbleKind.Bang),
            new StoryBeat(19.4f, StoryVerb.Say, "mage", default, 0f, "Wait!"),
            new StoryBeat(20.5f, StoryVerb.Enter, "slime", new Vector2(0.05f, 0.32f), 0.8f),
            new StoryBeat(21.3f, StoryVerb.WalkTo, "slime", new Vector2(0.72f, 0.32f), 3.5f),
            new StoryBeat(21.7f, StoryVerb.Face, "soldier"),
            new StoryBeat(22.9f, StoryVerb.Face, "mage"),
            new StoryBeat(23.8f, StoryVerb.Face, "knight"),
            new StoryBeat(26.0f, StoryVerb.Say, "soldier", default, 0f, "Is that a ... Slime!?"),
            new StoryBeat(29.5f, StoryVerb.Say, "knight", default, 0f, "It didn't notice us."),
            new StoryBeat(32.5f, StoryVerb.Say, "mage", default, 0f,
                "Let us follow it and see where it takes us."),
            new StoryBeat(36.0f, StoryVerb.WalkTo, "slime", new Vector2(0.86f, 0.32f), 2.0f),
            new StoryBeat(36.2f, StoryVerb.WalkTo, "soldier", new Vector2(0.30f, 0.40f), 1.8f),
            new StoryBeat(36.4f, StoryVerb.WalkTo, "mage", new Vector2(0.42f, 0.34f), 1.8f),
            new StoryBeat(36.6f, StoryVerb.WalkTo, "knight", new Vector2(0.56f, 0.28f), 1.8f),
            new StoryBeat(39.0f, StoryVerb.End)
        };

        scene.EditorSetContent("s01_intro", actors, beats, 120f, tapToContinue: true);

        EditorUtility.SetDirty(scene);
        AssetDatabase.SaveAssets();
        return scene;
    }

    private static StoryScheduleDef CreateSchedule(StorySceneDef scene)
    {
        StoryScheduleDef schedule = AssetDatabase.LoadAssetAtPath<StoryScheduleDef>(SchedulePath);

        if (schedule == null)
        {
            schedule = ScriptableObject.CreateInstance<StoryScheduleDef>();
            AssetDatabase.CreateAsset(schedule, SchedulePath);
        }

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
