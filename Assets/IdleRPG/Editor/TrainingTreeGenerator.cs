using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using IdleRPG.Data;
using IdleRPG.Economy;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Generates the Training tree asset: one Gold chain per hero (Attack / Armor / Max HP) plus the sparse
    /// party track, which is paid with Perk Tokens and gated by the LOWEST hero's chain depth.
    /// Menu: Tools > Idle RPG > Generate Training Tree. Numbers are placeholders - tune later.
    /// </summary>
    public static class TrainingTreeGenerator
    {
        public const string TreePath = "Assets/IdleRPG/Data/Training/TrainingTree.asset";

        private const string PartyConfigPath = "Assets/IdleRPG/Data/Config/PartyConfig.asset";

        // 150 full cycles of Attack -> Armor -> Max HP = 450 nodes per hero.
        private const int AppearancesPerHero = 150;

        [MenuItem("Tools/Idle RPG/Generate Training Tree", priority = 20)]
        public static void Generate()
        {
            PartyConfig party = AssetDatabase.LoadAssetAtPath<PartyConfig>(PartyConfigPath);

            if (party == null || party.Heroes == null || party.ValidHeroCount == 0)
            {
                Debug.LogError($"[TrainingTreeGenerator] No party roster at {PartyConfigPath}; tree not generated.");
                return;
            }

            List<TrainingNode> nodes = new List<TrainingNode>();

            for (int i = 0; i < party.Heroes.Count; i++)
            {
                HeroData hero = party.Heroes[i];

                if (hero != null)
                {
                    AddHeroChain(nodes, hero);
                }
            }

            // PARTY track: paid with Perk Tokens, gated by the LOWEST hero LEFT count.
            AddParty(nodes, 0, 0, TrainingEffect.UnlockFirstAbility, 1d, 2d,
                "Active Skill Slot", "Unlocks the first active ability for all heroes.");
            AddParty(nodes, 1, 13, TrainingEffect.BattleGoldPercent, 0.10d, 4d,
                "Battle Gold +10%", "More gold from every battle.");
            AddParty(nodes, 2, 26, TrainingEffect.CampHealPercent, 25d, 6d,
                "Camp Healing +25", "Every camp heals the whole party for more.");
            AddParty(nodes, 3, 38, TrainingEffect.FinishGoldPercent, 0.15d, 8d,
                "Finish Gold +15%", "Bonus gold for finishing a level.");
            AddParty(nodes, 4, 44, TrainingEffect.CampExperience, 5d, 12d,
                "Camp Experience +5", "Placeholder - needs the experience system.");
            AddParty(nodes, 5, 50, TrainingEffect.LevelUpHealPercent, 0.15d, 18d,
                "Level-Up Heal 15%", "Placeholder - needs the level system.");
            AddParty(nodes, 6, 50, TrainingEffect.SurviveDeathShield, 10d, 30d,
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

        /// <summary>One hero's Gold chain: Attack, Armor, Max HP, repeating, +1 to the amount on every appearance.</summary>
        private static void AddHeroChain(List<TrainingNode> nodes, HeroData hero)
        {
            string heroId = hero.HeroID;
            int order = 0;

            for (int appearance = 0; appearance < AppearancesPerHero; appearance++)
            {
                for (int slot = 0; slot < 3; slot++)
                {
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
                        default:
                            effect = TrainingEffect.HpFlat;
                            amount = 20 + appearance;
                            title = "Max HP +" + (20 + appearance);
                            break;
                    }

                    TrainingNode node = new TrainingNode();
                    node.EditorSet($"{heroId}:L{order:000}", heroId, TrainingBranch.Left, effect, amount, 25 + order * 8,
                        CurrencyType.Gold, order, 0, title, $"{hero.HeroName}'s permanent gold track.");
                    nodes.Add(node);
                    order++;
                }
            }
        }

        private static void AddParty(List<TrainingNode> nodes, int index, int requiresLeft, TrainingEffect effect,
            double amount, double tokens, string title, string description)
        {
            TrainingNode node = new TrainingNode();
            node.EditorSet($"party:R{index}", string.Empty, TrainingBranch.Right, effect, amount, tokens,
                CurrencyType.PrestigeTokens, index, requiresLeft, title, description);
            nodes.Add(node);
        }
    }
}
