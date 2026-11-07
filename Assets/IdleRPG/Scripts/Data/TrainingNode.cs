using System;
using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>Which of the two vertical branches a training node sits on.</summary>
    public enum TrainingBranch
    {
        Left = 0,
        Right = 1
    }

    /// <summary>What a training node grants. Deferred effects need a system that does not exist yet.</summary>
    public enum TrainingEffect
    {
        AttackFlat = 0,
        ArmorFlat = 1,
        HpFlat = 2,
        CampHealPercent = 3,
        BattleGoldPercent = 4,
        FinishGoldPercent = 5,
        UnlockFirstAbility = 6,

        // Deferred until the matching system exists (greyed "coming soon").
        CampExperience = 7,
        LevelUpHealPercent = 8,
        SurviveDeathShield = 9
    }

    /// <summary>
    /// One node in the Training tree (plain serializable data, stored in a <see cref="TrainingTreeDef"/>).
    /// LEFT nodes are a linear chain (each needs the one below by <see cref="Order"/>); RIGHT nodes are gated by
    /// how far LEFT has climbed (<see cref="RequiresLeftNodes"/>).
    /// </summary>
    [Serializable]
    public sealed class TrainingNode
    {
        [SerializeField] private string nodeID = "";
        [SerializeField] private TrainingBranch branch = TrainingBranch.Left;
        [SerializeField] private TrainingEffect effect = TrainingEffect.AttackFlat;
        [Tooltip("Points granted (flat stat, percent as a fraction like 0.04, or a count).")]
        [SerializeField] private double amount = 1d;
        [SerializeField] private double costGold = 100d;
        [Tooltip("Index up this branch (LEFT: the node below it must be unlocked first).")]
        [SerializeField] private int order;
        [Tooltip("RIGHT nodes: how many LEFT nodes must be unlocked before this is available.")]
        [SerializeField] private int requiresLeftNodes;
        [SerializeField] private string title = "";
        [TextArea(1, 3)]
        [SerializeField] private string description = "";

        public string NodeID => string.IsNullOrEmpty(nodeID) ? title : nodeID;
        public TrainingBranch Branch => branch;
        public TrainingEffect Effect => effect;
        public double Amount => amount;
        public double CostGold => costGold < 0d ? 0d : costGold;
        public int Order => order;
        public int RequiresLeftNodes => requiresLeftNodes < 0 ? 0 : requiresLeftNodes;
        public string Title => title;
        public string Description => description;

        /// <summary>True when the effect needs a system that does not exist yet.</summary>
        public bool IsDeferred =>
            effect == TrainingEffect.CampExperience ||
            effect == TrainingEffect.LevelUpHealPercent ||
            effect == TrainingEffect.SurviveDeathShield;

#if UNITY_EDITOR
        /// <summary>Editor-only write used by the generator. Runtime never calls it.</summary>
        public void EditorSet(string id, TrainingBranch nodeBranch, TrainingEffect nodeEffect, double nodeAmount,
            double cost, int nodeOrder, int requiresLeft, string nodeTitle, string nodeDescription)
        {
            nodeID = id;
            branch = nodeBranch;
            effect = nodeEffect;
            amount = nodeAmount;
            costGold = cost;
            order = nodeOrder;
            requiresLeftNodes = requiresLeft;
            title = nodeTitle;
            description = nodeDescription;
        }
#endif
    }
}
