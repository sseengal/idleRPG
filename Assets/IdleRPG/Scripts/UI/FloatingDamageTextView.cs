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
        [SerializeField] private float riseDistance = 56f;
        [SerializeField] private float horizontalDrift = 30f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 1.25f, 1f, 0.85f);

        [Header("Colours")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color criticalColor = new Color(1f, 0.85f, 0.25f, 1f);
        [SerializeField] private Color incomingColor = new Color(1f, 0.35f, 0.35f, 1f);
        [Tooltip("Colour of health restored by an ability.")]
        [SerializeField] private Color healColor = new Color(0.4f, 0.95f, 0.5f, 1f);
        [Tooltip("Colour of a guard raised by an ability (no number, just the word).")]
        [SerializeField] private Color guardColor = new Color(0.55f, 0.8f, 1f, 1f);
        [Tooltip("Colour of damage from an enemy ability: heavier than a plain hit.")]
        [SerializeField] private Color abilityColor = new Color(1f, 0.55f, 0.15f, 1f);
        [SerializeField] private float baseFontSize = UiType.Damage;
        [SerializeField] private float criticalScale = UiType.CritScale;
        [SerializeField] private float abilityScale = 1.5f;

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
            baseFontSize = label != null ? label.fontSize : baseFontSize;
        }

        /// <summary>Starts a number at a local anchored position.</summary>
        public void Play(double value, Vector2 anchoredPosition, FloatingTextStyle style, float driftSeed)
        {
            Play(NumberFormatter.Format(value), anchoredPosition, style, driftSeed);
        }

        /// <summary>
        /// Starts a number or a short word (a guard has no number) at a local anchored position. The style decides the
        /// colour and the size; the caller decides the text.
        /// </summary>
        public void Play(string text, Vector2 anchoredPosition, FloatingTextStyle style, float driftSeed)
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

            label.SetText(text);

            Color colour = normalColor;
            switch (style)
            {
                case FloatingTextStyle.Critical:
                    colour = criticalColor;
                    break;
                case FloatingTextStyle.Incoming:
                    colour = incomingColor;
                    break;
                case FloatingTextStyle.Heal:
                    colour = healColor;
                    break;
                case FloatingTextStyle.Guard:
                    colour = guardColor;
                    break;
                case FloatingTextStyle.AbilityHit:
                    colour = abilityColor;
                    break;
            }

            label.color = colour;

            // Crit and enemy-ability damage are the "chunky" showcase: same font, bigger. Reset for everything else.
            float scale = 1f;
            if (style == FloatingTextStyle.Critical)
            {
                scale = criticalScale;
            }
            else if (style == FloatingTextStyle.AbilityHit)
            {
                scale = abilityScale;
            }

            label.fontSize = baseFontSize * scale;

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
        Incoming = 2,

        /// <summary>Health restored by an ability: green.</summary>
        Heal = 3,

        /// <summary>A guard raised by an ability: blue.</summary>
        Guard = 4,

        /// <summary>Damage from an enemy ability: orange and bigger than a plain hit.</summary>
        AbilityHit = 5
    }
}