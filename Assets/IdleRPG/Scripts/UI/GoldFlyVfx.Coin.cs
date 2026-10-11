using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;

namespace IdleRPG.UI
{
    /// <summary>
    /// The coin itself: its burst out of the enemy, the moment it turns and magnetises to the counter, and the
    /// comet trail of fading ghosts behind it. One instance per visible coin, recycled by <c>GoldFlyVfx.Pool</c>.
    /// </summary>
    public sealed partial class GoldFlyVfx : MonoBehaviour
    {
        /// <summary>
        /// One flying coin. Two acts only:
        ///   1. BURST  - pop out with its own fan direction, fake gravity, and damping (it decelerates).
        ///   2. MAGNET - as soon as the burst runs out, the coin is pulled to the counter and melts in.
        /// No settle, no bounce, no pause — the pull starts the instant the burst decays.
        /// Not a MonoBehaviour: the pool ticks it.
        /// </summary>
        private sealed class Coin
        {
            private readonly GoldFlyVfx owner;
            private readonly RectTransform rect;
            private readonly Image dot;
            private readonly Image[] ghostImages;

            // Fixed per-instance trail history (indices: 0 = newest, N-1 = oldest).
            private readonly Vector2[] history;
            private int historyCount;
            private bool arrived;

            private Vector2 start;
            private Vector2 end;
            private Vector2 position;
            private Vector2 velocity;
            private Vector2 magnetStart;
            private float magnetDelay;
            private float burstEnd;
            private float settleEnd;
            private bool absorbStartedAtSet;
            private float absorbStartedAt = -1f;

            public Coin(GoldFlyVfx effect, RectTransform rectTransform, Image dotImage, Image[] ghosts)
            {
                owner = effect;
                rect = rectTransform;
                dot = dotImage;
                ghostImages = ghosts;
                history = new Vector2[Mathf.Max(2, ghostImages != null ? ghostImages.Length : 2)];
            }

            public void Start(Vector2 startLocal, Vector2 endLocal, Vector2 direction, float speed,
                float stagger)
            {
                start = startLocal;
                end = endLocal;
                position = startLocal;
                velocity = direction * speed;
                magnetDelay = stagger;

                burstEnd = Time.time + owner.burstDurationSec;
                settleEnd = burstEnd; // magnet begins exactly when the burst ends — no pause.

                arrived = false;
                historyCount = 0;
                absorbStartedAt = -1f;
                absorbStartedAtSet = false;

                if (rect != null)
                {
                    rect.gameObject.SetActive(true);
                    rect.anchoredPosition = start;
                    rect.localScale = Vector3.one;
                }

                if (dot != null)
                {
                    dot.color = owner.coinColor;
                    dot.rectTransform.localScale = Vector3.one;
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

            /// <summary>Advances the coin. Returns false when the coin is finished (pool it).</summary>
            public bool Tick(float deltaTime)
            {
                if (rect == null)
                {
                    return false;
                }

                float now = Time.time;

                if (now <= burstEnd)
                {
                    // Act 1 - burst: integrate velocity with fake gravity + damping so the coin visibly
                    // decelerates right before the magnet picks it up.
                    velocity += Vector2.down * (owner.burstGravity * deltaTime);
                    velocity *= Mathf.Exp(-owner.burstDamping * deltaTime);
                    position += velocity * deltaTime;
                }
                else
                {
                    // Act 2 - magnet: pull starts immediately at the burst-decay point, no rest frame in between.
                    position = Magnet(now);
                }

                rect.anchoredPosition = position;

                // Absorb: once the coin reaches the counter it shrinks + fades into the number, then recycles.
                if (absorbStartedAt >= 0f)
                {
                    float soak = now - absorbStartedAt;
                    float fade = Mathf.Clamp01(1f - soak / 0.12f);

                    if (dot != null)
                    {
                        Color color = dot.color;
                        color.a = fade;
                        dot.color = color;
                    }

                    rect.localScale = new Vector3(fade > 0f ? fade : 0.01f, fade > 0f ? fade : 0.01f, 1f);

                    if (fade <= 0f)
                    {
                        return false;
                    }
                }

                PushHistory(position);
                DrawHistory(position);
                return true;
            }

            /// <summary>Act 2 - magnet: the coin is pulled to the counter from wherever its burst decayed to.</summary>
            private Vector2 Magnet(float now)
            {
                if (!absorbStartedAtSet)
                {
                    // First magnet frame: lock the pull to start from the exact spot the burst decayed to,
                    // so the hand-off is continuous (no teleport, no pause).
                    magnetStart = position;
                    absorbStartedAtSet = true;
                }

                float t = Mathf.Clamp01((now - settleEnd - magnetDelay) / Mathf.Max(0.0001f, owner.magnetDurationSec));

                if (t >= 1f)
                {
                    if (!arrived)
                    {
                        arrived = true;
                        // Counter count-up starts when the FIRST coin of a burst lands.
                        HudHeaderUI.Instance?.SignalCoinsArrived();
                        absorbStartedAt = now;
                    }

                    // Hold briefly at the counter so the absorb fade starts from the exact spot.
                    return end;
                }

                // Ease-in (accelerating pull from the decayed rest), with an ease-out 'soften' tail at the end.
                float eased = EaseInCubic(t);
                Vector2 pulled = Vector2.Lerp(magnetStart, end, eased);

                if (t > 0.8f)
                {
                    pulled = Vector2.Lerp(pulled, end, EaseOutCubic((t - 0.8f) / 0.2f));
                }

                return pulled;
            }

            private void PushHistory(Vector2 position)
            {
                for (int i = history.Length - 1; i > 0; i--)
                {
                    history[i] = history[i - 1];
                }

                history[0] = position;
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

                    // Ghost g shows the position 1 slot older than the dot's own. The oldest ghosts fall off.
                    int index = g + 1;
                    if (index >= historyCount || index >= history.Length)
                    {
                        ghost.enabled = false;
                        continue;
                    }

                    ghost.enabled = true;

                    float age = historyCount > 1 ? (float)index / (historyCount - 1) : 1f;
                    Color color = owner.coinColor;
                    color.a = owner.trailAlpha * (1f - age);
                    ghost.color = color;

                    if (ghost.rectTransform != null)
                    {
                        ghost.rectTransform.anchoredPosition = history[index] - current;
                    }
                }
            }

            private static float EaseInCubic(float t)
            {
                return t * t * t;
            }

            private static float EaseOutCubic(float t)
            {
                float p = 1f - t;
                return 1f - p * p * p;
            }
        }
    }
}
