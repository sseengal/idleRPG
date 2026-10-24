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

        /// <summary>Character folder name under Art/Characters/TinyRPG ("Knight"). Empty = no animated art.</summary>
        public string art = "";
        public float baseHealth = 100f;
        public float baseAttack = 10f;
        public float baseDefense = 5f;
        public float attackIntervalSec = 1.5f;
        /// <summary>
        /// Ability ids from abilities.json that this hero may OWN ("shield_wall", "heavy_strike"). Empty = none.
        /// Each one sits behind its own unlock (token cost + stage gate). The player equips exactly ONE of them and
        /// can never unequip - a bought hero ability is always switched on (Ability-Plan).
        /// </summary>
        public List<string> abilities = new List<string>();
        /// <summary>Placeholder tint as "#RRGGBBAA".</summary>
        public string tint = "#FFFFFFFF";
    }

    [Serializable]
    public class EnemySpecFile
    {
        public int version = 1;

        /// <summary>
        /// The ladder's scaling table: one row per band, ten bands for the 100-stage run.
        ///
        /// This is THE place enemy difficulty is tuned. A row is a multiplier for that band's whole roster, so
        /// "bands 8-10 hit 20% harder" is one number here plus a regeneration - never forty rows of hand-edited
        /// enemies. The per-stage curve (BalanceConfig.EnemyHealthGrowth) still runs on top, so this table only
        /// says how much stronger a band's roster is than band 1's.
        /// </summary>
        public List<EnemyBandSpec> bands = new List<EnemyBandSpec>();

        public List<EnemySpec> enemies = new List<EnemySpec>();
    }

    /// <summary>
    /// One band's scaling. Band 1 covers stages 1-10, band 2 stages 11-20, and so on (band = (firstStage - 1) / 10 + 1).
    ///
    /// Multipliers, not absolute numbers: 1 = "the same as band 1". Health 2.5 means that band's roster has two
    /// and a half times band 1's health, before the per-stage curve is applied.
    /// </summary>
    [Serializable]
    public class EnemyBandSpec
    {
        /// <summary>Band number, 1-based (1 = stages 1-10).</summary>
        public int band = 1;

        public float health = 1f;
        public float attack = 1f;

        /// <summary>Defence is an absolute add rather than a multiplier, because band 1 sits at zero.</summary>
        public float defense = 0f;

        public float gold = 1f;

        /// <summary>Attack-interval multiplier: below 1 swings faster, which is how the late bands feel harder.</summary>
        public float interval = 1f;
    }

    [Serializable]
    public class EnemySpec
    {
        public string id = "";
        /// <summary>Asset file name without extension (e.g. "Enemy_Slime"). Empty = derive from the id.</summary>
        public string asset = "";
        public string displayName = "";
        public string sprite = "";

        /// <summary>Character folder name under Art/Characters/TinyRPG ("Black Knight_A"). Empty = placeholder art.</summary>
        public string art = "";

        /// <summary>First stage this enemy may appear on (see the stage ladder).</summary>
        public int firstStage = 1;

        /// <summary>Last stage this enemy may appear on. 0 = no limit.</summary>
        public int lastStage = 0;
        /// <summary>
        /// Which archetype shape the numbers take (see EnemyArchetypes in the generator): "brute" is a slow
        /// wall, "skirmisher" hits fast and light, "caster" hits hard but is fragile, "beast" is quick, plus
        /// "undead" and "humanoid" as the neutral shapes. Empty = "humanoid".
        /// </summary>
        public string archetype = "";

        /// <summary>
        /// Alternate art folders rotated by stage (index = stage % count), so one enemy archetype wears several
        /// skins across a run - e.g. the Goblin showing Demon / Blood Monster sheets. Empty = just <see cref="art"/>.
        /// </summary>
        public List<string> stageAlternates = new List<string>();

        /// <summary>
        /// These four are DERIVED from the band table when left at 0, which is the normal case: the generator
        /// writes band x archetype. A non-zero value is used exactly as written - the escape hatch for a
        /// hand-tuned special case (the four original enemies keep their hand numbers).
        /// </summary>
        public float baseHealth = 0f;
        public float baseAttack = 0f;
        public float baseDefense = 0f;
        public float baseGoldDrop = 0f;
        public float attackIntervalSec = 0f;

        public bool isBoss;
        public float bossHealthMultiplier = 1f;
        public float bossGoldMultiplier = 1f;

        /// <summary>
        /// How much bigger a boss draws than a normal enemy of the same art. 1.6 = 60% bigger, which is the size the
        /// old hard-coded boss box produced (128 px -> 205 px). Change it here and every boss follows.
        /// </summary>
        public float bossSizeScale = 1.6f;

        public string tint = "#FFFFFFFF";
        /// <summary>
        /// Ability ids from abilities.json that this enemy may cast ("enemy_block"). Empty = none. Enemies are NOT
        /// gated by unlock stage or tokens, and they cast on their own cooldown (Ability-Plan decision 5).
        /// </summary>
        public List<string> abilities = new List<string>();
        /// <summary>"inherit" | "frontmost" | "lowesthealthpercent" | "random" | "backlinefirst".</summary>
        public string targetRule = "inherit";
    }

    [Serializable]
    public class AbilitySpecFile
    {
        public int version = 1;
        public List<AbilitySpec> abilities = new List<AbilitySpec>();
    }

    /// <summary>
    /// One ability: what it does, which art slot plays it, how often it may fire, and what it costs to unlock.
    /// Authored in abilities.json. Heroes and enemies point at an id with their own "ability" field, so the same
    /// definition (e.g. the enemies' Block) is shared by everyone who uses it.
    /// </summary>
    [Serializable]
    public class AbilitySpec
    {
        public string id = "";
        public string displayName = "";

        /// <summary>"shield" | "heal" | "damage".</summary>
        public string kind = "damage";

        /// <summary>
        /// Which art slot plays it: "Ability" (the character's own special strip) or a numbered attack strip such as
        /// "Attack02". A character that has no such strip simply does not play one.
        /// </summary>
        public string artSlot = "Ability";

        /// <summary>Impact effect id under Resources/Vfx (a sliced sheet such as "ice_shatter"). Empty = none.</summary>
        public string impactVfx = "";

        /// <summary>Seconds the effect starts before the damage lands (the burst leads the number).</summary>
        public float impactDelaySec = 0f;

        /// <summary>Effect size relative to the shared base. 1 = standard, 1.15 = a bit bigger.</summary>
        public float impactVfxScale = 1f;

        /// <summary>Seconds between casts. The cast itself is instant; this is the clock that refills it.</summary>
        public float cooldownSec = 12f;

        /// <summary>Shield: damage taken is multiplied by this while active (0.5 = half damage).</summary>
        public float shieldFactor = 0.5f;

        /// <summary>Shield: how long the shield lasts.</summary>
        public float shieldSeconds = 4f;

        /// <summary>Heal: fraction of the target's maximum health restored.</summary>
        public float healFraction = 0.25f;

        /// <summary>Damage: multiplier applied to the caster's attack.</summary>
        public float damageMultiplier = 1.8f;

        /// <summary>Who the effect lands on: "self" | "party" | "lowestally" | "frontenemy" | "fronthero".</summary>
        public string target = "frontenemy";

        /// <summary>Stage the player must have reached before it can be bought. 0 = no gate.</summary>
        public int unlockStage = 0;

        /// <summary>Prestige tokens it costs. 0 = free (the starting ability, so the feature is usable at once).</summary>
        public double tokenCost = 0d;

        public string description = "";
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
        public List<AutomationSpec> automationUpgrades = new List<AutomationSpec>();
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
        /// <summary>
        /// "additive" (default) = base x (1 + level x gain) | "multiplicative" = base x (1 + gain)^level.
        /// Stored as text so it survives enum reordering, same as <see cref="statType"/>.
        /// </summary>
        public string effectMode = "additive";
        /// <summary>0 = unlimited.</summary>
        public int maxLevel;
    }

    [Serializable]
    public class PrestigeUpgradeSpec
    {
        public string id = "";
        /// <summary>Asset file name without extension (e.g. "Prestige_Gold").</summary>
        public string asset = "";
        /// <summary>"tokens" (ASCEND tree) | "gems" (SHOP shelf). Missing = tokens (B7 S4).</summary>
        public string costCurrency = "tokens";
        /// <summary>"gold" | "damage" | "health".</summary>
        public string effectType = "gold";
        public string displayName = "";
        public string description = "";
        public double baseCostTokens = 1d;
        public float costGrowth = 1.4f;
        public float effectPerLevel = 0.1f;
        public int maxLevel = 10;
    }

    /// <summary>One automation card row (B6): bought once with tokens, grants a capability.</summary>
    [Serializable]
    public class AutomationSpec
    {
        public string id = "";
        public string asset = "";
        /// <summary>What the card grants: "autoBuy" | "fastForward".</summary>
        public string automationId = "autoBuy";
        public string displayName = "";
        public string description = "";
        public double baseCostTokens = 4d;
        public int minAscensions = 0;
    }
}
