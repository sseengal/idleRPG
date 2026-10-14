using UnityEngine;

namespace IdleRPG.UI
{
    /// <summary>
    /// The game's spacing, in one file: the gap ladder plus the roles a layout asks for.
    ///
    /// ELI5: the ruler. One short list of gap sizes. A screen says WHAT a gap is for ("the screen edge", "between
    /// rows") and never how many units.
    ///
    /// Why it exists: text sizes live in <see cref="UiType"/> and finger sizes in <see cref="UiTouch"/>, but gaps
    /// were picked by hand in about 60 places - eight different numbers (0, 2, 4, 6, 8, 10, 12, 14). Two panels
    /// doing the same job padded 14/6 and 10/10, so nothing lined up. This is the fix: pick a role, never a number.
    ///
    /// Units are canvas units at the 1080x1920 reference - the same units <see cref="UiType"/> and
    /// <see cref="UiTouch"/> use. On a 1080-wide phone 1 unit is about 0.4 dp, so Small (8) is about 3 dp.
    ///
    /// The ladder (tweak here, nowhere else):
    ///   None 0  Hairline 2  Tight 4  Snug 6  Small 8  Base 10  Medium 12  Gutter 14  Large 24
    ///
    /// The older call sites move onto these roles one screen at a time, so until that pass is finished some
    /// hand-written numbers remain - this file is the target, not the finished state yet.
    /// </summary>
    public static class UiSpace
    {
        // ------------------------------------------------------------------
        // The ladder - the ONLY gap numbers in the game. Tweak here, nowhere else.
        // ------------------------------------------------------------------
        public const float None = 0f;
        public const float Hairline = 2f;
        public const float Tight = 4f;
        public const float Snug = 6f;
        public const float Small = 8f;
        public const float Base = 10f;
        public const float Medium = 12f;
        public const float Gutter = 14f;
        public const float Large = 24f;

        // ------------------------------------------------------------------
        // Roles - what a layout asks for. Each names a job, not a number.
        // ------------------------------------------------------------------

        /// <summary>The gap from the safe-area edge to a full-width bar, so every bar shares one edge.</summary>
        public const float ScreenMargin = Gutter;

        /// <summary>The gap between two big sections on a page.</summary>
        public const float SectionGap = Small;

        /// <summary>The gap between two rows in a list. The same value as <see cref="UiTouch.RowGap"/> on purpose.</summary>
        public const float RowGap = Small;

        /// <summary>The inset from a card or dialog edge to its content.</summary>
        public const float CardPadding = Medium;

        /// <summary>The left/right inset for a label inside a button.</summary>
        public const float LabelPadX = Small;

        /// <summary>The top/bottom inset for a label inside a button.</summary>
        public const float LabelPadY = Tight;

        /// <summary>The left/right inset for a label inside a small chip (currency, boost).</summary>
        public const float ChipPadX = Tight;

        /// <summary>The top/bottom inset for a label inside a small chip (currency, boost).</summary>
        public const float ChipPadY = Hairline;

        // ------------------------------------------------------------------
        // Helpers - build the padding an editor scroll view wants out of the ladder.
        // ------------------------------------------------------------------

        /// <summary>The same inset on all four sides.</summary>
        public static RectOffset Padding(float all)
        {
            return new RectOffset((int)all, (int)all, (int)all, (int)all);
        }

        /// <summary>One inset left and right, another top and bottom.</summary>
        public static RectOffset Padding(float horizontal, float vertical)
        {
            return new RectOffset((int)horizontal, (int)horizontal, (int)vertical, (int)vertical);
        }
    }
}
