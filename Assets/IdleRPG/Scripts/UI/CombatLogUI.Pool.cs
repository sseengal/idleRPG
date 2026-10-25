using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// Line pooling: labels are created once, rented, returned and trimmed here. The trim is a scrollback
    /// window only - logging itself is never dropped.
    /// </summary>
    public sealed partial class CombatLogUI : MonoBehaviour
    {

        private void TrimToMax()
        {
            while (liveLines.Count > Mathf.Max(1, maxLines))
            {
                Return(liveLines[0]);
                liveLines.RemoveAt(0);
            }
        }

        private TextMeshProUGUI Rent()
        {
            while (pool.Count > 0)
            {
                TextMeshProUGUI candidate = pool.Dequeue();
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return CreateLine();
        }

        private void Return(TextMeshProUGUI label)
        {
            if (label == null)
            {
                return;
            }

            label.gameObject.SetActive(false);
            pool.Enqueue(label);
        }

        /// <summary>
        /// New-session reset: every live line goes back to the pool and the scroll returns to the bottom,
        /// so a restart/offline load starts with an empty, visibly resuming feed instead of rolling noise.
        /// </summary>
        private void ClearLines()
        {
            for (int i = 0; i < liveLines.Count; i++)
            {
                Return(liveLines[i]);
            }

            liveLines.Clear();

            // The frame-end pass resizes the content and pins it after the rebuild.
            contentDirty = true;
        }

        private TextMeshProUGUI CreateLine()
        {
            TextMeshProUGUI label = Instantiate(lineTemplate, content);
            label.rectTransform.localScale = Vector3.one;

            LayoutElement element = label.gameObject.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = label.gameObject.AddComponent<LayoutElement>();
            }

            element.minHeight = lineHeight;
            element.preferredHeight = lineHeight;
            label.gameObject.SetActive(false);

            return label;
        }

        /// <summary>
        /// Height is computed explicitly: the list grows line by line and a ContentSizeFitter would need an extra
        /// layout pass to keep up. The numbers come from the content's own layout group, so the height matches
        /// where the group actually puts the last line (a mismatch left a blank band under the newest line).
        /// </summary>
        private void RefreshContentSize()
        {
            if (content == null)
            {
                return;
            }

            float padTop = contentPadding;
            float padBottom = contentPadding;
            float gap = lineSpacing;

            if (stack != null)
            {
                padTop = stack.padding.top;
                padBottom = stack.padding.bottom;
                gap = stack.spacing;
            }

            int count = liveLines.Count;
            float height = padTop + padBottom + count * lineHeight + Mathf.Max(0, count - 1) * gap;
            content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(1f, height));
        }
    }
}
