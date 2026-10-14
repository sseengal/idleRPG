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

        /// <summary>Pixels-per-sprite-pixel. ONE value for the whole roster (see <see cref="RosterReferencePx"/>),
        /// so every character's body renders at the same size in every pose AND keeps the size the artist gave it
        /// relative to the others. The rect follows the frame's own window, so a wide pose is transparent margin.</summary>
        private float pixelScale = 1f;

        /// <summary>
        /// The roster's biggest idle window, in art pixels - measured from the imported art (Demon_E, 50x29).
        /// This is the denominator of the shared scale: the biggest character exactly fills its slot, and every
        /// other character draws smaller by exactly as much as the art draws it smaller.
        ///
        /// Why a constant and not the character's own window: using each character's own window blew every one of
        /// them up to fill its box, so the art's pixel size differed character to character (a 21px wizard and a
        /// 50px demon drew with the same apparent body). One constant is the whole fix for that.
        /// </summary>
        private const float RosterReferencePx = 50f;

        /// <summary>
        /// Whole-roster size dial. 1.00 = the biggest character exactly fills its slot. Raising it makes every
        /// unit bigger together, in proportion, and the biggest one then overflows its box by the same amount.
        /// Tweak this one number to taste.
        ///
        /// 2.40 = twice that fill size, which is what a phone needs: at 1.00-1.20 the units read as tiny.
        /// </summary>
        private const float IconSizeBump = 2.4f;

        /// <summary>Whose look is wanted: true = face left (enemies). Combined with the art's own facing.</summary>
        public bool FacingLeft { get; set; }

        private Action onDone;

        public bool HasArt => art != null && art.HasAnimation;

        public CharacterArtSet Art => art;

        /// <summary>
        /// The icon size the slot announced (local units). Callers that need the BODY's footprint - the lunge's
        /// stop distance - must use this, not the live rect: a wide attack window has already grown that rect.
        /// </summary>
        public Vector2 SlotSize => slotSize;

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

                // The roster's ONE scale (see RosterReferencePx/IconSizeBump). The character's own window is
                // deliberately NOT read here: reading it per character is what made everyone draw at a
                // different pixel size.
                pixelScale = Mathf.Max(slotSize.x, slotSize.y) * IconSizeBump / RosterReferencePx;
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

        /// <summary>
        /// One-shot attack (prefers the character's second attack strip). When <paramref name="onFinished"/> is
        /// given it is invoked when the clip completes (the caller decides what comes next, e.g. the blink home);
        /// without it the animator returns to Idle on its own, as before.
        /// </summary>
        public void PlayAttack(Action onFinished = null)
        {
            if (art == null)
            {
                return;
            }

            Play(art.GetAttack(), loop: false, Second(art.AttackFps), idleOnDone: onFinished == null, done: onFinished);
        }

        public void PlayHurt()
        {
            if (art == null)
            {
                return;
            }

            Play(art.Hurt, loop: false, Second(art.HurtFps), idleOnDone: true);
        }

        /// <summary>
        /// Plays one specific clip once - an ability's own animation. An ability is not the base attack, so it is
        /// played by the frames the ability names rather than through <see cref="PlayAttack"/>: the Knight's Shield
        /// Wall is its Block strip, the Wizard's Arcane Burst a composited cast. Returns to Idle when it finishes
        /// unless the caller wants to decide what comes next.
        /// </summary>
        public void PlayClip(Sprite[] frames, int fps, Action onFinished = null)
        {
            if (frames == null || frames.Length == 0)
            {
                onFinished?.Invoke();
                return;
            }

            Play(frames, loop: false, Second(fps), idleOnDone: onFinished == null, done: onFinished);
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
        /// Sizes the icon rect to the current clip's window at the roster's one shared pixel scale
        /// (<see cref="RosterReferencePx"/>), so the character's body renders at exactly the same size in every
        /// pose (idle/attack/hurt/death) and at the size the art gives it relative to every other character.
        ///
        /// The rect is PROPORTIONAL to the frame's own window: a wide window (a lunging attack) makes the rect
        /// wider while the body keeps its size - the extra area is transparent margin. An earlier version capped
        /// the rect at 1.5x the slot, and that shrank the character itself: Archer's attack window is 2.5 slots
        /// wide, so he drew at 60% of his idle size mid-swing.
        /// </summary>
        private void ApplyClipSize()
        {
            if (target == null || clip == null || clip.Length == 0 || pixelScale <= 0f)
            {
                return;
            }

            Rect r = clip[0].rect;
            target.rectTransform.sizeDelta = new Vector2(r.width * pixelScale, r.height * pixelScale);
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
