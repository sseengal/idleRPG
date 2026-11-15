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
    ///   Display   62  Abaddon Bold   currency, hero numbers            cap ~2.2 mm
    ///   Title     54  Abaddon Bold   card / screen / modal titles      cap ~1.9 mm
    ///   Section   45  Abaddon Bold   section heads, primary buttons    cap ~1.6 mm
    ///   Sentence  33  Abaddon Light  running text: log, effects        x-height ~1.3 mm
    ///   Small     34  Abaddon Bold   nav, tags, costs, column heads    cap ~1.2 mm
    ///   Micro     29  Abaddon Bold   captions - never needed to act    cap ~1.0 mm
    ///   Damage    36  Abaddon Bold   floating damage (crit x1.3)       cap ~1.3 mm
    ///
    /// Two rules keep it honest:
    ///  * chrome (numbers, headings, buttons, labels) is Abaddon Bold; running sentences are Abaddon Light, the
    ///    lighter cut, so a paragraph never shouts.
    ///  * <see cref="Micro"/> is the floor. Nothing is smaller.
    ///
    /// Why the numbers moved (2026-11-15, Silkscreen/VT323 -> Abaddon). Abaddon's letters are shorter at the same
    /// number: cap height 0.625 of the size where Silkscreen's is 0.700, x-height 0.438 where VT323's is 0.400.
    /// Keeping 56/48/40/... would have made every label silently about 11% smaller. Each chrome size is therefore
    /// the old size x 0.700/0.625, and each sentence size the old size x 0.400/0.438, so the letters land on the
    /// same measured height as before and no screen has to be re-laid out by eye.
    /// </summary>
    public static class UiType
    {
        /// <summary>What a piece of text is for. Pick the role, never a number.</summary>
        public enum Role
        {
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
        public const float Display = 62f;
        public const float Title = 54f;
        public const float Section = 45f;
        public const float Sentence = 33f;
        public const float Small = 34f;
        public const float Micro = 29f;
        public const float Damage = 36f;

        /// <summary>Story dialogue in the battle log. One size up from Sentence, same readable font.</summary>
        public const float Dialogue = 44f;

        /// <summary>Crit damage text is this much bigger than <see cref="Damage"/>.</summary>
        public const float CritScale = 1.3f;

        /// <summary>The smallest size anything may use.</summary>
        public const float MinSize = Micro;

        /// <summary>
        /// Leading as a multiple of the size. Chrome keeps the line box it had under Silkscreen (1.28 / 1.12 = 1.15),
        /// so a fixed-height band cannot start clipping. Sentences use Abaddon's own 1.125 line plus a little air;
        /// each log line is therefore shorter than before and the same band shows about one more line.
        /// </summary>
        public const float LeadingChrome = 1.15f;
        public const float LeadingSentence = 1.25f;

        /// <summary>The size for a role.</summary>
        public static float Size(Role role)
        {
            switch (role)
            {
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

        /// <summary>The font for a role: Abaddon Light for running sentences, Abaddon Bold for everything else.</summary>
        public static TMP_FontAsset Font(Role role)
        {
            return role == Role.Sentence || role == Role.Dialogue ? UiTheme.Sentence : UiTheme.Display;
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
