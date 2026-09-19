using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;

namespace IdleRPG.UI
{
    /// <summary>
    /// View for the single enemy slot. Reads the live enemy from the simulator so the sprite,
    /// boss styling and HP all come from the same source of truth as combat.
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

        private float popTimer;
        private float deathTimer = -1f;

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

        private void OnEnemySpawned(string enemyName, double maxHealth, bool isBoss)
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
            if (hpBar != null)
            {
                hpBar.SetFill(info.NormalizedHealth);
                hpBar.SetValueLabel(info.CurrentHealth, info.MaxHealth);
            }
        }

        private void OnEnemyKilled(string enemyName, double goldReward)
        {
            deathTimer = deathFadeDurationSec;
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

            var enemy = manager.Combat.Simulator.Enemy;
            return enemy == null ? null : enemy.Data;
        }
    }
}