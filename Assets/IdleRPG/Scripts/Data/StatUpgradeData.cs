using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>The three upgradeable hero stats.</summary>
    public enum HeroStatType
    {
        Attack = 0,
        Health = 1,
        Defense = 2
    }

    public static class HeroStatTypeExtensions
    {
        /// <summary>Human readable label for UI.</summary>
        public static string ToDisplayName(this HeroStatType statType)
        {
            switch (statType)
            {
                case HeroStatType.Attack:
                    return "ATK";
                case HeroStatType.Health:
                    return "HP";
                case HeroStatType.Defense:
                    return "DEF";
                default:
                    return statType.ToString();
            }
        }
    }

    /// <summary>
    /// One upgradeable stat track (ATK / HP / DEF) bought with Gold.
    /// Pure data: cost maths lives in <see cref="IdleRPG.Progression.FormulaUtility"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "StatUpgradeData", menuName = "Idle RPG/Data/Stat Upgrade", order = 13)]
    public class StatUpgradeData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private HeroStatType statType = HeroStatType.Attack;

        [SerializeField] private string displayName = "";

        [TextArea(2, 4)]
        [SerializeField] private string description = "";

        [Header("Cost Curve")]
        [Tooltip("Cost of level 0 -> 1. Spec formula: Cost = BaseCost * 1.07^level")]
        [SerializeField] private double baseCost = 10d;

        [Tooltip("Growth applied per level. 0 or less falls back to BalanceConfig.UpgradeCostGrowth (1.07).")]
        [SerializeField] private float costGrowthMultiplier = 1.07f;

        [Header("Effect")]
        [Tooltip("Stat gain per level as a fraction of the hero's base stat. 0.1 = +10% of base per level.")]
        [SerializeField] private float statGainPerLevelFraction = 0.1f;

        [Tooltip("Level cap. 0 = unlimited.")]
        [SerializeField] private int maxLevel = 0;

        public HeroStatType StatType => statType;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? statType.ToDisplayName() : displayName;

        public string Description => description;

        public double BaseCost => baseCost < 0d ? 0d : baseCost;

        /// <summary>Returns the per-level growth, or <paramref name="fallbackGrowth"/> when unset.</summary>
        public float GetCostGrowth(float fallbackGrowth)
        {
            if (costGrowthMultiplier <= 0f)
            {
                return Mathf.Max(1f, fallbackGrowth);
            }

            return Mathf.Max(1f, costGrowthMultiplier);
        }

        public float StatGainPerLevelFraction => Mathf.Max(0f, statGainPerLevelFraction);

        /// <summary>int.MaxValue when unlimited.</summary>
        public int MaxLevel => maxLevel <= 0 ? int.MaxValue : maxLevel;

        public bool HasLevelCap => maxLevel > 0;

        /// <summary>True when <paramref name="level"/> has reached the cap.</summary>
        public bool IsAtMaxLevel(int level)
        {
            return HasLevelCap && level >= MaxLevel;
        }

        private void OnValidate()
        {
            if (baseCost < 0d)
            {
                baseCost = 0d;
            }

            if (statGainPerLevelFraction < 0f)
            {
                statGainPerLevelFraction = 0f;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = statType.ToDisplayName();
            }
        }
    }
}