using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// The mote pool: rent a recycled node (or build one), plus the soft dot sprite they all share.
    /// Pooled so a kill never allocates - see <c>XpFlyVfx.Mote</c> for what a mote then does.
    /// </summary>
    public sealed partial class XpFlyVfx : MonoBehaviour
    {
        private Mote Rent()
        {
            while (pool.Count > 0)
            {
                Mote candidate = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);

                if (candidate != null)
                {
                    return candidate;
                }
            }

            return CreateMote();
        }

        private Mote CreateMote()
        {
            GameObject node = UiRuntime.CreateNode("XpMote", transform);
            RectTransform rect = node.transform as RectTransform;

            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(moteSize, moteSize);
            }

            // Behind everything: the soft halo that makes the dot read as a light, not a square.
            Image halo = CreateLayer("Halo", rect, moteSize * haloScale, UiRuntime.SoftGlowSprite, UiTheme.XpTrail);

            // The trail: a couple of fading dots, drawn under the mote itself.
            int ghosts = Mathf.Max(1, trailGhosts);
            Image[] ghostImages = new Image[ghosts];

            for (int g = 0; g < ghosts; g++)
            {
                float age = ghosts > 1 ? (float)g / (ghosts - 1) : 0f;
                float size = moteSize * Mathf.Lerp(0.78f, 0.42f, age);
                Image ghost = CreateLayer($"Ghost{g}", rect, size, dotSprite, UiTheme.XpFillLight);
                ghost.enabled = false;
                ghostImages[g] = ghost;
            }

            // The mote itself, last so it draws on top of its own trail.
            Image dot = CreateLayer("Dot", rect, moteSize, dotSprite, UiTheme.XpFillLight);

            node.SetActive(false);
            return new Mote(this, rect, dot, halo, ghostImages);
        }

        private static Image CreateLayer(string name, RectTransform parent, float size, Sprite sprite, Color color)
        {
            GameObject node = UiRuntime.CreateNode(name, parent);
            Image image = node.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;

            RectTransform rect = node.transform as RectTransform;

            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(size, size);
            }

            return image;
        }

        /// <summary>A soft blue dot, generated once in code so no texture asset sits in the repo.</summary>
        private static Sprite CreateMoteDot()
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - (size - 1) * 0.5f;
                    float dy = y - (size - 1) * 0.5f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) / (size * 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - distance)));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
