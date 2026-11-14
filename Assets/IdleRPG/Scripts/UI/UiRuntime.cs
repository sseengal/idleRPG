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

        /// <summary>
        /// A TMP label on a role from <see cref="UiType"/>. The role supplies the font AND the size together, so a
        /// widget says what the text IS and cannot invent a size.
        ///
        /// Why the font is applied HERE: this builder used to set a size and no font at all, so every label a page
        /// built while the game ran came out in Unity's smooth default (Liberation Sans) while the battle page wore
        /// the pixel fonts - half the game read as a different game. Nothing may set a size without a font.
        /// </summary>
        public static TextMeshProUGUI CreateText(Transform parent, string name, string content, UiType.Role role,
            TextAlignmentOptions alignment, Color color)
        {
            GameObject node = CreateNode(name, parent);
            TextMeshProUGUI text = node.AddComponent<TextMeshProUGUI>();
            text.SetText(content);
            UiType.Apply(text, role);
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

        private static Sprite plateSprite;

        /// <summary>The chunky riveted plate (ui_frame) - the one face for panels and buttons. Runtime-loadable.</summary>
        public static Sprite PlateSprite =>
            plateSprite != null ? plateSprite : (plateSprite = Resources.Load<Sprite>("ui_frame"));

        /// <summary>A 9-sliced chunky plate. Big surfaces wear this; small chips stay flat by design.</summary>
        public static Image Plate(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Color color, bool raycast = false)
        {
            Image image = CreatePanel(parent, PlateSprite, color, raycast);
            image.gameObject.name = name;
            Anchor(image.rectTransform, anchorMin, anchorMax);
            return image;
        }

        private static Sprite softGlowSprite;

        /// <summary>A soft radial glow (white, baked alpha) shared by halos, highlights and the training tree.</summary>
        public static Sprite SoftGlowSprite
        {
            get
            {
                if (softGlowSprite != null) { return softGlowSprite; }

                const int size = 128;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                Vector2 centre = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
                float maxR = size * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), centre) / maxR;
                        float a = Mathf.Clamp01(1f - d);
                        a *= a;
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }

                tex.Apply();
                softGlowSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
                softGlowSprite.name = "UiSoftGlow";
                return softGlowSprite;
            }
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

        /// <summary>
        /// A button anchored to a normalised region, with a centred label. The caption takes <paramref name="role"/>
        /// (section by default - a button caption is chrome the player must read at a glance).
        /// </summary>
        public static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin,
            Vector2 anchorMax, UnityEngine.Events.UnityAction onClick, Color? background = null, Sprite sprite = null,
            UiType.Role role = UiType.Role.Section)
        {
            Image image = CreatePanel(parent, sprite, background ?? UiTheme.ButtonBlue, raycast: true);
            image.gameObject.name = name;
            Anchor(image.rectTransform, anchorMin, anchorMax);

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI text = CreateText(image.transform, "Label", label, role,
                TextAlignmentOptions.Center, Color.white);
            Stretch(text.rectTransform, UiSpace.LabelPadX, UiSpace.LabelPadY, UiSpace.LabelPadX, UiSpace.LabelPadY);
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
