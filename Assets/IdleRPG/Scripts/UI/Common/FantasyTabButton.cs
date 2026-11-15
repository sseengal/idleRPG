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

        /// <summary>A lift this close to its target counts as arrived, so the tween stops writing (canvas units).</summary>
        private const float LiftEpsilon = 0.01f;

        /// <summary>A scale this close to its target counts as arrived, so the tween stops writing.</summary>
        private const float ScaleEpsilon = 0.0005f;

        /// <summary>A colour channel this close to its target counts as arrived (about half a 1/255 step).</summary>
        private const float ColorEpsilon = 0.002f;

        /// <summary>An icon size or offset this close to its target counts as arrived (canvas units).</summary>
        private const float IconEpsilon = 0.01f;

        /// <summary>A halo alpha this close to its target counts as arrived.</summary>
        private const float GlowEpsilon = 0.004f;

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

            // Every write below reaches a native transform or graphic, so each one is compared first:
            // a settled tab must cost nothing. The selected tab keeps its breathing halo on purpose.
            if (root != null)
            {
                // Selected: the tab rises so it pops above its neighbours.
                float lift = selected && interactable ? LiftPixels : 0f;

                Vector2 max = root.offsetMax;
                float maxY = Mathf.Lerp(max.y, lift, smooth);
                if (Mathf.Abs(maxY - lift) < LiftEpsilon) { maxY = lift; }

                if (maxY != max.y)
                {
                    max.y = maxY;
                    root.offsetMax = max;
                }

                Vector2 min = root.offsetMin;
                float minY = Mathf.Lerp(min.y, lift, smooth);
                if (Mathf.Abs(minY - lift) < LiftEpsilon) { minY = lift; }

                if (minY != min.y)
                {
                    min.y = minY;
                    root.offsetMin = min;
                }

                // Pressed: squash toward 95.5% and spring back.
                float target = Mathf.Lerp(1f, 0.955f, press);
                Vector3 s = root.localScale;
                float k = 1f - Mathf.Exp(-20f * dt);
                float sx = Mathf.Lerp(s.x, target, k);
                float sy = Mathf.Lerp(s.y, target, k);
                if (Mathf.Abs(sx - target) < ScaleEpsilon) { sx = target; }
                if (Mathf.Abs(sy - target) < ScaleEpsilon) { sy = target; }

                if (sx != s.x || sy != s.y)
                {
                    s.x = sx;
                    s.y = sy;
                    root.localScale = s;
                }
            }

            // Smooth colour hand-off between idle and selected.
            if (frame != null)
            {
                Color want = selected ? activeFill : idleFill;
                Color now = frame.color;
                Color next = Color.Lerp(now, want, smooth);

                if (Near(next, want)) { next = want; }
                if (!Near(next, now)) { frame.color = next; }
            }

            if (label != null)
            {
                Color ink = !interactable ? UiTheme.InkDisabled : (selected ? activeInk : idleInk);
                ink.a = selected && interactable ? 1f : 0f;   // the word only speaks for the chosen tab
                Color now = label.color;
                Color next = Color.Lerp(now, ink, smooth);

                if (Near(next, ink)) { next = ink; }
                if (!Near(next, now)) { label.color = next; }
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
                        iconRect.sizeDelta = new Vector2(width, height);
                        iconRect.anchoredPosition = new Vector2(0f, centreFromBottom);
                    }
                    else
                    {
                        float w = Mathf.Lerp(size.x, width, smooth);
                        float h = Mathf.Lerp(size.y, height, smooth);
                        float y = Mathf.Lerp(pos.y, centreFromBottom, smooth);
                        if (Mathf.Abs(w - width) < IconEpsilon) { w = width; }
                        if (Mathf.Abs(h - height) < IconEpsilon) { h = height; }
                        if (Mathf.Abs(y - centreFromBottom) < IconEpsilon) { y = centreFromBottom; }

                        bool sizeMoved = w != size.x || h != size.y;
                        bool posMoved = pos.x != 0f || y != pos.y;

                        if (sizeMoved) { iconRect.sizeDelta = new Vector2(w, h); }
                        if (posMoved) { iconRect.anchoredPosition = new Vector2(0f, y); }
                    }
                }
            }

            // Selected: a gold halo breathes at 1.6 s. Every other tab stops once the halo is dark.
            if (glow != null)
            {
                glowClock += dt;
                bool live = selected && interactable;

                float a;
                if (live)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(glowClock * (2f * Mathf.PI / 1.6f));
                    a = 0.34f * (0.7f + 0.3f * pulse);
                }
                else
                {
                    a = 0f;
                }

                Color c = glow.color;
                float nextA = Mathf.Lerp(c.a, a, 1f - Mathf.Exp(-12f * dt));
                if (Mathf.Abs(nextA - a) < GlowEpsilon) { nextA = a; }

                if (nextA != c.a)
                {
                    c.a = nextA;
                    glow.color = c;
                }
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
