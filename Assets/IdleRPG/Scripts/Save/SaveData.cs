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
        public const int CurrentVersion = 4;

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

        public bool autoRetryEnabled = true;

        // --- Heroes ---
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

        // --- Permanent upgrades ---
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
                autoRetryEnabled = true,
                heroes = new List<HeroProgressRecord>(),
                partySlots = new List<int>(),
                prestigeUpgrades = new List<PrestigeUpgradeRecord>(),
                gold = 0d,
                gems = 0d,
                prestigeTokens = 0d
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