using System;
using System.Collections.Generic;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Save;

namespace IdleRPG.Progression
{
    /// <summary>
    /// The Training tree: which nodes the player has unlocked, the gold checkout, the branch gates, and the
    /// per-effect totals. LEFT nodes are a linear chain (each needs the one below); RIGHT nodes are gated by how far
    /// LEFT has climbed. Persisted as "train.&lt;id&gt;" keys in the save's keyed level list (like ability unlocks).
    /// </summary>
    public sealed class TrainingService
    {
        private const string KeyPrefix = "train.";

        private readonly EconomyManager economy;
        private readonly TrainingTreeDef tree;
        private readonly HashSet<string> unlocked = new HashSet<string>();
        private readonly Dictionary<int, TrainingNode> leftByOrder = new Dictionary<int, TrainingNode>();

        public TrainingService(EconomyManager economy, TrainingTreeDef tree)
        {
            this.economy = economy;
            this.tree = tree;
            BuildLeftIndex();
        }

        /// <summary>Raised when a node is unlocked or the service loads (UI refreshes).</summary>
        public event Action Changed;

        /// <summary>When the "Active Skill Slot" node unlocks, the game unlocks the first ability for all heroes.</summary>
        public Action FirstAbilityUnlockRequested;

        public TrainingTreeDef Tree => tree;

        public int UnlockedCount => unlocked.Count;

        /// <summary>How many LEFT nodes are unlocked (the RIGHT-branch gate).</summary>
        public int LeftUnlockedCount
        {
            get
            {
                int count = 0;
                IReadOnlyList<TrainingNode> nodes = tree != null ? tree.Nodes : Array.Empty<TrainingNode>();

                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].Branch == TrainingBranch.Left && unlocked.Contains(nodes[i].NodeID))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool IsUnlocked(TrainingNode node)
        {
            return node != null && unlocked.Contains(node.NodeID);
        }

        /// <summary>True when the node can be bought right now (gate satisfied, deferred effects excluded, gold enough).</summary>
        public bool CanUnlock(TrainingNode node)
        {
            if (node == null || IsUnlocked(node) || node.IsDeferred)
            {
                return false;
            }

            if (economy != null && !economy.CanAfford(CurrencyType.Gold, node.CostGold))
            {
                return false;
            }

            if (node.Branch == TrainingBranch.Left)
            {
                if (node.Order > 0)
                {
                    TrainingNode previous = FindLeft(node.Order - 1);
                    return previous != null && unlocked.Contains(previous.NodeID);
                }

                return true;
            }

            return LeftUnlockedCount >= node.RequiresLeftNodes;
        }

        /// <summary>Spends gold and unlocks the node. Returns false when it cannot be bought.</summary>
        public bool TryUnlock(TrainingNode node)
        {
            if (!CanUnlock(node))
            {
                return false;
            }

            if (economy != null && !economy.SpendGold(node.CostGold))
            {
                return false;
            }

            unlocked.Add(node.NodeID);

            if (node.Effect == TrainingEffect.UnlockFirstAbility)
            {
                FirstAbilityUnlockRequested?.Invoke();
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>Sum of every unlocked node's amount for one effect (flat stats + fractions).</summary>
        public double TotalFor(TrainingEffect effect)
        {
            double total = 0d;
            IReadOnlyList<TrainingNode> nodes = tree != null ? tree.Nodes : Array.Empty<TrainingNode>();

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Effect == effect && unlocked.Contains(nodes[i].NodeID))
                {
                    total += nodes[i].Amount;
                }
            }

            return total;
        }

        public void LoadFromSave(SaveData data)
        {
            unlocked.Clear();

            if (data != null && data.levels != null)
            {
                for (int i = 0; i < data.levels.Count; i++)
                {
                    LevelRecord record = data.levels[i];

                    if (record != null && record.level > 0 && !string.IsNullOrEmpty(record.key) &&
                        record.key.StartsWith(KeyPrefix, StringComparison.Ordinal))
                    {
                        string id = record.key.Substring(KeyPrefix.Length);
                        if (id.Length > 0)
                        {
                            unlocked.Add(id);
                        }
                    }
                }
            }

            Changed?.Invoke();
        }

        public void WriteToSave(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            foreach (string id in unlocked)
            {
                data.SetLevel(KeyPrefix + id, 1);
            }
        }

        private TrainingNode FindLeft(int order)
        {
            return leftByOrder.TryGetValue(order, out TrainingNode node) ? node : null;
        }

        private void BuildLeftIndex()
        {
            leftByOrder.Clear();

            if (tree == null)
            {
                return;
            }

            IReadOnlyList<TrainingNode> nodes = tree.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Branch == TrainingBranch.Left)
                {
                    leftByOrder[nodes[i].Order] = nodes[i];
                }
            }
        }
    }
}
