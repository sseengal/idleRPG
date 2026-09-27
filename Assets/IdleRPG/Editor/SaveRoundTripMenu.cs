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
    public static partial class SaveRoundTripMenu
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
            "\"autoRetryEnabled\"",
            // schema v5 (B5): fixed hero columns + prestige list became one keyed "levels" list.
            "\"heroes\"",
            "\"heroID\"",
            "\"attackLevel\"",
            "\"healthLevel\"",
            "\"defenseLevel\"",
            "\"prestigeUpgrades\"",
            "\"upgradeID\"",
            "\"level\":",
            // Fix A (2026-09-25): the short-lived string marker that stamped legacy saves as modern is retired.
            "\"powerEra\""
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

            data.levels = new List<LevelRecord>
            {
                new LevelRecord("hero_knight.attack", 12),
                new LevelRecord("hero_knight.health", 9),
                new LevelRecord("hero_knight.defense", 4),
                new LevelRecord("hero_archer.attack", 7),
                new LevelRecord("hero_archer.health", 3),
                new LevelRecord("hero_archer.defense", 11),
                new LevelRecord("hero_mage.attack", 1),
                new LevelRecord("hero_mage.health", 2),
                new LevelRecord("hero_mage.defense", 3),
                new LevelRecord("prestige_gold", 2),
                new LevelRecord("prestige_damage", 1)
            };

            data.partySlots = new List<int> { -1, 1, 2, 0, -1, -1 };

            data.gold = 123456.75d;
            data.gems = 42d;
            data.prestigeTokens = 3.5d;

            data.automation = new List<AutomationSetting>
            {
                new AutomationSetting("autoBuy", true, 0.25f),
                new AutomationSetting("fastForward", false, 0.5f)
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
            data.dailyStreakLastDate = "2026-09-26";
            data.dailyStreakCount = 4;

            data.adRedemptions = new System.Collections.Generic.List<AdRedemptionRecord>
            {
                new AdRedemptionRecord
                {
                    placementId = 0,
                    dayStamp = 20260926,
                    redemptions = 2,
                    lastRedeemedBinary = 638999999999999999d
                }
            };

            return data;
        }

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

                if (trimmed.Length > 0 && !IsVersionLine(trimmed) && !IsRetiredLine(trimmed) &&
                    !LinesMatch(trimmed, newer))
                {
                    missing++;
                }
            }

            return missing;
        }

        /// <summary>
        /// The line is "still there" if it appears verbatim OR if the same JSON field exists in the new file with a
        /// numerically equal value. Double values re-serialise with slightly different digits (217311.77801439282 ->
        /// 217311.7780143928), which is formatting, not data loss - byte comparison would be a false positive.
        /// </summary>
        private static bool LinesMatch(string oldTrimmed, string newer)
        {
            if (newer.Contains(oldTrimmed))
            {
                return true;
            }

            if (!TryParseNumberedField(oldTrimmed, out string key, out double oldValue))
            {
                return false;
            }

            foreach (string line in newer.Split('\n'))
            {
                string trimmed = line.Trim();
                if (TryParseNumberedField(trimmed, out string otherKey, out double newValue) &&
                    otherKey == key &&
                    System.Math.Abs(oldValue - newValue) <= 1e-6 * System.Math.Max(1d, System.Math.Abs(oldValue)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Matches `"field": <number>,` and returns the field name + numeric value.</summary>
        private static bool TryParseNumberedField(string trimmed, out string key, out double value)
        {
            key = null;
            value = 0d;

            int open = trimmed.IndexOf('"', 0);
            int close = trimmed.IndexOf('"', open + 1);

            if (open < 0 || close < 0)
            {
                return false;
            }

            string candidateKey = trimmed.Substring(open + 1, close - open - 1);

            int colon = trimmed.IndexOf(':', close);
            if (colon < 0)
            {
                return false;
            }

            string numberPart = trimmed.Substring(colon + 1).Trim().TrimEnd(',');
            double parsed;

            if (!double.TryParse(numberPart, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out parsed))
            {
                return false;
            }

            key = candidateKey;
            value = parsed;
            return true;
        }

        private static string DescribeMissingLines(string older, string newer)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            foreach (string line in older.Split('\n'))
            {
                string trimmed = line.Trim();

                if (trimmed.Length > 0 && !IsVersionLine(trimmed) && !IsRetiredLine(trimmed) &&
                    !LinesMatch(trimmed, newer))
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
