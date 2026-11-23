using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Shrinks the memory every texture in the game costs, without touching the art files themselves.
    ///
    /// ELI5: three rules that make pictures cheaper to hold in memory.
    ///   1. No mipmaps - a mip chain is +33% memory, and this game is 2D UI drawn at screen size, so the smaller
    ///      copies are never used.
    ///   2. A size cap - art bigger than a 1024-wide screen is downscaled on import. Pixel art gains nothing from
    ///      being stored larger than it is drawn.
    ///   3. Compression for the big pictures only - anything over 256x256 is stored compressed (about a quarter of
    ///      the memory); small pixel art stays uncompressed so 9-slice borders and icons stay crisp.
    ///
    /// Why it exists: 733 game PNGs at full size are about 293 MB as raw RGBA, and +33% again with mipmaps. On an
    /// 8 GB machine that is the difference between working and swapping.
    ///
    /// Menu: Tools &gt; Idle RPG &gt; Art &gt; Slim Texture Memory
    /// </summary>
    public static class TextureMemorySlim
    {
        /// <summary>Folders that hold game art. Screenshots and previews are deliberately not here.</summary>
        private static readonly string[] Roots =
        {
            "Assets/IdleRPG/Art",
            "Assets/IdleRPG/Resources",
            "Assets/ThirdParty",
        };

        /// <summary>Longest edge for ordinary art: bigger than any 1080-wide screen needs.</summary>
        private const int SizeCap = 1024;

        /// <summary>Longest edge for full-screen backdrops.</summary>
        private const int BackdropCap = 2048;

        /// <summary>Pictures with more pixels than this are worth compressing; smaller ones are cheaper to keep crisp.</summary>
        private const int CompressAbovePixels = 256 * 256;

        [MenuItem("Tools/Idle RPG/Art/Slim Texture Memory", priority = 26)]
        public static void Run()
        {
            List<string> changed = new List<string>();
            long before = 0;
            long after = 0;

            foreach (string root in Roots)
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { root }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                    if (importer == null)
                    {
                        continue;
                    }

                    importer.GetSourceTextureWidthAndHeight(out int width, out int height);

                    if (width <= 0 || height <= 0)
                    {
                        continue;
                    }

                    int cap = path.Contains("Backdrops") ? BackdropCap : SizeCap;
                    int finalWidth = Mathf.Min(width, cap);
                    int finalHeight = Mathf.Min(height, cap);
                    bool compress = finalWidth * finalHeight > CompressAbovePixels;

                    long wasBytes = Budget(width, height, importer.mipmapEnabled, false);
                    long willBeBytes = Budget(finalWidth, finalHeight, false, compress);

                    before += wasBytes;
                    after += willBeBytes;

                    bool dirty = false;

                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        dirty = true;
                    }

                    if (importer.isReadable)
                    {
                        importer.isReadable = false;
                        dirty = true;
                    }

                    if (importer.maxTextureSize > cap)
                    {
                        importer.maxTextureSize = cap;
                        dirty = true;
                    }

                    if (compress && importer.textureCompression == TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = TextureImporterCompression.CompressedHQ;
                        dirty = true;
                    }

                    if (!dirty)
                    {
                        continue;
                    }

                    changed.Add(path);
                    importer.SaveAndReimport();
                }
            }

            AssetDatabase.Refresh();

            Debug.Log($"[TextureMemorySlim] {changed.Count} texture(s) slimmed. " +
                      $"Estimated texture memory {Mb(before)} -> {Mb(after)} ({Mb(before - after)} saved). " +
                      "Rules: no mipmaps, longest edge " + SizeCap + " (" + BackdropCap + " for backdrops), " +
                      "compressed above 256x256.");
        }

        /// <summary>Rough RGBA cost of a texture, with room for the mip chain when it has one.</summary>
        private static long Budget(int width, int height, bool mips, bool compressed)
        {
            long bytes = (long)width * height * 4;

            if (compressed)
            {
                bytes /= 4;
            }

            if (mips)
            {
                bytes = bytes * 4 / 3;
            }

            return bytes;
        }

        private static string Mb(long bytes)
        {
            return (bytes / (1024f * 1024f)).ToString("0.0") + " MB";
        }
    }
}
