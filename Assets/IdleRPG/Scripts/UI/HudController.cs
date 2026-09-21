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
        [Tooltip("The battle board (display only): one view per formation slot, built at runtime.")]
        [SerializeField] private FormationBoardView formationBoard;
        [Tooltip("The enemy side: up to 3 stacked slots, each with its own HP bar.")]
        [SerializeField] private EnemyStackView enemyStack;
        [SerializeField] private FloatingDamageTextPool damageTextPool;

        public GameManager GameManager => gameManager;

        /// <summary>The damage-number pool (the Team board feeds it its anchors too).</summary>
        public FloatingDamageTextPool DamageTextPool => damageTextPool;

        /// <summary>The battle board (display only; the Party tab owns editing).</summary>
        public FormationBoardView FormationBoard => formationBoard;

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

            // The board owns one view per slot and refreshes itself on every board change.
            if (formationBoard != null)
            {
                formationBoard.Build(gameManager, damageTextPool);
            }

            if (enemyStack != null)
            {
                enemyStack.Build(gameManager, damageTextPool);
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