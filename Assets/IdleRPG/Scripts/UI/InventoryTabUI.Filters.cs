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
    /// The bag's filter bar: the count label and the two dropdowns (slot filter + sort). Both write the filter
    /// state on <c>InventoryTabUI</c> and call <c>Rebuild()</c> in <c>InventoryTabUI.Grid</c>.
    /// </summary>
    public sealed partial class InventoryTabUI : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Filter bar (count + slot/sort dropdowns)
        // ------------------------------------------------------------------
        private void BuildHeader()
        {
            countLabel = UiRuntime.CreateText(root, "InventoryCount", string.Empty, UiType.Role.Small,
                TextAlignmentOptions.MidlineLeft, bodyText);
            UiRuntime.Anchor(countLabel.rectTransform, new Vector2(0.03f, 0.905f), new Vector2(0.60f, 0.96f), 0f, 0f, 0f, 0f);
        }

        private void BuildDropdowns()
        {
            Button slotBtn = UiRuntime.CreateButton(root, "SlotDropdown", string.Empty,
                new Vector2(0.03f, 0.85f), new Vector2(0.50f, 0.905f), null, dropdownColor);
            slotDropdownLabel = LabelOf(slotBtn, UiType.Role.Small);
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
            sortDropdownLabel = LabelOf(sortBtn, UiType.Role.Small);
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

        private static TextMeshProUGUI LabelOf(Button button, UiType.Role role)
        {
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                UiType.Apply(label, role);
            }

            return label;
        }

        private void RefreshDropdownLabels()
        {
            if (slotDropdownLabel != null)
            {
                slotDropdownLabel.SetText("SLOT: " + (slotFilter < 0 ? "ALL" : SlotOptionLabels[slotFilter + 1]) + " " + UiGlyphs.Forward);
            }

            if (sortDropdownLabel != null)
            {
                sortDropdownLabel.SetText("SORT: " + SortOptionLabels[sortMode] + " " + UiGlyphs.Forward);
            }
        }

    }
}
