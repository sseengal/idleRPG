using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// Static definition of a hero. Balance data only — runtime state (levels, HP) lives elsewhere.
    /// </summary>
    [CreateAssetMenu(fileName = "HeroData", menuName = "Idle RPG/Data/Hero", order = 0)]
    public class HeroData : ScriptableObject
    {
        public const float MinAttackIntervalSec = 0.1f;

        [Header("Identity")]
        [SerializeField] private string heroID = "";
        [SerializeField] private string heroName = "";
        [SerializeField] private Sprite heroIcon;

        [Header("Base Stats (level 0)")]
        [SerializeField] private float baseHealth = 100f;
        [SerializeField] private float baseAttack = 10f;
        [SerializeField] private float baseDefense = 5f;
        [Tooltip("Seconds between attacks. Default 1.5s per design spec.")]
        [SerializeField] private float attackIntervalSec = 1.5f;

        [Header("Crit (level 0)")]
        [Tooltip("Base critical-hit chance, 0..1. 0.05 matches the MVP global default, so level 0 changes nothing.")]
        [SerializeField] private float baseCritChance = 0.05f;

        [Tooltip("Base critical damage multiplier. 2 = double damage (the MVP global default).")]
        [SerializeField] private float baseCritDamage = 2f;

        [Header("Combat role (Step 10)")]
        [Tooltip("Used by auto-arrange: tanks are pushed to the front row first.")]
        [SerializeField] private HeroRole role = HeroRole.Damage;

        [Header("Presentation")]
        [Tooltip("Optional tint for the placeholder sprite while real art is pending.")]
        [SerializeField] private Color placeholderTint = Color.white;

        [Tooltip("Animated frames (Idle/Attack/Hurt/Death). When set, the unit is rendered from these frames and the tint above is ignored (real art is coloured).")]
        [SerializeField] private CharacterArtSet artSet;

        public string HeroID => string.IsNullOrEmpty(heroID) ? name : heroID;

        public string HeroName => string.IsNullOrEmpty(heroName) ? name : heroName;

        public Sprite HeroIcon => heroIcon;

        public float BaseHealth => Mathf.Max(1f, baseHealth);

        public float BaseAttack => Mathf.Max(0f, baseAttack);

        public float BaseDefense => Mathf.Max(0f, baseDefense);

        public float AttackIntervalSec => Mathf.Max(MinAttackIntervalSec, attackIntervalSec);

        /// <summary>Base critical-hit chance at level 0 (0..1).</summary>
        public float BaseCritChance => Mathf.Clamp(baseCritChance, 0f, 1f);

        /// <summary>Base critical-hit damage multiplier at level 0 (1 = never crits harder).</summary>
        public float BaseCritDamage => Mathf.Max(1f, baseCritDamage);

        public Color PlaceholderTint => placeholderTint;

        /// <summary>Animated frames when real art is wired; null for placeholders/fallbacks.</summary>
        public CharacterArtSet ArtSet => artSet;

        public HeroRole Role => role;

        /// <summary>True when the asset is safe to use at runtime.</summary>
        public bool IsValid => BaseAttack > 0f && BaseHealth > 0f;

        private void OnValidate()
        {
            baseHealth = Mathf.Max(1f, baseHealth);
            baseAttack = Mathf.Max(0f, baseAttack);
            baseDefense = Mathf.Max(0f, baseDefense);
            attackIntervalSec = Mathf.Max(MinAttackIntervalSec, attackIntervalSec);
            baseCritChance = Mathf.Clamp(baseCritChance, 0f, 1f);
            baseCritDamage = Mathf.Max(1f, baseCritDamage);

            if (string.IsNullOrEmpty(heroID))
            {
                heroID = name;
            }

            if (string.IsNullOrEmpty(heroName))
            {
                heroName = name;
            }
        }
    }
}