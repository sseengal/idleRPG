using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using IdleRPG.Data;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Generates the Training tree asset (75 LEFT stat nodes + the sparse RIGHT branch) from a curve here.
    /// Placeholder numbers - tune later. Menu: Tools > Idle RPG > Generate Training Tree.
    /// </summary>
    public static class TrainingTreeGenerator
    {
        public const string TreePath = "Assets/IdleRPG/Data/Training/TrainingTree.asset";
        // 75 visual levels x 8 nodes (two full sets per level).
        private const int LeftNodeCount = 600;

        [MenuItem("Tools/Idle RPG/Generate Training Tree", priority = 20)]
        public static void Generate()
        {
            List<TrainingNode> nodes = new List<TrainingNode>();

            // LEFT: dense linear chain cycling Attack -> Armor -> Max HP -> Camp Heal.
            for (int i = 0; i < LeftNodeCount; i++)
            {
                int slot = i % 4;
                int appearance = i / 4;   // 0..149 - one per time this kind appears; amount steps +1 each time
                TrainingEffect effect;
                double amount;
                string title;

                switch (slot)
                {
                    case 0:
                        effect = TrainingEffect.AttackFlat;
                        amount = 3 + appearance;
                        title = "Attack +" + (3 + appearance);
                        break;
                    case 1:
                        effect = TrainingEffect.ArmorFlat;
                        amount = 20 + appearance;
                        title = "Armor +" + (20 + appearance);
                        break;
                    case 2:
                        effect = TrainingEffect.HpFlat;
                        amount = 20 + appearance;
                        title = "Max HP +" + (20 + appearance);
                        break;
                    default:
                        effect = TrainingEffect.CampHealPercent;
                        amount = 1 + appearance;
                        title = "Camp Heal +" + (1 + appearance);
                        break;
                }

                TrainingNode node = new TrainingNode();
                node.EditorSet($"L{i:000}", TrainingBranch.Left, effect, amount, 25 + i * 8, i, 0, title,
                    "LEFT branch: spend gold to train a permanent stat.");
                nodes.Add(node);
            }

            // RIGHT: sparse big nodes, gated by how many LEFT nodes are unlocked.
            AddRight(nodes, 0, 0, TrainingEffect.UnlockFirstAbility, 1d, 1000d,
                "Active Skill Slot", "Unlocks the first active ability for all heroes.");
            AddRight(nodes, 1, 13, TrainingEffect.BattleGoldPercent, 0.10d, 2500d,
                "Battle Gold +10%", "More gold from every battle.");
            AddRight(nodes, 2, 26, TrainingEffect.FinishGoldPercent, 0.15d, 5000d,
                "Finish Gold +15%", "Bonus gold for finishing a level.");
            AddRight(nodes, 3, 38, TrainingEffect.CampExperience, 5d, 8000d,
                "Camp Experience +5", "Placeholder - needs the experience system.");
            AddRight(nodes, 4, 44, TrainingEffect.LevelUpHealPercent, 0.15d, 12000d,
                "Level-Up Heal 15%", "Placeholder - needs the level system.");
            AddRight(nodes, 5, 50, TrainingEffect.SurviveDeathShield, 10d, 20000d,
                "Survive Once + Shield", "Placeholder - survive a death once, gain shield stacks.");

            Directory.CreateDirectory(Path.GetDirectoryName(TreePath));

            TrainingTreeDef tree = AssetDatabase.LoadAssetAtPath<TrainingTreeDef>(TreePath);
            if (tree == null)
            {
                tree = ScriptableObject.CreateInstance<TrainingTreeDef>();
                AssetDatabase.CreateAsset(tree, TreePath);
            }

            tree.EditorSetNodes(nodes);
            EditorUtility.SetDirty(tree);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TrainingTreeGenerator] Built {nodes.Count} nodes into {TreePath}.");
        }

        private static void AddRight(List<TrainingNode> nodes, int index, int requiresLeft, TrainingEffect effect,
            double amount, double cost, string title, string description)
        {
            TrainingNode node = new TrainingNode();
            node.EditorSet($"R{index}", TrainingBranch.Right, effect, amount, cost, index, requiresLeft, title, description);
            nodes.Add(node);
        }
    }
}
