using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Combat;
using IdleRPG.Core;

namespace IdleRPG.UI
{
    /// <summary>
    /// The PARTY page: a three-tab workspace.
    ///
    /// ROSTER     - PartyRosterView (hero cards, big portrait + stats, equipped gear preview - read only).
    /// FORMATION  - the board; the only place a hero is moved (PartyBoardUI), plus what the ranks do.
    /// INVENTORY  - InventoryTabUI (square tile bag, filters + sort, E markers, docked sheet).
    ///
    /// The page shell owns only the tabs and the wiring between the three views. Inventory selection /
    /// discarding lives ONLY on the INVENTORY tab; the Roster just shows what is worn.
    /// </summary>
    public sealed class PartyPanelUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private RectTransform boardRoot;
        [SerializeField] private Vector2 boardOffset = new Vector2(14f, -24f);

        private readonly Color activeTabColor = new Color(0.24f, 0.34f, 0.55f, 1f);
        private readonly Color inactiveTabColor = new Color(0.11f, 0.13f, 0.19f, 1f);
        private readonly Color bodyText = new Color(1f, 1f, 1f, 0.92f);
        private readonly Color dimText = new Color(1f, 1f, 1f, 0.55f);
        private readonly Color hintColor = new Color(1f, 0.92f, 0.7f);

        private GameManager manager;
        private PartyBoardUI board;
        private InventoryTabUI inventoryTab;
        private PartyRosterView rosterView;

        private Image[] tabBackgrounds;
        private GameObject[] tabPanels;
        private int activeTab;

        private TextMeshProUGUI formationLabel;

        private bool built;

        private void OnEnable()
        {
            if (!built)
            {
                // Boot can race: Start/OnEnable may fire before GameManager exists.
                Build();
            }
            else
            {
                // Levels and formation can change while the tab is closed, so re-read on open.
                RefreshAll();
            }
        }

        private void OnDestroy()
        {
            if (manager != null)
            {
                if (manager.Formation != null)
                {
                    manager.Formation.Changed -= RefreshAll;
                }

                if (manager.Gear != null)
                {
                    manager.Gear.Changed -= RefreshAll;
                }
            }
        }

        private void Build()
        {
            if (built)
            {
                return;
            }

            HudController hud = HudController.Instance;
            manager = hud != null ? hud.GameManager : null;

            if (manager == null || manager.Formation == null || manager.Party == null)
            {
                Debug.LogWarning("[PartyPanelUI] GameManager not ready yet; will bind on next open.");
                return;
            }

            RectTransform root = boardRoot != null ? boardRoot : (RectTransform)transform;

            BuildTabs(root);
            rosterView = tabPanels[0].AddComponent<PartyRosterView>();
            rosterView.Build(manager);
            BuildFormation((RectTransform)tabPanels[1].transform);
            BuildInventory((RectTransform)tabPanels[2].transform);

            ShowTab(0);

            manager.Formation.Changed += RefreshAll;
            if (manager.Gear != null)
            {
                manager.Gear.Changed += RefreshAll;
            }

            built = true;
            RefreshAll();
        }

        // ------------------------------------------------------------------
        // Sub-navigation (ROSTER | FORMATION | INVENTORY)
        // ------------------------------------------------------------------
        private void BuildTabs(RectTransform root)
        {
            string[] names = { "ROSTER", "FORMATION", "INVENTORY" };
            tabBackgrounds = new Image[names.Length];
            tabPanels = new GameObject[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                float xMin = 0.03f + i * 0.315f;
                float xMax = xMin + 0.30f;
                int captured = i;

                Button tab = UiRuntime.CreateButton(root, "Tab" + names[i], names[i],
                    new Vector2(xMin, 0.905f), new Vector2(xMax, 0.995f), () => ShowTab(captured), inactiveTabColor);
                tabBackgrounds[i] = tab != null ? tab.GetComponent<Image>() : null;

                GameObject panel = UiRuntime.CreateNode(names[i] + "Panel", root);
                UiRuntime.Anchor(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1f, 0.895f));
                tabPanels[i] = panel;
            }
        }

        private void ShowTab(int index)
        {
            activeTab = index;

            if (tabPanels != null)
            {
                for (int i = 0; i < tabPanels.Length; i++)
                {
                    if (tabPanels[i] != null)
                    {
                        tabPanels[i].SetActive(i == index);
                    }
                }
            }

            RefreshTabVisuals();

            if (index == 2 && inventoryTab != null)
            {
                inventoryTab.OnShown();
            }
        }

        private void RefreshTabVisuals()
        {
            if (tabBackgrounds == null)
            {
                return;
            }

            for (int i = 0; i < tabBackgrounds.Length; i++)
            {
                if (tabBackgrounds[i] != null)
                {
                    tabBackgrounds[i].color = i == activeTab ? activeTabColor : inactiveTabColor;
                }
            }
        }

        // ------------------------------------------------------------------
        // Formation (the board owns itself)
        // ------------------------------------------------------------------
        private void BuildFormation(RectTransform root)
        {
            GameObject boardObject = UiRuntime.CreateNode("PartyBoard", root);
            RectTransform rect = boardObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = boardOffset;

            board = boardObject.AddComponent<PartyBoardUI>();
            board.Build(manager);

            formationLabel = UiRuntime.CreateText(root, "FormationInfo", string.Empty, 16f,
                TextAlignmentOptions.TopLeft, bodyText);
            UiRuntime.Anchor(formationLabel.rectTransform, new Vector2(0.02f, 0.46f), new Vector2(0.98f, 0.62f), 8f, 0f, 8f, 0f);

            TextMeshProUGUI hint = UiRuntime.CreateText(root, "FormationHint", "tap a hero, then tap a slot", 16f,
                TextAlignmentOptions.TopLeft, hintColor);
            UiRuntime.Anchor(hint.rectTransform, new Vector2(0.02f, 0.38f), new Vector2(0.98f, 0.46f), 8f, 0f, 8f, 0f);
        }

        // ------------------------------------------------------------------
        // Inventory (the bag lives on its own component)
        // ------------------------------------------------------------------
        private void BuildInventory(RectTransform root)
        {
            inventoryTab = root.gameObject.AddComponent<InventoryTabUI>();
            inventoryTab.Build(manager);
        }

        // ------------------------------------------------------------------
        // Refresh
        // ------------------------------------------------------------------
        private void RefreshAll()
        {
            if (manager == null || manager.Formation == null || manager.Party == null || tabPanels == null)
            {
                return;
            }

            RefreshTabVisuals();
            if (rosterView != null)
            {
                rosterView.RefreshAll();
            }

            RefreshFormationText();
        }

        private void RefreshFormationText()
        {
            if (formationLabel == null)
            {
                return;
            }

            Formation formation = manager.Formation;
            StringBuilder builder = new StringBuilder(140);
            builder.AppendLine("front " + formation.FrontCount + "   back " + formation.BackCount);
            builder.Append("the back rank is targeted " + formation.Data.BackRowTargetWeight.ToString("0.##") + "x as often as the front");
            formationLabel.SetText(builder.ToString());
        }

        // ------------------------------------------------------------------
        // Shared row builder (the Roster and the Formation use the same locked-slot look)
        // ------------------------------------------------------------------
        internal static readonly Color SlotRowTextColor = new Color(1f, 1f, 1f, 0.55f);
        internal static readonly Color SlotRowCellColor = new Color(0.14f, 0.16f, 0.23f, 0.95f);

        /// <summary>A caption plus a row of empty, locked slots (abilities, filled by a later step).</summary>
        internal static void BuildSlotRow(RectTransform root, string caption, int count, float yMin, float yMax)
        {
            TextMeshProUGUI label = UiRuntime.CreateText(root, caption + "Caption", caption, 15f,
                TextAlignmentOptions.MidlineLeft, SlotRowTextColor);
            UiRuntime.Anchor(label.rectTransform, new Vector2(0.48f, yMax), new Vector2(0.72f, yMax + 0.035f), 4f, 0f, 0f, 0f);

            TextMeshProUGUI note = UiRuntime.CreateText(root, caption + "Note", "(coming soon)", 14f,
                TextAlignmentOptions.MidlineRight, SlotRowTextColor);
            UiRuntime.Anchor(note.rectTransform, new Vector2(0.60f, yMax), new Vector2(0.97f, yMax + 0.035f), 0f, 0f, 4f, 0f);

            const float xMin = 0.48f;
            const float xMax = 0.97f;
            const float gap = 0.012f;
            float width = (xMax - xMin - gap * (count - 1)) / Mathf.Max(1, count);

            for (int i = 0; i < count; i++)
            {
                float x0 = xMin + i * (width + gap);
                Image slot = UiRuntime.CreatePanel(root, null, SlotRowCellColor);
                UiRuntime.Anchor(slot.rectTransform, new Vector2(x0, yMin), new Vector2(x0 + width, yMax - 0.005f));
            }
        }
    }
}
