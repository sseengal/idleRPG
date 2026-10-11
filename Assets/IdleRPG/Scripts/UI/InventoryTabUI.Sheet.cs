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
    /// The docked, non-modal item sheet: build it for one item, refresh it, equip it, and the two-tap discard
    /// (first tap arms, second deletes). Only the sheet's own rect eats taps - the bag behind stays usable.
    /// </summary>
    public sealed partial class InventoryTabUI : MonoBehaviour
    {
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
            TextMeshProUGUI letter = UiRuntime.CreateText(content, "Letter", ItemVisuals.SlotLetter(item.SlotType), UiType.Role.Title,
                TextAlignmentOptions.Center, Color.white);
            UiRuntime.Anchor(letter.rectTransform, new Vector2(0.05f, 0.30f), new Vector2(0.31f, 0.62f), 0f, 0f, 0f, 0f);

            sheetName = UiRuntime.CreateText(content, "Name", string.Empty, UiType.Role.Section,
                TextAlignmentOptions.MidlineLeft, Color.white);
            // Item names vary ("Rare Helm" vs "Legendary Weapon"), so the name shrinks to fit its cell.
            UiType.ApplyAuto(sheetName, UiType.Role.Section);
            UiRuntime.Anchor(sheetName.rectTransform, new Vector2(0.36f, 0.56f), new Vector2(0.84f, 0.66f), 0f, 0f, 0f, 0f);

            sheetInfo = UiRuntime.CreateText(content, "Info", string.Empty, UiType.Role.Sentence,
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
                UiType.Apply(label, UiType.Role.Section);
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
                    ? "EQUIP  " + UiGlyphs.Forward + "  " + manager.Party.GetHero(activeHeroIndex).HeroName.ToUpperInvariant()
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
