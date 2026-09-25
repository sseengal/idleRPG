using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleRPG.Save
{
    /// <summary>Per-hero level snapshot (one entry per party lane).</summary>
    [Serializable]
    public class HeroProgressRecord
    {
        public string heroID = "";
        public int attackLevel;
        public int healthLevel;
        public int defenseLevel;

        public HeroProgressRecord()
        {
        }

        public HeroProgressRecord(string heroID, int attackLevel, int healthLevel, int defenseLevel)
        {
            this.heroID = heroID;
            this.attackLevel = attackLevel;
            this.healthLevel = healthLevel;
            this.defenseLevel = defenseLevel;
        }

        public int GetLevel(Data.HeroStatType statType)
        {
            switch (statType)
            {
                case Data.HeroStatType.Attack:
                    return attackLevel;
                case Data.HeroStatType.Health:
                    return healthLevel;
                case Data.HeroStatType.Defense:
                    return defenseLevel;
                default:
                    return 0;
            }
        }

        public void SetLevel(Data.HeroStatType statType, int level)
        {
            int safeLevel = level < 0 ? 0 : level;

            switch (statType)
            {
                case Data.HeroStatType.Attack:
                    attackLevel = safeLevel;
                    break;
                case Data.HeroStatType.Health:
                    healthLevel = safeLevel;
                    break;
                case Data.HeroStatType.Defense:
                    defenseLevel = safeLevel;
                    break;
            }
        }
    }

    /// <summary>
    /// One keyed progression level: the "key" is the whole contract (e.g. "hero_knight.attack" = 80, or a permanent
    /// track id). Schema v5 means the save file never needs to know what a stat is - a new track only adds new keys.
    /// </summary>
    [Serializable]
    public class LevelRecord
    {
        public string key = "";
        public int level;

        public LevelRecord()
        {
        }

        public LevelRecord(string key, int level)
        {
            this.key = key;
            this.level = level;
        }
    }

    /// <summary>
    /// One automation card's player settings (B6): whether the machine is on, and how much of the wallet the machine
    /// promises never to touch. Additive in schema v5 - an older file simply reads the defaults.
    /// </summary>
    [Serializable]
    public class AutomationSetting
    {
        public string ruleId = "";
        public bool enabled;
        public float budgetFraction = 0.5f;

        public AutomationSetting()
        {
        }

        public AutomationSetting(string ruleId, bool enabled, float budgetFraction)
        {
            this.ruleId = ruleId;
            this.enabled = enabled;
            this.budgetFraction = budgetFraction;
        }
    }

    /// <summary>Permanent (prestige) upgrade level snapshot.</summary>
    [Serializable]
    public class PrestigeUpgradeRecord
    {
        public string upgradeID = "";
        public int level;

        public PrestigeUpgradeRecord()
        {
        }

        public PrestigeUpgradeRecord(string upgradeID, int level)
        {
            this.upgradeID = upgradeID;
            this.level = level;
        }
    }

    /// <summary>
    /// Plain-serialisable snapshot of the whole game, written by <see cref="SaveSystem"/>.
    /// JsonUtility needs public fields and cannot serialise dictionaries, so lists are used.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Bumped whenever the schema changes; drives migration (see SaveMigrations).</summary>
        public const int CurrentVersion = 5;

        public int schemaVersion = CurrentVersion;

        // --- Progress ---
        public int currentStage = 1;
        public int currentWave = 1;
        public int highestStageReached = 1;

        /// <summary>
        /// Best stage reached in the CURRENT run (resets to 1 on ascension). Ascension is gated and priced on this,
        /// not on <see cref="highestStageReached"/>: otherwise the button stays live forever after the first
        /// ascension and can be pressed repeatedly for free tokens.
        /// </summary>
        public int runBestStage = 1;

        // --- Level records (schema v5): one "key -> level" list for the whole game ---
        /// <summary>
        /// Every progression level as {key, level}. Keys are stable strings, never enum values (AD5): a hero stat is
        /// "{heroId}.attack|health|defense", a permanent (prestige) track is its track id. New tracks only ever add
        /// keys - the file shape itself never has to change again for levels.
        /// </summary>
        public List<LevelRecord> levels = new List<LevelRecord>();

        // --- Automation (schema v5 additive, B6): on/off + the safety-reserve dial per card ---
        public List<AutomationSetting> automation = new List<AutomationSetting>();

        // --- Power era (v5 additive, Fix A 2026-09-25): has this save received the one-time compensation? ---
        /// <summary>
        /// False (the C# default) means the save is from before the B3d compounding switch and has additive-era hero
        /// levels; true means "compensation already applied" (or a brand-new save). JsonUtility keeps field
        /// initializers for missing JSON keys, so this MUST default to false - a false default flag is what lets a
        /// legacy file be recognised even though it never stored the marker. (A short-lived string marker with a
        /// "compounding" default failed exactly here: it stamped legacy files as modern and compensation never ran.)
        /// </summary>
        public bool powerCompensated;

        // --- Legacy (schema <= v4): kept ONLY as migration input; new saves always write these empty. ---
        public List<HeroProgressRecord> heroes = new List<HeroProgressRecord>();

        // --- Formation (schema v3, additive) ---
        /// <summary>
        /// Who stands in which board slot: one entry per slot, holding the party index (0-based) or -1 for an
        /// empty slot. An empty list means "no layout saved" (old files), which the game reads as the default
        /// front-row placement - so a v2 save loads exactly as it played before formation existed.
        /// </summary>
        public List<int> partySlots = new List<int>();

        // --- Currency ---
        public double gold;
        public double gems;
        public double prestigeTokens;

        // --- Permanent upgrades (legacy: migration input only since v5) ---
        public List<PrestigeUpgradeRecord> prestigeUpgrades = new List<PrestigeUpgradeRecord>();

        // --- Shop purchases (schema v2 additive: older files simply default to 0) ---
        /// <summary>Extra seconds of offline income cap bought with gems (the gem sink).</summary>
        public double offlineEquivalentCapBonusSeconds;

        public int offlineCapExtensionsPurchased;

        // --- Boosts ---
        public bool goldBoostActive;
        public double goldBoostExpiresAtBinary;

        // --- Lifetime stats (schema v2, additive: old files simply default to 0) ---
        public int totalKills;
        public double totalGoldEarned;
        public double playTimeSeconds;
        public int ascensionCount;
        public int saveCount;
        public int lastPageIndex;

        // --- Meta ---
        public double lastGoldPerSecond;
        public string lastLogoutTimestampBinary = "0";
        public string lastSaveTimestampBinary = "0";

        /// <summary>New-game defaults.</summary>
        public static SaveData CreateDefault()
        {
            return new SaveData
            {
                schemaVersion = CurrentVersion,
                currentStage = 1,
                currentWave = 1,
                highestStageReached = 1,
                runBestStage = 1,
                heroes = new List<HeroProgressRecord>(),
                levels = new List<LevelRecord>(),
                partySlots = new List<int>(),
                prestigeUpgrades = new List<PrestigeUpgradeRecord>(),
                gold = 0d,
                gems = 0d,
                prestigeTokens = 0d,
                powerCompensated = true
            };
        }

        /// <summary>True when the payload looks like a real save file.</summary>
        public bool IsUsable => currentStage >= 1 && highestStageReached >= 1 && schemaVersion > 0;

        /// <summary>Clamps values that could break the simulation if a file is hand-edited.</summary>
        public void Sanitize()
        {
            if (currentStage < 1)
            {
                currentStage = 1;
            }

            if (currentWave < 1)
            {
                currentWave = 1;
            }

            if (highestStageReached < currentStage)
            {
                highestStageReached = currentStage;
            }

            if (runBestStage < 1)
            {
                runBestStage = 1;
            }

            if (runBestStage > highestStageReached)
            {
                runBestStage = highestStageReached;
            }

            if (heroes == null)
            {
                heroes = new List<HeroProgressRecord>();
            }

            if (levels == null)
            {
                levels = new List<LevelRecord>();
            }
            else
            {
                for (int i = 0; i < levels.Count; i++)
                {
                    if (levels[i] == null)
                    {
                        levels[i] = new LevelRecord();
                        continue;
                    }

                    if (levels[i].level < 0)
                    {
                        levels[i].level = 0;
                    }
                }
            }

            if (automation == null)
            {
                automation = new List<AutomationSetting>();
            }
            else
            {
                for (int i = 0; i < automation.Count; i++)
                {
                    if (automation[i] == null)
                    {
                        automation[i] = new AutomationSetting();
                        continue;
                    }

                    if (float.IsNaN(automation[i].budgetFraction))
                    {
                        automation[i].budgetFraction = 0.5f;
                    }

                    automation[i].budgetFraction = Mathf.Clamp01(automation[i].budgetFraction);
                }
            }

            if (prestigeUpgrades == null)
            {
                prestigeUpgrades = new List<PrestigeUpgradeRecord>();
            }

            if (totalKills < 0)
            {
                totalKills = 0;
            }

            if (ascensionCount < 0)
            {
                ascensionCount = 0;
            }

            if (saveCount < 0)
            {
                saveCount = 0;
            }

            if (lastPageIndex < 0)
            {
                lastPageIndex = 0;
            }

            if (offlineCapExtensionsPurchased < 0)
            {
                offlineCapExtensionsPurchased = 0;
            }

            offlineEquivalentCapBonusSeconds = Progression.FormulaUtility.Sanitize(offlineEquivalentCapBonusSeconds);
            totalGoldEarned = Progression.FormulaUtility.Sanitize(totalGoldEarned);
            playTimeSeconds = Progression.FormulaUtility.Sanitize(playTimeSeconds);
            gold = Progression.FormulaUtility.Sanitize(gold);
            gems = Progression.FormulaUtility.Sanitize(gems);
            prestigeTokens = Progression.FormulaUtility.Sanitize(prestigeTokens);
            lastGoldPerSecond = Progression.FormulaUtility.Sanitize(lastGoldPerSecond);
        }

        /// <summary>Finds (or creates) the level record for a hero id.</summary>
        public HeroProgressRecord GetOrCreateHero(string heroID)
        {
            if (heroes == null)
            {
                heroes = new List<HeroProgressRecord>();
            }

            for (int i = 0; i < heroes.Count; i++)
            {
                if (heroes[i] != null && heroes[i].heroID == heroID)
                {
                    return heroes[i];
                }
            }

            HeroProgressRecord record = new HeroProgressRecord(heroID, 0, 0, 0);
            heroes.Add(record);
            return record;
        }

        /// <summary>Level of a permanent upgrade id, 0 when unknown.</summary>
        public int GetPrestigeLevel(string upgradeID)
        {
            if (prestigeUpgrades == null)
            {
                return 0;
            }

            for (int i = 0; i < prestigeUpgrades.Count; i++)
            {
                PrestigeUpgradeRecord record = prestigeUpgrades[i];
                if (record != null && record.upgradeID == upgradeID)
                {
                    return Mathf.Max(0, record.level);
                }
            }

            return 0;
        }

        /// <summary>Finds (or creates) the level record for a permanent upgrade id.</summary>
        public PrestigeUpgradeRecord GetOrCreatePrestigeUpgrade(string upgradeID)
        {
            if (prestigeUpgrades == null)
            {
                prestigeUpgrades = new List<PrestigeUpgradeRecord>();
            }

            for (int i = 0; i < prestigeUpgrades.Count; i++)
            {
                if (prestigeUpgrades[i] != null && prestigeUpgrades[i].upgradeID == upgradeID)
                {
                    return prestigeUpgrades[i];
                }
            }

            PrestigeUpgradeRecord record = new PrestigeUpgradeRecord(upgradeID, 0);
            prestigeUpgrades.Add(record);
            return record;
        }

        /// <summary>Level of a keyed progression record; 0 when unknown.</summary>
        public int GetLevel(string key)
        {
            if (levels == null || string.IsNullOrEmpty(key))
            {
                return 0;
            }

            for (int i = 0; i < levels.Count; i++)
            {
                LevelRecord record = levels[i];
                if (record != null && record.key == key)
                {
                    return Mathf.Max(0, record.level);
                }
            }

            return 0;
        }

        /// <summary>Writes or creates a keyed progression record. Never stores a negative level.</summary>
        public void SetLevel(string key, int level)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (levels == null)
            {
                levels = new List<LevelRecord>();
            }

            int safeLevel = level < 0 ? 0 : level;

            for (int i = 0; i < levels.Count; i++)
            {
                LevelRecord record = levels[i];
                if (record != null && record.key == key)
                {
                    record.level = safeLevel;
                    return;
                }
            }

            levels.Add(new LevelRecord(key, safeLevel));
        }

        /// <summary>
        /// Stable key builders - the strings in here ARE the save contract. Ship once, never reformat: an old key
        /// holds a player's progress and a renamed key silently throws it away (migrations must never rewrite these).
        /// </summary>
        public static class SaveKeys
        {
            /// <summary>"hero_knight.attack" - one level per hero per stat.</summary>
            public static string HeroStat(string heroId, Data.HeroStatType stat)
            {
                return heroId + "." + stat.ToString().ToLowerInvariant();
            }

            /// <summary>A permanent (prestige / global) track uses its own id as the key.</summary>
            public static string Track(string trackId)
            {
                return trackId;
            }
        }

        /// <summary>Debug-friendly summary.</summary>
        public override string ToString()
        {
            int heroCount = heroes == null ? 0 : heroes.Count;
            int prestigeCount = prestigeUpgrades == null ? 0 : prestigeUpgrades.Count;

            return $"[SaveData v{schemaVersion}] Stage {currentStage} (best {highestStageReached}) wave {currentWave}, " +
                   $"gold={gold}, gems={gems}, tokens={prestigeTokens}, heroes={heroCount}, prestige={prestigeCount}, " +
                   $"kills={totalKills}, play={playTimeSeconds:0}s, ascensions={ascensionCount}";
        }
    }
}