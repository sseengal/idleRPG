using UnityEngine;
using IdleRPG.Combat;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Sim;

namespace IdleRPG.UI
{
    /// <summary>
    /// The enemy side of the battle page: up to <see cref="BalanceConfig.HardEnemyCap"/> slots stacked
    /// vertically, each with its own sprite, name and health bar.
    ///
    /// ELI5: one tall column on the right. When the wave has 1 enemy it fills the column; 2 enemies means two
    /// half-height boxes; 3 means three thirds. Slots are made once and reused - nothing is created or destroyed
    /// during a fight - and the unused ones are simply switched off.
    ///
    /// Enemies have **no ranks**: the stack is presentation only (who gets hit is decided by the sim).
    /// </summary>
    public sealed class EnemyStackView : MonoBehaviour
    {
        /// <summary>Hard ceiling on stacked slots (matches BalanceConfig.HardEnemyCap / SimCaps).</summary>
        public const int MaxSlots = 3;

        /// <summary>Stagger pass: each lower slot drifts right, so the stack leans like a backslash.</summary>
        private const float ColumnStagger = 24f;

        /// <summary>Slot geometry mirrors the hero board (FormationBoardView): 160px wide, 176 tall with a
        /// 12px row gap, so enemy slots are grouped exactly like the heroes' instead of spread over the column.</summary>
        private const float SlotWidth = 160f;
        private const float SlotHeight = 176f;
        private const float SlotGap = 12f;

        [SerializeField] private RectTransform container;
        [SerializeField] private EnemyUnitView[] slots;
        [SerializeField] private RectTransform[] anchors;

        [Header("Juice (subtle)")]
        [Tooltip("Peak horizontal/vertical jitter of the whole column on a crit, in px.")]
        [SerializeField] private float critShakePixels = 1.25f;
        [SerializeField] private float critShakeDurationSec = 0.18f;
        [Tooltip("Squash-and-recover amount on wave clear (1.0 = no pop).")]
        [SerializeField] private float clearPopScale = 1.04f;
        [SerializeField] private float clearPopDurationSec = 0.2f;

        private GameManager manager;

        private Vector2 restPosition;
        private bool restPositionCaptured;
        private float shakeTimer;
        private float shakePixels;
        private float shakeDuration;
        private float clearPopTimer;

        /// <summary>Wires the manager and pushes fresh damage anchors into the floating-text pool.</summary>
        public void Build(GameManager gameManager, FloatingDamageTextPool damagePool)
        {
            manager = gameManager;
            damagePool?.SetEnemyAnchors(anchors);
        }

        private void OnEnable()
        {
            GameEvents.EnemySpawned += OnEnemySpawned;
            GameEvents.EnemyDamaged += OnEnemyDamaged;
            GameEvents.WaveCompleted += OnWaveCompleted;
        }

        private void OnDisable()
        {
            GameEvents.EnemySpawned -= OnEnemySpawned;
            GameEvents.EnemyDamaged -= OnEnemyDamaged;
            GameEvents.WaveCompleted -= OnWaveCompleted;
        }

        /// <summary>Any spawn event means "a new wave started" - the whole stack is re-laid out from the sim.</summary>
        private void OnEnemySpawned(string enemyName, double maxHealth, bool isBoss, int enemyIndex)
        {
            Refresh();
        }

        /// <summary>Only crits shake the column - a shake per hit would be a jitter blur.</summary>
        private void OnEnemyDamaged(EnemyDamagedInfo info)
        {
            if (!info.IsCritical || container == null)
            {
                return;
            }

            CaptureRestPosition();
            shakePixels = critShakePixels;
            shakeDuration = critShakeDurationSec;
            shakeTimer = critShakeDurationSec;
        }

        private void OnWaveCompleted(int stage, int wave)
        {
            if (container == null)
            {
                return;
            }

            clearPopTimer = clearPopDurationSec;
        }

        private void CaptureRestPosition()
        {
            if (!restPositionCaptured && container != null)
            {
                restPosition = container.anchoredPosition;
                restPositionCaptured = true;
            }
        }

        private void Update()
        {
            if (container == null)
            {
                return;
            }

            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(shakeTimer / Mathf.Max(0.0001f, shakeDuration));
                // Faster than linear falloff: sharp attack, quick settle.
                float dampen = 1f - t;
                Vector2 offset = new Vector2(
                    Mathf.Sin(t * Mathf.PI * 16f) * shakePixels * dampen,
                    Mathf.Cos(t * Mathf.PI * 10f) * shakePixels * dampen);
                container.anchoredPosition = restPosition + offset;

                if (shakeTimer <= 0f)
                {
                    container.anchoredPosition = restPosition;
                }
            }

            if (clearPopTimer > 0f)
            {
                clearPopTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(clearPopTimer / Mathf.Max(0.0001f, clearPopDurationSec));
                // 1 -> peak -> 1: a squash that springs back, then the stack is immediately re-laid out anyway.
                float scale = 1f + (clearPopScale - 1f) * Mathf.Sin(t * Mathf.PI * 0.5f);
                container.localScale = new Vector3(scale, scale, 1f);

                if (clearPopTimer <= 0f)
                {
                    container.localScale = Vector3.one;
                }
            }
        }

        /// <summary>
        /// Reads the wave from the simulator, arranges the used slots and binds each one. Reading the sim (instead
        /// of trusting the event) is what makes a slot that was switched off last wave come back correctly.
        /// </summary>
        public void Refresh()
        {
            if (slots == null || container == null)
            {
                return;
            }

            EnemyCombatant[] enemies = ResolveEnemies();
            int count = enemies == null ? 0 : Mathf.Min(enemies.Length, slots.Length);
            float height = container.rect.height > 1f ? container.rect.height : 400f;

            // Fixed hero-like box per enemy; shrink only enough to fit the wave in the column.
            float slotHeight = count > 0 ? Mathf.Min(SlotHeight, (height - (count - 1) * SlotGap) / count) : SlotHeight;

            // Vertically centre the packed group in the column so it lines up with the hero block instead
            // of hugging the column's top edge.
            float usedHeight = count * slotHeight + Mathf.Max(0, count - 1) * SlotGap;
            float startY = Mathf.Max(0f, (height - usedHeight) * 0.5f);

            for (int i = 0; i < slots.Length; i++)
            {
                EnemyUnitView slot = slots[i];

                if (slot == null)
                {
                    continue;
                }

                bool used = i < count && enemies[i] != null;
                slot.gameObject.SetActive(used);

                if (!used)
                {
                    continue;
                }

                if (slot.transform is RectTransform rect)
                {
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.sizeDelta = new Vector2(SlotWidth, slotHeight);
                    // Top-anchored rects: local y is negative downward, so the centered start offset is
                    // also negative (this keeps the whole group inside the column, centred on its middle).
                    float y = -(startY + i * (slotHeight + SlotGap));
                    rect.anchoredPosition = new Vector2(ColumnStagger * i, y);
                }

                EnemyCombatant enemy = enemies[i];
                slot.Configure(i);
                slot.SetIconSize(enemy.IsBoss ? Mathf.Min(200f, slotHeight * 0.9f) : SlotWidth * 0.8f);
                slot.Show(enemy.DisplayName, enemy.MaxHealth, enemy.IsBoss);

                if (anchors != null && i < anchors.Length && anchors[i] != null)
                {
                    anchors[i].anchoredPosition = new Vector2(ColumnStagger * i, -(startY + i * (slotHeight + SlotGap) + slotHeight * 0.5f));
                }
            }
        }

        private EnemyCombatant[] ResolveEnemies()
        {
            if (manager == null || manager.Combat == null || manager.Combat.Simulator == null)
            {
                return null;
            }

            return manager.Combat.Simulator.Enemies;
        }
    }
}
