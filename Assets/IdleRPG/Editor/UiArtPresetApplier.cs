using UnityEditor;
using UnityEngine;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Import policy for UI art (panels, buttons, icons). Companion to <see cref="ArtPresetApplier"/>,
    /// which owns character art: character packs are sliced into frame strips and sized in metres, UI art
    /// is a single sprite sized in canvas pixels, so the two must never share a preset.
    ///
    /// Policy (Docs/Art-Pipeline.md §17):
    /// - Point filter, no mipmaps, alpha as transparency, clamp - pixel art must stay sharp.
    /// - Uncompressed. A 48x48 panel with a 1px outline cannot survive block compression.
    /// - Single sprite, centre pivot, PPU 100 (matches the placeholder art the UI was built against).
    /// - 9-slice border 12 for everything in a "9-Slice" folder. Measured 2026-09-29: the outline is
    ///   1-2 px and the most decorated family (Ancient) keeps its edge detail inside 12 px, so a bigger
    ///   border would only steal middle area from small chips.
    ///
    /// Menu: Tools > Idle RPG > Art > Apply UI-Art Presets
    /// </summary>
    public static class UiArtPresetApplier
    {
        /// <summary>Root scanned for UI art. New UI packs are imported by adding their folder here.</summary>
        public const string UiArtRoot = "Assets/ThirdParty/Kenney_PixelUI";

        /// <summary>Matches the placeholder art, so swapping art never changes a layout.</summary>
        private const float PixelsPerUnit = 100f;

        /// <summary>9-slice border in source pixels. See the class summary for how this number was measured.</summary>
        private const int SliceBorder = 12;

        private const int MaxTextureSize = 2048;

        [MenuItem("Tools/Idle RPG/Art/Apply UI-Art Presets", priority = 21)]
        public static void ApplyAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { UiArtRoot });

            int textures = 0;
            int sliced = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // The pack's own poster, not a UI asset.
                if (path.EndsWith("Preview.png"))
                {
                    continue;
                }

                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    continue;
                }

                bool isSlicePiece = path.Contains("/9-Slice/");

                // FullRect mesh: sliced UI never needs a tight outline mesh, and it keeps geometry predictable.
                var importerSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(importerSettings);
                importerSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(importerSettings);

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.sRGBTexture = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = MaxTextureSize;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.spriteBorder = isSlicePiece
                    ? new Vector4(SliceBorder, SliceBorder, SliceBorder, SliceBorder)
                    : Vector4.zero;

                importer.SaveAndReimport();

                textures++;
                if (isSlicePiece)
                {
                    sliced++;
                }
            }

            Debug.Log($"[UiArtPresetApplier] UI preset applied to {textures} texture(s) under {UiArtRoot} " +
                      $"({sliced} with a {SliceBorder}px 9-slice border).");
        }
    }
}
