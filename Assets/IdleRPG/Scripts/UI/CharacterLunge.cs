using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// The "run in, hit, blink back" dance for one unit icon. Lives on the same GameObject as the
    /// <see cref="CharacterAnimator"/> and is driven by events the unit view forwards:
    ///   - <see cref="BeginRun"/> when the sim announces a swing (the unit commits),
    ///   - <see cref="Impact"/> when the damage actually lands,
    ///   - <see cref="ResetToHome"/> from Show/ClearVisual/Refresh so a wave change can never strand anyone.
    ///
    /// ELI5: the sim says "this guy is about to attack". We make him run to the target, wait for the real hit,
    /// then do the anime blink back to his fighting spot. If another swing arrives while he is still busy, he
    /// attacks in place (late waves stay calm against a sea of blinking).
    /// </summary>
    public sealed class CharacterLunge : MonoBehaviour
    {
        private enum State
        {
            Idle = 0,
            Advancing = 1,
            Holding = 2,
            BlinkOut = 3,
            Teleporting = 4,
            BlinkIn = 5
        }

        [Header("Run")]
        [Tooltip("How long (s) the run-in takes. Keep it well under the sim swing window.")]
        [SerializeField] private float advanceSeconds = 0.2f;

        [Tooltip("How far in front of the target's slot the unit stops, world units.")]
        [SerializeField] private float contactDistance = 30f;

        [Header("Swing + blink")]
        [Tooltip("How long (s) the unit poses at the target after impact before blinking home.")]
        [SerializeField] private float holdSeconds = 0.3f;

        [Tooltip("Blink-out duration: squash + fade at the target.")]
        [SerializeField] private float blinkOutSeconds = 0.06f;

        [Tooltip("Blink-in duration: pop back to full at home.")]
        [SerializeField] private float blinkInSeconds = 0.1f;

        [Tooltip("Pop-in overshoot scale (1.0 = no overshoot).")]
        [SerializeField] private float blinkInScale = 1.25f;

        private CharacterAnimator animator;
        private RectTransform rect;
        private Image image;

        private State state;
        private Vector3 home;
        private Vector3 contact;
        private Vector3 from;
        private float clock;

        private void Awake()
        {
            rect = transform as RectTransform;
            animator = GetComponent<CharacterAnimator>();
            image = GetComponent<Image>();

            if (rect != null)
            {
                home = rect.position;
            }
        }

        public bool IsBusy => state != State.Idle;

        /// <summary>Re-captures the home anchor. Call after a slot is (re)laid out so the unit returns to the right spot.</summary>
        public void RefreshHome()
        {
            if (rect != null)
            {
                home = rect.position;
            }
        }

        /// <summary>Starts the run-in toward a target slot. No-op while the unit is already busy.</summary>
        public void BeginRun(RectTransform targetSlot)
        {
            if (state != State.Idle || rect == null || targetSlot == null)
            {
                return;
            }

            if (animator != null && animator.HasArt)
            {
                animator.PlayWalk();
            }

            home = rect.position;

            // Stop just in front of the target, facing it. The contact point is nudged toward home so the
            // icon overlaps the enemy slot instead of landing exactly on its pivot.
            Vector2 toTarget = targetSlot.position - rect.position;
            float overshoot = toTarget.magnitude > contactDistance ? contactDistance : 0f;
            if (overshoot > 0f)
            {
                toTarget = toTarget.normalized * overshoot;
            }

            contact = targetSlot.position - (Vector3)toTarget;
            from = rect.position;
            clock = 0f;
            state = State.Advancing;
        }

        /// <summary>Called by the unit view when the real damage event lands: full contact at the target.</summary>
        public void Impact()
        {
            if (state != State.Advancing || rect == null)
            {
                return;
            }

            rect.position = contact;
            clock = 0f;
            state = State.Holding;
        }

        /// <summary>Snaps the unit straight back home (wave change, death, re-layout).</summary>
        public void ResetToHome()
        {
            state = State.Idle;

            if (rect != null)
            {
                rect.position = home;
            }

            if (image != null)
            {
                Color color = image.color;
                color.a = 1f;
                image.color = color;
            }

            if (animator != null && animator.HasArt)
            {
                animator.PlayIdle();
            }
        }
private void Update()
        {
            if (rect == null)
            {
                return;
            }

            switch (state)
            {
                case State.Advancing:
                    clock += Time.deltaTime;
                    float advanceT = Mathf.Clamp01(clock / Mathf.Max(0.01f, advanceSeconds));
                    rect.position = Vector3.Lerp(from, contact, EaseOutCubic(advanceT));

                    if (clock >= advanceSeconds)
                    {
                        state = State.Holding;
                        clock = 0f;
                    }

                    break;

                case State.Holding:
                    clock += Time.deltaTime;

                    if (clock >= Mathf.Max(0.05f, holdSeconds))
                    {
                        state = State.BlinkOut;
                        clock = 0f;
                    }

                    break;

                case State.BlinkOut:
                    clock += Time.deltaTime;
                    float outT = Mathf.Clamp01(clock / Mathf.Max(0.01f, blinkOutSeconds));

                    // Squash small + fade out at the target, then the next state teleports home.
                    float squash = 1f - outT * 0.35f;
                    rect.localScale = new Vector3(rect.localScale.x, squash, 1f);

                    if (image != null)
                    {
                        Color color = image.color;
                        color.a = 1f - outT;
                        image.color = color;
                    }

                    if (clock >= blinkOutSeconds)
                    {
                        rect.position = home;
                        state = State.BlinkIn;
                        clock = 0f;
                    }

                    break;

                case State.BlinkIn:
                    clock += Time.deltaTime;
                    float inT = Mathf.Clamp01(clock / Mathf.Max(0.01f, blinkInSeconds));

                    // Pop back with a touch of overshoot, then idle safely at home.
                    float scale = Mathf.Lerp(blinkInScale, 1f, EaseOutCubic(inT));
                    rect.localScale = new Vector3(rect.localScale.x, scale, 1f);

                    if (image != null)
                    {
                        Color color = image.color;
                        color.a = inT;
                        image.color = color;
                    }

                    if (clock >= blinkInSeconds)
                    {
                        rect.localScale = new Vector3(rect.localScale.x, 1f, 1f);
                        state = State.Idle;
                        clock = 0f;

                        if (animator != null && animator.HasArt)
                        {
                            animator.PlayIdle();
                        }
                    }

                    break;

                default:
                    return;
            }
        }

        /// <summary>Fast launch, gentle landing — the run reads as a dart, not a slide.</summary>
        private static float EaseOutCubic(float t)
        {
            float p = 1f - t;
            return 1f - p * p * p;
        }
    }
}