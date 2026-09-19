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

        [Header("Presentation")]
        [Tooltip("Optional tint for the placeholder sprite while real art is pending.")]
        [SerializeField] private Color placeholderTint = Color.white;

        public string HeroID => string.IsNullOrEmpty(heroID) ? name : heroID;

        public string HeroName => string.IsNullOrEmpty(heroName) ? name : heroName;

        public Sprite HeroIcon => heroIcon;

        public float BaseHealth => Mathf.Max(1f, baseHealth);

        public float BaseAttack => Mathf.Max(0f, baseAttack);

        public float BaseDefense => Mathf.Max(0f, baseDefense);

        public float AttackIntervalSec => Mathf.Max(MinAttackIntervalSec, attackIntervalSec);

        public Color PlaceholderTint => placeholderTint;

        /// <summary>True when the asset is safe to use at runtime.</summary>
        public bool IsValid => BaseAttack > 0f && BaseHealth > 0f;

        private void OnValidate()
        {
            baseHealth = Mathf.Max(1f, baseHealth);
            baseAttack = Mathf.Max(0f, baseAttack);
            baseDefense = Mathf.Max(0f, baseDefense);
            attackIntervalSec = Mathf.Max(MinAttackIntervalSec, attackIntervalSec);

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