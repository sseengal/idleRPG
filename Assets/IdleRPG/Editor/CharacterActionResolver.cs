using System;
using System.IO;

namespace IdleRPG.EditorTools
{
    /// <summary>What a strip file in a character folder actually is.</summary>
    public enum CharacterActionKind
    {
        /// <summary>Belongs in a <see cref="IdleRPG.Data.CharacterArtSet"/> slot.</summary>
        Base,

        /// <summary>A named special move (Summon, Block, Heal, Beam, Spike) - the ability material.</summary>
        Ability,

        /// <summary>A magic/beam/spike effect overlay, or a composited "with effects" variant of an action.</summary>
        Vfx,

        /// <summary>Nothing we understand. Reported, never silently skipped.</summary>
        Unknown
    }

    /// <summary>One resolved strip: what kind it is, and which art-set slot it fills (Base only).</summary>
    public struct CharacterAction
    {
        public CharacterActionKind Kind;
        public string Slot;
        public string Strip;

        public CharacterAction(CharacterActionKind kind, string slot, string strip)
        {
            Kind = kind;
            Slot = slot;
            Strip = strip;
        }
    }

    /// <summary>
    /// Turns a pack's action file name into a slot the game understands.
    ///
    /// ELI5: the packs name their files several different ways - <c>Attack</c>, <c>Attack3</c>, <c>DEATH</c>,
    /// <c>Walk01</c>, <c>Flying</c>, <c>Sumon_Effect</c>. The old importer compared each name to a fixed list and
    /// **quietly skipped anything that did not match**, so a whole animation could vanish with no error. This class
    /// maps the names instead, and labels anything it does not recognise as <see cref="CharacterActionKind.Unknown"/>
    /// so the caller can shout about it.
    ///
    /// The pack files are never renamed: the mapping lives here, so a pack stays verifiable against the store and
    /// re-downloadable.
    /// </summary>
    public static class CharacterActionResolver
    {
        /// <summary>The art-set slots the game can play. Everything else is ability material.</summary>
        public static readonly string[] BaseSlots =
        {
            "Idle", "Walk", "Attack01", "Attack02", "Attack03", "Hurt", "Death"
        };

        /// <summary>Resolves one action name (the part after <c>&lt;Char&gt;_</c>).</summary>
        public static CharacterAction Resolve(string action)
        {
            if (string.IsNullOrEmpty(action))
            {
                return new CharacterAction(CharacterActionKind.Unknown, null, action);
            }

            string a = action.Trim();

            // Effects first: a composited or overlay variant is never an action slot. This catches "*_Effect",
            // "*_effect", "(With magic effects)", "(No beam effects)", "(No spike effects)" and the pack's
            // "Sumon_Effect" typo in one rule.
            if (Has(a, "effect") || Has(a, "effects"))
            {
                return new CharacterAction(CharacterActionKind.Vfx, null, a);
            }

            // Movement. "Flying" is the movement animation for the four flying creatures (Bat,
            // Blood Monster_B, Ghostfire, Hellbat). "Walk01"/"Walk02" is the two-variant walk
            // (Knight Templar, Lancer); Walk01 wins because files are sorted before this is called.
            if (Is(a, "Idle"))
            {
                return Base("Idle", a);
            }

            if (Is(a, "Walk") || Is(a, "Walk01") || Is(a, "Walk02") || Is(a, "Flying"))
            {
                return Base("Walk", a);
            }

            // Attacks. "Attack" with no digits is Priest and Skeleton Archer; "Attack3" is Swordsman.
            if (Is(a, "Attack") || Is(a, "Attack01"))
            {
                return Base("Attack01", a);
            }

            if (Is(a, "Attack02"))
            {
                return Base("Attack02", a);
            }

            if (Is(a, "Attack3") || Is(a, "Attack03"))
            {
                return Base("Attack03", a);
            }

            if (Is(a, "Hurt"))
            {
                return Base("Hurt", a);
            }

            if (Is(a, "Death"))
            {
                return Base("Death", a);
            }

            // Named specials: the ability material. Recognised so they are never "unknown", but they do not
            // belong in an art set until the ability system exists.
            if (Is(a, "Summon") || Is(a, "Block") || Is(a, "Heal") ||
                Is(a, "Beam") || Is(a, "Spike"))
            {
                return new CharacterAction(CharacterActionKind.Ability, null, a);
            }

            return new CharacterAction(CharacterActionKind.Unknown, null, a);
        }

        /// <summary>
        /// Finds the strip file in a character folder that fills <paramref name="slot"/>, or null.
        /// Files are sorted first, so the same folder always yields the same file.
        /// </summary>
        public static string FindStrip(string directory, string character, string slot)
        {
            if (!Directory.Exists(directory))
            {
                return null;
            }

            string[] files = Directory.GetFiles(directory, character + "_*.png");
            Array.Sort(files, StringComparer.Ordinal);

            for (int i = 0; i < files.Length; i++)
            {
                string action = Path.GetFileNameWithoutExtension(files[i]).Substring(character.Length + 1);
                CharacterAction resolved = Resolve(action);

                if (resolved.Kind == CharacterActionKind.Base && resolved.Slot == slot)
                {
                    return files[i];
                }
            }

            return null;
        }

        private static CharacterAction Base(string slot, string strip)
        {
            return new CharacterAction(CharacterActionKind.Base, slot, strip);
        }

        private static bool Is(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static bool Has(string a, string b)
        {
            return a.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
