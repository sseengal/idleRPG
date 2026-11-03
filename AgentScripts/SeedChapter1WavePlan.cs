using System.IO;
using IdleRPG.Data;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-off authoring helper for the chapter 1 wave plan. Lives outside Assets/ so it never becomes a
/// project asset. Run from the pipeline: run_script AgentScripts/SeedChapter1WavePlan.cs SeedChapter1WavePlan.Run
/// </summary>
public static class SeedChapter1WavePlan
{
    private const string Folder = "Assets/IdleRPG/Data/Config";
    private const string PlanPath = Folder + "/Stage01_WavePlan.asset";
    private const string WaveConfigPath = Folder + "/WaveConfig.asset";

    public static void Run()
    {
        EnsureFolder(Folder);

        EnemyData bat = Load<EnemyData>("Assets/IdleRPG/Data/Enemies/Enemy_Bat.asset");
        EnemyData werewolf = Load<EnemyData>("Assets/IdleRPG/Data/Enemies/Enemy_Werewolf.asset");
        EnemyData slime = Load<EnemyData>("Assets/IdleRPG/Data/Enemies/Enemy_Slime.asset");
        WaveConfig config = Load<WaveConfig>(WaveConfigPath);

        if (bat == null || werewolf == null || slime == null || config == null)
        {
            Debug.LogError("[SeedChapter1WavePlan] Missing enemy or WaveConfig asset. Aborted.");
            return;
        }

        StageWavePlan plan = Load<StageWavePlan>(PlanPath);

        if (plan == null)
        {
            plan = ScriptableObject.CreateInstance<StageWavePlan>();
            AssetDatabase.CreateAsset(plan, PlanPath);
        }

        plan.EditorSetContent(1, 6,
            new StageWaveEntry(1, bat, 2),
            new StageWaveEntry(2, bat, 3),
            new StageWaveEntry(3, bat, 2),
            new StageWaveEntry(3, werewolf, 1),
            new StageWaveEntry(4, werewolf, 2),
            new StageWaveEntry(5, slime, 2),
            new StageWaveEntry(5, werewolf, 1),
            new StageWaveEntry(6, slime, 3));

        EditorUtility.SetDirty(plan);

        SerializedObject serializedConfig = new SerializedObject(config);
        SerializedProperty plans = serializedConfig.FindProperty("stageWavePlans");

        if (plans == null)
        {
            Debug.LogError("[SeedChapter1WavePlan] WaveConfig has no stageWavePlans field. Aborted.");
            return;
        }

        int planIndex = -1;

        for (int i = 0; i < plans.arraySize; i++)
        {
            if (plans.GetArrayElementAtIndex(i).objectReferenceValue == plan)
            {
                planIndex = i;
                break;
            }
        }

        if (planIndex < 0)
        {
            planIndex = plans.arraySize;
            plans.arraySize++;
        }

        plans.GetArrayElementAtIndex(planIndex).objectReferenceValue = plan;
        serializedConfig.ApplyModifiedProperties();

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SeedChapter1WavePlan] Wrote {PlanPath} and linked it to {WaveConfigPath}.");
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
