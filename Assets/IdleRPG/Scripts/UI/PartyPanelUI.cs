using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;

namespace IdleRPG.UI
{
    /// <summary>
    /// The PARTY page: a one-tab workspace (the ROSTER), with the INVENTORY tab built only when the bag is enabled.
    ///
    /// ELI5: this page is the changing room. Moving heroes used to happen here too; it now happens on the battle
    /// board itself (tap a hero, tap a free pad), so the page keeps only the roster and what the hero wears.
    ///
    /// The shell owns the tabs and the wiring. Inventory selection / discarding lives ONLY on the INVENTORY tab.
    /// </summary>
    public sealed class PartyPanelUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private RectTransform boardRoot;

        /// <summary>
        /// The INVENTORY sub-tab (the "bag": dropped loot, equip / salvage) is PARKED for MVP, so the tab is not
        /// built and boss drops are switched off in BalanceConfig (see Docs/Party-Page.md). The whole feature is
        /// intact - set this to true and re-enable the drop chance to bring it back.
        /// </summary>
        private const bool InventoryEnabled = false;

        private readonly Color activeTabColor = new Color(0.24f, 0.34f, 0.55f, 1f);
        private readonly Color inactiveTabColor = new Color(0.11f, 0.13f, 0.19f, 1f);

        private GameManager manager;
        private InventoryTabUI inventoryTab;
        private PartyRosterView rosterView;

        private Image[] tabBackgrounds;
        private GameObject[] tabPanels;
        private int activeTab;

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
                // Levels and gear can change while the tab is closed, so re-read on open.
                RefreshAll();
            }
        }

        private void OnDestroy()
        {
            if (manager != null && manager.Gear != null)
            {
                manager.Gear.Changed -= RefreshAll;
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

            if (manager == null || manager.Party == null)
            {
                Debug.LogWarning("[PartyPanelUI] GameManager not ready yet; will bind on next open.");
                return;
            }

            RectTransform root = boardRoot != null ? boardRoot : (RectTransform)transform;

            BuildTabs(root);
            rosterView = tabPanels[0].AddComponent<PartyRosterView>();
            rosterView.Build(manager);

            if (InventoryEnabled)
            {
                BuildInventory((RectTransform)tabPanels[1].transform);
            }

            ShowTab(0);

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
            // Tab set is data, not a fixed array: parking the bag removes one tab and the layout re-spreads
            // itself evenly (no re-authoring, no left-heavy bar).
            string[] names = InventoryEnabled
                ? new[] { "ROSTER", "INVENTORY" }
                : new[] { "ROSTER" };

            // With the bag parked there is one tab, so the bar is dropped and the roster gets the full panel
            // instead of a lonely button over empty space.
            bool showBar = names.Length > 1;
            float panelTop = showBar ? 0.895f : 1f;

            tabBackgrounds = showBar ? new Image[names.Length] : null;
            tabPanels = new GameObject[names.Length];

            const float barLeft = 0.03f;
            const float barRight = 0.97f;
            const float barGap = 0.012f;
            float width = ((barRight - barLeft) - barGap * (names.Length - 1)) / names.Length;

            for (int i = 0; i < names.Length; i++)
            {
                if (showBar)
                {
                    float xMin = barLeft + i * (width + barGap);
                    float xMax = xMin + width;
                    int captured = i;

                    Button tab = UiRuntime.CreateButton(root, "Tab" + names[i], names[i],
                        new Vector2(xMin, 0.905f), new Vector2(xMax, 0.995f), () => ShowTab(captured), inactiveTabColor);
                    tabBackgrounds[i] = tab != null ? tab.GetComponent<Image>() : null;
                }

                GameObject panel = UiRuntime.CreateNode(names[i] + "Panel", root);
                UiRuntime.Anchor(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1f, panelTop));
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
            if (manager == null || manager.Party == null || tabPanels == null)
            {
                return;
            }

            RefreshTabVisuals();
            if (rosterView != null)
            {
                rosterView.RefreshAll();
            }
        }

        // ------------------------------------------------------------------
        // Shared row builder (the Roster uses the locked-slot look)
        // ------------------------------------------------------------------
        internal static readonly Color SlotRowTextColor = new Color(1f, 1f, 1f, 0.55f);
        internal static readonly Color SlotRowCellColor = new Color(0.14f, 0.16f, 0.23f, 0.95f);

        /// <summary>A caption plus a row of empty, locked slots (abilities, filled by a later step).</summary>
        internal static void BuildSlotRow(RectTransform root, string caption, int count, float yMin, float yMax)
        {
            TextMeshProUGUI label = UiRuntime.CreateText(root, caption + "Caption", caption, UiType.Role.Small,
                TextAlignmentOptions.MidlineLeft, SlotRowTextColor);
            UiRuntime.Anchor(label.rectTransform, new Vector2(0.48f, yMax), new Vector2(0.72f, yMax + 0.035f), UiSpace.Tight, UiSpace.None, UiSpace.None, UiSpace.None);

            TextMeshProUGUI note = UiRuntime.CreateText(root, caption + "Note", "(coming soon)", UiType.Role.Small,
                TextAlignmentOptions.MidlineRight, SlotRowTextColor);
            UiRuntime.Anchor(note.rectTransform, new Vector2(0.60f, yMax), new Vector2(0.97f, yMax + 0.035f), UiSpace.None, UiSpace.None, UiSpace.Tight, UiSpace.None);

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
