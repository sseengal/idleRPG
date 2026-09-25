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

            // Fix A (2026-09-25): one-time power compensation for saves that predate the B3d compounding switch.
            //
            // Compounding is x1.09 per level; a legacy additive save at level 80 was 12 x (1 + 0.1 x 80) = 108 ATK,
            // but the same levels now mean 12 x 1.09^80 = 11,839 -> every enemy and boss gets one-shot and the game
            // stops being a game. On the FIRST load after this change we convert each hero level to the compounding
            // level that gives the SAME power (level 80 additive == ~level 25 compounding), so relative progress is
            // kept and the wall band returns. Prestige/automation tracks are untouched (they were never additive).
            // The flag flips, so it can never run twice or touch a modern save. The flag DEFAULTS to false, which is
            // what lets old files (which never wrote the marker) be recognised as legacy - never default it to true.
            if (!data.powerCompensated)
            {
                CompensateAdditiveEraLevels(data);
                data.powerCompensated = true;
                Debug.Log("[SaveMigrations] Applied one-time additive-era level compensation (power preserved).");
            }

            data.schemaVersion = SaveData.CurrentVersion;
            return data;
        }

        /// <summary>
        /// Converts hero levels earned in the additive era to the compounding era's levels with equal power.
        /// Additive power = (1 + additiveGain x L); compounding power = (1 + compoundingGain)^n. We solve for n and
        /// floor it, so the player is never granted MORE power than their era actually produced - the wall simply
        /// returns where it always was. Only keys shaped "&lt;heroId&gt;.attack|health|defense" are touched.
        /// </summary>
        private static void CompensateAdditiveEraLevels(SaveData data)
        {
            const double additiveAttackHealthGain = 0.10d; // the pre-B3d shipped additions
            const double additiveDefenseGain = 0.15d;
            const double compoundingAttackHealthGain = 0.09d; // the B3d shipped compounding
            const double compoundingDefenseGain = 0.05d;

            if (data.levels == null)
            {
                return;
            }

            for (int i = 0; i < data.levels.Count; i++)
            {
                LevelRecord record = data.levels[i];
                if (record == null || string.IsNullOrEmpty(record.key))
                {
                    continue;
                }

                int dot = record.key.IndexOf('.');

                // Global keys (Prestige_*, automation cards, tracks) have no dot and were never additive.
                if (dot < 0)
                {
                    continue;
                }

                string suffix = record.key.Substring(dot + 1);
                double additivePerLevel;
                double compoundingPerLevel;

                switch (suffix)
                {
                    case "attack":
                    case "health":
                        additivePerLevel = additiveAttackHealthGain;
                        compoundingPerLevel = compoundingAttackHealthGain;
                        break;
                    case "defense":
                        additivePerLevel = additiveDefenseGain;
                        compoundingPerLevel = compoundingDefenseGain;
                        break;
                    default:
                        // An unknown per-hero key must be left alone - never guess.
                        continue;
                }

                if (record.level <= 0)
                {
                    continue;
                }

                double additivePower = 1d + additivePerLevel * record.level;
                double powerLogRatio = System.Math.Log(additivePower) / System.Math.Log(1d + compoundingPerLevel);

                if (double.IsNaN(powerLogRatio) || double.IsInfinity(powerLogRatio) || powerLogRatio <= 0d)
                {
                    continue;
                }

                int compensated = (int)System.Math.Floor(powerLogRatio);

                // Bound: compounding never grants a higher level than the era produced at the same power point.
                record.level = compensated > record.level ? record.level : compensated;
            }
        }
    }
}
