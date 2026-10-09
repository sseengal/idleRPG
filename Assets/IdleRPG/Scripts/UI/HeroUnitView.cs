using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Sim;

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
        [Tooltip("Squash-to value on a hit (1.0 = no punch). Subtle: whole seat dips, icon and bar together.")]
        [SerializeField] private float hitPunchScale = 0.94f;
        [SerializeField] private float hitPunchDurationSec = 0.12f;

        private Color baseColor = Color.white;
        private float flashTimer;
        private float punchTimer;

        private CharacterAnimator animator;
        private CharacterLunge lunge;
        private HeroData appliedData;

        public int HeroIndex => heroIndex;

        public void Configure(int index)
        {
            heroIndex = index;
        }

        /// <summary>
        /// Wires a view that was created in code (the formation strip builds a view per slot at runtime, so it
        /// cannot use the inspector). Kept separate from <see cref="Configure(int)"/> so the scene path is unchanged.
        /// </summary>
        public void ConfigureRuntime(int index, Image icon, HpBarView hp, TextMeshProUGUI label, CanvasGroup group)
        {
            heroIndex = index;
            iconImage = icon;
            hpBar = hp;
            nameLabel = label;
            canvasGroup = group;
        }

        /// <summary>Shows or hides the health bar (the Party board has no health to show).</summary>
        public void SetHealthVisible(bool visible)
        {
            if (hpBar != null)
            {
                hpBar.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Blank the slot. Clearing used to be just `enabled = false`, which left the previous hero's icon and
        /// name on screen - so a hero who moved appeared in two slots at once.
        /// </summary>
        public void ClearVisual(string label = "")
        {
            heroIndex = -1;
            appliedData = null;
            flashTimer = 0f;
            punchTimer = 0f;
            transform.localScale = Vector3.one;
            baseColor = new Color(1f, 1f, 1f, 0f);   // empty seats rest invisible (Update() honours this)

            if (lunge != null)
            {
                lunge.ResetToHome();
            }

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.color = baseColor;
            }

            if (nameLabel != null)
            {
                nameLabel.SetText(label);
            }

            if (animator != null)
            {
                animator.Clear();
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (hpBar != null)
            {
                hpBar.SetFill(0f, instant: true);
                hpBar.SetLabel(string.Empty);
            }

            // An empty seat is a clean tile, not a dead hero: hide the health bar entirely (B8').
            SetHealthVisible(false);
        }

        /// <summary>Applies the static data (icon, tint, name) once at setup.</summary>
        public void Apply(HeroData data)
        {
            if (data == null || data == appliedData)
            {
                // Idempotent: repeated repaints (selection taps, refreshes) must not re-bind or resize anything.
                return;
            }

            appliedData = data;

            if (iconImage != null)
            {
                if (data.ArtSet != null)
                {
                    // Real art: animated frames, no tint (coloured sprites go muddy when tinted).
                    CharacterAnimator art = EnsureAnimator();
                    art.FacingLeft = false;
                    art.SetArt(data.ArtSet);
                    baseColor = Color.white;
                    iconImage.color = baseColor;
                }
                else if (data.HeroIcon != null)
                {
                    iconImage.sprite = data.HeroIcon;
                    baseColor = data.PlaceholderTint;
                    iconImage.color = baseColor;
                }

                // A hero landing in this (possibly re-laid-out) seat re-captures its home anchor for the lunge.
                CharacterLunge lunge = EnsureLunge();
                if (lunge != null)
                {
                    lunge.RefreshHome();
                    lunge.ResetToHome();
                }
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
            GameEvents.EnemyDamaged += OnAllyAttackLanded;
            GameEvents.SwingStarted += OnSwingStarted;
            GameEvents.HeroStatsChanged += OnHeroStatsChanged;
            GameEvents.WaveCompleted += OnWaveChanged;
            GameEvents.StageChanged += OnStageChangedHandler;
        }

        private void OnDisable()
        {
            GameEvents.HeroDamaged -= OnHeroDamaged;
            GameEvents.HeroDied -= OnHeroDied;
            GameEvents.EnemyDamaged -= OnAllyAttackLanded;
            GameEvents.SwingStarted -= OnSwingStarted;
            GameEvents.HeroStatsChanged -= OnHeroStatsChanged;
            GameEvents.WaveCompleted -= OnWaveChanged;
            GameEvents.StageChanged -= OnStageChangedHandler;
        }

        /// <summary>This hero committed a swing: run to the target slot; the hit lands after the swing window.</summary>
        private void OnSwingStarted(int attackerIndex, CombatantSide attackerSide, int targetIndex, CombatantSide targetSide)
        {
            if (attackerSide != CombatantSide.Party || attackerIndex != heroIndex)
            {
                return;
            }

            HudController hud = HudController.Instance;
            EnemyStackView stack = hud != null ? hud.EnemyStack : null;
            RectTransform targetSlot = stack != null ? stack.GetEnemySlotRect(targetIndex) : null;

            if (targetSlot == null)
            {
                return;
            }

            EnsureAnimator();
            CharacterLunge lunge = EnsureLunge();

            if (lunge != null && !lunge.IsBusy)
            {
                lunge.BeginRun(targetSlot);
            }
        }

        private void OnHeroDamaged(int index, double damage, double currentHealth, double maxHealth, int attackerEnemyIndex)
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
            punchTimer = hitPunchDurationSec;

            if (animator != null && animator.HasArt)
            {
                animator.PlayHurt();
            }
        }

        private void OnAllyAttackLanded(EnemyDamagedInfo info)
        {
            if (info.AttackerIndex != heroIndex || animator == null || !animator.HasArt)
            {
                return;
            }

            animator.PlayAttack(() =>
            {
                if (lunge != null)
                {
                    lunge.CompleteSwing();
                }
            });

            // The damage just landed: if the hero was running in, plant them at the target for the swing pose.
            if (lunge != null)
            {
                lunge.Impact();
            }
        }

        private void OnHeroDied(int index)
        {
            if (index != heroIndex)
            {
                return;
            }

            // No fade after death: the death animation is the whole visual. The hero stays fully opaque
            // on the death pose until the wave revives them (RefreshFromSimulator flips back to Idle).
            if (lunge != null)
            {
                lunge.ResetToHome();   // die in your own slot, never mid-run
            }

            if (animator != null && animator.HasArt)
            {
                animator.PlayDeath();
            }
        }

        private void OnHeroStatsChanged(int index)
        {
            if (index != heroIndex)
            {
                return;
            }

            RefreshStats();
        }

        private void OnWaveChanged(int stage, int wave)
        {
            RefreshStats();
        }

        private void OnStageChangedHandler(int stage, int wave, bool isBoss)
        {
            RefreshStats();
        }

        /// <summary>Pulls current HP from the simulation (after healing or an upgrade).</summary>
        /// <summary>Pulls the live HP from the simulation into the bar. Public so a board can refresh a seat the
        /// moment it (re)binds it, instead of waiting for the next combat event.</summary>
        public void RefreshStats()
        {
            GameManager manager = HudController.Instance != null ? HudController.Instance.GameManager : null;
            if (manager == null || manager.Combat == null || manager.Combat.Simulator == null)
            {
                return;
            }

            // heroIndex is -1 while a view is unbound/pooled, and a negative index *passes* the length check.
            var hero = heroIndex >= 0 && manager.Combat.Simulator.Heroes != null
                       && manager.Combat.Simulator.Heroes.Length > heroIndex
                ? manager.Combat.Simulator.Heroes[heroIndex]
                : null;

            if (hero == null)
            {
                SetDeadVisual();
                return;
            }

            if (hero.IsAlive && animator != null && animator.HasArt)
            {
                animator.PlayIdle();
            }

            // Alive again (revived seat, fresh wave): park the hero at home in case a lunge left them mid-run.
            if (hero.IsAlive && lunge != null)
            {
                lunge.RefreshHome();
                lunge.ResetToHome();
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (hpBar != null)
            {
                hpBar.SetFill((float)hero.HealthPercent);
                hpBar.SetValueLabel(hero.CurrentHealth, hero.MaxHealth);
            }
        }

        private void SetDeadVisual()
        {
            // Fully opaque - the death pose (or an absent hero) is never faded out on purpose.
            if (hpBar != null)
            {
                hpBar.SetFill(0f, instant: true);
                hpBar.SetValueLabel(0d, 1d);
            }
        }

        private CharacterAnimator EnsureAnimator()
        {
            if (animator == null && iconImage != null)
            {
                animator = iconImage.GetComponent<CharacterAnimator>();
                if (animator == null)
                {
                    animator = iconImage.gameObject.AddComponent<CharacterAnimator>();
                }

                // Captured while the rect is still the built one; the animator owns it from here on.
                animator.SetSlotSize(iconImage.rectTransform.sizeDelta);
                iconImage.preserveAspect = true;
                animator.FacingLeft = false;
            }

            return animator;
        }

        /// <summary>The lunge (run-to-target + blink) rides on the same icon so it tracks the art's pivot.</summary>
        private CharacterLunge EnsureLunge()
        {
            if (lunge == null && iconImage != null)
            {
                lunge = iconImage.GetComponent<CharacterLunge>();
                if (lunge == null)
                {
                    lunge = iconImage.gameObject.AddComponent<CharacterLunge>();
                }

                lunge.RefreshHome();
            }

            return lunge;
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
            }
            else if (lunge == null || !lunge.IsBusy)
            {
                // Not mid-blink (the lunge owns alpha while it fades in/out); return to the resting tint.
                if (iconImage.color != baseColor)
                {
                    iconImage.color = baseColor;
                }
            }

            if (punchTimer > 0f)
            {
                punchTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(punchTimer / Mathf.Max(0.0001f, hitPunchDurationSec));
                float scale = Mathf.Lerp(hitPunchScale, 1f, t);
                transform.localScale = new Vector3(scale, scale, 1f);
            }
            else if (transform.localScale != Vector3.one)
            {
                transform.localScale = Vector3.one;
            }
        }
    }
}