using System.IO;
using UnityEditor;
using UnityEngine;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Generates every placeholder sprite the MVP needs (heroes, enemies, UI chrome, icons)
    /// as real PNG assets, so nothing has to be downloaded and artists can replace files later.
    ///
    /// Menu: Tools > Idle RPG > Art > Generate Placeholder Sprites
    /// </summary>
    public static class PlaceholderSpriteGenerator
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

            AssetDatabase.Refresh();
            Debug.Log($"[PlaceholderSpriteGenerator] {written} placeholder sprite(s) ready in {ArtFolder}.");
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

        private static Vector2[] BuildPolygon(UnitShape shape, float cx, float cy, float radius)
        {
            switch (shape)
            {
                case UnitShape.Shield:
                    return new[]
                    {
                        new Vector2(cx - radius, cy + radius * 0.75f),
                        new Vector2(cx + radius, cy + radius * 0.75f),
                        new Vector2(cx + radius * 0.85f, cy - radius * 0.25f),
                        new Vector2(cx, cy - radius),
                        new Vector2(cx - radius * 0.85f, cy - radius * 0.25f)
                    };

                case UnitShape.Chevron:
                    return new[]
                    {
                        new Vector2(cx - radius, cy + radius * 0.4f),
                        new Vector2(cx, cy + radius),
                        new Vector2(cx + radius * 0.2f, cy - radius * 0.2f),
                        new Vector2(cx - radius * 0.45f, cy - radius),
                        new Vector2(cx - radius * 0.9f, cy - radius * 0.35f)
                    };

                case UnitShape.Diamond:
                    return new[]
                    {
                        new Vector2(cx, cy + radius),
                        new Vector2(cx + radius * 0.75f, cy),
                        new Vector2(cx, cy - radius),
                        new Vector2(cx - radius * 0.75f, cy)
                    };

                case UnitShape.Blob:
                    return new[]
                    {
                        new Vector2(cx - radius * 0.9f, cy - radius * 0.6f),
                        new Vector2(cx - radius * 0.55f, cy + radius * 0.75f),
                        new Vector2(cx + radius * 0.55f, cy + radius * 0.75f),
                        new Vector2(cx + radius * 0.9f, cy - radius * 0.6f)
                    };

                case UnitShape.Wings:
                    return new[]
                    {
                        new Vector2(cx - radius, cy + radius * 0.85f),
                        new Vector2(cx - radius * 0.25f, cy - radius * 0.1f),
                        new Vector2(cx, cy + radius * 0.35f),
                        new Vector2(cx + radius * 0.25f, cy - radius * 0.1f),
                        new Vector2(cx + radius, cy + radius * 0.85f),
                        new Vector2(cx, cy - radius)
                    };

                case UnitShape.Ears:
                    return new[]
                    {
                        new Vector2(cx - radius * 0.35f, cy + radius * 0.45f),
                        new Vector2(cx - radius, cy + radius),
                        new Vector2(cx - radius * 0.75f, cy + radius * 0.1f),
                        new Vector2(cx + radius * 0.75f, cy + radius * 0.1f),
                        new Vector2(cx + radius, cy + radius),
                        new Vector2(cx + radius * 0.35f, cy + radius * 0.45f),
                        new Vector2(cx + radius * 0.6f, cy - radius),
                        new Vector2(cx - radius * 0.6f, cy - radius)
                    };

                default:
                    return BuildStar(cx, cy, radius, radius * 0.55f, 9);
            }
        }

        private static Vector2[] BuildStar(float cx, float cy, float outer, float inner, int points)
        {
            Vector2[] polygon = new Vector2[points * 2];
            float step = Mathf.PI * 2f / polygon.Length;

            for (int i = 0; i < polygon.Length; i++)
            {
                float r = i % 2 == 0 ? outer : inner;
                float angle = i * step + Mathf.PI * 0.5f;
                polygon[i] = new Vector2(cx + Mathf.Cos(angle) * r, cy + Mathf.Sin(angle) * r);
            }

            return polygon;
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
        // Raster helpers
        // ------------------------------------------------------------------
        private static void Fill(Texture2D texture, Color color)
        {
            FillRect(texture, 0, 0, texture.width, texture.height, color);
        }

        private static void FillRect(Texture2D texture, int x0, int y0, int width, int height, Color color)
        {
            for (int y = y0; y < y0 + height; y++)
            {
                for (int x = x0; x < x0 + width; x++)
                {
                    Blend(texture, x, y, color);
                }
            }
        }

        private static void FillCircle(Texture2D texture, float cx, float cy, float radius, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius));
            int maxX = Mathf.Min(texture.width - 1, Mathf.CeilToInt(cx + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius));
            int maxY = Mathf.Min(texture.height - 1, Mathf.CeilToInt(cy + radius));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;

                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        Blend(texture, x, y, color);
                    }
                }
            }
        }

        private static void FillRoundedRect(Texture2D texture, int x0, int y0, int width, int height, int radius, Color color)
        {
            int r = Mathf.Clamp(radius, 0, Mathf.Min(width, height) / 2);
            int maxX = x0 + width - 1;
            int maxY = y0 + height - 1;

            for (int y = y0; y <= maxY; y++)
            {
                for (int x = x0; x <= maxX; x++)
                {
                    bool inside = true;

                    if (x < x0 + r && y < y0 + r)
                    {
                        inside = IsInsideCorner(x, y, x0 + r, y0 + r, r);
                    }
                    else if (x > maxX - r && y < y0 + r)
                    {
                        inside = IsInsideCorner(x, y, maxX - r, y0 + r, r);
                    }
                    else if (x < x0 + r && y > maxY - r)
                    {
                        inside = IsInsideCorner(x, y, x0 + r, maxY - r, r);
                    }
                    else if (x > maxX - r && y > maxY - r)
                    {
                        inside = IsInsideCorner(x, y, maxX - r, maxY - r, r);
                    }

                    if (inside)
                    {
                        Blend(texture, x, y, color);
                    }
                }
            }
        }

        private static void StrokeRoundedRect(Texture2D texture, int x0, int y0, int width, int height,
            int radius, int thickness, Color color)
        {
            int maxX = x0 + width - 1;
            int maxY = y0 + height - 1;

            for (int y = y0 - thickness; y <= maxY + thickness; y++)
            {
                for (int x = x0 - thickness; x <= maxX + thickness; x++)
                {
                    bool inOuter = IsInsideRoundedRect(x, y, x0 - thickness, y0 - thickness, width + thickness * 2, height + thickness * 2, radius + thickness);
                    bool inInner = IsInsideRoundedRect(x, y, x0, y0, width, height, radius);

                    if (inOuter && !inInner)
                    {
                        Blend(texture, x, y, color);
                    }
                }
            }
        }

        private static bool IsInsideRoundedRect(int x, int y, int x0, int y0, int width, int height, int radius)
        {
            int maxX = x0 + width - 1;
            int maxY = y0 + height - 1;

            if (x < x0 || x > maxX || y < y0 || y > maxY)
            {
                return false;
            }

            int r = Mathf.Clamp(radius, 0, Mathf.Min(width, height) / 2);

            if (x < x0 + r && y < y0 + r)
            {
                return IsInsideCorner(x, y, x0 + r, y0 + r, r);
            }

            if (x > maxX - r && y < y0 + r)
            {
                return IsInsideCorner(x, y, maxX - r, y0 + r, r);
            }

            if (x < x0 + r && y > maxY - r)
            {
                return IsInsideCorner(x, y, x0 + r, maxY - r, r);
            }

            if (x > maxX - r && y > maxY - r)
            {
                return IsInsideCorner(x, y, maxX - r, maxY - r, r);
            }

            return true;
        }

        private static bool IsInsideCorner(int x, int y, int cx, int cy, int radius)
        {
            float dx = x + 0.5f - cx;
            float dy = y + 0.5f - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        /// <summary>Fills a polygon, optionally with a 1.08x scaled outline behind it.</summary>
        private static void DrawPolygon(Texture2D texture, Vector2[] polygon, Color fill, Color outline)
        {
            if (polygon == null || polygon.Length < 3)
            {
                return;
            }

            Vector2 center = Centroid(polygon);

            if (outline.a > 0f)
            {
                DrawPolygonRaw(texture, ScalePolygon(polygon, center, 1.08f), outline);
            }

            DrawPolygonRaw(texture, polygon, fill);
        }

        private static void DrawPolygonRaw(Texture2D texture, Vector2[] polygon, Color color)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            for (int i = 0; i < polygon.Length; i++)
            {
                minX = Mathf.Min(minX, polygon[i].x);
                maxX = Mathf.Max(maxX, polygon[i].x);
                minY = Mathf.Min(minY, polygon[i].y);
                maxY = Mathf.Max(maxY, polygon[i].y);
            }

            int x0 = Mathf.Max(0, Mathf.FloorToInt(minX));
            int x1 = Mathf.Min(texture.width - 1, Mathf.CeilToInt(maxX));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(minY));
            int y1 = Mathf.Min(texture.height - 1, Mathf.CeilToInt(maxY));

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (IsInsidePolygon(polygon, x + 0.5f, y + 0.5f))
                    {
                        Blend(texture, x, y, color);
                    }
                }
            }
        }

        /// <summary>Even-odd point-in-polygon test.</summary>
        private static bool IsInsidePolygon(Vector2[] polygon, float px, float py)
        {
            bool inside = false;

            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                bool crosses = polygon[i].y > py != polygon[j].y > py;

                if (crosses &&
                    px < (polygon[j].x - polygon[i].x) * (py - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static Vector2[] ScalePolygon(Vector2[] polygon, Vector2 center, float scale)
        {
            Vector2[] scaled = new Vector2[polygon.Length];

            for (int i = 0; i < polygon.Length; i++)
            {
                scaled[i] = center + (polygon[i] - center) * scale;
            }

            return scaled;
        }

        private static Vector2 Centroid(Vector2[] polygon)
        {
            Vector2 sum = Vector2.zero;

            for (int i = 0; i < polygon.Length; i++)
            {
                sum += polygon[i];
            }

            return sum / polygon.Length;
        }

        private static void Blend(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height || color.a <= 0f)
            {
                return;
            }

            if (color.a >= 1f)
            {
                texture.SetPixel(x, y, color);
                return;
            }

            Color existing = texture.GetPixel(x, y);
            float alpha = color.a + existing.a * (1f - color.a);

            if (alpha <= 0f)
            {
                texture.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                return;
            }

            Color result = (color * color.a + existing * existing.a * (1f - color.a)) / alpha;
            result.a = alpha;
            texture.SetPixel(x, y, result);
        }

        private static Color Lighten(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                color.a);
        }

        private static Color Darken(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r * (1f - amount)),
                Mathf.Clamp01(color.g * (1f - amount)),
                Mathf.Clamp01(color.b * (1f - amount)),
                color.a);
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