using IdleRPG.Data;
using IdleRPG.Economy;

namespace IdleRPG.Progression
{
    /// <summary>Who owns the level: one level per hero (ATK/HP/DEF) or one level for the whole account.</summary>
    public enum TrackScope
    {
        Hero = 0,
        Global = 1
    }

    /// <summary>
    /// What one level does. Deliberately short: only kinds that something in the game already uses exist here.
    /// New kinds (abilityUnlock, rosterSlot, automation, loot) land with the system that consumes them.
    /// </summary>
    public enum TrackEffectKind
    {
        /// <summary>Hero stat, e.g. base * (1 + gain)^level (mode decides additive vs compounding).</summary>
        StatPercent = 0,

        /// <summary>Account-wide multiplier: gold / damage / max HP.</summary>
        GlobalPercent = 1,

        /// <summary>Grants a capability, not a stat: the auto-buy manager or the speed button (B6).</summary>
        AutomationUnlock = 2
    }

    /// <summary>Whether an ascension wipes this track's levels.</summary>
    public enum TrackResetPolicy
    {
        Ascension = 0,
        Never = 1
    }

    /// <summary>
    /// One row of the "menu": what it is called, what it costs, what a level gives you.
    ///
    /// ELI5: this is a dish on the menu card, not the kitchen. Session 1 builds these rows from the assets we already
    /// have (<see cref="StatUpgradeData"/>, <see cref="PrestigeUpgradeData"/>) so the game does not change yet; the
    /// later sessions make the row itself data, so a new row needs no code.
    /// </summary>
    public readonly struct ProgressionTrack
    {
        /// <summary>Stable key. Never an enum value - the save file stores this string.</summary>
        public readonly string Id;

        public readonly string DisplayName;

        /// <summary>What the player pays with.</summary>
        public readonly CurrencyType Currency;

        public readonly TrackScope Scope;
        public readonly TrackEffectKind EffectKind;

        /// <summary>Target of a <see cref="TrackEffectKind.StatPercent"/> track.</summary>
        public readonly HeroStatType Stat;

        /// <summary>Target of a <see cref="TrackEffectKind.GlobalPercent"/> track.</summary>
        public readonly PrestigeEffectType GlobalEffect;

        /// <summary>Additive (base * (1 + level*gain)) or compounding (base * (1+gain)^level).</summary>
        public readonly StatEffectMode EffectMode;

        public readonly float GainPerLevel;

        public readonly double BaseCost;
        public readonly double CostGrowth;

        /// <summary>Raw authoring value: 0 or less means "no cap".</summary>
        public readonly int RawMaxLevel;

        public readonly TrackResetPolicy ResetPolicy;

        /// <summary>Session-1 level source (a later session replaces these with generic keys).</summary>
        public readonly StatUpgradeData StatSource;

        public readonly PrestigeUpgradeData GlobalSource;

        /// <summary>Source of an <see cref="TrackEffectKind.AutomationUnlock"/> track (B6).</summary>
        public readonly AutomationDef AutomationSource;

        public bool IsAutomation => EffectKind == TrackEffectKind.AutomationUnlock;

        private ProgressionTrack(
            string id,
            string displayName,
            CurrencyType currency,
            TrackScope scope,
            TrackEffectKind effectKind,
            HeroStatType stat,
            PrestigeEffectType globalEffect,
            StatEffectMode effectMode,
            float gainPerLevel,
            double baseCost,
            double costGrowth,
            int rawMaxLevel,
            TrackResetPolicy resetPolicy,
            StatUpgradeData statSource,
            PrestigeUpgradeData globalSource,
            AutomationDef automationSource)
        {
            Id = id;
            DisplayName = displayName;
            Currency = currency;
            Scope = scope;
            EffectKind = effectKind;
            Stat = stat;
            GlobalEffect = globalEffect;
            EffectMode = effectMode;
            GainPerLevel = gainPerLevel;
            BaseCost = baseCost;
            CostGrowth = costGrowth;
            RawMaxLevel = rawMaxLevel;
            ResetPolicy = resetPolicy;
            StatSource = statSource;
            GlobalSource = globalSource;
            AutomationSource = automationSource;
        }

        public bool HasLevelCap => RawMaxLevel > 0;

        /// <summary>int.MaxValue when unlimited, matching the assets' own convention.</summary>
        public int MaxLevel => RawMaxLevel <= 0 ? int.MaxValue : RawMaxLevel;

        public bool IsAtMaxLevel(int level)
        {
            return HasLevelCap && level >= MaxLevel;
        }

        /// <summary>True when the player must re-buy this track after an ascension.</summary>
        public bool ResetsOnAscension => ResetPolicy == TrackResetPolicy.Ascension;

        /// <summary>Cost of the next <paramref name="levels"/> levels from <paramref name="currentLevel"/>.</summary>
        public double CostFor(int currentLevel, int levels)
        {
            return FormulaUtility.StatUpgradeBulkCost(BaseCost, currentLevel, levels, CostGrowth);
        }

        /// <summary>Levels actually buyable now: clamped to what the cap leaves.</summary>
        public int ClampLevels(int currentLevel, int levels)
        {
            if (levels <= 0)
            {
                return 0;
            }

            if (!HasLevelCap)
            {
                return levels;
            }

            int remaining = MaxLevel - currentLevel;
            if (remaining <= 0)
            {
                return 0;
            }

            return levels > remaining ? remaining : levels;
        }

        public bool HasSource => StatSource != null || GlobalSource != null || AutomationSource != null;

        private static int RawCapOf(int maxLevel, bool hasCap)
        {
            return hasCap ? maxLevel : 0;
        }

        /// <summary>A hero stat track (gold), built from the shipped StatUpgradeData numbers.</summary>
        public static ProgressionTrack FromStatUpgrade(StatUpgradeData data, float fallbackCostGrowth,
            TrackResetPolicy resetPolicy = TrackResetPolicy.Ascension)
        {
            if (data == null)
            {
                return default;
            }

            return new ProgressionTrack(
                data.name,
                data.DisplayName,
                CurrencyType.Gold,
                TrackScope.Hero,
                TrackEffectKind.StatPercent,
                data.StatType,
                PrestigeEffectType.GoldPercent,
                data.EffectMode,
                data.StatGainPerLevelFraction,
                data.BaseCost,
                data.GetCostGrowth(fallbackCostGrowth),
                RawCapOf(data.MaxLevel, data.HasLevelCap),
                resetPolicy,
                data,
                null,
                null);
        }

        /// <summary>A global multiplier track (tokens), built from the shipped PrestigeUpgradeData numbers.</summary>
        public static ProgressionTrack FromPrestige(PrestigeUpgradeData data,
            TrackResetPolicy resetPolicy = TrackResetPolicy.Never)
        {
            if (data == null)
            {
                return default;
            }

            return new ProgressionTrack(
                data.UpgradeID,
                data.DisplayName,
                CurrencyType.PrestigeTokens,
                TrackScope.Global,
                TrackEffectKind.GlobalPercent,
                HeroStatType.Attack,
                data.EffectType,
                StatEffectMode.AdditiveBase,
                data.EffectPerLevel,
                data.BaseCostTokens,
                data.CostGrowth,
                RawCapOf(data.MaxLevel, data.HasLevelCap),
                resetPolicy,
                null,
                data,
                null);
        }

        /// <summary>
        /// An automation card (B6): costs tokens once, max level 1 (owned or not), never resets. The behaviour the
        /// card grants lives in <see cref="AutomationService"/>.
        /// </summary>
        public static ProgressionTrack FromAutomation(AutomationDef data)
        {
            if (data == null)
            {
                return default;
            }

            return new ProgressionTrack(
                data.AutomationID,
                data.DisplayName,
                CurrencyType.PrestigeTokens,
                TrackScope.Global,
                TrackEffectKind.AutomationUnlock,
                HeroStatType.Attack,
                PrestigeEffectType.GoldPercent,
                StatEffectMode.AdditiveBase,
                1f,
                data.BaseCostTokens,
                1.0f,
                1,
                TrackResetPolicy.Never,
                null,
                null,
                data);
        }
    }
}
