using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// The chunky plate button every screen shares: the ui_frame face, a smooth idle/selected colour hand-off,
    /// a press squash, and a true grayscale when disabled (UIEffect).
    ///
    /// The bottom tabs use <see cref="FantasyTabButton"/>, which adds the lift, the breathing halo and the
    /// icon/word pose; everything else uses this one so a button feels the same on every page.
    /// </summary>
    public sealed class UiPlateButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private RectTransform root;
        private Image frame;
        private TextMeshProUGUI label;
        private UIEffect effect;
        private Button button;

        private Color idleFill = UiTheme.ButtonBlue;
        private Color activeFill = UiTheme.SelectedBlue;
        private bool selected, interactable = true;
        private float press;

        /// <summary>The underlying selectable, for callers that only need the click or the raycast target.</summary>
        public Button Button => button;

        /// <summary>Builds one button: plate face + centred label, already wired for all four states.</summary>
        public static UiPlateButton Create(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            string text, UiType.Role role, Color idle, Color active, UnityEngine.Events.UnityAction onClick)
        {
            Image face = UiRuntime.Plate(parent, name, anchorMin, anchorMax, idle, raycast: true);

            Button selectable = face.gameObject.AddComponent<Button>();
            selectable.targetGraphic = face;
            selectable.transition = Selectable.Transition.None;

            TextMeshProUGUI labelText = UiRuntime.CreateText(face.transform, "Label", text, role,
                TextAlignmentOptions.Center, UiTheme.Text);
            UiType.ApplyAuto(labelText, role);
            UiRuntime.Stretch(labelText.rectTransform);
            labelText.raycastTarget = false;

            UIEffect uiEffect = face.gameObject.AddComponent<UIEffect>();
            UiPlateButton view = face.gameObject.AddComponent<UiPlateButton>();
            view.Bind(face.rectTransform, face, labelText, uiEffect, selectable, idle, active);

            if (onClick != null)
            {
                selectable.onClick.AddListener(onClick);
            }

            return view;
        }

        public void Bind(RectTransform rootRect, Image frameImage, TextMeshProUGUI labelText, UIEffect uiEffect,
            Button buttonRef, Color idle, Color active)
        {
            root = rootRect;
            frame = frameImage;
            label = labelText;
            effect = uiEffect;
            button = buttonRef;
            idleFill = idle;
            activeFill = active;
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
                scale.x = Mathf.Lerp(scale.x, target, k);
                scale.y = Mathf.Lerp(scale.y, target, k);
                root.localScale = scale;
            }

            if (frame != null)
            {
                frame.color = Color.Lerp(frame.color, selected ? activeFill : idleFill, smooth);
            }

            if (label != null)
            {
                Color ink = interactable ? UiTheme.Text : UiTheme.InkDisabled;
                label.color = Color.Lerp(label.color, ink, smooth);
            }
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
