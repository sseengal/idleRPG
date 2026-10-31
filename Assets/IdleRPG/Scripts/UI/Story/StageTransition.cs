using System;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// A full-arena fade. It lives under the battle stage root, so it covers the arena render texture only:
    /// the header, battle log, nav and popups are never touched.
    ///
    /// ELI5: a black curtain inside the battle picture. Cover it, swap what is on stage, then pull it away.
    /// Time is unscaled, so a paused fight cannot freeze the curtain.
    /// </summary>
    public sealed class StageTransition : MonoBehaviour
    {
        [SerializeField] private float coverSeconds = 0.35f;
        [SerializeField] private float revealSeconds = 0.35f;
        [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Image overlay;
        private CanvasGroup group;

        private Action done;
        private float duration;
        private float elapsed;
        private float startAlpha;
        private float targetAlpha;
        private bool busy;

        /// <summary>True while a cover or reveal is running.</summary>
        public bool IsBusy => busy;

        /// <summary>Creates the curtain as the last child of the stage, so it draws over actors and numbers.</summary>
        public static StageTransition Create(RectTransform stageRoot)
        {
            if (stageRoot == null)
            {
                return null;
            }

            GameObject node = UiRuntime.CreateNode("StoryTransition", stageRoot);
            node.transform.SetAsLastSibling();

            StageTransition transition = node.AddComponent<StageTransition>();
            transition.Build();
            return transition;
        }

        /// <summary>Fades the arena to black, then runs <paramref name="onCovered"/>.</summary>
        public void Cover(Action onCovered)
        {
            Begin(coverSeconds, 1f, onCovered);
        }

        /// <summary>Fades the arena back in, then runs <paramref name="onRevealed"/>.</summary>
        public void Reveal(Action onRevealed)
        {
            Begin(revealSeconds, 0f, onRevealed);
        }

        /// <summary>Jumps to black without a fade (used when a page was hidden and must not pop).</summary>
        public void SnapCovered()
        {
            Stop();
            overlay.enabled = true;
            group.alpha = 1f;
        }

        private void Build()
        {
            RectTransform rect = transform as RectTransform;

            if (rect != null)
            {
                UiRuntime.Stretch(rect);
            }

            overlay = UiRuntime.CreatePanel(transform, null, Color.black, raycast: false);
            overlay.gameObject.name = "Black";
            UiRuntime.Stretch(overlay.rectTransform);
            overlay.enabled = false;

            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private void Begin(float seconds, float target, Action onDone)
        {
            if (overlay == null || group == null)
            {
                onDone?.Invoke();
                return;
            }

            // A fade can be interrupted. This happens when the player skips during the opening reveal:
            // the exit cover starts from the current alpha and replaces the reveal's (empty) callback.
            // Without this, the cover was ignored and the scene stayed in its exit phase forever.
            if (busy)
            {
                done = null;
            }

            busy = true;
            done = onDone;
            duration = Mathf.Max(0f, seconds);
            elapsed = 0f;
            startAlpha = group.alpha;
            targetAlpha = target;

            overlay.enabled = true;
        }

        private void Update()
        {
            if (!busy)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float eased = ease != null ? ease.Evaluate(t) : t;
            group.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, eased);

            if (t < 1f)
            {
                return;
            }

            group.alpha = targetAlpha;

            if (targetAlpha <= 0f)
            {
                overlay.enabled = false;
            }

            Finish();
        }

        private void Finish()
        {
            busy = false;
            Action callback = done;
            done = null;
            callback?.Invoke();
        }

        private void Stop()
        {
            busy = false;
            done = null;
        }
    }
}
