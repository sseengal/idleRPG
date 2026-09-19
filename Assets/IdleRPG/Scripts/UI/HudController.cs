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
        [SerializeField] private HeroUnitView[] heroViews;
        [SerializeField] private EnemyUnitView enemyView;
        [SerializeField] private FloatingDamageTextPool damageTextPool;

        public GameManager GameManager => gameManager;

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

            PartyConfig party = gameManager.Party;

            if (heroViews == null || party == null)
            {
                return;
            }

            for (int i = 0; i < heroViews.Length; i++)
            {
                HeroUnitView view = heroViews[i];
                if (view == null)
                {
                    continue;
                }

                HeroData hero = party.GetHero(i);
                view.Configure(i);
                view.Apply(hero);

                if (hero == null)
                {
                    Debug.LogWarning($"[HudController] Party has no hero at lane {i}.");
                }
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