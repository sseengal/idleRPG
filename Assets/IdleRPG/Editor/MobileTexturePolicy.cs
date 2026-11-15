using UnityEditor;
using UnityEngine;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// One switch for the phone texture format, so 549 .meta files are never hand-edited.
    ///
    /// ELI5: a picture on the phone is stored at 4 bytes per pixel. ASTC 4x4 stores it at 1 byte per pixel and
    /// looks almost the same. This tool rewrites the Android (and iPhone) slot of every art texture to ASTC 4x4.
    /// The Default slot is left alone, so the Editor still shows the true, lossless art.
    ///
    /// Measured on this project: 339 textures used by scenes/assets held 104 MB of pixels, 80 MB of them
    /// uncompressed. ASTC 4x4 brings the whole set to about 26 MB.
    ///
    /// Two menus exist on purpose: one to compress, one to put the old lossless setting back. A change to how the
    /// game looks must be one click away from undone.
    ///
    /// Menu: Tools > Idle RPG > Art > Apply Mobile Texture Compression (ASTC 4x4)
    /// Menu: Tools > Idle RPG > Art > Revert Mobile Textures To Uncompressed
    /// </summary>
    public static class MobileTexturePolicy
    {
        /// <summary>Folders that hold art the game can ship. Editor tools and docs are not scanned.</summary>
        private static readonly string[] Roots = { "Assets/IdleRPG", "Assets/ThirdParty" };

        /// <summary>Mobile slots to rewrite. iOS is kept in step even though Android is the current target.</summary>
        private static readonly string[] MobilePlatforms = { "Android", "iPhone" };

        [MenuItem("Tools/Idle RPG/Art/Apply Mobile Texture Compression (ASTC 4x4)", priority = 22)]
        public static void ApplyCompression()
        {
            Rewrite(true);
        }

        [MenuItem("Tools/Idle RPG/Art/Revert Mobile Textures To Uncompressed", priority = 23)]
        public static void RevertToUncompressed()
        {
            Rewrite(false);
        }

        private static void Rewrite(bool useAstc)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", Roots);
            int scanned = 0, changed = 0;

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    if (i % 25 == 0)
                    {
                        EditorUtility.DisplayProgressBar(
                            useAstc ? "Applying ASTC 4x4" : "Reverting to uncompressed",
                            i + " / " + guids.Length, (float)i / Mathf.Max(1, guids.Length));
                    }

                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                    if (string.IsNullOrEmpty(path) || !(AssetImporter.GetAtPath(path) is TextureImporter importer))
                    {
                        continue;
                    }

                    // UI sprite atlases and cubemaps are not plain 2D art; leave them to their own tools.
                    if (importer.textureShape != TextureImporterShape.Texture2D)
                    {
                        continue;
                    }

                    scanned++;
                    bool touched = false;

                    for (int p = 0; p < MobilePlatforms.Length; p++)
                    {
                        TextureImporterPlatformSettings current =
                            importer.GetPlatformTextureSettings(MobilePlatforms[p]);

                        TextureImporterFormat wantedFormat = useAstc
                            ? TextureImporterFormat.ASTC_4x4
                            : TextureImporterFormat.RGBA32;

                        TextureImporterCompression wantedCompression = useAstc
                            ? TextureImporterCompression.Compressed
                            : TextureImporterCompression.Uncompressed;

                        if (current.overridden &&
                            current.format == wantedFormat &&
                            current.textureCompression == wantedCompression)
                        {
                            continue;
                        }

                        var settings = new TextureImporterPlatformSettings
                        {
                            name = MobilePlatforms[p],
                            overridden = true,
                            // Never resize here: a 100 px slicing grid is measured in source pixels.
                            maxTextureSize = current.overridden && current.maxTextureSize > 0
                                ? current.maxTextureSize
                                : importer.maxTextureSize,
                            format = wantedFormat,
                            textureCompression = wantedCompression,
                            compressionQuality = 100,
                            crunchedCompression = false,
                            allowsAlphaSplitting = false,
                        };

                        importer.SetPlatformTextureSettings(settings);
                        touched = true;
                    }

                    if (touched)
                    {
                        importer.SaveAndReimport();
                        changed++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
            }

            Debug.Log("[MobileTexturePolicy] " + (useAstc ? "ASTC 4x4 applied" : "Uncompressed restored") +
                      ": " + changed + " texture(s) rewritten, " + scanned + " 2D texture(s) scanned.");
        }
    }
}
