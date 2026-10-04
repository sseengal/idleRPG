using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// The common chrome of a docked, non-modal sheet: a raycasting frame, an X that closes it through
    /// <see cref="UiTransient"/>, and a content root that fills the frame. Pages build their rows into
    /// <see cref="Content"/> (anchors are frame-relative, 0..1).
    /// </summary>
    public sealed class UiDockedSheet
    {
        public GameObject Root { get; private set; }

        public RectTransform Content { get; private set; }

        /// <summary>
        /// Creates the sheet frame under <paramref name="parent"/>. Keep the page's grid built BEFORE this
        /// so the sheet (a later sibling) renders above it, and only its own rect eats taps.
        /// </summary>
        public static UiDockedSheet Create(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color frameColor)
        {
            UiDockedSheet ui = new UiDockedSheet();

            GameObject frame = new GameObject(name, typeof(RectTransform));
            frame.transform.SetParent(parent, false);
            UiRuntime.Anchor((RectTransform)frame.transform, anchorMin, anchorMax);
            ui.Root = frame;

            Image bg = frame.AddComponent<Image>();
            bg.color = frameColor;
            bg.raycastTarget = true;

            RectTransform content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(frame.transform, false);
            UiRuntime.Anchor(content, Vector2.zero, Vector2.one);
            ui.Content = content;

            Button close = UiRuntime.CreateButton(frame.transform, "CloseButton", "X",
                new Vector2(0.88f, 0.90f), new Vector2(0.96f, 0.97f),
                () => UiTransient.Close(ui.Root), new Color(0.30f, 0.36f, 0.50f, 1f));
            TextMeshProUGUI closeLabel = close.GetComponentInChildren<TextMeshProUGUI>();
            if (closeLabel != null)
            {
                closeLabel.fontSize = 14f;
            }

            return ui;
        }
    }
}