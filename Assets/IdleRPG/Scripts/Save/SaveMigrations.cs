using UnityEngine;

namespace IdleRPG.Save
{
    /// <summary>
    /// Upgrades older save payloads to the current schema. Every step must be additive-safe:
    /// a v1 file has no lifetime stats, and the defaults (0) are correct for them.
    /// </summary>
    public static class SaveMigrations
    {
        public static SaveData Migrate(SaveData data)
        {
            if (data == null)
            {
                return SaveData.CreateDefault();
            }

            int version = data.schemaVersion;

            // Unknown or missing version: treat the payload as the oldest supported schema and let
            // the sanitising in SaveData repair anything unreasonable.
            if (version <= 0)
            {
                version = 1;
            }

            if (version > SaveData.CurrentVersion)
            {
                Debug.LogWarning($"[SaveMigrations] Save is from a newer version ({version}); loading best-effort.");
                data.schemaVersion = SaveData.CurrentVersion;
                return data;
            }

            if (version < 2)
            {
                // v1 -> v2: lifetime stats + last page index were added; nothing to convert.
                data.totalKills = Mathf.Max(0, data.totalKills);
                data.totalGoldEarned = data.totalGoldEarned < 0d ? 0d : data.totalGoldEarned;
                data.playTimeSeconds = data.playTimeSeconds < 0d ? 0d : data.playTimeSeconds;
                data.ascensionCount = Mathf.Max(0, data.ascensionCount);
                data.saveCount = Mathf.Max(0, data.saveCount);
                data.lastPageIndex = Mathf.Max(0, data.lastPageIndex);
                Debug.Log("[SaveMigrations] Migrated save v1 -> v2 (lifetime stats added).");
            }

            data.schemaVersion = SaveData.CurrentVersion;
            return data;
        }
    }
}
