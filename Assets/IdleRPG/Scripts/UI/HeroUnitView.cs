using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;

namespace IdleRPG.UI
{
    /// <summary>
    /// View for one party lane: icon, name label, HP bar, hit flash and death dim.
    /// Subscribes to <see cref="GameEvents"/>; needs no Update() polling.
    /// </summary>
    public sealed class HeroUnitView : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private int heroIndex;
        [SerializeField] private Image iconImage;
        [SerializeField] private HpBarView hpBar;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Feedback")]
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.45f, 0.45f, 1f);
        [SerializeField] private float hitFlashDurationSec = 0.12f;
        [SerializeField] private float deadAlpha = 0.35f;

        private Color baseColor = Color.white;
        private float flashTimer;

        public int HeroIndex => heroIndex;

        public void Configure(int index)
        {
            heroIndex = index;
        }

        /// <summary>Applies the static data (icon, tint, name) once at setup.</summary>
        public void Apply(HeroData data)
        {
            if (data == null)
            {
                return;
            }

            if (iconImage != null)
            {
                if (data.HeroIcon != null)
                {
                    iconImage.sprite = data.HeroIcon;
                }

                baseColor = data.PlaceholderTint;
                iconImage.color = baseColor;
            }

            if (nameLabel != null)
            {
                nameLabel.SetText(data.HeroName);
            }
        }

        private void OnEnable()
        {
            GameEvents.HeroDamaged += OnHeroDamaged;
            GameEvents.HeroDied += OnHeroDied;
            GameEvents.HeroStatsChanged += OnHeroStatsChanged;
            GameEvents.WaveCompleted += OnWaveChanged;
            GameEvents.StageChanged += OnStageChangedHandler;
        }

        private void OnDisable()
        {
            GameEvents.HeroDamaged -= OnHeroDamaged;
            GameEvents.HeroDied -= OnHeroDied;
            GameEvents.HeroStatsChanged -= OnHeroStatsChanged;
            GameEvents.WaveCompleted -= OnWaveChanged;
            GameEvents.StageChanged -= OnStageChangedHandler;
        }

        private void OnHeroDamaged(int index, double damage, double currentHealth, double maxHealth)
        {
            if (index != heroIndex)
            {
                return;
            }

            if (hpBar != null)
            {
                hpBar.SetVisible(true);
                hpBar.SetFill(maxHealth <= 0d ? 0f : (float)(currentHealth / maxHealth));
                hpBar.SetValueLabel(currentHealth, maxHealth);
            }

            flashTimer = hitFlashDurationSec;
        }

        private void OnHeroDied(int index)
        {
            if (index != heroIndex)
            {
                return;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = deadAlpha;
            }
        }

        private void OnHeroStatsChanged(int index)
        {
            if (index != heroIndex)
            {
                return;
            }

            RefreshFromSimulator();
        }

        private void OnWaveChanged(int stage, int wave)
        {
            RefreshFromSimulator();
        }

        private void OnStageChangedHandler(int stage, int wave, bool isBoss)
        {
            RefreshFromSimulator();
        }

        /// <summary>Pulls current HP from the simulation (after healing or an upgrade).</summary>
        private void RefreshFromSimulator()
        {
            GameManager manager = HudController.Instance != null ? HudController.Instance.GameManager : null;
            if (manager == null || manager.Combat == null || manager.Combat.Simulator == null)
            {
                return;
            }

            var hero = manager.Combat.Simulator.Heroes != null && manager.Combat.Simulator.Heroes.Length > heroIndex
                ? manager.Combat.Simulator.Heroes[heroIndex]
                : null;

            if (hero == null)
            {
                SetDeadVisual();
                return;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = hero.IsAlive ? 1f : deadAlpha;
            }

            if (hpBar != null)
            {
                hpBar.SetFill((float)hero.HealthPercent);
                hpBar.SetValueLabel(hero.CurrentHealth, hero.MaxHealth);
            }
        }

        private void SetDeadVisual()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = deadAlpha;
            }

            if (hpBar != null)
            {
                hpBar.SetFill(0f, instant: true);
                hpBar.SetValueLabel(0d, 1d);
            }
        }

        private void Update()
        {
            if (iconImage == null)
            {
                return;
            }

            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                iconImage.color = Color.Lerp(baseColor, hitFlashColor, Mathf.Clamp01(flashTimer / hitFlashDurationSec));
                return;
            }

            if (iconImage.color != baseColor)
            {
                iconImage.color = baseColor;
            }
        }
    }
}