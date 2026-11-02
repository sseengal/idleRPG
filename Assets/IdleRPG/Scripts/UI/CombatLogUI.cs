using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;

namespace IdleRPG.UI
{
    /// <summary>
    /// Live battle feed ("Mage hits Ogre for 24"). A VIEW ONLY: the history lives in <see cref="BattleLog"/>.
    ///
    /// ONE RULE: every entry the store holds becomes its own line. Nothing is aggregated, throttled or dropped.
    /// The visible list is a fixed-length window (oldest labels are recycled) purely so a long idle session cannot
    /// grow label memory without bound.
    ///
    /// The view subscribes only while it is visible and redraws the window from the store on every enable, so a
    /// hidden battle page can never lose, reorder or freeze the feed. Recycled labels are always moved to the end
    /// of the hierarchy: the layout group draws in hierarchy order.
    /// </summary>
    public sealed partial class CombatLogUI : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform content;
        [SerializeField] private TextMeshProUGUI lineTemplate;

        [Header("Limits")]
        [Tooltip("Visible scrollback length. Older lines recycle; logging itself is never capped.")]
        [SerializeField] private int maxLines = 100;

        [Tooltip("Labels created up front (the pool grows on demand up to the scrollback length).")]
        [SerializeField] private int poolSize = 40;

        [Tooltip("Line height in reference pixels. The dialogue role sets the default.")]
        [SerializeField] private float lineHeight = UiTheme.LogLineHeight;

        [Tooltip("Vertical gap between lines.")]
        [SerializeField] private float lineSpacing = 4f;

        [Tooltip("Padding above and below the list.")]
        [SerializeField] private float contentPadding = 12f;

        [Header("Colours")]
        [SerializeField] private Color heroHitColor = new Color(0.95f, 0.95f, 1f, 1f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color incomingColor = new Color(1f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color killColor = new Color(0.55f, 0.95f, 0.6f, 1f);
        [SerializeField] private Color eventColor = new Color(0.70f, 0.80f, 1f, 1f);
        [SerializeField] private Color goldColor = new Color(1f, 0.85f, 0.45f, 1f);
        [Tooltip("Colour of a heal line.")]
        [SerializeField] private Color healColor = new Color(0.5f, 0.95f, 0.6f, 1f);
        [Tooltip("Colour of a guard (shield) line.")]
        [SerializeField] private Color guardColor = new Color(0.6f, 0.82f, 1f, 1f);
        [Tooltip("Colour of an enemy ability line. Loud on purpose: the player must spot it in the scroll.")]
        [SerializeField] private Color enemyAbilityColor = new Color(1f, 0.55f, 0.15f, 1f);

        [Header("Typewriter")]
        [Tooltip("Reveal story dialogue one character at a time, classic JRPG style. Combat lines stay whole.")]
        [SerializeField] private bool typewriter = true;

        [Tooltip("Typing speed in characters per second. Uses real time, so a paused fight cannot stop it.")]
        [SerializeField] private float typewriterCharsPerSecond = 45f;

        private readonly List<TextMeshProUGUI> liveLines = new List<TextMeshProUGUI>();
        private readonly Queue<TextMeshProUGUI> pool = new Queue<TextMeshProUGUI>();

        /// <summary>True only while the player is physically dragging the feed (set by the drag relay).</summary>
        public bool UserDragging { get; set; }

        /// <summary>Layout group on the content; it owns the real padding and spacing used for the height.</summary>
        private VerticalLayoutGroup stack;

        /// <summary>True when the builder wired content + template; a half-wired feed never writes.</summary>
        private bool wired;

        /// <summary>Lines were added this frame; the content height is recomputed once per frame, not per line.</summary>
        private bool contentDirty;

        /// <summary>The one story line that is still revealing. Older lines are always whole.</summary>
        private TextMeshProUGUI typingLabel;

        private BattleLogEntry typingEntry;
        private int typingChars;
        private float typingCarry;
        private bool typingActive;

        /// <summary>History redraws must not replay the effect on an old line.</summary>
        private bool suppressTyping;

        // Story mode: one line at a time, normal feed hidden, prompt at the bottom.
        private GameObject storyPanel;
        private TextMeshProUGUI storyText;
        private TextMeshProUGUI storyPrompt;
        private BattleLogEntry storyEntry;
        private bool storyMode;
        private bool storyWaiting;
        private bool storyTyping;
        private int storyChars;
        private float storyCarry;

        private void Awake()
        {
            wired = lineTemplate != null && content != null;

            if (!wired)
            {
                // Never disable the feed: log loudly, but keep the component alive so a late wire-up still works.
                Debug.LogError("[CombatLogUI] lineTemplate/content not assigned; the feed shows nothing until wired.");
                return;
            }

            lineTemplate.gameObject.SetActive(false);
            stack = content.GetComponent<VerticalLayoutGroup>();

            for (int i = 0; i < Mathf.Max(1, poolSize); i++)
            {
                pool.Enqueue(CreateLine());
            }

            BuildStoryPanel();
        }

        private void BuildStoryPanel()
        {
            RectTransform panel = UiRuntime.CreateNode("StoryMode", transform).GetComponent<RectTransform>();
            storyPanel = panel.gameObject;
            UiRuntime.Stretch(panel, UiSpace.Medium, UiSpace.Small, UiSpace.Medium, UiSpace.Base);

            storyText = UiRuntime.CreateText(panel, "Line", string.Empty,
                UiType.Role.Dialogue, TextAlignmentOptions.TopLeft, UiTheme.Text);
            storyText.textWrappingMode = TextWrappingModes.Normal;
            storyText.richText = true;
            UiRuntime.Anchor(storyText.rectTransform, new Vector2(0f, 0.18f), Vector2.one);

            storyPrompt = UiRuntime.CreateText(panel, "Prompt", "TAP TO CONTINUE",
                UiType.Role.Small, TextAlignmentOptions.Center, UiTheme.AccentLight);
            UiRuntime.Anchor(storyPrompt.rectTransform, Vector2.zero, new Vector2(1f, 0.16f));
            storyPrompt.gameObject.SetActive(false);

            storyPanel.SetActive(false);
        }

        /// <summary>
        /// The view keeps NO history. It subscribes while visible and redraws the window from
        /// <see cref="BattleLog"/> every time it comes back, so a hidden page cannot lose or freeze the feed.
        /// </summary>
        private void OnEnable()
        {
            BattleLog.Appended += OnAppended;
            BattleLog.Cleared += OnCleared;

            if (storyMode)
            {
                if (storyPanel != null)
                {
                    storyPanel.SetActive(true);
                }

                if (scrollRect != null)
                {
                    scrollRect.gameObject.SetActive(false);
                }

                return;
            }

            Rebuild();
        }

        private void OnDisable()
        {
            BattleLog.Appended -= OnAppended;
            BattleLog.Cleared -= OnCleared;
            StopTyping();
            StopStoryTyping();
        }

        /// <summary>Redraws the window from the store: oldest first, newest last.</summary>
        private void Rebuild()
        {
            if (!wired)
            {
                return;
            }

            ClearLines();

            IReadOnlyList<BattleLogEntry> entries = BattleLog.Entries;
            int first = Mathf.Max(0, entries.Count - Mathf.Max(1, maxLines));

            suppressTyping = true;

            for (int i = first; i < entries.Count; i++)
            {
                // Story dialogue is a scene view, not feed history. Keep it out of the normal log
                // even after the scene ends; the store still holds the entry.
                if (entries[i].Kind == BattleLogKind.Dialogue)
                {
                    continue;
                }

                Append(entries[i]);
            }

            suppressTyping = false;
        }

        private void OnAppended(BattleLogEntry entry)
        {
            if (storyMode)
            {
                if (entry.Kind == BattleLogKind.Dialogue)
                {
                    ShowStory(entry);
                }

                return;
            }

            Append(entry);
        }

        private void OnCleared()
        {
            ClearLines();
        }

        /// <summary>
        /// Follow runs after the layout pass: the content height and the child positions are final by then, so the
        /// newest line is pinned without fighting the ScrollRect (no clamping war, no self-pausing feed).
        /// </summary>
        private void LateUpdate()
        {
            if (storyMode)
            {
                AdvanceStoryTyping();
                UpdateStoryPrompt();
                return;
            }

            AdvanceTyping();

            if (contentDirty)
            {
                RefreshContentSize();
                contentDirty = false;

                if (content != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                }
            }

            FollowNewest();
        }

        /// <summary>
        /// Pins the newest line into view. Only a real drag pauses this - the ScrollRect nudging the content
        /// itself (clamping) is never mistaken for the player.
        /// </summary>
        private void FollowNewest()
        {
            if (scrollRect == null || content == null || scrollRect.viewport == null || UserDragging)
            {
                return;
            }

            float overflow = content.rect.height - scrollRect.viewport.rect.height;

            if (overflow <= 0f || content.anchoredPosition.y >= overflow - 0.5f)
            {
                return;
            }

            scrollRect.verticalNormalizedPosition = 0f;
        }

        // ------------------------------------------------------------------
        // Line plumbing
        // ------------------------------------------------------------------
        /// <summary>Pins the feed to its newest line. Used while the log box changes height.</summary>
        public void PinToBottom()
        {
            if (scrollRect == null || UserDragging)
            {
                return;
            }

            scrollRect.verticalNormalizedPosition = 0f;
        }

        /// <summary>Adds one line. No budget, no merging - every entry the store holds becomes a visible line.</summary>
        private void Append(BattleLogEntry entry)
        {
            if (!wired)
            {
                return;
            }

            TextMeshProUGUI label = Rent();

            // A recycled label keeps its old place in the hierarchy, and the layout group draws by that place.
            // Put it at the end, or the newest line would surface at the TOP and the bottom would look frozen.
            label.transform.SetAsLastSibling();
            label.gameObject.SetActive(true);
            label.color = ColorFor(entry.Kind);
            label.richText = true;
            label.textWrappingMode = TextWrappingModes.Normal;
            UiType.Apply(label, UiType.Role.Dialogue);

            // A story line starts with the speaker only and reveals its words frame by frame. Any older
            // half-typed line finishes first, so the player never loses a sentence.
            CompleteTyping();

            bool shouldType = typewriter && !suppressTyping &&
                              entry.Kind == BattleLogKind.Dialogue &&
                              !string.IsNullOrEmpty(entry.Text);

            if (shouldType)
            {
                typingLabel = label;
                typingEntry = entry;
                typingChars = 0;
                typingCarry = 0f;
                typingActive = true;
                label.SetText(DisplayText(entry, string.Empty));
            }
            else
            {
                label.SetText(DisplayText(entry));
            }

            ApplyPreferredHeight(label);
            liveLines.Add(label);

            TrimToMax();
            contentDirty = true;
        }

        /// <summary>
        /// Builds the visible string. The speaker name gets its own gold colour; the words keep the line colour.
        /// TMP ignores rich-text tags when it measures, so wrapped heights still fit the real text.
        /// </summary>
        private string DisplayText(BattleLogEntry entry)
        {
            return DisplayText(entry, entry.Text);
        }

        private string DisplayText(BattleLogEntry entry, string body)
        {
            return FormatLine(entry.Speaker, body);
        }

        private string FormatLine(string speaker, string body)
        {
            if (body == null)
            {
                body = string.Empty;
            }

            if (string.IsNullOrEmpty(speaker))
            {
                return body;
            }

            string gold = ColorUtility.ToHtmlStringRGB(UiTheme.AccentLight);
            return $"<color=#{gold}>{speaker}</color>: {body}";
        }

        /// <summary>Hides the normal feed and shows the one-line story view in the same log box.</summary>
        public void BeginStory()
        {
            if (!wired)
            {
                return;
            }

            storyMode = true;
            storyWaiting = false;
            storyTyping = false;
            storyChars = 0;
            storyCarry = 0f;
            storyEntry = default;

            StopTyping();

            if (storyPanel != null)
            {
                storyPanel.SetActive(true);
            }

            if (scrollRect != null)
            {
                scrollRect.gameObject.SetActive(false);
            }

            if (storyText != null)
            {
                storyText.SetText(string.Empty);
                storyText.maxVisibleCharacters = 0;
            }

            if (storyPrompt != null)
            {
                storyPrompt.gameObject.SetActive(false);
            }
        }

        /// <summary>Puts the normal feed back. Story lines stay out of the feed.</summary>
        public void EndStory()
        {
            if (!storyMode)
            {
                return;
            }

            storyMode = false;
            storyWaiting = false;
            StopStoryTyping();

            if (storyPanel != null)
            {
                storyPanel.SetActive(false);
            }

            if (scrollRect != null)
            {
                scrollRect.gameObject.SetActive(true);
            }

            Rebuild();
        }

        /// <summary>Shows or hides the tap prompt for the current line.</summary>
        public void SetStoryWaiting(bool waiting)
        {
            if (!storyMode)
            {
                return;
            }

            storyWaiting = waiting;
            UpdateStoryPrompt();
        }

        /// <summary>
        /// Finishes the current story line. Returns true when it actually completed typing, so the
        /// first tap can finish the text and the second tap can advance.
        /// </summary>
        public bool CompleteStoryTyping()
        {
            if (!storyMode || !storyTyping)
            {
                return false;
            }

            if (storyText != null)
            {
                storyText.maxVisibleCharacters = int.MaxValue;
            }

            storyTyping = false;
            UpdateStoryPrompt();
            return true;
        }

        private void ShowStory(BattleLogEntry entry)
        {
            storyEntry = entry;
            storyWaiting = false;
            storyChars = 0;
            storyCarry = 0f;

            string body = entry.Text ?? string.Empty;
            storyTyping = typewriter && typewriterCharsPerSecond > 0f && body.Length > 0;

            if (storyText != null)
            {
                storyText.color = ColorFor(BattleLogKind.Dialogue);

                // Lay out the complete sentence once. Wrapping then uses whole words and never
                // reflows when the next character arrives.
                storyText.SetText(DisplayText(entry));
                storyText.maxVisibleCharacters = storyTyping
                    ? StoryPrefixCharacters(entry)
                    : int.MaxValue;
            }

            UpdateStoryPrompt();
        }

        private int StoryPrefixCharacters(BattleLogEntry entry)
        {
            return string.IsNullOrEmpty(entry.Speaker) ? 0 : entry.Speaker.Length + 2;
        }

        private void AdvanceStoryTyping()
        {
            if (!storyTyping || storyText == null)
            {
                return;
            }

            string body = storyEntry.Text ?? string.Empty;

            if (!typewriter || typewriterCharsPerSecond <= 0f || body.Length == 0)
            {
                CompleteStoryTyping();
                return;
            }

            storyCarry += typewriterCharsPerSecond * Time.unscaledDeltaTime;
            int shown = storyChars + Mathf.FloorToInt(storyCarry);

            if (shown >= body.Length)
            {
                CompleteStoryTyping();
                return;
            }

            if (shown <= storyChars)
            {
                return;
            }

            storyCarry -= shown - storyChars;
            storyChars = shown;
            storyText.maxVisibleCharacters = StoryPrefixCharacters(storyEntry) + storyChars;
        }

        private void UpdateStoryPrompt()
        {
            if (storyPrompt == null)
            {
                return;
            }

            bool show = storyMode && storyWaiting;
            storyPrompt.gameObject.SetActive(show);

            if (!show)
            {
                return;
            }

            storyPrompt.SetText("TAP TO CONTINUE");
        }

        private void StopStoryTyping()
        {
            storyTyping = false;
            storyChars = 0;
            storyCarry = 0f;
        }

        private void AdvanceTyping()
        {
            if (!typingActive)
            {
                return;
            }

            if (typingLabel == null || !typingLabel.gameObject.activeInHierarchy)
            {
                StopTyping();
                return;
            }

            string body = typingEntry.Text ?? string.Empty;

            if (!typewriter || typewriterCharsPerSecond <= 0f || body.Length == 0)
            {
                CompleteTyping();
                return;
            }

            typingCarry += typewriterCharsPerSecond * Time.unscaledDeltaTime;
            int shown = typingChars + Mathf.FloorToInt(typingCarry);

            if (shown >= body.Length)
            {
                CompleteTyping();
                return;
            }

            if (shown <= typingChars)
            {
                return;
            }

            typingCarry -= shown - typingChars;
            typingChars = shown;

            float before = PreferredHeight(typingLabel);
            typingLabel.SetText(DisplayText(typingEntry, body.Substring(0, typingChars)));
            float after = ApplyPreferredHeight(typingLabel);

            if (Mathf.Abs(after - before) > 0.5f)
            {
                contentDirty = true;
            }
        }

        /// <summary>Shows the whole current story line at once. Used when a scene ends or is skipped.</summary>
        public void CompleteTyping()
        {
            if (!typingActive)
            {
                return;
            }

            if (typingLabel != null)
            {
                typingLabel.SetText(DisplayText(typingEntry));
                ApplyPreferredHeight(typingLabel);
            }

            StopTyping();
            contentDirty = true;
        }

        private void StopTyping()
        {
            typingActive = false;
            typingLabel = null;
            typingChars = 0;
            typingCarry = 0f;
        }

        private float PreferredHeight(TextMeshProUGUI label)
        {
            LayoutElement element = label != null ? label.GetComponent<LayoutElement>() : null;
            return element != null ? element.preferredHeight : lineHeight;
        }

        private Color ColorFor(BattleLogKind kind)
        {
            switch (kind)
            {
                case BattleLogKind.Critical:
                    return criticalColor;
                case BattleLogKind.Incoming:
                case BattleLogKind.Defeat:
                    return incomingColor;
                case BattleLogKind.EnemyKilled:
                case BattleLogKind.Gold:
                    return goldColor;
                case BattleLogKind.HeroDied:
                    return killColor;
                case BattleLogKind.Ability:
                case BattleLogKind.System:
                    return eventColor;
                case BattleLogKind.Dialogue:
                    return heroHitColor;
                case BattleLogKind.EnemyAbility:
                    return enemyAbilityColor;
                case BattleLogKind.Heal:
                    return healColor;
                case BattleLogKind.Guard:
                    return guardColor;
                default:
                    return heroHitColor;
            }
        }

    }
}
