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
        private const string SourceFolder = "Assets/ThirdParty/PixelFonts";
        private const string OutputFolder = "Assets/IdleRPG/Resources/Fonts";

        private const int AtlasSize = 1024;
        private const int SamplingPointSize = 64;

        private static readonly string[] Fonts = { "Silkscreen", "VT323", "Jersey10" };

        [MenuItem("Tools/Idle RPG/Art/Generate Pixel Fonts", priority = 22)]
        public static void GenerateAll()
        {
            EnsureFolder(OutputFolder);

            int built = 0;

            foreach (string name in Fonts)
            {
                string sourcePath = SourceFolder + "/" + name + "-Regular.ttf";
                Font source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);

                if (source == null)
                {
                    Debug.LogWarning($"[PixelFontAssetGenerator] Missing font source '{sourcePath}'.");
                    continue;
                }

                string assetPath = $"{OutputFolder}/{name} Pixel.asset";

                if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }

                TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                    source,
                    SamplingPointSize,
                    7,                                  // SDF padding (TMP default for SDFAA)
                    GlyphRenderMode.SDFAA,
                    AtlasSize,
                    AtlasSize,
                    AtlasPopulationMode.Dynamic,
                    true);

                if (fontAsset == null)
                {
                    Debug.LogError($"[PixelFontAssetGenerator] TMP refused to build '{sourcePath}'.");
                    continue;
                }

                fontAsset.name = name + " Pixel";
                fontAsset.isMultiAtlasTexturesEnabled = true;

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