using UnityEditor;
using UnityEngine;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Import policy for the action-button and panel art. One tool, one table: an artist drops a PNG in the
    /// folder, runs this menu item, and the sprite is ready the same way every time.
    ///
    /// Why not the shared UI preset (<see cref="UiArtPresetApplier"/>): that one is built for the Kenney kit's
    /// 12 px border. These two art sets have their own measured borders, and their 1 px lines must stay
    /// UNCOMPRESSED on devices - ASTC 4x4 turns a hairline into mush, and each texture is tiny.
    ///
    /// Menu: Tools > Idle RPG > Art > Apply UI Art Presets
    /// </summary>
    public static class UiArtPreset
    {
        /// <summary>One folder and the border measured from its art, as (left, bottom, right, top) source pixels.</summary>
        private struct ArtSet
        {
            public string Folder;
            public Vector4 Border;
            public string What;
        }

        private static readonly ArtSet[] Sets =
        {
            // 89x19 bar: 1 px highlight, 15 px face, 3 px shade, 1 px right edge.
            new ArtSet
            {
                Folder = "Assets/IdleRPG/Art/UI/Buttons",
                Border = new Vector4(1f, 3f, 1f, 1f),
                What = "action buttons"
            },
            // 75x75 frame: the rim is 5 px on every side (measured: the face starts at 5 and ends at 69).
            new ArtSet
            {
                Folder = "Assets/IdleRPG/Art/UI/Frames",
                Border = new Vector4(5f, 5f, 5f, 5f),
                What = "panels"
            },
            // 95x23 ribbon: the folded tails are the end caps, the middle plate stretches. No vertical slice.
            new ArtSet
            {
                Folder = "Assets/IdleRPG/Art/UI/Banners",
                Border = new Vector4(8f, 0f, 8f, 0f),
                What = "banners"
            },
            // Tab icons: plain sprites, never sliced. Same import policy so they stay crisp and uncompressed.
            new ArtSet
            {
                Folder = "Assets/IdleRPG/Art/UI/Icons",
                Border = Vector4.zero,
                What = "icons"
            },
            // 82x17 container: flat face with a 1 px dark bottom line. Only that line is a fixed slice.
            new ArtSet
            {
                Folder = "Assets/IdleRPG/Art/UI/Containers",
                Border = new Vector4(0f, 1f, 0f, 0f),
                What = "containers"
            },
        };

        private const float PixelsPerUnit = 100f;
        private const int MaxTextureSize = 2048;

        private static readonly string[] MobilePlatforms = { "Android", "iPhone" };

        [MenuItem("Tools/Idle RPG/Art/Apply UI Art Presets", priority = 21)]
        public static void ApplyAll()
        {
            int total = 0;

            for (int s = 0; s < Sets.Length; s++)
            {
                ArtSet set = Sets[s];
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { set.Folder });

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    {
                        continue;
                    }

                    Apply(importer, set.Border);
                    importer.SaveAndReimport();
                    total++;
                }

                Debug.Log($"[UiArtPreset] {guids.Length} {set.What} in {set.Folder}, border {set.Border}.");
            }

            Debug.Log($"[UiArtPreset] Applied to {total} sprite(s): FullRect, Point, uncompressed, RGBA32 on Android/iOS.");
        }

        private static void Apply(TextureImporter importer, Vector4 border)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

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
            importer.spriteBorder = border;

            // Keep the 1 px lines crisp on the phone. Uncompressed RGBA32, no resize.
            for (int i = 0; i < MobilePlatforms.Length; i++)
            {
                TextureImporterPlatformSettings platform = importer.GetPlatformTextureSettings(MobilePlatforms[i]);
                platform.overridden = true;
                platform.maxTextureSize = MaxTextureSize;
                platform.format = TextureImporterFormat.RGBA32;
                platform.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(platform);
            }
        }
    }
}
