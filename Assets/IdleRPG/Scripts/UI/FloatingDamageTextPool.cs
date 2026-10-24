using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

        [Header("Impact effect")]
        [Tooltip("Frames per second for an on-target impact sheet (Resources/Vfx/<id>).")]
        [SerializeField] private float impactVfxFps = 12f;

        /// <summary>
        /// Effect size as a multiple of one enemy slot. 2.5 was picked in play: at 1.4 the burst read as a spark,
        /// at 2.0 it still sat inside the sprite's transparent margin. The sheets have padding, so the number is
        /// deliberately larger than the unit it covers.
        /// </summary>
        private const float ImpactVfxSlotScale = 2.5f;

        /// <summary>Height of one enemy slot, pushed by the stack on every layout. 176 is the shipped slot box.</summary>
        private float enemySlotHeight = 176f;

        private readonly List<ImpactPlay> impacts = new List<ImpactPlay>();
        private readonly Dictionary<string, Sprite[]> impactCache = new Dictionary<string, Sprite[]>();

        /// <summary>Effects waiting for their lead time (an ability's burst starts before its damage).</summary>
        private readonly List<PendingImpact> pendingImpacts = new List<PendingImpact>();

        /// <summary>Last time an effect started for each enemy slot, so the damage that follows does not spawn a second one.</summary>
        private readonly Dictionary<int, float> lastImpactAt = new Dictionary<int, float>();

        private sealed class PendingImpact
        {
            public int index;
            public string vfxId;
            public float scale;
            public float atTime;
        }

        private sealed class ImpactPlay
        {
            public Image image;
            public RectTransform rect;
            public Sprite[] frames;
            public float clock;
            public int frame;
            public bool active;
        }

        /// <summary>The live pool, so a unit view can post a word at its own position.</summary>
        public static FloatingDamageTextPool Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
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
            GameEvents.Healed += OnHealed;
            GameEvents.ShieldApplied += OnShieldApplied;
            GameEvents.AbilityCast += OnAbilityCast;
            GameEvents.ImpactVfx += OnImpactVfx;
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            GameEvents.EnemyDamaged -= OnEnemyDamaged;
            GameEvents.Healed -= OnHealed;
            GameEvents.ShieldApplied -= OnShieldApplied;
            GameEvents.AbilityCast -= OnAbilityCast;
            GameEvents.ImpactVfx -= OnImpactVfx;
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

            // Call-out over the caster, centred above the sprite.
            if (!string.IsNullOrEmpty(abilityName))
            {
                Spawn(0d, AnchorFor(CombatantSide.Enemy, index), FloatingTextStyle.AbilityHit, abilityName, new Vector2(0f, 96f));
            }
        }

        /// <summary>True while the damage from that enemy slot still belongs to an ability it just called out.</summary>
        public bool IsAbilityHit(int enemyIndex)
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
        /// Height of one enemy slot, pushed by <see cref="EnemyStackView"/> whenever it lays the wave out.
        /// Impact effects size themselves from this, because the damage anchors are 1x1 markers.
        /// </summary>
        public void SetEnemySlotHeight(float height)
        {
            if (height > 1f)
            {
                enemySlotHeight = height;
            }
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

            // Name the move at the beat it lands, centred above the enemy: read as a title, not a stray number.
            if (!string.IsNullOrEmpty(info.AbilityName))
            {
                Spawn(0d, EnemyAnchor(info.EnemyIndex), FloatingTextStyle.AbilityHit, info.AbilityName, new Vector2(0f, 96f));
            }

            // A scheduled burst (ability) already played a moment ago; do not start a second one on its damage.
            bool alreadyBurst = !string.IsNullOrEmpty(info.VfxId)
                && lastImpactAt.TryGetValue(info.EnemyIndex, out float lastAt)
                && Time.time - lastAt < 0.75f;

            if (!alreadyBurst)
            {
                PlayImpact(info.EnemyIndex, info.VfxId);
            }
        }

        private RectTransform EnemyAnchor(int enemyIndex)
        {
            if (enemyAnchors == null || enemyAnchors.Length == 0)
            {
                return enemyAnchor;
            }

            return enemyAnchors[Mathf.Clamp(enemyIndex, 0, enemyAnchors.Length - 1)];
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
            Spawn(0d, AnchorFor(shield.Side, shield.Index), FloatingTextStyle.Guard, "GUARD UP", new Vector2(0f, 96f));
        }

        /// <summary>
        /// The unit's live body rect (registered by its view), falling back to the enemy slot anchors for a spawn that
        /// has no view yet. Heroes own their numbers and words, so there is no party fallback to go stale.
        /// </summary>
        private RectTransform AnchorFor(CombatantSide side, int index)
        {
            // Live position first: a unit that dashes (the Knight guarding an ally) must take its numbers with it.
            RectTransform live = UnitAnchors.Get(side, index);

            if (live != null)
            {
                return live;
            }

            return side == CombatantSide.Enemy ? EnemyAnchor(index) : null;
        }

        /// <summary>
        /// A cast announced its on-target effect. The burst starts after the lead time, which is deliberately BEFORE
        /// the damage lands: the player should see the shatter, then the number.
        /// </summary>
        private void OnImpactVfx(CombatantSide side, int index, string vfxId, float leadSeconds, float scale)
        {
            if (side != CombatantSide.Enemy || string.IsNullOrEmpty(vfxId))
            {
                return;
            }

            pendingImpacts.Add(new PendingImpact
            {
                index = index,
                vfxId = vfxId,
                scale = scale <= 0f ? 1f : scale,
                atTime = Time.time + Mathf.Max(0f, leadSeconds)
            });
        }

        private void TickPendingImpacts()
        {
            for (int i = pendingImpacts.Count - 1; i >= 0; i--)
            {
                PendingImpact pending = pendingImpacts[i];
                if (Time.time < pending.atTime)
                {
                    continue;
                }

                pendingImpacts.RemoveAt(i);
                PlayImpact(pending.index, pending.vfxId, pending.scale);
            }
        }

        /// <summary>
        /// Shows a number over one unit, anchored to that unit's own transform. Hero views call this so a slot at the
        /// edge of the board (the bottom hero) gets its number where the hero actually stands.
        /// </summary>
        public void ShowNumber(RectTransform anchor, double value, FloatingTextStyle style)
        {
            if (value <= 0d)
            {
                return;
            }

            Spawn(value, anchor, style);
        }

        /// <summary>
        /// Shows a short word over one unit. Used by unit views that know exactly where they are right now (a guard
        /// that stepped in front of an ally), so the word cannot lag behind a move.
        /// </summary>
        public void ShowWord(RectTransform anchor, string text, FloatingTextStyle style, float height = 78f)
        {
            Spawn(0d, anchor, style, text, new Vector2(0f, height));
        }

        /// <summary>Plays an on-target impact sheet at one enemy slot. Missing art is a quiet no-op.</summary>
        private void PlayImpact(int enemyIndex, string vfxId, float scale = 1f)
        {
            if (string.IsNullOrEmpty(vfxId))
            {
                return;
            }

            RectTransform anchor = EnemyAnchor(enemyIndex);
            if (anchor == null)
            {
                return;
            }

            Sprite[] frames = ImpactFrames(vfxId);
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            ImpactPlay play = RentImpact();
            play.frames = frames;
            play.frame = 0;
            play.clock = 0f;
            play.active = true;

            RectTransform rt = play.rect;
            rt.SetParent(anchor.parent, false);
            rt.anchorMin = anchor.anchorMin;
            rt.anchorMax = anchor.anchorMax;
            rt.pivot = anchor.pivot;
            rt.anchoredPosition = anchor.anchoredPosition;

            float baseSize = enemySlotHeight > 1f ? enemySlotHeight : 176f;
            float size = Mathf.Max(96f, baseSize * ImpactVfxSlotScale * Mathf.Max(0.1f, scale));
            rt.sizeDelta = new Vector2(size, size);
            lastImpactAt[enemyIndex] = Time.time;

            // Just above its own unit, so the burst covers the enemy but stays under the numbers.
            int sibling = Mathf.Clamp(anchor.GetSiblingIndex() + 1, 0, anchor.parent.childCount - 1);
            rt.SetSiblingIndex(sibling);

            play.image.sprite = frames[0];
            play.image.enabled = true;
            play.image.raycastTarget = false;
        }

        private void TickImpacts(float deltaTime)
        {
            for (int i = 0; i < impacts.Count; i++)
            {
                ImpactPlay play = impacts[i];
                if (!play.active)
                {
                    continue;
                }

                play.clock += deltaTime;
                int frame = Mathf.FloorToInt(play.clock * Mathf.Max(1f, impactVfxFps));

                if (frame >= play.frames.Length)
                {
                    play.active = false;
                    play.image.enabled = false;
                    continue;
                }

                play.image.sprite = play.frames[frame];
            }
        }

        private ImpactPlay RentImpact()
        {
            for (int i = 0; i < impacts.Count; i++)
            {
                if (!impacts[i].active)
                {
                    return impacts[i];
                }
            }

            var host = new GameObject("ImpactVfx", typeof(RectTransform), typeof(Image));
            var rect = host.GetComponent<RectTransform>();
            var image = host.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;

            var play = new ImpactPlay { image = image, rect = rect };
            impacts.Add(play);
            return play;
        }

        /// <summary>Frames for one effect id, loaded once from Resources/Vfx/&lt;id&gt; and sorted 0,1,2,... .</summary>
        private Sprite[] ImpactFrames(string vfxId)
        {
            if (impactCache.TryGetValue(vfxId, out Sprite[] cached))
            {
                return cached;
            }

            Sprite[] frames = Resources.LoadAll<Sprite>("Vfx/" + vfxId);
            if (frames != null && frames.Length > 1)
            {
                System.Array.Sort(frames, (a, b) =>
                {
                    if (a.name.Length != b.name.Length)
                    {
                        return a.name.Length - b.name.Length;
                    }

                    return string.CompareOrdinal(a.name, b.name);
                });
            }

            impactCache[vfxId] = frames;
            return frames;
        }

        private void Spawn(double value, RectTransform anchor, FloatingTextStyle style, string text = null, Vector2? offsetOverride = null)
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

            // Position from the anchor's WORLD position, converted into the parent's local space, then into the
            // anchoredPosition the text rect needs. Reading anchor.anchoredPosition ignored a unit moved by a dash
            // (the lunge changes position, not the stored anchoredPosition); converting world -> local alone was not
            // enough either, because the text inherits the anchor's own anchors and pivot.
            Rect spaceRect = anchor.parent is RectTransform space ? space.rect : new Rect(0f, 0f, 0f, 0f);
            Vector2 local = anchor.parent is RectTransform localSpace
                ? (Vector2)localSpace.InverseTransformPoint(anchor.position)
                : anchor.anchoredPosition;

            Vector2 offset = offsetOverride ?? new Vector2(
                side * (40f + Random.Range(0f, Mathf.Abs(anchorJitter) * 0.5f)),
                Random.Range(18f, 42f));

            local += offset;

            if (spaceRect.width > 0f && spaceRect.height > 0f)
            {
                local.x = Mathf.Clamp(local.x, spaceRect.xMin + 18f, spaceRect.xMax - 18f);
                local.y = Mathf.Clamp(local.y, spaceRect.yMin + 14f, spaceRect.yMax - 10f);
            }

            // uGUI: localPosition = anchorReference + anchoredPosition + (pivot - 0.5) * size.
            Vector2 anchorReference = new Vector2(
                spaceRect.xMin + spaceRect.width * rt.anchorMin.x,
                spaceRect.yMin + spaceRect.height * rt.anchorMin.y);
            Vector2 pivotOffset = Vector2.Scale(rt.sizeDelta, rt.pivot - new Vector2(0.5f, 0.5f));
            Vector2 position = local - anchorReference - pivotOffset;

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
            TickPendingImpacts();
            TickImpacts(Time.deltaTime);

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