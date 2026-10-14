using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Combat;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Equipment;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// The ROSTER tab's structure: the hero cards (idle clip + name) and the three equipped-gear tiles.
    /// Every widget is created once here - <c>PartyRosterView.Refresh</c> only fills them afterwards.
    /// </summary>
    public sealed partial class PartyRosterView : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Cards + big portrait + stats
        // ------------------------------------------------------------------
        private void BuildHeroCards(RectTransform root)
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

                TextMeshProUGUI name = UiRuntime.CreateText(cardRoot, "Name", string.Empty, UiType.Role.Small,
                    TextAlignmentOptions.Center, Color.white);
                UiType.ApplyAuto(name, UiType.Role.Small);
                UiRuntime.Anchor(name.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.32f), UiSpace.Hairline, UiSpace.None, UiSpace.Hairline, UiSpace.None);
                cardNameLabels[i] = name;
            }

            bigPortrait = UiRuntime.CreateIcon("BigPortrait", root);
            UiRuntime.CenterOn(bigPortrait.rectTransform, new Vector2(0.24f, 0.52f), new Vector2(300f, 340f));
            bigPortrait.preserveAspect = true;
            bigAnimator = bigPortrait.gameObject.AddComponent<CharacterAnimator>();
            bigAnimator.SetSlotSize(new Vector2(300f, 340f));
            bigAnimator.FacingLeft = false;

            bigNameLabel = UiRuntime.CreateText(root, "BigName", string.Empty, UiType.Role.Section,
                TextAlignmentOptions.Center, Color.white);
            UiRuntime.Anchor(bigNameLabel.rectTransform, new Vector2(0.02f, 0.28f), new Vector2(0.46f, 0.36f), UiSpace.Snug, UiSpace.None, UiSpace.Snug, UiSpace.None);

            Image statsPanel = UiRuntime.CreatePanel(root, null, cardColor);
            UiRuntime.Anchor(statsPanel.rectTransform, new Vector2(0.48f, 0.44f), new Vector2(0.97f, 0.795f));

            roleLabel = UiRuntime.CreateText(root, "Role", string.Empty, UiType.Role.Small,
                TextAlignmentOptions.MidlineLeft, accentColor);
            UiRuntime.Anchor(roleLabel.rectTransform, new Vector2(0.50f, 0.745f), new Vector2(0.95f, 0.783f), UiSpace.Hairline, UiSpace.None, UiSpace.None, UiSpace.None);

            statKeys = new TextMeshProUGUI[Rows.Length];
            statValues = new TextMeshProUGUI[Rows.Length];
            statBonusLabels = new TextMeshProUGUI[Rows.Length];

            // The sheet band: roles sit above, gear below. The pitch adapts to the row count, so adding a stat
            // never means re-anchoring anything.
            const float rowsTop = 0.72f;
            const float rowsBottom = 0.47f;
            float pitch = (rowsTop - rowsBottom) / Rows.Length;
            float rowHeight = pitch * 0.82f;

            for (int i = 0; i < Rows.Length; i++)
            {
                // Bottom of this row, climbing as the index grows (anchors are bottom-based).
                float yBottom = rowsTop - (i + 1) * pitch;

                statKeys[i] = UiRuntime.CreateText(root, "Key" + i, Rows[i].Key, UiType.Role.Small,
                    TextAlignmentOptions.MidlineLeft, dimText);
                // The key column is only about 124 units wide and the keys vary ("HP" vs "CRIT DMG"), so they
                // shrink to fit rather than running into the value column.
                UiType.ApplyAuto(statKeys[i], UiType.Role.Small);
                UiRuntime.Anchor(statKeys[i].rectTransform, new Vector2(0.50f, yBottom), new Vector2(0.62f, yBottom + rowHeight), UiSpace.Hairline, UiSpace.None, UiSpace.None, UiSpace.None);

                statValues[i] = UiRuntime.CreateText(root, "Val" + i, string.Empty, UiType.Role.Small,
                    TextAlignmentOptions.MidlineRight, bodyText);
                UiType.ApplyAuto(statValues[i], UiType.Role.Small);
                UiRuntime.Anchor(statValues[i].rectTransform, new Vector2(0.62f, yBottom), new Vector2(0.79f, yBottom + rowHeight), 0f, 0f, 0f, 0f);

                statBonusLabels[i] = UiRuntime.CreateText(root, "Bonus" + i, string.Empty, UiType.Role.Micro,
                    TextAlignmentOptions.MidlineRight, bonusColor);
                UiRuntime.Anchor(statBonusLabels[i].rectTransform, new Vector2(0.79f, yBottom), new Vector2(0.95f, yBottom + rowHeight), 0f, 0f, 0f, 0f);
            }
        }

        // ------------------------------------------------------------------
        // Equipped gear row (view only)
        // ------------------------------------------------------------------
        private void BuildGearRow(RectTransform root)
        {
            TextMeshProUGUI caption = UiRuntime.CreateText(root, "GearCaption", "GEAR (equipped)", UiType.Role.Small,
                TextAlignmentOptions.MidlineLeft, dimText);
            // The caption's cell is about 249 units wide and the word is long, so it shrinks to fit.
            UiType.ApplyAuto(caption, UiType.Role.Small);
            UiRuntime.Anchor(caption.rectTransform, new Vector2(0.48f, 0.415f), new Vector2(0.72f, 0.45f), UiSpace.Tight, UiSpace.None, UiSpace.None, UiSpace.None);

            TextMeshProUGUI hint = UiRuntime.CreateText(root, "GearHint", "gear: coming soon", UiType.Role.Micro,
                TextAlignmentOptions.MidlineRight, dimText);
            UiRuntime.Anchor(hint.rectTransform, new Vector2(0.62f, 0.415f), new Vector2(0.97f, 0.45f), UiSpace.None, UiSpace.None, UiSpace.Tight, UiSpace.None);

            const float xMin = 0.48f;
            const float xMax = 0.97f;
            const float gap = 0.012f;
            float width = (xMax - xMin - gap * (GearSlotCount - 1)) / GearSlotCount;

            gearSlotBackgrounds = new Image[GearSlotCount];
            gearSlotStatLabels = new TextMeshProUGUI[GearSlotCount];

            for (int i = 0; i < GearSlotCount; i++)
            {
                float x0 = xMin + i * (width + gap);
                Image slot = UiRuntime.CreatePanel(root, null, slotColor);
                UiRuntime.Anchor(slot.rectTransform, new Vector2(x0, 0.335f), new Vector2(x0 + width, 0.412f));
                gearSlotBackgrounds[i] = slot;

                TextMeshProUGUI letter = UiRuntime.CreateText(slot.transform, "Letter", ItemVisuals.SlotLetter((ItemSlotType)i), UiType.Role.Section,
                    TextAlignmentOptions.Center, Color.white);
                UiRuntime.Anchor(letter.rectTransform, new Vector2(0.02f, 0.38f), new Vector2(0.98f, 0.95f), 0f, 0f, 0f, 0f);

                TextMeshProUGUI stat = UiRuntime.CreateText(slot.transform, "Stat", "empty", UiType.Role.Micro,
                    TextAlignmentOptions.Center, dimText);
                UiRuntime.Anchor(stat.rectTransform, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.30f), 0f, 0f, 0f, 0f);
                gearSlotStatLabels[i] = stat;
            }
        }

    }
}
