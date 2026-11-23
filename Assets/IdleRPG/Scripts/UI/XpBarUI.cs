using System.Collections.Generic;
using IdleRPG.Core;
using IdleRPG.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// The party XP bar: every label on the left, a blue fill on the right, and the fill that moves as XP lands.
    ///
    /// ELI5: the shared experience bar for the three heroes. Level and numbers sit together on the left so the eye
    /// finds them in one place. The blue part grows in steps while blue motes fly in from the enemy that paid. When
    /// the bar fills, it flashes and starts the next level from empty.
    ///
    /// It draws only what the game tells it: <see cref="GameManager.Progression"/> owns the maths and publishes a
    /// receipt per payment (<see cref="GameEvents.PartyXpGained"/>). The receipt carries the start and end points,
    /// so this view never guesses XP from a stage, a boss flag or an enemy type. A new XP source therefore needs no
    /// change here. The numbers are always shown; the level-up card is the one place that carries the pick count.
    /// </summary>
    public sealed class XpBarUI : MonoBehaviour
    {
        /// <summary>Current bar, or null when the scene has no battle HUD (debug scenes).</summary>
        public static XpBarUI Instance { get; private set; }

        [Header("Motion")]
        [Tooltip("How fast the drawn fill eases toward the XP it was given. Higher = snappier.")]
        [SerializeField] private float fillEasePerSecond = 9f;

        [Tooltip("Seconds after one share lands before the next share may be forced out. A safety net only.")]
        [SerializeField] private float shareFallbackSec = 0.22f;

        [Tooltip("How long the first mote may take to arrive before the bar starts filling anyway. " +
                 "Covers a gain with no enemy (debug, camp, a future login bonus) and sits just above burst + pull.")]
        [SerializeField] private float firstShareGraceSec = 0.75f;

        [Tooltip("How long the level-up wash lasts.")]
        [SerializeField] private float levelFlashSec = 0.24f;

        [Tooltip("Oldest payments are folded into the truth when this many wait. Keeps the bar honest at x1.6.")]
        [SerializeField] private int maxQueuedGains = 6;

        [Header("Motes")]
        [Tooltip("Motes per payment, inclusive. Each mote carries an equal share of that payment's XP.")]
        [SerializeField] private int minMotesPerGain = 3;
        [SerializeField] private int maxMotesPerGain = 5;

        /// <summary>The bar never shrinks below this, so a long number cannot eat it.</summary>
        private const float MinBarWidth = 120f;

        /// <summary>
        /// The track keeps today's share of the strip: the old anchors were 0.18..0.82 of its height. A layout group
        /// sizes the cross axis from sizeDelta, so the ratio has to be applied to the height directly.
        /// </summary>
        private const float TrackHeightRatio = 0.64f;

        /// <summary>One payment waiting to be shown. Shares are released one per arriving mote.</summary>
        private sealed class GainPacket
        {
            public double Amount;
            public int TotalShares;
            public int SharesLeft;
            public float NextReleaseAt;
            public int EndLevel;
            public double EndXp;
            public bool AtMax;
        }

        private readonly List<GainPacket> gains = new List<GainPacket>();

        private TextMeshProUGUI levelLabel;
        private TextMeshProUGUI valueLabel;
        private RectTransform trackRect;
        private RectTransform fillRect;
        private RectTransform edgeRect;
        private Image flashImage;

        private PartyProgression progression;
        private int playLevel = 1;
        private double playXp;
        private float displayedProgress;
        private float targetProgress;
        private bool atMax;

        private float flashElapsed = 99f;
        private float punchElapsed = 99f;

        private void Awake()
        {
            Instance = this;
            Build();
        }

        private void OnEnable()
        {
            GameEvents.PartyXpGained += OnXpGained;
            GameEvents.PartyLevelGained += OnLevelGained;
        }

        private void OnDisable()
        {
            GameEvents.PartyXpGained -= OnXpGained;
            GameEvents.PartyLevelGained -= OnLevelGained;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            // The manager restores the save during wiring, which happens before this view's first frame, so the
            // first real read happens here rather than in Awake.
            Refresh();
        }

        /// <summary>Draws the truth straight from the manager and clears anything still animating.</summary>
        public void Refresh()
        {
            GameManager manager = Manager;

            if (manager == null || manager.Progression == null)
            {
                return;
            }

            progression = manager.Progression;
            gains.Clear();

            playLevel = progression.Level;
            playXp = progression.Xp;
            atMax = progression.AtMaxLevel;

            displayedProgress = targetProgress = ProgressFor(playLevel, playXp);

            UpdateLevelLabel();
            UpdateValueLabel();
            SetFill(displayedProgress);
            SnapEffects();
        }

        private void OnXpGained(XpGain gain)
        {
            if (!gain.HasXp)
            {
                return;
            }

            if (progression == null)
            {
                progression = Manager != null ? Manager.Progression : null;
            }

            atMax = gain.AtMax;

            int motes = Random.Range(minMotesPerGain, maxMotesPerGain + 1);
            GainPacket packet = new GainPacket
            {
                Amount = gain.Amount,
                TotalShares = motes,
                SharesLeft = motes,
                EndLevel = gain.EndLevel,
                EndXp = gain.EndXp,
                AtMax = gain.AtMax
            };

            // Cap the queue: the bar may show less detail under x1.6, but it may never show a false number.
            while (gains.Count >= maxQueuedGains)
            {
                FlushOldest();
            }

            bool flying = XpFlyVfx.Instance != null && XpFlyVfx.Instance.Play(motes);
            packet.NextReleaseAt = Time.unscaledTime + (flying ? firstShareGraceSec : 0.05f);
            gains.Add(packet);
        }

        private void OnLevelGained(int level, int pendingPicks)
        {
            // The level-up card owns the pick count. This handler exists only for the reset case: a reset or an
            // ascension moves the level DOWN, so there is no payment to animate and the bar must snap.
            if (level < playLevel)
            {
                Refresh();
            }
        }

        /// <summary>Called by the flying motes when one lands. One landing = one share of its payment.</summary>
        public void SignalMoteArrived()
        {
            if (gains.Count > 0)
            {
                ReleaseShare();
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (!Mathf.Approximately(displayedProgress, targetProgress))
            {
                float ease = 1f - Mathf.Exp(-fillEasePerSecond * dt);
                displayedProgress = Mathf.Lerp(displayedProgress, targetProgress, ease);

                if (Mathf.Abs(displayedProgress - targetProgress) < 0.0005f)
                {
                    displayedProgress = targetProgress;
                }

                SetFill(displayedProgress);
            }

            // Safety net: a payment with no mote (no enemy, debug, a lost burst) still fills, one share at a time.
            if (gains.Count > 0 && Time.unscaledTime >= gains[0].NextReleaseAt)
            {
                ReleaseShare();
            }

            UpdateEffects(dt);
        }

        private void ReleaseShare()
        {
            GainPacket packet = gains[0];
            packet.SharesLeft--;

            AdvanceBy(packet.Amount / packet.TotalShares);
            packet.NextReleaseAt = Time.unscaledTime + shareFallbackSec;

            if (packet.SharesLeft <= 0)
            {
                gains.RemoveAt(0);

                // Land exactly on the receipt, so rounding can never leave the bar a hair off the save.
                AdvanceTo(packet.EndLevel, packet.EndXp, packet.AtMax);
            }
        }

        private void FlushOldest()
        {
            GainPacket packet = gains[0];
            gains.RemoveAt(0);
            AdvanceTo(packet.EndLevel, packet.EndXp, packet.AtMax);
        }

        /// <summary>Adds XP to the playhead and walks every level it crosses.</summary>
        private void AdvanceBy(double amount)
        {
            playXp += amount < 0d ? 0d : amount;

            if (progression == null || atMax)
            {
                targetProgress = ProgressFor(playLevel, playXp);
                UpdateValueLabel();
                return;
            }

            int guard = 0;

            while (guard++ < 64)
            {
                double required = progression.RequiredXpFor(playLevel);

                if (required <= 0d || playXp < required)
                {
                    break;
                }

                playXp -= required;
                playLevel++;
                FlashLevelUp();
            }

            UpdateLevelLabel();
            UpdateValueLabel();
            targetProgress = ProgressFor(playLevel, playXp);
        }

        /// <summary>Jumps the playhead to a receipt's end point, with a flash when the level moved.</summary>
        private void AdvanceTo(int level, double xp, bool max)
        {
            if (level > playLevel)
            {
                FlashLevelUp();
            }

            playLevel = level < 1 ? 1 : level;
            playXp = xp < 0d ? 0d : xp;
            atMax = max;

            UpdateLevelLabel();
            UpdateValueLabel();
            targetProgress = ProgressFor(playLevel, playXp);
        }

        private float ProgressFor(int level, double xp)
        {
            if (atMax)
            {
                return 1f;
            }

            if (progression == null)
            {
                return 0f;
            }

            double required = progression.RequiredXpFor(level);
            double progress = required > 0d ? xp / required : 1d;
            return (float)(progress < 0d ? 0d : (progress > 1d ? 1d : progress));
        }

        private void UpdateLevelLabel()
        {
            if (levelLabel != null)
            {
                levelLabel.SetText("LV " + playLevel);
            }
        }

        private void UpdateValueLabel()
        {
            if (valueLabel == null)
            {
                return;
            }

            if (atMax || progression == null)
            {
                valueLabel.SetText("MAX");
                return;
            }

            double required = progression.RequiredXpFor(playLevel);
            valueLabel.SetText(Utils.NumberFormatter.Format(playXp) + "/" + Utils.NumberFormatter.Format(required));
        }

        private void FlashLevelUp()
        {
            flashElapsed = 0f;
            punchElapsed = 0f;
        }

        private void SnapEffects()
        {
            flashElapsed = 99f;
            punchElapsed = 99f;

            if (flashImage != null)
            {
                flashImage.color = UiTheme.WithAlpha(UiTheme.XpFlash, 0f);
            }

            if (levelLabel != null)
            {
                levelLabel.rectTransform.localScale = Vector3.one;
            }
        }

        private void UpdateEffects(float dt)
        {
            // Level wash: alpha rises and falls once. One line, no tween library.
            if (flashImage != null)
            {
                if (flashElapsed < levelFlashSec)
                {
                    flashElapsed += dt;
                    float t = Mathf.Clamp01(flashElapsed / Mathf.Max(0.01f, levelFlashSec));
                    float alpha = Mathf.Sin(t * Mathf.PI) * 0.45f;
                    flashImage.color = UiTheme.WithAlpha(UiTheme.XpFlash, alpha);
                }
                else if (flashImage.color.a != 0f)
                {
                    flashImage.color = UiTheme.WithAlpha(UiTheme.XpFlash, 0f);
                }
            }

            // Level label punch: a single small scale beat, so the new level is seen before it is read.
            if (levelLabel != null)
            {
                if (punchElapsed < levelFlashSec)
                {
                    punchElapsed += dt;
                    float t = Mathf.Clamp01(punchElapsed / Mathf.Max(0.01f, levelFlashSec));
                    float scale = 1f + 0.12f * Mathf.Sin(t * Mathf.PI);
                    levelLabel.rectTransform.localScale = new Vector3(scale, scale, 1f);
                }
                else if (levelLabel.rectTransform.localScale.x != 1f)
                {
                    levelLabel.rectTransform.localScale = Vector3.one;
                }
            }
        }

        private GameManager Manager =>
            HudController.Instance != null ? HudController.Instance.GameManager : null;

        /// <summary>World point where the fill currently ends: the aim point for the flying motes.</summary>
        public Vector3 FillEdgeWorld
        {
            get
            {
                if (fillRect == null)
                {
                    return transform.position;
                }

                Rect rect = fillRect.rect;
                return fillRect.TransformPoint(new Vector3(rect.xMax, rect.center.y, 0f));
            }
        }

        /// <summary>Draws the fill as the left-hand slice of the track (0 = empty, 1 = full).</summary>
        private void SetFill(float progress)
        {
            if (fillRect == null)
            {
                return;
            }

            float clamped = Mathf.Clamp01(progress);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(clamped, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            // The leading edge is a thin bright cap: the part that is "about to arrive".
            if (edgeRect != null)
            {
                bool show = clamped > 0.001f && clamped < 0.999f;
                edgeRect.gameObject.SetActive(show);

                if (show)
                {
                    edgeRect.anchorMin = new Vector2(clamped, 0.12f);
                    edgeRect.anchorMax = new Vector2(clamped, 0.88f);
                    edgeRect.sizeDelta = new Vector2(2f, 0f);
                    edgeRect.anchoredPosition = Vector2.zero;
                }
            }
        }

        /// <summary>Today's share of the strip, in canvas units. Recomputed when the strip's height changes.</summary>
        private float TrackHeight()
        {
            return Mathf.Max(1f, ((RectTransform)transform).rect.height * TrackHeightRatio);
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyTrackHeight();
        }

        private void ApplyTrackHeight()
        {
            if (trackRect == null)
            {
                return;
            }

            float height = TrackHeight();

            if (Mathf.Abs(trackRect.sizeDelta.y - height) > 0.01f)
            {
                trackRect.sizeDelta = new Vector2(trackRect.sizeDelta.x, height);
            }
        }

        // ------------------------------------------------------------------
        // Build (once, at Awake)
        // ------------------------------------------------------------------
        private void Build()
        {
            if (levelLabel != null)
            {
                return;
            }

            RectTransform root = (RectTransform)transform;

            // A dark backing, so the bar reads as a strip in the row of strips instead of floating text.
            Image backing = UiRuntime.CreatePanel(root, null, UiTheme.PanelDeep, raycast: false);
            UiRuntime.Stretch(backing.rectTransform);
            backing.transform.SetAsFirstSibling();

            // The words and the bar live in one layout row. The labels are content-sized, so the gaps stay at the
            // ruler's numbers and the bar takes every unit that is left. No fixed slot, no dead space.
            GameObject rowNode = UiRuntime.CreateNode("Row", root);
            RectTransform row = (RectTransform)rowNode.transform;
            UiRuntime.Stretch(row);

            HorizontalLayoutGroup rowLayout = rowNode.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset((int)UiSpace.Small, 0, 0, 0);
            rowLayout.spacing = UiSpace.Medium;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            // Level and numbers travel together with their own, tighter gap.
            GameObject textNode = UiRuntime.CreateNode("Text", row);
            RectTransform textGroup = (RectTransform)textNode.transform;
            UiRuntime.Stretch(textGroup);

            HorizontalLayoutGroup textLayout = textNode.AddComponent<HorizontalLayoutGroup>();
            textLayout.padding = new RectOffset(0, 0, 0, 0);
            textLayout.spacing = UiSpace.Small;
            textLayout.childAlignment = TextAnchor.MiddleLeft;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = false;
            textLayout.childForceExpandWidth = false;
            textLayout.childForceExpandHeight = false;

            levelLabel = UiRuntime.CreateText(textGroup, "Level", "LV 1", UiType.Role.Small,
                TextAlignmentOptions.MidlineLeft, UiTheme.Text);
            UiType.ApplyAuto(levelLabel, UiType.Role.Small);
            UiRuntime.Stretch(levelLabel.rectTransform);

            valueLabel = UiRuntime.CreateText(textGroup, "Value", "0/0", UiType.Role.Micro,
                TextAlignmentOptions.MidlineLeft, UiTheme.Text);
            UiType.ApplyAuto(valueLabel, UiType.Role.Micro);
            UiRuntime.Stretch(valueLabel.rectTransform);

            // The track takes the rest of the row. Its height is today's 64% of the strip, applied as sizeDelta:
            // a layout group sizes the cross axis from sizeDelta, not from anchors.
            Image track = UiRuntime.CreatePanel(row, null, UiTheme.CardDark);
            track.name = "Track";
            track.raycastTarget = false;
            trackRect = track.rectTransform;
            trackRect.anchorMin = new Vector2(0f, 0.5f);
            trackRect.anchorMax = new Vector2(1f, 0.5f);
            trackRect.anchoredPosition = Vector2.zero;
            trackRect.sizeDelta = new Vector2(0f, TrackHeight());

            LayoutElement trackLayout = track.gameObject.AddComponent<LayoutElement>();
            trackLayout.flexibleWidth = 1f;
            trackLayout.minWidth = MinBarWidth;

            // Fill by WIDTH, not by Image.type = Filled: a sprite-less filled image is the one case UGUI draws
            // inconsistently, and a bar that never moves is worse than a plain rectangle.
            Image fillImage = UiRuntime.CreatePanel(track.transform, null, UiTheme.XpFill);
            fillImage.name = "Fill";
            fillImage.raycastTarget = false;
            fillRect = fillImage.rectTransform;

            // The leading edge: a 2-unit bright cap that travels with the fill.
            Image edge = UiRuntime.CreatePanel(track.transform, null, UiTheme.XpFillLight);
            edge.name = "Edge";
            edge.raycastTarget = false;
            edgeRect = edge.rectTransform;
            edgeRect.pivot = new Vector2(0f, 0.5f);

            // One-shot wash when a level lands. It sits inside the track, so it can never tint the words.
            Image flash = UiRuntime.CreatePanel(track.transform, null, UiTheme.XpFlash, raycast: false);
            flash.name = "LevelFlash";
            UiRuntime.Stretch(flash.rectTransform);
            flashImage = flash;
            flashImage.color = UiTheme.WithAlpha(UiTheme.XpFlash, 0f);

            SetFill(0f);
        }
    }
}

