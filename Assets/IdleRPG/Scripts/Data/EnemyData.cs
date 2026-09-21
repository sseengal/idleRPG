using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// Static definition of an enemy (normal or boss).
    /// Runtime HP is scaled by stage through <see cref="Progression.FormulaUtility"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Idle RPG/Data/Enemy", order = 1)]
    public class EnemyData : ScriptableObject
    {
        public const float MinAttackIntervalSec = 0.1f;

        [Header("Identity")]
        [SerializeField] private string enemyName = "";
        [Tooltip("Stable id used by saves and content specs. Empty = asset name.")]
        [SerializeField] private string enemyID = "";

        [SerializeField] private Sprite enemySprite;

        [Header("Base Stats (stage 1)")]
        [SerializeField] private float baseHealth = 50f;
        [SerializeField] private float baseAttack = 5f;
        [Tooltip("Not in the original spec; needed to mitigate hero damage.")]
        [SerializeField] private float baseDefense = 0f;
        [SerializeField] private float baseGoldDrop = 10f;

        [Header("Behaviour")]
        [SerializeField] private bool isBoss;
        [Tooltip("Enemy attack cadence. Spec only defined it for heroes.")]
        [SerializeField] private float attackIntervalSec = 2f;

        [Tooltip("Who this enemy attacks. Inherit = use the wave's global rule (BalanceConfig.EnemyTargeting).")]
        [SerializeField] private EnemyTargetingMode targetRule = EnemyTargetingMode.Inherit;

        [Header("Boss Multipliers (applied when isBoss)")]
        [SerializeField] private float bossHealthMultiplier = 10f;
        [SerializeField] private float bossGoldMultiplier = 5f;

        [Header("Presentation")]
        [SerializeField] private Color placeholderTint = new Color(0.85f, 0.3f, 0.3f, 1f);

        public string EnemyName => string.IsNullOrEmpty(enemyName) ? name : enemyName;

        /// <summary>Stable string id (save keys, encounter specs). Falls back to the asset name.</summary>
        public string EnemyID => string.IsNullOrEmpty(enemyID) ? name : enemyID;

        public Sprite EnemySprite => enemySprite;

        public float BaseHealth => Mathf.Max(1f, baseHealth);

        public float BaseAttack => Mathf.Max(0f, baseAttack);

        public float BaseDefense => Mathf.Max(0f, baseDefense);

        public float BaseGoldDrop => Mathf.Max(0f, baseGoldDrop);

        public bool IsBoss => isBoss;

        /// <summary>Who this archetype attacks (Step 11). <see cref="EnemyTargetingMode.Inherit"/> = wave default.</summary>
        public EnemyTargetingMode TargetRule => targetRule;

        public float AttackIntervalSec => Mathf.Max(MinAttackIntervalSec, attackIntervalSec);

        public float BossHealthMultiplier => Mathf.Max(1f, bossHealthMultiplier);

        public float BossGoldMultiplier => Mathf.Max(1f, bossGoldMultiplier);

        public Color PlaceholderTint => placeholderTint;

        /// <summary>Set by the editor data generator so boss assets are always flagged consistently.</summary>
        public void EditorSetIsBoss(bool value)
        {
            isBoss = value;
        }

        public bool IsValid => BaseHealth > 0f;

        private void OnValidate()
        {
            baseHealth = Mathf.Max(1f, baseHealth);
            baseAttack = Mathf.Max(0f, baseAttack);
            baseDefense = Mathf.Max(0f, baseDefense);
            baseGoldDrop = Mathf.Max(0f, baseGoldDrop);
            attackIntervalSec = Mathf.Max(MinAttackIntervalSec, attackIntervalSec);
            bossHealthMultiplier = Mathf.Max(1f, bossHealthMultiplier);
            bossGoldMultiplier = Mathf.Max(1f, bossGoldMultiplier);

            if (string.IsNullOrEmpty(enemyName))
            {
                enemyName = name;
            }

            if (string.IsNullOrEmpty(enemyID))
            {
                enemyID = name;
            }
        }
    }
}