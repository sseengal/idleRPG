using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// Live battle feed ("Mage hits Ogre for 24").
    ///
    /// ONE RULE: every action the game raises becomes its own line. Nothing is aggregated, throttled or
    /// dropped. The visible list is a fixed-length scrollback (oldest lines are recycled) purely so a long
    /// idle session cannot grow label memory without bound - the logging itself is total.
    ///
    /// The feed subscribes once for the component's lifetime (Awake/OnDestroy) so hiding the battle page can
    /// never make it miss events, and it follows the newest line unless the player is genuinely dragging.
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

        [Tooltip("Line height in reference pixels.")]
        [SerializeField] private float lineHeight = 34f;

        [Tooltip("Vertical gap between lines.")]
        [SerializeField] private float lineSpacing = 2f;

        [Tooltip("Padding above and below the list.")]
        [SerializeField] private float contentPadding = 12f;

        [Header("Colours")]
        [SerializeField] private Color heroHitColor = new Color(0.95f, 0.95f, 1f, 1f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color incomingColor = new Color(1f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color killColor = new Color(0.55f, 0.95f, 0.6f, 1f);
        [SerializeField] private Color eventColor = new Color(0.70f, 0.80f, 1f, 1f);
        [SerializeField] private Color goldColor = new Color(1f, 0.85f, 0.45f, 1f);

        private readonly List<TextMeshProUGUI> liveLines = new List<TextMeshProUGUI>();
        private readonly Queue<TextMeshProUGUI> pool = new Queue<TextMeshProUGUI>();

        /// <summary>True only while the player is physically dragging the feed (set by the drag relay).</summary>
        public bool UserDragging { get; set; }

        /// <summary>Lines were added this frame; the content height is recomputed once per frame, not per line.</summary>
        private bool contentDirty;

        private void Awake()
        {
            if (lineTemplate == null || content == null)
            {
                // Never disable the feed: log loudly, but keep the component alive so a late wire-up still works.
                Debug.LogError("[CombatLogUI] lineTemplate/content not assigned; the feed shows nothing until wired.");
            }
            else
            {
                lineTemplate.gameObject.SetActive(false);

                for (int i = 0; i < Mathf.Max(1, poolSize); i++)
                {
                    pool.Enqueue(CreateLine());
                }
            }

            // Subscribe once for the whole lifetime. A hidden battle page must not lose events.
            GameEvents.EnemyDamaged += OnEnemyDamaged;
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.HeroDamaged += OnHeroDamaged;
            GameEvents.HeroDied += OnHeroDied;
            GameEvents.AscensionCompleted += OnAscensionCompleted;
            GameEvents.UpgradePurchased += OnUpgradePurchased;
            GameEvents.OfflineRewardsClaimed += OnOfflineRewardsClaimed;
            GameEvents.CombatMessage += OnCombatMessage;
            GameEvents.SaveLoaded += OnSaveLoaded;
        }

        private void OnDestroy()
        {
            GameEvents.EnemyDamaged -= OnEnemyDamaged;
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.HeroDamaged -= OnHeroDamaged;
            GameEvents.HeroDied -= OnHeroDied;
            GameEvents.AscensionCompleted -= OnAscensionCompleted;
            GameEvents.UpgradePurchased -= OnUpgradePurchased;
            GameEvents.OfflineRewardsClaimed -= OnOfflineRewardsClaimed;
            GameEvents.CombatMessage -= OnCombatMessage;
            GameEvents.SaveLoaded -= OnSaveLoaded;
        }

        private void Update()
        {
            if (contentDirty)
            {
                RefreshContentSize();
                contentDirty = false;
            }

            FollowNewest();
        }

        /// <summary>
        /// Pins the newest line into view. Only a real drag pauses this - the ScrollRect nudging the content
        /// itself (clamping, momentum) is never mistaken for the player.
        /// </summary>
        private void FollowNewest()
        {
            if (scrollRect == null || content == null || UserDragging)
            {
                return;
            }

            content.anchoredPosition = new Vector2(content.anchoredPosition.x, OverflowY());
        }

        /// <summary>How far the content can scroll; 0 when everything already fits.</summary>
        private float OverflowY()
        {
            if (scrollRect == null || scrollRect.viewport == null)
            {
                return 0f;
            }

            float overflow = content.rect.height - scrollRect.viewport.rect.height;
            return overflow > 0f ? overflow : 0f;
        }

        // ------------------------------------------------------------------
        // Line plumbing
        // ------------------------------------------------------------------
        /// <summary>Adds one line. No budget, no merging - every call writes a line.</summary>
        private void Append(string text, Color color)
        {
            if (lineTemplate == null || content == null)
            {
                return;
            }

            TextMeshProUGUI label = Rent();
            label.gameObject.SetActive(true);
            label.color = color;
            label.SetText(text);
            liveLines.Add(label);

            TrimToMax();
            contentDirty = true;
        }

        // ------------------------------------------------------------------
        // Name resolution (falls back to generic labels when data is unavailable)
        // ------------------------------------------------------------------
        private static string HeroName(int heroIndex)
        {
            HudController hud = HudController.Instance;
            GameManager manager = hud != null ? hud.GameManager : null;
            PartyConfig party = manager != null ? manager.Party : null;

            if (party == null || heroIndex < 0)
            {
                return "The party";
            }

            HeroData hero = party.GetHero(heroIndex);
            return hero != null ? hero.HeroName : string.Format("Hero {0}", heroIndex + 1);
        }

        /// <summary>
        /// Name of one enemy. When the wave holds several of the same archetype the slots are suffixed
        /// A/B/C, so "Goblin" x3 reads as Goblin A, Goblin B, Goblin C in the log.
        /// </summary>
        private static string EnemyName(int enemyIndex = 0)
        {
            HudController hud = HudController.Instance;
            GameManager manager = hud != null ? hud.GameManager : null;

            if (manager == null || manager.Combat == null || manager.Combat.Simulator == null)
            {
                return "the enemy";
            }

            var enemies = manager.Combat.Simulator.Enemies;

            if (enemies == null || enemies.Length == 0)
            {
                return "the enemy";
            }

            int index = Mathf.Clamp(enemyIndex, 0, enemies.Length - 1);
            var enemy = enemies[index];

            if (enemy == null)
            {
                return "the enemy";
            }

            return enemies.Length > 1 ? string.Format("{0} {1}", enemy.DisplayName, (char)('A' + index)) : enemy.DisplayName;
        }
    }
}
