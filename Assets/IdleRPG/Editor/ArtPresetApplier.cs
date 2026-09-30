using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// One-click import normaliser for third-party pixel-art character packs.
    ///
    /// Per file it: forces the pixel-art preset (Point filter, no mips, uncompressed, sRGB, clamped), slices
    /// horizontal frame strips into sprites, TRIMS each sprite to the art instead of the padded cell, and derives
    /// pixels-per-unit and the pivot from the art itself.
    ///
    /// Why trimming and derived values matter for these packs: the art is tiny inside a 100x100 cell (the Soldier
    /// is 21 px tall), so a full-cell sprite would be an 8 world-unit quad with a small character floating inside it
    /// - oversized for sorting and overdraw, and with a pivot that is nowhere near the feet. Measuring the union of
    /// every frame's alpha bounds gives one rect and one pivot shared by the whole animation (so a walk cycle cannot
    /// jitter) with the feet on the pivot line and PPU chosen so the character lands at its intended world height.
    ///
    /// Variants whose folder name contains "with shadows" are skipped: they bake a fake drop shadow into the art and
    /// we cast real ones.
    ///
    /// Menu: Tools > Idle RPG > Art > Apply Pixel-Art Presets
    /// </summary>
    public static class ArtPresetApplier
    {
        /// <summary>Folders scanned for character art. Third-party packs land here.</summary>
        private static readonly string[] Roots = { "Assets/ThirdParty" };

        /// <summary>Cell size of the frame strips in these packs.</summary>
        private const int FrameSize = 100;

        /// <summary>
        /// World height each character should end up being, in metres. PPU is derived from this, so a unit can be
        /// dropped into a scene at scale 1 and already be the right size.
        /// </summary>
        private static readonly Dictionary<string, float> DesiredWorldHeight = new Dictionary<string, float>
        {
            { "Soldier", 1.7f },
            { "Orc", 1.2f },
            { "Goblin", 1.15f },
            { "CuteGoblin", 1.0f },
            { "ChunkyGoblin", 1.15f },
            { "StockyGoblin", 1.15f },
        };

        private const float DefaultWorldHeight = 1.5f;
        private const byte AlphaThreshold = 16;

        [MenuItem("Tools/Idle RPG/Art/Apply Pixel-Art Presets", priority = 20)]
        public static void ApplyAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", Roots);

            int textures = 0;
            int frames = 0;
            var report = new List<string>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (path.Contains("with shadows"))
                {
                    continue;
                }

                // UI art is not character art: it is never a frame strip, must keep a 9-slice border and
                // is sized in canvas pixels, not metres. UiArtPresetApplier owns those files.
                if (path.Contains("Kenney_PixelUI"))
                {
                    continue;
                }

                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    continue;
                }

                SpriteArt art = Measure(path);

                if (art.ArtHeightPx <= 0)
                {
                    Debug.LogWarning($"[ArtPresetApplier] '{path}' has no opaque pixels; skipped.");
                    continue;
                }

                string character = CharacterName(path);
                float worldHeight = DesiredWorldHeight.TryGetValue(character, out float height) ? height : DefaultWorldHeight;
                float ppu = art.ArtHeightPx / worldHeight;

                ApplyPreset(importer, art, ppu, path);

                textures++;
                frames += art.CellCount;
                report.Add($"{Path.GetFileNameWithoutExtension(path)}: {art.CellCount} frame(s), art {art.Union.width:0}x{art.Union.height:0}px, " +
                           $"PPU {ppu:0.0} -> {worldHeight:0.0}m tall");
            }

            AssetDatabase.Refresh();

            foreach (string line in report)
            {
                Debug.Log("[ArtPresetApplier] " + line);
            }

            Debug.Log($"[ArtPresetApplier] {textures} texture(s), {frames} frame(s) processed from {string.Join(", ", Roots)}.");
        }

        private static void ApplyPreset(TextureImporter importer, SpriteArt art, float ppu, string path)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;

            string baseName = Path.GetFileNameWithoutExtension(path);
            var frames = new SpriteMetaData[art.CellCount];

            for (int i = 0; i < art.CellCount; i++)
            {
                // Same trimmed rect for every frame of the strip: no jitter during an animation.
                frames[i] = new SpriteMetaData
                {
                    name = art.CellCount > 1 ? $"{baseName}_{i}" : baseName,
                    rect = new Rect((i * art.CellWidth) + art.Union.x, art.Union.y, art.Union.width, art.Union.height),
                    alignment = (int)SpriteAlignment.Custom,
                    // Bottom of the trimmed rect is the feet line, so the pivot is the ground contact point.
                    pivot = new Vector2(0.5f, 0f),
                };
            }

            importer.spritesheet = frames;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Per-cell alpha bounds for every frame, unioned so all frames share one rect and one pivot. The PNG bytes are
        /// read directly, so this works before the importer has ever sliced anything.
        /// </summary>
        private static SpriteArt Measure(string path)
        {
            var art = new SpriteArt { CellCount = 1, CellWidth = FrameSize, Union = new Rect(0, 0, 0, 0), ArtHeightPx = 0 };

            byte[] bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (!texture.LoadImage(bytes))
            {
                Object.DestroyImmediate(texture);
                return art;
            }

            bool isStrip = false;
            int cellSize = texture.width;

            // A strip is one uniform row of N square cells (N >= 2). This catches the Tiny pack's 100px cells and
            // PixelLab animation sheets at any cell size (e.g. 92px goblin frames). Single images fall through.
            int cols = texture.width / texture.height;

            if (texture.height >= 16 && texture.width % texture.height == 0 && cols >= 2)
            {
                isStrip = true;
                cellSize = texture.height;
            }

            int cellCount = isStrip ? cols : 1;
            int cellWidth = isStrip ? cellSize : texture.width;

            Color32[] pixels = texture.GetPixels32();
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            for (int cell = 0; cell < cellCount; cell++)
            {
                int originX = cell * cellWidth;

                for (int y = 0; y < texture.height; y++)
                {
                    for (int x = 0; x < cellWidth; x++)
                    {
                        if (pixels[(y * texture.width) + originX + x].a <= AlphaThreshold)
                        {
                            continue;
                        }

                        if (x < minX) { minX = x; }
                        if (x > maxX) { maxX = x; }
                        if (y < minY) { minY = y; }
                        if (y > maxY) { maxY = y; }
                    }
                }
            }

            if (maxX >= minX && maxY >= minY)
            {
                art.CellCount = cellCount;
                art.CellWidth = cellWidth;
                art.Union = new Rect(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
                art.ArtHeightPx = (maxY - minY) + 1;
            }

            Object.DestroyImmediate(texture);
            return art;
        }

        private static string CharacterName(string path)
        {
            string file = Path.GetFileNameWithoutExtension(path);

            foreach (string key in DesiredWorldHeight.Keys)
            {
                if (file.StartsWith(key, System.StringComparison.OrdinalIgnoreCase))
                {
                    return key;
                }
            }

            return file;
        }

        private struct SpriteArt
        {
            public int CellCount;
            public int CellWidth;
            public Rect Union;
            public int ArtHeightPx;
        }
    }
}
