using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using IdleRPG.Data;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// The gate the art pipeline promised and never had (`Art-Pipeline.md` §5).
    ///
    /// ELI5: this walks the art folders and says out loud what is wrong, instead of trusting anyone to remember.
    /// It exists because two wrong claims reached the owner - "Priest has no attack", "Lancer has no walk" - and both
    /// were silence rather than a failure. A name the game does not understand is now an error, not a shrug.
    ///
    /// It uses the SAME resolver the importer uses, so the two can never disagree about what a file means.
    ///
    /// Checks:
    ///   1. strip names    - every file in a pack resolves to base / ability / vfx; anything else is an error
    ///   2. preset drift   - imported strips must be Point, no mips, uncompressed, PPU 100
    ///   3. art set cover  - every imported character has Idle, Attack01, Hurt and Death frames
    ///   4. provenance     - every ThirdParty pack is named in PROVENANCE.md
    /// </summary>
    public static class ArtPipelineValidator
    {
        private const string ImportedRoot = "Assets/IdleRPG/Art/Characters/TinyRPG";
        private const string ArtSetDir = "Assets/IdleRPG/Data/Characters";
        private const string Provenance = "Assets/ThirdParty/PROVENANCE.md";
        private const int ExpectedPpu = 100;

        /// <summary>Pack drop folders, relative to the project root. Absent on a machine without them.</summary>
        private static readonly string[] Packs =
        {
            "TinyRPGAssets/Characters(100x100 split)",
            "TinyRPGAssets2/Characters(100x100 split)"
        };

        [MenuItem("Tools/Idle RPG/Art/Validate Art Pipeline")]
        public static void Validate()
        {
            var problems = new List<string>();

            CheckStripNames(problems);
            CheckPresetDrift(problems);
            CheckArtSets(problems);
            CheckProvenance(problems);

            var report = new StringBuilder();
            report.AppendLine("[ArtPipelineValidator] " +
                              (problems.Count == 0 ? "PASS - no problems found" : problems.Count + " problem(s) found"));

            for (int i = 0; i < problems.Count; i++)
            {
                report.AppendLine("  " + (i + 1) + ". " + problems[i]);
                Debug.LogError("[ArtPipelineValidator] " + problems[i]);
            }

            Debug.Log(report.ToString());
        }

        /// <summary>Check 1: every strip file in a pack must resolve to something the game understands.</summary>
        private static void CheckStripNames(List<string> problems)
        {
            int characters = 0, files = 0, baseStrips = 0, abilities = 0, vfx = 0;

            for (int p = 0; p < Packs.Length; p++)
            {
                string root = Path.Combine(Directory.GetCurrentDirectory(), Packs[p]);

                if (!Directory.Exists(root))
                {
                    Debug.Log("[ArtPipelineValidator] pack not on this machine, skipped: " + Packs[p]);
                    continue;
                }

                foreach (string characterDir in Directory.GetDirectories(root))
                {
                    string name = Path.GetFileName(characterDir);
                    string inner = Path.Combine(characterDir, name);

                    if (!Directory.Exists(inner))
                    {
                        continue;
                    }

                    characters++;

                    foreach (string file in Directory.GetFiles(inner, "*.png"))
                    {
                        string bare = Path.GetFileNameWithoutExtension(file);

                        // The bare <Char>.png combined sheet is not an action strip.
                        if (bare == name)
                        {
                            continue;
                        }

                        files++;
                        CharacterAction resolved = CharacterActionResolver.Resolve(bare.Substring(name.Length + 1));

                        if (resolved.Kind == CharacterActionKind.Base)
                        {
                            baseStrips++;
                        }
                        else if (resolved.Kind == CharacterActionKind.Ability)
                        {
                            abilities++;
                        }
                        else if (resolved.Kind == CharacterActionKind.Vfx)
                        {
                            vfx++;
                        }
                        else
                        {
                            problems.Add("unrecognised action in " + name + ": '" + bare + "' - teach " +
                                         nameof(CharacterActionResolver) + " about it, or the animation is dropped");
                        }
                    }
                }
            }

            Debug.Log("[ArtPipelineValidator] strip names: " + characters + " character(s), " + files + " file(s) - " +
                      baseStrips + " base, " + abilities + " ability, " + vfx + " vfx");
        }

        /// <summary>Check 2: imported strips must carry the project's import preset.</summary>
        private static void CheckPresetDrift(List<string> problems)
        {
            if (!Directory.Exists(ImportedRoot))
            {
                Debug.Log("[ArtPipelineValidator] no imported strips yet, preset check skipped");
                return;
            }

            int checkedCount = 0;

            foreach (string file in Directory.GetFiles(ImportedRoot, "*.png", SearchOption.AllDirectories))
            {
                string path = file.Replace(Path.DirectorySeparatorChar, '/');

                // _Source holds the raw pack drop, kept for traceability. It is archive material, not shipped art,
                // so it is not expected to carry the import preset.
                if (path.IndexOf("/_Source/", System.StringComparison.Ordinal) >= 0)
                {
                    continue;
                }

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null)
                {
                    continue;
                }

                checkedCount++;

                if (importer.filterMode != FilterMode.Point)
                {
                    problems.Add("preset drift: " + path + " is not Point filtered");
                }

                if (importer.mipmapEnabled)
                {
                    problems.Add("preset drift: " + path + " has mipmaps on");
                }

                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    problems.Add("preset drift: " + path + " is compressed (pixel art smears)");
                }

                if (Mathf.Abs(importer.spritePixelsPerUnit - ExpectedPpu) > 0.01f)
                {
                    problems.Add("preset drift: " + path + " is PPU " + importer.spritePixelsPerUnit +
                                 ", expected " + ExpectedPpu);
                }
            }

            Debug.Log("[ArtPipelineValidator] preset drift: " + checkedCount + " imported strip(s) checked");
        }

        /// <summary>Check 3: every imported character has an art set with the frames the unit views need.</summary>
        private static void CheckArtSets(List<string> problems)
        {
            if (!Directory.Exists(ImportedRoot))
            {
                Debug.Log("[ArtPipelineValidator] no imported characters yet, art set check skipped");
                return;
            }

            int sets = 0;

            foreach (string characterDir in Directory.GetDirectories(ImportedRoot))
            {
                string name = Path.GetFileName(characterDir);

                // "_Source" holds the raw pack drop, it is not a character.
                if (name.StartsWith("_"))
                {
                    continue;
                }

                string path = ArtSetDir + "/" + name + "_Art.asset";
                CharacterArtSet art = AssetDatabase.LoadAssetAtPath<CharacterArtSet>(path);

                if (art == null)
                {
                    problems.Add("imported character '" + name + "' has no art set (expected " + path + ")");
                    continue;
                }

                sets++;
                Require(art.Idle, "Idle", name, problems);
                Require(art.Attack01, "Attack01", name, problems);
                Require(art.Hurt, "Hurt", name, problems);
                Require(art.Death, "Death", name, problems);
            }

            Debug.Log("[ArtPipelineValidator] art sets: " + sets + " checked");
        }

        private static void Require(Sprite[] frames, string slot, string character, List<string> problems)
        {
            if (frames == null || frames.Length == 0)
            {
                problems.Add(character + " art set has no " + slot + " frames");
            }
        }

        /// <summary>Check 4: every third-party pack is traceable in the provenance record (G15 wants this).</summary>
        private static void CheckProvenance(List<string> problems)
        {
            const string thirdParty = "Assets/ThirdParty";

            if (!File.Exists(Provenance))
            {
                problems.Add("missing provenance record: " + Provenance);
                return;
            }

            if (!Directory.Exists(thirdParty))
            {
                problems.Add("missing third-party art folder: " + thirdParty);
                return;
            }

            string record = File.ReadAllText(Provenance);
            int packs = 0;

            foreach (string dir in Directory.GetDirectories(thirdParty))
            {
                string name = Path.GetFileName(dir);

                if (name.StartsWith("."))
                {
                    continue;
                }

                packs++;

                if (record.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    problems.Add("pack folder '" + name + "' is not named in " + Provenance);
                }
            }

            Debug.Log("[ArtPipelineValidator] provenance: " + packs + " pack folder(s) checked");
        }
    }
}

