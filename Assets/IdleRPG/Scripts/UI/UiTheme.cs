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
        // ==================================================================
        // 1. PRIMITIVES - the raw ramp. A screen never uses these names.
        //
        // One blue-tinted neutral ramp, darkest to lightest, so every surface in the game belongs to the same
        // family. Seven steps are enough for page, panel, card and track; more steps only invite drift.
        // ==================================================================
        /// <summary>#0B0F15 - the darkest step: wells, bar tracks.</summary>
        public static readonly Color Slate950 = new Color32(11, 15, 21, 255);
        /// <summary>#10151F - the page behind everything.</summary>
        public static readonly Color Slate900 = new Color32(16, 21, 31, 255);
        /// <summary>#161C29 - a raised layer.</summary>
        public static readonly Color Slate800 = new Color32(22, 28, 41, 255);
        /// <summary>#1E2536 - panels, strips, the header.</summary>
        public static readonly Color Slate700 = new Color32(30, 37, 54, 255);
        /// <summary>#273044 - cards and list rows.</summary>
        public static readonly Color Slate600 = new Color32(39, 48, 68, 255);
        /// <summary>#323D57 - borders and strong lines.</summary>
        public static readonly Color Slate500 = new Color32(50, 61, 87, 255);
        /// <summary>#41506F - the lightest neutral: disabled faces, hairlines over art.</summary>
        public static readonly Color Slate400 = new Color32(65, 80, 111, 255);

        /// <summary>#B8862F - gold pressed into shadow.</summary>
        public static readonly Color Gold600 = new Color32(184, 134, 47, 255);
        /// <summary>#D69E42 - the brand gold.</summary>
        public static readonly Color Gold500 = new Color32(214, 158, 66, 255);
        /// <summary>#F7CB6E - gold catching light.</summary>
        public static readonly Color Gold300 = new Color32(247, 203, 110, 255);

        // ==================================================================
        // 2. SEMANTIC TOKENS - the only names a screen may use.
        //
        // A screen says what a colour is FOR (a page, a card, a pressed button, a danger), never a hex value.
        // ------------------------------------------------------------------
        // Surfaces: one ramp, kept intentionally short. If a screen needs a new surface, it picks one of these
        // four - it does not invent a fifth near-identical blue.
        // ------------------------------------------------------------------
        /// <summary>The deepest layer: bar tracks, wells, inputs.</summary>
        public static readonly Color SurfaceSunken = Slate950;
        /// <summary>The page behind everything.</summary>
        public static readonly Color SurfaceBase = Slate900;
        /// <summary>Panels and strips that sit on the page: header, log, camp band.</summary>
        public static readonly Color SurfaceRaised = Slate700;
        /// <summary>Cards and list rows that sit on a panel.</summary>
        public static readonly Color SurfaceCard = Slate600;

        /// <summary>A hairline or a quiet edge.</summary>
        public static readonly Color BorderSubtle = Slate600;
        /// <summary>A visible edge: the rim of a button, a selected row.</summary>
        public static readonly Color BorderStrong = Slate500;

        // ------------------------------------------------------------------
        // The button family. Every button in the game is the same filled button: one face, two state tones, one
        // ink. DARKER on purpose - it sits closer to the panels than the old mid-blue, so the UI reads as one
        // surface with raised controls, not as blue stickers on a dark page.
        // ------------------------------------------------------------------
        /// <summary>The face of an idle button: #2A3A57.</summary>
        public static readonly Color ActionFace = new Color32(42, 58, 87, 255);
        /// <summary>The face of a selected / active button: #33466A.</summary>
        public static readonly Color ActionFaceSelected = new Color32(51, 70, 106, 255);
        /// <summary>The face of a pressed button: #22304A.</summary>
        public static readonly Color ActionFacePressed = new Color32(34, 48, 74, 255);
        /// <summary>The label on a button face.</summary>
        public static readonly Color ActionInk = new Color32(236, 240, 248, 255);
        // ------------------------------------------------------------------
        // Text inks, brightest to dimmest. Opaque on purpose: the old white-with-alpha inks turned into a
        // different colour on every backdrop. When text sits over art, it sits on a shade (ShadeBar), not on art.
        // ------------------------------------------------------------------
        /// <summary>#ECF0F8 - headings, numbers, the thing the player must read.</summary>
        public static readonly Color InkHigh = new Color32(236, 240, 248, 255);
        /// <summary>#B8C2D6 - body text and secondary labels.</summary>
        public static readonly Color InkMedium = new Color32(184, 194, 214, 255);
        /// <summary>#929CB4 - captions and hints that are not needed to act.</summary>
        public static readonly Color InkLow = new Color32(146, 156, 180, 255);
        /// <summary>#6E7A93 - disabled text and icons.</summary>
        public static readonly Color InkOff = new Color32(110, 122, 147, 255);
        /// <summary>#261E0C - dark ink on gold.</summary>
        public static readonly Color InkOnGold = new Color32(38, 30, 12, 255);

        // ------------------------------------------------------------------
        // Brand and status. One tone per meaning: gold is value, green is healthy, amber is a warning, red is
        // danger, blue is information.
        // ------------------------------------------------------------------
        /// <summary>The brand gold: currency, active state, boost.</summary>
        public static readonly Color AccentBase = Gold500;
        /// <summary>Gold over art and on highlights.</summary>
        public static readonly Color AccentBright = Gold300;
        /// <summary>The soft gold halo behind an active thing.</summary>
        public static readonly Color AccentGlow = new Color(Gold500.r, Gold500.g, Gold500.b, 0.10f);

        /// <summary>Healthy, ready, owned.</summary>
        public static readonly Color StatusOk = new Color32(89, 217, 115, 255);
        /// <summary>Healthy, lighter - for use over art.</summary>
        public static readonly Color StatusOkSoft = new Color32(140, 242, 153, 255);
        /// <summary>Healthy, pressed into shadow: badges and deep fills.</summary>
        public static readonly Color StatusOkDeep = new Color32(13, 51, 13, 255);

        /// <summary>Wounded, waiting, a caution.</summary>
        public static readonly Color StatusWarn = new Color32(242, 191, 51, 255);
        /// <summary>Wounded, lighter - for use over art.</summary>
        public static readonly Color StatusWarnSoft = new Color32(255, 204, 128, 255);

        /// <summary>Danger as a FILL: a failing health bar, a destructive button.</summary>
        public static readonly Color StatusBad = new Color32(230, 64, 51, 255);
        /// <summary>Danger as INK: damage numbers and warnings, readable on a dark ground.</summary>
        public static readonly Color StatusBadText = new Color32(255, 115, 115, 255);
        /// <summary>Danger pressed into shadow.</summary>
        public static readonly Color StatusBadDeep = new Color32(191, 46, 46, 255);

        /// <summary>Neutral information: a guard, an event, a rare find.</summary>
        public static readonly Color StatusInfo = new Color32(140, 204, 255, 255);
        /// <summary>Information, lighter - for use over art.</summary>
        public static readonly Color StatusInfoSoft = new Color32(178, 204, 255, 255);
        /// <summary>Epic rarity and other arcane marks.</summary>
        public static readonly Color StatusEpic = new Color32(199, 140, 255, 255);
        // Experience is blue on purpose: gold means money, green means ready, red means danger.
        /// <summary>The party XP bar's body.</summary>
        public static readonly Color XpFill = new Color32(64, 140, 255, 255);
        /// <summary>The leading edge of the XP fill and the flying motes that feed it.</summary>
        public static readonly Color XpFillLight = new Color32(158, 217, 255, 255);
        /// <summary>The halo around a flying XP mote; low alpha, so a stack of motes stays light.</summary>
        public static readonly Color XpTrail = new Color(XpFillLight.r, XpFillLight.g, XpFillLight.b, 0.35f);
        /// <summary>The one-shot wash when a level lands.</summary>
        public static readonly Color XpFlash = new Color32(217, 242, 255, 255);

        // ------------------------------------------------------------------
        // Veils and shades. The alpha belongs to the token, not to each call site.
        // ------------------------------------------------------------------
        /// <summary>The near-black behind numbers on a nameplate, and inside a bar's track. 75%.</summary>
        public static readonly Color ShadeBar = new Color(0f, 0f, 0f, 0.75f);
        /// <summary>A dimming layer behind a sheet. 65%.</summary>
        public static readonly Color VeilSoft = new Color(0f, 0f, 0f, 0.65f);
        /// <summary>A near-opaque layer behind a modal card. 88%.</summary>
        public static readonly Color VeilStrong = new Color(0f, 0f, 0f, 0.88f);
        /// <summary>An invisible raycast blocker.</summary>
        public static readonly Color VeilTrace = new Color(0f, 0f, 0f, 0.003f);

        // ------------------------------------------------------------------
        // Stage backdrop (art, not chrome): the three stops of the arena atmosphere.
        // ------------------------------------------------------------------
        public static readonly Color StageBottom = new Color32(6, 9, 15, 255);
        public static readonly Color StageMid = new Color32(19, 28, 38, 255);
        public static readonly Color StageTop = new Color32(11, 15, 26, 255);

        // ==================================================================
        // 3. LEGACY ALIASES - deprecated. Each one points at a semantic token.
        //
        // Nothing breaks, and every screen shifts at once. Do NOT add a new use of these names: UiColourCheck
        // fails the build on one. The sweep deletes them screen by screen.
        // ==================================================================

        // Surfaces.
        public static readonly Color Panel = SurfaceRaised;
        public static readonly Color PanelDeep = SurfaceRaised;
        public static readonly Color PanelLight = SurfaceCard;
        public static readonly Color CardDark = SurfaceCard;
        public static readonly Color Row = SurfaceCard;
        public static readonly Color RowLight = SurfaceCard;
        public static readonly Color RowWarm = SurfaceCard;
        public static readonly Color PageSunken = SurfaceBase;
        public static readonly Color TabDim = SurfaceBase;
        public static readonly Color FaceDim = SurfaceRaised;
        public static readonly Color NavIdle = SurfaceRaised;
        public static readonly Color IconWell = SurfaceCard;
        public static readonly Color CardBlue = new Color(SurfaceCard.r, SurfaceCard.g, SurfaceCard.b, 0.95f);
        public static readonly Color SlotBlue = new Color(SurfaceRaised.r, SurfaceRaised.g, SurfaceRaised.b, 0.95f);
        public static readonly Color GridDim = new Color(SurfaceBase.r, SurfaceBase.g, SurfaceBase.b, 0.90f);

        // Borders.
        public static readonly Color LineGrey = BorderSubtle;
        public static readonly Color LineSoft = BorderStrong;
        // Buttons.
        public static readonly Color ButtonBlue = ActionFace;
        public static readonly Color MenuFace = ActionFace;
        public static readonly Color SheetFace = ActionFace;
        public static readonly Color SelectedBlue = ActionFaceSelected;
        public static readonly Color SelectedBlueDeep = ActionFacePressed;
        public static readonly Color NavSelected = ActionFaceSelected;
        public static readonly Color CardSelected = ActionFaceSelected;

        // Text and inks. The alpha inks keep their alpha because they are drawn over art; the opaque ones move
        // onto the ink ramp.
        public static readonly Color Text = InkHigh;
        public static readonly Color TextDim = InkLow;
        public static readonly Color TextMuted = InkMedium;
        public static readonly Color Parchment = InkHigh;
        public static readonly Color InkOnAccent = InkOnGold;
        public static readonly Color InkDark = InkOnGold;
        public static readonly Color InkGreyMid = InkOff;
        public static readonly Color InkSoftDark = InkOff;
        public static readonly Color InkDisabled = InkOff;
        public static readonly Color InkBody = new Color(InkHigh.r, InkHigh.g, InkHigh.b, 0.92f);
        public static readonly Color InkSoft = new Color(InkHigh.r, InkHigh.g, InkHigh.b, 0.75f);
        public static readonly Color InkDim = new Color(InkHigh.r, InkHigh.g, InkHigh.b, 0.60f);
        public static readonly Color InkFaint = new Color(InkHigh.r, InkHigh.g, InkHigh.b, 0.45f);
        public static readonly Color InkHint = new Color(InkHigh.r, InkHigh.g, InkHigh.b, 0.30f);
        public static readonly Color InkGhost = new Color(InkHigh.r, InkHigh.g, InkHigh.b, 0.18f);

        // Veils.
        public static readonly Color Scrim = VeilStrong;
        public static readonly Color ScrimSoft = VeilSoft;
        public static readonly Color ScrimTrace = VeilTrace;
        public static readonly Color PlateShade = ShadeBar;

        // Gold: two tones and one halo, whatever the old name was.
        public static readonly Color Gold = AccentBright;
        public static readonly Color GoldDeep = AccentBright;
        public static readonly Color GoldDim = AccentBright;
        public static readonly Color GoldSoft = AccentBright;
        public static readonly Color GoldBright = AccentBright;
        public static readonly Color GoldLight = AccentBright;
        public static readonly Color GoldCrit = AccentBright;
        public static readonly Color GoldRim = AccentBright;
        public static readonly Color GoldRing = AccentBright;
        public static readonly Color GoldDark = AccentBase;
        public static readonly Color Accent = AccentBase;
        public static readonly Color AccentLight = AccentBright;
        public static readonly Color GoldGlow = AccentGlow;
        public static readonly Color HaloGold = AccentGlow;
        public static readonly Color RarityLegendary = AccentBright;
        public static readonly Color ItemGold = AccentBase;
        public static readonly Color GhostSlot = new Color(AccentBright.r, AccentBright.g, AccentBright.b, 0.42f);

        // Healthy.
        public static readonly Color Ready = StatusOk;
        public static readonly Color HpHealthy = StatusOk;
        public static readonly Color GreenBadge = StatusOk;
        public static readonly Color GreenSoft = StatusOkSoft;
        public static readonly Color GreenDeep = StatusOkDeep;
        public static readonly Color HaloGreen = new Color(StatusOk.r, StatusOk.g, StatusOk.b, 0.10f);

        // Waiting / wounded.
        public static readonly Color Queued = StatusWarn;
        public static readonly Color HpWounded = StatusWarn;
        public static readonly Color Orange = StatusWarn;
        public static readonly Color OrangeBright = StatusWarn;
        public static readonly Color OrangeSoft = StatusWarnSoft;
        public static readonly Color Cooling = InkOff;
        public static readonly Color HpGhost = new Color(AccentBright.r, AccentBright.g, AccentBright.b, 0.5f);

        // Danger.
        public static readonly Color Danger = StatusBadText;
        public static readonly Color DangerBright = StatusBadText;
        public static readonly Color DangerDeep = StatusBadDeep;
        public static readonly Color HpCritical = StatusBad;
        public static readonly Color ArmedRed = new Color(StatusBadDeep.r, StatusBadDeep.g, StatusBadDeep.b, 0.95f);

        // Information and rarity.
        public static readonly Color InfoBlue = StatusInfo;
        public static readonly Color EventBlue = StatusInfoSoft;
        public static readonly Color GuardBlue = StatusInfoSoft;
        public static readonly Color RarityRare = StatusInfo;
        public static readonly Color ItemBlue = StatusInfo;
        public static readonly Color Purple = StatusEpic;
        public static readonly Color RarityEpic = StatusEpic;
        public static readonly Color ItemPurple = StatusEpic;

        // Tracks.
        public static readonly Color Locked = SurfaceSunken;

        // Stage backdrop.
        public static readonly Color AtmosphereBottom = StageBottom;
        public static readonly Color AtmosphereMid = StageMid;
        public static readonly Color AtmosphereTop = StageTop;

        // ------------------------------------------------------------------
        // Fonts (in a Resources folder; paths relative to it)
        // ------------------------------------------------------------------
        private static TMP_FontAsset cachedDisplay;
        private static TMP_FontAsset cachedSentence;

        /// <summary>Chunky font for numbers, buttons, currency and damage/crits. Abaddon Bold.</summary>
        public static TMP_FontAsset Display =>
            cachedDisplay != null ? cachedDisplay : (cachedDisplay = Resources.Load<TMP_FontAsset>("Fonts/Abaddon Bold Pixel"));

        /// <summary>Readable font for sentences, e.g. the battle log. Abaddon Light.</summary>
        public static TMP_FontAsset Sentence =>
            cachedSentence != null ? cachedSentence : (cachedSentence = Resources.Load<TMP_FontAsset>("Fonts/Abaddon Light Pixel"));

        // ------------------------------------------------------------------
        // Type sizes
        // ------------------------------------------------------------------
        // Sizes are NOT here: they live in UiType, one role per job, so a skin can never fork the type hierarchy.
        // This file owns look only - fonts, colours, sprites.

        /// <summary>
        /// How tall one line of the combat log is. Derived from the dialogue role, so the log follows the scale.
        /// </summary>
        public static float LogLineHeight => UiType.LineHeight(UiType.Role.Dialogue);

        /// <summary>
        /// The health bar's colour for a fraction of life: green while healthy, amber when wounded, red when nearly
        /// gone. One rule, so a bar in an ability cell and a bar over a hero tell the same story.
        /// </summary>
        public static Color HpTint(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);

            if (normalized > 0.5f)
            {
                return Color.Lerp(HpWounded, HpHealthy, (normalized - 0.5f) * 2f);
            }

            return Color.Lerp(HpCritical, HpWounded, normalized * 2f);
        }

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