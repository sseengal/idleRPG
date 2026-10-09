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
    /// Pooled: coin nodes (dot + ghosts) are created once and recycled. One Update() drives every active coin.
    /// </summary>
    public sealed class GoldFlyVfx : MonoBehaviour
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

            if (start == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(hostRect,
                    RectTransformUtility.WorldToScreenPoint(canvasCamera, start.TransformPoint(start.rect.center)),
                    canvasCamera, out startLocal)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(hostRect,
                    RectTransformUtility.WorldToScreenPoint(canvasCamera, header.GoldAnchor.TransformPoint(header.GoldAnchor.rect.center)),
                    canvasCamera, out endLocal))
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

        private Coin Rent()
        {
            while (pool.Count > 0)
            {
                Coin candidate = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);

                if (candidate != null)
                {
                    return candidate;
                }
            }

            return CreateCoin();
        }

        private Coin CreateCoin()
        {
            GameObject node = UiRuntime.CreateNode("Coin", transform);
            RectTransform rect = node.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(coinSize, coinSize);
            }

            // The front dot (the coin itself).
            GameObject dotNode = UiRuntime.CreateNode("Dot", rect);
            Image dot = dotNode.AddComponent<Image>();
            dot.sprite = coinSprite;
            dot.color = coinColor;
            dot.raycastTarget = false;

            RectTransform dotRect = dotNode.transform as RectTransform;
            if (dotRect != null)
            {
                dotRect.anchorMin = new Vector2(0.5f, 0.5f);
                dotRect.anchorMax = new Vector2(0.5f, 0.5f);
                dotRect.pivot = new Vector2(0.5f, 0.5f);
                dotRect.sizeDelta = new Vector2(coinSize, coinSize);
            }

            // The comet tail: trailGhosts ghost dots, each a child of the coin node, positioned
            // (ghostLocal - current) so they trail the coin inside the coin's own coordinate space.
            int ghosts = Mathf.Max(2, trailGhosts);
            Image[] ghostImages = new Image[ghosts];

            for (int g = 0; g < ghosts; g++)
            {
                GameObject ghostNode = UiRuntime.CreateNode($"Ghost{g}", rect);
                Image ghost = ghostNode.AddComponent<Image>();
                ghost.sprite = coinSprite;
                ghost.color = coinColor;
                ghost.raycastTarget = false;
                ghost.enabled = false;

                RectTransform ghostRect = ghostNode.transform as RectTransform;
                if (ghostRect != null)
                {
                    ghostRect.anchorMin = new Vector2(0.5f, 0.5f);
                    ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
                    ghostRect.pivot = new Vector2(0.5f, 0.5f);

                    float age = ghosts > 1 ? (float)g / (ghosts - 1) : 0f;
                    float size = Mathf.Lerp(trailGhostSize, trailGhostSize * 0.4f, age);
                    ghostRect.sizeDelta = new Vector2(size, size);
                }

                ghostImages[g] = ghost;
            }

            node.SetActive(false);
            return new Coin(this, rect, dot, ghostImages);
        }
/// <summary>A soft gold dot, generated once in code so no texture asset sits in the repo.</summary>
        private static Sprite CreateGoldDot()
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - (size - 1) * 0.5f;
                    float dy = y - (size - 1) * 0.5f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) / (size * 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - distance)));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// One flying coin. Two acts only:
        ///   1. BURST  - pop out with its own fan direction, fake gravity, and damping (it decelerates).
        ///   2. MAGNET - as soon as the burst runs out, the coin is pulled to the counter and melts in.
        /// No settle, no bounce, no pause — the pull starts the instant the burst decays.
        /// Not a MonoBehaviour: the pool ticks it.
        /// </summary>
        private sealed class Coin
        {
            private readonly GoldFlyVfx owner;
            private readonly RectTransform rect;
            private readonly Image dot;
            private readonly Image[] ghostImages;

            // Fixed per-instance trail history (indices: 0 = newest, N-1 = oldest).
            private readonly Vector2[] history;
            private int historyCount;
            private bool arrived;

            private Vector2 start;
            private Vector2 end;
            private Vector2 position;
            private Vector2 velocity;
            private Vector2 magnetStart;
            private float magnetDelay;
            private float burstEnd;
            private float settleEnd;
            private bool absorbStartedAtSet;
            private float absorbStartedAt = -1f;

            public Coin(GoldFlyVfx effect, RectTransform rectTransform, Image dotImage, Image[] ghosts)
            {
                owner = effect;
                rect = rectTransform;
                dot = dotImage;
                ghostImages = ghosts;
                history = new Vector2[Mathf.Max(2, ghostImages != null ? ghostImages.Length : 2)];
            }

            public void Start(Vector2 startLocal, Vector2 endLocal, Vector2 direction, float speed,
                float stagger)
            {
                start = startLocal;
                end = endLocal;
                position = startLocal;
                velocity = direction * speed;
                magnetDelay = stagger;

                burstEnd = Time.time + owner.burstDurationSec;
                settleEnd = burstEnd; // magnet begins exactly when the burst ends — no pause.

                arrived = false;
                historyCount = 0;
                absorbStartedAt = -1f;
                absorbStartedAtSet = false;

                if (rect != null)
                {
                    rect.gameObject.SetActive(true);
                    rect.anchoredPosition = start;
                    rect.localScale = Vector3.one;
                }

                if (dot != null)
                {
                    dot.color = owner.coinColor;
                    dot.rectTransform.localScale = Vector3.one;
                }

                HideGhosts();
            }

            private void HideGhosts()
            {
                if (ghostImages == null)
                {
                    return;
                }

                for (int g = 0; g < ghostImages.Length; g++)
                {
                    if (ghostImages[g] != null)
                    {
                        ghostImages[g].enabled = false;
                    }
                }
            }

            /// <summary>Advances the coin. Returns false when the coin is finished (pool it).</summary>
            public bool Tick(float deltaTime)
            {
                if (rect == null)
                {
                    return false;
                }

                float now = Time.time;

                if (now <= burstEnd)
                {
                    // Act 1 - burst: integrate velocity with fake gravity + damping so the coin visibly
                    // decelerates right before the magnet picks it up.
                    velocity += Vector2.down * (owner.burstGravity * deltaTime);
                    velocity *= Mathf.Exp(-owner.burstDamping * deltaTime);
                    position += velocity * deltaTime;
                }
                else
                {
                    // Act 2 - magnet: pull starts immediately at the burst-decay point, no rest frame in between.
                    position = Magnet(now);
                }

                rect.anchoredPosition = position;

                // Absorb: once the coin reaches the counter it shrinks + fades into the number, then recycles.
                if (absorbStartedAt >= 0f)
                {
                    float soak = now - absorbStartedAt;
                    float fade = Mathf.Clamp01(1f - soak / 0.12f);

                    if (dot != null)
                    {
                        Color color = dot.color;
                        color.a = fade;
                        dot.color = color;
                    }

                    rect.localScale = new Vector3(fade > 0f ? fade : 0.01f, fade > 0f ? fade : 0.01f, 1f);

                    if (fade <= 0f)
                    {
                        return false;
                    }
                }

                PushHistory(position);
                DrawHistory(position);
                return true;
            }

            /// <summary>Act 2 - magnet: the coin is pulled to the counter from wherever its burst decayed to.</summary>
            private Vector2 Magnet(float now)
            {
                if (!absorbStartedAtSet)
                {
                    // First magnet frame: lock the pull to start from the exact spot the burst decayed to,
                    // so the hand-off is continuous (no teleport, no pause).
                    magnetStart = position;
                    absorbStartedAtSet = true;
                }

                float t = Mathf.Clamp01((now - settleEnd - magnetDelay) / Mathf.Max(0.0001f, owner.magnetDurationSec));

                if (t >= 1f)
                {
                    if (!arrived)
                    {
                        arrived = true;
                        // Counter count-up starts when the FIRST coin of a burst lands.
                        HudHeaderUI.Instance?.SignalCoinsArrived();
                        absorbStartedAt = now;
                    }

                    // Hold briefly at the counter so the absorb fade starts from the exact spot.
                    return end;
                }

                // Ease-in (accelerating pull from the decayed rest), with an ease-out 'soften' tail at the end.
                float eased = EaseInCubic(t);
                Vector2 pulled = Vector2.Lerp(magnetStart, end, eased);

                if (t > 0.8f)
                {
                    pulled = Vector2.Lerp(pulled, end, EaseOutCubic((t - 0.8f) / 0.2f));
                }

                return pulled;
            }

            private void PushHistory(Vector2 position)
            {
                for (int i = history.Length - 1; i > 0; i--)
                {
                    history[i] = history[i - 1];
                }

                history[0] = position;
                historyCount = Mathf.Min(history.Length, historyCount + 1);
            }

            private void DrawHistory(Vector2 current)
            {
                if (ghostImages == null)
                {
                    return;
                }

                for (int g = 0; g < ghostImages.Length; g++)
                {
                    Image ghost = ghostImages[g];

                    if (ghost == null)
                    {
                        continue;
                    }

                    // Ghost g shows the position 1 slot older than the dot's own. The oldest ghosts fall off.
                    int index = g + 1;
                    if (index >= historyCount || index >= history.Length)
                    {
                        ghost.enabled = false;
                        continue;
                    }

                    ghost.enabled = true;

                    float age = historyCount > 1 ? (float)index / (historyCount - 1) : 1f;
                    Color color = owner.coinColor;
                    color.a = owner.trailAlpha * (1f - age);
                    ghost.color = color;

                    if (ghost.rectTransform != null)
                    {
                        ghost.rectTransform.anchoredPosition = history[index] - current;
                    }
                }
            }

            private static float EaseInCubic(float t)
            {
                return t * t * t;
            }

            private static float EaseOutCubic(float t)
            {
                float p = 1f - t;
                return 1f - p * p * p;
            }
        }
    }
}