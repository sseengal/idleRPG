using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;

namespace IdleRPG.UI
{
    /// <summary>
    /// Gold fly effect, two acts per coin:
    ///   1. BURST  - coins pop out of the dead enemy with their own direction + fake gravity, decelerating fast.
    ///   2. MAGNET - the instant the burst decays, each coin is sucked to the counter (no settle, no bounce).
    /// Each coin also drags a comet trail of fading ghost dots, so a kill reads as a stash of coins, not one dot.
    ///
    /// The real gold was already banked by the kill event; this is only the visible "money travel" moment.
    ///
    /// Split by job, the house pattern: this file is the component (tunables, state, the kill hook and the
    /// per-frame driver), <c>GoldFlyVfx.Pool</c> rents and builds the coin nodes, and <c>GoldFlyVfx.Coin</c> is
    /// the coin's own behaviour. Pooled: nodes are created once and recycled, one Update() drives them all.
    /// </summary>
    public sealed partial class GoldFlyVfx : MonoBehaviour
    {
        [Header("Phase 1 - burst out of the enemy")]
        [Tooltip("Coins per kill. Each coin gets its own direction, so 8 coins read as 8 coins.")]
        [SerializeField] private int coinsPerKill = 8;

        [Tooltip("How long a coin flies away from the enemy (outward pop).")]
        [SerializeField] private float burstDurationSec = 0.3f;

        [Tooltip("Burst speed range, canvas units/s. Coins scatter, they do not stream in a line.")]
        [SerializeField] private float burstSpeedMin = 90f;
        [SerializeField] private float burstSpeedMax = 190f;

        [Tooltip("Fake gravity that bends the burst downward, so coins 'fall' away from the counter first.")]
        [SerializeField] private float burstGravity = 260f;

        [Tooltip("How fast the burst slows down (per second). Higher = coins decelerate and get pulled back sooner.")]
        [SerializeField] private float burstDamping = 4.5f;

        [Header("Phase 2 - magnet into the counter")]
        [Tooltip("How long the pull lasts. Slower = the eye tracks each coin all the way in.")]
        [SerializeField] private float magnetDurationSec = 0.6f;

        [Tooltip("Per-coin delay before the magnet starts, so coins arrive in a stream.")]
        [SerializeField] private float magnetStaggerSec = 0.06f;

        [Header("Coin look")]
        [Tooltip("Coin diameter in canvas units. Bigger dots read as distinct coins.")]
        [SerializeField] private float coinSize = 18f;

        [Tooltip("Gold tint (soft warm yellow).")]
        [SerializeField] private Color coinColor = new Color(1f, 0.82f, 0.28f, 1f);

        [Header("Trail (ghost dots)")]
        [Tooltip("How many fading ghost dots follow a coin. A long comet tail, not one smear.")]
        [SerializeField] private int trailGhosts = 8;

        [Tooltip("Ghost alpha at the newest dot; ghosts fade to fully transparent by the oldest.")]
        [SerializeField] private float trailAlpha = 0.55f;

        [Tooltip("Ghost dot diameter in canvas units (the newest ghost is biggest).")]
        [SerializeField] private float trailGhostSize = 12f;

        [Header("Pool")]
        [Tooltip("Upper bound on simultaneously flying coins (burst, settle, magnet all count).")]
        [SerializeField] private int maxActive = 32;

        [Tooltip("Minimum gap between two bursts. Fast-forward kills arrive fast; a burst every frame is a blur.")]
        [SerializeField] private float minBurstGapSec = 0.04f;

        private readonly List<Coin> pool = new List<Coin>();
        private readonly List<Coin> active = new List<Coin>();

        private FloatingDamageTextPool damagePool;
        private RectTransform hostRect;
        private Camera canvasCamera;
        private Sprite coinSprite;
        private float lastBurstTime = -10f;
/// <summary>Wires the effect. Called once by the HUD after this node is created.</summary>
        public void Initialize(FloatingDamageTextPool damageNumberPool)
        {
            damagePool = damageNumberPool;
            hostRect = transform as RectTransform;
            canvasCamera = GetComponentInParent<Canvas>()?.worldCamera;
            coinSprite = CreateGoldDot();

            int prewarm = Mathf.Max(6, maxActive / 2);

            for (int i = 0; i < prewarm; i++)
            {
                pool.Add(CreateCoin());
            }
        }

        private void OnEnable()
        {
            GameEvents.EnemyKilled += OnEnemyKilled;
        }

        private void OnDisable()
        {
            GameEvents.EnemyKilled -= OnEnemyKilled;
        }

        private void OnEnemyKilled(string enemyName, double goldReward, int enemyIndex)
        {
            if (damagePool == null || hostRect == null)
            {
                return;
            }

            HudHeaderUI header = HudHeaderUI.Instance;
            if (header == null || header.GoldAnchor == null)
            {
                return;
            }

            // Cross-canvas-safe: convert both points through screen space into THIS canvas' local coordinates.
            RectTransform start = damagePool.GetEnemyAnchor(enemyIndex);
            Vector2 startLocal;
            Vector2 endLocal;

            if (start == null)
            {
                return;
            }

            Vector3 startWorld = start.TransformPoint(start.rect.center);
            Vector2 startScreen;

            if (!BattleStageSurface.TryStageToScreen(startWorld, out startScreen))
            {
                startScreen = RectTransformUtility.WorldToScreenPoint(canvasCamera, startWorld);
            }

            Vector2 endScreen = RectTransformUtility.WorldToScreenPoint(
                canvasCamera,
                header.GoldAnchor.TransformPoint(header.GoldAnchor.rect.center));

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(hostRect, startScreen, canvasCamera, out startLocal)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(hostRect, endScreen, canvasCamera, out endLocal))
            {
                return;
            }

            // Throttle: pace x1.6 + a 3-enemy wave fires kills quickly; keep the screen calm.
            if (Time.time - lastBurstTime < minBurstGapSec)
            {
                return;
            }

            lastBurstTime = Time.time;
            BurstTowards(startLocal, endLocal, coinsPerKill);
        }

        private void BurstTowards(Vector2 startLocal, Vector2 endLocal, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (active.Count >= maxActive)
                {
                    return;
                }

                Coin coin = Rent();
                if (coin == null)
                {
                    return;
                }

                // Every coin gets its own fan direction (away from the counter, biased downward) + speed,
                // so the burst spreads instead of launching 8 coins along one line.
                Vector2 away = (startLocal - endLocal).normalized;
                float fanAngle = Random.Range(-65f, 65f);
                Vector2 direction = Quaternion.Euler(0f, 0f, fanAngle) * away;
                float speed = Random.Range(burstSpeedMin, burstSpeedMax);

                coin.Start(startLocal, endLocal, direction, speed,
                    i * magnetStaggerSec);
                active.Add(coin);
            }
        }

        private void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Coin coin = active[i];

                if (coin == null)
                {
                    active.RemoveAt(i);
                    continue;
                }

                if (!coin.Tick(Time.deltaTime))
                {
                    active.RemoveAt(i);
                    pool.Add(coin);
                }
            }
        }
    }
}
