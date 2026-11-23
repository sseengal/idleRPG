namespace IdleRPG.UI
{
    /// <summary>
    /// Mobile sizing rules for the upgrade-style screens (one upgrade per row).
    ///
    /// ELI5: a finger is about 1.6-2 cm wide, so every button must be at least 1 cm across or players will
    /// miss it. The canvas is built at 1080x1920, so on a 1080p phone one unit is about a third of a
    /// device-independent pixel: 1 cm is roughly 114 units and Material's 48 dp is about 144. We use 144.
    ///
    /// Why this file exists: the hero upgrade page used to fit five stat tiles - with a +1 and +10 button each -
    /// into one 320-unit row, which left every button about 28 units tall (roughly 9 dp, a quarter of a
    /// centimetre). This is the number that fixes it, in one place.
    /// </summary>
    public static class UiTouch
    {
        /// <summary>Minimum tappable width/height. 144 units is about 48 dp / 1.1 cm on a 1080p phone.</summary>
        public const float MinTarget = 144f;

        /// <summary>One upgrade row: name + effect + level + cost, with its buy buttons on the right.</summary>
        public const float RowHeight = 180f;

        /// <summary>
        /// Gap between rows in a stacked list. Points at <see cref="UiSpace.RowGap"/> so the spacing ladder owns
        /// the number; the name stays because the touch rules (and their docs) use it.
        /// </summary>
        public const float RowGap = UiSpace.RowGap;

        /// <summary>How tall a list of <paramref name="rows"/> rows is, including the gaps between them.</summary>
        public static float ListHeight(int rows)
        {
            return rows <= 0 ? 0f : rows * RowHeight + (rows - 1) * RowGap;
        }

        // ------------------------------------------------------------------
        // Sheet buttons - one size for every stacked dialog button (pause, settings, "end this run?").
        //
        // ELI5: the buttons on a pop-up sheet all share one height, one gap and one width, so a sheet never looks
        // home-made. A 2026-11-23 measurement of the pause sheet: 202-tall buttons with 82 between them and 209 of
        // dead space under the last one - the box was 40% too tall and the caption could never fill it. These four
        // numbers are the fix, and nothing else may pick its own.
        // ------------------------------------------------------------------

        /// <summary>The height of one sheet button. The touch minimum is also the standard: bigger is not better.</summary>
        public const float SheetButtonHeight = MinTarget;

        /// <summary>The gap between two sheet buttons: one full rung, so two rows can never read as one block.</summary>
        public const float SheetButtonGap = UiSpace.Space32;

        /// <summary>The space under the last sheet button: the same rung as the side padding, so the stack is framed.</summary>
        public const float SheetBottomPad = UiSpace.Space64;

        /// <summary>The gap between the sheet's ribbon and its first button.</summary>
        public const float SheetTopGap = UiSpace.Space32;

        /// <summary>
        /// The inset from the sheet's left and right rim to its buttons. Buttons fill what is left, so a sheet can
        /// never end up with a narrow button floating in a wide panel.
        /// </summary>
        public const float SheetPadX = UiSpace.Space64;

        /// <summary>The inset from the sheet's top and bottom rim. Equal to the side inset: one frame, four equal sides.</summary>
        public const float SheetPadY = UiSpace.Space64;

        /// <summary>One currency row in the "you keep" list: icon plus amount.</summary>
        public const float KeepRowHeight = 56f;

        /// <summary>The icon in a "you keep" row.</summary>
        public const float KeepIconSize = 40f;

        /// <summary>The gap between two "you keep" rows.</summary>
        public const float KeepRowGap = UiSpace.Space8;

        /// <summary>Total height of a stack of sheet buttons, gaps included.</summary>
        public static float SheetStackHeight(int rows)
        {
            return rows <= 0 ? 0f : rows * SheetButtonHeight + (rows - 1) * SheetButtonGap;
        }
    }
}
