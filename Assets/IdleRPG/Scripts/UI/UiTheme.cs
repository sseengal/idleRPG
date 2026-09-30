using TMPro;
using UnityEngine;

namespace IdleRPG.UI
{
    /// <summary>
    /// The battle-page look, in one file: palette + fonts + type sizes (design tokens).
    ///
    /// Owner decision 2026-09-29: look A from the UI Lab - Silkscreen for numbers/buttons/damage, VT323 for
    /// sentences like the battle log, dark-slate + gold palette. Every battle-screen colour or size on this
    /// page comes from here; the old scattered literals in the scene builder are being retired one by one.
    ///
    /// Fonts live in a Resources folder on purpose: runtime-created text (log pool lines, hero HP bars, the
    /// damage pool) cannot use editor-only AssetDatabase paths, and wiring a font reference into every
    /// runtime-created label would be more code than this.
    /// </summary>
    public static class UiTheme
    {
        // ------------------------------------------------------------------
        // Palette (look A)
        // ------------------------------------------------------------------
        public static readonly Color PanelDeep = new Color32(31, 36, 54, 255);   // strips and bars
        public static readonly Color Panel = new Color32(37, 43, 62, 255);       // default surfaces
        public static readonly Color PanelLight = new Color32(48, 56, 80, 255);  // chips, inactive tabs
        public static readonly Color Accent = new Color32(214, 158, 66, 255);    // gold: active, boost
        public static readonly Color AccentLight = new Color32(247, 203, 110, 255); // gold over art
        public static readonly Color Text = new Color32(236, 240, 248, 255);
        public static readonly Color TextDim = new Color32(146, 156, 180, 255);
        public static readonly Color InkOnAccent = new Color32(38, 30, 12, 255);

        // ------------------------------------------------------------------
        // Fonts (in a Resources folder; paths relative to it)
        // ------------------------------------------------------------------
        private static TMP_FontAsset cachedDisplay;
        private static TMP_FontAsset cachedSentence;

        /// <summary>Chunky font for numbers, buttons, currency and damage/crits. Silkscreen.</summary>
        public static TMP_FontAsset Display =>
            cachedDisplay != null ? cachedDisplay : (cachedDisplay = Resources.Load<TMP_FontAsset>("Fonts/Silkscreen Pixel"));

        /// <summary>Readable font for sentences, e.g. the battle log. VT323.</summary>
        public static TMP_FontAsset Sentence =>
            cachedSentence != null ? cachedSentence : (cachedSentence = Resources.Load<TMP_FontAsset>("Fonts/VT323 Pixel"));

        // ------------------------------------------------------------------
        // Type sizes (reference pixels at 1080x1920)
        // ------------------------------------------------------------------
        public const float NumberSize = 40f;    // currency values
        public const float LabelSize = 26f;     // short labels
        public const float RowLabelSize = 22f;  // HP values, small labels
        public const float LogSize = 34f;       // battle log lines (VT323, user: "bigger")
        public const float LogLineHeight = 46f;
        public const float DamageSize = 32f;    // floating damage numbers
        public const float CritScale = 1.3f;    // crit text is this much bigger

        /// <summary>Applies a theme font + size to a text object (font, material and size together).</summary>
        public static void ApplyFont(TextMeshProUGUI text, TMP_FontAsset font, float size)
        {
            if (text == null || font == null)
            {
                return;
            }

            text.font = font;
            text.fontSharedMaterial = font.material;
            text.fontSize = size;
        }
    }
}