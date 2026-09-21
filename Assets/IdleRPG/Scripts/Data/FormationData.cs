using UnityEngine;
using IdleRPG.Sim;

namespace IdleRPG.Data
{
    /// <summary>
    /// The party's battle formation: how many heroes stand in each rank and how attractive the back rank is.
    ///
    /// ELI5: two lines of defence. The front rank is what enemies aim at; the back rank stands behind it, so it
    /// gets picked *less often* - not hit softer (a landed hit always does full damage). New heroes always start in
    /// the front rank, so a fresh game plays exactly as it did before formation existed.
    ///
    /// Deliberately tiny: the board is a fixed two-by-three, every slot is usable, and the numbers live in data so
    /// balance never touches code. Team size (3 -> 4 -> 5) is a *roster* rule owned by Step 17, not a board rule.
    /// </summary>
    [CreateAssetMenu(fileName = "FormationData", menuName = "Idle RPG/Data/Formation", order = 16)]
    public class FormationData : ScriptableObject
    {
        public const int RankCount = 2;
        public const int MaxSlotsPerRank = 4;

        [Header("Board")]
        [Tooltip("Heroes per rank. Rank 0 = front, rank 1 = back. Every slot is available from the start.")]
        [SerializeField] private int frontSlots = 3;

        [SerializeField] private int backSlots = 3;

        [Header("Row rule")]
        [Tooltip("How attractive the back rank is when an attacker picks a target (front rank = 1). 0.35 means " +
                 "roughly one swing in four goes to the back rank. 0 = strict front rank.")]
        [Range(0f, 1f)]
        [SerializeField] private float backRowTargetWeight = 0.35f;

        public int FrontSlots => Mathf.Clamp(frontSlots, 1, MaxSlotsPerRank);

        public int BackSlots => Mathf.Clamp(backSlots, 1, MaxSlotsPerRank);

        /// <summary>Slots per rank when reading/writing an index (both ranks share one width).</summary>
        public int SlotsPerRank => Mathf.Max(FrontSlots, BackSlots);

        public int SlotCount => SlotsPerRank * RankCount;

        /// <summary>How attractive the back rank is relative to the front rank (1 = equal odds).</summary>
        public float BackRowTargetWeight => Mathf.Clamp01(backRowTargetWeight);

        /// <summary>Rank of a slot index: 0 = front, 1 = back, -1 = out of range.</summary>
        public int RankOfSlot(int slotIndex)
        {
            return slotIndex < 0 || slotIndex >= SlotCount ? -1 : slotIndex / SlotsPerRank;
        }

        /// <summary>Vertical position of a slot within its rank (0 = closest to the enemy).</summary>
        public int PositionOfSlot(int slotIndex)
        {
            return slotIndex < 0 || slotIndex >= SlotCount ? -1 : slotIndex % SlotsPerRank;
        }

        public CombatRow RowOfSlot(int slotIndex)
        {
            return RankOfSlot(slotIndex) == 1 ? CombatRow.Back : CombatRow.Front;
        }

        public int SlotIndexOf(CombatRow row, int position)
        {
            int rank = row == CombatRow.Back ? 1 : 0;
            return rank * SlotsPerRank + Mathf.Clamp(position, 0, SlotsPerRank - 1);
        }

        /// <summary>True when this slot belongs to a rank that has that many heroes' worth of room.</summary>
        public bool IsSlotInUse(int slotIndex)
        {
            int rank = RankOfSlot(slotIndex);

            if (rank < 0)
            {
                return false;
            }

            int position = PositionOfSlot(slotIndex);
            return position < (rank == 0 ? FrontSlots : BackSlots);
        }

        private void OnValidate()
        {
            frontSlots = Mathf.Clamp(frontSlots, 1, MaxSlotsPerRank);
            backSlots = Mathf.Clamp(backSlots, 1, MaxSlotsPerRank);
            backRowTargetWeight = Mathf.Clamp01(backRowTargetWeight);
        }
    }
}
