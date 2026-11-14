using IdleRPG.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// The party XP bar: level on the left, a gold fill in the middle, the numbers on the right.
    ///
    /// ELI5: the shared experience bar for the three heroes. It builds its own pieces when it wakes up, so a scene
    /// rebuild can never leave it half-wired - the builder only has to place one empty rect in the band under the
    /// header.
    ///
    /// It draws only what the game tells it: <see cref="GameManager.Progression"/> owns the maths and publishes
    /// <see cref="GameEvents.PartyXpChanged"/>. When a choice is owed, the right-hand label says so instead of the
    /// numbers, because that is the thing the player must act on.
    /// </summary>
    public sealed class XpBarUI : MonoBehaviour
    {
        [Header("Colours")]
        [SerializeField] private Color trackColor = UiTheme.CardDark;
        [SerializeField] private Color fillColor = UiTheme.GoldDark;

        private TextMeshProUGUI levelLabel;
        private TextMeshProUGUI valueLabel;
        private TextMeshProUGUI picksLabel;
        private RectTransform fillRect;

        private void Awake()
        {
            Build();
        }

        private void OnEnable()
        {
            GameEvents.PartyXpChanged += OnXpChanged;
            GameEvents.PartyLevelGained += OnLevelGained;
        }

        private void OnDisable()
        {
            GameEvents.PartyXpChanged -= OnXpChanged;
            GameEvents.PartyLevelGained -= OnLevelGained;
        }

        private void Start()
        {
            // The manager restores the save during wiring, which happens before this view's first frame, so the
            // first real read happens here rather than in Awake.
            Refresh();
        }

        /// <summary>Draws the current values straight from the manager (a save load may land after an event).</summary>
        public void Refresh()
        {
            GameManager manager = HudController.Instance != null ? HudController.Instance.GameManager : null;

            if (manager == null || manager.Progression == null)
            {
                return;
            }

            Show(manager.Progression.Level, manager.Progression.Xp, manager.Progression.RequiredXp,
                manager.Progression.PendingPicks);
        }

        private void OnXpChanged(int level, double xp, double requiredXp)
        {
            GameManager manager = HudController.Instance != null ? HudController.Instance.GameManager : null;
            int picks = manager != null && manager.Progression != null ? manager.Progression.PendingPicks : 0;

            Show(level, xp, requiredXp, picks);
        }

        private void OnLevelGained(int level, int pendingPicks)
        {
            ApplyPicks(pendingPicks);
        }

        private void Show(int level, double xp, double requiredXp, int pendingPicks)
        {
            if (levelLabel != null)
            {
                levelLabel.SetText("LV " + level);
            }

            double progress = requiredXp > 0d ? xp / requiredXp : 1d;
            SetFill(progress);

            if (valueLabel != null)
            {
                valueLabel.SetText(requiredXp > 0d
                    ? Utils.NumberFormatter.Format(xp) + " / " + Utils.NumberFormatter.Format(requiredXp)
                    : "MAX");
            }

            ApplyPicks(pendingPicks);
        }

        /// <summary>Draws the gold fill as the left-hand slice of the track (0 = empty, 1 = full).</summary>
        private void SetFill(double progress)
        {
            if (fillRect == null)
            {
                return;
            }

            float clamped = (float)(progress < 0d ? 0d : (progress > 1d ? 1d : progress));
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(clamped, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }

        private void ApplyPicks(int pendingPicks)
        {
            if (picksLabel == null)
            {
                return;
            }

            bool waiting = pendingPicks > 0;

            // The two labels share one slot: the choice is the thing to act on, so it takes the space from the
            // numbers instead of being drawn on top of them.
            picksLabel.gameObject.SetActive(waiting);

            if (valueLabel != null)
            {
                valueLabel.gameObject.SetActive(!waiting);
            }

            if (waiting)
            {
                picksLabel.SetText(pendingPicks == 1 ? "1 PICK" : pendingPicks + " PICKS");
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

            levelLabel = UiRuntime.CreateText(root, "Level", "LV 1", UiType.Role.Small,
                TextAlignmentOptions.MidlineLeft, UiTheme.Text);
            UiType.ApplyAuto(levelLabel, UiType.Role.Small);
            UiRuntime.Anchor(levelLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0.14f, 1f));

            Image track = UiRuntime.CreatePanel(root, null, trackColor);
            track.raycastTarget = false;
            UiRuntime.Anchor(track.rectTransform, new Vector2(0.15f, 0.18f), new Vector2(0.82f, 0.82f));

            // Fill by WIDTH, not by Image.type = Filled: a sprite-less filled image is the one case UGUI draws
            // inconsistently, and a bar that never moves is worse than a plain rectangle.
            Image fillImage = UiRuntime.CreatePanel(track.transform, null, fillColor);
            fillImage.name = "Fill";
            fillImage.raycastTarget = false;
            fillRect = fillImage.rectTransform;
            SetFill(0d);

            valueLabel = UiRuntime.CreateText(root, "Value", "0 / 0", UiType.Role.Micro,
                TextAlignmentOptions.MidlineRight, UiTheme.TextDim);
            UiRuntime.Anchor(valueLabel.rectTransform, new Vector2(0.83f, 0f), new Vector2(1f, 1f));

            // Sits over the numbers when a choice is owed: the thing the player must act on wins the space.
            picksLabel = UiRuntime.CreateText(root, "Picks", "1 PICK", UiType.Role.Micro,
                TextAlignmentOptions.MidlineRight, UiTheme.AccentLight);
            UiRuntime.Anchor(picksLabel.rectTransform, new Vector2(0.83f, 0f), new Vector2(1f, 1f));
            picksLabel.gameObject.SetActive(false);
        }
    }
}

