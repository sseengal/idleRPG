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
    /// The INVENTORY tab of the Party page: a FIXED 4x5 (20-slot) bag of square tiles, two dropdowns
    /// (slot filter + sort) and a docked, non-modal item sheet. Built at runtime by PartyPanelUI;
    /// refreshes from ItemService.Changed. Transient UI comes from the shared kit (DropdownMenu,
    /// UiDockedSheet, UiTransient, ItemVisuals) so nothing here hand-rolls popups.
    /// </summary>
    public sealed class InventoryTabUI : MonoBehaviour
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

        // ------------------------------------------------------------------
        // Build (structure)
        // ------------------------------------------------------------------
        private void BuildHeader()
        {
            countLabel = UiRuntime.CreateText(root, "InventoryCount", string.Empty, 18f,
                TextAlignmentOptions.MidlineLeft, bodyText);
            UiRuntime.Anchor(countLabel.rectTransform, new Vector2(0.03f, 0.905f), new Vector2(0.60f, 0.96f), 0f, 0f, 0f, 0f);
        }

        private void BuildDropdowns()
        {
            Button slotBtn = UiRuntime.CreateButton(root, "SlotDropdown", string.Empty,
                new Vector2(0.03f, 0.85f), new Vector2(0.50f, 0.905f), null, dropdownColor);
            slotDropdownLabel = LabelOf(slotBtn, 16f);
            slotDropdown = new DropdownMenu(root, slotBtn, alignRight: false, SlotOptionLabels,
                index =>
                {
                    slotFilter = SlotOptionValues[index];
                    RefreshDropdownLabels();
                    Rebuild();
                },
                dropdownColor, optionActiveColor,
                index => SlotOptionValues[index] == slotFilter);

            Button sortBtn = UiRuntime.CreateButton(root, "SortDropdown", string.Empty,
                new Vector2(0.52f, 0.85f), new Vector2(0.97f, 0.905f), null, dropdownColor);
            sortDropdownLabel = LabelOf(sortBtn, 16f);
            sortDropdown = new DropdownMenu(root, sortBtn, alignRight: true, SortOptionLabels,
                index =>
                {
                    sortMode = index;
                    RefreshDropdownLabels();
                    Rebuild();
                },
                dropdownColor, optionActiveColor,
                index => index == sortMode);

            RefreshDropdownLabels();
        }

        private static TextMeshProUGUI LabelOf(Button button, float size)
        {
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.fontSize = size;
            }

            return label;
        }

        private void RefreshDropdownLabels()
        {
            if (slotDropdownLabel != null)
            {
                slotDropdownLabel.SetText("SLOT: " + (slotFilter < 0 ? "ALL" : SlotOptionLabels[slotFilter + 1]) + "  ▾");
            }

            if (sortDropdownLabel != null)
            {
                sortDropdownLabel.SetText("SORT: " + SortOptionLabels[sortMode] + "  ▾");
            }
        }

        private void BuildGrid()
        {
            GameObject node = UiRuntime.CreateNode("InventoryGrid", root);
            gridRoot = node.GetComponent<RectTransform>();
            UiRuntime.Anchor(gridRoot, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.84f));
        }

        // ------------------------------------------------------------------
        // Fixed 20-slot grid
        // ------------------------------------------------------------------
        private void Rebuild()
        {
            slotDropdown?.Close();
            sortDropdown?.Close();
            CloseItemSheet();

            if (manager == null || manager.Gear == null || gridRoot == null)
            {
                return;
            }

            if (countLabel != null)
            {
                bool full = manager.Gear.InventoryCount >= manager.Gear.InventoryCap;
                countLabel.color = full ? fullBagColor : bodyText;
                countLabel.SetText("INVENTORY  " + manager.Gear.InventoryCount + "/" + manager.Gear.InventoryCap);
            }

            for (int i = gridRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(gridRoot.GetChild(i).gameObject);
            }

            List<ItemInstance> items = VisibleItems();
            int total = Mathf.Max(1, gridColumns) * Mathf.Max(1, gridRows);

            for (int index = 0; index < total; index++)
            {
                ItemInstance item = index < items.Count ? items[index] : null;
                BuildTile(item, index % Mathf.Max(1, gridColumns), index / Mathf.Max(1, gridColumns));
            }
        }

        private List<ItemInstance> VisibleItems()
        {
            List<ItemInstance> list = new List<ItemInstance>();
            List<ItemInstance> all = manager.Gear != null ? manager.Gear.AllInstances() : new List<ItemInstance>();

            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] == null)
                {
                    continue;
                }

                if (slotFilter != SlotFilterAll && (int)all[i].SlotType != slotFilter)
                {
                    continue;
                }

                list.Add(all[i]);
            }

            switch (sortMode)
            {
                case SortRarity:
                    list.Sort((a, b) => b.Rarity.CompareTo(a.Rarity) != 0 ? b.Rarity.CompareTo(a.Rarity) : b.Level.CompareTo(a.Level));
                    break;
                case SortBonus:
                    list.Sort((a, b) => b.StatFraction.CompareTo(a.StatFraction));
                    break;
                default:
                    list.Reverse(); // NEWEST: newest drops first
                    break;
            }

            return list;
        }

        private void BuildTile(ItemInstance item, int column, int row)
        {
            GameObject tileNode = UiRuntime.CreateNode("Slot" + (row * gridColumns + column), gridRoot);
            RectTransform tileRect = tileNode.GetComponent<RectTransform>();
            tileRect.anchorMin = new Vector2(0f, 1f);
            tileRect.anchorMax = new Vector2(0f, 1f);
            tileRect.pivot = new Vector2(0.5f, 0.5f);

            float columns = Mathf.Max(1, gridColumns);
            float totalRowWidth = columns * tileSize + (columns - 1) * TileGap;
            float xStart = (gridRoot.rect.width - totalRowWidth) * 0.5f;
            float x = xStart + column * (tileSize + TileGap) + tileSize * 0.5f;
            float y = -(EdgePad + row * (tileSize + TileGap) + tileSize * 0.5f);
            tileRect.anchoredPosition = new Vector2(x, y);
            tileRect.sizeDelta = new Vector2(tileSize, tileSize);

            if (item == null)
            {
                // Empty slot: closes the sheet if one is open (bag stays non-modal).
                Button emptySlot = UiRuntime.CreateButton(tileNode.transform, "EmptySlot", string.Empty,
                    Vector2.zero, Vector2.one, () => CloseItemSheet(), new Color(0.10f, 0.12f, 0.17f, 0.9f));
                emptySlot.transform.localPosition = Vector3.zero;
                TextMeshProUGUI empty = UiRuntime.CreateText(tileNode.transform, "Empty", "empty", 13f,
                    TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.22f));
                UiRuntime.Stretch(empty.rectTransform, 0f, 56f, 0f, 8f);
                return;
            }

            bool equipped = manager.Gear.IsEquipped(item.InstanceId, out _, out _);

            Image ring = UiRuntime.CreatePanel(tileNode.transform, null, equipped ? equippedRingColor : new Color(0f, 0f, 0f, 0f));
            UiRuntime.Stretch(ring.rectTransform, -5f, -5f, -5f, -5f);
            ring.raycastTarget = false;

            Button tile = UiRuntime.CreateButton(tileNode.transform, "TileButton", string.Empty, Vector2.zero, Vector2.one,
                () => OpenItemSheet(item), ItemVisuals.RarityColor(item.Rarity));
            tile.transform.localPosition = Vector3.zero;

            TextMeshProUGUI letter = UiRuntime.CreateText(tile.transform, "Letter", ItemVisuals.SlotLetter(item.SlotType), 30f,
                TextAlignmentOptions.Center, Color.white);
            UiRuntime.Stretch(letter.rectTransform, 0f, 18f, 0f, 8f);

            TextMeshProUGUI stat = UiRuntime.CreateText(tile.transform, "Stat",
                "+" + item.PercentText + " " + ItemVisuals.StatLabel(item.SlotType), 14f,
                TextAlignmentOptions.Center, bodyText);
            UiRuntime.Anchor(stat.rectTransform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.22f), 0f, 0f, 0f, 0f);

            TextMeshProUGUI classTag = UiRuntime.CreateText(tile.transform, "Role", ItemVisuals.RoleLetter(item.Role).ToString(), 12f,
                TextAlignmentOptions.MidlineRight, dimText);
            UiRuntime.Anchor(classTag.rectTransform, new Vector2(0.72f, 0.78f), new Vector2(0.97f, 0.93f), 0f, 0f, 0f, 0f);

            if (equipped)
            {
                Image badge = UiRuntime.CreatePanel(tile.transform, null, new Color(0.24f, 0.34f, 0.55f, 1f));
                UiRuntime.Anchor(badge.rectTransform, new Vector2(0.02f, 0.78f), new Vector2(0.14f, 0.95f), 0f, 0f, 0f, 0f);
                TextMeshProUGUI e = UiRuntime.CreateText(tile.transform, "EBadge", "E", 14f, TextAlignmentOptions.Center, Color.white);
                UiRuntime.Anchor(e.rectTransform, new Vector2(0.02f, 0.78f), new Vector2(0.14f, 0.95f), 0f, 0f, 0f, 0f);
            }
        }

        // ------------------------------------------------------------------
        // Item sheet (docked by UiDockedSheet - non-modal, only its rect eats taps)
        // ------------------------------------------------------------------
        private void OpenItemSheet(ItemInstance item)
        {
            slotDropdown?.Close();
            sortDropdown?.Close();
            CloseItemSheet();

            if (item == null)
            {
                return;
            }

            activeItem = item;
            activeIsEquipped = manager.Gear.IsEquipped(item.InstanceId, out int heroIndex, out _);
            activeHeroIndex = activeIsEquipped ? heroIndex : manager.Gear.HeroFor(item);
            armedInstanceId = null;

            itemSheet = UiDockedSheet.Create(root, "ItemSheet", new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.41f), cardColor);
            Transform content = itemSheet.Content;

            Image tile = UiRuntime.CreatePanel(content, null, ItemVisuals.RarityColor(item.Rarity), true);
            UiRuntime.Anchor(tile.rectTransform, new Vector2(0.05f, 0.30f), new Vector2(0.31f, 0.62f));
            TextMeshProUGUI letter = UiRuntime.CreateText(content, "Letter", ItemVisuals.SlotLetter(item.SlotType), 42f,
                TextAlignmentOptions.Center, Color.white);
            UiRuntime.Anchor(letter.rectTransform, new Vector2(0.05f, 0.30f), new Vector2(0.31f, 0.62f), 0f, 0f, 0f, 0f);

            sheetName = UiRuntime.CreateText(content, "Name", string.Empty, 18f,
                TextAlignmentOptions.MidlineLeft, Color.white);
            UiRuntime.Anchor(sheetName.rectTransform, new Vector2(0.36f, 0.56f), new Vector2(0.84f, 0.66f), 0f, 0f, 0f, 0f);

            sheetInfo = UiRuntime.CreateText(content, "Info", string.Empty, 14f,
                TextAlignmentOptions.TopLeft, bodyText);
            UiRuntime.Anchor(sheetInfo.rectTransform, new Vector2(0.36f, 0.30f), new Vector2(0.95f, 0.53f), 0f, 0f, 0f, 0f);

            sheetEquipLabel = BuildSheetButton(content, "EquipButton", new Vector2(0.05f, 0.07f), new Vector2(0.63f, 0.25f), EquipAction);
            sheetDiscardLabel = BuildSheetButton(content, "DiscardButton", new Vector2(0.67f, 0.07f), new Vector2(0.95f, 0.25f), DiscardAction);

            RefreshSheet();
        }

        private static TextMeshProUGUI BuildSheetButton(Transform parent, string name, Vector2 min, Vector2 max,
            UnityEngine.Events.UnityAction onClick)
        {
            Button button = UiRuntime.CreateButton(parent, name, string.Empty, min, max, onClick, new Color(0.24f, 0.34f, 0.55f, 1f));
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.fontSize = 16f;
            }

            return label;
        }

        private void RefreshSheet()
        {
            if (activeItem == null || sheetName == null)
            {
                return;
            }

            sheetName.SetText(activeItem.ShortLabel);
            sheetInfo.SetText(
                activeItem.Role.ToString().ToUpperInvariant() + " ONLY\n" +
                activeItem.SlotType.DisplayName() + "  ·  +" + activeItem.PercentText + " " + ItemVisuals.StatLabel(activeItem.SlotType) + "\n" +
                "stage " + activeItem.Level + " drop      " + (activeItem.Price > 0d ? NumberFormatter.Format(activeItem.Price) + " gold" : string.Empty));

            // Equip is only offered when it can genuinely succeed (bag item + its class exists).
            bool canEquip = !activeIsEquipped
                && activeHeroIndex >= 0
                && manager.Gear != null
                && manager.Gear.FindFor(activeItem.InstanceId) != null;

            sheetEquipLabel.text = activeIsEquipped
                ? "UNEQUIP"
                : canEquip && activeHeroIndex >= 0
                    ? "EQUIP  →  " + manager.Party.GetHero(activeHeroIndex).HeroName.ToUpperInvariant()
                    : "EQUIP";

            Button equipButton = FindSheetButton("EquipButton");
            if (equipButton != null)
            {
                equipButton.interactable = canEquip || activeIsEquipped;
            }

            bool armed = armedInstanceId == activeItem.InstanceId;
            sheetDiscardLabel.SetText(armed ? "TAP AGAIN" : "DISCARD");
            Button discard = FindSheetButton("DiscardButton");
            if (discard != null)
            {
                discard.GetComponent<Image>().color = armed ? new Color(0.75f, 0.18f, 0.18f, 1f) : armedColor;
            }
        }

        private Button FindSheetButton(string name)
        {
            return itemSheet != null && itemSheet.Content.Find(name) != null
                ? itemSheet.Content.Find(name).GetComponent<Button>()
                : null;
        }

        private void EquipAction()
        {
            if (activeItem == null || manager.Gear == null)
            {
                return;
            }

            bool ok;
            string message = string.Empty;

            if (activeIsEquipped)
            {
                ok = manager.Gear.Unequip(activeHeroIndex, activeItem.SlotType, out message);
            }
            else
            {
                int hero = manager.Gear.HeroFor(activeItem);
                ok = hero >= 0 && manager.Gear.Equip(hero, activeItem.InstanceId, out message);
            }

            if (!ok && !string.IsNullOrEmpty(message))
            {
                GameEvents.RaiseToast(message);
            }

            CloseItemSheet();
            Rebuild();
        }

        /// <summary>Two-tap discard: first tap arms the button, the second actually deletes.</summary>
        private void DiscardAction()
        {
            if (activeItem == null || manager.Gear == null)
            {
                return;
            }

            if (armedInstanceId == activeItem.InstanceId)
            {
                manager.Gear.Discard(activeItem.InstanceId, out _);
                CloseItemSheet();
                Rebuild();
            }
            else
            {
                armedInstanceId = activeItem.InstanceId;
                RefreshSheet();
            }
        }

        private void CloseItemSheet()
        {
            if (itemSheet != null)
            {
                UiTransient.Close(itemSheet.Root);
                itemSheet = null;
            }

            activeItem = null;
            activeHeroIndex = -1;
            activeIsEquipped = false;
            armedInstanceId = null;
        }
    }
}
