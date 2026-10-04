using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Combat;
using IdleRPG.Core;
using IdleRPG.Equipment;
using IdleRPG.Data;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// The PARTY page: a two-tab workspace.
    ///
    /// ROSTER    - animated portraits (idle clips) with the selected hero large and its stats beside it.
    /// FORMATION - the board; the only place a hero is moved (PartyBoardUI), plus what the ranks do.
    ///
    /// ELI5: the changing room. Top strip = your heroes; tap one and it walks onto the big stand while its
    /// card shows next to it. The other tab is the pitch where you place them. Gear and ability rows are
    /// drawn but empty on purpose - the space is reserved so those systems drop in without a redesign.
    /// See Docs/Party-Page.md for the full vision and phasing.
    /// </summary>
    public sealed class PartyPanelUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private RectTransform boardRoot;
        [SerializeField] private Vector2 boardOffset = new Vector2(14f, -24f);

        [Header("Placeholders")]
        [Tooltip("Ability slots drawn but not usable yet (Step 13).")]
        [SerializeField] private int abilitySlotPlaceholders = 3;

        private readonly Color activeTabColor = new Color(0.24f, 0.34f, 0.55f, 1f);
        private readonly Color inactiveTabColor = new Color(0.11f, 0.13f, 0.19f, 1f);
        private readonly Color cardColor = new Color(0.16f, 0.19f, 0.28f, 0.95f);
        private readonly Color cardSelectedColor = new Color(0.30f, 0.42f, 0.62f, 1f);
        private readonly Color slotColor = new Color(0.14f, 0.16f, 0.23f, 0.95f);
        private readonly Color bodyText = new Color(1f, 1f, 1f, 0.92f);
        private readonly Color dimText = new Color(1f, 1f, 1f, 0.55f);
        private readonly Color hintColor = new Color(1f, 0.92f, 0.7f);
        private readonly Color accentColor = new Color(1f, 0.82f, 0.3f, 1f);

        private const int StatRowCount = 4;
        private static readonly string[] StatKeyNames = { "HP", "ATK", "DEF", "DPS" };

        private const int GearSlotCount = 3;
        private const int PickerPageSize = 4;
        private static readonly string[] GearSlotLetters = { "W", "A", "T" };
        private static readonly string[] GearSlotNames = { "Weapon", "Armor", "Trinket" };

        private static readonly Color RarityCommonColor = new Color(0.60f, 0.63f, 0.68f, 1f);
        private static readonly Color RarityRareColor = new Color(0.35f, 0.68f, 0.95f, 1f);
        private static readonly Color RarityEpicColor = new Color(0.72f, 0.52f, 0.92f, 1f);
        private static readonly Color RarityLegendaryColor = new Color(0.95f, 0.72f, 0.30f, 1f);
        private static readonly Color PickerRowColor = new Color(0.14f, 0.16f, 0.23f, 0.95f);
        private static readonly Color PickerRowArmedColor = new Color(0.52f, 0.16f, 0.16f, 0.95f);

        private GameManager manager;
        private PartyBoardUI board;

        private Image[] tabBackgrounds;
        private GameObject[] tabPanels;
        private int activeTab;

        private Image[] cardBackgrounds;
        private Image[] cardIcons;
        private CharacterAnimator[] cardAnimators;
        private TextMeshProUGUI[] cardNameLabels;

        private Image bigPortrait;
        private CharacterAnimator bigAnimator;
        private TextMeshProUGUI bigNameLabel;
        private TextMeshProUGUI roleLabel;
        private TextMeshProUGUI[] statKeys;
        private TextMeshProUGUI[] statValues;
        private TextMeshProUGUI formationLabel;

        // --- Gear (3 typed slots + inventory picker) ---
        private Image[] gearSlotBackgrounds;
        private TextMeshProUGUI[] gearSlotStatLabels;
        private TextMeshProUGUI inventoryButtonLabel;
        private GameObject pickerRoot;
        private string pickerTitle;
        private int pickerFilterSlot = -1;
        private int pickerPage;
        private string pickerArmedInstanceId;

        private int selectedHeroIndex = -1;
        private int boundBigHero = -2;   // -2 = never bound, -1 = bound to "nothing"

        private void Start()
        {
            Build();
        }

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
            BuildRoster((RectTransform)tabPanels[0].transform);
            BuildFormation((RectTransform)tabPanels[1].transform);

            ShowTab(0);

            manager.Formation.Changed += RefreshAll;
            if (manager.Gear != null)
            {
                manager.Gear.Changed += RefreshAll;
            }
            built = true;

            // Open on the first real hero so the stand + card are never empty on arrival.
            if (selectedHeroIndex < 0)
            {
                selectedHeroIndex = FirstHeroIndex();
            }

            RefreshAll();
        }

        private int FirstHeroIndex()
        {
            for (int i = 0; i < manager.Party.Heroes.Count; i++)
            {
                if (manager.Party.GetHero(i) != null)
                {
                    return i;
                }
            }

            return -1;
        }

        // ------------------------------------------------------------------
        // Sub-navigation (Roster | Formation)
        // ------------------------------------------------------------------
        private void BuildTabs(RectTransform root)
        {
            string[] names = { "ROSTER", "FORMATION" };
            tabBackgrounds = new Image[names.Length];
            tabPanels = new GameObject[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                float xMin = i == 0 ? 0.03f : 0.51f;
                float xMax = i == 0 ? 0.49f : 0.97f;
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
            if (activeTab != index && index != 0)
            {
                ClosePicker();
            }

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
        // Roster
        // ------------------------------------------------------------------
        private void BuildRoster(RectTransform root)
        {
            int heroes = manager.Party.Heroes.Count;
            cardBackgrounds = new Image[heroes];
            cardIcons = new Image[heroes];
            cardAnimators = new CharacterAnimator[heroes];
            cardNameLabels = new TextMeshProUGUI[heroes];

            const float cardIconPx = 78f;

            for (int i = 0; i < heroes; i++)
            {
                float xMin = 0.03f + i * 0.325f;
                float xMax = xMin + 0.30f;
                int captured = i;

                Button card = UiRuntime.CreateButton(root, "Card" + i, string.Empty,
                    new Vector2(xMin, 0.79f), new Vector2(xMax, 0.99f), () => SelectHero(captured), cardColor);
                cardBackgrounds[i] = card != null ? card.GetComponent<Image>() : null;
                Transform cardRoot = card != null ? card.transform : root;

                Image icon = UiRuntime.CreateIcon("Portrait", cardRoot);
                UiRuntime.CenterOn(icon.rectTransform, new Vector2(0.5f, 0.63f), new Vector2(cardIconPx, cardIconPx));
                icon.preserveAspect = true;

                CharacterAnimator animator = icon.gameObject.AddComponent<CharacterAnimator>();
                animator.SetSlotSize(new Vector2(cardIconPx, cardIconPx));
                animator.FacingLeft = false;
                cardIcons[i] = icon;
                cardAnimators[i] = animator;

                TextMeshProUGUI name = UiRuntime.CreateText(cardRoot, "Name", string.Empty, 16f,
                    TextAlignmentOptions.Center, Color.white);
                UiRuntime.Anchor(name.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.32f), 2f, 0f, 2f, 0f);
                cardNameLabels[i] = name;
            }

            // The selected hero, big, on the left - idle animation playing.
            bigPortrait = UiRuntime.CreateIcon("BigPortrait", root);
            UiRuntime.CenterOn(bigPortrait.rectTransform, new Vector2(0.24f, 0.52f), new Vector2(300f, 340f));
            bigPortrait.preserveAspect = true;
            bigAnimator = bigPortrait.gameObject.AddComponent<CharacterAnimator>();
            bigAnimator.SetSlotSize(new Vector2(300f, 340f));
            bigAnimator.FacingLeft = false;

            bigNameLabel = UiRuntime.CreateText(root, "BigName", string.Empty, 22f,
                TextAlignmentOptions.Center, Color.white);
            UiRuntime.Anchor(bigNameLabel.rectTransform, new Vector2(0.02f, 0.28f), new Vector2(0.46f, 0.36f), 6f, 0f, 6f, 0f);

            // Its card, on the right: role + stat rows up top, gear/abilities reserved below it.
            Image statsPanel = UiRuntime.CreatePanel(root, null, cardColor);
            UiRuntime.Anchor(statsPanel.rectTransform, new Vector2(0.48f, 0.44f), new Vector2(0.97f, 0.795f));

            roleLabel = UiRuntime.CreateText(root, "Role", string.Empty, 15f,
                TextAlignmentOptions.MidlineLeft, accentColor);
            UiRuntime.Anchor(roleLabel.rectTransform, new Vector2(0.50f, 0.745f), new Vector2(0.95f, 0.783f), 2f, 0f, 0f, 0f);

            statKeys = new TextMeshProUGUI[StatRowCount];
            statValues = new TextMeshProUGUI[StatRowCount];

            for (int i = 0; i < StatRowCount; i++)
            {
                float yTop = 0.68f - i * 0.055f;

                statKeys[i] = UiRuntime.CreateText(root, "Key" + StatKeyNames[i], StatKeyNames[i], 17f,
                    TextAlignmentOptions.MidlineLeft, dimText);
                UiRuntime.Anchor(statKeys[i].rectTransform, new Vector2(0.50f, yTop), new Vector2(0.68f, yTop + 0.045f), 0f, 0f, 0f, 0f);

                statValues[i] = UiRuntime.CreateText(root, "Val" + StatKeyNames[i], string.Empty, 17f,
                    TextAlignmentOptions.MidlineRight, bodyText);
                UiRuntime.Anchor(statValues[i].rectTransform, new Vector2(0.70f, yTop), new Vector2(0.95f, yTop + 0.045f), 0f, 0f, 0f, 0f);
            }

            BuildGearRow(root);
            BuildSlotRow(root, "ABILITIES", abilitySlotPlaceholders, 0.175f, 0.255f);
        }

        /// <summary>A caption plus a row of empty, locked slots (gear / abilities, filled by later steps).</summary>
        private void BuildSlotRow(RectTransform root, string caption, int count, float yMin, float yMax)
        {
            TextMeshProUGUI label = UiRuntime.CreateText(root, caption + "Caption", caption, 15f,
                TextAlignmentOptions.MidlineLeft, dimText);
            UiRuntime.Anchor(label.rectTransform, new Vector2(0.48f, yMax), new Vector2(0.72f, yMax + 0.035f), 4f, 0f, 0f, 0f);

            TextMeshProUGUI note = UiRuntime.CreateText(root, caption + "Note", "(coming soon)", 14f,
                TextAlignmentOptions.MidlineRight, dimText);
            UiRuntime.Anchor(note.rectTransform, new Vector2(0.60f, yMax), new Vector2(0.97f, yMax + 0.035f), 0f, 0f, 4f, 0f);

            const float xMin = 0.48f;
            const float xMax = 0.97f;
            const float gap = 0.012f;
            float width = (xMax - xMin - gap * (count - 1)) / Mathf.Max(1, count);

            for (int i = 0; i < count; i++)
            {
                float x0 = xMin + i * (width + gap);
                Image slot = UiRuntime.CreatePanel(root, null, slotColor);
                UiRuntime.Anchor(slot.rectTransform, new Vector2(x0, yMin), new Vector2(x0 + width, yMax - 0.005f));
            }
        }

        // ------------------------------------------------------------------
// ------------------------------------------------------------------
        // Gear: 3 typed slots + inventory picker
        // ------------------------------------------------------------------
        /// <summary>The GEAR row: three typed slots (Weapon/Armor/Trinket) + the inventory caption button.</summary>
        private void BuildGearRow(RectTransform root)
        {
            TextMeshProUGUI caption = UiRuntime.CreateText(root, "GearCaption", "GEAR", 15f,
                TextAlignmentOptions.MidlineLeft, dimText);
            UiRuntime.Anchor(caption.rectTransform, new Vector2(0.48f, 0.415f), new Vector2(0.72f, 0.45f), 4f, 0f, 0f, 0f);

            Button inventoryButton = UiRuntime.CreateButton(root, "InventoryButton", string.Empty,
                new Vector2(0.60f, 0.415f), new Vector2(0.97f, 0.45f), OpenInventoryPicker,
                new Color(0.10f, 0.12f, 0.18f, 0.9f));
            inventoryButtonLabel = inventoryButton != null ? inventoryButton.GetComponentInChildren<TextMeshProUGUI>() : null;

            const float xMin = 0.48f;
            const float xMax = 0.97f;
            const float gap = 0.012f;
            float width = (xMax - xMin - gap * (GearSlotCount - 1)) / GearSlotCount;

            gearSlotBackgrounds = new Image[GearSlotCount];
            gearSlotStatLabels = new TextMeshProUGUI[GearSlotCount];

            for (int i = 0; i < GearSlotCount; i++)
            {
                float x0 = xMin + i * (width + gap);
                int captured = i;

                Button slot = UiRuntime.CreateButton(root, "GearSlot" + i, GearSlotLetters[i],
                    new Vector2(x0, 0.335f), new Vector2(x0 + width, 0.412f), () => OpenSlotPicker(captured), slotColor);
                gearSlotBackgrounds[i] = slot != null ? slot.GetComponent<Image>() : null;

                TextMeshProUGUI stat = UiRuntime.CreateText(slot.transform, "Stat", "empty", 12f,
                    TextAlignmentOptions.Center, dimText);
                UiRuntime.Anchor(stat.rectTransform, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.30f), 0f, 0f, 0f, 0f);
                gearSlotStatLabels[i] = stat;
            }
        }

        /// <summary>Refreshes the 3 gear slots and the inventory count for the selected hero.</summary>
        private void RefreshGear()
        {
            if (manager == null || manager.Gear == null || gearSlotBackgrounds == null)
            {
                return;
            }

            for (int i = 0; i < GearSlotCount; i++)
            {
                ItemInstance item = selectedHeroIndex >= 0
                    ? manager.Gear.GetEquipped(selectedHeroIndex, (ItemSlotType)i)
                    : null;

                gearSlotBackgrounds[i].color = item != null ? RarityColor(item.Rarity) : slotColor;

                if (gearSlotStatLabels[i] != null)
                {
                    gearSlotStatLabels[i].color = item != null ? Color.white : dimText;
                    gearSlotStatLabels[i].SetText(item != null
                        ? "+" + item.PercentText + " " + item.SlotType.PrimaryStatLabel()
                        : "empty");
                }
            }

            if (inventoryButtonLabel != null)
            {
                inventoryButtonLabel.SetText("INVENTORY " + manager.Gear.InventoryCount + "/" + manager.Gear.InventoryCap);
            }
        }

        private static Color RarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare:
                    return RarityRareColor;
                case ItemRarity.Epic:
                    return RarityEpicColor;
                case ItemRarity.Legendary:
                    return RarityLegendaryColor;
                default:
                    return RarityCommonColor;
            }
        }
// ------------------------------------------------------------------
        // Inventory picker (slot picker + all-items picker share one drawer)
        // ------------------------------------------------------------------
        private void OpenSlotPicker(int slotIndex)
        {
            if (manager == null || manager.Gear == null || selectedHeroIndex < 0)
            {
                return;
            }

            if (manager.Party.GetHero(selectedHeroIndex) == null)
            {
                return;
            }

            pickerFilterSlot = slotIndex;
            pickerPage = 0;
            pickerArmedInstanceId = null;
            pickerTitle = GearSlotNames[slotIndex] + "  -  tap an item to equip";
            BuildPicker();
        }

        private void OpenInventoryPicker()
        {
            if (manager == null || manager.Gear == null)
            {
                return;
            }

            pickerFilterSlot = -1;
            pickerPage = 0;
            pickerArmedInstanceId = null;
            pickerTitle = "INVENTORY  -  tap to equip,  X twice to discard";
            BuildPicker();
        }

        private void ClosePicker()
        {
            TearDownPicker();
            pickerArmedInstanceId = null;
            pickerTitle = null;
        }

        private void TearDownPicker()
        {
            if (pickerRoot != null)
            {
                Destroy(pickerRoot.gameObject);
                pickerRoot = null;
            }
        }

        private void BuildPicker()
        {
            TearDownPicker();

            if (tabPanels == null || pickerTitle == null)
            {
                return;
            }

            GameObject panel = UiRuntime.CreateNode("GearPicker", tabPanels[0].transform);
            UiRuntime.Anchor(panel.GetComponent<RectTransform>(), new Vector2(0.47f, 0.09f), new Vector2(0.98f, 0.335f));
            panel.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.15f, 0.97f);
            pickerRoot = panel;

            TextMeshProUGUI title = UiRuntime.CreateText(panel.transform, "Title", pickerTitle, 14f,
                TextAlignmentOptions.MidlineLeft, hintColor);
            UiRuntime.Anchor(title.rectTransform, new Vector2(0.02f, 0.855f), new Vector2(0.70f, 0.985f), 4f, 0f, 0f, 0f);

            Button prev = UiRuntime.CreateButton(panel.transform, "Prev", "<", new Vector2(0.72f, 0.86f), new Vector2(0.80f, 0.985f),
                () =>
                {
                    pickerPage = Mathf.Max(0, pickerPage - PickerPageSize);
                    BuildPicker();
                }, new Color(0.20f, 0.24f, 0.34f, 1f));
            TextMeshProUGUI prevLabel = prev.GetComponentInChildren<TextMeshProUGUI>();
            if (prevLabel != null)
            {
                prevLabel.fontSize = 16f;
            }

            Button next = UiRuntime.CreateButton(panel.transform, "Next", ">", new Vector2(0.81f, 0.86f), new Vector2(0.89f, 0.985f),
                () =>
                {
                    pickerPage += PickerPageSize;
                    BuildPicker();
                }, new Color(0.20f, 0.24f, 0.34f, 1f));
            TextMeshProUGUI nextLabel = next.GetComponentInChildren<TextMeshProUGUI>();
            if (nextLabel != null)
            {
                nextLabel.fontSize = 16f;
            }

            Button close = UiRuntime.CreateButton(panel.transform, "Close", "X", new Vector2(0.90f, 0.86f), new Vector2(0.98f, 0.985f),
                ClosePicker, new Color(0.20f, 0.24f, 0.34f, 1f));
            TextMeshProUGUI closeLabel = close.GetComponentInChildren<TextMeshProUGUI>();
            if (closeLabel != null)
            {
                closeLabel.fontSize = 14f;
            }
List<ItemInstance> items = PickerItems();
            int totalPages = Mathf.Max(1, (items.Count + PickerPageSize - 1) / PickerPageSize);
            int pageIndex = Mathf.Clamp(pickerPage / PickerPageSize, 0, totalPages - 1);
            pickerPage = pageIndex * PickerPageSize;

            TextMeshProUGUI pageLabel = UiRuntime.CreateText(panel.transform, "Page", (pageIndex + 1) + "/" + totalPages, 12f,
                TextAlignmentOptions.Center, dimText);
            UiRuntime.Anchor(pageLabel.rectTransform, new Vector2(0.72f, 0.70f), new Vector2(0.89f, 0.80f), 0f, 0f, 0f, 0f);

            int shown = 0;
            for (int i = pickerPage; i < items.Count && shown < PickerPageSize; i++, shown++)
            {
                ItemInstance item = items[i];
                float yTop = 0.70f - shown * 0.155f;
                float yBottom = yTop - 0.125f;

                Color rowColor = pickerArmedInstanceId == item.InstanceId ? PickerRowArmedColor : PickerRowColor;
                Button row = UiRuntime.CreateButton(panel.transform, "Row" + i, string.Empty,
                    new Vector2(0.02f, yBottom), new Vector2(0.98f, yTop), () => EquipFromPicker(item), rowColor);
                TextMeshProUGUI rowText = UiRuntime.CreateText(row.transform, "Item", string.Empty, 13f,
                    TextAlignmentOptions.MidlineLeft, bodyText);
                UiRuntime.Anchor(rowText.rectTransform, new Vector2(0.04f, 0f), new Vector2(0.80f, 1f), 4f, 0f, 0f, 0f);
                bool usable = selectedHeroIndex >= 0
                    && manager.Party.GetHero(selectedHeroIndex) != null
                    && manager.Party.GetHero(selectedHeroIndex).Role == item.Role;
                rowText.SetText(item.ShortLabel + "  +" + item.PercentText + " " + item.SlotType.PrimaryStatLabel()
                    + (pickerFilterSlot < 0 ? "  (" + item.Role + ")" : string.Empty));
                rowText.color = usable ? Color.white : dimText;

                Button trash = UiRuntime.CreateButton(row.transform, "Trash", pickerArmedInstanceId == item.InstanceId ? "again?" : "X",
                    new Vector2(0.84f, 0.12f), new Vector2(0.97f, 0.88f), () => ToggleDiscard(item), new Color(0.32f, 0.20f, 0.20f, 0.95f));
                TextMeshProUGUI trashLabel = trash.GetComponentInChildren<TextMeshProUGUI>();
                if (trashLabel != null)
                {
                    trashLabel.fontSize = 11f;
                }
            }

            if (items.Count == 0)
            {
                TextMeshProUGUI empty = UiRuntime.CreateText(panel.transform, "Empty", "no items here yet", 13f,
                    TextAlignmentOptions.Center, dimText);
                UiRuntime.Anchor(empty.rectTransform, new Vector2(0.02f, 0.40f), new Vector2(0.98f, 0.60f), 0f, 0f, 0f, 0f);
            }
        }

        private List<ItemInstance> PickerItems()
        {
            List<ItemInstance> list = new List<ItemInstance>();
            if (manager == null || manager.Gear == null)
            {
                return list;
            }

            IReadOnlyList<ItemInstance> inventory = manager.Gear.Inventory;
            for (int i = 0; i < inventory.Count; i++)
            {
                ItemInstance item = inventory[i];
                if (item == null)
                {
                    continue;
                }

                if (pickerFilterSlot >= 0)
                {
                    HeroData hero = selectedHeroIndex >= 0 ? manager.Party.GetHero(selectedHeroIndex) : null;
                    if (hero == null || (int)item.SlotType != pickerFilterSlot || item.Role != hero.Role)
                    {
                        continue;
                    }
                }

                list.Add(item);
            }

            return list;
        }

        private void EquipFromPicker(ItemInstance item)
        {
            if (manager.Gear.Equip(selectedHeroIndex, item.InstanceId, out string message))
            {
                ClosePicker();
                RefreshAll();
            }
            else
            {
                GameEvents.RaiseToast(message);
            }
        }

        /// <summary>Two-tap delete: first tap arms the row (red), the second actually discards.</summary>
        private void ToggleDiscard(ItemInstance item)
        {
            if (pickerArmedInstanceId == item.InstanceId)
            {
                manager.Gear.Discard(item.InstanceId, out _);
                pickerArmedInstanceId = null;
                BuildPicker();
            }
            else
            {
                pickerArmedInstanceId = item.InstanceId;
                BuildPicker();
            }
        }

        // ------------------------------------------------------------------
        // Formation
        // ------------------------------------------------------------------
        // Formation
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
        // Selection + refresh
        // ------------------------------------------------------------------
        private void SelectHero(int heroIndex)
        {
            selectedHeroIndex = heroIndex;
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (manager == null || manager.Formation == null || manager.Party == null || tabPanels == null)
            {
                return;
            }

            RefreshTabVisuals();
            RefreshCards();
            RefreshBigPortrait();
            RefreshStats();
            RefreshFormationText();
            RefreshGear();
        }

        /// <summary>Binds each card's idle clip once; later refreshes only touch colours and names.</summary>
        private void RefreshCards()
        {
            if (cardAnimators == null)
            {
                return;
            }

            for (int i = 0; i < cardAnimators.Length; i++)
            {
                if (cardNameLabels[i] != null)
                {
                    cardNameLabels[i].color = i == selectedHeroIndex ? new Color(1f, 0.9f, 0.5f) : Color.white;
                }

                if (cardBackgrounds[i] != null)
                {
                    cardBackgrounds[i].color = i == selectedHeroIndex ? cardSelectedColor : cardColor;
                }

                if (cardAnimators[i] == null || cardIcons[i] == null)
                {
                    continue;
                }

                HeroData hero = manager.Party.GetHero(i);

                if (hero == null)
                {
                    cardAnimators[i].Clear();
                    if (cardNameLabels[i] != null) cardNameLabels[i].SetText("empty");
                    continue;
                }

                if (cardNameLabels[i] != null) cardNameLabels[i].SetText(hero.HeroName);

                if (!cardAnimators[i].HasArt)
                {
                    if (hero.ArtSet != null) cardAnimators[i].SetArt(hero.ArtSet);
                    else if (hero.HeroIcon != null) cardIcons[i].sprite = hero.HeroIcon;
                }
            }
        }

        /// <summary>Rebinds the big portrait only when the SELECTED HERO changes - taps must not resize it.</summary>
        private void RefreshBigPortrait()
        {
            if (bigAnimator == null || bigPortrait == null || bigNameLabel == null)
            {
                return;
            }

            if (boundBigHero == selectedHeroIndex)
            {
                return;
            }

            boundBigHero = selectedHeroIndex;

            HeroData hero = selectedHeroIndex >= 0 ? manager.Party.GetHero(selectedHeroIndex) : null;

            if (hero == null)
            {
                bigAnimator.Clear();
                bigNameLabel.SetText(string.Empty);
                return;
            }

            bigNameLabel.SetText(hero.HeroName);

            if (hero.ArtSet != null)
            {
                bigAnimator.SetArt(hero.ArtSet);
            }
            else
            {
                bigAnimator.Clear();
                if (hero.HeroIcon != null) bigPortrait.sprite = hero.HeroIcon;
            }
        }

        private void RefreshStats()
        {
            if (statValues == null || statKeys == null || roleLabel == null)
            {
                return;
            }

            if (selectedHeroIndex < 0 || manager.Party.GetHero(selectedHeroIndex) == null)
            {
                roleLabel.SetText("select a hero");
                for (int i = 0; i < statValues.Length; i++)
                {
                    statValues[i].SetText("-");
                }
                return;
            }

            HeroData hero = manager.Party.GetHero(selectedHeroIndex);
            double health = Resolve(i => manager.Resolver.GetMaxHealth(hero, i), selectedHeroIndex, hero.BaseHealth);
            double attack = Resolve(i => manager.Resolver.GetAttack(hero, i), selectedHeroIndex, hero.BaseAttack);
            double defense = Resolve(i => manager.Resolver.GetDefense(hero, i), selectedHeroIndex, hero.BaseDefense);
            double dps = hero.AttackIntervalSec > 0.05d ? attack / hero.AttackIntervalSec : attack;

            roleLabel.SetText(hero.Role.ToString().ToUpperInvariant());
            statValues[0].SetText(NumberFormatter.Format(health));
            statValues[1].SetText(NumberFormatter.Format(attack));
            statValues[2].SetText(NumberFormatter.Format(defense));
            statValues[3].SetText(NumberFormatter.Format(dps));
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
    }
}
