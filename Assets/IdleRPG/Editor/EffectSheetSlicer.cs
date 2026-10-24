using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Slices an effect strip (one row of square cells) into named sprites so a view can play it frame by frame.
    ///
    /// ELI5: the artist draws an explosion as one long picture with N squares side by side. Unity sees one picture.
    /// This cuts it into N pictures named "ice_shatter_0", "ice_shatter_1", ... so the game can flip through them.
    ///
    /// Only textures under Resources/Vfx are touched, and only those still imported as a single sprite.
    /// </summary>
    public static class EffectSheetSlicer
    {
        private const string VfxFolder = "Assets/IdleRPG/Resources/Vfx";
        private const int CellSize = 100;

        [MenuItem("Tools/Idle RPG/Art/Slice VFX Sheets")]
        public static void SliceMenu()
        {
            int sliced = SliceFolder(VfxFolder);
            Debug.Log(sliced > 0
                ? $"[EffectSheetSlicer] Sliced {sliced} sheet(s) in {VfxFolder}."
                : $"[EffectSheetSlicer] Nothing to do - every sheet in {VfxFolder} is already sliced.");
        }

        /// <summary>Returns how many textures were converted.</summary>
        public static int SliceFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogWarning($"[EffectSheetSlicer] Folder not found: {folder}");
                return 0;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            int sliced = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (SliceOne(path))
                {
                    sliced++;
                }
            }

            AssetDatabase.Refresh();
            return sliced;
        }

        private static bool SliceOne(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                return false;
            }

            // Already a real sprite sheet? Nothing to do. A texture with spriteMode=Multiple but textureType=Default
            // has NO sprites at all (Resources.LoadAll returns 0), so that case must be fixed here too.
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Multiple)
            {
                return false;
            }

            // The true size comes from the PNG header, not from the imported texture: Unity rescales a non-power-of-two
            // sheet to the next power of two by default (1000 px became 1024), which would put every cut in the wrong
            // place. Force "no rescale" first, then read the texture back.
            if (!TryReadPngSize(path, out int sourceWidth, out int sourceHeight))
            {
                Debug.LogWarning($"[EffectSheetSlicer] Could not read the PNG size of {path}. Skipped.");
                return false;
            }

            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureType = TextureImporterType.Sprite;
            importer.maxTextureSize = Mathf.Max(2048, Mathf.NextPowerOfTwo(Mathf.Max(sourceWidth, sourceHeight)));
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                return false;
            }

            int columns = sourceWidth / CellSize;
            int rows = sourceHeight / CellSize;

            if (columns <= 0 || rows <= 0 || sourceWidth % CellSize != 0 || sourceHeight % CellSize != 0)
            {
                Debug.LogWarning($"[EffectSheetSlicer] {path} is {sourceWidth}x{sourceHeight}; not a clean {CellSize}px grid. Skipped.");
                return false;
            }

            if (texture.width != sourceWidth || texture.height != sourceHeight)
            {
                Debug.LogWarning($"[EffectSheetSlicer] {path} imported at {texture.width}x{texture.height}, expected {sourceWidth}x{sourceHeight}. Skipped.");
                return false;
            }

            string baseName = Path.GetFileNameWithoutExtension(path);
            var sprites = new List<SpriteMetaData>();

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    sprites.Add(new SpriteMetaData
                    {
                        name = $"{baseName}_{row * columns + column}",
                        rect = new Rect(column * CellSize, sourceHeight - (row + 1) * CellSize, CellSize, CellSize),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f)
                    });
                }
            }

            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritesheet = sprites.ToArray();
            importer.SaveAndReimport();

            Debug.Log($"[EffectSheetSlicer] {baseName}: {sprites.Count} frame(s) from {sourceWidth}x{sourceHeight}.");
            return true;
        }

        /// <summary>Reads width/height from the PNG IHDR chunk (bytes 16-23), so importer scaling cannot lie to us.</summary>
        private static bool TryReadPngSize(string path, out int width, out int height)
        {
            width = 0;
            height = 0;

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                if (bytes.Length < 24 || bytes[0] != 0x89 || bytes[1] != 0x50)
                {
                    return false;
                }

                width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
                height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
                return width > 0 && height > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
