using UnityEngine;

namespace IdleRPG.UI
{
    /// <summary>
    /// The game's spacing, in one file: one doubling ladder plus the roles a layout asks for.
    ///
    /// ELI5: the ruler. One short list of gap sizes - 2, 4, 8, 16, 32, 64 - so two screens that do the same job
    /// leave the same gap. A screen says WHAT a gap is for ("the screen edge", "between rows"), never a number.
    ///
    /// Why it exists: gaps were picked by hand in about 60 places with eight different numbers (2, 4, 6, 8, 10, 12,
    /// 14, 24). Two panels doing the same job padded 14/6 and 10/10, so nothing lined up. The 2026-11-23 pass moved
    /// every call site onto this ladder: the ladder standardises, it does not grow the gaps - a token keeps the
    /// value it had wherever that value was already on the ladder (8 stayed 8), and the half-steps snapped to the
    /// nearer rung (6 -> 8, 10 -> 8, 12 -> 16, 14 -> 16, 24 -> 32).
    ///
    /// Units are canvas units at the 1080x1920 reference. On a 1080-wide phone 1 unit is about 0.4 dp, so a
    /// "16 dp" padding is 40 units - but this ladder is deliberately TIGHT: the game's chrome is drawn at a
    /// compact scale and a textbook 40-unit gate would push every row apart. Size, not airiness, is the goal.
    /// </summary>
    public static class UiSpace
    {
        // ------------------------------------------------------------------
        // The ladder - the ONLY gap numbers in the game. Tweak here, nowhere else.
        // Every rung doubles, so two neighbours are always visibly different.
        // ------------------------------------------------------------------
        public const float Space0 = 0f;
        public const float Space2 = 2f;
        public const float Space4 = 4f;
        public const float Space8 = 8f;
        public const float Space16 = 16f;
        public const float Space32 = 32f;
        public const float Space64 = 64f;

        // ------------------------------------------------------------------
        // Roles - what a layout asks for. Each names a job, not a number.
        // ------------------------------------------------------------------

        /// <summary>The gap from the safe-area edge to a full-width bar, so every bar shares one edge.</summary>
        public const float ScreenMargin = Space16;

        /// <summary>
        /// The screen edge for the in-game chrome (header, combat log, camp strip, viewport). One rung wider than
        /// <see cref="ScreenMargin"/> because a 16-unit edge is only ~6 dp on a phone and reads as touching the bezel.
        /// </summary>
        public const float PageMargin = Space32;

        /// <summary>The gap between two big sections on a page.</summary>
        public const float SectionGap = Space16;

        /// <summary>The gap between two rows in a list. <see cref="UiTouch.RowGap"/> points here.</summary>
        public const float RowGap = Space8;

        /// <summary>The inset from a card or dialog edge to its content.</summary>
        public const float CardPadding = Space16;

        /// <summary>The left/right inset for a label inside a button.</summary>
        public const float LabelPadX = Space8;

        /// <summary>The top/bottom inset for a label inside a button.</summary>
        public const float LabelPadY = Space4;

        /// <summary>The left/right inset for a label inside a small chip (currency, boost).</summary>
        public const float ChipPadX = Space8;

        /// <summary>The top/bottom inset for a label inside a small chip (currency, boost).</summary>
        public const float ChipPadY = Space4;

        /// <summary>
        /// The height of a health bar: tall enough to hold one line of Micro numbers INSIDE it, so the bar says
        /// what it is without a separate caption. Used by the ability cells and by the arena nameplates.
        /// </summary>
        public const float HealthBarHeight = Space16 + Space8;

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
