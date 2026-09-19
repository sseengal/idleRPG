using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Small helpers for building uGUI hierarchies from code. Keeps the scene builders
    /// readable and guarantees consistent RectTransform/CanvasRenderer setup.
    /// </summary>
    public static class UiFactory
    {
        public const string ArtFolder = PlaceholderSpriteGenerator.ArtFolder;

        /// <summary>Default TMP font from the imported essentials.</summary>
        public static TMP_FontAsset Font => TmpBootstrapper.GetDefaultFont();

        /// <summary>An empty UI node with a RectTransform (no visual).</summary>
        public static GameObject Node(string name, Transform parent)
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

        /// <summary>An Image node using a generated placeholder sprite.</summary>
        public static Image Panel(string name, Transform parent, string spriteName, Color color, bool raycast = false)
        {
            GameObject node = Node(name, parent);
            Image image = node.AddComponent<Image>();
            Sprite sprite = LoadSprite(spriteName);

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }

            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>An unsliced image (unit icons, backgrounds).</summary>
        public static Image Icon(string name, Transform parent, Color color, bool preserveAspect = true)
        {
            GameObject node = Node(name, parent);
            Image image = node.AddComponent<Image>();
            image.color = color;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A text label using the shared TMP font.</summary>
        public static TextMeshProUGUI Text(string name, Transform parent, string content, float size,
            TextAlignmentOptions alignment, Color color)
        {
            GameObject node = Node(name, parent);
            TextMeshProUGUI text = node.AddComponent<TextMeshProUGUI>();

            if (Font != null)
            {
                text.font = Font;
            }

            text.SetText(content);
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        /// <summary>A button using a placeholder sprite, with an optional listener.</summary>
        public static Button Button(string name, Transform parent, string label, string spriteName,
            float fontSize, Color textColor, UnityAction onClick)
        {
            Image background = Panel(name, parent, spriteName, Color.white, raycast: true);
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            TextMeshProUGUI text = Text("Label", background.transform, label, fontSize,
                TextAlignmentOptions.Center, textColor);
            Stretch(text.rectTransform, 8f, 4f, 8f, 4f);

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            return button;
        }

        /// <summary>Stretches a rect to its parent with per-side inset (in reference pixels).</summary>
        public static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Anchors a rect to a normalised parent region.</summary>
        public static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Anchors a rect to a normalised region with insets.</summary>
        public static void Anchor(RectTransform rect, Vector2 min, Vector2 max, float left, float bottom, float right, float top)
        {
            Anchor(rect, min, max);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Centres a rect on a normalised point with a fixed size.</summary>
        public static void CenterOn(RectTransform rect, Vector2 normalisedPoint, Vector2 size)
        {
            rect.anchorMin = normalisedPoint;
            rect.anchorMax = normalisedPoint;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>A vertical stack container for list-style panels.</summary>
        public static VerticalLayoutGroup VerticalStack(GameObject target, float spacing, RectOffset padding, bool expandChildren = false)
        {
            VerticalLayoutGroup group = target.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.padding = padding ?? new RectOffset(0, 0, 0, 0);
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = expandChildren;
            return group;
        }

        /// <summary>Horizontal layout used inside a single upgrade row.</summary>
        public static HorizontalLayoutGroup HorizontalStack(GameObject target, float spacing)
        {
            HorizontalLayoutGroup group = target.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;
            return group;
        }

        /// <summary>Loads a generated placeholder sprite by file name (without extension).</summary>
        public static Sprite LoadSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                return null;
            }

            Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + spriteName + ".png");

            if (sprite == null)
            {
                Debug.LogWarning($"[UiFactory] Missing placeholder sprite '{spriteName}'.");
            }

            return sprite;
        }
    }
}