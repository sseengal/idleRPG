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
    /// The bag's fixed 4x5 grid: which items are visible under the current filter and sort, and one square tile
    /// per item (rarity colour, slot letter, stat line, equipped ring and badge).
    /// </summary>
    public sealed partial class InventoryTabUI : MonoBehaviour
    {
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
                TextMeshProUGUI empty = UiRuntime.CreateText(tileNode.transform, "Empty", "empty", UiType.Role.Micro,
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

            TextMeshProUGUI letter = UiRuntime.CreateText(tile.transform, "Letter", ItemVisuals.SlotLetter(item.SlotType), UiType.Role.Title,
                TextAlignmentOptions.Center, Color.white);
            UiRuntime.Stretch(letter.rectTransform, 0f, 18f, 0f, 8f);

            TextMeshProUGUI stat = UiRuntime.CreateText(tile.transform, "Stat",
                "+" + item.PercentText + " " + ItemVisuals.StatLabel(item.SlotType), UiType.Role.Micro,
                TextAlignmentOptions.Center, bodyText);
            UiType.ApplyAuto(stat, UiType.Role.Micro);
            UiRuntime.Anchor(stat.rectTransform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.22f), 0f, 0f, 0f, 0f);

            TextMeshProUGUI classTag = UiRuntime.CreateText(tile.transform, "Role", ItemVisuals.RoleLetter(item.Role).ToString(), UiType.Role.Micro,
                TextAlignmentOptions.MidlineRight, dimText);
            UiRuntime.Anchor(classTag.rectTransform, new Vector2(0.72f, 0.78f), new Vector2(0.97f, 0.93f), 0f, 0f, 0f, 0f);

            if (equipped)
            {
                Image badge = UiRuntime.CreatePanel(tile.transform, null, new Color(0.24f, 0.34f, 0.55f, 1f));
                UiRuntime.Anchor(badge.rectTransform, new Vector2(0.02f, 0.78f), new Vector2(0.14f, 0.95f), 0f, 0f, 0f, 0f);
                TextMeshProUGUI e = UiRuntime.CreateText(tile.transform, "EBadge", "E", UiType.Role.Micro, TextAlignmentOptions.Center, Color.white);
                UiRuntime.Anchor(e.rectTransform, new Vector2(0.02f, 0.78f), new Vector2(0.14f, 0.95f), 0f, 0f, 0f, 0f);
            }
        }

    }
}
