using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using IdleRPG.Data;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Writes the starter boon deck: ten cards plus the pool that deals them.
    ///
    /// ELI5: the pile of cards the level-up screen draws from. Running this menu item recreates the pile, so the
    /// numbers live in one place (this file) instead of being hand-typed into dozens of inspector fields.
    ///
    /// It is idempotent: existing assets are updated in place, never duplicated, so a re-run after a balance tweak
    /// keeps the same ids - and therefore the same saves.
    /// </summary>
    public static class BoonAssetGenerator
    {
        private const string Folder = "Assets/IdleRPG/Data/Boons";
        private const string PoolPath = Folder + "/PerkPool.asset";

        private const string Knight = "hero_knight";
        private const string Soldier = "hero_soldier";
        private const string Mage = "hero_mage";

        [MenuItem("Tools/Idle RPG/Generate Boon Assets", priority = 16)]
        public static void Generate()
        {
            EnsureFolder(Folder);

            List<PerkData> boons = new List<PerkData>
            {
                Boon("boon_sharp_blades", "Sharpened Blades", PerkRarity.Common, null, 5,
                    Effect(PerkEffectSpec.Op.AttackPercent, 0.12f)),
                Boon("boon_iron_skin", "Iron Skin", PerkRarity.Common, null, 5,
                    Effect(PerkEffectSpec.Op.HealthPercent, 0.12f)),
                Boon("boon_greedy_hands", "Greedy Hands", PerkRarity.Common, null, 5,
                    Effect(PerkEffectSpec.Op.GoldPercent, 0.10f)),
                Boon("boon_steady_hands", "Steady Hands", PerkRarity.Common, null, 5,
                    Effect(PerkEffectSpec.Op.CritChanceFlat, 0.04f)),
                Boon("boon_knight_wall", "Knight's Wall", PerkRarity.Rare, Knight, 3,
                    Effect(PerkEffectSpec.Op.DefensePercent, 0.25f)),
                Boon("boon_mage_focus", "Mage's Focus", PerkRarity.Rare, Mage, 3,
                    Effect(PerkEffectSpec.Op.CritChanceFlat, 0.12f)),
                Boon("boon_soldier_tempo", "Soldier's Tempo", PerkRarity.Rare, Soldier, 3,
                    Effect(PerkEffectSpec.Op.AttackSpeedPercent, 0.15f)),
                // Rarity rule (Docs/Boons.md 5): Common and Rare are numbers, Epic and Legendary add a mechanic.
                // So the big stat cards are RARE, and no Legendary ships until the trigger pass lands - a
                // numbers-only legendary would break the rule the game is played by.
                Boon("boon_brutal_strikes", "Brutal Strikes", PerkRarity.Rare, null, 3,
                    Effect(PerkEffectSpec.Op.CritDamageFlat, 0.22f)),
                Boon("boon_titans_blood", "Titan's Blood", PerkRarity.Rare, null, 3,
                    Effect(PerkEffectSpec.Op.HealthPercent, 0.22f))
            };

            WritePool(boons);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BoonAssetGenerator] Wrote {boons.Count} boons + the pool at {PoolPath}.");
        }

        private static (PerkEffectSpec.Op op, float amount) Effect(PerkEffectSpec.Op op, float amount)
        {
            return (op, amount);
        }

        // ------------------------------------------------------------------
        // Cards
        // ------------------------------------------------------------------
        private static PerkData Boon(string id, string displayName, PerkRarity rarity, string heroId,
            int maxStacks, params (PerkEffectSpec.Op op, float amount)[] effects)
        {
            string path = $"{Folder}/{id}.asset";
            PerkData boon = AssetDatabase.LoadAssetAtPath<PerkData>(path);

            if (boon == null)
            {
                boon = ScriptableObject.CreateInstance<PerkData>();
                AssetDatabase.CreateAsset(boon, path);
            }

            SerializedObject serialized = new SerializedObject(boon);
            serialized.FindProperty("boonID").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("description").stringValue = string.Empty;   // built from the effects
            serialized.FindProperty("rarity").enumValueIndex = (int)rarity;
            serialized.FindProperty("allHeroes").boolValue = string.IsNullOrEmpty(heroId);
            serialized.FindProperty("heroID").stringValue = heroId ?? string.Empty;
            serialized.FindProperty("maxStacks").intValue = maxStacks;

            SerializedProperty list = serialized.FindProperty("effects");
            list.arraySize = effects.Length;

            for (int i = 0; i < effects.Length; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("operation").enumValueIndex = (int)effects[i].op;
                element.FindPropertyRelative("amount").floatValue = effects[i].amount;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(boon);
            return boon;
        }

        // ------------------------------------------------------------------
        // Pool
        // ------------------------------------------------------------------
        private static void WritePool(List<PerkData> boons)
        {
            PerkPoolDef pool = AssetDatabase.LoadAssetAtPath<PerkPoolDef>(PoolPath);

            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<PerkPoolDef>();
                AssetDatabase.CreateAsset(pool, PoolPath);
            }

            SerializedObject serialized = new SerializedObject(pool);

            SerializedProperty list = serialized.FindProperty("boons");
            list.arraySize = boons.Count;

            for (int i = 0; i < boons.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = boons[i];
            }

            SerializedProperty weights = serialized.FindProperty("rarityWeights");
            weights.arraySize = 4;
            weights.GetArrayElementAtIndex(0).intValue = 65;   // common
            weights.GetArrayElementAtIndex(1).intValue = 25;   // rare
            weights.GetArrayElementAtIndex(2).intValue = 8;    // epic
            weights.GetArrayElementAtIndex(3).intValue = 2;    // legendary

            serialized.FindProperty("offerSize").intValue = 3;
            serialized.FindProperty("rerollsPerRun").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pool);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(folder);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
