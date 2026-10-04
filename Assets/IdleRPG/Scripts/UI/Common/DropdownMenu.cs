using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// A reusable dropdown: an opening button + a transient option menu handled by a full-page catcher
    /// (tap outside dismisses) with same-frame close, a pressed tint on the open button, and its rows built
    /// as later siblings of the overlay so they always sit above. One instance per dropdown.
    /// </summary>
    public sealed class DropdownMenu
    {
        private readonly Transform page;
        private readonly bool alignRight;
        private readonly string[] labels;
        private readonly Action<int> onSelect;
        private readonly Button openButton;
        private readonly Image openButtonImage;
        private readonly Color idleColor;
        private readonly Color activeColor;
        private readonly Func<int, bool> selectedChecker;

        private GameObject overlay;
        private bool isOpen;

        /// <summary>
        /// Wraps an existing opening button. <paramref name="page"/> is where the transient overlay is
        /// parented (the page root), <paramref name="onSelect"/> receives the option index (0-based).
        /// </summary>
        public DropdownMenu(Transform page, Button openButton, bool alignRight, string[] labels,
            Action<int> onSelect, Color idleColor, Color activeColor, Func<int, bool> selectedChecker = null)
        {
            this.page = page;
            this.alignRight = alignRight;
            this.labels = labels;
            this.onSelect = onSelect;
            this.openButton = openButton;
            this.openButtonImage = openButton != null ? openButton.GetComponent<Image>() : null;
            this.idleColor = idleColor;
            this.activeColor = activeColor;
            this.selectedChecker = selectedChecker;

            openButton.onClick.AddListener(Toggle);
        }

        public bool IsOpen => isOpen;

        public void Toggle()
        {
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Close()
        {
            if (overlay != null)
            {
                UiTransient.Close(overlay);
                overlay = null;
            }

            if (openButtonImage != null)
            {
                openButtonImage.color = idleColor;
            }

            isOpen = false;
        }

        private void Open()
        {
            Close(); // never two menus

            isOpen = true;
            if (openButtonImage != null)
            {
                openButtonImage.color = activeColor;
            }

            overlay = new GameObject("DropdownOverlay", typeof(RectTransform));
            overlay.transform.SetParent(page, false);
            UiRuntime.Anchor(overlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            UiTransient.CreateTapCatcher(overlay.transform, Close);

            float xMin = alignRight ? 0.52f : 0.03f;
            float xMax = alignRight ? 0.97f : 0.50f;
            float rowHeight = 0.055f;
            float panelHeight = labels.Length * rowHeight + 0.008f;

            GameObject panel = new GameObject("OptionsPanel", typeof(RectTransform));
            panel.transform.SetParent(overlay.transform, false);
            UiRuntime.Anchor(panel.GetComponent<RectTransform>(), new Vector2(xMin, 0.85f - panelHeight), new Vector2(xMax, 0.85f));

            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.08f, 0.10f, 0.15f, 0.98f);
            panelBg.raycastTarget = true; // eats taps on the menu's own frame

            for (int i = 0; i < labels.Length; i++)
            {
                int captured = i;
                float yMin = 0.85f - panelHeight + 0.004f + i * rowHeight;
                float yMax = yMin + rowHeight - 0.008f;
                bool selected = IsSelected(i);
                Button row = UiRuntime.CreateButton(overlay.transform, "Opt" + labels[i], labels[i],
                    new Vector2(xMin + 0.004f, yMin), new Vector2(xMax - 0.004f, yMax),
                    () => Select(captured), selected ? activeColor : optionRowColor);
                TextMeshProUGUI rowLabel = row.GetComponentInChildren<TextMeshProUGUI>();
                if (rowLabel != null)
                {
                    rowLabel.fontSize = 15f;
                    rowLabel.SetText((selected ? "\u2022 " : "") + labels[i]);
                }
            }
        }

        private bool IsSelected(int index)
        {
            return selectedChecker != null && selectedChecker(index);
        }

        private static readonly Color optionRowColor = new Color(0.14f, 0.16f, 0.23f, 0.95f);

        private void Select(int index)
        {
            Action<int> handler = onSelect;
            Close();
            handler?.Invoke(index);
        }
    }
}