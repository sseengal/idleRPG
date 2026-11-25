using TMPro;
using UnityEngine;

namespace IdleRPG.UI
{
    /// <summary>
    /// The game's type hierarchy, in one file. Nothing else may hard-code a text size.
    ///
    /// ELI5: the size list. Six sizes, each with one job and one font. Change a number here and every screen
    /// follows - there is no second place to change.
    ///
    /// Why it exists: sizes were written by hand in about 90 places (15 different numbers between 13 and 42), so
    /// half the game came out in Unity's smooth default font at roughly half the size a phone needs, and no two
    /// screens matched. This file is the fix: pick a role, never a number.
    ///
    /// The scale (reference canvas 1080x1920; on a 1080-wide phone 1 unit is about 0.4 dp):
    ///   Hero      72  Lilita One    the one primary action (START)      cap ~2.8 mm  (+28% step, deliberate)
    ///   Display   56  Lilita One    currency, hero numbers            cap ~2.2 mm
    ///   Title     48  Lilita One    card / screen / modal titles      cap ~1.9 mm
    ///   Section   40  Lilita One    section heads, button captions    cap ~1.6 mm
    ///   Sentence  34  Lilita One    running text: settings, item info cap ~1.4 mm
    ///   Small     30  Lilita One    nav, tags, costs, column heads    cap ~1.2 mm
    ///   Micro     26  Lilita One    captions - never needed to act    cap ~1.0 mm
    ///   Damage    32  Lilita One    floating damage (crit x1.3)       cap ~1.3 mm
    ///   Dialogue  44  Abaddon Light battle log only                   cap ~1.4 mm
    ///
    /// Two rules keep it honest:
    ///  * every label is the one chrome face, Lilita One; only the battle log keeps Abaddon Light, whose smaller
    ///    letters suit a paragraph.
    ///  * <see cref="Micro"/> is the floor. Nothing is smaller.
    ///
    /// The ladder rule (2026-11-23). Neighbouring steps must be 12-20% apart, or the eye cannot tell them apart.
    /// Before this pass the bottom of the scale read 29 / 30 / 28 - three names for one visual size - so a caption
    /// and a body sentence looked the same and the sheet labels felt cramped. The steps are now
    /// 56 -> 48 -> 40 (17 / 20%), 40 -> 34 (15%), 34 -> 30 (12%), 30 -> 26 (13%).
    ///
    /// Why the numbers moved again (2026-11-23, Abaddon -> Lilita One). The two faces sit differently on the
    /// body: Lilita's cap height is 0.704 of the size where Abaddon's is 0.625, and its x-height 0.506 where
    /// Abaddon Light's is 0.438. Keeping 62/54/45/... would have drawn every label about 13% taller and wider
    /// and broken the rows that were fitted to Abaddon. Each chrome size is therefore the old size x 0.888
    /// (0.625/0.704), so the letters land on the same measured height; the log keeps its size because its font
    /// did not change.
    ///
    /// Earlier move, for the record (2026-11-15, Silkscreen/VT323 -> Abaddon): cap height 0.700 -> 0.625 and
    /// x-height 0.400 -> 0.438, so each chrome size was the old size x 0.700/0.625.
    /// </summary>
    public static class UiType
    {
        /// <summary>What a piece of text is for. Pick the role, never a number.</summary>
        public enum Role
        {
            /// <summary>The one primary action on a screen (START). Bigger than every other label on purpose.</summary>
            Hero,
            Display,
            Title,
            Section,
            Sentence,
            Small,
            Micro,
            Damage,

            /// <summary>Running dialogue in the battle log. Bigger than Sentence, same VT323 font.</summary>
            Dialogue
        }

        // ------------------------------------------------------------------
        // The scale - the ONLY text sizes in the game. Tweak here, nowhere else.
        // ------------------------------------------------------------------
        /// <summary>
        /// The primary action's label: 72 units (about 29 dp). It is a deliberate +28% above Display, outside the
        /// 12-20% step band, because it has to beat every other word on the screen and it appears once per screen.
        /// </summary>
        public const float Hero = 72f;
        public const float Display = 56f;
        public const float Title = 48f;
        public const float Section = 40f;
        public const float Sentence = 34f;
        public const float Small = 30f;
        public const float Micro = 26f;
        public const float Damage = 32f;

        /// <summary>Story dialogue in the battle log. The one role still on Abaddon Light.</summary>
        public const float Dialogue = 44f;

        /// <summary>Crit damage text is this much bigger than <see cref="Damage"/>.</summary>
        public const float CritScale = 1.3f;

        /// <summary>The smallest size anything may use.</summary>
        public const float MinSize = Micro;

        /// <summary>
        /// Leading as a multiple of the size. Chrome keeps the line box it had under Silkscreen (1.28 / 1.12 = 1.15),
        /// so a fixed-height band cannot start clipping. The log uses Abaddon's own 1.125 line plus a little air;
        /// each log line is therefore shorter than before and the same band shows about one more line.
        /// </summary>
        public const float LeadingChrome = 1.15f;
        public const float LeadingSentence = 1.25f;

        /// <summary>The size for a role.</summary>
        public static float Size(Role role)
        {
            switch (role)
            {
                case Role.Hero:
                    return Hero;
                case Role.Display:
                    return Display;
                case Role.Title:
                    return Title;
                case Role.Section:
                    return Section;
                case Role.Small:
                    return Small;
                case Role.Micro:
                    return Micro;
                case Role.Damage:
                    return Damage;
                case Role.Dialogue:
                    return Dialogue;
                case Role.Sentence:
                default:
                    return Sentence;
            }
        }

        /// <summary>
        /// The font for a role: the chrome face for everything except the battle log, which keeps Abaddon Light.
        /// </summary>
        public static TMP_FontAsset Font(Role role)
        {
            return role == Role.Dialogue ? UiTheme.Sentence : UiTheme.Loud;
        }

        /// <summary>How tall one line of this role is, in canvas units.</summary>
        public static float LineHeight(Role role)
        {
            bool sentence = role == Role.Sentence || role == Role.Dialogue;
            return Size(role) * (sentence ? LeadingSentence : LeadingChrome);
        }

        /// <summary>Applies the role's font and its size.</summary>
        public static void Apply(TextMeshProUGUI text, Role role)
        {
            Apply(text, role, Size(role));
        }

        /// <summary>Applies the role's font at an explicit size (the damage text scales itself by <see cref="CritScale"/>).</summary>
        public static void Apply(TextMeshProUGUI text, Role role, float size)
        {
            if (text == null)
            {
                return;
            }

            text.enableAutoSizing = false;
            text.fontSize = size;   // set first, so a missing font asset still leaves the size correct
            UiTheme.ApplyFont(text, Font(role), size);
        }

        /// <summary>
        /// Applies the role, but lets the text shrink towards <see cref="MinSize"/> when the string is too long for
        /// its box. Use it for labels whose text length varies (nav tabs, button captions, currency), so a long
        /// string shrinks instead of spilling over its neighbours.
        /// </summary>
        public static void ApplyAuto(TextMeshProUGUI text, Role role)
        {
            if (text == null)
            {
                return;
            }

            float size = Size(role);
            text.fontSize = size;
            UiTheme.ApplyFont(text, Font(role), size);
            text.fontSizeMin = MinSize;
            text.fontSizeMax = size;
            text.enableAutoSizing = true;
        }
    }
}
