using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>What a glow is doing right now.</summary>
    public enum GlowState
    {
        Off,
        Steady,
        Pulse,
        Flash
    }

    /// <summary>
    /// Plays one halo. The only file that owns glow motion, so swapping the tween engine later is one file.
    ///
    /// ELI5: a lamp behind a sticker. Off, on, breathing, or one bright pop. The recipe (colour, size,
    /// speed) comes from a <see cref="GlowStyle"/> asset, so every glowing thing in the game belongs to
    /// the same family.
    ///
    /// Rules from Docs/Motion.md:
    ///  * one state per view; setting a state cancels the old one;
    ///  * a pulse is a full breath over <c>PulseSeconds</c>;
    ///  * alpha only, never size, so the canvas never relayouts;
    ///  * no allocation after Awake: one float and one sin, no coroutines.
    ///
    /// Why not the tween library yet: LitMotion is installed but Unity has not registered its assembly
    /// (the import was blocked while the project had a compile error). This file is deliberately the
    /// only place that would change when it does.
    /// </summary>
    public sealed class GlowView : MonoBehaviour
    {
        [SerializeField] private Image halo;
        [SerializeField] private GlowStyle style;

        private GlowState state = GlowState.Off;
        private float clock;
        private float flashRemaining;

        public GlowState State => state;

        /// <summary>Wires the halo image and its recipe. Called by whoever builds the widget.</summary>
        public void Bind(Image haloImage, GlowStyle glowStyle)
        {
            halo = haloImage;
            style = glowStyle;

            if (halo != null)
            {
                halo.raycastTarget = false;
                SetAlpha(0f);
            }
        }

        /// <summary>The state to hold. Flash is a one-shot; use <see cref="Flash"/> for it.</summary>
        public void SetState(GlowState next)
        {
            if (halo == null || style == null || state == next)
            {
                return;
            }

            state = next;
            clock = 0f;
            flashRemaining = 0f;

            switch (next)
            {
                case GlowState.Steady:
                    SetAlpha(style.SteadyAlpha);
                    break;

                case GlowState.Pulse:
                    SetAlpha(style.TroughAlpha);
                    break;

                default:
                    SetAlpha(0f);
                    break;
            }
        }

        /// <summary>One bright pop, then dark. Leaves the view Off.</summary>
        public void Flash()
        {
            if (halo == null || style == null)
            {
                return;
            }

            state = GlowState.Flash;
            flashRemaining = Mathf.Max(0.01f, style.FlashSeconds);
            SetAlpha(style.PeakAlpha);
        }

        private void Update()
        {
            if (halo == null || style == null || state == GlowState.Off || state == GlowState.Steady)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;

            if (state == GlowState.Pulse)
            {
                clock += dt;
                float cycle = Mathf.Max(0.05f, style.PulseSeconds);
                float phase = (Mathf.Sin(((clock / cycle) * Mathf.PI * 2f) - (Mathf.PI * 0.5f)) + 1f) * 0.5f;
                SetAlpha(Mathf.Lerp(style.TroughAlpha, style.PeakAlpha, phase));
                return;
            }

            // Flash: bright to dark, easing out.
            flashRemaining -= dt;
            float t = Mathf.Clamp01(flashRemaining / Mathf.Max(0.01f, style.FlashSeconds));
            SetAlpha(style.PeakAlpha * t * t);

            if (flashRemaining <= 0f)
            {
                state = GlowState.Off;
                SetAlpha(0f);
            }
        }

        private void OnDisable()
        {
            state = GlowState.Off;
            flashRemaining = 0f;

            if (halo != null)
            {
                SetAlpha(0f);
            }
        }

        private void SetAlpha(float alpha)
        {
            Color color = style != null ? style.Tint : Color.white;
            color.a = Mathf.Clamp01(alpha);
            halo.color = color;
        }
    }
}
