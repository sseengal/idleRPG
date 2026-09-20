using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;

namespace IdleRPG.UI
{
    /// <summary>
    /// Scene-level composition root for the HUD: owns the reference to the game systems and
    /// pushes the party data into the unit views once. Views stay decoupled from each other.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudController : MonoBehaviour
    {
        /// <summary>Current HUD instance, or null when the scene has no HUD (e.g. debug scenes).</summary>
        public static HudController Instance { get; private set; }

        [Header("Systems")]
        [SerializeField] private GameManager gameManager;

        [Header("Views")]
        [Tooltip("The party board (Step 10c). It builds one view per formation slot at runtime.")]
        [SerializeField] private FormationStripUI formationStrip;
        [SerializeField] private EnemyUnitView enemyView;
        [SerializeField] private FloatingDamageTextPool damageTextPool;

        public GameManager GameManager => gameManager;

        /// <summary>The damage-number pool (the Team board feeds it its anchors too).</summary>
        public FloatingDamageTextPool DamageTextPool => damageTextPool;

        /// <summary>The party board (Step 10c).</summary>
        public FormationStripUI FormationStrip => formationStrip;

        private void Awake()
        {
            Instance = this;

            if (gameManager == null)
            {
                Debug.LogError("[HudController] GameManager reference is missing; HUD disabled.");
                enabled = false;
            }
        }

        private void Start()
        {
            if (gameManager == null)
            {
                return;
            }

            // The formation strip owns one view per board slot and refreshes itself on every board change.
            if (formationStrip != null)
            {
                formationStrip.Build(gameManager, damageTextPool);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}