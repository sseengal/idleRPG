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

        [Tooltip("How much bigger a boss draws than a normal enemy of the same art. 1 = same size. 1.6 = 60% bigger, which is the old hard-coded boss box (128 -> 205 px). Written from the content spec.")]
        [SerializeField] private float bossSizeScale = 1.6f;

        [Header("Presentation")]
        [SerializeField] private Color placeholderTint = new Color(0.85f, 0.3f, 0.3f, 1f);

        [Tooltip("Animated frames (Idle/Attack/Hurt/Death). When set, the unit is rendered from these frames and the tint above is ignored.")]
        [SerializeField] private CharacterArtSet artSet;

        [Tooltip("Alternate looks rotated by stage (index = stage % length). Used to give one archetype several skins across a run, e.g. Demon/Blood Monster on the band-1 wave.")]
        [SerializeField] private CharacterArtSet[] stageAlternates;

        [Tooltip("Abilities this enemy may cast (empty = none). Enemies are not gated by unlock stage or tokens.")]
        [SerializeField] private AbilityDef[] abilities;

        [Header("Stage window")]
        [Tooltip("First stage this enemy may appear on. 1 = available from the start.")]
        [SerializeField] private int firstStage = 1;

        [Tooltip("Last stage this enemy may appear on. 0 = no limit (available to the end).")]
        [SerializeField] private int lastStage = 0;

        /// <summary>First stage this enemy may appear on (never below 1).</summary>
        public int FirstStage => Mathf.Max(1, firstStage);

        /// <summary>Last stage this enemy may appear on, or 0 for no limit.</summary>
        public int LastStage => Mathf.Max(0, lastStage);

        /// <summary>
        /// True when this enemy may be picked at <paramref name="stage"/>.
        /// With the defaults (1 and 0) every enemy is available at every stage, which is exactly how the wave pool
        /// behaved before stage windows existed - so an asset that never sets these fields is unaffected.
        /// </summary>
        public bool IsAvailableAt(int stage)
        {
            if (stage < FirstStage)
            {
                return false;
            }

            return LastStage == 0 || stage <= LastStage;
        }

        /// <summary>Set by the editor data generator from the content spec.</summary>
        public void EditorSetStageWindow(int first, int last)
        {
            firstStage = Mathf.Max(1, first);
            lastStage = Mathf.Max(0, last);
        }

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

        /// <summary>Abilities this enemy may cast, never null. Enemies cast on their own cooldown.</summary>
        public AbilityDef[] Abilities => abilities ?? new AbilityDef[0];

        public float AttackIntervalSec => Mathf.Max(MinAttackIntervalSec, attackIntervalSec);

        public float BossHealthMultiplier => Mathf.Max(1f, bossHealthMultiplier);

        public float BossGoldMultiplier => Mathf.Max(1f, bossGoldMultiplier);

        /// <summary>How much bigger a boss draws than a normal enemy of the same art (1 = same size).</summary>
        public float BossSizeScale => Mathf.Max(1f, bossSizeScale);

        public Color PlaceholderTint => placeholderTint;

        /// <summary>Animated frames when real art is wired; null for placeholders/fallbacks.</summary>
        public CharacterArtSet ArtSet => artSet;

        /// <summary>Stages 0,1,2 -> alternates[0],alternates[1],alternates[0]... or the default ArtSet.</summary>
        public CharacterArtSet ResolveArt(int stage)
        {
            if (stageAlternates != null && stageAlternates.Length > 0)
            {
                int index = Mathf.Max(0, stage) % stageAlternates.Length;
                return stageAlternates[index];
            }

            return artSet;
        }

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