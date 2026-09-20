using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// Tiny runtime UI builder. The editor has <c>UiFactory</c> for authoring the scene; this is the runtime twin
    /// for widgets that build themselves in code (the formation strip, shop rows), so those classes do not have to
    /// repeat the same RectTransform boilerplate.
    ///
    /// ELI5: the same Lego bricks the scene builder uses, but available while the game is running.
    /// </summary>
    public static class UiRuntime
    {
        /// <summary>An empty UI node with a RectTransform on the UI layer.</summary>
        public static GameObject CreateNode(string name, Transform parent)
        {
            GameObject node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(parent, false);

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                node.layer = uiLayer;
            }

            return node;
        }

        /// <summary>A solid/sliced panel image.</summary>
        public static Image CreatePanel(Transform parent, Sprite sprite, Color color, bool raycast = false)
        {
            GameObject node = CreateNode("Panel", parent);
            Image image = node.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        /// <summary>A sprite-less icon image (tinted placeholder art).</summary>
        public static Image CreateIcon(string name, Transform parent)
        {
            GameObject node = CreateNode(name, parent);
            Image image = node.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A TMP label on the shared font (falls back to TMP's default when none is loaded yet).</summary>
        public static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size,
            TextAlignmentOptions alignment, Color color)
        {
            GameObject node = CreateNode(name, parent);
            TextMeshProUGUI text = node.AddComponent<TextMeshProUGUI>();
            text.SetText(content);
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        /// <summary>Stretches a rect to its parent with per-side inset.</summary>
        public static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Anchors a rect to a normalised region of its parent.</summary>
        public static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Anchors a rect to a normalised region with per-side insets.</summary>
        public static void Anchor(RectTransform rect, Vector2 min, Vector2 max, float left, float bottom, float right, float top)
        {
            Anchor(rect, min, max);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Centres a rect on a normalised point with a fixed size.</summary>
        public static void CenterOn(RectTransform rect, Vector2 normalizedCenter, Vector2 size)
        {
            rect.anchorMin = normalizedCenter;
            rect.anchorMax = normalizedCenter;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>A button anchored to a normalised region, with a centred label.</summary>
        public static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin,
            Vector2 anchorMax, UnityEngine.Events.UnityAction onClick, Color? background = null)
        {
            Image image = CreatePanel(parent, null, background ?? new Color(0.22f, 0.32f, 0.5f, 1f), raycast: true);
            image.gameObject.name = name;
            Anchor(image.rectTransform, anchorMin, anchorMax);

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI text = CreateText(image.transform, "Label", label, 20f,
                TextAlignmentOptions.Center, Color.white);
            Stretch(text.rectTransform, 8f, 4f, 8f, 4f);
            text.textWrappingMode = TextWrappingModes.Normal;

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            return button;
        }

        /// <summary>A horizontal filled image (health bar fill).</summary>
        public static Image CreateFilledImage(string name, Transform parent, Color color)
        {
            GameObject node = CreateNode(name, parent);
            Image image = node.AddComponent<Image>();
            image.color = color;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 1f;
            image.raycastTarget = false;
            Stretch(image.rectTransform);
            return image;
        }
    }
}
