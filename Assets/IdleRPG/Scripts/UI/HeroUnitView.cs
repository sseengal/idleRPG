using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Sim;

namespace IdleRPG.UI
{
    /// <summary>
    /// View for one HERO (not one seat): icon, name label, HP bar, hit flash, death dim, dash and board move.
    /// Subscribes to <see cref="GameEvents"/>; needs no Update() polling.
    ///
    /// ELI5: a hero is a thing that stands somewhere, not a hole in the ground. The board moves this whole view to a
    /// pad; the icon inside it dashes at enemies. Anything that needs "where is he" asks <see cref="BodyRect"/>.
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
        [Tooltip("Flash colour when an ability raises a guard on this hero (no health moved, so the bar is untouched).")]
        [SerializeField] private Color shieldFlashColor = new Color(0.55f, 0.8f, 1f, 1f);
        [SerializeField] private float shieldFlashDurationSec = 0.2f;

        [Header("Board move")]
        [Tooltip("Seconds a hero takes to slide to a new pad when the player moves him. 0 = snap.")]
        [SerializeField] private float slideSeconds = 0.22f;

        private Vector2 slideFrom;
        private Vector2 slideTo;
        private float slideT = -1f;

        private Color baseColor = Color.white;
        private readonly UnitHitJuice hitJuice = new UnitHitJuice();

        private CharacterAnimator animator;
        private CharacterLunge lunge;
        private HeroData appliedData;

        public int HeroIndex => heroIndex;

        /// <summary>
        /// Where this hero is DRAWN right now - the icon, which is the only thing that moves. Numbers, words and any
        /// future effect read this, and read it once: a number is born at the impact point and never follows.
        /// </summary>
        public RectTransform BodyRect => iconImage != null ? iconImage.rectTransform : transform as RectTransform;

        /// <summary>True while the view is sliding to a new pad (taps are ignored until it lands).</summary>
        public bool IsSliding => slideT >= 0f;

        /// <summary>
        /// Moves the whole view to a board pad. Instant on the first layout; a short ease when the player moves him,
        /// so the eye can follow which hero went where. The lunge re-captures its home when the slide lands.
        /// </summary>
        public void SlideTo(RectTransform pad, bool instant)
        {
            RectTransform self = transform as RectTransform;

            if (self == null || pad == null)
            {
                return;
            }

            // A seat move under a running dash would leave the dash aiming at the old layout: park him first.
            // (Home is derived from the seat, so nothing has to be re-captured afterwards.)
            bool movingToAnotherPad = (self.anchoredPosition - pad.anchoredPosition).sqrMagnitude > 0.01f;

            if (movingToAnotherPad && lunge != null)
            {
                lunge.ResetToHome();
            }

            if (instant || slideSeconds <= 0f)
            {
                self.anchoredPosition = pad.anchoredPosition;
                slideT = -1f;
                return;
            }

            slideFrom = self.anchoredPosition;
            slideTo = pad.anchoredPosition;
            slideT = 0f;
        }

        public void Configure(int index)
        {
            heroIndex = index;
            UnitAnchors.Register(CombatantSide.Party, index, BodyRect);
        }

        /// <summary>
        /// Wires a view that was created in code (the battle board builds one view per hero at runtime, so it
        /// cannot use the inspector). Kept separate from <see cref="Configure(int)"/> so the scene path is unchanged.
        /// </summary>
        public void ConfigureRuntime(int index, Image icon, HpBarView hp, TextMeshProUGUI label, CanvasGroup group)
        {
            heroIndex = index;
            iconImage = icon;
            hpBar = hp;
            nameLabel = label;
            canvasGroup = group;
            UnitAnchors.Register(CombatantSide.Party, index, BodyRect);
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
            hitJuice.Clear();
            transform.localScale = Vector3.one;
            slideT = -1f;
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

                // A hero landing in this (possibly re-laid-out) seat parks at the seat before any swing measures from it.
                CharacterLunge lunge = EnsureLunge();
                if (lunge != null)
                {
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
            // Only register once the icon is wired: a code-built view is enabled by AddComponent BEFORE
            // ConfigureRuntime hands it its icon, and registering then would point this hero's numbers at the
            // view's own box instead of the character.
            if (iconImage != null)
            {
                UnitAnchors.Register(CombatantSide.Party, heroIndex, BodyRect);
            }

            GameEvents.HeroDamaged += OnHeroDamaged;
            GameEvents.HeroDied += OnHeroDied;
            GameEvents.EnemyDamaged += OnAllyAttackLanded;
            GameEvents.Healed += OnHealed;
            GameEvents.ShieldApplied += OnShieldApplied;
            GameEvents.HeroStatsChanged += OnHeroStatsChanged;
            GameEvents.WaveCompleted += OnWaveChanged;
            GameEvents.StageChanged += OnStageChangedHandler;
        }

        private void OnDisable()
        {
            UnitAnchors.Unregister(CombatantSide.Party, heroIndex, BodyRect);
            GameEvents.HeroDamaged -= OnHeroDamaged;
            GameEvents.HeroDied -= OnHeroDied;
            GameEvents.EnemyDamaged -= OnAllyAttackLanded;
            GameEvents.Healed -= OnHealed;
            GameEvents.ShieldApplied -= OnShieldApplied;
            GameEvents.HeroStatsChanged -= OnHeroStatsChanged;
            GameEvents.WaveCompleted -= OnWaveChanged;
            GameEvents.StageChanged -= OnStageChangedHandler;
        }

        /// <summary>
        /// <summary>
        /// Plays this hero's action clip. Called by the <see cref="BeatDirector"/> on the announce, so the strike frame
        /// still lands on the hit.
        /// </summary>
        public void PlaySwing(string clipSlot)
        {
            EnsureLunge();
            StartSwing(clipSlot);
        }

        /// <summary>
        /// Sends this hero to a world spot decided by the <see cref="BeatDirector"/>. A cover lingers longer than an
        /// ordinary swing. Movement is refused while the hero is already out, exactly as before.
        /// </summary>
        public void RunTo(Vector3 worldSpot, bool cover)
        {
            CharacterLunge mover = EnsureLunge();

            if (mover == null || mover.IsBusy)
            {
                return;
            }

            if (cover)
            {
                mover.BeginCoverTo(worldSpot);
            }
            else
            {
                mover.BeginRunTo(worldSpot);
            }
        }

        /// <summary>
        /// Plays one action clip, then lets the lunge blink home. An empty slot is the ordinary attack strip; a named
        /// slot is an ability strip. Both finish through the same callback, so the lunge can never be left waiting for a
        /// clip that another play call replaced.
        /// </summary>
        private void StartSwing(string clipSlot)
        {
            CharacterAnimator swing = EnsureAnimator();

            if (swing == null || !swing.HasArt)
            {
                return;
            }

            CharacterArtSet art = appliedData != null ? appliedData.ArtSet : null;
            Sprite[] frames = art != null && !string.IsNullOrEmpty(clipSlot) ? art.FramesFor(clipSlot) : null;

            if (frames != null && frames.Length > 0)
            {
                swing.PlayClip(frames, art.FpsFor(clipSlot), OnSwingClipFinished);
                return;
            }

            swing.PlayAttack(OnSwingClipFinished);
        }

        private void OnSwingClipFinished()
        {
            if (lunge != null)
            {
                lunge.CompleteSwing();
            }
        }

        /// <summary>An ability healed this hero: refresh the bar so the number matches the fight.</summary>
        private void OnHealed(HealEvent heal)
        {
            if (heal.Side != CombatantSide.Party || heal.Index != heroIndex)
            {
                return;
            }

            if (hpBar != null)
            {
                hpBar.SetVisible(true);
                hpBar.SetFill(heal.TargetMaxHealth <= 0d ? 0f : (float)(heal.TargetHealth / heal.TargetMaxHealth));
                hpBar.SetValueLabel(heal.TargetHealth, heal.TargetMaxHealth);
            }
        }

        /// <summary>An ability raised a guard on this hero: a short blue flash, because no health moved.</summary>
        private void OnShieldApplied(ShieldEvent shield)
        {
            if (shield.Side != CombatantSide.Party || shield.Index != heroIndex)
            {
                return;
            }

            hitJuice.Flash(baseColor, shieldFlashColor, shieldFlashDurationSec);
        }

        private void OnHeroDamaged(int index, double damage, double currentHealth, double maxHealth, int attackerEnemyIndex, bool isCritical, bool isGuarded)
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

            hitJuice.Flash(baseColor, hitFlashColor, hitFlashDurationSec);
            hitJuice.Punch(hitPunchScale, hitPunchDurationSec);

            // Land the dash FIRST: the number and the word are placed from the body's position, so the body must
            // already be at the impact point when they are read. (Ordering, not magic - this used to be last.)
            if (lunge != null && lunge.IsBusy)
            {
                lunge.Impact();
                lunge.CompleteSwing();
            }

            // This unit posts its own numbers: the text is placed from its live body position, so a slot on the edge of
            // the board (the bottom hero) is not shifted inward by the pool's centre-of-screen bookkeeping.
            FloatingDamageTextPool pool = FloatingDamageTextPool.Instance;

            if (pool != null)
            {
                FloatingTextStyle style = isCritical
                    ? FloatingTextStyle.Critical
                    : pool.IsAbilityHit(attackerEnemyIndex) ? FloatingTextStyle.AbilityHit : FloatingTextStyle.Incoming;

                pool.ShowNumber(BodyRect, damage, style);
            }

            // A guarded hit: say GUARD at this unit's body, which is where the knight actually stands right now.
            if (isGuarded)
            {
                FloatingDamageTextPool.Instance?.ShowWord(BodyRect, "GUARD", FloatingTextStyle.Guard);
            }

            if (animator != null && animator.HasArt)
            {
                animator.PlayHurt();
            }
        }

        /// <summary>
        /// The blow landed (the sim's impact). The swing clip is already playing - it started on the announce - so
        /// this only plants the hero on the target and restarts the pose clock at the hit.
        /// </summary>
        private void OnAllyAttackLanded(EnemyDamagedInfo info)
        {
            if (info.AttackerIndex != heroIndex)
            {
                return;
            }

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
            }

            return lunge;
        }

        private void Update()
        {
            TickSlide();

            if (iconImage == null)
            {
                return;
            }

            if (!hitJuice.TickFlash(Time.deltaTime, iconImage))
            {
                // Not mid-blink (the lunge owns alpha while it fades in/out); return to the resting tint.
                if (lunge == null || !lunge.IsBusy)
                {
                    if (iconImage.color != baseColor)
                    {
                        iconImage.color = baseColor;
                    }
                }
            }

            hitJuice.TickPunch(Time.deltaTime, transform);
        }

        /// <summary>Eases the view toward the pad it was sent to; the lunge re-captures home on landing.</summary>
        private void TickSlide()
        {
            if (slideT < 0f)
            {
                return;
            }

            RectTransform self = transform as RectTransform;

            if (self == null)
            {
                slideT = -1f;
                return;
            }

            slideT += Time.deltaTime / Mathf.Max(0.01f, slideSeconds);
            float t = Mathf.Clamp01(slideT);
            float eased = t * t * (3f - 2f * t);
            self.anchoredPosition = Vector2.Lerp(slideFrom, slideTo, eased);

            if (t < 1f)
            {
                return;
            }

            slideT = -1f;
        }
    }
}