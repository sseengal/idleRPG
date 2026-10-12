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

        /// <summary>Widest strip in the packs is 1800 px (18 frames), so 2048 covers every one of them.</summary>
        private const int MaxMobileTextureSize = 2048;

        [MenuItem("Tools/Idle RPG/Art/Import TinyRPG Characters")]
        public static void ImportAll()
        {
            if (!AssetDatabase.IsValidFolder(OutputDir))
            {
                AssetDatabase.CreateFolder("Assets/IdleRPG/Data", "Characters");
            }

            // The character list is SCANNED, not hardcoded. The old list named four characters, so adding art
            // silently did nothing - the same class of mistake as the hardcoded action names in S1.
            List<string> characters = FindCharacters();

            foreach (string character in characters)
            {
                ImportCharacter(character);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CharacterArtImporter] imported " + characters.Count + " character(s): " +
                      string.Join(", ", characters));
        }

        /// <summary>Every character folder under <see cref="Root"/>. "_Source" is the raw pack drop, not a character.</summary>
        private static List<string> FindCharacters()
        {
            var found = new List<string>();
            string root = Root.Replace('/', Path.DirectorySeparatorChar);

            if (!Directory.Exists(root))
            {
                Debug.LogError("[CharacterArtImporter] no character root at " + Root);
                return found;
            }

            string[] dirs = Directory.GetDirectories(root);
            System.Array.Sort(dirs, System.StringComparer.Ordinal);

            for (int i = 0; i < dirs.Length; i++)
            {
                string name = Path.GetFileName(dirs[i]);

                if (name.StartsWith("_"))
                {
                    continue;
                }

                found.Add(name);
            }

            return found;
        }

        /// <summary>
        /// Declares a texture's settings for one mobile platform explicitly, instead of leaving the inert
        /// "inherit the default" stub that Unity writes on its own when a mobile target is active.
        /// </summary>
        private static void ApplyMobilePolicy(TextureImporter importer, string platform)
        {
            var settings = new TextureImporterPlatformSettings
            {
                name = platform,
                overridden = true,
                maxTextureSize = MaxMobileTextureSize,
                format = TextureImporterFormat.RGBA32,
                textureCompression = TextureImporterCompression.Uncompressed,
                compressionQuality = 100,
                crunchedCompression = false,
                allowsAlphaSplitting = false,
            };

            importer.SetPlatformTextureSettings(settings);
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
            // Names are MAPPED, not matched: the packs use Attack, Attack3, DEATH, Walk01, Flying and more.
            // Anything unrecognised is reported, never silently skipped.
            int baseCount = 0, abilityCount = 0, vfxCount = 0;
            var unknown = new List<string>();

            foreach (string strip in Directory.GetFiles(dir, character + "_*.png"))
            {
                string action = Path.GetFileNameWithoutExtension(strip).Substring(character.Length + 1);
                CharacterAction resolved = CharacterActionResolver.Resolve(action);

                switch (resolved.Kind)
                {
                    case CharacterActionKind.Base:
                        SliceStrip(ToAssetPath(strip));
                        baseCount++;
                        break;

                    case CharacterActionKind.Ability:
                        abilityCount++;
                        break;

                    case CharacterActionKind.Vfx:
                        vfxCount++;
                        break;

                    default:
                        unknown.Add(action);
                        break;
                }
            }

            if (unknown.Count > 0)
            {
                Debug.LogWarning("[CharacterArtImporter] " + character + ": unrecognised action(s) - " +
                                 string.Join(", ", unknown) + ". NOT imported. Teach " +
                                 nameof(CharacterActionResolver) + " about them if the game should use them.");
            }

            Debug.Log("[CharacterArtImporter] " + character + ": " + baseCount + " base strip(s) imported; " +
                      abilityCount + " ability and " + vfxCount + " effect strip(s) recognised, not imported.");

            string sheet = dir + Path.DirectorySeparatorChar + character + ".png";
            if (File.Exists(sheet))
            {
                PointImport(ToAssetPath(sheet));
            }

            RebuildArtSet(character);
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

            // The per-platform policy is DECLARED here, not hand-edited into 344 .meta files. Pixel art with 1 px
            // black outlines turns to mush under block compression (Art-Pipeline 17), so both mobile platforms
            // start uncompressed. If the device pass says the memory is too high, THIS is the one place to switch
            // them to ASTC 4x4 - both stores support ASTC, with ETC2 as the Android fallback.
            ApplyMobilePolicy(importer, "Android");
            ApplyMobilePolicy(importer, "iPhone");

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

            bool cropped = false;

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
                    // The body always sits inside its own tile (x0..x1 are tile-local), so the ONLY thing that can
                    // overflow is the 2 px pad. If the crop pushed past the tile edge, every frame rect built from
                    // it hangs off the right of the strip and Unity silently DROPS that frame - 289 frames went
                    // missing that way on the widest poses. Clamping to the tile can only trim the pad, never the
                    // body, so this is safe for every strip.
                    int pad = 2;
                    int cx = Mathf.Max(0, x0 - pad);
                    int cy = Mathf.Max(0, y0 - pad);
                    int cw = Mathf.Min(Tile - cx, x1 - x0 + 1 + pad * 2);
                    int ch = Mathf.Min(Tile - cy, y1 - y0 + 1 + pad * 2);
                    window = new Rect(cx, cy, cw, ch);
                    cropped = true;
                }

                // NOTE: the texture is NOT destroyed here. It belongs to the AssetDatabase, so destroying it is
                // illegal - Unity threw "Destroying assets is not permitted", and because that happened AFTER the
                // crop window was computed but BEFORE it was returned, every strip silently fell back to the full
                // 100 px tile and characters rendered tiny (the bug fixed in Art-Pipeline 18). The readable copy is
                // released by the isReadable=false + reimport below.
            }

            if (!cropped)
            {
                Debug.LogError("[CharacterArtImporter] no crop window for " + assetPath +
                               " - this strip will render at FULL TILE SIZE. Check that the texture imported " +
                               "readable and that its first tile has opaque pixels.");
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

            // The four flying creatures (Bat, Blood Monster_B, Ghostfire, Hellbat) have no Idle strip: their
            // movement animation carries the pose, so Idle falls back to Walk rather than rendering nothing.
            if ((art.Idle == null || art.Idle.Length == 0) && art.Walk != null && art.Walk.Length > 0)
            {
                art.Idle = art.Walk;
            }

            // This pack's art is drawn facing right (verified from the sheets); heroes keep that orientation
            // and enemies get mirrored, so both end up facing the opponent.
            art.ArtFacesLeft = false;

            EditorUtility.SetDirty(art);
        }

        private static Sprite[] LoadFrames(string character, string slot, string dir)
        {
            // The slot is FOUND, not guessed: the pack may call the same animation Attack, Attack3, Walk01 or Flying.
            string strip = CharacterActionResolver.FindStrip(dir, character, slot);

            if (strip == null)
            {
                return new Sprite[0];
            }

            string stripName = Path.GetFileNameWithoutExtension(strip).Substring(character.Length + 1);
            string path = ToAssetPath(strip);
            string prefix = character + "_" + stripName + "_";

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

        /// <summary>
        /// Reads the PNG IHDR width without touching the imported texture.
        /// Internal so the art validator can compare a strip's promised frame count against the art set's real one.
        /// </summary>
        internal static int ReadPngWidth(string assetPath)
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
