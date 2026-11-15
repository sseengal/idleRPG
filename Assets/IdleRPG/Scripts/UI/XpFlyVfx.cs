using System.Collections.Generic;
using IdleRPG.Core;
using UnityEngine;

namespace IdleRPG.UI
{
    /// <summary>
    /// XP motes: small blue lights that pop out of the enemy that paid and fly into the XP bar.
    ///
    /// ELI5: the XP bar drinks. Each killed enemy sends a few blue sparks up and across; every spark that lands
    /// pushes the blue part of the bar a little further. The same size as a gold coin, but blue and slower, so the
    /// two streams read as two different things.
    ///
    /// The bar owns the count and the share of XP each mote carries. This effect only flies them, so the numbers
    /// can never drift from the picture. Split by job, the house pattern: this file is the component (tunables,
    /// the kill position, the spawn call and the frame driver), <c>XpFlyVfx.Pool</c> rents and builds the nodes,
    /// and <c>XpFlyVfx.Mote</c> is one mote's own movement.
    /// </summary>
    public sealed partial class XpFlyVfx : MonoBehaviour
    {
        [Header("Mote look")]
        [Tooltip("Mote diameter in canvas units. Kept equal to the gold coin so both streams sit on one scale.")]
        [SerializeField] private float moteSize = 18f;

        [Tooltip("Soft halo around a mote. Same colour family, low alpha, so the dot reads as a light.")]
        [SerializeField] private float haloScale = 2.2f;

        [Tooltip("How many fading dots follow a mote. Two is enough to show direction without a smear.")]
        [SerializeField] private int trailGhosts = 2;

        [Header("Phase 1 - pop out of the enemy")]
        [SerializeField] private float burstDurationSec = 0.14f;
        [SerializeField] private float burstSpeedMin = 70f;
        [SerializeField] private float burstSpeedMax = 140f;
        [SerializeField] private float burstGravity = 160f;
        [SerializeField] private float burstDamping = 5f;

        [Header("Phase 2 - curve into the bar")]
        [Tooltip("How long the pull lasts. Slower than gold: XP is a pour, not a snatch.")]
        [SerializeField] private float pullDurationSec = 0.45f;

        [Tooltip("Per-mote delay before the pull starts, so they arrive as a little stream.")]
        [SerializeField] private float staggerSec = 0.04f;

        [Tooltip("Sideways bow of the flight path, in canvas units. Gives the stream a liquid arc.")]
        [SerializeField] private float curveAmount = 55f;

        [Header("Pool")]
        [SerializeField] private int maxActive = 24;

        [Tooltip("Seconds after an enemy dies that its position still counts as the source of the XP.")]
        [SerializeField] private float killWindowSec = 0.35f;

        /// <summary>Current effect, or null when the scene has no battle HUD.</summary>
        public static XpFlyVfx Instance { get; private set; }

        private readonly List<Mote> pool = new List<Mote>();
        private readonly List<Mote> active = new List<Mote>();

        private FloatingDamageTextPool damagePool;
        private RectTransform hostRect;
        private Camera canvasCamera;
        private Sprite dotSprite;
        private int lastEnemyIndex = -1;
        private float lastKillAt = -10f;

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>Wires the effect. Called once by the HUD after this node is created.</summary>
        public void Initialize(FloatingDamageTextPool damageNumberPool)
        {
            damagePool = damageNumberPool;
            hostRect = transform as RectTransform;
            canvasCamera = GetComponentInParent<Canvas>()?.worldCamera;
            dotSprite = CreateMoteDot();

            int prewarm = Mathf.Max(5, maxActive / 2);

            for (int i = 0; i < prewarm; i++)
            {
                pool.Add(CreateMote());
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

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnemyKilled(string enemyName, double goldReward, int enemyIndex)
        {
            lastEnemyIndex = enemyIndex;
            lastKillAt = Time.unscaledTime;
        }

        /// <summary>
        /// Flies <paramref name="count"/> motes from the enemy just killed into the bar's fill edge. Returns false
        /// when there is nowhere to fly from or to, so the bar can fill on its own timer instead of waiting.
        /// </summary>
        public bool Play(int count)
        {
            if (hostRect == null || count <= 0)
            {
                return false;
            }

            XpBarUI bar = XpBarUI.Instance;

            if (bar == null || Time.unscaledTime - lastKillAt > killWindowSec)
            {
                return false;
            }

            RectTransform start = damagePool != null ? damagePool.GetEnemyAnchor(lastEnemyIndex) : null;

            if (start == null)
            {
                return false;
            }

            if (!TryStartLocal(start, out Vector2 startLocal) || !TryEndLocal(bar, out Vector2 endLocal))
            {
                return false;
            }

            Burst(startLocal, endLocal, count);
            return true;
        }

        private bool TryStartLocal(RectTransform start, out Vector2 local)
        {
            Vector3 world = start.TransformPoint(start.rect.center);
            Vector2 screen;

            if (!BattleStageSurface.TryStageToScreen(world, out screen))
            {
                screen = RectTransformUtility.WorldToScreenPoint(canvasCamera, world);
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(hostRect, screen, canvasCamera, out local);
        }

        private bool TryEndLocal(XpBarUI bar, out Vector2 local)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(canvasCamera, bar.FillEdgeWorld);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(hostRect, screen, canvasCamera, out local);
        }

        private void Burst(Vector2 startLocal, Vector2 endLocal, int count)
        {
            Vector2 away = startLocal - endLocal;

            if (away.sqrMagnitude < 0.0001f)
            {
                away = Vector2.up;
            }

            away.Normalize();

            for (int i = 0; i < count; i++)
            {
                if (active.Count >= maxActive)
                {
                    return;
                }

                Mote mote = Rent();

                if (mote == null)
                {
                    return;
                }

                // A tight fan (not the gold coin's wide scatter): the motes rise and curve, they do not explode.
                Vector2 direction = Quaternion.Euler(0f, 0f, Random.Range(-28f, 28f)) * away;
                float speed = Random.Range(burstSpeedMin, burstSpeedMax);
                float bow = Random.Range(0.6f, 1f) * (Random.value < 0.5f ? -1f : 1f);

                mote.Start(startLocal, endLocal, direction, speed, i * staggerSec, bow);
                active.Add(mote);
            }
        }

        private void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Mote mote = active[i];

                if (mote == null)
                {
                    active.RemoveAt(i);
                    continue;
                }

                if (!mote.Tick(Time.unscaledDeltaTime))
                {
                    active.RemoveAt(i);
                    pool.Add(mote);
                }
            }
        }

        /// <summary>Called by a mote the moment it reaches the bar. One landing = one share of XP.</summary>
        private void OnMoteLanded()
        {
            XpBarUI.Instance?.SignalMoteArrived();
        }
    }
}

