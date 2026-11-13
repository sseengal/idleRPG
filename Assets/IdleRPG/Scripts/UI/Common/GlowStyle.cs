using UnityEngine;

namespace IdleRPG.UI
{
    /// <summary>
    /// How a glow looks, in one asset: colour, shape and timing. One style is shared by every widget
    /// that glows, so a step dot and (later) a card breathe at the same rate.
    ///
    /// ELI5: a recipe for light. Copy the asset, change the colour, and a different thing glows the
    /// same way. Nothing in code picks a duration; it asks this asset.
    /// </summary>
    [CreateAssetMenu(fileName = "GlowStyle", menuName = "Idle RPG/UI/Glow Style", order = 4)]
    public sealed class GlowStyle : ScriptableObject
    {
        [Header("Colour")]
        [SerializeField] private Color tint = new Color(0.84f, 0.62f, 0.26f, 1f);

        [Header("Shape")]
        [Tooltip("Halo size as a multiple of the mark it sits behind. 2.6 turns a 20 px dot into a 52 px halo.")]
        [SerializeField] private float haloScale = 2.6f;

        [Header("Alpha")]
        [Range(0f, 1f)] [SerializeField] private float steadyAlpha = 0.30f;
        [Range(0f, 1f)] [SerializeField] private float peakAlpha = 0.55f;
        [Range(0f, 1f)] [SerializeField] private float troughAlpha = 0.12f;

        [Header("Timing (seconds)")]
        [Tooltip("One full breath. The doc's ladder says 1.6.")]
        [SerializeField] private float pulseSeconds = 1.6f;

        [Tooltip("One flash, then gone. The doc's ladder says 0.18.")]
        [SerializeField] private float flashSeconds = 0.18f;

        public Color Tint => tint;
        public float HaloScale => haloScale;
        public float SteadyAlpha => steadyAlpha;
        public float PeakAlpha => peakAlpha;
        public float TroughAlpha => troughAlpha;
        public float PulseSeconds => pulseSeconds;
        public float FlashSeconds => flashSeconds;
    }
}
