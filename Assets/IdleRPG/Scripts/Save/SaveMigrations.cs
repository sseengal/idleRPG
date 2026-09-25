using System.Collections.Generic;
using UnityEngine;

namespace IdleRPG.Save
{
    /// <summary>
    /// Upgrades older save payloads to the current schema. Every step must be additive-safe: a v1 file has no
    /// lifetime stats (0 is correct), and a v2 file has no party board (empty means "default front row").
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

            if (version < 3)
            {
                // v2 -> v3: the party board was added (Step 10b). An empty layout is deliberate: the game reads
                // "no layout" as the default front-row placement, so an upgraded save plays exactly as it did.
                if (data.partySlots == null)
                {
                    data.partySlots = new List<int>();
                }

                Debug.Log($"[SaveMigrations] Migrated save v2 -> v3 (formation; {data.partySlots.Count} slot(s) recorded).");
            }

            if (version < 4)
            {
                // v3 -> v4: the per-run best stage was added (ascension gate + token yield). An older save has no
                // record of the current run's progress, so the honest conversion is "your run best is where you
                // are now" - it can never invent a stage the player did not reach, and it cannot be exploited.
                data.runBestStage = Mathf.Max(1, data.currentStage, data.runBestStage);
                Debug.Log($"[SaveMigrations] Migrated save v3 -> v4 (run best stage = {data.runBestStage}).");
            }

            if (version < 5)
            {
                // v4 -> v5: three fixed hero columns + a prestige list became ONE keyed list of {key, level}
                // ("hero_knight.attack" = 80, "Prestige_Gold" = 3). Every existing level is copied by hand into its
                // stable key - this is a rename, it cannot lose or cheapen a single level. The legacy lists are
                // emptied afterwards so the file has exactly one shape from here on.
                if (data.levels == null)
                {
                    data.levels = new List<LevelRecord>();
                }

                if (data.heroes != null)
                {
                    for (int i = 0; i < data.heroes.Count; i++)
                    {
                        HeroProgressRecord hero = data.heroes[i];
                        if (hero == null || string.IsNullOrEmpty(hero.heroID))
                        {
                            continue;
                        }

                        data.SetLevel(SaveData.SaveKeys.HeroStat(hero.heroID, Data.HeroStatType.Attack), hero.attackLevel);
                        data.SetLevel(SaveData.SaveKeys.HeroStat(hero.heroID, Data.HeroStatType.Health), hero.healthLevel);
                        data.SetLevel(SaveData.SaveKeys.HeroStat(hero.heroID, Data.HeroStatType.Defense), hero.defenseLevel);
                    }
                }

                if (data.prestigeUpgrades != null)
                {
                    for (int i = 0; i < data.prestigeUpgrades.Count; i++)
                    {
                        PrestigeUpgradeRecord record = data.prestigeUpgrades[i];
                        if (record != null && !string.IsNullOrEmpty(record.upgradeID))
                        {
                            data.SetLevel(record.upgradeID, record.level);
                        }
                    }
                }

                data.heroes = new List<HeroProgressRecord>();
                data.prestigeUpgrades = new List<PrestigeUpgradeRecord>();

                Debug.Log($"[SaveMigrations] Migrated save v4 -> v5 (keyed progress records; {data.levels.Count} level key(s)).");
            }

            data.schemaVersion = SaveData.CurrentVersion;
            return data;
        }
    }
}
