using System;
using IdleRPG.Data;
using IdleRPG.Sim;

namespace IdleRPG.Combat
{
    /// <summary>
    /// Who stands where. The board comes from <see cref="FormationData"/>; this class only remembers which hero
    /// occupies which slot and answers the questions the sim and the UI ask.
    ///
    /// ELI5: the coach's clipboard. It does not decide the rules - the pitch does - it just records "Knight
    /// front-left, Archer back-left, nobody back-right" and can swap two names in one move.
    ///
    /// Pure C# and no Unity: it can be tested, saved and reasoned about without a scene. Swapping raises
    /// <see cref="Changed"/> so the save can be marked dirty.
    /// </summary>
    public sealed class Formation
    {
        /// <summary>-1 = empty slot.</summary>
        private readonly int[] heroBySlot;

        private readonly FormationData data;

        public Formation(FormationData data)
        {
            this.data = data != null ? data : throw new ArgumentNullException(nameof(data));

            heroBySlot = new int[this.data.SlotCount];
            for (int i = 0; i < heroBySlot.Length; i++)
            {
                heroBySlot[i] = -1;
            }
        }

        /// <summary>Raised after any successful placement or swap (the save listens to this).</summary>
        public event Action Changed;

        public FormationData Data => data;

        public int SlotCount => heroBySlot.Length;

        /// <summary>Hero occupying a slot, or -1 when empty. The value is the PartyConfig index.</summary>
        public int HeroAt(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < heroBySlot.Length ? heroBySlot[slotIndex] : -1;
        }

        /// <summary>Slot a hero occupies, or -1 when unplaced.</summary>
        public int SlotOfHero(int heroIndex)
        {
            for (int i = 0; i < heroBySlot.Length; i++)
            {
                if (heroBySlot[i] == heroIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        public CombatRow RowOfHero(int heroIndex)
        {
            int slot = SlotOfHero(heroIndex);
            return slot < 0 ? CombatRow.Front : data.RowForSlot(slot);
        }

        public int ColumnOfHero(int heroIndex)
        {
            int slot = SlotOfHero(heroIndex);
            return slot < 0 ? heroIndex : data.ColumnForSlot(slot);
        }

        /// <summary>
        /// The standard start: hero 0..N-1 into slot 0..N-1 (front row, left to right) - exactly the MVP's fixed
        /// lanes, so a fresh game behaves identically to before formation existed.
        /// </summary>
        public void PlaceInDefaultSlots(int heroCount, int highestStageReached = 1)
        {
            ClearSlots();

            int fielded = Math.Min(heroCount, data.MaxTeamSize(highestStageReached));
            int rooms = Math.Max(1, Math.Min(fielded, data.UnlockedSlotCount(highestStageReached)));

            for (int hero = 0; hero < fielded; hero++)
            {
                // Overflow (more heroes than rooms) stacks from slot 0 rather than dropping the hero.
                int slot = hero < rooms ? hero : hero % rooms;
                heroBySlot[slot] = hero;
            }

            Changed?.Invoke();
        }

        /// <summary>How many heroes may be fielded at a given progress point (team size, not board size).</summary>
        public int MaxTeamSize(int highestStageReached)
        {
            return data.MaxTeamSize(highestStageReached);
        }

        /// <summary>
        /// Moves a hero into a slot. Returns false when the slot is locked or out of range. If the target is
        /// occupied the two heroes trade places (a tap-swap is two placements).
        /// </summary>
        public bool TryMoveHero(int heroIndex, int targetSlot, int highestStageReached = int.MaxValue)
        {
            if (heroIndex < 0 || targetSlot < 0 || targetSlot >= heroBySlot.Length)
            {
                return false;
            }

            if (!data.IsSlotUnlocked(targetSlot, highestStageReached))
            {
                return false;
            }

            int fromSlot = SlotOfHero(heroIndex);
            int displaced = heroBySlot[targetSlot];

            if (fromSlot >= 0)
            {
                heroBySlot[fromSlot] = displaced;
            }

            heroBySlot[targetSlot] = heroIndex;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Swaps whatever is in two slots (either may be empty).</summary>
        public bool TrySwapSlots(int slotA, int slotB)
        {
            if (slotA < 0 || slotB < 0 || slotA >= heroBySlot.Length || slotB >= heroBySlot.Length || slotA == slotB)
            {
                return false;
            }

            int temp = heroBySlot[slotA];
            heroBySlot[slotA] = heroBySlot[slotB];
            heroBySlot[slotB] = temp;
            Changed?.Invoke();
            return true;
        }

        private void ClearSlots()
        {
            for (int i = 0; i < heroBySlot.Length; i++)
            {
                heroBySlot[i] = -1;
            }
        }

        /// <summary>
        /// Auto-arrange: the heaviest heroes move to the front row, ordered left to right.
        /// <paramref name="weight"/> is the sorting key - the caller passes "how much can this hero take".
        /// </summary>
        public void AutoArrange(int heroCount, Func<int, double> weight)
        {
            int[] order = new int[Math.Max(0, heroCount)];

            for (int hero = 0; hero < order.Length; hero++)
            {
                order[hero] = hero;
            }

            if (weight != null)
            {
                // Insertion sort: 3-6 heroes, clarity beats cleverness, and equal weights keep their order.
                for (int i = 1; i < order.Length; i++)
                {
                    int current = order[i];
                    double currentWeight = weight(current);
                    int j = i - 1;

                    while (j >= 0 && weight(order[j]) < currentWeight)
                    {
                        order[j + 1] = order[j];
                        j--;
                    }

                    order[j + 1] = current;
                }
            }

            ClearSlots();

            int rooms = Math.Max(1, heroBySlot.Length);

            for (int i = 0; i < order.Length; i++)
            {
                // Slot 0..N-1 in sorted order: heaviest first (front row), which is the tank's job.
                heroBySlot[i < rooms ? i : i % rooms] = order[i];
            }

            Changed?.Invoke();
        }

        /// <summary>Slot layout for saves and tooling ("-1" = empty slot).</summary>
        public int[] ToSlotArray()
        {
            int[] copy = new int[heroBySlot.Length];
            Array.Copy(heroBySlot, copy, heroBySlot.Length);
            return copy;
        }

        /// <summary>
        /// Restores a saved layout, ignoring anything out of range or duplicated, so a hand-edited save can never
        /// put two heroes in one slot.
        /// </summary>
        public void ApplySlotArray(int[] slots)
        {
            ClearSlots();

            if (slots == null)
            {
                Changed?.Invoke();
                return;
            }

            for (int i = 0; i < slots.Length && i < heroBySlot.Length; i++)
            {
                int hero = slots[i];

                if (hero < 0 || SlotOfHero(hero) >= 0)
                {
                    continue;
                }

                heroBySlot[i] = hero;
            }

            Changed?.Invoke();
        }

        /// <summary>Readable board for the dev overlay and logs: hero ids front-to-back, "x" = locked, "-" = empty.</summary>
        public string Describe()
        {
            string front = string.Empty;
            string back = string.Empty;

            for (int slot = 0; slot < data.SlotCount; slot++)
            {
                int hero = heroBySlot[slot];
                string label = hero >= 0 ? hero.ToString() : (data.IsSlotUnlocked(slot, int.MaxValue) ? "-" : "x");

                if (data.RowForSlot(slot) == CombatRow.Front)
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
    }
}
