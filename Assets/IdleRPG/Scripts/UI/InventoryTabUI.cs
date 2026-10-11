using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Equipment;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// The INVENTORY tab of the Party page: the bag's own state (slot filter, sort mode, selection) and its
    /// lifecycle. Refreshes from ItemService.Changed.
    ///
    /// Split by screen part, the house pattern: <c>InventoryTabUI.Filters</c> owns the count and the two
    /// dropdowns, <c>.Grid</c> the fixed 20 tiles, <c>.Sheet</c> the docked item sheet. Transient UI comes from
    /// the shared kit (DropdownMenu, UiDockedSheet, UiTransient, ItemVisuals) - nothing here hand-rolls popups.
    /// </summary>
    public sealed partial class InventoryTabUI : MonoBehaviour
    {
        [SerializeField] private int gridColumns = 4;
        [SerializeField] private int gridRows = 5;      // 20 slots, always visible
        [SerializeField] private float tileSize = 150f;

        private const float TileGap = 10f;
        private const float EdgePad = 12f;

        private readonly Color dropdownColor = new Color(0.11f, 0.13f, 0.19f, 1f);
        private readonly Color optionActiveColor = new Color(0.24f, 0.34f, 0.55f, 1f);
        private readonly Color cardColor = new Color(0.16f, 0.19f, 0.28f, 0.95f);
        private readonly Color dimText = new Color(1f, 1f, 1f, 0.55f);
        private readonly Color bodyText = new Color(1f, 1f, 1f, 0.92f);
        private readonly Color fullBagColor = new Color(1f, 0.82f, 0.3f, 1f);
        private readonly Color equippedRingColor = new Color(1f, 0.9f, 0.5f, 1f);
        private readonly Color armedColor = new Color(0.52f, 0.16f, 0.16f, 0.95f);

        private const int SlotFilterAll = -1;
        private const int SortNewest = 0;
        private const int SortRarity = 1;
        private const int SortBonus = 2;

        private static readonly string[] SlotOptionLabels = { "ALL", "WEAPON", "ARMOR", "TRINKET" };
        private static readonly int[] SlotOptionValues = { -1, 0, 1, 2 };
        private static readonly string[] SortOptionLabels = { "NEWEST", "RARITY", "BONUS" };

        private GameManager manager;
        private RectTransform root;

        private int slotFilter = SlotFilterAll;
        private int sortMode = SortNewest;

        private TextMeshProUGUI countLabel;
        private DropdownMenu slotDropdown;
        private DropdownMenu sortDropdown;
        private TextMeshProUGUI slotDropdownLabel;
        private TextMeshProUGUI sortDropdownLabel;
        private RectTransform gridRoot;

        private UiDockedSheet itemSheet;
        private ItemInstance activeItem;
        private int activeHeroIndex = -1;
        private bool activeIsEquipped;
        private string armedInstanceId;

        private TextMeshProUGUI sheetName;
        private TextMeshProUGUI sheetInfo;
        private TextMeshProUGUI sheetEquipLabel;
        private TextMeshProUGUI sheetDiscardLabel;

        private bool subscribed;

        public void Build(GameManager gameManager)
        {
            manager = gameManager;
            root = (RectTransform)transform;

            BuildHeader();
            BuildDropdowns();
            BuildGrid();
            Rebuild();

            if (manager != null && manager.Gear != null && !subscribed)
            {
                manager.Gear.Changed += Rebuild;
                subscribed = true;
            }
        }

        private void OnDestroy()
        {
            if (manager != null && manager.Gear != null && subscribed)
            {
                manager.Gear.Changed -= Rebuild;
                subscribed = false;
            }
        }

        /// <summary>The tab was opened: redraw so the bag is fresh.</summary>
        public void OnShown()
        {
            Rebuild();
        }

        /// <summary>Kept for tooling; the dropdown drives the filter in the UI.</summary>
        public void SetSlotFilter(int slotIndex)
        {
            slotFilter = slotIndex >= 0 && slotIndex < 3 ? slotIndex : SlotFilterAll;
            RefreshDropdownLabels();
            Rebuild();
        }

    }
}
