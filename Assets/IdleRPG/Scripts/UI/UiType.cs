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
    ///   Display   56  Silkscreen  currency, hero numbers            cap ~2.2 mm
    ///   Title     48  Silkscreen  card / screen / modal titles      cap ~1.9 mm
    ///   Section   40  Silkscreen  section heads, primary buttons    cap ~1.6 mm
    ///   Sentence  36  VT323       running text: log, effects       cap ~1.3 mm
    ///   Small     30  Silkscreen  nav, tags, costs, column heads    cap ~1.2 mm
    ///   Micro     26  Silkscreen  captions - never needed to act    cap ~1.0 mm
    ///   Damage    32  Silkscreen  floating damage (crit x1.3)       cap ~1.3 mm
    ///
    /// Two rules keep it honest:
    ///  * chrome (numbers, headings, buttons, labels) is Silkscreen; running sentences are VT323. VT323's x-height
    ///    is only 0.41 of its size, so it reads smaller than Silkscreen at the same number - it never carries a
    ///    label the player must read at a glance.
    ///  * <see cref="Micro"/> is the floor. Nothing is smaller.
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
            Damage
        }

        // ------------------------------------------------------------------
        // The scale - the ONLY text sizes in the game. Tweak here, nowhere else.
        // ------------------------------------------------------------------
        public const float Display = 56f;
        public const float Title = 48f;
        public const float Section = 40f;
        public const float Sentence = 36f;
        public const float Small = 30f;
        public const float Micro = 26f;
        public const float Damage = 32f;

        /// <summary>Crit damage text is this much bigger than <see cref="Damage"/>.</summary>
        public const float CritScale = 1.3f;

        /// <summary>The smallest size anything may use.</summary>
        public const float MinSize = Micro;

        /// <summary>
        /// Leading as a multiple of the size. The two fonts differ: Silkscreen's natural line height is 1.28 of
        /// its size, VT323's is 1.0 - so sentence text needs the extra leading spelled out.
        /// </summary>
        public const float LeadingChrome = 1.28f;
        public const float LeadingSentence = 1.3f;

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
                case Role.Sentence:
                default:
                    return Sentence;
            }
        }

        /// <summary>The font for a role: VT323 for running sentences, Silkscreen for everything else.</summary>
        public static TMP_FontAsset Font(Role role)
        {
            return role == Role.Sentence ? UiTheme.Sentence : UiTheme.Display;
        }

        /// <summary>How tall one line of this role is, in canvas units.</summary>
        public static float LineHeight(Role role)
        {
            return Size(role) * (role == Role.Sentence ? LeadingSentence : LeadingChrome);
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
