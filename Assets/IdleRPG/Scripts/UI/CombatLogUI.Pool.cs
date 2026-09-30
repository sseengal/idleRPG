using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// Line pooling: labels are created once, rented, returned and trimmed here.
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

            if (aggregateLabel == label)
            {
                CloseAggregate();
            }
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
            CloseAggregate();
            RefreshContentSize();

            if (content != null)
            {
                content.anchoredPosition = Vector2.zero;
            }

            pinnedToBottom = true;
            lastAppliedY = -1f;
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
        /// Height is computed explicitly: the list grows line by line and a ContentSizeFitter would
        /// need an extra layout pass to keep up. The scroll position is handled by FollowNewest().
        /// </summary>
        private void RefreshContentSize()
        {
            if (content == null)
            {
                return;
            }

            float height = contentPadding * 2f + liveLines.Count * (lineHeight + lineSpacing);
            content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(1f, height));
        }
    }
}
