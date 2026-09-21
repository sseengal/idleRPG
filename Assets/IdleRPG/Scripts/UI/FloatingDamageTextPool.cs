using System.Collections.Generic;
using UnityEngine;
using IdleRPG.Core;

namespace IdleRPG.UI
{
    /// <summary>
    /// Pool of floating combat numbers. Subscribes to the combat events, reuses instances and
    /// drives every active text from a single Update loop (no allocations per hit).
    /// </summary>
    public sealed class FloatingDamageTextPool : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private FloatingDamageTextView prefab;
        [SerializeField] private RectTransform enemyAnchor;
        [Tooltip("One anchor per enemy slot (1-3); takes priority over enemyAnchor when set by the stack.")]
        [SerializeField] private RectTransform[] enemyAnchors;
        [SerializeField] private RectTransform[] heroAnchors;

        [Header("Pool")]
        [SerializeField] private int prewarmCount = 8;
        [SerializeField] private int maxActive = 24;
        [SerializeField] private float anchorJitter = 26f;

        private readonly List<FloatingDamageTextView> pool = new List<FloatingDamageTextView>();
        private readonly List<FloatingDamageTextView> active = new List<FloatingDamageTextView>();
        private float driftSeed;

        /// <summary>
        /// Replaces the per-hero damage anchors. The formation strip calls this whenever the board changes, so a
        /// swapped hero keeps its numbers coming out of the right slot.
        /// </summary>
        public void SetHeroAnchors(RectTransform[] anchors)
        {
            heroAnchors = anchors;
        }

        private void Start()
        {
            if (prefab == null)
            {
                Debug.LogError("[FloatingDamageTextPool] No prefab assigned.");
                enabled = false;
                return;
            }

            for (int i = 0; i < Mathf.Max(0, prewarmCount); i++)
            {
                pool.Add(CreateInstance());
            }
        }

        private void OnEnable()
        {
            GameEvents.EnemyDamaged += OnEnemyDamaged;
            GameEvents.HeroDamaged += OnHeroDamaged;
        }

        private void OnDisable()
        {
            GameEvents.EnemyDamaged -= OnEnemyDamaged;
            GameEvents.HeroDamaged -= OnHeroDamaged;
        }

        /// <summary>Replaces the per-enemy damage anchors so numbers come out of the enemy that was hit.</summary>
        public void SetEnemyAnchors(RectTransform[] anchors)
        {
            enemyAnchors = anchors;
        }

        private void OnEnemyDamaged(EnemyDamagedInfo info)
        {
            if (info.Damage <= 0d)
            {
                return;
            }

            Spawn(info.Damage, EnemyAnchor(info.EnemyIndex), info.IsCritical ? FloatingTextStyle.Critical : FloatingTextStyle.Normal);
        }

        private RectTransform EnemyAnchor(int enemyIndex)
        {
            if (enemyAnchors == null || enemyAnchors.Length == 0)
            {
                return enemyAnchor;
            }

            return enemyAnchors[Mathf.Clamp(enemyIndex, 0, enemyAnchors.Length - 1)];
        }

        private void OnHeroDamaged(int heroIndex, double damage, double currentHealth, double maxHealth)
        {
            if (damage <= 0d || heroAnchors == null || heroIndex < 0 || heroIndex >= heroAnchors.Length)
            {
                return;
            }

            Spawn(damage, heroAnchors[heroIndex], FloatingTextStyle.Incoming);
        }

        private void Spawn(double value, RectTransform anchor, FloatingTextStyle style)
        {
            if (anchor == null || active.Count >= Mathf.Max(1, maxActive))
            {
                return;
            }

            FloatingDamageTextView view = Rent();
            if (view == null)
            {
                return;
            }

            Vector2 position = anchor.anchoredPosition + new Vector2(
                Random.Range(-anchorJitter, anchorJitter),
                Random.Range(0f, anchorJitter));

            driftSeed += 1.37f;
            view.transform.SetParent(anchor.parent, false);
            view.Play(value, position, style, driftSeed);
            active.Add(view);
        }

        private FloatingDamageTextView Rent()
        {
            while (pool.Count > 0)
            {
                FloatingDamageTextView candidate = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);

                if (candidate != null)
                {
                    return candidate;
                }
            }

            return CreateInstance();
        }

        private FloatingDamageTextView CreateInstance()
        {
            FloatingDamageTextView instance = Instantiate(prefab, transform);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                FloatingDamageTextView view = active[i];

                if (view == null)
                {
                    active.RemoveAt(i);
                    continue;
                }

                if (!view.Tick(Time.deltaTime))
                {
                    active.RemoveAt(i);
                    pool.Add(view);
                }
            }
        }
    }
}