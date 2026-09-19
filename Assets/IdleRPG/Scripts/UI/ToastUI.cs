using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using IdleRPG.Core;

namespace IdleRPG.UI
{
    /// <summary>
    /// Bottom-anchored transient message line ("Not enough gold"). Queues messages so rapid
    /// events do not overwrite each other.
    /// </summary>
    public sealed class ToastUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private float visibleSec = 2f;
        [SerializeField] private float fadeSec = 0.25f;

        private readonly Queue<string> pending = new Queue<string>();
        private Coroutine routine;

        private void OnEnable()
        {
            GameEvents.ToastRequested += OnToastRequested;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        private void OnDisable()
        {
            GameEvents.ToastRequested -= OnToastRequested;
        }

        private void OnToastRequested(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            pending.Enqueue(message);

            if (routine == null)
            {
                routine = StartCoroutine(DrainQueue());
            }
        }

        private IEnumerator DrainQueue()
        {
            while (pending.Count > 0)
            {
                string message = pending.Dequeue();

                if (label != null)
                {
                    label.SetText(message);
                }

                yield return FadeTo(1f, fadeSec);
                yield return new WaitForSeconds(visibleSec);
                yield return FadeTo(0f, fadeSec);
            }

            routine = null;
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (canvasGroup == null)
            {
                yield break;
            }

            float start = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, elapsed / Mathf.Max(0.0001f, duration));
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }
    }
}