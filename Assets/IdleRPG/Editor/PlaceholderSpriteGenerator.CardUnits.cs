using System.IO;
using UnityEditor;
using UnityEngine;
using IdleRPG.EditorTools.Content;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Card-picture drawing (B8' tool 1): one picture per hero/enemy card that has none yet.
    /// </summary>
    public static partial class PlaceholderSpriteGenerator
    {
        // ------------------------------------------------------------------
        // Card pictures (one per hero/enemy card)
        // ------------------------------------------------------------------
        /// <summary>
        /// Draws a picture for every hero and enemy card that does not have one yet.
        ///
        /// Plain words: you write a card for a new monster, press one menu button, and it gets a picture.
        /// No code change needed - that is the whole point, and it is how content scales later.
        ///
        /// Two rules keep it safe:
        ///   * an existing file is NEVER overwritten, so hand-made art and the curated units stay untouched;
        ///   * the shape comes from the words in the card's id and the colour from the card's tint, because both
        ///     already survive the trip between cards and assets. A brand-new card field would be dropped when
        ///     specs are exported back from the assets, so the family lives in the id instead.
        /// </summary>
        public static int GenerateCardUnits()
        {
            return GenerateCardUnits(out int cardsChecked);
        }

        /// <summary>
        /// Same as <see cref="GenerateCardUnits()"/>, and also reports how many cards were looked at, so the menu
        /// line can say "7 cards checked, 0 new pictures drawn" instead of a bare number.
        /// </summary>
        public static int GenerateCardUnits(out int cardsChecked)
        {
            EnsureFolder(ArtFolder);

            int written = 0;
            cardsChecked = 0;

            HeroSpecFile heroes = ContentSpecIO.Load<HeroSpecFile>(ContentSpecIO.HeroesPath);
            if (heroes != null)
            {
                for (int i = 0; i < heroes.heroes.Count; i++)
                {
                    HeroSpec hero = heroes.heroes[i];
                    cardsChecked++;
                    written += WriteCardUnit(PictureName(hero.icon, hero.id), hero.tint, hero.id, false);
                }
            }

            EnemySpecFile enemies = ContentSpecIO.Load<EnemySpecFile>(ContentSpecIO.EnemiesPath);
            if (enemies != null)
            {
                for (int i = 0; i < enemies.enemies.Count; i++)
                {
                    EnemySpec enemy = enemies.enemies[i];
                    cardsChecked++;
                    written += WriteCardUnit(PictureName(enemy.sprite, enemy.id), enemy.tint, enemy.id, enemy.isBoss);
                }
            }

            if (written > 0)
            {
                Debug.Log($"[PlaceholderSpriteGenerator] {written} new card picture(s) drawn ({cardsChecked} card(s) checked).");
            }

            return written;
        }

        /// <summary>Picture file name for a card: its sprite name when it has one, otherwise its id.</summary>
        private static string PictureName(string sprite, string id)
        {
            return string.IsNullOrEmpty(sprite) ? id : sprite;
        }

        /// <summary>Draws one card picture, but only when the file is missing. Returns 1 when it drew something.</summary>
        private static int WriteCardUnit(string fileName, string tintHex, string cardId, bool isBoss)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return 0;
            }

            string path = ArtFolder + "/" + fileName + ".png";
            if (File.Exists(path))
            {
                return 0;   // never touch an existing picture
            }

            Color fill = ContentSpecIO.FromHex(tintHex, Color.white);
            return WriteUnit(fileName, fill, ShapeForCard(cardId, isBoss), Darken(fill));
        }

        /// <summary>
        /// Shape family for a card, read from the words in its id: "enemy_slime" is a blob, "enemy_bat" has wings.
        /// A card with no family word still gets a picture (a blob), and the content check warns about it.
        /// </summary>
        private static UnitShape ShapeForCard(string cardId, bool isBoss)
        {
            switch (FamilyOf(cardId, isBoss))
            {
                case "spikes":
                    return UnitShape.Spikes;

                case "wings":
                    return UnitShape.Wings;

                case "ears":
                    return UnitShape.Ears;

                case "shield":
                    return UnitShape.Shield;

                case "diamond":
                    return UnitShape.Diamond;

                case "chevron":
                    return UnitShape.Chevron;

                default:
                    return UnitShape.Blob;
            }
        }

        /// <summary>
        /// Family name for a card - "blob", "wings", "ears", "shield", "diamond", "chevron", "spikes", or "unknown"
        /// when the id carries no family word. Every boss is "spikes".
        ///
        /// This is the single place the words are listed: the drawing tool turns the family into a shape, and the
        /// content check warns when the family is "unknown", so the naming rule is visible instead of hidden here.
        /// </summary>
        public static string FamilyOf(string cardId, bool isBoss)
        {
            if (isBoss)
            {
                return "spikes";
            }

            string id = (cardId ?? string.Empty).ToLowerInvariant();

            if (ContainsAny(id, "slime", "blob", "ooze", "swarm", "spider", "worm"))
            {
                return "blob";
            }

            if (ContainsAny(id, "bat", "wing", "fly", "moth", "harpy", "wyvern"))
            {
                return "wings";
            }

            if (ContainsAny(id, "goblin", "orc", "ogre", "brute", "troll", "rat", "wolf", "beast"))
            {
                return "ears";
            }

            if (ContainsAny(id, "knight", "guard", "shield", "turtle", "golem", "armou", "armor", "tank"))
            {
                return "shield";
            }

            if (ContainsAny(id, "mage", "wizard", "witch", "sorcer", "spirit", "elemental"))
            {
                return "diamond";
            }

            if (ContainsAny(id, "archer", "ranger", "hunter", "gold", "rich", "coin", "mimic", "chest", "treasure"))
            {
                return "chevron";
            }

            return "unknown";
        }

        private static bool ContainsAny(string text, params string[] words)
        {
            for (int i = 0; i < words.Length; i++)
            {
                if (text.Contains(words[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Outline colour for a card picture: the card colour, darkened so the silhouette reads.</summary>
        private static Color Darken(Color fill)
        {
            return new Color(fill.r * 0.35f, fill.g * 0.35f, fill.b * 0.35f, 1f);
        }
    }
}
