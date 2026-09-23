using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using IdleRPG.Save;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Save-schema drift test. Writes a fully-populated payload, parses it back, writes it again and compares the
    /// two texts byte for byte - plus migration checks from the older schemas.
    ///
    /// ELI5: a save file that loses one number is a player losing their progress. This tool round-trips the whole
    /// payload through the exact code the game uses and shouts if a single character moved. Same idea as the
    /// content pipeline's export/import drift check, applied to saves.
    ///
    /// Menu: Tools > Idle RPG > Save > Round-Trip Drift Test
    /// </summary>
    public static class SaveRoundTripMenu
    {
        /// <summary>
        /// Keys that were deliberately removed from the schema. An old file still carries them and a fresh capture
        /// will not, so they must not read as "lost data" - that would train us to ignore a real drop.
        ///
        /// `autoRetryEnabled` (removed 2026-09-22): the fallback bounce resumes a wipe by itself, so the flag decided
        /// nothing. Dropping it costs the player nothing.
        /// </summary>
        private static readonly HashSet<string> RetiredKeys = new HashSet<string>
        {
            "\"autoRetryEnabled\""
        };

        [MenuItem("Tools/Idle RPG/Save/Round-Trip Drift Test", priority = 65)]
        public static void Run()
        {
            RunChecks();
        }

        /// <summary>Same test, but returns the verdict so the combined regression menu can aggregate it.</summary>
        public static bool RunChecks()
        {
            bool ok = true;

            ok &= CheckDrift(SaveData.CreateDefault(), "default payload");
            ok &= CheckDrift(BuildFullPayload(), "fully-populated payload");
            ok &= CheckMigrations();
            ok &= CheckLiveSaveFile();

            Debug.Log(ok
                ? "[SaveRoundTrip] PASS - schema v" + SaveData.CurrentVersion + " round-trips cleanly and migrations are sane."
                : "[SaveRoundTrip] FAIL - see the lines above.");

            return ok;
        }

        /// <summary>Serialise -> parse -> serialise. Any difference is data loss.</summary>
        private static bool CheckDrift(SaveData original, string label)
        {
            string first = JsonUtility.ToJson(original, true);
            SaveData parsed = JsonUtility.FromJson<SaveData>(first);

            if (parsed == null)
            {
                Debug.LogError($"[SaveRoundTrip] {label}: could not be parsed back at all.");
                return false;
            }

            string second = JsonUtility.ToJson(parsed, true);

            if (first != second)
            {
                Debug.LogError($"[SaveRoundTrip] {label}: DRIFT detected.\n{FirstDifference(first, second)}");
                return false;
            }

            Debug.Log($"[SaveRoundTrip] {label}: stable ({first.Length} chars, v{parsed.schemaVersion}).");
            return true;
        }

        /// <summary>Every field carries a non-default value, so a dropped field cannot hide behind a default.</summary>
        private static SaveData BuildFullPayload()
        {
            SaveData data = SaveData.CreateDefault();

            data.schemaVersion = SaveData.CurrentVersion;
            data.currentStage = 37;
            data.currentWave = 6;
            data.highestStageReached = 41;
            data.runBestStage = 39;

            data.heroes = new List<HeroProgressRecord>
            {
                new HeroProgressRecord("hero_knight", 12, 9, 4),
                new HeroProgressRecord("hero_archer", 7, 3, 11),
                new HeroProgressRecord("hero_mage", 1, 2, 3)
            };

            data.partySlots = new List<int> { -1, 1, 2, 0, -1, -1 };

            data.gold = 123456.75d;
            data.gems = 42d;
            data.prestigeTokens = 3.5d;

            data.prestigeUpgrades = new List<PrestigeUpgradeRecord>
            {
                new PrestigeUpgradeRecord("prestige_gold", 2),
                new PrestigeUpgradeRecord("prestige_damage", 1)
            };

            data.offlineEquivalentCapBonusSeconds = 3600d;
            data.offlineCapExtensionsPurchased = 1;
            data.goldBoostActive = true;
            data.goldBoostExpiresAtBinary = 638999999999999999d;
            data.totalKills = 4321;
            data.totalGoldEarned = 98765.5d;
            data.playTimeSeconds = 12345.25d;
            data.ascensionCount = 2;
            data.saveCount = 88;
            data.lastPageIndex = 3;
            data.lastGoldPerSecond = 12.5d;
            data.lastLogoutTimestampBinary = "638888888888888888";
            data.lastSaveTimestampBinary = "638999999999999999";

            return data;
        }

        /// <summary>Old payloads must upgrade without losing anything, and without inventing a layout.</summary>
        private static bool CheckMigrations()
        {
            bool ok = true;

            // A v2 file has no partySlots: it must upgrade to v3 with an empty layout (read as default front row).
            SaveData v2 = SaveData.CreateDefault();
            v2.schemaVersion = 2;
            v2.partySlots = null;
            v2.gold = 500d;

            SaveData migratedV2 = SaveMigrations.Migrate(v2);

            if (migratedV2.schemaVersion != SaveData.CurrentVersion || migratedV2.partySlots == null)
            {
                Debug.LogError($"[SaveRoundTrip] v2 migration wrong: version={migratedV2.schemaVersion}, " +
                               $"slots={(migratedV2.partySlots == null ? "null" : "ok")}.");
                ok = false;
            }
            else if (migratedV2.partySlots.Count != 0)
            {
                Debug.LogError($"[SaveRoundTrip] v2 migration invented a layout ({migratedV2.partySlots.Count} slots); " +
                               "it must stay empty so the default front row applies.");
                ok = false;
            }
            else if (!Mathf.Approximately((float)migratedV2.gold, 500f))
            {
                Debug.LogError($"[SaveRoundTrip] v2 migration lost gold ({migratedV2.gold}).");
                ok = false;
            }
            else
            {
                Debug.Log("[SaveRoundTrip] v2 -> v3 migration: keeps values, adds an empty board.");
            }

            // A v3 file has no runBestStage: it must adopt the current stage (never invent a higher one) and stay
            // inside the lifetime best, because that number gates and prices the next ascension.
            SaveData v3 = SaveData.CreateDefault();
            v3.schemaVersion = 3;
            v3.currentStage = 12;
            v3.highestStageReached = 20;
            v3.runBestStage = 0;

            SaveData migratedV3 = SaveMigrations.Migrate(v3);

            if (migratedV3.schemaVersion != SaveData.CurrentVersion)
            {
                Debug.LogError($"[SaveRoundTrip] v3 migration left version {migratedV3.schemaVersion}.");
                ok = false;
            }
            else if (migratedV3.runBestStage != 12)
            {
                Debug.LogError($"[SaveRoundTrip] v3 migration set runBestStage {migratedV3.runBestStage}; " +
                               "expected the current stage (12).");
                ok = false;
            }
            else
            {
                Debug.Log("[SaveRoundTrip] v3 -> v4 migration: run best adopts the current stage.");
            }

            // A v1 file (no version at all) must still land on the current schema.
            SaveData v1 = SaveData.CreateDefault();
            v1.schemaVersion = 0;
            SaveData migratedV1 = SaveMigrations.Migrate(v1);

            if (migratedV1.schemaVersion != SaveData.CurrentVersion)
            {
                Debug.LogError($"[SaveRoundTrip] v1 migration left the version at {migratedV1.schemaVersion}.");
                ok = false;
            }
            else
            {
                Debug.Log("[SaveRoundTrip] v1 -> v3 migration: lands on the current schema.");
            }

            return ok;
        }

        /// <summary>
        /// If a real save exists, prove two things about it: the payload is stable through the *current* schema,
        /// and upgrading it from its on-disk version is additive-only (no line on disk is lost).
        /// </summary>
        private static bool CheckLiveSaveFile()
        {
            SaveSystem system = new SaveSystem();

            if (!system.HasSave || !File.Exists(system.SavePath))
            {
                Debug.Log("[SaveRoundTrip] No save file on disk yet; skipped the file-level check.");
                return true;
            }

            string onDisk = File.ReadAllText(system.SavePath);
            SaveData parsed = JsonUtility.FromJson<SaveData>(onDisk);

            if (parsed == null)
            {
                Debug.LogError("[SaveRoundTrip] The save file on disk cannot be parsed.");
                return false;
            }

            int fileVersion = parsed.schemaVersion;

            // Upgrade the file the way a load does, then prove the upgraded payload is stable.
            SaveData migrated = SaveMigrations.Migrate(parsed);
            string first = JsonUtility.ToJson(migrated, true);
            string second = JsonUtility.ToJson(JsonUtility.FromJson<SaveData>(first), true);

            if (first != second)
            {
                Debug.LogError($"[SaveRoundTrip] The upgraded file is not byte-stable.\n{FirstDifference(first, second)}");
                return false;
            }

            // Additive-only: every field that existed on disk must still be there after the upgrade.
            int lost = CountMissingLines(onDisk, second);

            if (lost > 0)
            {
                Debug.LogError($"[SaveRoundTrip] The upgrade dropped {lost} line(s) that existed on disk:\n" +
                               $"{DescribeMissingLines(onDisk, second)}");
                return false;
            }

            Debug.Log($"[SaveRoundTrip] file {Path.GetFileName(system.SavePath)}: v{fileVersion} -> v{migrated.schemaVersion}, " +
                      $"stable, additive-only ({onDisk.Length} -> {first.Length} chars, " +
                      $"{migrated.partySlots?.Count ?? 0} slot(s), {lost} line(s) lost).");
            return true;
        }

        /// <summary>
        /// Counts non-empty lines of <paramref name="older"/> that do not appear in <paramref name="newer"/>.
        /// The version line is skipped on purpose: bumping the version is the migration *succeeding*.
        /// </summary>
        private static int CountMissingLines(string older, string newer)
        {
            int missing = 0;

            foreach (string line in older.Split('\n'))
            {
                string trimmed = line.Trim();

                if (trimmed.Length > 0 && !IsVersionLine(trimmed) && !IsRetiredLine(trimmed) && !newer.Contains(trimmed))
                {
                    missing++;
                }
            }

            return missing;
        }

        private static string DescribeMissingLines(string older, string newer)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            foreach (string line in older.Split('\n'))
            {
                string trimmed = line.Trim();

                if (trimmed.Length > 0 && !IsVersionLine(trimmed) && !IsRetiredLine(trimmed) && !newer.Contains(trimmed))
                {
                    builder.AppendLine("    lost: " + trimmed);
                }
            }

            return builder.ToString().TrimEnd();
        }

        private static bool IsVersionLine(string trimmedLine)
        {
            return trimmedLine.StartsWith("\"schemaVersion\"");
        }

        /// <summary>True when the line is a key we deliberately retired from the schema (see <see cref="RetiredKeys"/>).</summary>
        private static bool IsRetiredLine(string trimmedLine)
        {
            foreach (string key in RetiredKeys)
            {
                if (trimmedLine.StartsWith(key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Reports the first differing line, which is usually the field that broke.</summary>
        private static string FirstDifference(string a, string b)
        {
            string[] left = a.Split('\n');
            string[] right = b.Split('\n');
            int lines = Mathf.Min(left.Length, right.Length);

            for (int i = 0; i < lines; i++)
            {
                if (left[i] != right[i])
                {
                    return $"  first difference at line {i + 1}:\n    written:  {left[i].Trim()}\n    re-read:  {right[i].Trim()}";
                }
            }

            return $"  line counts differ: {left.Length} vs {right.Length}";
        }
    }
}
