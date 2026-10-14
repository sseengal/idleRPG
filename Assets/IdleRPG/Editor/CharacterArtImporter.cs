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
            int baseCount = 0, abilityCount = 0, vfxCount = 0, compositeCount = 0;
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
                        // The named special strips are the ability material: sliced like any base strip so an
                        // ability can play its own animation (Ability-Plan S1).
                        SliceStrip(ToAssetPath(strip));
                        abilityCount++;
                        break;

                    case CharacterActionKind.Composite:
                        // Sliced like the rest, but only PLACED when the character has a spare attack slot (see the
                        // art-set fill below). Its frames exist either way, so a later rule change needs no re-slice.
                        SliceStrip(ToAssetPath(strip));
                        compositeCount++;
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

            Debug.Log("[CharacterArtImporter] " + character + ": " + baseCount + " base strip(s) and " +
                      abilityCount + " special strip(s) imported; " + vfxCount +
                      " effect strip(s) recognised, not imported; " + compositeCount +
                      " composited strip(s) sliced for a spare attack slot.");

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

            // A composited strip ("<Action>(With magic effects)") is the same wind-up with its spell already drawn
            // into the frames - real animation the game can show. It may fill an attack slot this character has NO
            // plain strip for, and never Attack01, so the primary swing is never replaced by the version with the
            // spell baked in. The Wizard's plain attacks stop at Attack02, so its composited cast becomes Attack03
            // and its second ability has a distinct clip to play instead of repeating its basic attack.
            bool spare02 = IsEmpty(art.Attack02);
            if (spare02 || IsEmpty(art.Attack03))
            {
                Sprite[] composite = LoadCompositeFrames(character, dir, out string compositeAction);

                if (composite.Length > 0)
                {
                    if (spare02)
                    {
                        art.Attack02 = composite;
                    }
                    else
                    {
                        art.Attack03 = composite;
                    }

                    Debug.Log($"[CharacterArtImporter] {character}: composited strip \"{compositeAction}\" fills the " +
                              $"spare {(spare02 ? "Attack02" : "Attack03")} slot ({composite.Length} frames).");
                }
            }

            // The named special (Block / Heal / Beam / Spike / Summon) is the ability material: sliced and cropped
            // exactly like a base strip, with its own fps and its own measured strike frame, so an ability can play
            // its own animation and land its effect on the frame the pose extends.
            art.Ability = LoadAbilityFrames(character, dir, out string abilityAction, out string abilityStrip);
            art.AbilityStrip = abilityAction ?? string.Empty;
            art.AbilityFps = DefaultAbilityFps;
            art.AbilityStrikeFrame = abilityStrip == null
                ? -1
                : MeasureStrikeFrame(ToAssetPath(abilityStrip), ReadPngWidth(ToAssetPath(abilityStrip)) / Tile);

            // The four flying creatures (Bat, Blood Monster_B, Ghostfire, Hellbat) have no Idle strip: their
            // movement animation carries the pose, so Idle falls back to Walk rather than rendering nothing.
            if ((art.Idle == null || art.Idle.Length == 0) && art.Walk != null && art.Walk.Length > 0)
            {
                art.Idle = art.Walk;
            }

            // This pack's art is drawn facing right (verified from the sheets); heroes keep that orientation
            // and enemies get mirrored, so both end up facing the opponent.
            art.ArtFacesLeft = false;

            // Which frame the blow lands on, measured from the pixels (see MeasureStrikeFrame). Without this the
            // sim has to guess a fraction of the clip, and every character whose strike is not at that fraction
            // shows a visible gap between the swing connecting and the enemy reacting.
            art.StrikeFrame = MeasureStrikeFrame(dir, character);

            EditorUtility.SetDirty(art);
        }

        /// <summary>
        /// The frame the blow lands on, measured from the art: the FIRST frame whose opaque content is within 95%
        /// of the strip's widest frame.
        ///
        /// The widest frame is the most extended pose - the sword at full reach, the bow released, the spell burst
        /// - which is the moment a player reads as the hit. The FIRST such frame is used, so a strip that holds the
        /// extended pose for several frames strikes on the frame it first reaches full extension.
        ///
        /// Returns -1 when the strip cannot be read (the art set then falls back to its last frame), so a
        /// measurement failure can never make the hit land early.
        /// </summary>
        private static int MeasureStrikeFrame(string dir, string character)
        {
            // Same preference order as CharacterArtSet.GetAttack: the primary attack strip first.
            string strip = CharacterActionResolver.FindStrip(dir, character, "Attack01");

            if (strip == null)
            {
                strip = CharacterActionResolver.FindStrip(dir, character, "Attack02");
            }

            if (strip == null)
            {
                strip = CharacterActionResolver.FindStrip(dir, character, "Attack03");
            }

            if (strip == null)
            {
                return -1;
            }

            string path = ToAssetPath(strip);
            return MeasureStrikeFrame(path, ReadPngWidth(path) / Tile);
        }

        /// <summary>
        /// The pixel half of the measurement: which frame of this strip is the most extended pose. Used for the
        /// attack strip and for the special (ability) strip alike, so both land their effect on the same kind of
        /// frame - the sword at full reach, the bow released, the spell burst.
        /// </summary>
        private static int MeasureStrikeFrame(string assetPath, int cols)
        {
            string path = assetPath;

            if (cols <= 0)
            {
                return -1;
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            bool wasReadable = importer != null && importer.isReadable;

            if (importer != null && !wasReadable)
            {
                importer.isReadable = true;
                (importer as AssetImporter).SaveAndReimport();
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int strike = -1;

            if (tex != null && tex.isReadable)
            {
                Color[] px = tex.GetPixels();
                int width = tex.width;
                int[] widths = new int[cols];
                int widest = 0;

                for (int frame = 0; frame < cols; frame++)
                {
                    int minX = int.MaxValue;
                    int maxX = -1;

                    for (int x = frame * Tile; x < Mathf.Min((frame + 1) * Tile, width); x++)
                    {
                        for (int y = 0; y < tex.height; y++)
                        {
                            if (px[y * width + x].a < 12f / 255f)
                            {
                                continue;
                            }

                            if (x < minX)
                            {
                                minX = x;
                            }

                            if (x > maxX)
                            {
                                maxX = x;
                            }
                        }
                    }

                    widths[frame] = maxX >= minX ? maxX - minX + 1 : 0;

                    if (widths[frame] > widest)
                    {
                        widest = widths[frame];
                    }
                }

                for (int frame = 0; frame < cols && widest > 0; frame++)
                {
                    if (widths[frame] * 100 >= widest * 95)
                    {
                        strike = frame;
                        break;
                    }
                }
            }

            if (!wasReadable && importer != null)
            {
                importer.isReadable = false;
                (importer as AssetImporter).SaveAndReimport();
            }

            return strike;
        }

        /// <summary>
        /// Playback rate the pack authors its special strips at. Stored per character on the art set, so a strip
        /// that feels wrong can be tuned in the inspector without touching this.
        /// </summary>
        private const int DefaultAbilityFps = 10;

        private static Sprite[] LoadFrames(string character, string slot, string dir)
        {
            // The slot is FOUND, not guessed: the pack may call the same animation Attack, Attack3, Walk01 or Flying.
            string strip = CharacterActionResolver.FindStrip(dir, character, slot);

            return strip == null ? new Sprite[0] : LoadStripFrames(character, strip);
        }

        /// <summary>True when a slot holds no frames at all.</summary>
        private static bool IsEmpty(Sprite[] frames)
        {
            return frames == null || frames.Length == 0;
        }

        /// <summary>
        /// The character's first composited strip ("&lt;Action&gt;(With magic effects)") as frames, or an empty array.
        /// <paramref name="actionName"/> receives the pack's own name for it, for the log.
        /// </summary>
        private static Sprite[] LoadCompositeFrames(string character, string dir, out string actionName)
        {
            string strip = CharacterActionResolver.FindCompositeStrip(dir, character, out actionName);

            return strip == null ? new Sprite[0] : LoadStripFrames(character, strip);
        }

        /// <summary>
        /// The character's special strip frames (the ability material), or an empty array when it has none.
        /// <paramref name="actionName"/> receives the pack's own name for it ("Block", "Heal", "Summon", ...) and
        /// <paramref name="stripPath"/> the file itself - the art set records the name, the measurement needs the
        /// file.
        /// </summary>
        private static Sprite[] LoadAbilityFrames(string character, string dir, out string actionName, out string stripPath)
        {
            stripPath = CharacterActionResolver.FindAbilityStrip(dir, character, out actionName);

            return stripPath == null ? new Sprite[0] : LoadStripFrames(character, stripPath);
        }

        /// <summary>Every sprite of one sliced strip, in frame order.</summary>
        private static Sprite[] LoadStripFrames(string character, string strip)
        {
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
