using UnityEditor;
using UnityEngine;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Slices the camp fire sheet into its six frames, once.
    ///
    /// ELI5: cuts one strip of six pictures into six pictures the game can flip through. The cells are cut at a FIXED
    /// size on purpose: the flame is a different height in every frame (42, 40, 46, 43, 41, 43 px), so letting Unity
    /// trim each one would make the fire jump up and down instead of flicker.
    ///
    /// The sheet is 216 x 48 with six 36-wide cells and 2-3 px gutters. Pivot is bottom-centre, so the fire stands on
    /// whatever line it is placed on.
    ///
    /// Menu: Tools &gt; Idle RPG &gt; Art &gt; Slice Campfire
    /// </summary>
    public static class CampfireSlicer
    {
        private const string SheetPath = "Assets/IdleRPG/Art/Camp/campfire.png";

        /// <summary>Cell size in art pixels, measured from the sheet.</summary>
        private const int CellWidth = 36;
        private const int CellHeight = 48;

        /// <summary>How many pictures the strip holds.</summary>
        private const int FrameCount = 6;

        [MenuItem("Tools/Idle RPG/Art/Slice Campfire", priority = 21)]
        public static void Slice()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;

            if (importer == null)
            {
                Debug.LogError("[CampfireSlicer] No sheet at " + SheetPath + ".");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            importer.SetTextureSettings(settings);

            SpriteMetaData[] meta = new SpriteMetaData[FrameCount];

            for (int i = 0; i < FrameCount; i++)
            {
                meta[i] = new SpriteMetaData
                {
                    name = "campfire_" + i,
                    rect = new Rect(i * CellWidth, 0f, CellWidth, CellHeight),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0f),
                    border = Vector4.zero,
                };
            }

            importer.spritesheet = meta;
            importer.SaveAndReimport();

            Sprite[] sprites = LoadFrames();

            Debug.Log($"[CampfireSlicer] Sliced {sprites.Length} frame(s) at {CellWidth}x{CellHeight}, " +
                      "bottom-centre pivot, Point filter, uncompressed.");
        }

        /// <summary>The six frames in order, or an empty array when the sheet is not sliced yet.</summary>
        public static Sprite[] LoadFrames()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SheetPath);
            Sprite[] frames = new Sprite[FrameCount];

            foreach (Object asset in assets)
            {
                if (!(asset is Sprite sprite) || !sprite.name.StartsWith("campfire_"))
                {
                    continue;
                }

                if (int.TryParse(sprite.name.Substring("campfire_".Length), out int index) &&
                    index >= 0 && index < FrameCount)
                {
                    frames[index] = sprite;
                }
            }

            int found = 0;

            foreach (Sprite frame in frames)
            {
                if (frame != null)
                {
                    found++;
                }
            }

            return found == FrameCount ? frames : new Sprite[0];
        }
    }
}
