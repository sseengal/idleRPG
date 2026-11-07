using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// The whole Training tree: two branches of <see cref="TrainingNode"/> data. Adding content = editing this one
    /// asset (or re-running the generator). No code change to add a node.
    /// </summary>
    [CreateAssetMenu(fileName = "TrainingTree", menuName = "Idle RPG/Data/Training Tree", order = 30)]
    public sealed class TrainingTreeDef : ScriptableObject
    {
        [SerializeField] private List<TrainingNode> nodes = new List<TrainingNode>();

        public IReadOnlyList<TrainingNode> Nodes =>
            nodes != null ? (IReadOnlyList<TrainingNode>)nodes : Array.Empty<TrainingNode>();

        public int Count => nodes == null ? 0 : nodes.Count;

        /// <summary>The node with this id, or null.</summary>
        public TrainingNode Find(string nodeId)
        {
            if (nodes == null || string.IsNullOrEmpty(nodeId))
            {
                return null;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].NodeID == nodeId)
                {
                    return nodes[i];
                }
            }

            return null;
        }

#if UNITY_EDITOR
        /// <summary>Editor-only write used by the generator.</summary>
        public void EditorSetNodes(List<TrainingNode> newNodes)
        {
            nodes = newNodes ?? new List<TrainingNode>();
        }
#endif
    }
}
