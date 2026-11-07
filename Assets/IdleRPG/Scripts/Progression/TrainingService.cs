using System;
using System.Collections.Generic;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Save;

namespace IdleRPG.Progression
{
    /// <summary>
    /// The Training tree: one Gold chain PER HERO (Attack / Armor / Max HP) and a party track paid with Perk Tokens
    /// and gated by the LOWEST hero's chain depth. Owns unlocks, the single currency checkout, the branch gates and
    /// the per-effect totals. Persisted as "train.&lt;nodeID&gt;" keys in the save's keyed level list.
    /// </summary>
    public sealed class TrainingService
    {
        private const string KeyPrefix = "train.";

        private readonly EconomyManager economy;
        private readonly TrainingTreeDef tree;
        private readonly HashSet<string> unlocked = new HashSet<string>();

        /// <summary>Every node id in the tree; used to drop stale save keys from older tree shapes.</summary>
        private readonly HashSet<string> allNodeIds = new HashSet<string>();

        /// <summary>heroId -> (order -> node) for the LEFT chains.</summary>
        private readonly Dictionary<string, Dictionary<int, TrainingNode>> leftByHero =
            new Dictionary<string, Dictionary<int, TrainingNode>>();

        /// <summary>Hero ids in tree order (Knight, Mage, Soldier...).</summary>
        private readonly List<string> heroIds = new List<string>();

        public TrainingService(EconomyManager economy, TrainingTreeDef tree)
        {
            this.economy = economy;
            this.tree = tree;
            BuildIndexes();
        }

        /// <summary>Raised when a node is unlocked or the service loads (UI refreshes).</summary>
        public event Action Changed;

        /// <summary>When the "Active Skill Slot" node unlocks, the game unlocks the first ability for all heroes.</summary>
        public Action FirstAbilityUnlockRequested;

        public TrainingTreeDef Tree => tree;

        public int UnlockedCount => unlocked.Count;

        /// <summary>Hero ids that own a LEFT chain, in tree order.</summary>
        public IReadOnlyList<string> HeroIds => heroIds;

        // ------------------------------------------------------------------
        // Gates and totals
        // ------------------------------------------------------------------
        /// <summary>How many LEFT nodes this hero has unlocked.</summary>
        public int LeftUnlockedCount(string heroId)
        {
            int count = 0;
            Dictionary<int, TrainingNode> chain = GetChain(heroId);

            if (chain == null)
            {
                return 0;
            }

            foreach (TrainingNode node in chain.Values)
            {
                if (node != null && unlocked.Contains(node.NodeID))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Every hero's LEFT nodes added together (all columns).</summary>
        public int TotalLeftUnlockedCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < heroIds.Count; i++)
                {
                    count += LeftUnlockedCount(heroIds[i]);
                }

                return count;
            }
        }

        /// <summary>The weakest hero's LEFT count - the party track gate.</summary>
        public int LowestLeftCount
        {
            get
            {
                int lowest = int.MaxValue;

                for (int i = 0; i < heroIds.Count; i++)
                {
                    int count = LeftUnlockedCount(heroIds[i]);

                    if (count < lowest)
                    {
                        lowest = count;
                    }
                }

                return lowest == int.MaxValue ? 0 : lowest;
            }
        }

        /// <summary>The hero currently holding the party track back (lowest LEFT count). False when there are none.</summary>
        public bool TryGetLaggard(out string heroId, out int count)
        {
            heroId = string.Empty;
            count = 0;
            int lowest = int.MaxValue;

            for (int i = 0; i < heroIds.Count; i++)
            {
                int current = LeftUnlockedCount(heroIds[i]);

                if (current < lowest)
                {
                    lowest = current;
                    heroId = heroIds[i];
                    count = current;
                }
            }

            return heroId.Length > 0;
        }

        public bool IsUnlocked(TrainingNode node)
        {
            return node != null && unlocked.Contains(node.NodeID);
        }

        /// <summary>True when the branch gate is satisfied (ignores currency and owned state).</summary>
        public bool IsGateMet(TrainingNode node)
        {
            if (node == null)
            {
                return false;
            }

            if (node.Branch == TrainingBranch.Right)
            {
                return LowestLeftCount >= node.RequiresLeftNodes;
            }

            if (node.Order <= 0)
            {
                return true;
            }

            TrainingNode previous = FindLeft(node.HeroID, node.Order - 1);
            return previous != null && unlocked.Contains(previous.NodeID);
        }

        /// <summary>True when the player holds enough of the node's own currency.</summary>
        public bool CanAffordNode(TrainingNode node)
        {
            return node != null && (economy == null || economy.CanAfford(node.CostCurrency, node.Cost));
        }

        /// <summary>True when the node can be bought right now (gate + currency + not owned + not deferred).</summary>
        public bool CanUnlock(TrainingNode node)
        {
            if (node == null || IsUnlocked(node) || node.IsDeferred)
            {
                return false;
            }

            return IsGateMet(node) && CanAffordNode(node);
        }

        /// <summary>Spends the node's currency and unlocks it. Returns false when it cannot be bought.</summary>
        public bool TryUnlock(TrainingNode node)
        {
            if (!CanUnlock(node))
            {
                return false;
            }

            if (economy != null && !economy.SpendCurrency(node.CostCurrency, node.Cost))
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

        /// <summary>Sum of the PARTY track's unlocked amounts for one effect (money, camp heal, ability slot).</summary>
        public double TotalFor(TrainingEffect effect)
        {
            IReadOnlyList<TrainingNode> nodes = tree != null ? tree.Nodes : Array.Empty<TrainingNode>();
            double total = 0d;

            for (int i = 0; i < nodes.Count; i++)
            {
                TrainingNode node = nodes[i];

                if (node != null && node.IsPartyNode && node.Effect == effect && unlocked.Contains(node.NodeID))
                {
                    total += node.Amount;
                }
            }

            return total;
        }

        /// <summary>Sum of ONE hero's unlocked amounts for one effect (their flat stats).</summary>
        public double TotalFor(TrainingEffect effect, string heroId)
        {
            IReadOnlyList<TrainingNode> nodes = tree != null ? tree.Nodes : Array.Empty<TrainingNode>();
            double total = 0d;

            if (string.IsNullOrEmpty(heroId))
            {
                return 0d;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                TrainingNode node = nodes[i];

                if (node != null && node.HeroID == heroId && node.Effect == effect && unlocked.Contains(node.NodeID))
                {
                    total += node.Amount;
                }
            }

            return total;
        }

        // ------------------------------------------------------------------
        // Save
        // ------------------------------------------------------------------
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
                        if (id.Length > 0 && allNodeIds.Contains(id))
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

        // ------------------------------------------------------------------
        // Indexes
        // ------------------------------------------------------------------
        private Dictionary<int, TrainingNode> GetChain(string heroId)
        {
            return !string.IsNullOrEmpty(heroId) && leftByHero.TryGetValue(heroId, out Dictionary<int, TrainingNode> chain)
                ? chain
                : null;
        }

        private TrainingNode FindLeft(string heroId, int order)
        {
            Dictionary<int, TrainingNode> chain = GetChain(heroId);
            return chain != null && chain.TryGetValue(order, out TrainingNode node) ? node : null;
        }

        private void BuildIndexes()
        {
            leftByHero.Clear();
            heroIds.Clear();
            allNodeIds.Clear();

            if (tree == null)
            {
                return;
            }

            IReadOnlyList<TrainingNode> nodes = tree.Nodes;

            for (int i = 0; i < nodes.Count; i++)
            {
                TrainingNode node = nodes[i];

                if (node == null)
                {
                    continue;
                }

                allNodeIds.Add(node.NodeID);

                if (node.Branch != TrainingBranch.Left || node.IsPartyNode)
                {
                    continue;
                }

                if (!leftByHero.TryGetValue(node.HeroID, out Dictionary<int, TrainingNode> chain))
                {
                    chain = new Dictionary<int, TrainingNode>();
                    leftByHero[node.HeroID] = chain;
                    heroIds.Add(node.HeroID);
                }

                chain[node.Order] = node;
            }
        }
    }
}
