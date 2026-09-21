using System;
using System.Collections.Generic;

namespace IdleRPG.EditorTools.Content
{
    /// <summary>
    /// JSON shapes for the content specs (`Assets/IdleRPG/Content/Specs/*.json`).
    ///
    /// ELI5: instead of poking values into Unity assets by hand, each thing in the game gets a recipe card
    /// written in a text file. The generator reads the cards and fills the assets; the validator reads them
    /// and complains about typos or nonsense numbers. Cards are diffable in git, which assets are not.
    ///
    /// JsonUtility rules: public fields, [Serializable], no dictionaries.
    /// </summary>
    [Serializable]
    public class HeroSpecFile
    {
        public int version = 1;
        public List<HeroSpec> heroes = new List<HeroSpec>();
    }

    [Serializable]
    public class HeroSpec
    {
        public string id = "";
        /// <summary>Asset file name without extension (e.g. "Hero_Knight"). Empty = derive from the id.</summary>
        public string asset = "";
        public string displayName = "";
        public string icon = "";
        public float baseHealth = 100f;
        public float baseAttack = 10f;
        public float baseDefense = 5f;
        public float attackIntervalSec = 1.5f;
        /// <summary>Placeholder tint as "#RRGGBBAA".</summary>
        public string tint = "#FFFFFFFF";
    }

    [Serializable]
    public class EnemySpecFile
    {
        public int version = 1;
        public List<EnemySpec> enemies = new List<EnemySpec>();
    }

    [Serializable]
    public class EnemySpec
    {
        public string id = "";
        /// <summary>Asset file name without extension (e.g. "Enemy_Slime"). Empty = derive from the id.</summary>
        public string asset = "";
        public string displayName = "";
        public string sprite = "";
        public float baseHealth = 60f;
        public float baseAttack = 9f;
        public float baseDefense = 0f;
        public float baseGoldDrop = 8f;
        public float attackIntervalSec = 2f;
        public bool isBoss;
        public float bossHealthMultiplier = 1f;
        public float bossGoldMultiplier = 1f;
        public string tint = "#FFFFFFFF";
        /// <summary>"inherit" | "frontmost" | "lowesthealthpercent" | "random" | "backlinefirst".</summary>
        public string targetRule = "inherit";
    }

    [Serializable]
    public class PartySpecFile
    {
        public int version = 1;
        /// <summary>Hero ids in lane order (index 0 = front).</summary>
        public List<string> heroIds = new List<string>();
    }

    [Serializable]
    public class WaveSpecFile
    {
        public int version = 1;
        /// <summary>Enemy ids rotated across normal waves.</summary>
        public List<string> normalEnemyIds = new List<string>();
        /// <summary>Enemy ids rotated across boss waves.</summary>
        public List<string> bossEnemyIds = new List<string>();
    }

    [Serializable]
    public class UpgradeSpecFile
    {
        public int version = 1;
        public List<StatUpgradeSpec> statUpgrades = new List<StatUpgradeSpec>();
        public List<PrestigeUpgradeSpec> prestigeUpgrades = new List<PrestigeUpgradeSpec>();
    }

    [Serializable]
    public class StatUpgradeSpec
    {
        public string id = "";
        /// <summary>Asset file name without extension (e.g. "StatUpgrade_ATK").</summary>
        public string asset = "";
        /// <summary>"attack" | "health" | "defense" - stored as text so it survives enum reordering.</summary>
        public string statType = "attack";
        public string displayName = "";
        public string description = "";
        public double baseCost = 10d;
        public float costGrowth = 1.07f;
        public float statGainPerLevelFraction = 0.1f;
        /// <summary>0 = unlimited.</summary>
        public int maxLevel;
    }

    [Serializable]
    public class PrestigeUpgradeSpec
    {
        public string id = "";
        /// <summary>Asset file name without extension (e.g. "Prestige_Gold").</summary>
        public string asset = "";
        /// <summary>"gold" | "damage" | "health".</summary>
        public string effectType = "gold";
        public string displayName = "";
        public string description = "";
        public double baseCostTokens = 1d;
        public float costGrowth = 1.4f;
        public float effectPerLevel = 0.1f;
        public int maxLevel = 10;
    }
}
