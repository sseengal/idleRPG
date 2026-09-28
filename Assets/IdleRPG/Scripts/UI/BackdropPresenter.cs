using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;

namespace IdleRPG.UI
{
    /// <summary>
    /// Swaps the battle-area backdrop when the stage changes (boss killed -> stage +1; a defeat
    /// rollback -> stage -1 so the world stays consistent with the stage header). The sprite is
    /// picked by stage number and wraps endlessly through the <see cref="BackdropCatalog"/>.
    /// The Image runs with PreserveAspect, so backdrops are never stretched.
    /// </summary>
    public sealed class BackdropPresenter : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Image background;
        [SerializeField] private BackdropCatalog catalog;

        private int shownIndex = -1;

        private void OnEnable()
        {
            GameEvents.StageChanged += OnStageChanged;
            background.preserveAspect = false; // the explicit cover rect handles aspect, never stretching
            Show(CurrentStage());
        }

        private void OnDisable()
        {
            GameEvents.StageChanged -= OnStageChanged;
        }

        private IEnumerator Start()
        {
            // CanvasScaler fixes rect sizes after activation; fit the cover rect once layout settles.
            yield return null;

            if (background != null && background.sprite != null)
            {
                ApplyCoverFit(background.sprite);
            }
        }

        private static int CurrentStage()
        {
            HudController hud = HudController.Instance;
            GameManager manager = hud != null ? hud.GameManager : null;
            return manager != null ? manager.CurrentStage : 1;
        }

        private void OnStageChanged(int stage, int wave, bool isBossWave)
        {
            Show(stage);
        }

        private void Show(int stage)
        {
            if (background == null || catalog == null || catalog.Count == 0)
            {
                return;
            }

            int index = (Mathf.Max(1, stage) - 1) % catalog.Count;
            if (index == shownIndex)
            {
                return;
            }

            Sprite next = catalog.GetBackdrop(stage);
            if (next == null)
            {
                return;
            }

            shownIndex = index;
            background.sprite = next;
            ApplyCoverFit(next);
        }

        /// <summary>
        /// Scales the backdrop to fully cover the viewport - touching the left and right edges with no
        /// letterbox bars - while preserving the sprite's aspect ratio. Any overflow is cropped (top for
        /// tall/square art, sides for wide art) and hidden by the Viewport's RectMask2D, so it can never
        /// bleed over the header or combat log.
        /// </summary>
        private void ApplyCoverFit(Sprite sprite)
        {
            if (sprite == null || background == null)
            {
                return;
            }

            RectTransform rect = background.rectTransform;
            RectTransform parent = rect.parent != null ? rect.parent as RectTransform : null;

            if (parent == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);

            Vector2 parentSize = parent.rect.size;
            float pw = Mathf.Max(1f, parentSize.x);
            float ph = Mathf.Max(1f, parentSize.y);

            Vector2 spriteSize = sprite.bounds.size;
            float iw = Mathf.Max(1f, spriteSize.x);
            float ih = Mathf.Max(1f, spriteSize.y);

            float scale = Mathf.Max(pw / iw, ph / ih);
            float w = iw * scale;
            float h = ih * scale;

            // sizeDelta on a stretched rect is parent size + delta, so overflow = image - parent.
            float deltaW = w - pw;
            float deltaH = h - ph;
            rect.sizeDelta = new Vector2(deltaW, deltaH);
            // Center horizontally; rest the bottom edge on the viewport so the ground stays visible
            // and the cropped overflow is only at the masked top.
            rect.anchoredPosition = new Vector2(0f, deltaH * 0.5f);
        }
    }
}