using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// Live battle feed ("Mage hits Ogre for 24"). Reads the combat events, aggregates rapid
    /// repeats from the same attacker and keeps a bounded, scrollable ring of lines.
    /// Labels are pooled: nothing is instantiated per hit.
    /// </summary>
    public sealed class CombatLogUI : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform content;
        [SerializeField] private TextMeshProUGUI lineTemplate;

        [Header("Limits")]
        [Tooltip("Maximum lines kept on screen.")]
        [SerializeField] private int maxLines = 60;

        [Tooltip("Labels created up front.")]
        [SerializeField] private int poolSize = 24;

        [Tooltip("New lines allowed per second; extras are summarised.")]
        [SerializeField] private int maxLinesPerSecond = 3;

        [Tooltip("Repeated hits from the same attacker within this window merge into one line.")]
        [SerializeField] private float aggregateWindowSec = 0.35f;

        [Tooltip("Line height in reference pixels.")]
        [SerializeField] private float lineHeight = 34f;

        [Tooltip("Vertical gap between lines.")]
        [SerializeField] private float lineSpacing = 2f;

        [Tooltip("Padding above and below the list.")]
        [SerializeField] private float contentPadding = 12f;

        [Header("Bottom follow")]
        [Tooltip("Slide new lines in smoothly instead of snapping to the bottom.")]
        [SerializeField] private bool smoothFollow = true;

        [Tooltip("How quickly the feed settles on the newest line.")]
        [SerializeField] private float followSpeed = 10f;

        [Tooltip("Distance from the bottom (px) at which auto-follow re-engages.")]
        [SerializeField] private float reengageDistance = 32f;

        [Header("Colours")]
        [SerializeField] private Color heroHitColor = new Color(0.95f, 0.95f, 1f, 1f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color incomingColor = new Color(1f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color killColor = new Color(0.55f, 0.95f, 0.6f, 1f);
        [SerializeField] private Color eventColor = new Color(0.70f, 0.80f, 1f, 1f);
        [SerializeField] private Color goldColor = new Color(1f, 0.85f, 0.45f, 1f);

        private readonly List<TextMeshProUGUI> liveLines = new List<TextMeshProUGUI>();
        private readonly Queue<TextMeshProUGUI> pool = new Queue<TextMeshProUGUI>();

        private float lineBudget;
        private int droppedLines;
        private string aggregateKey = string.Empty;
        private double aggregateDamage;
        private int aggregateCount;
        private float aggregateAge;
        private TextMeshProUGUI aggregateLabel;
        private bool pinnedToBottom = true;

        /// <summary>Scroll position we wrote last: anything else moving the content is the player.</summary>
        private float lastAppliedY = -1f;

        private void Awake()
        {
            if (lineTemplate == null || content == null)
            {
                Debug.LogError("[CombatLogUI] lineTemplate/content not assigned; log disabled.");
                enabled = false;
                return;
            }

            lineTemplate.gameObject.SetActive(false);

            for (int i = 0; i < Mathf.Max(1, poolSize); i++)
            {
                pool.Enqueue(CreateLine());
            }
        }

        private void Update()
        {
            lineBudget = Mathf.Min(lineBudget + maxLinesPerSecond * Time.deltaTime, maxLinesPerSecond);

            if (aggregateCount > 0)
            {
                aggregateAge += Time.deltaTime;

                if (aggregateAge > aggregateWindowSec)
                {
                    CloseAggregate();
                }
            }

            if (droppedLines > 0 && lineBudget >= 1f)
            {
                int summarized = droppedLines;
                droppedLines = 0;
                Append(string.Format("... {0} more hit{1}", summarized, summarized == 1 ? string.Empty : "s"), eventColor);
            }

            FollowNewest(Time.deltaTime);
        }

        /// <summary>
        /// Keeps the newest line in view. Auto-follow pauses while the player scrolls back through
        /// history and re-engages as soon as they return to the bottom.
        /// </summary>
        /// <summary>
        /// Keeps the newest line in view. The content grows every time a line is added, so growth
        /// must NOT be mistaken for the player scrolling: only a position we did not write counts as
        /// user input. Scrolling up pauses the follow until they come back to the bottom.
        /// </summary>
        private void FollowNewest(float deltaTime)
        {
            if (scrollRect == null || content == null)
            {
                return;
            }

            float target = OverflowY();
            float currentY = content.anchoredPosition.y;

            if (lastAppliedY >= 0f && Mathf.Abs(currentY - lastAppliedY) > 0.5f)
            {
                // Something other than us moved the feed: a drag, a wheel scroll or the scrollbar.
                pinnedToBottom = target - currentY <= reengageDistance;
            }

            if (!pinnedToBottom)
            {
                if (target - currentY <= reengageDistance)
                {
                    pinnedToBottom = true;
                }
                else
                {
                    lastAppliedY = currentY;
                    return;
                }
            }

            float y = smoothFollow
                ? Mathf.Lerp(currentY, target, Mathf.Clamp01(followSpeed * deltaTime))
                : target;

            if (Mathf.Abs(target - y) < 0.5f)
            {
                y = target;
            }

            content.anchoredPosition = new Vector2(content.anchoredPosition.x, y);
            lastAppliedY = y;
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
        // Event handlers
        // ------------------------------------------------------------------
        private void OnEnable()
        {
            GameEvents.EnemySpawned += OnEnemySpawned;
            GameEvents.EnemyDamaged += OnEnemyDamaged;
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.HeroDamaged += OnHeroDamaged;
            GameEvents.HeroDied += OnHeroDied;
            GameEvents.WaveCompleted += OnWaveCompleted;
            GameEvents.PartyWiped += OnPartyWiped;
            GameEvents.BossFailed += OnBossFailed;
            GameEvents.AscensionCompleted += OnAscensionCompleted;
            GameEvents.UpgradePurchased += OnUpgradePurchased;
            GameEvents.OfflineRewardsClaimed += OnOfflineRewardsClaimed;
        }

        private void OnDisable()
        {
            GameEvents.EnemySpawned -= OnEnemySpawned;
            GameEvents.EnemyDamaged -= OnEnemyDamaged;
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.HeroDamaged -= OnHeroDamaged;
            GameEvents.HeroDied -= OnHeroDied;
            GameEvents.WaveCompleted -= OnWaveCompleted;
            GameEvents.PartyWiped -= OnPartyWiped;
            GameEvents.BossFailed -= OnBossFailed;
            GameEvents.AscensionCompleted -= OnAscensionCompleted;
            GameEvents.UpgradePurchased -= OnUpgradePurchased;
            GameEvents.OfflineRewardsClaimed -= OnOfflineRewardsClaimed;
        }

        private void OnEnemySpawned(string enemyName, double maxHealth, bool isBoss, int enemyIndex)
        {
            // A wave spawns its enemies one event at a time. The log writes ONE line per wave (on the first
            // enemy) so a 3-enemy wave cannot flood the feed, and it always closes a pending aggregate so a
            // line can never merge across a wave boundary.
            CloseAggregate();

            if (enemyIndex != 0)
            {
                return;
            }

            HudController hud = HudController.Instance;
            GameManager manager = hud != null ? hud.GameManager : null;
            int wave = manager != null && manager.Combat != null ? manager.Combat.CurrentWave : 0;

            Append(string.Format("-- Wave {0}: {1}{2} --",
                wave, WaveSummary(), isBoss ? " (BOSS)" : string.Empty), eventColor);
        }

        /// <summary>
        /// Roster of the wave in plain words, duplicates collapsed: "Goblin x2, Slime".
        /// Reads the live wave so it always describes what is actually standing there.
        /// </summary>
        private static string WaveSummary()
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

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null || !IsFirstOfName(enemies, i))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                int count = CountOfName(enemies, enemies[i].DisplayName);
                builder.Append(count > 1
                    ? string.Format("{0} x{1}", enemies[i].DisplayName, count)
                    : enemies[i].DisplayName);
            }

            return builder.Length > 0 ? builder.ToString() : "the enemy";
        }

        private static bool IsFirstOfName(Sim.Combatant[] enemies, int index)
        {
            for (int j = 0; j < index; j++)
            {
                if (enemies[j] != null && enemies[j].DisplayName == enemies[index].DisplayName)
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountOfName(Sim.Combatant[] enemies, string name)
        {
            int count = 0;

            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null && enemies[i].DisplayName == name)
                {
                    count++;
                }
            }

            return count;
        }

        private void OnEnemyDamaged(EnemyDamagedInfo info)
        {
            string attacker = HeroName(info.AttackerIndex);
            string target = EnemyName(info.EnemyIndex);
            string key = string.Format("enemy:{0}:{1}", info.AttackerIndex, info.IsCritical);

            AddOrAggregate(key, info.Damage, info.IsCritical ? criticalColor : heroHitColor,
                count => info.IsCritical
                    ? string.Format("{0} CRITS {1} for {2}", attacker, target, NumberFormatter.Format(info.Damage))
                    : string.Format("{0} hits {1} for {2}{3}", attacker, target,
                        NumberFormatter.Format(info.Damage), count > 1 ? string.Format(" x{0}", count) : string.Empty));
        }

        private void OnHeroDamaged(int heroIndex, double damage, double currentHealth, double maxHealth, int attackerEnemyIndex)
        {
            // Keyed per (hero, enemy): hits from different enemies must never merge into one line, and the
            // attacker index is what names the enemy that actually swung.
            string attacker = EnemyName(attackerEnemyIndex);
            string target = HeroName(heroIndex);
            string key = string.Format("hero:{0}:{1}", heroIndex, attackerEnemyIndex);

            AddOrAggregate(key, damage, incomingColor,
                count => string.Format("{0} hits {1} for {2}  ({3}/{4}){5}", attacker, target,
                    NumberFormatter.Format(damage),
                    NumberFormatter.Format(currentHealth),
                    NumberFormatter.Format(maxHealth),
                    count > 1 ? string.Format(" x{0}", count) : string.Empty));
        }

        private void OnEnemyKilled(string enemyName, double goldReward, int enemyIndex)
        {
            Append(string.Format("{0} defeated  +{1} gold", enemyName, NumberFormatter.Format(goldReward)), goldColor);
        }

        private void OnHeroDied(int heroIndex)
        {
            Append(string.Format("{0} has fallen", HeroName(heroIndex)), killColor);
        }

        private void OnWaveCompleted(int stage, int wave)
        {
            Append(string.Format("Stage {0} wave {1} cleared", stage, wave), eventColor);
        }

        private void OnPartyWiped()
        {
            Append("Party wiped - falling back a stage", incomingColor);
        }

        private void OnBossFailed()
        {
            Append("Boss encounter failed", incomingColor);
        }

        private void OnAscensionCompleted(double tokensEarned, int newHighestStage)
        {
            Append(string.Format("Ascended - +{0} token(s), best stage {1}",
                NumberFormatter.Format(tokensEarned), newHighestStage), goldColor);
        }

        private void OnUpgradePurchased(int heroIndex, HeroStatType statType, int newLevel, double goldCost)
        {
            Append(string.Format("{0} {1} -> Lv {2}  (-{3} gold)", HeroName(heroIndex),
                statType.ToDisplayName(), newLevel, NumberFormatter.Format(goldCost)), eventColor);
        }

        private void OnOfflineRewardsClaimed(double gold)
        {
            Append(string.Format("Offline earnings claimed  +{0} gold", NumberFormatter.Format(gold)), goldColor);
        }

        // ------------------------------------------------------------------
        // Line plumbing
        // ------------------------------------------------------------------
        /// <summary>Adds a line, honouring the per-second budget (extras are summarised later).</summary>
        private void Append(string text, Color color)
        {
            if (lineBudget < 1f)
            {
                droppedLines++;
                return;
            }

            lineBudget -= 1f;

            // A new line ends the current aggregate run.
            CloseAggregate();

            TextMeshProUGUI label = Rent();
            label.gameObject.SetActive(true);
            label.color = color;
            label.SetText(text);
            liveLines.Add(label);

            TrimToMax();
            RefreshContentSize();
        }

        /// <summary>
        /// Merges repeated hits from the same attacker into the last line while the window is
        /// open, otherwise appends a fresh line. Keeps the feed readable during fast fights.
        /// </summary>
        private void AddOrAggregate(string key, double damage, Color color, System.Func<int, string> format)
        {
            if (aggregateCount > 0 && aggregateKey == key && aggregateLabel != null && aggregateAge <= aggregateWindowSec)
            {
                aggregateCount++;
                aggregateDamage += damage;
                aggregateAge = 0f;
                aggregateLabel.SetText(format(aggregateCount));
                return;
            }

            if (lineBudget < 1f)
            {
                droppedLines++;
                return;
            }

            lineBudget -= 1f;
            CloseAggregate();

            TextMeshProUGUI label = Rent();
            label.gameObject.SetActive(true);
            label.color = color;
            label.SetText(format(1));
            liveLines.Add(label);

            aggregateKey = key;
            aggregateDamage = damage;
            aggregateCount = 1;
            aggregateAge = 0f;
            aggregateLabel = label;

            TrimToMax();
            RefreshContentSize();
        }

        private void CloseAggregate()
        {
            aggregateCount = 0;
            aggregateDamage = 0d;
            aggregateAge = 0f;
            aggregateLabel = null;
            aggregateKey = string.Empty;
        }

        private void TrimToMax()
        {
            while (liveLines.Count > Mathf.Max(1, maxLines))
            {
                Return(liveLines[0]);
                liveLines.RemoveAt(0);
            }
        }

        private TextMeshProUGUI Rent()
        {
            while (pool.Count > 0)
            {
                TextMeshProUGUI candidate = pool.Dequeue();
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return CreateLine();
        }

        private void Return(TextMeshProUGUI label)
        {
            if (label == null)
            {
                return;
            }

            label.gameObject.SetActive(false);
            pool.Enqueue(label);

            if (aggregateLabel == label)
            {
                CloseAggregate();
            }
        }

        private TextMeshProUGUI CreateLine()
        {
            TextMeshProUGUI label = Instantiate(lineTemplate, content);
            label.rectTransform.localScale = Vector3.one;

            LayoutElement element = label.gameObject.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = label.gameObject.AddComponent<LayoutElement>();
            }

            element.minHeight = lineHeight;
            element.preferredHeight = lineHeight;
            label.gameObject.SetActive(false);

            return label;
        }

        /// <summary>
        /// Height is computed explicitly: the list grows line by line and a ContentSizeFitter would
        /// need an extra layout pass to keep up. The scroll position is handled by FollowNewest().
        /// </summary>
        private void RefreshContentSize()
        {
            if (content == null)
            {
                return;
            }

            float height = contentPadding * 2f + liveLines.Count * (lineHeight + lineSpacing);
            content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(1f, height));
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