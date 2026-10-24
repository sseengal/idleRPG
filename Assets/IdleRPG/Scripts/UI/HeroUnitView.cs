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
        [Tooltip("Flash colour when an ability raises a guard on this hero (no health moved, so the bar is untouched).")]
        [SerializeField] private Color shieldFlashColor = new Color(0.55f, 0.8f, 1f, 1f);
        [SerializeField] private float shieldFlashDurationSec = 0.2f;

        private Color baseColor = Color.white;
        private readonly UnitHitJuice hitJuice = new UnitHitJuice();

        private CharacterAnimator animator;
        private CharacterLunge lunge;
        private HeroData appliedData;

        public int HeroIndex => heroIndex;

        public void Configure(int index)
        {
            heroIndex = index;
            UnitAnchors.Register(CombatantSide.Party, index, transform as RectTransform);
        }

        /// <summary>
        /// Wires a view that was created in code (the formation strip builds a view per slot at runtime, so it
        /// cannot use the inspector). Kept separate from <see cref="Configure(int)"/> so the scene path is unchanged.
        /// </summary>
        public void ConfigureRuntime(int index, Image icon, HpBarView hp, TextMeshProUGUI label, CanvasGroup group)
        {
            heroIndex = index;
            UnitAnchors.Register(CombatantSide.Party, index, transform as RectTransform);
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
            hitJuice.Clear();
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
            UnitAnchors.Register(CombatantSide.Party, heroIndex, transform as RectTransform);
            GameEvents.HeroDamaged += OnHeroDamaged;
            GameEvents.HeroDied += OnHeroDied;
            GameEvents.EnemyDamaged += OnAllyAttackLanded;
            GameEvents.SwingStarted += OnSwingStarted;
            GameEvents.Healed += OnHealed;
            GameEvents.ShieldApplied += OnShieldApplied;
            GameEvents.HeroStatsChanged += OnHeroStatsChanged;
            GameEvents.WaveCompleted += OnWaveChanged;
            GameEvents.StageChanged += OnStageChangedHandler;
            GameEvents.CoverIntercepted += OnCoverIntercepted;
        }

        private void OnDisable()
        {
            UnitAnchors.Unregister(CombatantSide.Party, heroIndex, transform as RectTransform);
            GameEvents.HeroDamaged -= OnHeroDamaged;
            GameEvents.HeroDied -= OnHeroDied;
            GameEvents.EnemyDamaged -= OnAllyAttackLanded;
            GameEvents.SwingStarted -= OnSwingStarted;
            GameEvents.Healed -= OnHealed;
            GameEvents.ShieldApplied -= OnShieldApplied;
            GameEvents.HeroStatsChanged -= OnHeroStatsChanged;
            GameEvents.WaveCompleted -= OnWaveChanged;
            GameEvents.StageChanged -= OnStageChangedHandler;
            GameEvents.CoverIntercepted -= OnCoverIntercepted;
        }

        /// <summary>
        /// A guard intercepted a hit meant for <paramref name="allyIndex"/>. The guarding hero dashes in front of that
        /// ally (the same run-in the lunge already does), so the player sees who was saved.
        /// </summary>
        private void OnCoverIntercepted(int guardIndex, int allyIndex)
        {
            if (guardIndex != heroIndex || allyIndex < 0)
            {
                return;
            }

            // Create the mover on demand: the knight may not have attacked yet, and the dash used to be skipped
            // silently when no lunge existed (that is why the text stayed over his old slot).
            CharacterLunge dash = EnsureLunge();
            if (dash == null)
            {
                return;
            }

            if (dash.IsBusy)
            {
                dash.ResetToHome();
            }

            RectTransform allySlot = FindAllySlot(allyIndex);

            if (allySlot != null)
            {
                dash.BeginRun(allySlot);
            }
        }

        /// <summary>The slot rect of one sibling hero, or null when the board is not built yet.</summary>
        private RectTransform FindAllySlot(int allyIndex)
        {
            Transform parent = transform.parent;

            if (parent == null)
            {
                return null;
            }

            HeroUnitView[] views = parent.GetComponentsInChildren<HeroUnitView>(true);

            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] != null && views[i].HeroIndex == allyIndex)
                {
                    return views[i].transform as RectTransform;
                }
            }

            return null;
        }

        /// <summary>
        /// This hero committed an action: run to the target slot (hostile targets only) and play the clip. The clip
        /// slot rides on the event, so an ability needs no handler of its own - the ability path and the attack path are
        /// the same path, which is what stops one of them drifting.
        /// </summary>
        private void OnSwingStarted(int attackerIndex, CombatantSide attackerSide, int targetIndex, CombatantSide targetSide, string clipSlot, int coveredAllyIndex, bool stayPlanted)
        {
            if (attackerSide != CombatantSide.Party || attackerIndex != heroIndex)
            {
                return;
            }

            EnsureLunge();
            StartSwing(clipSlot);

            // A self/party guard stays planted: a guard that charges at nothing is a lie.
            // A ranged spell sets stayPlanted too: the caster throws from where it stands.
            if (stayPlanted || targetSide != CombatantSide.Enemy)
            {
                return;
            }

            HudController hud = HudController.Instance;
            EnemyStackView stack = hud != null ? hud.EnemyStack : null;
            RectTransform targetSlot = stack != null ? stack.GetEnemySlotRect(targetIndex) : null;

            if (lunge != null && !lunge.IsBusy && targetSlot != null)
            {
                lunge.BeginRun(targetSlot);
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

            // This unit posts its own numbers: the text is placed from its live transform, so a slot on the edge of
            // the board (the bottom hero) is not shifted inward by the pool's centre-of-screen bookkeeping.
            FloatingDamageTextPool pool = FloatingDamageTextPool.Instance;

            if (pool != null)
            {
                FloatingTextStyle style = isCritical
                    ? FloatingTextStyle.Critical
                    : pool.IsAbilityHit(attackerEnemyIndex) ? FloatingTextStyle.AbilityHit : FloatingTextStyle.Incoming;

                pool.ShowNumber(transform as RectTransform, damage, style);
            }

            // A guarded hit: say GUARD at this unit's own transform, which is where the knight actually stands right now.
            if (isGuarded)
            {
                FloatingDamageTextPool.Instance?.ShowWord(transform as RectTransform, "GUARD", FloatingTextStyle.Guard);
            }

            // A covered hit: land the dash on the damage beat, then blink home.
            if (lunge != null && lunge.IsBusy)
            {
                lunge.Impact();
                lunge.CompleteSwing();
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
    }
}