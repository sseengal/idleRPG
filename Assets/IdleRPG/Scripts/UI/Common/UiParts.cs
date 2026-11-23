using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>What a surface is for. A screen asks for a KIND; it never picks a sprite or a colour.</summary>
    public enum PanelKind
    {
        /// <summary>The ornate gold-rimmed sheet frame (pause, modal cards).</summary>
        Frame,
        /// <summary>The blue modal body that carries a ribbon.</summary>
        Modal,
        /// <summary>A flat band: header, log, stage row, camp strip.</summary>
        Band,
        /// <summary>A card or list row that sits on a band.</summary>
        Card,
        /// <summary>A small chip: currency, cost, tag.</summary>
        Chip
    }

    /// <summary>
    /// THE one component for UI surfaces. Every panel, card, chip, modal, scrim and ribbon goes through here,
    /// so art, tint, border scale and ribbon behaviour live in one place. Screens pass a <see cref="PanelKind"/>
    /// and a rectangle - never a sprite, never a colour, never a border number.
    ///
    /// ELI5: the parts box. Ask for "a modal" and you get the right picture, colour and edges, every time.
    /// </summary>
    public static class UiParts
    {
        /// <summary>
        /// The one drop shadow for every surface and button. Two offset copies fake a soft falloff: one hard copy
        /// reads as a duplicate, not a shadow. Values are shared so nothing drifts.
        /// </summary>
        public const float ShadowNearAlpha = 0.38f;
        public const float ShadowNearOffset = -5f;
        public const float ShadowFarAlpha = 0.18f;
        public const float ShadowFarOffset = -12f;

        /// <summary>Adds the shared two-step drop shadow to a graphic's GameObject.</summary>
        public static void AddShadow(GameObject node)
        {
            if (node == null)
            {
                return;
            }

            Shadow near = node.AddComponent<Shadow>();
            near.effectColor = UiTheme.WithAlpha(UiTheme.Slate950, ShadowNearAlpha);
            near.effectDistance = new Vector2(0f, ShadowNearOffset);
            near.useGraphicAlpha = true;

            Shadow far = node.AddComponent<Shadow>();
            far.effectColor = UiTheme.WithAlpha(UiTheme.Slate950, ShadowFarAlpha);
            far.effectDistance = new Vector2(0f, ShadowFarOffset);
            far.useGraphicAlpha = true;
        }

        /// <summary>A surface, anchored to a rectangle inside its parent. A tint overrides the kind's colour.</summary>
        public static Image Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            PanelKind kind, bool raycast = false, Color? tint = null, bool shadow = true)
        {
            Image image = Build(parent, name, kind, raycast, tint, shadow);
            UiRuntime.Anchor(image.rectTransform, anchorMin, anchorMax);
            return image;
        }

        /// <summary>A surface stretched over its whole parent.</summary>
        public static Image Fill(Transform parent, string name, PanelKind kind, bool raycast = false, Color? tint = null)
        {
            Image image = Build(parent, name, kind, raycast, tint, true);
            UiRuntime.Stretch(image.rectTransform);
            return image;
        }

        /// <summary>The dimming layer behind a modal. Also the tap catcher.</summary>
        public static Image Scrim(Transform parent, string name = "Scrim", bool raycast = true)
        {
            Image image = UiRuntime.CreatePanel(parent, null, UiTheme.Scrim, raycast);
            image.gameObject.name = name;
            UiRuntime.Stretch(image.rectTransform);
            return image;
        }

        /// <summary>The ribbon across a sheet's top edge. One builder for every sheet.</summary>
        public static TextMeshProUGUI Banner(Transform panel, string text)
        {
            return UiBanner.Build(panel, text);
        }

        private static Image Build(Transform parent, string name, PanelKind kind, bool raycast, Color? tintOverride,
            bool shadow)
        {
            UiSkin skin = UiSkin.Current;

            Sprite sprite;
            Color tint;
            float scale;

            switch (kind)
            {
                case PanelKind.Frame:
                    sprite = skin.frameSprite;
                    tint = Color.white;
                    scale = skin.frameBorderScale;
                    break;

                case PanelKind.Modal:
                    sprite = skin.containerSprite;
                    tint = UiTheme.ContainerFace;
                    scale = skin.containerBorderScale;
                    break;

                case PanelKind.Card:
                    sprite = skin.containerSprite;
                    tint = UiTheme.SurfaceCard;
                    scale = skin.containerBorderScale;
                    break;

                case PanelKind.Chip:
                    sprite = skin.containerSprite;
                    tint = UiTheme.SurfaceRaised;
                    scale = skin.containerBorderScale;
                    break;

                case PanelKind.Band:
                default:
                    sprite = skin.containerSprite;
                    tint = UiTheme.ContainerFace;
                    scale = skin.containerBorderScale;
                    break;
            }

            Image image = UiRuntime.CreatePanel(parent, sprite, tintOverride ?? tint, raycast);
            image.gameObject.name = name;

            if (sprite != null)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = scale;
            }

            // Every surface stands proud of what is behind it, unless it sits flush inside another one.
            if (shadow)
            {
                AddShadow(image.gameObject);
            }

            return image;
        }
    }
}
