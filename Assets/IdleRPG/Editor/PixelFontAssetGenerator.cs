using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Turns the downloaded pixel fonts into TextMeshPro font assets.
    ///
    /// Version 2 (2026-09-29): SDF instead of raster, sampled at 64px.
    ///
    /// Why this changed: the game's canvas re-scales to fit any phone (ScaleWithScreenSize), so the scale
    /// factor is almost never a whole number. A RASTER (bitmap) font is crisp only at integer scaling - at
    /// the Game view's 0.667x it smeared ("garbled"), and real devices would do the same. SDF is smooth at
    /// ANY size, so the text is legible everywhere; the retro identity lives in the panels, chips, icons
    /// and backdrops, which are images and scale cleanly.
    ///
    /// A large sampling size (64) keeps the SDF sharp enough that the fonts' square letterforms still read
    /// as "pixel". Display sizes are now free-form (24, 30, 32 ...) - the old "whole multiples only" rule
    /// died with the raster mode.
    ///
    /// Menu: Tools > Idle RPG > Art > Generate Pixel Fonts
    /// </summary>
    public static class PixelFontAssetGenerator
    {
        private const string ThirdPartyFolder = "Assets/ThirdParty";
        private const string OutputFolder = "Assets/IdleRPG/Resources/Fonts";

        private const int AtlasSize = 1024;
        private const int SamplingPointSize = 64;

        /// <summary>
        /// One download to convert. The source file name and the TMP asset name no longer have to match: the
        /// Abaddon pack ships "Abaddon Bold.ttf" / "Abaddon Light.ttf", where the file name carries the weight.
        /// </summary>
        private struct FontSource
        {
            public string Path;
            public string OutputName;

            /// <summary>Atlas edge in pixels; 0 keeps <see cref="AtlasSize"/>.</summary>
            public int Atlas;

            /// <summary>
            /// A dark rim drawn with the glyphs, 0 for none. It lives on the font's own material, so every label
            /// that uses the font gets it without a second place to wire. 0.2 is about one screen pixel at the
            /// section size - a rim, not a weight.
            /// </summary>
            public float OutlineWidth;
        }

        private static readonly FontSource[] Fonts =
        {
            new FontSource { Path = ThirdPartyFolder + "/PixelFonts/Jersey10-Regular.ttf", OutputName = "Jersey10 Pixel" },
            new FontSource { Path = ThirdPartyFolder + "/Abaddon_Fonts_v1.2/Abaddon Light.ttf", OutputName = "Abaddon Light Pixel" },

            // The chrome face (2026-11-23). Tall and wide: cap height 0.704 em against Abaddon's 0.625, so the
            // sizes in UiType are smaller than they were - same letter height, not smaller text. 2048 because the
            // face is large on the body and now carries a rim.
            new FontSource
            {
                Path = ThirdPartyFolder + "/LilitaOne/LilitaOne-Regular.ttf",
                OutputName = "Lilita One",
                Atlas = 2048,
                OutlineWidth = 0.2f,
            },
        };

        [MenuItem("Tools/Idle RPG/Art/Generate Pixel Fonts", priority = 22)]
        public static void GenerateAll()
        {
            Generate(force: false);
        }

        /// <summary>
        /// Rebuilds every font asset from its TTF, DELETING the existing ones first. A deleted asset gets a new
        /// GUID, so every scene and prefab that pointed at it loses the font. Only use this when the font assets
        /// themselves are broken or the sampling settings changed.
        /// </summary>
        [MenuItem("Tools/Idle RPG/Art/Generate Pixel Fonts (force rebuild)", priority = 24)]
        public static void GenerateAllForced()
        {
            Generate(force: true);
        }

        private static void Generate(bool force)
        {
            EnsureFolder(OutputFolder);

            int built = 0;

            foreach (FontSource entry in Fonts)
            {
                Font source = AssetDatabase.LoadAssetAtPath<Font>(entry.Path);

                if (source == null)
                {
                    Debug.LogWarning($"[PixelFontAssetGenerator] Missing font source '{entry.Path}'.");
                    continue;
                }

                string assetPath = $"{OutputFolder}/{entry.OutputName}.asset";

                if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null)
                {
                    if (!force)
                    {
                        // Keep the existing asset, and with it its GUID: every scene and prefab that points at it
                        // stays wired. Recreating it would silently leave those references empty.
                        Debug.Log($"[PixelFontAssetGenerator] '{entry.OutputName}' already exists - kept as it is.");
                        continue;
                    }

                    AssetDatabase.DeleteAsset(assetPath);
                }

                int atlas = entry.Atlas > 0 ? entry.Atlas : AtlasSize;

                TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                    source,
                    SamplingPointSize,
                    7,                                  // SDF padding (TMP default for SDFAA)
                    GlyphRenderMode.SDFAA,
                    atlas,
                    atlas,
                    AtlasPopulationMode.Dynamic,
                    true);

                if (fontAsset == null)
                {
                    Debug.LogError($"[PixelFontAssetGenerator] TMP refused to build '{entry.Path}'.");
                    continue;
                }

                fontAsset.name = entry.OutputName;
                fontAsset.isMultiAtlasTexturesEnabled = true;

                if (entry.OutlineWidth > 0f)
                {
                    // Set on the font's own material, before the asset is written, so every label that uses the
                    // font draws the rim and nothing has to remember to ask for it.
                    Material material = fontAsset.material;
                    material.EnableKeyword(ShaderUtilities.Keyword_Outline);
                    material.SetFloat(ShaderUtilities.ID_OutlineWidth, entry.OutlineWidth);
                    material.SetColor(ShaderUtilities.ID_OutlineColor, IdleRPG.UI.UiTheme.Slate950);
                    EditorUtility.SetDirty(material);
                }

                // Textures and the material are separate objects; they must be added to the same asset
                // file or the font asset saves with dangling references.
                AssetDatabase.CreateAsset(fontAsset, assetPath);
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                AssetDatabase.SaveAssets();

                built++;
            }

            Debug.Log($"[PixelFontAssetGenerator] {built} SDF font asset(s) at {SamplingPointSize}px sampling " +
                      $"in {OutputFolder}. Any display size is now safe - Raster's integer-scale rule is gone.");
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
        }
    }
}