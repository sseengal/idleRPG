using System.Collections.Generic;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Sim;

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

        [Header("Enemy ability")]
        [Tooltip("How long after an enemy announces its ability its damage still counts as ability damage.")]
        [SerializeField] private float abilityHitWindowSec = 2.5f;

        private readonly List<float> enemyCastUntil = new List<float>();
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
            GameEvents.Healed += OnHealed;
            GameEvents.ShieldApplied += OnShieldApplied;
            GameEvents.AbilityCast += OnAbilityCast;
        }

        private void OnDisable()
        {
            GameEvents.EnemyDamaged -= OnEnemyDamaged;
            GameEvents.HeroDamaged -= OnHeroDamaged;
            GameEvents.Healed -= OnHealed;
            GameEvents.ShieldApplied -= OnShieldApplied;
            GameEvents.AbilityCast -= OnAbilityCast;
        }

        /// <summary>
        /// Marks the enemy that announced an ability. Its hit lands a moment later, so the damage event that
        /// follows carries no "ability" flag; this window is what turns that plain hit into a heavy one.
        /// </summary>
        private void OnAbilityCast(CombatantSide side, int index, string abilityId, string abilityName, string clipSlot, double cooldownSeconds)
        {
            if (side != CombatantSide.Enemy || index < 0)
            {
                return;
            }

            while (enemyCastUntil.Count <= index)
            {
                enemyCastUntil.Add(0f);
            }

            enemyCastUntil[index] = Time.time + Mathf.Max(0.1f, abilityHitWindowSec);

            // Call-out over the caster: the player sees where the move came from, not just that one happened.
            if (!string.IsNullOrEmpty(abilityName))
            {
                Spawn(0d, AnchorFor(CombatantSide.Enemy, index), FloatingTextStyle.AbilityHit, abilityName);
            }
        }

        /// <summary>True while the damage from that enemy slot still belongs to an ability it just called out.</summary>
        private bool IsAbilityHit(int enemyIndex)
        {
            return enemyIndex >= 0
                && enemyIndex < enemyCastUntil.Count
                && Time.time <= enemyCastUntil[enemyIndex];
        }

        /// <summary>Replaces the per-enemy damage anchors so numbers come out of the enemy that was hit.</summary>
        public void SetEnemyAnchors(RectTransform[] anchors)
        {
            enemyAnchors = anchors;
        }

        /// <summary>
        /// Anchor rect for one enemy slot, for effects that need to fly out of the right enemy
        /// (the gold-fly effect). Returns null when no per-slot anchors are wired.
        /// </summary>
        public RectTransform GetEnemyAnchor(int enemyIndex)
        {
            if (enemyAnchors == null || enemyAnchors.Length == 0)
            {
                return null;
            }

            return enemyAnchors[Mathf.Clamp(enemyIndex, 0, enemyAnchors.Length - 1)];
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

        private void OnHeroDamaged(int heroIndex, double damage, double currentHealth, double maxHealth, int attackerEnemyIndex, bool isCritical)
        {
            if (damage <= 0d || heroAnchors == null || heroIndex < 0 || heroIndex >= heroAnchors.Length)
            {
                return;
            }

            FloatingTextStyle style = IsAbilityHit(attackerEnemyIndex)
                ? FloatingTextStyle.AbilityHit
                : FloatingTextStyle.Incoming;

            Spawn(damage, heroAnchors[heroIndex], style);
        }

        /// <summary>Health restored by an ability: a green number over the unit that was healed.</summary>
        private void OnHealed(HealEvent heal)
        {
            if (heal.Amount <= 0d)
            {
                return;
            }

            Spawn(heal.Amount, AnchorFor(heal.Side, heal.Index), FloatingTextStyle.Heal);
        }

        /// <summary>A guard raised by an ability: a blue "GUARD" over the unit (no number, because no health moved).</summary>
        private void OnShieldApplied(ShieldEvent shield)
        {
            Spawn(0d, AnchorFor(shield.Side, shield.Index), FloatingTextStyle.Guard, "GUARD");
        }

        /// <summary>The anchor that belongs to one unit on either side.</summary>
        private RectTransform AnchorFor(CombatantSide side, int index)
        {
            if (side == CombatantSide.Enemy)
            {
                return EnemyAnchor(index);
            }

            return heroAnchors != null && index >= 0 && index < heroAnchors.Length ? heroAnchors[index] : null;
        }

        private void Spawn(double value, RectTransform anchor, FloatingTextStyle style, string text = null)
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

            // Directional: damage dealt to enemies pops from the enemy's top-LEFT; damage taken by
            // heroes pops from the hero's top-RIGHT. The rect adopts the anchor's anchor/pivot so the
            // offset lands in the SAME coordinate space (otherwise numbers drifted below the sim), then
            // it is clamped to stay inside the battle view.
            float side = style == FloatingTextStyle.Incoming || style == FloatingTextStyle.AbilityHit || style == FloatingTextStyle.Heal ? 1f : -1f;

            RectTransform rt = view.transform as RectTransform;
            rt.SetParent(anchor.parent, false);
            rt.anchorMin = anchor.anchorMin;
            rt.anchorMax = anchor.anchorMax;
            rt.pivot = anchor.pivot;

            Vector2 position = anchor.anchoredPosition + new Vector2(
                side * (40f + Random.Range(0f, Mathf.Abs(anchorJitter) * 0.5f)),
                Random.Range(18f, 42f));

            if (anchor.parent is RectTransform parentRect)
            {
                Rect bounds = parentRect.rect;
                position.x = Mathf.Clamp(position.x, -(bounds.width * 0.5f) + 18f, (bounds.width * 0.5f) - 18f);
                position.y = Mathf.Clamp(position.y, -(bounds.height * 0.5f) + 14f, (bounds.height * 0.5f) - 10f);
            }

            driftSeed += 1.37f;

            if (string.IsNullOrEmpty(text))
            {
                view.Play(value, position, style, driftSeed);
            }
            else
            {
                view.Play(text, position, style, driftSeed);
            }

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