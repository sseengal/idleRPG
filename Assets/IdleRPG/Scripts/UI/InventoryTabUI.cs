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
    /// The INVENTORY tab of the Party page: the bag as a square tile grid with slot filters and sorting.
    /// Tapping a tile opens a bottom sheet with the item's info plus EQUIP/UNEQUIP and a two-tap DISCARD.
    /// Equipped tiles carry an 'E' badge and a bright border. Built at runtime by <see cref="PartyPanelUI"/>
    /// on its third panel; refreshes from ItemService.Changed. Mobile-minimum touch/font sizes throughout.
    /// </summary>
    public sealed class InventoryTabUI : MonoBehaviour
    {
        [SerializeField] private int gridColumns = 4;
        [SerializeField] private float tileSize = 150f;

        private const float TileGap = 10f;
        private const float EdgePad = 12f;

        private readonly Color chipActiveColor = new Color(0.24f, 0.34f, 0.55f, 1f);
        private readonly Color chipInactiveColor = new Color(0.11f, 0.13f, 0.19f, 1f);
        private readonly Color cardColor = new Color(0.16f, 0.19f, 0.28f, 0.95f);
        private readonly Color dimText = new Color(1f, 1f, 1f, 0.55f);
        private readonly Color bodyText = new Color(1f, 1f, 1f, 0.92f);
        private readonly Color equippedRingColor = new Color(1f, 0.9f, 0.5f, 1f);
        private readonly Color armedColor = new Color(0.52f, 0.16f, 0.16f, 0.95f);

        private const int SlotFilterAll = -1;
        private const int SortNewest = 0;
        private const int SortRarity = 1;
        private const int SortBonus = 2;

        private static readonly string[] SlotChipLabels = { "ALL", "WEAPON", "ARMOR", "TRINKET" };
        private static readonly int[] SlotChipValues = { -1, 0, 1, 2 };
        private static readonly string[] SortChipLabels = { "NEWEST", "RARITY", "BONUS" };

        private GameManager manager;
        private RectTransform root;

        private int slotFilter = SlotFilterAll;
        private int sortMode = SortNewest;

        private TextMeshProUGUI countLabel;
        private Button[] slotChips;
        private Button[] sortChips;
        private RectTransform gridViewport;
        private RectTransform gridContent;

        private GameObject popupRoot;
        private Image popupTileImage;
        private TextMeshProUGUI popupLetter;
        private TextMeshProUGUI popupName;
        private TextMeshProUGUI popupInfo;
        private TextMeshProUGUI popupEquipLabel;
        private TextMeshProUGUI popupDiscardLabel;

        private ItemInstance activeItem;
        private int activeHeroIndex = -1;
        private bool activeIsEquipped;
        private string armedInstanceId;

        private bool subscribed;

        public void Build(GameManager gameManager)
        {
            manager = gameManager;
            root = (RectTransform)transform;

            BuildHeader();
            BuildChips();
            BuildGridViewport();
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

        /// <summary>Applies a slot filter (used by the Roster's "tap a slot" jump).</summary>
        public void SetSlotFilter(int slotIndex)
        {
            slotFilter = slotIndex >= 0 && slotIndex < 3 ? slotIndex : SlotFilterAll;
            Rebuild();
            RefreshChipVisuals();
        }
// ------------------------------------------------------------------
        // Build (structure)
        // ------------------------------------------------------------------
        private void BuildHeader()
        {
            countLabel = UiRuntime.CreateText(root, "InventoryCount", string.Empty, 18f,
                TextAlignmentOptions.MidlineLeft, bodyText);
            UiRuntime.Anchor(countLabel.rectTransform, new Vector2(0.03f, 0.86f), new Vector2(0.60f, 0.905f), 0f, 0f, 0f, 0f);
        }

        private void BuildChips()
        {
            int slots = SlotChipLabels.Length;
            int sorts = SortChipLabels.Length;

            slotChips = new Button[slots];
            float slotWidth = (0.94f - (slots - 1) * 0.015f) / slots;

            for (int i = 0; i < slots; i++)
            {
                int captured = i;
                Button chip = UiRuntime.CreateButton(root, "SlotChip" + SlotChipLabels[i], SlotChipLabels[i],
                    new Vector2(0.03f + i * (slotWidth + 0.015f), 0.785f),
                    new Vector2(0.03f + i * (slotWidth + 0.015f) + slotWidth, 0.855f),
                    () => SelectSlotChip(captured), chipInactiveColor);
                slotChips[i] = chip;
                TextMeshProUGUI label = chip.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.fontSize = 14f;
                }
            }

            sortChips = new Button[sorts];
            float sortWidth = (0.94f - (sorts - 1) * 0.015f) / sorts;

            for (int i = 0; i < sorts; i++)
            {
                int captured = i;
                Button chip = UiRuntime.CreateButton(root, "SortChip" + SortChipLabels[i], SortChipLabels[i],
                    new Vector2(0.03f + i * (sortWidth + 0.015f), 0.715f),
                    new Vector2(0.03f + i * (sortWidth + 0.015f) + sortWidth, 0.785f),
                    () => SelectSortChip(captured), chipInactiveColor);
                sortChips[i] = chip;
                TextMeshProUGUI label = chip.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.fontSize = 14f;
                }
            }

            RefreshChipVisuals();
        }

        private void RefreshChipVisuals()
        {
            for (int i = 0; i < slotChips.Length; i++)
            {
                if (slotChips[i] != null)
                {
                    slotChips[i].GetComponent<Image>().color = SlotChipValues[i] == slotFilter ? chipActiveColor : chipInactiveColor;
                }
            }

            for (int i = 0; i < sortChips.Length; i++)
            {
                if (sortChips[i] != null)
                {
                    sortChips[i].GetComponent<Image>().color = i == sortMode ? chipActiveColor : chipInactiveColor;
                }
            }
        }

        private void SelectSlotChip(int chipIndex)
        {
            slotFilter = SlotChipValues[chipIndex];
            Rebuild();
            RefreshChipVisuals();
        }

        private void SelectSortChip(int chipIndex)
        {
            sortMode = chipIndex;
            Rebuild();
            RefreshChipVisuals();
        }
private void BuildGridViewport()
        {
            GameObject viewportNode = UiRuntime.CreateNode("InventoryGrid", root);
            gridViewport = viewportNode.GetComponent<RectTransform>();
            UiRuntime.Anchor(gridViewport, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.705f));

            Image maskImage = viewportNode.AddComponent<Image>();
            maskImage.color = new Color(0f, 0f, 0f, 0.001f);
            maskImage.raycastTarget = false;
            viewportNode.AddComponent<RectMask2D>();

            GameObject contentNode = UiRuntime.CreateNode("GridContent", viewportNode.transform);
            gridContent = contentNode.GetComponent<RectTransform>();
            gridContent.anchorMin = new Vector2(0f, 1f);
            gridContent.anchorMax = new Vector2(0f, 1f);
            gridContent.pivot = new Vector2(0f, 1f);
            gridContent.anchoredPosition = Vector2.zero;
        }

        // ------------------------------------------------------------------
        // Grid
        // ------------------------------------------------------------------
        private void Rebuild()
        {
            TearDownPopup();

            if (manager == null || manager.Gear == null || gridContent == null)
            {
                return;
            }

            if (countLabel != null)
            {
                countLabel.SetText("INVENTORY  " + manager.Gear.InventoryCount + "/" + manager.Gear.InventoryCap);
            }

            for (int i = gridContent.childCount - 1; i >= 0; i--)
            {
                Destroy(gridContent.GetChild(i).gameObject);
            }

            List<ItemInstance> items = VisibleItems();
            int columns = Mathf.Max(1, gridColumns);
            int rows = (items.Count + columns - 1) / Mathf.Max(1, columns);
            float viewportWidth = Mathf.Max(200f, gridViewport.rect.width);
            gridContent.sizeDelta = new Vector2(viewportWidth, rows > 0 ? rows * (tileSize + TileGap) + EdgePad : 0f);

            for (int i = 0; i < items.Count; i++)
            {
                BuildTile(items[i], i % columns, i / columns, viewportWidth);
            }

            if (items.Count == 0)
            {
                TextMeshProUGUI empty = UiRuntime.CreateText(gridContent, "Empty", "no gear here yet\nbosses drop gear", 15f,
                    TextAlignmentOptions.Center, dimText);
                UiRuntime.Anchor(empty.rectTransform, new Vector2(0f, 0.3f), new Vector2(1f, 0.6f), 0f, 0f, 0f, 0f);
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
private void BuildTile(ItemInstance item, int column, int row, float viewportWidth)
        {
            int tileCount = gridContent.childCount;
            GameObject tileNode = UiRuntime.CreateNode("Tile" + tileCount, gridContent);
            RectTransform tileRect = tileNode.GetComponent<RectTransform>();
            tileRect.anchorMin = new Vector2(0f, 1f);
            tileRect.anchorMax = new Vector2(0f, 1f);
            tileRect.pivot = new Vector2(0.5f, 0.5f);

            float columns = Mathf.Max(1, gridColumns);
            float totalRowWidth = columns * tileSize + (columns - 1) * TileGap;
            float xStart = (viewportWidth - totalRowWidth) * 0.5f;
            float x = xStart + column * (tileSize + TileGap) + tileSize * 0.5f;
            float y = -(EdgePad + row * (tileSize + TileGap) + tileSize * 0.5f);
            tileRect.anchoredPosition = new Vector2(x, y);
            tileRect.sizeDelta = new Vector2(tileSize, tileSize);

            bool equipped = manager.Gear.IsEquipped(item.InstanceId, out _, out _);

            // Equipped: a bright ring sits behind the tile (visible on all four edges).
            Image ring = UiRuntime.CreatePanel(tileNode.transform, null, equipped ? equippedRingColor : new Color(0f, 0f, 0f, 0f));
            UiRuntime.Stretch(ring.rectTransform, -5f, -5f, -5f, -5f);
            ring.raycastTarget = false;

            Button tile = UiRuntime.CreateButton(tileNode.transform, "TileButton", string.Empty, Vector2.zero, Vector2.one,
                () => OpenPopup(item), PartyPanelUI.RarityColor(item.Rarity));
            tile.transform.localPosition = Vector3.zero;

            TextMeshProUGUI letter = UiRuntime.CreateText(tile.transform, "Letter", GearSlotLetter(item.SlotType), 30f,
                TextAlignmentOptions.Center, Color.white);
            UiRuntime.Stretch(letter.rectTransform, 0f, 18f, 0f, 8f);

            TextMeshProUGUI stat = UiRuntime.CreateText(tile.transform, "Stat", "+" + item.PercentText + " " + ItemSlotTypeExtensions.PrimaryStatLabel(item.SlotType), 14f,
                TextAlignmentOptions.Center, bodyText);
            UiRuntime.Anchor(stat.rectTransform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.22f), 0f, 0f, 0f, 0f);

            TextMeshProUGUI classTag = UiRuntime.CreateText(tile.transform, "Role", RoleLetter(item.Role).ToString(), 12f,
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

        private static string GearSlotLetter(ItemSlotType slot)
        {
            switch (slot)
            {
                case ItemSlotType.Weapon:
                    return "W";
                case ItemSlotType.Armor:
                    return "A";
                default:
                    return "T";
            }
        }

        private static char RoleLetter(HeroRole role)
        {
            return role.ToString()[0];
        }
// ------------------------------------------------------------------
        // Item popup (bottom sheet)
        // ------------------------------------------------------------------
        private void OpenPopup(ItemInstance item)
        {
            TearDownPopup();

            if (item == null)
            {
                return;
            }

            activeItem = item;
            activeIsEquipped = manager.Gear.IsEquipped(item.InstanceId, out int heroIndex, out _);
            activeHeroIndex = activeIsEquipped ? heroIndex : manager.Gear.HeroFor(item);
            armedInstanceId = null;

            GameObject popup = UiRuntime.CreateNode("ItemPopup", root);
            UiRuntime.Anchor(popup.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            popupRoot = popup;

            // Dimmed backdrop; tapping it closes the sheet.
            Image dim = UiRuntime.CreatePanel(popup.transform, null, new Color(0f, 0f, 0f, 0.6f));
            UiRuntime.Stretch(dim.rectTransform);
            Button dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.targetGraphic = dim;
            dimButton.onClick.AddListener(ClosePopup);

            // The sheet.
            Image sheet = UiRuntime.CreatePanel(popup.transform, null, cardColor);
            UiRuntime.Anchor(sheet.rectTransform, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.47f));

            popupTileImage = UiRuntime.CreatePanel(popup.transform, null, PartyPanelUI.RarityColor(item.Rarity));
            UiRuntime.Anchor(popupTileImage.rectTransform, new Vector2(0.06f, 0.30f), new Vector2(0.32f, 0.62f));
            popupLetter = UiRuntime.CreateText(popup.transform, "Letter", GearSlotLetter(item.SlotType), 42f,
                TextAlignmentOptions.Center, Color.white);
            UiRuntime.Anchor(popupLetter.rectTransform, new Vector2(0.06f, 0.30f), new Vector2(0.32f, 0.62f), 0f, 0f, 0f, 0f);

            popupName = UiRuntime.CreateText(popup.transform, "Name", string.Empty, 18f,
                TextAlignmentOptions.MidlineLeft, Color.white);
            UiRuntime.Anchor(popupName.rectTransform, new Vector2(0.37f, 0.56f), new Vector2(0.90f, 0.66f), 0f, 0f, 0f, 0f);

            popupInfo = UiRuntime.CreateText(popup.transform, "Info", string.Empty, 14f,
                TextAlignmentOptions.TopLeft, bodyText);
            UiRuntime.Anchor(popupInfo.rectTransform, new Vector2(0.37f, 0.30f), new Vector2(0.95f, 0.53f), 0f, 0f, 0f, 0f);

            popupEquipLabel = BuildPopupButton(popup.transform, "EquipButton", new Vector2(0.05f, 0.07f), new Vector2(0.63f, 0.25f), EquipAction, new Color(0.24f, 0.34f, 0.55f, 1f));
            popupDiscardLabel = BuildPopupButton(popup.transform, "DiscardButton", new Vector2(0.67f, 0.07f), new Vector2(0.95f, 0.25f), DiscardAction, armedColor);

            RefreshPopup();
        }

        private static TextMeshProUGUI BuildPopupButton(Transform parent, string name, Vector2 min, Vector2 max,
            UnityEngine.Events.UnityAction onClick, Color background)
        {
            Button button = UiRuntime.CreateButton(parent, name, string.Empty, min, max, onClick, background);
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.fontSize = 16f;
            }

            return label;
        }

        private void RefreshPopup()
        {
            if (activeItem == null || popupName == null)
            {
                return;
            }

            popupName.SetText(activeItem.ShortLabel);
            popupInfo.SetText(
                activeItem.Role.ToString().ToUpperInvariant() + " ONLY\n" +
                activeItem.SlotType.DisplayName() + "  ·  +" + activeItem.PercentText + " " + ItemSlotTypeExtensions.PrimaryStatLabel(activeItem.SlotType) + "\n" +
                "stage " + activeItem.Level + " drop      " + (activeItem.Price > 0d ? NumberFormatter.Format(activeItem.Price) + " gold" : string.Empty));

            popupEquipLabel.SetText(activeIsEquipped
                ? "UNEQUIP"
                : activeHeroIndex >= 0
                    ? "EQUIP  →  " + manager.Party.GetHero(activeHeroIndex).HeroName.ToUpperInvariant()
                    : "EQUIP");

            bool armed = armedInstanceId == activeItem.InstanceId;
            popupDiscardLabel.SetText(armed ? "TAP AGAIN" : "DISCARD");
            Button discard = FindButton("DiscardButton");
            if (discard != null)
            {
                discard.GetComponent<Image>().color = armed ? new Color(0.75f, 0.18f, 0.18f, 1f) : armedColor;
            }
        }

        private Button FindButton(string name)
        {
            if (popupRoot == null)
            {
                return null;
            }

            Transform found = popupRoot.transform.Find(name);
            return found != null ? found.GetComponent<Button>() : null;
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
                ok = activeHeroIndex >= 0 && manager.Gear.Equip(activeHeroIndex, activeItem.InstanceId, out message);
            }

            if (!ok)
            {
                GameEvents.RaiseToast(string.IsNullOrEmpty(message) ? "cannot do that" : message);
            }

            TearDownPopup();
            Rebuild();
        }

        /// <summary>Two-tap discard: the first tap arms the button, the second actually deletes.</summary>
        private void DiscardAction()
        {
            if (activeItem == null || manager.Gear == null)
            {
                return;
            }

            if (armedInstanceId == activeItem.InstanceId)
            {
                manager.Gear.Discard(activeItem.InstanceId, out _);
                TearDownPopup();
                Rebuild();
            }
            else
            {
                armedInstanceId = activeItem.InstanceId;
                RefreshPopup();
            }
        }

        private void TearDownPopup()
        {
            if (popupRoot != null)
            {
                Destroy(popupRoot);
                popupRoot = null;
            }

            activeItem = null;
            activeHeroIndex = -1;
            activeIsEquipped = false;
            armedInstanceId = null;
        }

        private void ClosePopup()
        {
            TearDownPopup();
        }
    }
}