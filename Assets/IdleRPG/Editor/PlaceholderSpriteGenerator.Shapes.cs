using System.IO;
using UnityEditor;
using UnityEngine;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Pixel and polygon drawing primitives: shapes, fills, rounded rects, blending and colour helpers.
    /// </summary>
    public static partial class PlaceholderSpriteGenerator
    {
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
    }
}
