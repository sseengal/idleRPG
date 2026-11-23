using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>What an action button means. The skin owns the art and the ink for each one.</summary>
    public enum ButtonFace
    {
        /// <summary>The old tintable plate. Used where the skin has no art (fallback, lab screens).</summary>
        Plate,
        /// <summary>Gold bar: the main action on a screen (START, REVIVE).</summary>
        Primary,
        /// <summary>Red bar: an action that costs something or ends something (QUIT RUN, END RUN).</summary>
        Danger,
        /// <summary>Slate bar: a secondary action (CANCEL, CLOSE, SOUND).</summary>
        Neutral
    }

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

        [Header("Action buttons (89x19 bar art, 1/3/1/1 border)")]
        [Tooltip("Gold. The main action on a screen: START, REVIVE.")]
        public Sprite primaryButtonSprite;

        [Tooltip("Red. An action that costs or ends something: QUIT RUN, END RUN.")]
        public Sprite dangerButtonSprite;

        [Tooltip("Slate. A secondary action: CANCEL, CLOSE, SOUND.")]
        public Sprite neutralButtonSprite;

        [Header("Panels (75x75 frame, 5/5/5/5 border, baked colour)")]
        [Tooltip("The sheet/modal face: settings, pause. Drawn white-tinted because the art carries its own colour.")]
        public Sprite sheetSprite;

        [Tooltip("Image.pixelsPerUnitMultiplier DIVIDES the border. 1/7 shows the 5 px rim as 35 units.")]
        public float sheetBorderScale = 1f / 7f;

        [Header("Banner (95x23 ribbon, 8/0/8/0 border, baked colour)")]
        [Tooltip("The ribbon that sits across a sheet's top edge. Drawn white-tinted at bannerScale.")]
        public Sprite bannerSprite;

        [Tooltip("Screen units per art pixel. Sets the ribbon height AND the slice scale (border = 1/bannerScale).")]
        public float bannerScale = 7f;

        [Tooltip("Ribbon text ink. White reads on the red face.")]
        public Color bannerInk = new Color32(236, 240, 248, 255);

        [Header("Containers (82x17 flat face, 1 px dark bottom line)")]
        [Tooltip("Flat container: header bar, combat log, stage row, camp strip. Baked colour, drawn white.")]
        public Sprite containerSprite;

        [Tooltip("Image.pixelsPerUnitMultiplier DIVIDES the border. 1/7 shows the 1 px line as 7 units.")]
        public float containerBorderScale = 1f / 7f;

        [Tooltip("Image.pixelsPerUnitMultiplier DIVIDES the slice border, so 1/7 shows a 1 px art line as 7 units.")]
        public float buttonBorderScale = 1f / 7f;

        [Header("Action button ink")]
        public Color primaryButtonInk = new Color32(58, 36, 16, 255);
        public Color dangerButtonInk = new Color32(236, 240, 248, 255);
        public Color neutralButtonInk = new Color32(236, 240, 248, 255);


        [Header("Colours")]
        public Color scrim = UiTheme.ScrimSoft;
        public Color card = UiTheme.TabDim;
        public Color title = UiTheme.Text;
        public Color body = UiTheme.InkSoft;
        public Color button = UiTheme.SheetFace;
        public Color accent = UiTheme.GoldDark;

        [Header("Icons (Tavmir + Fantasy RPG icon packs)")]
        [Tooltip("Currency chip icons. Empty keeps the scene's placeholder icon.")]
        public Sprite goldIcon;
        public Sprite gemIcon;
        public Sprite tokenIcon;

        [Tooltip("Gear slot icons: 0 Weapon, 1 Armor, 2 Trinket. Empty falls back to the slot letter.")]
        public Sprite weaponIcon;
        public Sprite armorIcon;
        public Sprite trinketIcon;

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

        /// <summary>
        /// The art and ink for one action face. False when the skin has no sprite for it, so the caller can fall
        /// back to the tintable plate instead of drawing nothing.
        /// </summary>
        public bool TryResolveButtonFace(ButtonFace face, out Sprite sprite, out Color ink)
        {
            sprite = null;
            ink = Color.white;

            switch (face)
            {
                case ButtonFace.Primary:
                    sprite = primaryButtonSprite;
                    ink = primaryButtonInk;
                    break;
                case ButtonFace.Danger:
                    sprite = dangerButtonSprite;
                    ink = dangerButtonInk;
                    break;
                case ButtonFace.Neutral:
                    sprite = neutralButtonSprite;
                    ink = neutralButtonInk;
                    break;
                default:
                    return false;
            }

            return sprite != null;
        }

        /// <summary>
        /// Paints a flat container (header bar, log band, stage row, camp strip) with the skin's container art.
        /// Returns false when the skin has none, so the caller can keep its old face.
        /// </summary>
        public bool ApplyContainer(Image image, Color? tint = null)
        {
            if (image == null || containerSprite == null)
            {
                return false;
            }

            image.sprite = containerSprite;
            image.type = Image.Type.Sliced;
            image.color = tint ?? UiTheme.ContainerFace;
            image.pixelsPerUnitMultiplier = containerBorderScale;
            return true;
        }
    }
}
