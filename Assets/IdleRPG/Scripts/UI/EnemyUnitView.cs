using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;

namespace IdleRPG.UI
{
    /// <summary>
    /// View for **one** enemy of a 1-3 enemy wave. Reads that enemy from the simulator by index, so sprite,
    /// name and HP all come from the same source of truth as combat.
    ///
    /// ELI5: this used to be "the enemy" - a single slot that showed whatever was being fought. Now each slot
    /// knows its own number (0, 1, 2) and ignores events that are not about it, so three stacked slots each
    /// update their own sprite and HP bar.
    /// </summary>
    public sealed class EnemyUnitView : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Image spriteImage;
        [SerializeField] private HpBarView hpBar;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private RectTransform root;
        [SerializeField] private Image bossFrame;

        [Header("Feedback")]
        [SerializeField] private float spawnPopScale = 0.65f;
        [SerializeField] private float spawnPopDurationSec = 0.18f;
        [SerializeField] private float deathFadeDurationSec = 0.20f;

        private int enemyIndex;
        private float popTimer;
        private float deathTimer = -1f;

        /// <summary>Which enemy of the wave this view shows (0-based).</summary>
        public int EnemyIndex => enemyIndex;

        /// <summary>Points this view at slot <paramref name="index"/> of the wave.</summary>
        public void Configure(int index)
        {
            enemyIndex = index;
        }

        /// <summary>Runtime wiring (used by the code-built battle page).</summary>
        public void ConfigureRuntime(int index, Image icon, HpBarView bar, TextMeshProUGUI label, RectTransform rect, Image frame)
        {
            enemyIndex = index;
            spriteImage = icon;
            hpBar = bar;
            nameLabel = label;
            root = rect;
            bossFrame = frame;
        }

        private void OnEnable()
        {
            GameEvents.EnemySpawned += OnEnemySpawned;
            GameEvents.EnemyDamaged += OnEnemyDamaged;
            GameEvents.EnemyKilled += OnEnemyKilled;
        }

        private void OnDisable()
        {
            GameEvents.EnemySpawned -= OnEnemySpawned;
            GameEvents.EnemyDamaged -= OnEnemyDamaged;
            GameEvents.EnemyKilled -= OnEnemyKilled;
        }

        private void OnEnemySpawned(string enemyName, double maxHealth, bool isBoss, int index)
        {
            if (index != enemyIndex)
            {
                return;
            }

            Show(enemyName, maxHealth, isBoss);
        }

        /// <summary>
        /// Resizes the sprite. A 1-enemy wave fills the space; a 3-enemy wave shrinks so the three sprites do not
        /// overlap their neighbours' bars.
        /// </summary>
        public void SetIconSize(float pixels)
        {
            if (spriteImage != null)
            {
                spriteImage.rectTransform.sizeDelta = new Vector2(pixels, pixels);
            }
        }

        /// <summary>
        /// Shows this enemy (sprite, name, full health bar). Called by the spawn event and by the stack when it
        /// re-lays out a wave, so a slot that was switched off last wave comes back correctly.
        /// </summary>
        public void Show(string enemyName, double maxHealth, bool isBoss)
        {
            EnemyData data = ResolveEnemyData();

            if (spriteImage != null)
            {
                if (data != null && data.EnemySprite != null)
                {
                    spriteImage.sprite = data.EnemySprite;
                }

                spriteImage.color = data != null ? data.PlaceholderTint : Color.white;
                spriteImage.enabled = true;
            }

            if (nameLabel != null)
            {
                nameLabel.SetText(isBoss ? $"{enemyName} <size=70%>BOSS</size>" : enemyName);
            }

            if (bossFrame != null)
            {
                bossFrame.enabled = isBoss;
            }

            if (hpBar != null)
            {
                hpBar.SetVisible(true);
                hpBar.SetFill(1f, instant: true);
                hpBar.SetValueLabel(maxHealth, maxHealth);
            }

            popTimer = spawnPopDurationSec;
            deathTimer = -1f;
        }

        private void OnEnemyDamaged(EnemyDamagedInfo info)
        {
            if (info.EnemyIndex != enemyIndex || hpBar == null)
            {
                return;
            }

            hpBar.SetFill(info.NormalizedHealth);
            hpBar.SetValueLabel(info.CurrentHealth, info.MaxHealth);
        }

        private void OnEnemyKilled(string enemyName, double goldReward, int index)
        {
            if (index == enemyIndex)
            {
                deathTimer = deathFadeDurationSec;
            }
        }

        private void Update()
        {
            if (root == null)
            {
                return;
            }

            if (popTimer > 0f)
            {
                popTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(popTimer / Mathf.Max(0.0001f, spawnPopDurationSec));
                float scale = Mathf.Lerp(spawnPopScale, 1f, t);
                root.localScale = new Vector3(scale, scale, 1f);
                return;
            }

            if (deathTimer > 0f)
            {
                deathTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(deathTimer / Mathf.Max(0.0001f, deathFadeDurationSec));

                if (spriteImage != null)
                {
                    Color color = spriteImage.color;
                    color.a = t;
                    spriteImage.color = color;
                }

                if (deathTimer <= 0f && spriteImage != null)
                {
                    spriteImage.enabled = false;
                }

                return;
            }

            if (root.localScale != Vector3.one)
            {
                root.localScale = Vector3.one;
            }
        }

        private EnemyData ResolveEnemyData()
        {
            GameManager manager = HudController.Instance != null ? HudController.Instance.GameManager : null;
            if (manager == null || manager.Combat == null || manager.Combat.Simulator == null)
            {
                return null;
            }

            var enemies = manager.Combat.Simulator.Enemies;

            if (enemies == null || enemyIndex < 0 || enemyIndex >= enemies.Length || enemies[enemyIndex] == null)
            {
                return null;
            }

            return enemies[enemyIndex].Data;
        }
    }
}