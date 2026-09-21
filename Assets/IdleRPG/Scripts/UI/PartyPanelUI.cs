using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;

namespace IdleRPG.UI
{
    /// <summary>
    /// The PARTY tab: the board (the only place formation is edited), the roster, and the selected hero's details.
    ///
    /// ELI5: the changing room. Board on the left, team list on the right, and whichever hero you tap shows its
    /// card underneath - stats today, gear and abilities in later steps. Every row is built from data, so adding a
    /// stat or an equipment slot later appends a row instead of redesigning the screen.
    /// </summary>
    public sealed class PartyPanelUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private RectTransform boardRoot;
        [SerializeField] private Vector2 boardOffset = new Vector2(14f, -58f);

        [Header("Placeholders")]
        [Tooltip("Equipment slots drawn but not usable yet (Step 18). Reserving the space now keeps the layout stable.")]
        [SerializeField] private int gearSlotPlaceholders = 4;

        private GameManager manager;
        private PartyBoardUI board;
        private TextMeshProUGUI hintLabel;
        private TextMeshProUGUI rosterLabel;
        private TextMeshProUGUI detailLabel;

        private int selectedHeroIndex = -1;
        private Button[] rosterButtons;

        private void Start()
        {
            Build();
        }

        private void OnEnable()
        {
            // Hero levels can change while the tab is closed, so re-read them on open.
            RefreshAll();
        }

        private void Build()
        {
            HudController hud = HudController.Instance;
            manager = hud != null ? hud.GameManager : null;

            if (manager == null || manager.Formation == null)
            {
                Debug.LogWarning("[PartyPanelUI] GameManager not ready yet; will bind on next open.");
                return;
            }

            RectTransform root = boardRoot != null ? boardRoot : (RectTransform)transform;

            BuildBoard(root);
            BuildRoster(root);
            BuildDetail(root);

            manager.Formation.Changed += RefreshAll;
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (manager != null && manager.Formation != null)
            {
                manager.Formation.Changed -= RefreshAll;
            }
        }

        private void BuildBoard(RectTransform root)
        {
            GameObject boardObject = UiRuntime.CreateNode("PartyBoard", root);
            RectTransform rect = boardObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = boardOffset;

            board = boardObject.AddComponent<PartyBoardUI>();
            board.Build(manager);

            hintLabel = UiRuntime.CreateText(root, "Hint", "tap a hero, then tap a slot", 16f,
                TextAlignmentOptions.MidlineLeft, new Color(1f, 0.92f, 0.7f));
            UiRuntime.Anchor(hintLabel.rectTransform, new Vector2(0f, 0.60f), new Vector2(0.46f, 0.66f), 16f, 0f, 8f, 0f);
        }

        private void BuildRoster(RectTransform root)
        {
            int heroes = manager.Party != null ? manager.Party.Heroes.Count : 0;
            rosterButtons = new Button[heroes];

            const float rowHeight = 0.075f;
            float top = 0.92f;

            for (int i = 0; i < heroes; i++)
            {
                int index = i;
                float yMax = top - i * rowHeight;
                float yMin = yMax - rowHeight + 0.008f;

                Button row = UiRuntime.CreateButton(root, "RosterRow" + i, string.Empty,
                    new Vector2(0.50f, yMin), new Vector2(0.99f, yMax), () => SelectHero(index),
                    new Color(0.16f, 0.19f, 0.28f, 0.95f));

                if (row != null)
                {
                    rosterButtons[i] = row;
                }
            }
        }

        private void BuildDetail(RectTransform root)
        {
            rosterLabel = UiRuntime.CreateText(root, "RosterDetail", string.Empty, 18f,
                TextAlignmentOptions.TopLeft, new Color(1f, 1f, 1f, 0.92f));
            UiRuntime.Anchor(rosterLabel.rectTransform, new Vector2(0.50f, 0.24f), new Vector2(1f, 0.60f), 8f, 0f, 14f, 0f);

            detailLabel = UiRuntime.CreateText(root, "Detail", string.Empty, 17f,
                TextAlignmentOptions.TopLeft, new Color(1f, 1f, 1f, 0.8f));
            UiRuntime.Anchor(detailLabel.rectTransform, new Vector2(0f, 0.04f), new Vector2(1f, 0.22f), 16f, 0f, 14f, 0f);
        }

        private void SelectHero(int heroIndex)
        {
            selectedHeroIndex = heroIndex;
            RefreshAll();
        }

        // ------------------------------------------------------------------
        // Text
        // ------------------------------------------------------------------
        private void RefreshAll()
        {
            if (manager == null || manager.Formation == null)
            {
                return;
            }

            RefreshRosterRows();
            RefreshRosterDetail();
            RefreshHeroDetail();

            if (hintLabel != null)
            {
                hintLabel.SetText("tap a hero, then tap a slot");
            }
        }

        private void RefreshRosterRows()
        {
            if (rosterButtons == null || manager.Party == null)
            {
                return;
            }

            for (int i = 0; i < rosterButtons.Length; i++)
            {
                if (rosterButtons[i] == null)
                {
                    continue;
                }

                TextMeshProUGUI label = rosterButtons[i].GetComponentInChildren<TextMeshProUGUI>();

                if (label == null)
                {
                    continue;
                }

                HeroData hero = manager.Party.GetHero(i);
                label.SetText(hero != null ? RosterLine(i, hero) : "empty slot");
                label.color = i == selectedHeroIndex ? new Color(1f, 0.9f, 0.5f) : Color.white;
                label.alignment = TextAlignmentOptions.MidlineLeft;
            }
        }

        private string RosterLine(int heroIndex, HeroData hero)
        {
            int slot = manager.Formation.SlotOfHero(heroIndex);
            string where = slot < 0
                ? "benched"
                : (manager.Formation.Data.RowOfSlot(slot) == Sim.CombatRow.Front ? "front" : "back") + " " +
                  (manager.Formation.Data.PositionOfSlot(slot) + 1);

            double health = Resolve(i => manager.Resolver.GetMaxHealth(hero, i), heroIndex, hero.BaseHealth);
            double attack = Resolve(i => manager.Resolver.GetAttack(hero, i), heroIndex, hero.BaseAttack);

            return $"{hero.HeroName}   {hero.Role}   {where}   hp {health:0}  atk {attack:0}";
        }

        private static double Resolve(System.Func<int, double> read, int heroIndex, double fallback)
        {
            try
            {
                return read(heroIndex);
            }
            catch (System.Exception)
            {
                return fallback;
            }
        }

        private void RefreshRosterDetail()
        {
            if (rosterLabel == null)
            {
                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(200);
            builder.AppendLine("RANK  " + manager.Formation.Describe());
            builder.AppendLine($"front {manager.Formation.FrontCount}   back {manager.Formation.BackCount}");
            builder.AppendLine($"enemies aim at the back rank {manager.Formation.Data.BackRowTargetWeight:0.##} as often as the front");
            rosterLabel.SetText(builder.ToString().TrimEnd());
        }

        private void RefreshHeroDetail()
        {
            if (detailLabel == null)
            {
                return;
            }

            if (selectedHeroIndex < 0 || manager.Party == null)
            {
                detailLabel.SetText("tap a hero on the right to see its card");
                return;
            }

            HeroData hero = manager.Party.GetHero(selectedHeroIndex);

            if (hero == null)
            {
                detailLabel.SetText("no hero in that slot");
                return;
            }

            double health = Resolve(i => manager.Resolver.GetMaxHealth(hero, i), selectedHeroIndex, hero.BaseHealth);
            double attack = Resolve(i => manager.Resolver.GetAttack(hero, i), selectedHeroIndex, hero.BaseAttack);
            double defense = Resolve(i => manager.Resolver.GetDefense(hero, i), selectedHeroIndex, hero.BaseDefense);

            System.Text.StringBuilder builder = new System.Text.StringBuilder(240);
            builder.AppendLine($"{hero.HeroName}   [{hero.Role}]");
            builder.AppendLine($"hp {health:0}   atk {attack:0}   def {defense:0}   every {hero.AttackIntervalSec:0.##}s");
            builder.Append("gear: ");

            for (int i = 0; i < Mathf.Max(0, gearSlotPlaceholders); i++)
            {
                builder.Append("[   ] ");
            }

            builder.Append("  (Step 18)");
            detailLabel.SetText(builder.ToString().TrimEnd());
        }
    }
}
