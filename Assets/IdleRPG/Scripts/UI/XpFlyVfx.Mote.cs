using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// One XP mote: a short pop out of the enemy, then a bowed pull into the bar, with a halo and a small trail.
    /// Not a MonoBehaviour: the pool ticks it, like the gold coins.
    /// </summary>
    public sealed partial class XpFlyVfx : MonoBehaviour
    {
        private sealed class Mote
        {
            private readonly XpFlyVfx owner;
            private readonly RectTransform rect;
            private readonly Image dot;
            private readonly Image halo;
            private readonly Image[] ghostImages;

            private readonly Vector2[] history;
            private int historyCount;

            private Vector2 end;
            private Vector2 position;
            private Vector2 velocity;
            private Vector2 pullStart;
            private float delay;
            private float burstEnd;
            private float bornAt;
            private float phase;
            private float bow;
            private bool pullStarted;

            public Mote(XpFlyVfx effect, RectTransform rectTransform, Image dotImage, Image haloImage,
                Image[] ghosts)
            {
                owner = effect;
                rect = rectTransform;
                dot = dotImage;
                halo = haloImage;
                ghostImages = ghosts;
                history = new Vector2[Mathf.Max(2, ghostImages != null ? ghostImages.Length + 1 : 2)];
            }

            public void Start(Vector2 startLocal, Vector2 endLocal, Vector2 direction, float speed, float stagger,
                float curveSide)
            {
                end = endLocal;
                position = startLocal;
                velocity = direction * speed;
                delay = stagger;
                bow = curveSide;
                phase = Random.value * Mathf.PI * 2f;
                bornAt = Time.unscaledTime;
                burstEnd = Time.unscaledTime + owner.burstDurationSec;
                pullStarted = false;
                historyCount = 0;

                if (rect != null)
                {
                    rect.gameObject.SetActive(true);
                    rect.anchoredPosition = startLocal;
                    rect.localScale = Vector3.one * 0.65f;
                }

                if (dot != null)
                {
                    dot.color = UiTheme.XpFillLight;
                    dot.rectTransform.localScale = Vector3.one;
                }

                if (halo != null)
                {
                    halo.color = UiTheme.XpTrail;
                    halo.rectTransform.localScale = Vector3.one;
                }

                HideGhosts();
            }

            private void HideGhosts()
            {
                if (ghostImages == null)
                {
                    return;
                }

                for (int g = 0; g < ghostImages.Length; g++)
                {
                    if (ghostImages[g] != null)
                    {
                        ghostImages[g].enabled = false;
                    }
                }
            }

            public bool Tick(float dt)
            {
                float now = Time.unscaledTime;

                if (now < burstEnd)
                {
                    velocity.y -= owner.burstGravity * dt;
                    velocity *= Mathf.Max(0f, 1f - owner.burstDamping * dt);
                    position += velocity * dt;
                }
                else
                {
                    if (!pullStarted)
                    {
                        pullStart = position;
                        pullStarted = true;
                    }

                    float t = Mathf.Clamp01((now - burstEnd - delay) /
                                            Mathf.Max(0.0001f, owner.pullDurationSec));

                    if (t >= 1f)
                    {
                        Land();
                        return false;
                    }

                    Vector2 delta = end - pullStart;
                    Vector2 perpendicular = delta.sqrMagnitude > 0.0001f
                        ? new Vector2(-delta.y, delta.x).normalized
                        : Vector2.zero;

                    float eased = t * t * t;
                    position = Vector2.Lerp(pullStart, end, eased)
                               + perpendicular * (Mathf.Sin(t * Mathf.PI) * bow * owner.curveAmount);
                }

                if (rect != null)
                {
                    rect.anchoredPosition = position;

                    // A tiny pop at birth: the eye catches the mote even when the burst is behind a coin flash.
                    float pop = Mathf.Clamp01((now - bornAt) / 0.12f);
                    pop = 1f - (1f - pop) * (1f - pop);
                    float scale = Mathf.Lerp(0.65f, 1f, pop);
                    rect.localScale = new Vector3(scale, scale, 1f);
                }

                if (halo != null)
                {
                    float pulse = 1f + 0.10f * Mathf.Sin(now * 14f + phase);
                    halo.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
                }

                PushHistory(position);
                DrawHistory(position);
                return true;
            }

            private void Land()
            {
                if (rect != null)
                {
                    rect.gameObject.SetActive(false);
                }

                owner.OnMoteLanded();
            }

            private void PushHistory(Vector2 point)
            {
                for (int i = history.Length - 1; i > 0; i--)
                {
                    history[i] = history[i - 1];
                }

                history[0] = point;
                historyCount = Mathf.Min(history.Length, historyCount + 1);
            }

            private void DrawHistory(Vector2 current)
            {
                if (ghostImages == null)
                {
                    return;
                }

                for (int g = 0; g < ghostImages.Length; g++)
                {
                    Image ghost = ghostImages[g];

                    if (ghost == null)
                    {
                        continue;
                    }

                    int index = g + 1;

                    if (index >= historyCount || index >= history.Length)
                    {
                        ghost.enabled = false;
                        continue;
                    }

                    ghost.enabled = true;

                    float age = historyCount > 1 ? (float)index / (historyCount - 1) : 1f;
                    Color color = UiTheme.XpFillLight;
                    color.a = 0.45f * (1f - age);
                    ghost.color = color;

                    if (ghost.rectTransform != null)
                    {
                        ghost.rectTransform.anchoredPosition = history[index] - current;
                    }
                }
            }
        }
    }
}

