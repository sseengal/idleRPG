using UnityEngine;
using IdleRPG.Sim;

namespace IdleRPG.Data
{
    /// <summary>
    /// The party's battle formation: how many rows/columns exist, when each slot unlocks, and the row rules.
    ///
    /// ELI5: a football pitch painted on the ground. The pitch says how many spots there are, which spots you
    /// are allowed to use yet, and what happens to a player standing at the back (they take less of a beating,
    /// as long as somebody is still standing at the front to block for them).
    ///
    /// Everything here is **data**: the sim reads these numbers, it never hardcodes a row count or a damage
    /// multiplier. Balance can be changed without touching code, which is the same rule as heroes and enemies.
    /// </summary>
    [CreateAssetMenu(fileName = "FormationData", menuName = "Idle RPG/Data/Formation", order = 16)]
    public class FormationData : ScriptableObject
    {
        public const int MaxRows = 2;
        public const int MaxColumns = 3;

        [Header("Layout")]
        [Tooltip("1 or 2. Row 0 is the front row (takes the hits); row 1 is the back row.")]
        [SerializeField] private int rows = 2;

        [Tooltip("Columns per row: how many heroes can stand side by side.")]
        [SerializeField] private int columns = 2;

        [Header("Unlocks")]
        [Tooltip("Highest stage needed for each board slot to be usable, in slot order (slot 0 = front-left). " +
                 "Default: both rows are painted from the start and only the last spot is gated. Board slots and " +
                 "team size are different rules - see teamSizeUnlockStages.")]
        [SerializeField] private int[] slotUnlockStages = { 1, 1, 1, 1, 1, 9999 };

        [Tooltip("How many heroes may stand on the board, per unlock. Default: 3 at start, 4th at stage 21 " +
                 "(zone 2), 5th at stage 41 (zone 4) - the Progression.md team-size rule.")]
        [SerializeField] private int[] teamSizeUnlockStages = { 1, 1, 1, 21, 41 };

        [Header("Row rules")]
        [Tooltip("Damage a back-row hero takes while its front row still has a living member (design: 0.75).")]
        [Range(0f, 1f)]
        [SerializeField] private float backRowDamageTakenMultiplier = 0.75f;

        [Tooltip("Off = rows are cosmetic and position carries no protection (useful for A/B testing).")]
        [SerializeField] private bool frontRowProtectsBackRow = true;

        public int Rows => Mathf.Clamp(rows, 1, MaxRows);

        public int Columns => Mathf.Clamp(columns, 1, MaxColumns);

        /// <summary>Total slots on the board (rows x columns).</summary>
        public int SlotCount => Rows * Columns;

        public float BackRowDamageTakenMultiplier => Mathf.Clamp(backRowDamageTakenMultiplier, 0f, 1f);

        public bool FrontRowProtectsBackRow => frontRowProtectsBackRow;

        /// <summary>Slots unlocked at a given progress point (highest stage reached).</summary>
        public int UnlockedSlotCount(int highestStageReached)
        {
            int safeStage = Mathf.Max(1, highestStageReached);

            if (slotUnlockStages == null || slotUnlockStages.Length == 0)
            {
                return SlotCount;
            }

            int unlocked = 0;

            for (int i = 0; i < slotUnlockStages.Length && i < SlotCount; i++)
            {
                if (slotUnlockStages[i] <= safeStage)
                {
                    unlocked++;
                }
            }

            return unlocked;
        }

        public bool IsSlotUnlocked(int slotIndex, int highestStageReached)
        {
            return slotIndex >= 0 && slotIndex < UnlockedSlotCount(highestStageReached);
        }

        /// <summary>
        /// How many heroes may be fielded at a given progress point (3 at the start, 4 after zone 2, 5 after
        /// zone 4 by default). This is the *team size* rule and it is separate from which board slots exist, so a
        /// player with three heroes can already choose to stand one of them in the back row on day one.
        /// </summary>
        public int MaxTeamSize(int highestStageReached)
        {
            int safeStage = Mathf.Max(1, highestStageReached);

            if (teamSizeUnlockStages == null || teamSizeUnlockStages.Length == 0)
            {
                return SlotCount;
            }

            int allowed = 0;

            for (int i = 0; i < teamSizeUnlockStages.Length; i++)
            {
                if (teamSizeUnlockStages[i] <= safeStage)
                {
                    allowed++;
                }
            }

            return Mathf.Clamp(allowed, 0, SlotCount);
        }

        /// <summary>Row a slot belongs to. Slots are laid out row-major: 0..Columns-1 = front row.</summary>
        public CombatRow RowForSlot(int slotIndex)
        {
            return RowIndexForSlot(slotIndex) == 0 ? CombatRow.Front : CombatRow.Back;
        }

        public int RowIndexForSlot(int slotIndex)
        {
            if (slotIndex < 0)
            {
                return 0;
            }

            return Mathf.Clamp(slotIndex / Columns, 0, Rows - 1);
        }

        public int ColumnForSlot(int slotIndex)
        {
            if (slotIndex < 0)
            {
                return 0;
            }

            return Mathf.Clamp(slotIndex % Columns, 0, Columns - 1);
        }

        /// <summary>First slot index of a row (used by auto-arrange and the UI board).</summary>
        public int FirstSlotOfRow(int rowIndex)
        {
            return Mathf.Clamp(rowIndex, 0, Rows - 1) * Columns;
        }

        private void OnValidate()
        {
            rows = Mathf.Clamp(rows, 1, MaxRows);
            columns = Mathf.Clamp(columns, 1, MaxColumns);

            if (slotUnlockStages == null || slotUnlockStages.Length == 0)
            {
                slotUnlockStages = new[] { 1 };
            }

            if (teamSizeUnlockStages == null || teamSizeUnlockStages.Length == 0)
            {
                teamSizeUnlockStages = new[] { 1 };
            }

            for (int i = 0; i < slotUnlockStages.Length; i++)
            {
                slotUnlockStages[i] = Mathf.Max(1, slotUnlockStages[i]);
            }
        }
    }
}
