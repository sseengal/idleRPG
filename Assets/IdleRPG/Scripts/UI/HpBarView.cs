using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// The game's ONE health bar: a dark track, a delayed "ghost" fill that trails the real fill, a fill that glides
    /// to its new length, and the numbers inside it.
    ///
    /// ELI5: the life bar. Green while healthy, amber when hurt, red when nearly gone. A hit makes the bar slide
    /// down and a pale tail follow a moment later, so you can see how much was just lost. The numbers inside say
    /// how much life is left. The hero cells under the fight and the bars over the units in the arena are the same
    /// widget, so one hero can never read two ways.
    ///
    /// Pure view: callers push values in, nothing here polls the game state.
    /// </summary>
    public sealed class HpBarView : MonoBehaviour
    {
        /// <summary>
        /// How fast the drawn fill eases toward the health it was given. A hit arrives as one step, so copying it
        /// would snap; 16 spreads the step over roughly 60 ms, which the eye reads as a slide. The value always
        /// converges on the real health, so the bar cannot lie about it.
        /// </summary>
        private const float FillEasePerSecond = 16f;

        [Header("Wiring")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image ghostImage;
        [SerializeField] private TextMeshProUGUI valueLabel;

        [Header("Behaviour")]
        [Tooltip("How fast the ghost bar catches up, in fill-fraction per second.")]
        [SerializeField] private float ghostDrainSpeed = 0.6f;

        [Tooltip("Seconds before the ghost bar starts draining after a hit.")]
        [SerializeField] private float ghostDelaySec = 0.25f;

        /// <summary>The health the fight reported. The drawn fill eases toward it; the two are equal at rest.</summary>
        private float targetFill = 1f;

        private float displayedFill = 1f;
        private float ghostFill = 1f;
        private float ghostTimer;

        public void SetFill(float normalized, bool instant = false)
        {
            normalized = Mathf.Clamp01(normalized);

            if (instant)
            {
                targetFill = normalized;
                displayedFill = normalized;
                ghostFill = normalized;
                ApplyVisuals();
                return;
            }

            if (normalized < targetFill)
            {
                // Damage taken: hold the ghost where it is, then let it drain.
                ghostTimer = ghostDelaySec;
            }
            else
            {
                ghostFill = normalized;
            }

            targetFill = normalized;
        }

        /// <summary>
        /// Sets the label, e.g. "240/360". No spaces around the slash: the bar's value box is narrow, and at the
        /// type scale the spaces alone cost more width than the bar has to spare. Writes only on a change, because
        /// an ability cell pushes this every frame and a rewrite would rebuild the text mesh every frame.
        /// </summary>
        public void SetValueLabel(double current, double max)
        {
            if (valueLabel == null)
            {
                return;
            }

            string text = NumberFormatter.Format(current) + "/" + NumberFormatter.Format(max);

            if (valueLabel.text != text)
            {
                valueLabel.SetText(text);
            }
        }

        public void SetLabel(string text)
        {
            if (valueLabel != null && valueLabel.text != text)
            {
                valueLabel.SetText(text);
            }
        }

        /// <summary>Wires a bar that was created in code (formation strip slots, ability cells).</summary>
        public void ConfigureRuntime(Image fill, Image ghost, TextMeshProUGUI label)
        {
            fillImage = fill;
            ghostImage = ghost;
            valueLabel = label;
            StyleNumbers();
        }

        /// <summary>
        /// Keeps the numbers on one line and clips rather than spilling. They sit OUTSIDE the bar now (on the page, or
        /// on the nameplate), so no outline is needed - and no per-label material instance is created.
        /// </summary>
        private void StyleNumbers()
        {
            if (valueLabel == null)
            {
                return;
            }

            valueLabel.textWrappingMode = TextWrappingModes.NoWrap;
            valueLabel.overflowMode = TextOverflowModes.Masking;
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        private void Update()
        {
            // The fill glides toward the health it was given. A hit lands as one step; copying it would snap, and a
            // snap reads as a glitch beside a picture that slides. The value always lands on the real number.
            if (!Mathf.Approximately(displayedFill, targetFill))
            {
                float ease = 1f - Mathf.Exp(-FillEasePerSecond * Time.deltaTime);
                displayedFill = Mathf.Lerp(displayedFill, targetFill, ease);

                if (Mathf.Abs(displayedFill - targetFill) < 0.0005f)
                {
                    displayedFill = targetFill;
                }

                ApplyVisuals();
            }

            if (Mathf.Approximately(ghostFill, displayedFill))
            {
                return;
            }

            if (ghostTimer > 0f)
            {
                ghostTimer -= Time.deltaTime;
                return;
            }

            ghostFill = Mathf.MoveTowards(ghostFill, displayedFill, ghostDrainSpeed * Time.deltaTime);
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            SetFraction(fillImage, displayedFill, UiTheme.HpTint(displayedFill));
            SetFraction(ghostImage, Mathf.Max(ghostFill, displayedFill), null);
        }

        /// <summary>
        /// Draws a fraction of the bar by moving its RIGHT edge, from the left edge. Anchors, not
        /// <see cref="Image.fillAmount"/>: a Filled image with no sprite ignores fillAmount and draws the whole
        /// rect - which is why these bars used to sit full through every hit. Anchors crop by geometry, so no
        /// sprite is needed, and the same trick works in the ability cells.
        /// </summary>
        private static void SetFraction(Image image, float amount, Color? tint)
        {
            if (image == null)
            {
                return;
            }

            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (tint.HasValue)
            {
                image.color = tint.Value;
            }
        }
    }
}