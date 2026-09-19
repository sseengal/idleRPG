using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// Health bar with a delayed "ghost" fill that drains after the real fill, so a big hit
    /// reads clearly. Pure view: callers push values in, nothing here polls the game state.
    /// </summary>
    public sealed class HpBarView : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image ghostImage;
        [SerializeField] private TextMeshProUGUI valueLabel;

        [Header("Behaviour")]
        [Tooltip("How fast the ghost bar catches up, in fill-fraction per second.")]
        [SerializeField] private float ghostDrainSpeed = 0.6f;

        [Tooltip("Seconds before the ghost bar starts draining after a hit.")]
        [SerializeField] private float ghostDelaySec = 0.25f;

        [SerializeField] private Color healthyColor = new Color(0.35f, 0.85f, 0.45f, 1f);
        [SerializeField] private Color woundedColor = new Color(0.95f, 0.75f, 0.2f, 1f);
        [SerializeField] private Color criticalColor = new Color(0.9f, 0.25f, 0.2f, 1f);

        private float displayedFill = 1f;
        private float ghostFill = 1f;
        private float ghostTimer;

        public void SetFill(float normalized, bool instant = false)
        {
            normalized = Mathf.Clamp01(normalized);

            if (instant)
            {
                displayedFill = normalized;
                ghostFill = normalized;
                ApplyVisuals();
                return;
            }

            if (normalized < displayedFill)
            {
                // Damage taken: hold the ghost where it is, then let it drain.
                ghostTimer = ghostDelaySec;
            }
            else
            {
                ghostFill = normalized;
            }

            displayedFill = normalized;
            ApplyVisuals();
        }

        /// <summary>Sets the label, e.g. "240 / 360".</summary>
        public void SetValueLabel(double current, double max)
        {
            if (valueLabel != null)
            {
                valueLabel.SetText(string.Format("{0} / {1}", NumberFormatter.Format(current), NumberFormatter.Format(max)));
            }
        }

        public void SetLabel(string text)
        {
            if (valueLabel != null)
            {
                valueLabel.SetText(text);
            }
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
            if (fillImage != null)
            {
                fillImage.fillAmount = displayedFill;
                fillImage.color = ColorFor(displayedFill);
            }

            if (ghostImage != null)
            {
                ghostImage.fillAmount = Mathf.Max(ghostFill, displayedFill);
            }
        }

        private Color ColorFor(float normalized)
        {
            if (normalized > 0.5f)
            {
                return Color.Lerp(woundedColor, healthyColor, (normalized - 0.5f) * 2f);
            }

            return Color.Lerp(criticalColor, woundedColor, normalized * 2f);
        }
    }
}