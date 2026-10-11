using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Leaderboard;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// The global ranks sheet: a shared card (<see cref="UiCard"/>) over the battle page with the mock leaderboard rows
    /// (rank, tag, party level, best stage) and the local player's own row highlighted. Runtime-built; opened from the
    /// trophy button on the battle HUD. Rows are informational (not buttons) and pooled, so refreshing while open is
    /// free.
    /// </summary>
    public sealed class LeaderboardSheetUI : MonoBehaviour
    {
        private const int TopRows = 12;

        private readonly Color dimText = new Color(1f, 1f, 1f, 0.55f);
        private readonly Color youHighlight = new Color(1f, 0.82f, 0.3f, 1f);

        private GameManager manager;
        private RectTransform rowsRoot;
        private TextMeshProUGUI subtitleLabel;

        /// <summary>
        /// Pooled rows: built once, only filled afterwards. Rows used to be destroyed and recreated on every refresh,
        /// which stacked duplicates whenever two refreshes landed in one frame, and cost allocations while the sheet
        /// was open (UI-UX §1: "pool anything spawned").
        /// </summary>
        private GameObject[] rowSlots;
        private TextMeshProUGUI[] rowRanks;
        private TextMeshProUGUI[] rowTags;
        private TextMeshProUGUI[] rowLevels;
        private TextMeshProUGUI[] rowStages;

        private UiCard card;

        public void Build(GameManager gameManager)
        {
            manager = gameManager;

            // The shared card: stretched overlay, scrim, 9-slice face, title and X (see UiCard). It owns the look
            // (UiSkin) and its own show/hide, so this page only fills the content area.
            card = UiCard.Create("LeaderboardCard", "GLOBAL RANKS", new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.88f));
            Transform sheet = card.Content;

            // Same role as the column heads below it: the subtitle is a quiet line above the table, so it must not
            // out-size the table's own headings.
            subtitleLabel = card.CreateBodyText(sheet, "Subtitle", string.Empty, UiType.Role.Small,
                TextAlignmentOptions.MidlineLeft, card.Skin.body);
            UiRuntime.Anchor(subtitleLabel.rectTransform, new Vector2(0f, 0.90f), new Vector2(0.95f, 0.99f));

            TextMeshProUGUI rankHead = card.CreateBodyText(sheet, "ColRank", "#", UiType.Role.Small, TextAlignmentOptions.MidlineLeft, dimText);
            UiRuntime.Anchor(rankHead.rectTransform, new Vector2(0f, 0.84f), new Vector2(0.12f, 0.88f));
            TextMeshProUGUI tagHead = card.CreateBodyText(sheet, "ColTag", "TAG", UiType.Role.Small, TextAlignmentOptions.MidlineLeft, dimText);
            UiRuntime.Anchor(tagHead.rectTransform, new Vector2(0.16f, 0.84f), new Vector2(0.58f, 0.88f));
            TextMeshProUGUI lvlHead = card.CreateBodyText(sheet, "ColLvl", "LVL", UiType.Role.Small, TextAlignmentOptions.MidlineRight, dimText);
            UiRuntime.Anchor(lvlHead.rectTransform, new Vector2(0.60f, 0.84f), new Vector2(0.74f, 0.88f));
            TextMeshProUGUI stageHead = card.CreateBodyText(sheet, "ColStage", "STAGE", UiType.Role.Small, TextAlignmentOptions.MidlineRight, dimText);
            UiRuntime.Anchor(stageHead.rectTransform, new Vector2(0.78f, 0.84f), new Vector2(0.96f, 0.88f));

            GameObject rowsNode = UiRuntime.CreateNode("Rows", sheet);
            rowsRoot = rowsNode.GetComponent<RectTransform>();
            UiRuntime.Anchor(rowsRoot, new Vector2(0f, 0f), new Vector2(1f, 0.82f));
            CreateRowPool();

            Show();
        }

        /// <summary>Rebuilds the board and shows the card.</summary>
        public void Show()
        {
            if (card == null)
            {
                return;
            }

            card.Show();
            RefreshRows();
        }

        private void OnDestroy()
        {
            manager = null;

            // The card lives on the overlay canvas, not under this component, so it would outlive us - and a recreated
            // sheet would leave a second card behind on the modal canvas. Take it with us.
            if (card != null && card.Root != null)
            {
                Destroy(card.Root);
            }

            card = null;
        }

        private void OnEnable()
        {
            GameEvents.StageChanged += OnStageChanged;
        }

        private void OnDisable()
        {
            GameEvents.StageChanged -= OnStageChanged;
        }

        private void OnStageChanged(int stage, int wave, bool isBossWave)
        {
            if (isActiveAndEnabled)
            {
                RefreshRows();
            }
        }

        private void RefreshRows()
        {
            if (manager == null || manager.Leaderboard == null)
            {
                return;
            }

            manager.Leaderboard.Refresh();

            if (subtitleLabel != null)
            {
                int yourRank = 1;
                LeaderboardEntry you = manager.Leaderboard.You;
                for (int i = 0; i < manager.Leaderboard.Ranked.Count; i++)
                {
                    if (manager.Leaderboard.Ranked[i].IsYou)
                    {
                        yourRank = i + 1;
                        break;
                    }
                }

                subtitleLabel.SetText("you are #" + yourRank + " of " + manager.Leaderboard.Ranked.Count);
            }

            if (rowSlots == null)
            {
                return;
            }

            int yourIndex = -1;
            for (int i = 0; i < manager.Leaderboard.Ranked.Count; i++)
            {
                if (manager.Leaderboard.Ranked[i].IsYou)
                {
                    yourIndex = i;
                    break;
                }
            }

            int rows = Mathf.Min(TopRows, manager.Leaderboard.Ranked.Count);
            int slot = 0;

            for (int i = 0; i < rows; i++)
            {
                if (i == yourIndex)
                {
                    continue; // the player row is pinned below the top cut
                }

                FillRow(slot++, i, manager.Leaderboard.Ranked[i], isYou: false);
            }

            if (yourIndex >= rows)
            {
                FillRow(slot++, yourIndex, manager.Leaderboard.You, isYou: true);
            }
            else if (yourIndex >= 0)
            {
                FillRow(slot++, yourIndex, manager.Leaderboard.Ranked[yourIndex], isYou: true);
            }

            for (int i = slot; i < rowSlots.Length; i++)
            {
                rowSlots[i].SetActive(false);
            }
        }

        /// <summary>
        /// Builds every row slot once (up to <see cref="TopRows"/> plus the pinned player row). RefreshRows only fills
        /// them, so the sheet costs nothing to reopen and can never stack duplicate rows.
        /// </summary>
        private void CreateRowPool()
        {
            int count = TopRows + 1;
            rowSlots = new GameObject[count];
            rowRanks = new TextMeshProUGUI[count];
            rowTags = new TextMeshProUGUI[count];
            rowLevels = new TextMeshProUGUI[count];
            rowStages = new TextMeshProUGUI[count];

            for (int i = 0; i < count; i++)
            {
                GameObject slot = UiRuntime.CreateNode("Row" + i, rowsRoot);
                UiRuntime.Stretch((RectTransform)slot.transform);
                rowSlots[i] = slot;

                // Rows start at the very top of their band, i.e. directly under the column heads (the old docked sheet
                // had a taller header area, which left a dead gap here).
                float slotTop = 1f - i * 0.058f;
                float slotBottom = slotTop - 0.048f;

                rowRanks[i] = CreateRowLabel(slot.transform, "R", new Vector2(0f, slotBottom), new Vector2(0.12f, slotTop), TextAlignmentOptions.MidlineLeft);
                rowTags[i] = CreateRowLabel(slot.transform, "Tag", new Vector2(0.16f, slotBottom), new Vector2(0.58f, slotTop), TextAlignmentOptions.MidlineLeft);
                rowLevels[i] = CreateRowLabel(slot.transform, "Lvl", new Vector2(0.60f, slotBottom), new Vector2(0.74f, slotTop), TextAlignmentOptions.MidlineRight);
                rowStages[i] = CreateRowLabel(slot.transform, "Stage", new Vector2(0.78f, slotBottom), new Vector2(0.96f, slotTop), TextAlignmentOptions.MidlineRight);

                slot.SetActive(false);
            }
        }

        private TextMeshProUGUI CreateRowLabel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            TextAlignmentOptions alignment)
        {
            TextMeshProUGUI label = card.CreateBodyText(parent, name, string.Empty, UiType.Role.Sentence, alignment, Color.white);
            UiRuntime.Anchor(label.rectTransform, anchorMin, anchorMax);
            return label;
        }

        /// <summary>
        /// Fills one pooled row. <paramref name="displaySlot"/> is where it sits on screen; <paramref name="rank"/> is
        /// the entry's real rank - the pinned player row shows their true number, not its screen position.
        /// </summary>
        private void FillRow(int displaySlot, int rank, LeaderboardEntry entry, bool isYou)
        {
            if (displaySlot < 0 || displaySlot >= rowSlots.Length)
            {
                return;
            }

            Color color = isYou ? youHighlight : Color.white;

            rowSlots[displaySlot].SetActive(true);
            rowRanks[displaySlot].SetText((rank + 1).ToString());
            rowTags[displaySlot].SetText((isYou ? UiGlyphs.Marker + " " : "") + entry.Tag);
            rowLevels[displaySlot].SetText(entry.PartyLevel.ToString());
            rowStages[displaySlot].SetText(entry.MaxStage.ToString());

            rowRanks[displaySlot].color = color;
            rowTags[displaySlot].color = color;
            rowLevels[displaySlot].color = color;
            rowStages[displaySlot].color = color;
        }
    }
}
