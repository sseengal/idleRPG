namespace IdleRPG.UI
{
    /// <summary>
    /// The only glyphs UI strings may use. Everything here is verified present in the auto-generated
    /// LiberationSans SDF (the project's default runtime font); anything else - \u25BE \u25C6 \u2192
    /// \u25A0 etc. - is NOT in that font and renders as a replacement box (\u25A1) with a console warning
    /// every frame the label lives. Add a glyph only after confirming it in the font's character table.
    /// </summary>
    public static class UiGlyphs
    {
        /// <summary>Bullet / marker - selected option, pinned leaderboard row.</summary>
        public const string Marker = "\u2022";

        /// <summary>Middle dot - title separators ("GLOBAL RANKS \u00B7 you are #1").</summary>
        public const string Separator = "\u00B7";

        /// <summary>Forward chevron - dropdown open affordance, "EQUIP \u00BB KNIGHT".</summary>
        public const string Forward = "\u00BB";

        /// <summary>Em dash - clamped labels.</summary>
        public const string Dash = "\u2014";
    }
}