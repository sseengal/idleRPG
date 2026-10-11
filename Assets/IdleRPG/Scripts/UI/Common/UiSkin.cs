using TMPro;
using UnityEngine;

namespace IdleRPG.UI
{
    /// <summary>
    /// The look of every shared card, in one asset: the 9-slice faces from the pixel kit and the colours. Nothing
    /// else may hard-code a card's sprite or colour - swapping the UI art is meant to be THIS file.
    ///
    /// ELI5: a recipe card for how a panel looks. The code that builds a card asks this for its pictures and colours;
    /// when real art lands, point these fields at the new PNGs and every card in the game changes at once.
    ///
    /// Text sizes are NOT here - they belong to <see cref="UiType"/>, which owns the whole type hierarchy. A skin is
    /// look only, so a reskin can never fork the game's font sizes.
    ///
    /// It lives in a Resources folder (like the pixel fonts) so runtime-built UI can load it. A missing asset is NOT
    /// an error: the defaults below draw flat-coloured cards, which is exactly what the old screens did.
    ///
    /// The 9-slice border is NOT set here - it comes from the sprite's own import settings (12 px, applied by
    /// `Tools > Idle RPG > Art > Apply UI-Art Presets`), so a new PNG only has to be imported the same way.
    /// </summary>
    [CreateAssetMenu(fileName = "UiSkin", menuName = "Idle RPG/UI Skin", order = 3)]
    public sealed class UiSkin : ScriptableObject
    {
        /// <summary>Path inside a Resources folder.</summary>
        public const string ResourcePath = "UiSkin";

        [Header("9-slice faces (Kenney Pixel UI: 9-Slice/Colored/grey - tintable, 12 px border)")]
        [Tooltip("Panel face. Tinted by the colours below, so keep it the kit's white/grey piece.")]
        public Sprite panelSprite;

        [Tooltip("Button face. Leave empty to reuse the panel face.")]
        public Sprite buttonSprite;

        [Header("Colours")]
        public Color scrim = new Color(0f, 0f, 0f, 0.65f);
        public Color card = new Color(0.10f, 0.12f, 0.18f, 0.98f);
        public Color title = new Color(0.94f, 0.96f, 1f, 1f);
        public Color body = new Color(1f, 1f, 1f, 0.75f);
        public Color button = new Color(0.30f, 0.36f, 0.50f, 1f);
        public Color accent = new Color(0.84f, 0.62f, 0.26f, 1f);

        private static UiSkin current;

        /// <summary>The project's skin, or a default instance when the asset is missing (never null).</summary>
        public static UiSkin Current
        {
            get
            {
                if (current == null)
                {
                    current = Resources.Load<UiSkin>(ResourcePath);
                }

                if (current == null)
                {
                    // No asset yet: draw flat-coloured cards rather than fail. Cached, so this lookup happens once.
                    current = CreateInstance<UiSkin>();
                    current.hideFlags = HideFlags.HideAndDontSave;
                }

                return current;
            }
        }

        /// <summary>The button face - falls back to the panel face, so one kit piece is enough.</summary>
        public Sprite ResolveButtonSprite()
        {
            return buttonSprite != null ? buttonSprite : panelSprite;
        }
    }
}
