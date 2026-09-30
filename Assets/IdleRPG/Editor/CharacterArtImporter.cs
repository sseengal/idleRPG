using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using IdleRPG.Data;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Imports the TinyRPG character strips: each `<Char>_<Action>.png` sheet (N x 100px) gets Point/NoMips/
    /// Uncompressed/PPU 100 settings and is sliced on the 100px grid into named sprites, then the per-character
    /// <see cref="CharacterArtSet"/> asset is (re)generated from those slices.
    /// Re-running after an art update refreshes the slices and the arrays - safe to run any time.
    /// </summary>
    public static class CharacterArtImporter
    {
        private const string Root = "Assets/IdleRPG/Art/Characters/TinyRPG";
        private const string OutputDir = "Assets/IdleRPG/Data/Characters";
        private const int Tile = 100;

        private static readonly string[] Characters = { "Soldier", "Orc", "Demon_A", "Blood Monster_A" };
        private static readonly string[] Actions = { "Idle", "Walk", "Attack01", "Attack02", "Attack03", "Hurt", "Death" };

        [MenuItem("Tools/Idle RPG/Art/Import TinyRPG Characters")]
        public static void ImportAll()
        {
            if (!AssetDatabase.IsValidFolder(OutputDir))
            {
                AssetDatabase.CreateFolder("Assets/IdleRPG/Data", "Characters");
            }

            foreach (string character in Characters)
            {
                ImportCharacter(character);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CharacterArtImporter] imported: " + string.Join(", ", Characters));
        }

        private static void ImportCharacter(string character)
        {
            string dir = (Root + "/" + character).Replace('/', Path.DirectorySeparatorChar);

            if (!Directory.Exists(dir))
            {
                Debug.LogWarning("[CharacterArtImporter] missing folder " + dir);
                return;
            }

            // Per-action strips only (the bare <Char>.png combined sheet is point-imported, not sliced).
            foreach (string strip in Directory.GetFiles(dir, character + "_*.png"))
            {
                string fileName = Path.GetFileNameWithoutExtension(strip);
                string action = fileName.Substring(character.Length + 1);

                if (!IsAction(action))
                {
                    continue;
                }

                SliceStrip(ToAssetPath(strip));
            }

            string sheet = dir + Path.DirectorySeparatorChar + character + ".png";
            if (File.Exists(sheet))
            {
                PointImport(ToAssetPath(sheet));
            }

            RebuildArtSet(character);
        }

        private static bool IsAction(string action)
        {
            for (int i = 0; i < Actions.Length; i++)
            {
                if (Actions[i] == action)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SliceStrip(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }

            if (importer == null)
            {
                Debug.LogWarning("[CharacterArtImporter] no importer for " + path);
                return;
            }

            // Width straight from the PNG header (bytes 16-19, big-endian), so the slice count can never
            // depend on a stale imported-texture cache.
            int fileWidth = ReadPngWidth(path);
            int cols = fileWidth > 0 ? fileWidth / Tile : 1;

            // The 100x100 pack tiles carry a lot of empty margin (character body is only ~20% of the tile),
            // so sliced sprites render tiny in the unit slots. Crop every strip to the union of the opaque
            // bounds over ALL frames of that action - one shared window per action keeps the animation stable
            // and makes the character fill the slot like the tight placeholder sprites did.
            Rect crop = ComputeCropWindow(path, cols);

            var metas = new List<SpriteMetaData>();
            string baseName = Path.GetFileNameWithoutExtension(path);

            for (int i = 0; i < cols; i++)
            {
                var meta = new SpriteMetaData
                {
                    name = baseName + "_" + i,
                    rect = new Rect(i * Tile, 0, Tile, Tile),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                };
                metas.Add(meta);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = Tile;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            // Slice rects use the shared crop window; a tiny 2px pad keeps the AA edge alive.
            for (int i = 0; i < metas.Count; i++)
            {
                UnityEditor.SpriteMetaData md = metas[i];
                md.rect = new Rect(i * Tile + crop.x, crop.y, Mathf.Max(4f, crop.width), Mathf.Max(4f, crop.height));
                metas[i] = md;
            }

            importer.spritesheet = metas.ToArray();

            (importer as AssetImporter).SaveAndReimport();
        }

        /// <summary>
        /// Union of nontransparent pixels across the whole strip (x range then y range), clamped to the tile.
        /// Shadows are semi-transparent so the alpha bar is deliberately low (>= 12) to include them.
        /// </summary>
        private static Rect ComputeCropWindow(string assetPath, int cols)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            bool wasReadable = importer != null && importer.isReadable;

            if (importer == null || !importer.isReadable)
            {
                if (importer != null)
                {
                    importer.isReadable = true;
                    (importer as AssetImporter).SaveAndReimport();
                }
                else
                {
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                    importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer != null)
                    {
                        importer.isReadable = true;
                        (importer as AssetImporter).SaveAndReimport();
                    }
                }
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            Rect window = new Rect(0f, 0f, Tile, Tile);

            if (tex != null && tex.isReadable)
            {
                Color[] px = tex.GetPixels();
                int w = tex.width;
                int x0 = Tile, y0 = Tile, x1 = -1, y1 = -1;

                for (int y = 0; y < Tile && y < tex.height; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (px[y * w + x].a < 12f / 255f)
                        {
                            continue;
                        }

                        int col = x % Tile;
                        if (col < x0) x0 = col;
                        if (col > x1) x1 = col;
                        if (y < y0) y0 = y;
                        if (y > y1) y1 = y;
                    }
                }

                if (x1 >= x0 && y1 >= y0)
                {
                    int pad = 2;
                    int cx = Mathf.Max(0, x0 - pad);
                    int cy = Mathf.Max(0, y0 - pad);
                    int cw = Mathf.Min(Tile, x1 - x0 + 1 + pad * 2);
                    int ch = Mathf.Min(Tile, y1 - y0 + 1 + pad * 2);
                    window = new Rect(cx, cy, cw, ch);
                }

                UnityEngine.Object.DestroyImmediate(tex);
            }

            if (!wasReadable && importer != null)
            {
                importer.isReadable = false;
                (importer as AssetImporter).SaveAndReimport();
            }

            return window;
        }

        private static void PointImport(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }

            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = Tile;
            (importer as AssetImporter).SaveAndReimport();
        }

        private static void RebuildArtSet(string character)
        {
            string output = OutputDir + "/" + character + "_Art.asset";
            CharacterArtSet art = AssetDatabase.LoadAssetAtPath<CharacterArtSet>(output);

            if (art == null)
            {
                art = ScriptableObject.CreateInstance<CharacterArtSet>();
                AssetDatabase.CreateAsset(art, output);
            }

            string dir = (Root + "/" + character).Replace('/', Path.DirectorySeparatorChar);
            art.Idle = LoadFrames(character, "Idle", dir);
            art.Walk = LoadFrames(character, "Walk", dir);
            art.Attack01 = LoadFrames(character, "Attack01", dir);
            art.Attack02 = LoadFrames(character, "Attack02", dir);
            art.Attack03 = LoadFrames(character, "Attack03", dir);
            art.Hurt = LoadFrames(character, "Hurt", dir);
            art.Death = LoadFrames(character, "Death", dir);

            EditorUtility.SetDirty(art);
        }

        private static Sprite[] LoadFrames(string character, string action, string dir)
        {
            string path = ToAssetPath(dir + Path.DirectorySeparatorChar + character + "_" + action + ".png");
            string prefix = character + "_" + action + "_";

            var sprites = new List<Sprite>();
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);

            for (int i = 0; i < all.Length; i++)
            {
                Sprite sprite = all[i] as Sprite;
                if (sprite != null && sprite.name.StartsWith(prefix))
                {
                    sprites.Add(sprite);
                }
            }

            sprites.Sort((a, b) =>
            {
                int ia = FrameIndex(a.name, prefix);
                int ib = FrameIndex(b.name, prefix);
                return ia.CompareTo(ib);
            });

            return sprites.ToArray();
        }

        private static int FrameIndex(string spriteName, string prefix)
        {
            string tail = spriteName.Substring(prefix.Length);
            int value;
            return int.TryParse(tail, out value) ? value : 0;
        }

        /// <summary>Reads the PNG IHDR width without touching the imported texture.</summary>
        private static int ReadPngWidth(string assetPath)
        {
            string full = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            if (!File.Exists(full))
            {
                return -1;
            }

            using (FileStream stream = File.OpenRead(full))
            {
                var header = new byte[24];
                if (stream.Read(header, 0, 24) < 24 || header[0] != 0x89)
                {
                    return -1;
                }

                return (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            }
        }

        private static string ToAssetPath(string fullPath)
        {
            return fullPath.Replace(Path.DirectorySeparatorChar, '/').Substring(fullPath.IndexOf("Assets/")).Replace(Path.DirectorySeparatorChar, '/');
        }
    }
}
