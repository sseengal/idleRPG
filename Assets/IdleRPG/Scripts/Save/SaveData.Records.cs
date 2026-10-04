using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleRPG.Save
{
    /// <summary>
    /// The per-entry record shapes a save file is built from (top-level, like the main
    /// <see cref="SaveData"/> payload).
    /// </summary>
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
    [Serializable]
    public class AdRedemptionRecord
    {
        public int placementId;
        public int dayStamp;
        public int redemptions;
        public double lastRedeemedBinary;

        public AdRedemptionRecord()
        {
        }
    }
    [Serializable]
    public class ItemSaveRecord
    {
        public string instanceId = "";
        public int slotType;
        public int rarity;
        public int role;
        public int level;
        public double statValue;
        public double price;

        public ItemSaveRecord()
        {
        }

        public ItemSaveRecord(string instanceId, int slotType, int rarity, int role, int level, double statValue, double price)
        {
            this.instanceId = instanceId;
            this.slotType = slotType;
            this.rarity = rarity;
            this.role = role;
            this.level = level;
            this.statValue = statValue;
            this.price = price;
        }
    }
    [Serializable]
    public class EquippedGearRecord
    {
        public int heroIndex;
        public string weaponId = "";
        public string armorId = "";
        public string trinketId = "";

        public EquippedGearRecord()
        {
        }

        public EquippedGearRecord(int heroIndex)
        {
            this.heroIndex = heroIndex;
        }
    }
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
}
