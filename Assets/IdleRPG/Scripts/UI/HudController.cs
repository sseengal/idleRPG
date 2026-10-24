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

        [Tooltip("The modal canvas: every card (ranks, defeat, offline) is parented here so it sits above the HUD " +
                 "and the damage numbers.")]
        [SerializeField] private RectTransform modalRoot;

        [Header("Views")]
        [Tooltip("The battle board (display only): one view per formation slot, built at runtime.")]
        [SerializeField] private FormationBoardView formationBoard;
        [Tooltip("The enemy side: up to 3 stacked slots, each with its own HP bar.")]
        [SerializeField] private EnemyStackView enemyStack;
        [SerializeField] private FloatingDamageTextPool damageTextPool;

        public GameManager GameManager => gameManager;

        /// <summary>The modal canvas - where every card belongs (above the HUD and the damage numbers).</summary>
        public RectTransform ModalRoot => modalRoot;

        /// <summary>The damage-number pool (the Team board feeds it its anchors too).</summary>
        public FloatingDamageTextPool DamageTextPool => damageTextPool;

        public EnemyStackView EnemyStack => enemyStack;

        /// <summary>The battle board: one view per hero (which can move between pads) and six pads to move to.</summary>
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

                // The first wave can already have spawned (this Start may run after the GameManager's), so lay the
                // stack out now instead of waiting for the next spawn - otherwise the enemy area stays blank for a
                // whole wave. Refresh() is safe when nothing has spawned yet: it just leaves the slots off.
                enemyStack.Refresh();
            }

            // Gold-fly effect: coins launch from a dead enemy into the top-bar counter. The node is created on the
            // same canvas that hosts the damage numbers, so coordinate space is shared.
            if (damageTextPool != null)
            {
                Transform hostParent = damageTextPool.transform.parent != null ? damageTextPool.transform.parent : transform;
                GameObject host = UiRuntime.CreateNode("GoldFlyVfx", hostParent);

                // Coin positions are resolved as local points of this rect, so give it the full parent size.
                if (host.transform is RectTransform hostRect)
                {
                    UiRuntime.Stretch(hostRect);
                }

                GoldFlyVfx vfx = host.AddComponent<GoldFlyVfx>();
                vfx.Initialize(damageTextPool);
            }

            // The one planner of the fight's moving picture: it decides who stands where for every beat. Without it
            // nobody stages a swing at all, so it is created here, on the object that owns the views.
            if (GetComponent<BeatDirector>() == null)
            {
                gameObject.AddComponent<BeatDirector>();
            }

            // The cutscene projector: bars, a title line, and the pause it needs. Created here for the same reason.
            CutsceneRunner runner = GetComponent<CutsceneRunner>();

            if (runner == null)
            {
                runner = gameObject.AddComponent<CutsceneRunner>();
            }

            runner.Initialize(modalRoot != null ? modalRoot : (RectTransform)transform);
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