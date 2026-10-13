using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// Static definition of one ability: what it does, which art slot plays it, how often it may fire, and what it
    /// costs the player to unlock. Written from abilities.json by the content generator.
    ///
    /// A hero or enemy points at one of these by id (see <see cref="HeroData"/> / <see cref="EnemyData"/>), so the
    /// same definition is shared by everyone who uses it - the six blocking enemies all name the same "enemy_block".
    ///
    /// The cast is INSTANT (Ability-Plan decision 1): there is no wind-up and no channel. The cooldown is the only
    /// gate, and it is measured in battle time, so pausing the fight pauses the clock too.
    /// </summary>
    [CreateAssetMenu(fileName = "Ability", menuName = "Idle RPG/Data/Ability", order = 3)]
    public class AbilityDef : ScriptableObject
    {
        public const float MinCooldownSec = 1f;

        [Header("Identity")]
        [SerializeField] private string displayName = "";
        [Tooltip("Stable id used by saves and content specs. Empty = asset name.")]
        [SerializeField] private string abilityID = "";
        [TextArea]
        [SerializeField] private string description = "";

        [Header("Effect")]
        [SerializeField] private AbilityKind kind = AbilityKind.Damage;
        [SerializeField] private AbilityTarget target = AbilityTarget.FrontEnemy;

        [Tooltip("Which art slot plays it: \"Ability\" (the character's own special strip) or a numbered attack strip such as \"Attack02\". A character without that strip simply does not play one.")]
        [SerializeField] private string artSlot = "Ability";

        [Tooltip("Seconds between casts. The cast itself is instant; this is the clock that refills it.")]
        [SerializeField] private float cooldownSec = 12f;

        [Header("Shield")]
        [Tooltip("Damage taken is multiplied by this while the shield is up. 0.5 = half damage.")]
        [SerializeField] private float shieldFactor = 0.5f;
        [SerializeField] private float shieldSeconds = 4f;

        [Header("Heal")]
        [Tooltip("Fraction of the target's MAXIMUM health restored.")]
        [SerializeField] private float healFraction = 0.25f;

        [Header("Damage")]
        [Tooltip("Multiplier applied to the caster's attack.")]
        [SerializeField] private float damageMultiplier = 1.8f;

        [Header("Unlock")]
        [Tooltip("Stage the player must have reached before it can be bought. 0 = no gate.")]
        [SerializeField] private int unlockStage = 0;
        [Tooltip("Prestige tokens it costs. 0 = free, which is how the starting ability stays usable at once.")]
        [SerializeField] private double tokenCost = 0d;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        /// <summary>Stable string id (save keys, content specs). Falls back to the asset name.</summary>
        public string AbilityID => string.IsNullOrEmpty(abilityID) ? name : abilityID;

        public string Description => description;

        public AbilityKind Kind => kind;

        public AbilityTarget Target => target;

        /// <summary>Name of the art slot that plays the cast ("Ability" or an attack strip name).</summary>
        public string ArtSlot => string.IsNullOrEmpty(artSlot) ? "Ability" : artSlot;

        public float CooldownSec => Mathf.Max(MinCooldownSec, cooldownSec);

        /// <summary>Damage multiplier while shielded, kept in (0, 1]: a shield can never make damage worse.</summary>
        public float ShieldFactor => Mathf.Clamp(shieldFactor, 0.05f, 1f);

        public float ShieldSeconds => Mathf.Max(0f, shieldSeconds);

        public float HealFraction => Mathf.Clamp01(healFraction);

        public float DamageMultiplier => Mathf.Max(0f, damageMultiplier);

        public int UnlockStage => Mathf.Max(0, unlockStage);

        public double TokenCost => tokenCost < 0d ? 0d : tokenCost;

        /// <summary>True when the player's progress has reached the gate (stage 0 = ungated).</summary>
        public bool IsAvailableAt(int stage)
        {
            return stage >= UnlockStage;
        }
    }
}
