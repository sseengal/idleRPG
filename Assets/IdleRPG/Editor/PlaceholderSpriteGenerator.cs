using System.IO;
using UnityEditor;
using UnityEngine;
using IdleRPG.EditorTools.Content;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Generates every placeholder sprite the MVP needs (heroes, enemies, UI chrome, icons)
    /// as real PNG assets, so nothing has to be downloaded and artists can replace files later.
    ///
    /// Menu: Tools > Idle RPG > Art > Generate Placeholder Sprites
    /// </summary>
    public static partial class PlaceholderSpriteGenerator
    {
        public const string ArtFolder = "Assets/IdleRPG/Art/Placeholder";

        private const int UnitSize = 128;
        private const int PanelSize = 64;
        private const int IconSize = 64;

        [MenuItem("Tools/Idle RPG/Art/Generate Placeholder Sprites", priority = 10)]

        public static void GenerateAll()
        {
            EnsureFolder(ArtFolder);

            int written = 0;
            written += GenerateUnits();
            written += GeneratePanels();
            written += GenerateIcons();
            written += GenerateBackground();
            int newCardPictures = GenerateCardUnits(out int cardsChecked);   // one picture per card that has none yet
            written += newCardPictures;

            AssetDatabase.Refresh();
            Debug.Log($"[PlaceholderSpriteGenerator] {written} sprite(s) written (units, panels, icons, background) | " +
                      $"{cardsChecked} card(s) checked, {newCardPictures} new card picture(s) drawn | existing pictures kept.");
        }

        // ------------------------------------------------------------------
        // Sprite definitions
        // ------------------------------------------------------------------
        private static int GenerateUnits()
        {
            int count = 0;

            count += WriteUnit("hero_knight", new Color(0.35f, 0.55f, 0.95f), UnitShape.Shield, new Color(0.10f, 0.16f, 0.30f));
            count += WriteUnit("hero_archer", new Color(0.35f, 0.85f, 0.45f), UnitShape.Chevron, new Color(0.08f, 0.22f, 0.12f));
            count += WriteUnit("hero_mage", new Color(0.75f, 0.40f, 0.95f), UnitShape.Diamond, new Color(0.20f, 0.10f, 0.28f));
            count += WriteUnit("enemy_slime", new Color(0.50f, 0.90f, 0.40f), UnitShape.Blob, new Color(0.08f, 0.20f, 0.08f));
            count += WriteUnit("enemy_bat", new Color(0.62f, 0.42f, 0.32f), UnitShape.Wings, new Color(0.16f, 0.10f, 0.08f));
            count += WriteUnit("enemy_goblin", new Color(0.45f, 0.75f, 0.35f), UnitShape.Ears, new Color(0.12f, 0.18f, 0.08f));
            count += WriteUnit("boss_ogre", new Color(0.85f, 0.25f, 0.20f), UnitShape.Spikes, new Color(0.22f, 0.06f, 0.05f));

            return count;
        }

        private static int GeneratePanels()
        {
            int count = 0;

            count += WritePanel("ui_panel", new Color(0.11f, 0.13f, 0.20f, 1f), new Color(0.00f, 0.00f, 0.00f, 0f), 12);
            count += WritePanel("ui_panel_light", new Color(0.18f, 0.21f, 0.30f, 1f), new Color(0.00f, 0.00f, 0.00f, 0f), 12);
            count += WritePanel("ui_panel_bordered", new Color(0.11f, 0.13f, 0.20f, 1f), new Color(0.30f, 0.45f, 0.70f, 1f), 12);
            count += WritePanel("ui_button", new Color(0.22f, 0.32f, 0.50f, 1f), new Color(0.45f, 0.65f, 0.95f, 1f), 10);
            count += WritePanel("ui_button_gold", new Color(0.55f, 0.42f, 0.10f, 1f), new Color(0.95f, 0.80f, 0.30f, 1f), 10);
            count += WritePanel("ui_tab_on", new Color(0.20f, 0.28f, 0.44f, 1f), new Color(0.50f, 0.70f, 1f, 1f), 8);
            count += WritePanel("ui_tab_off", new Color(0.10f, 0.12f, 0.18f, 1f), new Color(0.20f, 0.24f, 0.32f, 1f), 8);

            return count;
        }

        private static int GenerateIcons()
        {
            int count = 0;

            count += WriteIcon("ui_icon_gold", new Color(0.98f, 0.80f, 0.25f), IconShape.Coin);
            count += WriteIcon("ui_icon_gem", new Color(0.45f, 0.85f, 0.98f), IconShape.Diamond);
            count += WriteIcon("ui_icon_token", new Color(0.85f, 0.55f, 0.98f), IconShape.Star);
            count += WriteIcon("ui_icon_ad", new Color(0.35f, 0.85f, 0.55f), IconShape.Play);

            // Step 9b-3: one icon per currency row, so a currency is never a bare label in the UI.
            count += WriteIcon("ui_icon_shard", new Color(0.70f, 0.78f, 0.90f), IconShape.Shard);
            count += WriteIcon("ui_icon_material", new Color(0.72f, 0.60f, 0.42f), IconShape.Ingot);
            count += WriteIcon("ui_icon_essence", new Color(0.55f, 0.95f, 0.85f), IconShape.Flask);
            count += WriteIcon("ui_icon_scroll", new Color(0.92f, 0.86f, 0.66f), IconShape.Scroll);

            return count;
        }

        private static int GenerateBackground()
        {
            const int width = 256;
            const int height = 256;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color top = new Color(0.06f, 0.08f, 0.14f, 1f);
            Color bottom = new Color(0.14f, 0.17f, 0.26f, 1f);

            for (int y = 0; y < height; y++)
            {
                Color row = Color.Lerp(bottom, top, y / (float)(height - 1));

                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, row);
                }
            }

            // Ground strip so units have a horizon line.
            FillRect(texture, 0, 0, width, 52, new Color(0.05f, 0.07f, 0.11f, 1f));
            FillRect(texture, 0, 50, width, 2, new Color(0.30f, 0.45f, 0.70f, 1f));

            return WriteTexture(texture, "combat_bg", new Vector4(0, 0, 0, 0));
        }

        private enum UnitShape
        {
            Shield,
            Chevron,
            Diamond,
            Blob,
            Wings,
            Ears,
            Spikes
        }

        private enum IconShape
        {
            Coin,
            Diamond,
            Star,
            Play,
            Shard,
            Ingot,
            Flask,
            Scroll
        }

        // ------------------------------------------------------------------
        // Unit sprites
        // ------------------------------------------------------------------
        private static int WriteUnit(string fileName, Color fill, UnitShape shape, Color outline)
        {
            Texture2D texture = new Texture2D(UnitSize, UnitSize, TextureFormat.RGBA32, false);
            Color transparent = new Color(0f, 0f, 0f, 0f);
            Fill(texture, transparent);

            int pad = 12;
            int size = UnitSize - pad * 2;
            Vector2[] polygon = BuildPolygon(shape, UnitSize / 2f, UnitSize / 2f, size / 2f);

            DrawPolygon(texture, polygon, fill, outline);

            // Simple highlight so shapes read as "sprites" rather than flat blobs.
            DrawPolygon(texture, ScalePolygon(polygon, Centroid(polygon), 0.45f), Lighten(fill, 0.25f), transparent);

            return WriteTexture(texture, fileName, new Vector4(0, 0, 0, 0));
        }

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
            if (isBoss)
            {
                return UnitShape.Spikes;
            }

            string id = (cardId ?? string.Empty).ToLowerInvariant();

            if (ContainsAny(id, "slime", "blob", "ooze", "swarm", "spider", "worm"))
            {
                return UnitShape.Blob;
            }

            if (ContainsAny(id, "bat", "wing", "fly", "moth", "harpy", "wyvern"))
            {
                return UnitShape.Wings;
            }

            if (ContainsAny(id, "goblin", "orc", "ogre", "brute", "troll", "rat", "wolf", "beast"))
            {
                return UnitShape.Ears;
            }

            if (ContainsAny(id, "knight", "guard", "shield", "turtle", "golem", "armou", "armor", "tank"))
            {
                return UnitShape.Shield;
            }

            if (ContainsAny(id, "mage", "wizard", "witch", "sorcer", "spirit", "elemental"))
            {
                return UnitShape.Diamond;
            }

            if (ContainsAny(id, "archer", "ranger", "hunter", "gold", "rich", "coin", "mimic", "chest", "treasure"))
            {
                return UnitShape.Chevron;
            }

            return UnitShape.Blob;
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

        // ------------------------------------------------------------------
        // Panel and icon sprites
        // ------------------------------------------------------------------
        private static int WritePanel(string fileName, Color fill, Color border, int cornerRadius)
        {
            Texture2D texture = new Texture2D(PanelSize, PanelSize, TextureFormat.RGBA32, false);
            Fill(texture, new Color(0f, 0f, 0f, 0f));

            bool hasBorder = border.a > 0f;
            int borderThickness = hasBorder ? 2 : 0;

            FillRoundedRect(texture, borderThickness, borderThickness,
                PanelSize - borderThickness * 2, PanelSize - borderThickness * 2, cornerRadius, fill);

            if (hasBorder)
            {
                StrokeRoundedRect(texture, 0, 0, PanelSize, PanelSize, cornerRadius, borderThickness, border);
            }

            // 9-slice border: cover radius + thickness so corners never stretch.
            int slice = cornerRadius + borderThickness + 2;
            return WriteTexture(texture, fileName, new Vector4(slice, slice, slice, slice));
        }

        private static int WriteIcon(string fileName, Color tint, IconShape shape)
        {
            Texture2D texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            Fill(texture, new Color(0f, 0f, 0f, 0f));

            float cx = IconSize * 0.5f;
            float cy = IconSize * 0.5f;
            float radius = IconSize * 0.44f;
            Color dark = Darken(tint, 0.45f);

            switch (shape)
            {
                case IconShape.Coin:
                    FillCircle(texture, cx, cy, radius, dark);
                    FillCircle(texture, cx, cy, radius - 5f, tint);
                    FillCircle(texture, cx, cy, radius * 0.35f, dark);
                    break;

                case IconShape.Diamond:
                    DrawPolygon(texture, new[]
                    {
                        new Vector2(cx, cy + radius),
                        new Vector2(cx + radius * 0.72f, cy + radius * 0.1f),
                        new Vector2(cx, cy - radius),
                        new Vector2(cx - radius * 0.72f, cy + radius * 0.1f)
                    }, tint, dark);
                    break;

                case IconShape.Star:
                    DrawPolygon(texture, BuildStar(cx, cy, radius, radius * 0.45f, 5), tint, dark);
                    break;

                case IconShape.Shard:
                    DrawPolygon(texture, new[]
                    {
                        new Vector2(cx - radius * 0.2f, cy + radius),
                        new Vector2(cx + radius * 0.65f, cy + radius * 0.25f),
                        new Vector2(cx + radius * 0.15f, cy - radius),
                        new Vector2(cx - radius * 0.7f, cy - radius * 0.15f)
                    }, tint, dark);
                    break;

                case IconShape.Ingot:
                    DrawPolygon(texture, new[]
                    {
                        new Vector2(cx - radius * 0.8f, cy - radius * 0.6f),
                        new Vector2(cx + radius * 0.45f, cy - radius * 0.6f),
                        new Vector2(cx + radius * 0.8f, cy + radius * 0.6f),
                        new Vector2(cx - radius * 0.45f, cy + radius * 0.6f)
                    }, tint, dark);
                    break;

                case IconShape.Flask:
                    DrawPolygon(texture, new[]
                    {
                        new Vector2(cx - radius * 0.22f, cy + radius),
                        new Vector2(cx + radius * 0.22f, cy + radius),
                        new Vector2(cx + radius * 0.22f, cy),
                        new Vector2(cx + radius * 0.75f, cy - radius),
                        new Vector2(cx - radius * 0.75f, cy - radius),
                        new Vector2(cx - radius * 0.22f, cy)
                    }, tint, dark);
                    break;

                case IconShape.Scroll:
                    FillRoundedRect(texture, Mathf.RoundToInt(cx - radius * 0.7f), Mathf.RoundToInt(cy - radius * 0.55f),
                        Mathf.RoundToInt(radius * 1.4f), Mathf.RoundToInt(radius * 1.1f), 6, tint);
                    FillCircle(texture, cx - radius * 0.7f, cy, radius * 0.22f, dark);
                    FillCircle(texture, cx + radius * 0.7f, cy, radius * 0.22f, dark);
                    break;

                default:
                    FillRoundedRect(texture, Mathf.RoundToInt(IconSize * 0.12f), Mathf.RoundToInt(IconSize * 0.22f),
                        Mathf.RoundToInt(IconSize * 0.76f), Mathf.RoundToInt(IconSize * 0.56f), 8, tint);
                    DrawPolygon(texture, new[]
                    {
                        new Vector2(cx - 7f, cy + 11f),
                        new Vector2(cx - 7f, cy - 11f),
                        new Vector2(cx + 13f, cy)
                    }, dark, new Color(0f, 0f, 0f, 0f));
                    break;
            }

            return WriteTexture(texture, fileName, new Vector4(0, 0, 0, 0));
        }

        // ------------------------------------------------------------------
        // Asset IO
        // ------------------------------------------------------------------
        private static int WriteTexture(Texture2D texture, string fileName, Vector4 border)
        {
            string path = ArtFolder + "/" + fileName + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[PlaceholderSpriteGenerator] No TextureImporter for {path}.");
                return 0;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = border;
            importer.SaveAndReimport();

            return 1;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
        }
    }
}
