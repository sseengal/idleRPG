using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// The plate button every screen shares: the container face, a smooth idle/selected colour hand-off, a press
    /// squash, and a true grayscale when disabled (UIEffect).
    ///
    /// ELI5: the one button recipe. It wears the same 9-slice plate as everything else, but the plate's border is
    /// drawn thinner so the button reads as a crisp edge instead of a thick slab, and a press darkens the face
    /// anyway (a gold button darkens too, without turning navy).
    ///
    /// The bottom tabs use <see cref="FantasyTabButton"/>, which adds the lift, the breathing halo and the
    /// icon/word pose; everything else uses this one so a button feels the same on every page.
    /// </summary>
    public sealed class UiPlateButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        /// <summary>How much a pressed button darkens its own face. Works on any face colour, so gold darkens too.</summary>
        private const float PressDarken = 0.18f;

        /// <summary>A scale this close to its target counts as arrived, so the tween stops writing.</summary>
        private const float ScaleEpsilon = 0.0005f;

        /// <summary>A colour channel this close to its target counts as arrived (about half a 1/255 step).</summary>
        private const float ColorEpsilon = 0.002f;

        /// <summary>
        /// Divides the plate sprite's 18-pixel border, so a 186-unit-tall button shows a ~7-unit edge instead of a
        /// 10%-of-the-button slab. One number, every button in the game.
        /// </summary>
        private const float BorderSlim = 2.5f;

        private RectTransform root;
        private Image frame;
        private TextMeshProUGUI label;
        private UIEffect effect;
        private Button button;

        private Color idleFill = UiTheme.ButtonBlue;
        private Color activeFill = UiTheme.SelectedBlue;
        private Color inkIdle = UiTheme.ActionInk;
        private bool selected, interactable = true;
        private float press;

        /// <summary>The underlying selectable, for callers that only need the click or the raycast target.</summary>
        public Button Button => button;

        /// <summary>Builds one button: plate face + centred label, already wired for all four states.</summary>
        public static UiPlateButton Create(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            string text, UiType.Role role, Color idle, Color active, UnityEngine.Events.UnityAction onClick,
            ButtonFace face = ButtonFace.Plate)
        {
            // One source of truth: the skin owns the art, its ink and the border scale. A missing sprite falls
            // back to the tintable plate, so an empty skin can never leave a button invisible.
            Sprite art = null;
            Color artInk = Color.white;
            float borderScale = 1f;
            Color tint = idle;

            if (face != ButtonFace.Plate && UiSkin.Current.TryResolveButtonFace(face, out Sprite resolved, out Color resolvedInk))
            {
                art = resolved;
                artInk = resolvedInk;
                borderScale = UiSkin.Current.buttonBorderScale;
                tint = Color.white;
            }

            Image image = UiRuntime.Plate(parent, name, anchorMin, anchorMax, tint, raycast: true);

            if (art != null)
            {
                // Art that carries its own colour is drawn untinted at its own border scale.
                image.sprite = art;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = borderScale;
            }

            Button selectable = image.gameObject.AddComponent<Button>();
            selectable.targetGraphic = image;
            selectable.transition = Selectable.Transition.None;

            TextMeshProUGUI labelText = UiRuntime.CreateText(image.transform, "Label", text, role,
                TextAlignmentOptions.Center, UiTheme.Text);
            UiType.ApplyAuto(labelText, role);
            UiRuntime.Stretch(labelText.rectTransform);
            labelText.raycastTarget = false;

            UIEffect uiEffect = image.gameObject.AddComponent<UIEffect>();
            UiPlateButton view = image.gameObject.AddComponent<UiPlateButton>();
            view.Bind(image.rectTransform, image, labelText, uiEffect, selectable, tint, tint,
                art == null, art != null ? artInk : (Color?)null);

            if (onClick != null)
            {
                selectable.onClick.AddListener(onClick);
            }

            return view;
        }

        public void Bind(RectTransform rootRect, Image frameImage, TextMeshProUGUI labelText, UIEffect uiEffect,
            Button buttonRef, Color idle, Color active, bool slimBorder = true, Color? ink = null)
        {
            root = rootRect;
            frame = frameImage;
            label = labelText;
            effect = uiEffect;
            button = buttonRef;
            idleFill = idle;
            activeFill = active;
            inkIdle = ink ?? UiTheme.ActionInk;

            // A slimmer bevel than the raw sprite: 18 px on a 186-unit button reads as a slab, 7 reads as an edge.
            // Art with its own tight border (the gold START bar) sets its scale in Create, so it is skipped here.
            if (frame != null && slimBorder)
            {
                frame.pixelsPerUnitMultiplier = BorderSlim;
            }

            Refresh();
        }

        public void SetSelected(bool value)
        {
            if (selected == value) { return; }
            selected = value;
        }

        /// <summary>Change the face colours (a node that changes state). The colour eases to the new idle.</summary>
        public void SetFaces(Color idle, Color active)
        {
            idleFill = idle;
            activeFill = active;
        }

        /// <summary>Disabling is a true grayscale drain (UIEffect), not just a dead raycast.</summary>
        public void SetInteractable(bool value)
        {
            interactable = value;
            if (button != null) { button.interactable = value; }
            Refresh();
        }

        public void OnPointerDown(PointerEventData e) { if (interactable) { press = 1f; } }
        public void OnPointerUp(PointerEventData e) { press = 0f; }
        public void OnPointerExit(PointerEventData e) { press = 0f; }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float smooth = 1f - Mathf.Exp(-16f * dt);

            if (root != null)
            {
                // Pressed: squash toward 95.5% and spring back - the same feel as the tabs.
                float target = Mathf.Lerp(1f, 0.955f, press);
                Vector3 scale = root.localScale;
                float k = 1f - Mathf.Exp(-20f * dt);
                float x = Mathf.Lerp(scale.x, target, k);
                float y = Mathf.Lerp(scale.y, target, k);

                // A settled button must stop writing. One page can hold over a thousand buttons, and every
                // write reaches the native transform and graphic. Compare first, write last.
                if (Mathf.Abs(x - target) < ScaleEpsilon) { x = target; }
                if (Mathf.Abs(y - target) < ScaleEpsilon) { y = target; }

                if (x != scale.x || y != scale.y)
                {
                    scale.x = x;
                    scale.y = y;
                    root.localScale = scale;
                }
            }

            if (frame != null)
            {
                // Pressed darkens whatever face this button has, so a gold button does not flash navy.
                Color face = selected ? activeFill : idleFill;
                Color want = press > 0f ? Color.Lerp(face, Color.black, PressDarken) : face;
                Color now = frame.color;
                Color next = Color.Lerp(now, want, smooth);

                if (Near(next, want)) { next = want; }
                if (!Near(next, now)) { frame.color = next; }
            }

            if (label != null)
            {
                Color ink = interactable ? inkIdle : UiTheme.InkOff;
                Color now = label.color;
                Color next = Color.Lerp(now, ink, smooth);

                if (Near(next, ink)) { next = ink; }
                if (!Near(next, now)) { label.color = next; }
            }
        }

        /// <summary>True when two colours are within one visible step. Used to end a tween exactly on target.</summary>
        private static bool Near(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < ColorEpsilon && Mathf.Abs(a.g - b.g) < ColorEpsilon &&
                   Mathf.Abs(a.b - b.b) < ColorEpsilon && Mathf.Abs(a.a - b.a) < ColorEpsilon;
        }

        private void Refresh()
        {
            if (effect == null) { return; }

            // Only the disabled state uses UIEffect: a true grayscale drain.
            effect.toneFilter = interactable ? ToneFilter.None : ToneFilter.Grayscale;
            effect.toneIntensity = interactable ? 0f : 1f;
        }
    }
}
