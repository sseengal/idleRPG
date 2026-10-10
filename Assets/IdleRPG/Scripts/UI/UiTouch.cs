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

        /// <summary>Gap between rows in a stacked list.</summary>
        public const float RowGap = 8f;

        /// <summary>Section header (a hero's name on the upgrades page).</summary>
        public const float HeaderHeight = 90f;

        /// <summary>How tall a list of <paramref name="rows"/> rows is, including the gaps between them.</summary>
        public static float ListHeight(int rows)
        {
            return rows <= 0 ? 0f : rows * RowHeight + (rows - 1) * RowGap;
        }
    }
}
