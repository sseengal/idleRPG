using System;
using IdleRPG.Data;
using IdleRPG.Sim;

namespace IdleRPG.Combat
{
    /// <summary>
    /// Who stands where. The board shape and the row rule live in <see cref="FormationData"/>; this class only
    /// records the assignment and answers questions.
    ///
    /// ELI5: the coach's clipboard. "Knight and Archer in the front line, Mage behind them." One move operation,
    /// no rules of its own, no Unity - so it can be saved, logged and reasoned about on its own.
    /// </summary>
    public sealed class Formation
    {
        /// <summary>PartyConfig index per slot (rank-major). -1 = empty.</summary>
        private readonly int[] heroBySlot;

        private readonly FormationData data;

        public Formation(FormationData data)
        {
            this.data = data != null ? data : throw new ArgumentNullException(nameof(data));

            heroBySlot = new int[this.data.SlotCount];
            ClearSlots();
        }

        /// <summary>Raised after any change (the save marks itself dirty, the fight re-stamps its rows).</summary>
        public event Action Changed;

        public FormationData Data => data;

        public int SlotCount => heroBySlot.Length;

        /// <summary>Hero occupying a slot, or -1 when empty.</summary>
        public int HeroAt(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < heroBySlot.Length ? heroBySlot[slotIndex] : -1;
        }

        /// <summary>Slot a hero occupies, or -1 when the hero is not on the board.</summary>
        public int SlotOfHero(int heroIndex)
        {
            for (int slot = 0; slot < heroBySlot.Length; slot++)
            {
                if (heroBySlot[slot] == heroIndex)
                {
                    return slot;
                }
            }

            return -1;
        }

        public CombatRow RankOfHero(int heroIndex)
        {
            int slot = SlotOfHero(heroIndex);
            return slot < 0 ? CombatRow.Front : data.RowOfSlot(slot);
        }

        /// <summary>Vertical position of a hero inside its rank (0 = closest to the enemy).</summary>
        public int PositionOfHero(int heroIndex)
        {
            int slot = SlotOfHero(heroIndex);
            return slot < 0 ? heroIndex : data.PositionOfSlot(slot);
        }

        public int FrontCount
        {
            get
            {
                int count = 0;
                for (int slot = 0; slot < heroBySlot.Length; slot++)
                {
                    if (heroBySlot[slot] >= 0 && data.RowOfSlot(slot) == CombatRow.Front)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int BackCount => PlacedCount - FrontCount;

        public int PlacedCount
        {
            get
            {
                int count = 0;
                for (int slot = 0; slot < heroBySlot.Length; slot++)
                {
                    if (heroBySlot[slot] >= 0)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// The one move operation: put a hero in a slot. An empty slot simply receives the hero; an occupied slot
        /// swaps the two, so the source slot is always vacated. Returns false only for out-of-range input.
        /// </summary>
        public bool TryMove(int heroIndex, int targetSlot)
        {
            if (heroIndex < 0 || targetSlot < 0 || targetSlot >= heroBySlot.Length || !data.IsSlotInUse(targetSlot))
            {
                return false;
            }

            int fromSlot = SlotOfHero(heroIndex);

            if (fromSlot == targetSlot)
            {
                return false;
            }

            int displaced = heroBySlot[targetSlot];

            if (fromSlot >= 0)
            {
                heroBySlot[fromSlot] = displaced;   // -1 when the target was empty: the old slot is vacated
            }

            heroBySlot[targetSlot] = heroIndex;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Default layout: hero 0, 1, 2... fill the front rank first, then the back rank. This is exactly the
        /// MVP's fixed lanes, so a fresh run behaves as it always did until the player moves somebody.
        /// </summary>
        public void PlaceInDefaultSlots(int heroCount)
        {
            ClearSlots();

            int slots = Math.Min(Math.Max(0, heroCount), heroBySlot.Length);

            for (int hero = 0; hero < slots; hero++)
            {
                heroBySlot[DefaultSlotFor(hero)] = hero;
            }

            Changed?.Invoke();
        }

        /// <summary>Front rank in order, then back rank in order (purely to make the default layout obvious).</summary>
        private int DefaultSlotFor(int order)
        {
            if (order < data.FrontSlots)
            {
                return order;
            }

            return data.SlotsPerRank + (order - data.FrontSlots);
        }

        /// <summary>Slot layout for saves and tooling ("-1" = empty).</summary>
        public int[] ToSlotArray()
        {
            int[] copy = new int[heroBySlot.Length];
            Array.Copy(heroBySlot, copy, heroBySlot.Length);
            return copy;
        }

        /// <summary>
        /// Restores a saved layout. Out-of-range, out-of-shape and duplicated entries are ignored, so a hand-edited
        /// save can never put two heroes in one slot or drop one off the board.
        /// </summary>
        public void ApplySlotArray(int[] slots, int heroCount)
        {
            int[] previous = ToSlotArray();
            ClearSlots();

            if (slots != null)
            {
                for (int slot = 0; slot < slots.Length && slot < heroBySlot.Length; slot++)
                {
                    int hero = slots[slot];

                    if (hero < 0 || hero >= heroCount || !data.IsSlotInUse(slot) || SlotOfHero(hero) >= 0)
                    {
                        continue;
                    }

                    heroBySlot[slot] = hero;
                }
            }

            // Any hero the save did not place (new hero, older save) lands in the free front-most slot.
            for (int hero = 0; hero < heroCount; hero++)
            {
                if (SlotOfHero(hero) >= 0)
                {
                    continue;
                }

                int free = FirstFreeSlot();

                if (free >= 0)
                {
                    heroBySlot[free] = hero;
                }
            }

            if (!SameLayout(previous, heroBySlot))
            {
                Changed?.Invoke();
            }
        }

        private int FirstFreeSlot()
        {
            for (int slot = 0; slot < heroBySlot.Length; slot++)
            {
                if (heroBySlot[slot] < 0 && data.IsSlotInUse(slot))
                {
                    return slot;
                }
            }

            return -1;
        }

        private static bool SameLayout(int[] a, int[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>One-line board dump for logs and the dev overlay: "front[0 2] back[1]".</summary>
        public string Describe()
        {
            string front = string.Empty;
            string back = string.Empty;

            for (int slot = 0; slot < heroBySlot.Length; slot++)
            {
                if (!data.IsSlotInUse(slot))
                {
                    continue;
                }

                int hero = heroBySlot[slot];
                string label = hero >= 0 ? hero.ToString() : "-";

                if (data.RowOfSlot(slot) == CombatRow.Front)
                {
                    front += label + " ";
                }
                else
                {
                    back += label + " ";
                }
            }

            return $"front[{front.TrimEnd()}] back[{back.TrimEnd()}]";
        }

        private void ClearSlots()
        {
            for (int slot = 0; slot < heroBySlot.Length; slot++)
            {
                heroBySlot[slot] = -1;
            }
        }
    }
}
