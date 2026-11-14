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
        // The named palette (2026-11-14). Every colour the UI draws comes from here; UiColourCheck keeps it
        // that way. Values are the exact ones the screens used to write by hand - near-duplicates were merged
        // (<= 0.05 in one channel) so one job has one name. Consolidating further is a design decision, made
        // with screenshots, not a refactor.
        // ------------------------------------------------------------------
        /// <summary></summary>
        public static readonly Color InkBody = new Color(1f, 1f, 1f, 0.92f);
        /// <summary></summary>
        public static readonly Color InkSoft = new Color(1f, 1f, 1f, 0.75f);
        /// <summary></summary>
        public static readonly Color InkDim = new Color(1f, 1f, 1f, 0.60f);
        /// <summary></summary>
        public static readonly Color InkFaint = new Color(1f, 1f, 1f, 0.45f);
        /// <summary></summary>
        public static readonly Color InkHint = new Color(1f, 1f, 1f, 0.30f);
        /// <summary></summary>
        public static readonly Color InkGhost = new Color(1f, 1f, 1f, 0.18f);
        /// <summary></summary>
        public static readonly Color Scrim = new Color(0f, 0f, 0f, 0.88f);
        /// <summary></summary>
        public static readonly Color ScrimSoft = new Color(0f, 0f, 0f, 0.65f);
        /// <summary></summary>
        public static readonly Color ScrimTrace = new Color(0f, 0f, 0f, 0.003f);
        /// <summary></summary>
        public static readonly Color InkDark = new Color(0.12f, 0.12f, 0.14f, 1f);
        /// <summary></summary>
        public static readonly Color InkGreyMid = new Color(0.45f, 0.46f, 0.50f, 1f);
        /// <summary></summary>
        public static readonly Color InkSoftDark = new Color(0.28f, 0.28f, 0.31f, 1f);
        /// <summary></summary>
        public static readonly Color InkDisabled = new Color(0.55f, 0.55f, 0.60f, 1f);
        /// <summary></summary>
        public static readonly Color TextMuted = new Color(0.72f, 0.76f, 0.84f, 1f);
        /// <summary></summary>
        public static readonly Color NavIdle = new Color(0.12f, 0.16f, 0.24f, 1f);
        /// <summary></summary>
        public static readonly Color NavSelected = new Color(0.28f, 0.38f, 0.56f, 1f);
        /// <summary></summary>
        public static readonly Color SelectedBlue = new Color(0.24f, 0.34f, 0.55f, 1f);
        /// <summary></summary>
        public static readonly Color SelectedBlueDeep = new Color(0.20f, 0.28f, 0.44f, 1f);
        /// <summary></summary>
        public static readonly Color FaceDim = new Color(0.11f, 0.13f, 0.19f, 1f);
        /// <summary></summary>
        public static readonly Color ButtonBlue = new Color(0.22f, 0.32f, 0.50f, 1f);
        /// <summary></summary>
        public static readonly Color MenuFace = new Color(0.20f, 0.26f, 0.40f, 1f);
        /// <summary></summary>
        public static readonly Color TabDim = new Color(0.10f, 0.12f, 0.18f, 1f);
        /// <summary></summary>
        public static readonly Color GridDim = new Color(0.10f, 0.12f, 0.17f, 0.90f);
        /// <summary></summary>
        public static readonly Color InfoBlue = new Color(0.55f, 0.80f, 1f, 1f);
        /// <summary></summary>
        public static readonly Color EventBlue = new Color(0.70f, 0.80f, 1f, 1f);
        /// <summary></summary>
        public static readonly Color GuardBlue = new Color(0.60f, 0.82f, 1f, 1f);
        /// <summary></summary>
        public static readonly Color RarityRare = new Color(0.45f, 0.72f, 1f, 1f);
        /// <summary></summary>
        public static readonly Color CardSelected = new Color(0.30f, 0.42f, 0.62f, 1f);
        /// <summary></summary>
        public static readonly Color CardBlue = new Color(0.16f, 0.19f, 0.28f, 0.95f);
        /// <summary></summary>
        public static readonly Color SlotBlue = new Color(0.14f, 0.16f, 0.23f, 0.95f);
        /// <summary></summary>
        public static readonly Color SheetFace = new Color(0.30f, 0.36f, 0.50f, 1f);
        /// <summary></summary>
        public static readonly Color PageSunken = new Color(0.10f, 0.11f, 0.13f, 1f);
        /// <summary></summary>
        public static readonly Color Row = new Color(0.13f, 0.14f, 0.16f, 1f);
        /// <summary></summary>
        public static readonly Color RowLight = new Color(0.18f, 0.19f, 0.22f, 1f);
        /// <summary></summary>
        public static readonly Color RowWarm = new Color(0.20f, 0.17f, 0.12f, 1f);
        /// <summary></summary>
        public static readonly Color CardDark = new Color(0.13f, 0.15f, 0.21f, 1f);
        /// <summary></summary>
        public static readonly Color LineGrey = new Color(0.30f, 0.31f, 0.35f, 1f);
        /// <summary></summary>
        public static readonly Color LineSoft = new Color(0.35f, 0.36f, 0.40f, 1f);
        /// <summary></summary>
        public static readonly Color IconWell = new Color(0.30f, 0.30f, 0.40f, 1f);
        /// <summary></summary>
        public static readonly Color Parchment = new Color(0.96f, 0.92f, 0.80f, 1f);
        /// <summary></summary>
        public static readonly Color GhostSlot = new Color(1f, 0.93f, 0.72f, 0.42f);
        /// <summary></summary>
        public static readonly Color Gold = new Color(0.98f, 0.80f, 0.28f, 1f);
        /// <summary></summary>
        public static readonly Color GoldDeep = new Color(0.98f, 0.80f, 0.22f, 1f);
        /// <summary></summary>
        public static readonly Color GoldDim = new Color(0.90f, 0.72f, 0.15f, 1f);
        /// <summary></summary>
        public static readonly Color GoldSoft = new Color(1f, 0.82f, 0.30f, 1f);
        /// <summary></summary>
        public static readonly Color GoldBright = new Color(1f, 0.85f, 0.30f, 1f);
        /// <summary></summary>
        public static readonly Color GoldLight = new Color(1f, 0.85f, 0.45f, 1f);
        /// <summary></summary>
        public static readonly Color GoldCrit = new Color(1f, 0.85f, 0.25f, 1f);
        /// <summary></summary>
        public static readonly Color GoldRim = new Color(1f, 0.95f, 0.66f, 1f);
        /// <summary></summary>
        public static readonly Color GoldRing = new Color(1f, 0.90f, 0.50f, 1f);
        /// <summary></summary>
        public static readonly Color GoldDark = new Color(0.84f, 0.62f, 0.26f, 1f);
        /// <summary></summary>
        public static readonly Color GoldGlow = new Color(0.98f, 0.78f, 0.20f, 0.10f);
        /// <summary></summary>
        public static readonly Color HaloGold = new Color(1f, 0.82f, 0.35f, 0.10f);
        /// <summary></summary>
        public static readonly Color Orange = new Color(0.95f, 0.55f, 0.15f, 1f);
        /// <summary></summary>
        public static readonly Color OrangeBright = new Color(1f, 0.55f, 0.15f, 1f);
        /// <summary></summary>
        public static readonly Color OrangeSoft = new Color(1f, 0.80f, 0.50f, 1f);
        /// <summary></summary>
        public static readonly Color Purple = new Color(0.80f, 0.55f, 0.98f, 1f);
        /// <summary></summary>
        public static readonly Color RarityEpic = new Color(0.78f, 0.55f, 1f, 1f);
        /// <summary></summary>
        public static readonly Color RarityLegendary = new Color(1f, 0.78f, 0.30f, 1f);
        /// <summary></summary>
        public static readonly Color Ready = new Color(0.30f, 0.68f, 0.42f, 1f);
        /// <summary></summary>
        public static readonly Color Cooling = new Color(0.30f, 0.33f, 0.40f, 1f);
        /// <summary></summary>
        public static readonly Color Queued = new Color(0.72f, 0.62f, 0.28f, 1f);
        /// <summary></summary>
        public static readonly Color Locked = new Color(0.22f, 0.24f, 0.29f, 1f);
        /// <summary></summary>
        public static readonly Color HpHealthy = new Color(0.35f, 0.85f, 0.45f, 1f);
        /// <summary></summary>
        public static readonly Color HpWounded = new Color(0.95f, 0.75f, 0.20f, 1f);
        /// <summary></summary>
        public static readonly Color HpCritical = new Color(0.90f, 0.25f, 0.20f, 1f);
        /// <summary></summary>
        public static readonly Color GreenSoft = new Color(0.55f, 0.95f, 0.60f, 1f);
        /// <summary></summary>
        public static readonly Color GreenBadge = new Color(0.30f, 0.90f, 0.40f, 1f);
        /// <summary></summary>
        public static readonly Color GreenDeep = new Color(0.05f, 0.20f, 0.05f, 1f);
        /// <summary></summary>
        public static readonly Color Danger = new Color(1f, 0.45f, 0.45f, 1f);
        /// <summary></summary>
        public static readonly Color DangerBright = new Color(1f, 0.35f, 0.35f, 1f);
        /// <summary></summary>
        public static readonly Color DangerDeep = new Color(0.75f, 0.18f, 0.18f, 1f);
        /// <summary></summary>
        public static readonly Color ArmedRed = new Color(0.52f, 0.16f, 0.16f, 0.95f);
        /// <summary></summary>
        public static readonly Color HaloGreen = new Color(0.35f, 0.75f, 0.45f, 0.10f);
        /// <summary></summary>
        public static readonly Color ItemBlue = new Color(0.35f, 0.68f, 0.95f, 1f);
        /// <summary></summary>
        public static readonly Color ItemPurple = new Color(0.72f, 0.52f, 0.92f, 1f);
        /// <summary></summary>
        public static readonly Color ItemGold = new Color(0.95f, 0.72f, 0.30f, 1f);
        /// <summary></summary>
        public static readonly Color AtmosphereBottom = new Color(0.025f, 0.035f, 0.06f, 1f);
        /// <summary></summary>
        public static readonly Color AtmosphereMid = new Color(0.075f, 0.11f, 0.15f, 1f);
        /// <summary></summary>
        public static readonly Color AtmosphereTop = new Color(0.045f, 0.06f, 0.10f, 1f);

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
        // Type sizes
        // ------------------------------------------------------------------
        // Sizes are NOT here: they live in UiType, one role per job, so a skin can never fork the type hierarchy.
        // This file owns look only - fonts, colours, sprites.

        /// <summary>
        /// How tall one line of the combat log is. Derived from the dialogue role, so the log follows the scale.
        /// </summary>
        public static float LogLineHeight => UiType.LineHeight(UiType.Role.Dialogue);

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