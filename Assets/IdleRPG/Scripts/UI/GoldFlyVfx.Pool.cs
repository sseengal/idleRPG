using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;

namespace IdleRPG.UI
{
    /// <summary>
    /// The coin pool: rent a recycled node (or build one), and the gold-dot sprite they all share.
    /// Pooled so a kill never allocates - see <c>GoldFlyVfx.Coin</c> for what a coin then does.
    /// </summary>
    public sealed partial class GoldFlyVfx : MonoBehaviour
    {
        private Coin Rent()
        {
            while (pool.Count > 0)
            {
                Coin candidate = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);

                if (candidate != null)
                {
                    return candidate;
                }
            }

            return CreateCoin();
        }

        private Coin CreateCoin()
        {
            GameObject node = UiRuntime.CreateNode("Coin", transform);
            RectTransform rect = node.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(coinSize, coinSize);
            }

            // The front dot (the coin itself).
            GameObject dotNode = UiRuntime.CreateNode("Dot", rect);
            Image dot = dotNode.AddComponent<Image>();
            dot.sprite = coinSprite;
            dot.color = coinColor;
            dot.raycastTarget = false;

            RectTransform dotRect = dotNode.transform as RectTransform;
            if (dotRect != null)
            {
                dotRect.anchorMin = new Vector2(0.5f, 0.5f);
                dotRect.anchorMax = new Vector2(0.5f, 0.5f);
                dotRect.pivot = new Vector2(0.5f, 0.5f);
                dotRect.sizeDelta = new Vector2(coinSize, coinSize);
            }

            // The comet tail: trailGhosts ghost dots, each a child of the coin node, positioned
            // (ghostLocal - current) so they trail the coin inside the coin's own coordinate space.
            int ghosts = Mathf.Max(2, trailGhosts);
            Image[] ghostImages = new Image[ghosts];

            for (int g = 0; g < ghosts; g++)
            {
                GameObject ghostNode = UiRuntime.CreateNode($"Ghost{g}", rect);
                Image ghost = ghostNode.AddComponent<Image>();
                ghost.sprite = coinSprite;
                ghost.color = coinColor;
                ghost.raycastTarget = false;
                ghost.enabled = false;

                RectTransform ghostRect = ghostNode.transform as RectTransform;
                if (ghostRect != null)
                {
                    ghostRect.anchorMin = new Vector2(0.5f, 0.5f);
                    ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
                    ghostRect.pivot = new Vector2(0.5f, 0.5f);

                    float age = ghosts > 1 ? (float)g / (ghosts - 1) : 0f;
                    float size = Mathf.Lerp(trailGhostSize, trailGhostSize * 0.4f, age);
                    ghostRect.sizeDelta = new Vector2(size, size);
                }

                ghostImages[g] = ghost;
            }

            node.SetActive(false);
            return new Coin(this, rect, dot, ghostImages);
        }
        /// <summary>A soft gold dot, generated once in code so no texture asset sits in the repo.</summary>
        private static Sprite CreateGoldDot()
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
