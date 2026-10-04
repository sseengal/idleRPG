using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Leaderboard;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// The global ranks sheet: a docked panel over the battle page with the mock leaderboard rows
    /// (rank, tag, party level, best stage) and the local player's own row highlighted. Runtime-built;
    /// opened from the trophy button on the battle HUD. Rows are informational (not buttons).
    /// </summary>
    public sealed class LeaderboardSheetUI : MonoBehaviour
    {
        private const int TopRows = 12;

        private readonly Color cardColor = new Color(0.10f, 0.12f, 0.18f, 0.98f);
        private readonly Color dimText = new Color(1f, 1f, 1f, 0.55f);
        private readonly Color youHighlight = new Color(1f, 0.82f, 0.3f, 1f);

        private GameManager manager;
        private RectTransform rowsRoot;
        private TextMeshProUGUI headerLabel;

        private UiDockedSheet frame;

        public void Build(GameManager gameManager)
        {
            manager = gameManager;

            frame = UiDockedSheet.Create(transform, "LeaderboardFrame", new Vector2(0.10f, 0.14f), new Vector2(0.90f, 0.88f), cardColor);
            Transform sheet = frame.Content;

            headerLabel = UiRuntime.CreateText(sheet, "Header", string.Empty, 18f,
                TextAlignmentOptions.MidlineLeft, Color.white);
            UiRuntime.Anchor(headerLabel.rectTransform, new Vector2(0.05f, 0.90f), new Vector2(0.80f, 0.97f), 0f, 0f, 0f, 0f);

            TextMeshProUGUI rankHead = UiRuntime.CreateText(sheet, "ColRank", "#", 13f, TextAlignmentOptions.MidlineLeft, dimText);
            UiRuntime.Anchor(rankHead.rectTransform, new Vector2(0.05f, 0.86f), new Vector2(0.16f, 0.89f), 0f, 0f, 0f, 0f);
            TextMeshProUGUI tagHead = UiRuntime.CreateText(sheet, "ColTag", "TAG", 13f, TextAlignmentOptions.MidlineLeft, dimText);
            UiRuntime.Anchor(tagHead.rectTransform, new Vector2(0.20f, 0.86f), new Vector2(0.55f, 0.89f), 0f, 0f, 0f, 0f);
            TextMeshProUGUI lvlHead = UiRuntime.CreateText(sheet, "ColLvl", "LVL", 13f, TextAlignmentOptions.MidlineRight, dimText);
            UiRuntime.Anchor(lvlHead.rectTransform, new Vector2(0.60f, 0.86f), new Vector2(0.73f, 0.89f), 0f, 0f, 0f, 0f);
            TextMeshProUGUI stageHead = UiRuntime.CreateText(sheet, "ColStage", "STAGE", 13f, TextAlignmentOptions.MidlineRight, dimText);
            UiRuntime.Anchor(stageHead.rectTransform, new Vector2(0.78f, 0.86f), new Vector2(0.95f, 0.89f), 0f, 0f, 0f, 0f);

            GameObject rowsNode = UiRuntime.CreateNode("Rows", sheet);
            rowsRoot = rowsNode.GetComponent<RectTransform>();
            UiRuntime.Anchor(rowsRoot, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.85f));

            Show();
        }

        /// <summary>Rebuilds the board and shows the sheet.</summary>
        public void Show()
        {
            gameObject.SetActive(true);
            RefreshRows();
        }

        private void OnDestroy()
        {
            manager = null;
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

            if (headerLabel != null)
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

                headerLabel.SetText("GLOBAL RANKS   ·   you are #" + yourRank + " of " + manager.Leaderboard.Ranked.Count);
            }

            if (rowsRoot == null)
            {
                return;
            }

            for (int i = rowsRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(rowsRoot.GetChild(i).gameObject);
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
            StringBuilder builder = new StringBuilder(rows * 32);

            for (int i = 0; i < rows; i++)
            {
                if (i == yourIndex)
                {
                    continue; // the player row is pinned below the top cut
                }

                BuildRow(i, manager.Leaderboard.Ranked[i], isYou: false);
            }

            if (yourIndex >= rows)
            {
                BuildRow(rows, manager.Leaderboard.You, isYou: true);
            }
            else if (yourIndex >= 0)
            {
                BuildRow(yourIndex, manager.Leaderboard.Ranked[yourIndex], isYou: true);
            }
        }

        private void BuildRow(int slotIndex, LeaderboardEntry entry, bool isYou)
        {
            float slotTop = 0.80f - slotIndex * 0.058f;
            float slotBottom = slotTop - 0.048f;
            Color color = isYou ? youHighlight : Color.white;

            TextMeshProUGUI rank = UiRuntime.CreateText(rowsRoot, "R" + slotIndex, (slotIndex + 1).ToString(), 15f,
                TextAlignmentOptions.MidlineLeft, color);
            UiRuntime.Anchor(rank.rectTransform, new Vector2(0f, slotBottom), new Vector2(0.12f, slotTop), 0f, 0f, 0f, 0f);

            TextMeshProUGUI tag = UiRuntime.CreateText(rowsRoot, "Tag" + slotIndex, (isYou ? "◆ " : "") + entry.Tag, 15f,
                TextAlignmentOptions.MidlineLeft, color);
            UiRuntime.Anchor(tag.rectTransform, new Vector2(0.16f, slotBottom), new Vector2(0.58f, slotTop), 0f, 0f, 0f, 0f);

            TextMeshProUGUI lvl = UiRuntime.CreateText(rowsRoot, "Lvl" + slotIndex, entry.PartyLevel.ToString(), 15f,
                TextAlignmentOptions.MidlineRight, color);
            UiRuntime.Anchor(lvl.rectTransform, new Vector2(0.60f, slotBottom), new Vector2(0.74f, slotTop), 0f, 0f, 0f, 0f);

            TextMeshProUGUI stage = UiRuntime.CreateText(rowsRoot, "Stage" + slotIndex, entry.MaxStage.ToString(), 15f,
                TextAlignmentOptions.MidlineRight, color);
            UiRuntime.Anchor(stage.rectTransform, new Vector2(0.78f, slotBottom), new Vector2(0.96f, slotTop), 0f, 0f, 0f, 0f);
        }
    }
}
