using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// The ribbon that hangs across a sheet's top edge: art, text, centring and shadow in one place, so every
    /// sheet that has a heading looks the same. The skin owns the sprite, the scale and the ink.
    ///
    /// ELI5: a red ribbon with a title, pinned to the top of a panel. Call this and it appears.
    /// </summary>
    public static class UiBanner
    {
        /// <summary>Owner's width: the ribbon (tails included) spans three quarters of the panel.</summary>
        public const float WidthFraction = 0.75f;

        /// <summary>Room above the panel's top edge, above the inner bottom bands, to keep the words centred.</summary>
        private const float TextNudge = 2.5f;

        /// <summary>Builds the ribbon and returns its label, or null when the skin has no banner art.</summary>
        public static TextMeshProUGUI Build(Transform panel, string text, float widthFraction = WidthFraction)
        {
            UiSkin skin = UiSkin.Current;

            if (panel == null || skin.bannerSprite == null)
            {
                return null;
            }

            GameObject node = new GameObject("Banner", typeof(RectTransform), typeof(Image));
            node.transform.SetParent(panel, false);

            float half = Mathf.Clamp01(widthFraction) * 0.5f;
            RectTransform rect = (RectTransform)node.transform;
            rect.anchorMin = new Vector2(0.5f - half, 1f);
            rect.anchorMax = new Vector2(0.5f + half, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, skin.bannerSprite.rect.height * skin.bannerScale);
            rect.anchoredPosition = Vector2.zero;

            Image image = node.GetComponent<Image>();
            image.sprite = skin.bannerSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.0001f, skin.bannerScale);
            image.raycastTarget = false;

            // The same two-step shadow every surface and button uses.
            UiParts.AddShadow(node);

            TextMeshProUGUI label = UiRuntime.CreateText(node.transform, "Label", text, UiType.Role.Section,
                TextAlignmentOptions.Center, skin.bannerInk);

            // The box covers the ribbon's red face (art rows 1-16 of 23); the drop centres the capitals inside it.
            UiRuntime.Anchor(label.rectTransform, new Vector2(0f, 0.30f), new Vector2(1f, 0.96f));
            label.rectTransform.anchoredPosition =
                new Vector2(0f, -(CapDrop(label.font, label.fontSize) + TextNudge));
            label.raycastTarget = false;

            return label;
        }

        /// <summary>
        /// How far a line of CAPITALS must move down to look centred in its box. TMP centres the full line box
        /// (ascender to descender); capitals sit higher inside it. Measured from the font, scaled to the size.
        /// </summary>
        private static float CapDrop(TMP_FontAsset font, float size)
        {
            if (font == null || size <= 0f)
            {
                return 0f;
            }

            UnityEngine.TextCore.FaceInfo info = font.faceInfo;

            if (info.pointSize <= 0f)
            {
                return 0f;
            }

            float capCentre = (info.capLine + info.baseline) * 0.5f;
            float lineCentre = (info.ascentLine + info.descentLine) * 0.5f;
            return (capCentre - lineCentre) * (size / info.pointSize);
        }
    }
}
