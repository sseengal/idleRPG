using TMPro;
using UnityEngine;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// One pooled floating damage number. Animated by <see cref="FloatingDamageTextPool"/>,
    /// never by its own Update(), so a hundred numbers cost one loop.
    /// </summary>
    public sealed class FloatingDamageTextView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private RectTransform rectTransform;

        [Header("Animation")]
        [SerializeField] private float lifetimeSec = 0.9f;
        [SerializeField] private float riseDistance = 90f;
        [SerializeField] private float horizontalDrift = 40f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 1.25f, 1f, 0.85f);

        [Header("Colours")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color criticalColor = new Color(1f, 0.85f, 0.25f, 1f);
        [SerializeField] private Color incomingColor = new Color(1f, 0.35f, 0.35f, 1f);

        private float elapsed;
        private Vector2 startPosition;
        private float driftDirection;
        private float baseScale = 1f;

        /// <summary>True while the item is animating and must still be ticked.</summary>
        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            baseScale = rectTransform != null ? rectTransform.localScale.x : 1f;
        }

        /// <summary>Starts a number at a local anchored position.</summary>
        public void Play(double value, Vector2 anchoredPosition, FloatingTextStyle style, float driftSeed)
        {
            if (label == null || rectTransform == null)
            {
                IsPlaying = false;
                return;
            }

            rectTransform.anchoredPosition = anchoredPosition;
            startPosition = anchoredPosition;
            elapsed = 0f;
            driftDirection = driftSeed % 2f < 1f ? -1f : 1f;

            label.SetText(NumberFormatter.Format(value));
            label.color = style == FloatingTextStyle.Critical
                ? criticalColor
                : (style == FloatingTextStyle.Incoming ? incomingColor : normalColor);

            gameObject.SetActive(true);
            IsPlaying = true;
        }

        /// <summary>Advances the animation. Returns false when the item has finished.</summary>
        public bool Tick(float deltaTime)
        {
            if (!IsPlaying || rectTransform == null)
            {
                return false;
            }

            elapsed += deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, lifetimeSec));

            rectTransform.anchoredPosition = startPosition + new Vector2(
                driftDirection * horizontalDrift * t,
                riseDistance * t);

            float scale = scaleCurve.Evaluate(t) * baseScale;
            rectTransform.localScale = new Vector3(scale, scale, 1f);

            if (label != null)
            {
                Color color = label.color;
                color.a = 1f - t * t;
                label.color = color;
            }

            if (t < 1f)
            {
                return true;
            }

            IsPlaying = false;
            gameObject.SetActive(false);
            return false;
        }

        public void Stop()
        {
            IsPlaying = false;
            gameObject.SetActive(false);
        }
    }

    /// <summary>Visual style for a floating number.</summary>
    public enum FloatingTextStyle
    {
        Normal = 0,
        Critical = 1,
        Incoming = 2
    }
}