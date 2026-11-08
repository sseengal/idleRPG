using System;
using System.Collections.Generic;
using IdleRPG.Data;
using IdleRPG.Save;

namespace IdleRPG.Progression
{
    /// <summary>
    /// The boons picked this run, and the totals they add up to.
    ///
    /// ELI5: the cards in your hand. Each one gives a small bonus; this class adds them up so the shop of stats
    /// never has to care how many cards there are. Everything clears when the run ends.
    ///
    /// Why a separate class: the numbers have to be readable by the stat resolver (per hero), by the card UI (to
    /// avoid offering a maxed card) and by the save. One owner, three readers.
    /// </summary>
    public sealed class PerkLoadout
    {
        private readonly List<PerkData> chosen = new List<PerkData>();
        private readonly Dictionary<string, int> stacks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Raised whenever the hand changes, so the stats and the UI can refresh.</summary>
        public event Action Changed;

        /// <summary>Every boon taken this run, in pick order (duplicates repeat).</summary>
        public IReadOnlyList<PerkData> Chosen => chosen;

        public int Count => chosen.Count;

        /// <summary>How many copies of a boon this run already holds.</summary>
        public int StacksOf(PerkData boon)
        {
            if (boon == null)
            {
                return 0;
            }

            return stacks.TryGetValue(boon.BoonID, out int count) ? count : 0;
        }

        /// <summary>True when the card is at its run limit and must not be dealt again.</summary>
        public bool IsMaxed(PerkData boon)
        {
            if (boon == null)
            {
                return true;
            }

            int limit = boon.MaxStacks;

            return limit > 0 && StacksOf(boon) >= limit;
        }

        /// <summary>Adds one card. False when it is maxed (the offer service should have filtered it).</summary>
        public bool Add(PerkData boon)
        {
            if (boon == null || IsMaxed(boon))
            {
                return false;
            }

            chosen.Add(boon);
            stacks[boon.BoonID] = StacksOf(boon) + 1;
            Changed?.Invoke();
            return true;
        }

        public void ResetForNewRun()
        {
            if (chosen.Count == 0 && stacks.Count == 0)
            {
                return;
            }

            chosen.Clear();
            stacks.Clear();
            Changed?.Invoke();
        }

        // ------------------------------------------------------------------
        // Totals (already summed; the resolver turns them into multipliers)
        // ------------------------------------------------------------------
        /// <summary>Total of one op for one hero: party-wide cards plus cards aimed at that hero.</summary>
        public double TotalFor(PerkEffectSpec.Op operation, string heroId)
        {
            double total = 0d;

            for (int i = 0; i < chosen.Count; i++)
            {
                PerkData boon = chosen[i];

                if (boon != null && boon.AppliesTo(heroId))
                {
                    total += boon.Sum(operation);
                }
            }

            return total;
        }

        /// <summary>Party-wide gold bonus (gold is not per hero, so only cards that help everyone count).</summary>
        public double GoldBonus
        {
            get
            {
                double total = 0d;

                for (int i = 0; i < chosen.Count; i++)
                {
                    PerkData boon = chosen[i];

                    if (boon != null && boon.AllHeroes)
                    {
                        total += boon.Sum(PerkEffectSpec.Op.GoldPercent);
                    }
                }

                return total;
            }
        }

        // ------------------------------------------------------------------
        // Save contract: ids only, so rebalanced numbers apply on load
        // ------------------------------------------------------------------
        public void WriteToSave(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            data.runBoonIds = new List<string>(chosen.Count);

            for (int i = 0; i < chosen.Count; i++)
            {
                if (chosen[i] != null)
                {
                    data.runBoonIds.Add(chosen[i].BoonID);
                }
            }
        }

        /// <summary>Rebuilds the hand from saved ids. Unknown ids (content removed) are skipped, never fatal.</summary>
        public void LoadFromSave(SaveData data, PerkPoolDef pool)
        {
            chosen.Clear();
            stacks.Clear();

            if (data != null && data.runBoonIds != null && pool != null)
            {
                for (int i = 0; i < data.runBoonIds.Count; i++)
                {
                    PerkData boon = pool.Find(data.runBoonIds[i]);

                    if (boon == null)
                    {
                        continue;
                    }

                    chosen.Add(boon);
                    stacks[boon.BoonID] = StacksOf(boon) + 1;
                }
            }

            Changed?.Invoke();
        }
    }
}
