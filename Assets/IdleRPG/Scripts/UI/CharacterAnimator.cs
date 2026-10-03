using System;
using IdleRPG.Data;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// Swaps a unit icon <see cref="Image"/> between the frames of a <see cref="CharacterArtSet"/>.
    /// Tiny per-frame timer only; no SpriteRenderer/Animator, so the uGUI layout (slots, anchors, theme)
    /// the battle page already has is untouched.
    ///
    /// State machine: Idle/Walk loop forever; Attack/Hurt/Death play once and run the delegate, then the
    /// caller (unit view) tells it what to do next (most often straight back to Idle). Safe to call on a
    /// component with no art set - every method no-ops.
    /// </summary>
    public sealed class CharacterAnimator : MonoBehaviour
    {
        private Image target;
        private CharacterArtSet art;

        private Sprite[] clip;
        private bool loop;
        private float frameDuration;
        private float timer;
        private int cursor;

        /// <summary>
        /// The icon rect size the SLOT wants (announced once via SetSlotSize; never read back from the live rect).
        /// Reading the live rect made repeated re-binds ratchet the sprite bigger, because a wider action pose
        /// got captured as the new base.
        /// </summary>
        private Vector2 slotSize;

        /// <summary>Pixels-per-sprite-pixel so every action renders its body at the same size (the pack's
        /// attack/death frames have much wider windows than idle; a fixed rect would shrink those poses).</summary>
        private float pixelScale = 1f;

        /// <summary>Whose look is wanted: true = face left (enemies). Combined with the art's own facing.</summary>
        public bool FacingLeft { get; set; }

        private Action onDone;

        public bool HasArt => art != null && art.HasAnimation;

        public CharacterArtSet Art => art;

        /// <summary>Announces the icon size this slot wants. Idempotent - call again only on a real resize.</summary>
        public void SetSlotSize(Vector2 size)
        {
            if (size.x > 0f && size.y > 0f)
            {
                slotSize = size;
            }
        }

        private void Awake()
        {
            target = GetComponent<Image>();
        }

        /// <summary>Binds the frames (safe to call repeatedly - rebind restarts Idle).</summary>
        public void SetArt(CharacterArtSet artSet)
        {
            if (artSet == null)
            {
                return;
            }

            art = artSet;

            if (target == null)
            {
                target = GetComponent<Image>();
            }

            if (target != null)
            {
                if (slotSize == Vector2.zero)
                {
                    // Safety: a slot that never announced its size falls back to the built rect once.
                    slotSize = target.rectTransform.sizeDelta;
                }

                Sprite[] reference = art.Idle != null && art.Idle.Length > 0 ? art.Idle : art.GetAttack();
                if (reference != null && reference.Length > 0)
                {
                    Rect r = reference[0].rect;
                    pixelScale = Mathf.Max(slotSize.x, slotSize.y)
                                 / Mathf.Max(1f, Mathf.Max(r.width, r.height));
                }
            }

            PlayIdle();
        }

        public void Clear()
        {
            art = null;
            clip = null;
            onDone = null;
            timer = 0f;
            cursor = 0;
            if (target != null)
            {
                target.sprite = null;
                target.rectTransform.sizeDelta = slotSize;
            }
        }

        public void PlayIdle()
        {
            if (art == null)
            {
                return;
            }

            Play(art.Idle, loop: true, Second(art.IdleFps), idleOnDone: false);
        }

        public void PlayWalk()
        {
            if (art == null)
            {
                return;
            }

            if (art.Walk == null || art.Walk.Length == 0)
            {
                PlayIdle();
                return;
            }

            Play(art.Walk, loop: true, Second(art.WalkFps), idleOnDone: false);
        }

        /// <summary>One-shot attack (prefers the character's second attack strip). Returns to Idle after.</summary>
        public void PlayAttack()
        {
            if (art == null)
            {
                return;
            }

            Play(art.GetAttack(), loop: false, Second(art.AttackFps), idleOnDone: true);
        }

        public void PlayHurt()
        {
            if (art == null)
            {
                return;
            }

            Play(art.Hurt, loop: false, Second(art.HurtFps), idleOnDone: true);
        }

        public void PlayDeath(Action onFinished = null)
        {
            if (art == null)
            {
                onFinished?.Invoke();
                return;
            }

            Play(art.Death, loop: false, Second(art.DeathFps), idleOnDone: false, done: onFinished);
        }

        /// <summary>True while a one-shot clip is still playing (used to throttle repeats).</summary>
        public bool IsBusy()
        {
            return clip != null && !loop && art != null;
        }

        private void Play(Sprite[] frames, bool loop, float duration, bool idleOnDone, Action done = null)
        {
            if (target == null)
            {
                target = GetComponent<Image>();
            }

            if (target == null || frames == null || frames.Length == 0)
            {
                done?.Invoke();
                return;
            }

            // A one-shot clip that is already running is not restarted (swing/hurt spam would stutter).
            if (!loop && clip == frames)
            {
                return;
            }

            clip = frames;
            this.loop = loop;
            frameDuration = Mathf.Max(0.01f, duration);
            timer = 0f;
            cursor = 0;

            ApplyClipSize();

            onDone = null;
            if (done != null)
            {
                onDone = done;
            }
            else if (idleOnDone && art != null)
            {
                onDone = PlayIdle;
            }

            ApplyFrame();
        }

        /// <summary>
        /// Sizes the icon rect to the current clip's window at a single per-character pixel scale, so every
        /// pose (idle/attack/hurt/death) renders its body at the same size. Extreme swings are capped at 1.5x
        /// the wired base so a wide attack never overflows its slot row.
        /// </summary>
        private void ApplyClipSize()
        {
            if (target == null || clip == null || clip.Length == 0 || pixelScale <= 0f)
            {
                return;
            }

            Rect r = clip[0].rect;
            float baseMax = Mathf.Max(1f, Mathf.Max(slotSize.x, slotSize.y));
            float cap = baseMax * 1.5f;
            float natural = Mathf.Max(r.width, r.height) * pixelScale;
            float mul = Mathf.Min(1f, cap / Mathf.Max(1f, natural));

            target.rectTransform.sizeDelta = new Vector2(r.width * pixelScale * mul, r.height * pixelScale * mul);
        }

        private static float Second(int fps)
        {
            return 1f / Mathf.Max(1, fps);
        }

        private void Update()
        {
            if (target == null || clip == null || clip.Length == 0)
            {
                return;
            }

            Tick();
        }

        private void Tick()
        {
            if (target == null || clip == null || clip.Length == 0)
            {
                return;
            }

            timer += Time.deltaTime;
            if (timer >= frameDuration)
            {
                timer -= frameDuration;
                cursor++;

                if (cursor >= clip.Length)
                {
                    if (loop)
                    {
                        cursor = 0;
                    }
                    else
                    {
                        var finished = onDone;
                        clip = null;
                        onDone = null;
                        finished?.Invoke();
                        return;
                    }
                }

                ApplyFrame();
            }

            ApplyFacing();
        }

        private void ApplyFrame()
        {
            if (target == null)
            {
                return;
            }

            if (clip != null && cursor >= 0 && cursor < clip.Length)
            {
                target.sprite = clip[cursor];
            }

            ApplyFacing();
        }

        /// <summary>
        /// Mirrors the art when its native facing and the requested facing disagree. The icon is flipped about
        /// its own centre, so the slot layout, name labels and health bars are unaffected.
        /// </summary>
        private void ApplyFacing()
        {
            if (target == null || art == null)
            {
                return;
            }

            float native = art.ArtFacesLeft ? -1f : 1f;
            float wanted = FacingLeft ? -1f : 1f;
            float x = wanted * native;

            RectTransform rect = target.rectTransform;
            Vector3 scale = rect.localScale;
            if (Mathf.Abs(scale.x - x) > 0.01f)
            {
                rect.localScale = new Vector3(x, scale.y, scale.z);
            }
        }
    }
}
