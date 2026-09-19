using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>What a permanent (prestige) upgrade modifies globally.</summary>
    public enum PrestigeEffectType
    {
        GoldPercent = 0,
        DamagePercent = 1,
        HealthPercent = 2
    }

    public static class PrestigeEffectTypeExtensions
    {
        public static string ToDisplayName(this PrestigeEffectType effectType)
        {
            switch (effectType)
            {
                case PrestigeEffectType.GoldPercent:
                    return "+% Gold";
                case PrestigeEffectType.DamagePercent:
                    return "+% Damage";
                case PrestigeEffectType.HealthPercent:
                    return "+% HP";
                default:
                    return effectType.ToString();
            }
        }
    }

    /// <summary>
    /// A permanent upgrade bought with Prestige Tokens on the ascension panel.
    /// Effects are global multipliers applied by <see cref="IdleRPG.Progression.StatResolver"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "PrestigeUpgradeData", menuName = "Idle RPG/Data/Prestige Upgrade", order = 14)]
    public class PrestigeUpgradeData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string upgradeID = "";

        [SerializeField] private string displayName = "";

        [TextArea(2, 4)]
        [SerializeField] private string description = "";

        [SerializeField] private PrestigeEffectType effectType = PrestigeEffectType.GoldPercent;

        [Header("Cost Curve (Prestige Tokens)")]
        [Tooltip("Token cost of level 0 -> 1.")]
        [SerializeField] private double baseCostTokens = 1d;

        [Tooltip("Cost growth per level. Typical 1.5 (each level costs 50% more).")]
        [SerializeField] private float costGrowth = 1.5f;

        [Tooltip("Level cap. 0 = unlimited.")]
        [SerializeField] private int maxLevel = 10;

        [Header("Effect")]
        [Tooltip("Bonus per level as a fraction. 0.05 = +5% per level, additive across levels.")]
        [SerializeField] private float effectPerLevel = 0.05f;

        public string UpgradeID => string.IsNullOrEmpty(upgradeID) ? name : upgradeID;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        public string Description => description;

        public PrestigeEffectType EffectType => effectType;

        public double BaseCostTokens => baseCostTokens < 0d ? 0d : baseCostTokens;

        public float CostGrowth => Mathf.Max(1f, costGrowth);

        public int MaxLevel => maxLevel <= 0 ? int.MaxValue : maxLevel;

        public bool HasLevelCap => maxLevel > 0;

        public float EffectPerLevel => effectPerLevel;

        /// <summary>Total multiplier bonus for a level, e.g. level 3 * 0.05 = 0.15 (+15%).</summary>
        public float GetTotalBonus(int level)
        {
            int safeLevel = Mathf.Max(0, level);
            if (HasLevelCap && safeLevel > MaxLevel)
            {
                safeLevel = MaxLevel;
            }

            return safeLevel * effectPerLevel;
        }

        /// <summary>Multiplier to apply, e.g. 1.15 for +15%.</summary>
        public double GetMultiplier(int level)
        {
            double multiplier = 1d + GetTotalBonus(level);
            return multiplier < 0d ? 0d : multiplier;
        }

        public bool IsAtMaxLevel(int level)
        {
            return HasLevelCap && level >= MaxLevel;
        }

        private void OnValidate()
        {
            if (baseCostTokens < 0d)
            {
                baseCostTokens = 0d;
            }

            if (costGrowth < 1f)
            {
                costGrowth = 1f;
            }

            if (string.IsNullOrEmpty(upgradeID))
            {
                upgradeID = name;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = name;
            }
        }
    }
}