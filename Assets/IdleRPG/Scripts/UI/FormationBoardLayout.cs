using UnityEngine;
using IdleRPG.Data;
using IdleRPG.Sim;

namespace IdleRPG.UI
{
    /// <summary>
    /// Where every board slot sits on screen. One function, used by the battle board and the Party board, so the
    /// two can never disagree about the layout.
    ///
    /// ELI5: the pitch markings. Ranks are **columns** (front rank nearest the enemy on the right, back rank to its
    /// left) and positions are **rows** stacked downwards - the board reads vertically, like a formation, not like
    /// a spreadsheet.
    /// </summary>
    public static class FormationBoardLayout
    {
        /// <summary>Column (left to right) for a rank: the back rank is left, the front rank is right.</summary>
        public static int ColumnOfRank(CombatRow row)
        {
            return row == CombatRow.Back ? 0 : 1;
        }

        /// <summary>Top-left anchored position of a slot, relative to the board's own top-left pivot.</summary>
        public static Vector2 PositionOf(int rank, int position, Vector2 slotSize, float columnGap, float rowGap)
        {
            float x = ColumnOfRank(rank == 1 ? CombatRow.Back : CombatRow.Front) * (slotSize.x + columnGap);
            float y = -position * (slotSize.y + rowGap);
            return new Vector2(x, y);
        }

        public static Vector2 PositionOfSlot(FormationData data, int slotIndex, Vector2 slotSize, float columnGap, float rowGap)
        {
            return PositionOf(data.RankOfSlot(slotIndex), data.PositionOfSlot(slotIndex), slotSize, columnGap, rowGap);
        }

        /// <summary>Board size for a given shape (so the host rect can be sized to fit).</summary>
        public static Vector2 SizeOf(FormationData data, Vector2 slotSize, float columnGap, float rowGap)
        {
            int columns = FormationData.RankCount;
            int rows = data.SlotsPerRank;

            return new Vector2(
                columns * slotSize.x + (columns - 1) * columnGap,
                rows * slotSize.y + (rows - 1) * rowGap);
        }
    }
}
