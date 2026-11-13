using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// One bottom-bar tab with all four states: idle, selected, pressed, disabled.
    /// The selected tab holds a steady gold face plus a gold halo that breathes; the face is
    /// never washed to white (regular gold). Disabled is a true grayscale, via UIEffect.
    /// </summary>
    public sealed class FantasyTabButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private RectTransform root;
        private Image frame;
        private Image glow;
        private TextMeshProUGUI label;
        private Image icon;
        private RectTransform iconRect;
        private float iconAspect = 1f;
        private float iconHeightFactor = 1f;
        private float iconSelectedScale = 1f;
        private float iconVerticalOffset = 0f;
        private float iconSelectedVerticalOffset = 0f;
        private UIEffect effect;
        private Button button;

        private Color idleFill, activeFill, idleInk, activeInk;
        private bool selected, interactable = true;
        private float press, glowClock;

        /// <summary>How far the selected tab rises above its neighbours, in canvas units.</summary>
        private const float LiftPixels = 30f;

        public Image Frame => frame;

        public void Bind(RectTransform rootRect, Image frameImage, Image glowImage, TextMeshProUGUI labelText,
            UIEffect uiEffect, Button buttonRef, Color idle, Color active, Color idleText, Color activeText,
            Image iconImage = null, float iconScale = 1f, float selectedScale = 1f, float verticalOffset = 0f,
            float selectedVerticalOffset = 0f)
        {
            root = rootRect;
            frame = frameImage;
            glow = glowImage;
            label = labelText;
            effect = uiEffect;
            button = buttonRef;
            icon = iconImage;
            iconRect = icon != null ? icon.rectTransform : null;
            iconHeightFactor = iconScale;
            iconSelectedScale = selectedScale;
            iconVerticalOffset = verticalOffset;
            iconSelectedVerticalOffset = selectedVerticalOffset;
            iconAspect = icon != null && icon.sprite != null ? icon.sprite.rect.width / icon.sprite.rect.height : 1f;
            idleFill = idle;
            activeFill = active;
            idleInk = idleText;
            activeInk = activeText;

            if (glow != null)
            {
                glow.sprite = GlowSprite;
                glow.raycastTarget = false;
                Color c = glow.color;
                c.a = 0f;
                glow.color = c;
            }

            Refresh();
        }

        public void SetSelected(bool value)
        {
            if (selected == value) { return; }
            selected = value;
            Refresh();
        }

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
                // Selected: the tab rises so it pops above its neighbours.
                float lift = selected && interactable ? LiftPixels : 0f;
                Vector2 max = root.offsetMax;
                max.y = Mathf.Lerp(max.y, lift, smooth);
                root.offsetMax = max;

                Vector2 min = root.offsetMin;
                min.y = Mathf.Lerp(min.y, lift, smooth);
                root.offsetMin = min;

                // Pressed: squash toward 95.5% and spring back.
                float target = Mathf.Lerp(1f, 0.955f, press);
                Vector3 s = root.localScale;
                float k = 1f - Mathf.Exp(-20f * dt);
                s.x = Mathf.Lerp(s.x, target, k);
                s.y = Mathf.Lerp(s.y, target, k);
                root.localScale = s;
            }

            // Smooth colour hand-off between idle and selected.
            if (frame != null) { frame.color = Color.Lerp(frame.color, selected ? activeFill : idleFill, smooth); }
            if (label != null)
            {
                Color ink = !interactable ? new Color(0.55f, 0.55f, 0.60f, 1f) : (selected ? activeInk : idleInk);
                ink.a = selected && interactable ? 1f : 0f;   // the word only speaks for the chosen tab
                label.color = Color.Lerp(label.color, ink, smooth);
            }

            if (iconRect != null)
            {
                // One shared visual HEIGHT for every icon (width follows each sprite's aspect), so a tall icon
                // and a wide one read as one set. Idle: centred. Selected: slightly smaller and dropped so the
                // word tucks straight under it.
                float tabH = root != null ? root.rect.height : 0f;
                if (tabH > 1f)
                {
                    // iconHeightFactor is the idle-only size; iconSelectedScale the selected-only size.
                    float frac = selected ? 0.50f * iconSelectedScale : 0.66f * iconHeightFactor;
                    float height = tabH * frac;
                    float width = height * iconAspect;
                    // Grow from a fixed bottom: idle sits low; selected keeps its bottom (above the word), so a
                    // bigger icon grows upward instead of crowding the text. Per-icon vertical nudge centres an
                    // off-balance silhouette (idle) or lifts one above its word (selected).
                    float vertical = selected ? iconSelectedVerticalOffset : iconVerticalOffset;
                    float bottom = tabH * (selected ? 0.35f : 0.19f) + height * vertical;
                    float centreFromBottom = bottom + height * 0.5f;

                    Vector2 size = iconRect.sizeDelta;
                    Vector2 pos = iconRect.anchoredPosition;

                    if (size.y < 1f)
                    {
                        size = new Vector2(width, height);
                        pos = new Vector2(0f, centreFromBottom);
                    }
                    else
                    {
                        size.x = Mathf.Lerp(size.x, width, smooth);
                        size.y = Mathf.Lerp(size.y, height, smooth);
                        pos.x = 0f;
                        pos.y = Mathf.Lerp(pos.y, centreFromBottom, smooth);
                    }

                    iconRect.sizeDelta = size;
                    iconRect.anchoredPosition = pos;
                }
            }

            // Selected: a gold halo breathes at 1.6 s.
            if (glow != null)
            {
                glowClock += dt;
                bool live = selected && interactable;
                float pulse = 0.5f + 0.5f * Mathf.Sin(glowClock * (2f * Mathf.PI / 1.6f));
                float a = live ? 0.34f * (0.7f + 0.3f * pulse) : 0f;
                Color c = glow.color;
                c.a = Mathf.Lerp(c.a, a, 1f - Mathf.Exp(-12f * dt));
                glow.color = c;
            }
        }

        private void Refresh()
        {
            if (effect == null) { return; }

            // Only the disabled state uses UIEffect: a true grayscale drain.
            effect.toneFilter = interactable ? ToneFilter.None : ToneFilter.Grayscale;
            effect.toneIntensity = interactable ? 0f : 1f;
        }

        private static Sprite glowSprite;

        private static Sprite GlowSprite
        {
            get
            {
                if (glowSprite != null) { return glowSprite; }

                const int size = 64;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Vector2 centre = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
                float maxR = size * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), centre) / maxR;
                        float a = Mathf.Clamp01(1f - d);
                        a *= a;
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }

                tex.Apply();
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                glowSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
                glowSprite.name = "FantasyTabGlow";
                return glowSprite;
            }
        }
    }
}
