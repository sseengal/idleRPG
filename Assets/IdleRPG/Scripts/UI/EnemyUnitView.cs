using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Sim;

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

        [Header("Boss scale")]
        [SerializeField] private float bossNameSizeScale = 1.3f;

        /// <summary>
        /// The name's size, taken from the type scale so a role change carries here. A boss scales it up (below).
        /// The label is NOT auto-sized and NOT ellipsized: with NoWrap, either of those renders nothing at all when
        /// the name cannot fit the slot, and the name plate goes blank. It is masked (clipped) instead.
        /// </summary>
        private float baseNameSize = UiType.Size(UiType.Role.Small);

        [Header("Feedback")]
        [SerializeField] private float spawnPopScale = 0.65f;
        [SerializeField] private float spawnPopDurationSec = 0.18f;

        [Header("Hit juice (subtle)")]
        [SerializeField] private Color hitFlashColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private float hitFlashDurationSec = 0.14f;
        [SerializeField] private float critFlashDurationSec = 0.24f;
        [SerializeField] private float hitPunchScale = 0.94f;
        [SerializeField] private float critPunchScale = 0.88f;
        [SerializeField] private float punchDurationSec = 0.12f;
        [Tooltip("Flash colour when an ability raises a guard on this enemy (no health moved, so the bar is untouched).")]
        [SerializeField] private Color shieldFlashColor = new Color(0.55f, 0.8f, 1f, 1f);
        [SerializeField] private float shieldFlashDurationSec = 0.2f;

        private int enemyIndex;
        private float popTimer;

        private Color baseColor = Color.white;
        private readonly UnitHitJuice hitJuice = new UnitHitJuice();

        private CharacterAnimator animator;
        private CharacterLunge lunge;
        private Vector2 iconSlotSize;

        /// <summary>Which enemy of the wave this view shows (0-based).</summary>
        public int EnemyIndex => enemyIndex;

        /// <summary>Points this view at slot <paramref name="index"/> of the wave.</summary>
        public void Configure(int index)
        {
            enemyIndex = index;
            UnitAnchors.Register(CombatantSide.Enemy, index, transform as RectTransform);
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

            // baseNameSize comes from UiType (the label auto-sizes, so reading nameLabel.fontSize here would read a
            // computed value, not the role's ceiling).
        }

        private void OnEnable()
        {
            UnitAnchors.Register(CombatantSide.Enemy, enemyIndex, transform as RectTransform);
            GameEvents.EnemySpawned += OnEnemySpawned;
            GameEvents.EnemyDamaged += OnEnemyDamaged;
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.HeroDamaged += OnEnemyAttackLanded;
            GameEvents.SwingStarted += OnSwingStarted;
            GameEvents.Healed += OnHealed;
            GameEvents.ShieldApplied += OnShieldApplied;
        }

        private void OnDisable()
        {
            UnitAnchors.Unregister(CombatantSide.Enemy, enemyIndex, transform as RectTransform);
            GameEvents.EnemySpawned -= OnEnemySpawned;
            GameEvents.EnemyDamaged -= OnEnemyDamaged;
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.HeroDamaged -= OnEnemyAttackLanded;
            GameEvents.SwingStarted -= OnSwingStarted;
            GameEvents.Healed -= OnHealed;
            GameEvents.ShieldApplied -= OnShieldApplied;
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
                iconSlotSize = new Vector2(pixels, pixels);

                if (animator != null)
                {
                    animator.SetSlotSize(iconSlotSize);
                }
            }
        }

        /// <summary>
        /// Shows this enemy (sprite, name, full health bar). Called by the spawn event and by the stack when it
        /// re-lays out a wave, so a slot that was switched off last wave comes back correctly.
        /// </summary>
        public void Show(string enemyName, double maxHealth, bool isBoss)
        {
            EnemyData data = ResolveEnemyData();

            CharacterArtSet art = data != null ? data.ResolveArt(CurrentStage()) : null;

            if (spriteImage != null)
            {
                // An art set with no frames would animate nothing and leave the unit invisible, so it counts as "no art".
                bool hasAnimatedArt = art != null && art.HasAnimation;

                if (hasAnimatedArt)
                {
                    // Real art: animated frames, flipped to face the heroes, no tint.
                    CharacterAnimator animation = EnsureAnimator();
                    animation.FacingLeft = true;
                    animation.SetArt(art);
                    spriteImage.color = Color.white;
                }
                else if (data != null && data.EnemySprite != null)
                {
                    // Placeholder art: a stale animator from a previous archetype must not keep swapping
                    // frames over the placeholder, so it is cleared before the static sprite is shown.
                    if (animator != null)
                    {
                        animator.Clear();
                    }

                    spriteImage.sprite = data.EnemySprite;
                    spriteImage.color = data.PlaceholderTint;
                }

                // else: nothing is known about this slot yet (a spawn that lands before the simulator holds it).
                // The look is deliberately LEFT ALONE - clearing it here is what made the unit vanish.

                spriteImage.enabled = true;

                // Fresh state: capture the resting tint (flash lerps off it) and cancel any stale feedback.
                baseColor = spriteImage.color;
                hitJuice.Clear();
            }

            if (nameLabel != null)
            {
                nameLabel.SetText(isBoss ? $"{enemyName} <size=70%>BOSS</size>" : enemyName);

                // A boss gets the bigger size. NOT auto-size and NOT ellipsis: see baseNameSize above - either one
                // blanks the name plate when even the floor does not fit the slot. It is masked (clipped) instead.
                nameLabel.enableAutoSizing = false;
                nameLabel.fontSize = isBoss ? baseNameSize * bossNameSizeScale : baseNameSize;
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

            // The stack re-laid out this slot: re-capture home and park the unit there (mid-lunge units snap back).
            EnsureAnimator();
            CharacterLunge runner = EnsureLunge();
            if (runner != null)
            {
                runner.RefreshHome();
                runner.ResetToHome();
            }
        }

        private void OnEnemyDamaged(EnemyDamagedInfo info)
        {
            if (info.EnemyIndex != enemyIndex || hpBar == null)
            {
                return;
            }

            hpBar.SetFill(info.NormalizedHealth);
            hpBar.SetValueLabel(info.CurrentHealth, info.MaxHealth);

            // Hit juice: white flash + a small squash. Crits flash longer and punch deeper.
            hitJuice.Flash(hitFlashColor, baseColor, info.IsCritical ? critFlashDurationSec : hitFlashDurationSec);
            hitJuice.Punch(info.IsCritical ? critPunchScale : hitPunchScale, punchDurationSec);

            if (animator != null && animator.HasArt)
            {
                animator.PlayHurt();
            }
        }

        /// <summary>
        /// The blow landed (the sim's impact). The swing clip is already playing - it started on the announce - so
        /// this only plants the enemy on the target and restarts the pose clock at the hit.
        /// </summary>
        private void OnEnemyAttackLanded(int heroIndex, double damage, double currentHealth, double maxHealth, int attackerEnemyIndex, bool isCritical, bool isGuarded)
        {
            if (attackerEnemyIndex != enemyIndex)
            {
                return;
            }

            if (lunge != null)
            {
                lunge.Impact();
            }
        }

        /// <summary>
        /// This enemy committed an action: start the clip and run at the hero it is about to hit. The clip slot lets an
        /// enemy cast play its own strip through the same entry point a swing uses.
        /// </summary>
        private void OnSwingStarted(int attackerIndex, CombatantSide attackerSide, int targetIndex, CombatantSide targetSide, string clipSlot, int coveredAllyIndex, bool stayPlanted)
        {
            if (attackerSide != CombatantSide.Enemy || attackerIndex != enemyIndex)
            {
                return;
            }

            EnsureLunge();
            StartSwing(clipSlot);

            // A self/party guard stays planted; only a hostile target pulls the enemy forward.
            if (targetSide != CombatantSide.Party)
            {
                return;
            }

            HudController hud = HudController.Instance;
            FormationBoardView board = hud != null ? hud.FormationBoard : null;

            // A guard intercepts the hit: the knight has already stepped in front of the ally, so the enemy runs at
            // THAT slot. Using the knight's own index would send it into an empty slot he just left.
            int visualTargetIndex = coveredAllyIndex >= 0 ? coveredAllyIndex : targetIndex;
            RectTransform targetSlot = board != null ? board.GetHeroSlotRect(visualTargetIndex) : null;

            if (lunge != null && !lunge.IsBusy && targetSlot != null)
            {
                lunge.BeginRun(targetSlot);
            }
        }

        /// <summary>
        /// Plays one action clip, then lets the lunge blink home. Empty slot = the ordinary attack; a named slot = the
        /// ability strip the cast asked for.
        /// </summary>
        private void StartSwing(string clipSlot)
        {
            CharacterAnimator swing = EnsureAnimator();

            if (swing == null || !swing.HasArt)
            {
                return;
            }

            EnemyData data = ResolveEnemyData();
            CharacterArtSet art = data != null ? data.ResolveArt(CurrentStage()) : null;
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

        /// <summary>An ability healed this enemy: refresh the bar so the number matches the fight.</summary>
        private void OnHealed(HealEvent heal)
        {
            if (heal.Side != CombatantSide.Enemy || heal.Index != enemyIndex || hpBar == null)
            {
                return;
            }

            hpBar.SetFill(heal.TargetMaxHealth <= 0d ? 0f : (float)(heal.TargetHealth / heal.TargetMaxHealth));
            hpBar.SetValueLabel(heal.TargetHealth, heal.TargetMaxHealth);
        }

        /// <summary>An ability raised a guard on this enemy: a short blue flash, because no health moved.</summary>
        private void OnShieldApplied(ShieldEvent shield)
        {
            if (shield.Side != CombatantSide.Enemy || shield.Index != enemyIndex)
            {
                return;
            }

            hitJuice.Flash(shieldFlashColor, baseColor, shieldFlashDurationSec);
        }

        private void OnEnemyKilled(string enemyName, double goldReward, int index)
        {
            if (index != enemyIndex)
            {
                return;
            }

            // No fade after death, and nothing is hidden: the death clip plays and its last frame stays on screen,
            // exactly like a hero's death pose. The next wave's Show() re-binds this slot.
            if (lunge != null)
            {
                lunge.ResetToHome();
            }

            if (animator != null && animator.HasArt)
            {
                animator.PlayDeath();
            }
        }

        private void Update()
        {
            if (root == null)
            {
                return;
            }

            // Spawn pop wins over a hit punch (they basically never overlap anyway).
            if (popTimer > 0f)
            {
                popTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(popTimer / Mathf.Max(0.0001f, spawnPopDurationSec));
                float scale = Mathf.Lerp(spawnPopScale, 1f, t);
                root.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                hitJuice.TickPunch(Time.deltaTime, root);
            }

            // White flash cross-fades to the resting tint (placeholder sprites pop against it).
            if (spriteImage == null)
            {
                return;
            }

            if (!hitJuice.TickFlash(Time.deltaTime, spriteImage))
            {
                // Not mid-blink (the lunge owns alpha while it fades in/out); return to the resting tint.
                if (lunge == null || !lunge.IsBusy)
                {
                    if (spriteImage.color != baseColor)
                    {
                        spriteImage.color = baseColor;
                    }
                }
            }
        }

        private static int CurrentStage()
        {
            GameManager manager = HudController.Instance != null ? HudController.Instance.GameManager : null;
            return manager != null ? manager.CurrentStage : 1;
        }

        private CharacterAnimator EnsureAnimator()
        {
            if (animator == null && spriteImage != null)
            {
                animator = spriteImage.GetComponent<CharacterAnimator>();
                if (animator == null)
                {
                    animator = spriteImage.gameObject.AddComponent<CharacterAnimator>();
                }

                // The stack sizes the slot per wave; the animator scales around that, never around its own output.
                animator.SetSlotSize(iconSlotSize != Vector2.zero ? iconSlotSize : spriteImage.rectTransform.sizeDelta);
                spriteImage.preserveAspect = true;
                animator.FacingLeft = true;
            }

            return animator;
        }

        /// <summary>The lunge (run-to-target + blink) rides on the same icon so it tracks the art's pivot.</summary>
        private CharacterLunge EnsureLunge()
        {
            if (lunge == null && spriteImage != null)
            {
                lunge = spriteImage.GetComponent<CharacterLunge>();
                if (lunge == null)
                {
                    lunge = spriteImage.gameObject.AddComponent<CharacterLunge>();
                }

                lunge.RefreshHome();
            }

            return lunge;
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