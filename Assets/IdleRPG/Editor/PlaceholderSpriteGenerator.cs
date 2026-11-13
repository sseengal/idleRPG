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

        /// <summary>Runtime-loadable folder. The tintable UI frame lives here so StartScreenUI can fetch it.</summary>
        public const string ResourcesFolder = "Assets/IdleRPG/Resources";

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
            count += WriteUnit("hero_soldier", new Color(0.35f, 0.85f, 0.45f), UnitShape.Chevron, new Color(0.08f, 0.22f, 0.12f));
            count += WriteUnit("hero_mage", new Color(0.75f, 0.40f, 0.95f), UnitShape.Diamond, new Color(0.20f, 0.10f, 0.28f));
            count += WriteUnit("enemy_slime", new Color(0.50f, 0.90f, 0.40f), UnitShape.Blob, new Color(0.08f, 0.20f, 0.08f));
            count += WriteUnit("enemy_bat", new Color(0.62f, 0.42f, 0.32f), UnitShape.Wings, new Color(0.16f, 0.10f, 0.08f));
            count += WriteUnit("boss_ogre", new Color(0.85f, 0.25f, 0.20f), UnitShape.Spikes, new Color(0.22f, 0.06f, 0.05f));

            return count;
        }

        private static int GeneratePanels()
        {
            int count = 0;

            // Tintable neutral frame: one sprite that every lobby panel and button can colour.
            count += WriteFrame("ui_frame");

            // Chunky 16-bit candidates for the UI Lab comparison (single vs double bevel).
            count += WriteChunkyFrame("ui_chunky_a", innerLine: false);
            count += WriteChunkyFrame("ui_chunky_b", innerLine: true);

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

            // Journey trail marks. White, so the band tints them; point filtered, because they are pixel art.
            count += WriteIcon("ui_mark_dot", Color.white, IconShape.Circle, pointFilter: true);
            count += WriteIcon("ui_mark_ring", Color.white, IconShape.Ring, pointFilter: true);

            // Glow halos. White with baked alpha, so one tint drives every glow.
            count += WriteIcon("ui_glow_soft", Color.white, IconShape.SoftGlow);
            count += WriteIcon("ui_glow_steps", Color.white, IconShape.StepsGlow, pointFilter: true);
            count += WriteIcon("ui_glow_burst", Color.white, IconShape.BurstGlow, pointFilter: true);

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
            Scroll,
            Circle,
            Ring,
            SoftGlow,
            StepsGlow,
            BurstGlow
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
        // Panel and icon sprites
        // ------------------------------------------------------------------
        private static int WritePanel(string fileName, Color fill, Color border, int cornerRadius)
        {
            Texture2D texture = new Texture2D(PanelSize, PanelSize, TextureFormat.RGBA32, false);
            Fill(texture, new Color(0f, 0f, 0f, 0f));

            bool hasBorder = border.a > 0f;
            int borderThickness = hasBorder ? 3 : 0;

            // The face sits inset by the border, so the border stroke owns the outer 2 px.
            int left = borderThickness;
            int top = borderThickness;
            int right = PanelSize - borderThickness;   // exclusive
            int bottom = PanelSize - borderThickness;  // exclusive

            FillRoundedRect(texture, left, top, right - left, bottom - top, cornerRadius, fill);

            // Retro bevel: a light band on the top + left edges and a dark band on the bottom + right
            // edges, drawn only on the straight spans so the rounded corners stay clean. This is what
            // turns a flat rectangle into a raised "3D" frame.
            // 4 px so the rim still reads after a 9-slice stretch (a 2 px band vanished on big buttons).
            const int bevel = 4;
            Color highlight = Lighten(fill, 0.22f);
            Color shadow = Darken(fill, 0.45f);

            FillRect(texture, left + cornerRadius, top, right - left - cornerRadius * 2, bevel, highlight);
            FillRect(texture, left, top + cornerRadius, bevel, bottom - top - cornerRadius * 2, highlight);
            FillRect(texture, left + cornerRadius, bottom - bevel, right - left - cornerRadius * 2, bevel, shadow);
            FillRect(texture, right - bevel, top + cornerRadius, bevel, bottom - top - cornerRadius * 2, shadow);

            if (hasBorder)
            {
                StrokeRoundedRect(texture, 0, 0, PanelSize, PanelSize, cornerRadius, borderThickness, border);
            }

            // 9-slice border: cover radius + border + bevel so the corners never stretch.
            int slice = cornerRadius + borderThickness + bevel + 2;
            return WriteTexture(texture, fileName, new Vector4(slice, slice, slice, slice));
        }

        /// <summary>
        /// A neutral, tintable 9-slice frame: white face, light top + left, dark bottom + right, black
        /// outline. Tint it with any colour and it reads as a raised button, so one sprite serves the whole
        /// lobby. Lives in Resources so StartScreenUI can load it at runtime.
        /// </summary>
        private static int WriteFrame(string fileName)
        {
            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Fill(texture, new Color(0f, 0f, 0f, 0f));

            const int cornerRadius = 1;
            const int outline = 5;
            const int bevel = 10;

            int left = outline;
            int top = outline;
            int right = size - outline;   // exclusive
            int bottom = size - outline;  // exclusive
            int spanX = right - left - cornerRadius * 2;
            int spanY = bottom - top - cornerRadius * 2;

            // Face is mid grey, so the bright ridge still reads after the whole plate is tinted.
            Color face = new Color(0.90f, 0.90f, 0.90f, 1f);
            FillRoundedRect(texture, left, top, right - left, bottom - top, cornerRadius, face);

            // Chunky fantasy metal band: bright ridge + dark groove on the lit edges, dark band + light
            // lip on the shaded edges.
            // Sun from the top-left (texture y=0 is the bottom, so the visual top is the high-y side).
            // Stepped gradients on the lit and shaded bands give the gold a polished-metal sheen.
            Color hi = Color.white;
            Color lo = new Color(0.05f, 0.05f, 0.07f, 1f);

            // Visual top: bright glint fading into the face (10 px).
            FillRect(texture, left + cornerRadius, bottom - 10, spanX, 3, hi);
            FillRect(texture, left + cornerRadius, bottom - 7, spanX, 3, new Color(0.96f, 0.96f, 0.96f, 1f));
            FillRect(texture, left + cornerRadius, bottom - 4, spanX, 4, new Color(0.92f, 0.92f, 0.92f, 1f));

            // Visual bottom: black fading up into the face (10 px).
            FillRect(texture, left + cornerRadius, top, spanX, 4, lo);
            FillRect(texture, left + cornerRadius, top + 4, spanX, 3, new Color(0.30f, 0.30f, 0.30f, 1f));
            FillRect(texture, left + cornerRadius, top + 7, spanX, 3, new Color(0.60f, 0.60f, 0.60f, 1f));

            // Visual left: thin bright edge (5 px).
            FillRect(texture, left, top + cornerRadius, 2, spanY, hi);
            FillRect(texture, left + 2, top + cornerRadius, 3, spanY, new Color(0.92f, 0.92f, 0.92f, 1f));

            // Visual right: thin dark edge (5 px).
            FillRect(texture, right - 2, top + cornerRadius, 2, spanY, lo);
            FillRect(texture, right - 5, top + cornerRadius, 3, spanY, new Color(0.30f, 0.30f, 0.30f, 1f));

            // Near-black plate edge.
            // Inner border at the bevel's inner edge, with the same clipped (stair) corners.
            DrawInnerBorder(texture, left + 5, top + 10, right - 5, bottom - 10, 4, new Color(0.20f, 0.20f, 0.24f, 1f));

            StrokeRoundedRect(texture, 0, 0, size, size, cornerRadius, outline, new Color(0.05f, 0.05f, 0.08f, 1f));

            // Clip the OUTER corner at the TEXTURE edge (not the inner face) with a big pixel stair.
            const int clipSteps = 5;
            Color plateEdge = new Color(0.05f, 0.05f, 0.08f, 1f);
            ClipOuterCorner(texture, 0, size - 1, 1, -1, clipSteps, plateEdge);
            ClipOuterCorner(texture, size - 1, size - 1, -1, -1, clipSteps, plateEdge);
            ClipOuterCorner(texture, 0, 0, 1, 1, clipSteps, plateEdge);
            ClipOuterCorner(texture, size - 1, 0, -1, 1, clipSteps, plateEdge);

            int slice = cornerRadius + outline + bevel + 2;
            return WriteTexture(texture, fileName, new Vector4(slice, slice, slice, slice), false, ResourcesFolder);
        }

        /// <summary>A 1 px border with clipped (stair) corners, so the inner edge is chamfered like the outer one.</summary>
        private static void DrawInnerBorder(Texture2D texture, int x0, int y0, int x1, int y1, int chamfer, Color color)
        {
            for (int x = x0 + chamfer; x <= x1 - chamfer; x++)
            {
                texture.SetPixel(x, y0, color);
                texture.SetPixel(x, y1, color);
            }

            for (int y = y0 + chamfer; y <= y1 - chamfer; y++)
            {
                texture.SetPixel(x0, y, color);
                texture.SetPixel(x1, y, color);
            }

            for (int i = 0; i < chamfer; i++)
            {
                int j = chamfer - i;
                texture.SetPixel(x0 + i, y0 + j, color);
                texture.SetPixel(x1 - i, y0 + j, color);
                texture.SetPixel(x0 + i, y1 - j, color);
                texture.SetPixel(x1 - i, y1 - j, color);
            }
        }

        /// <summary>Clips the OUTER corner in a pixel stair and strokes the new edge, so the silhouette is chamfered and bordered.</summary>
        private static void ClipOuterCorner(Texture2D texture, int x, int y, int dx, int dy, int steps, Color outline)
        {
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int i = 0; i < steps; i++)
            {
                for (int j = 0; j < steps - i; j++)
                {
                    texture.SetPixel(x + dx * i, y + dy * j, clear);
                }

                // The first kept pixel along this column becomes the new outer edge: stroke it dark.
                texture.SetPixel(x + dx * i, y + dy * (steps - i), outline);
            }
        }

        /// <summary>
        /// Chunky 16-bit (GBA) frame: near-square corners, a thick high-contrast bevel and a near-black
        /// outline. <paramref name="innerLine"/> adds the second ridge (double bevel). Tintable like
        /// <see cref="WriteFrame"/>: white = full tint, grey = darker.
        /// </summary>
        private static int WriteChunkyFrame(string fileName, bool innerLine)
        {
            Texture2D texture = new Texture2D(PanelSize, PanelSize, TextureFormat.RGBA32, false);
            Fill(texture, new Color(0f, 0f, 0f, 0f));

            const int cornerRadius = 4;
            const int outline = 2;
            const int bevel = 3;

            int left = outline;
            int top = outline;
            int right = PanelSize - outline;   // exclusive
            int bottom = PanelSize - outline;  // exclusive
            int spanX = right - left - cornerRadius * 2;
            int spanY = bottom - top - cornerRadius * 2;

            // Face sits below white so the highlight still reads once tinted.
            Color face = new Color(0.85f, 0.85f, 0.85f, 1f);
            FillRoundedRect(texture, left, top, right - left, bottom - top, cornerRadius, face);

            // High-contrast bevel: white = full tint, dark grey = deep shadow.
            Color hi = Color.white;
            Color lo = new Color(0.35f, 0.35f, 0.35f, 1f);
            FillRect(texture, left + cornerRadius, top, spanX, bevel, hi);
            FillRect(texture, left, top + cornerRadius, bevel, spanY, hi);
            FillRect(texture, left + cornerRadius, bottom - bevel, spanX, bevel, lo);
            FillRect(texture, right - bevel, top + cornerRadius, bevel, spanY, lo);

            if (innerLine)
            {
                // Second ridge: a dark line inside the light edge, a light line inside the dark edge.
                Color ridge = new Color(0.55f, 0.55f, 0.55f, 1f);
                Color lip = new Color(0.97f, 0.97f, 0.97f, 1f);
                FillRect(texture, left + cornerRadius, top + bevel, spanX, 1, ridge);
                FillRect(texture, left + bevel, top + cornerRadius, 1, spanY, ridge);
                FillRect(texture, left + cornerRadius, bottom - bevel - 1, spanX, 1, lip);
                FillRect(texture, right - bevel - 1, top + cornerRadius, 1, spanY, lip);
            }

            // Near-black outline stays dark under any tint.
            StrokeRoundedRect(texture, 0, 0, PanelSize, PanelSize, cornerRadius, outline,
                new Color(0.06f, 0.06f, 0.10f, 1f));

            int slice = cornerRadius + outline + bevel + (innerLine ? 1 : 0) + 2;
            return WriteTexture(texture, fileName, new Vector4(slice, slice, slice, slice));
        }

        private static int WriteIcon(string fileName, Color tint, IconShape shape, bool pointFilter = false)
        {
            Texture2D texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            Clear(texture);

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

                case IconShape.Circle:
                    FillCircle(texture, cx, cy, radius, tint);
                    break;

                case IconShape.Ring:
                    DrawRing(texture, cx, cy, radius, radius * 0.62f, tint);
                    break;

                case IconShape.SoftGlow:
                    DrawRadialAlpha(texture, cx, cy, radius, d => Mathf.Pow(1f - d, 3f) * 0.85f);
                    break;

                case IconShape.StepsGlow:
                    DrawRadialAlpha(texture, cx, cy, radius,
                        d => d < 0.34f ? 0.30f : (d < 0.58f ? 0.18f : (d < 0.82f ? 0.09f : 0f)));
                    break;

                case IconShape.BurstGlow:
                    DrawRadialAlpha(texture, cx, cy, radius,
                        d => d < 0.62f ? 0.10f : (d < 0.74f ? 0.55f : (d < 0.86f ? 0.22f : 0f)));
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

            return WriteTexture(texture, fileName, new Vector4(0, 0, 0, 0), pointFilter);
        }

        // ------------------------------------------------------------------
        // Asset IO
        // ------------------------------------------------------------------
        private static int WriteTexture(Texture2D texture, string fileName, Vector4 border, bool pointFilter = false,
            string folder = null)
        {
            string path = (folder ?? ArtFolder) + "/" + fileName + ".png";
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
            importer.filterMode = pointFilter ? FilterMode.Point : FilterMode.Bilinear;
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
