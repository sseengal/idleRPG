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
    /// ELI5: the sim says "this guy is about to attack" and waits ~0.35 s before the blow actually lands. We make him
    /// run to the target AND start his swing on that announcement, so the strike lands on the hit; then we do the
    /// anime blink back to his fighting spot. If another swing arrives while he is still busy, he attacks in place
    /// (late waves stay calm against a sea of blinking).
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

        [Tooltip("Extra breathing room between the attacker's icon edge and the target's icon edge, world units.")]
        [SerializeField] private float contactPaddingPx = 18f;

        [Header("Swing + blink")]
        [Tooltip("Minimum pose time (s) after a hit, counted from impact.")]
        [SerializeField] private float holdSeconds = 0.3f;

        [Tooltip("Minimum pose time (s) for a COVER: the guard stands in front of the ally long enough to read as " +
                 "\"I took that for you\", then blinks back.")]
        [SerializeField] private float coverHoldSeconds = 0.65f;

        [Tooltip("Hard ceiling for the pose (s), counted from impact: a long/broken clip, or a swing whose damage " +
                 "event never fired, can never stall the fight.")]
        [SerializeField] private float maxHoldSeconds = 0.7f;

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
        private Vector3 contact;
        private Vector3 from;
        private float clock;

        /// <summary>What the current run aims at, kept so the contact point can be re-computed at impact.</summary>
        private RectTransform runTarget;
        private float runSide;
        private bool runTargetIsBody;
        private float runHold;

        /// <summary>
        /// The icon's resting offset inside its unit node. Re-captured at the start of every run (the unit is at rest
        /// by definition then), so home is always DERIVED from the current layout. A remembered WORLD point is what
        /// used to survive a reposition and send a unit back to the seat it had left.
        /// </summary>
        private Vector3 restLocal;

        /// <summary>Set when the attack clip finishes during the pose; Holding waits for this before blinking.</summary>
        private bool blinkQueued;

        private void Awake()
        {
            rect = transform as RectTransform;
            animator = GetComponent<CharacterAnimator>();
            image = GetComponent<Image>();

            if (rect != null)
            {
                restLocal = rect.localPosition;
            }
        }

        public bool IsBusy => state != State.Idle;

        /// <summary>
        /// Where this unit belongs RIGHT NOW: its resting offset converted through the node it lives in. Ask this
        /// instead of storing a point - the node is what the player moves, so it is always the truth.
        /// </summary>
        private Vector3 HomeNow()
        {
            if (rect == null)
            {
                return Vector3.zero;
            }

            return rect.parent != null ? rect.parent.TransformPoint(restLocal) : restLocal;
        }

        /// <summary>
        /// Starts the run-in toward a target. No-op while the unit is already busy.
        ///
        /// <paramref name="side"/> = 0 (default): approach the target from the unit's own side, which is what an
        /// attack should do. Non-zero: stand on THAT side of the target, ignoring where the unit came from.
        /// <paramref name="targetIsBody"/>: the target rect IS the character (an icon), not a pad - its centre is the
        /// body centre, so a unit can chase another unit that is moving.
        /// </summary>
        public void BeginRun(RectTransform target, float side = 0f, bool targetIsBody = false)
        {
            StartRun(target, side, targetIsBody, 0f);
        }

        /// <summary>Runs to stand in front of an ally (a cover) and holds there longer than an attack would.</summary>
        public void BeginCover(RectTransform target, float side)
        {
            StartRun(target, side, true, coverHoldSeconds);
        }

        private void StartRun(RectTransform target, float side, bool targetIsBody, float lingerSeconds)
        {
            if (state != State.Idle || rect == null || target == null)
            {
                return;
            }

            runTarget = target;
            runSide = side;
            runTargetIsBody = targetIsBody;
            runHold = Mathf.Max(0f, lingerSeconds);

            blinkQueued = false;

            // He is standing at rest, so this is exactly where home is in the CURRENT layout.
            restLocal = rect.localPosition;

            contact = ComputeContact();
            from = rect.position;
            clock = 0f;
            state = State.Advancing;
        }

        /// <summary>
        /// Where the run should stop: the near flank of the target's BODY, gapped by both icons' half-widths plus
        /// breathing room (world units; lossyScale converts local rect sizes).
        ///
        /// A pad keeps its own convention: the slot builders place every icon with CenterOn(0.5, 0.60) inside a rect
        /// whose pivot sits at a corner, so `slot.position` alone is the slot's top edge, not the body. A live body
        /// needs none of that - its own centre IS the body centre.
        /// </summary>
        private Vector3 ComputeContact()
        {
            if (rect == null || runTarget == null)
            {
                return rect != null ? rect.position : Vector3.zero;
            }

            Vector3 bodyCenter;

            if (runTargetIsBody)
            {
                bodyCenter = runTarget.position;
            }
            else
            {
                float worldW = runTarget.rect.width * Mathf.Abs(runTarget.lossyScale.x);
                float worldH = runTarget.rect.height * Mathf.Abs(runTarget.lossyScale.y);
                float bodyX = runTarget.position.x + (0.5f - runTarget.pivot.x) * worldW;
                float bodyY = runTarget.position.y - (0.40f - (1f - runTarget.pivot.y)) * worldH;
                bodyCenter = new Vector3(bodyX, bodyY, runTarget.position.z);
            }

            float dirX = bodyCenter.x >= rect.position.x ? 1f : -1f;

            // Both halves use the SLOT width (the body's footprint), never the live icon rect: a wide attack
            // window has already grown that rect, and using it would park the unit short of its target.
            float selfHalf = BodyWidth(animator, rect) * 0.5f * Mathf.Abs(rect.lossyScale.x);
            float targetHalf = BodyWidth(runTarget.GetComponent<CharacterAnimator>(), runTarget) * 0.5f *
                               Mathf.Abs(runTarget.lossyScale.x);
            float gap = selfHalf + targetHalf + contactPaddingPx;

            // An attack stands on the side it came from; a cover stands where it was told to (the enemy side), so the
            // reader sees the guard between the ally and the danger.
            float along = Mathf.Abs(runSide) > 0.001f ? Mathf.Sign(runSide) : -dirX;
            return new Vector3(bodyCenter.x + (along * gap), bodyCenter.y, bodyCenter.z);
        }

        /// <summary>
        /// Called by the unit view when the blow actually lands: plant the unit on the target and restart the pose
        /// clock at the hit.
        ///
        /// Accepts the run-in AND the pose that follows it. The old "only while Advancing" guard made this a no-op in
        /// practice: the run-in is 0.2 s and the sim's impact lands at 0.35 s, so the lunge was already Holding and
        /// nothing was ever synced to the hit.
        /// </summary>
        public void Impact()
        {
            if (rect == null || state != State.Advancing)
            {
                return;
            }

            // Re-aim: the target may have MOVED since the announce (a guard steps in front of an ally, an enemy runs
            // at the party). Landing on where it is now is what keeps the attacker in front of the guard instead of
            // passing through him.
            if (runTarget != null)
            {
                contact = ComputeContact();
            }

            rect.position = contact;
            clock = 0f;
            blinkQueued = false;
            state = State.Holding;
        }

        /// <summary>
        /// The attack clip finished - the full swing (strike + follow-through) has played out. The unit blinks home
        /// once the minimum pose (<see cref="holdSeconds"/>) is also satisfied.
        ///
        /// A unit that attacked in place (the lunge was still busy, so no run-in happened) has nothing to blink, so
        /// it returns to Idle instead of freezing on the swing's last frame.
        /// </summary>
        public void CompleteSwing()
        {
            if (state == State.Holding)
            {
                blinkQueued = true;
                return;
            }

            if (state == State.Idle && animator != null && animator.HasArt)
            {
                animator.PlayIdle();
            }
        }

        /// <summary>Snaps the unit straight back home (wave change, death, re-layout).</summary>
        public void ResetToHome()
        {
            state = State.Idle;
            blinkQueued = false;
            runTarget = null;
            runHold = 0f;

            if (rect != null)
            {
                rect.position = HomeNow();
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

                    // The swing pose lasts until the attack clip finishes (the full strike AND follow-through
                    // must play), subject to a minimum pose and a hard ceiling so nothing can stall the queue
                    // (a long/broken clip, or an aborted swing whose damage event never fired). Placeholders
                    // (no art) have no clip, so they blink on the minimum alone.
                    bool clipDone = (animator == null || !animator.HasArt) || blinkQueued;
                    float minHold = Mathf.Max(holdSeconds, runHold);
                    float ceiling = Mathf.Max(maxHoldSeconds, minHold);
                    bool minPoseDone = clock >= Mathf.Max(0.05f, minHold);

                    if ((clipDone && minPoseDone) || clock >= Mathf.Max(0.05f, ceiling))
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
                        rect.position = HomeNow();

                        // Cut the swing clip at the teleport (the unit is invisible now): fade in the idle pose,
                        // never the attack's tail frames.
                        if (animator != null && animator.HasArt)
                        {
                            animator.PlayIdle();
                        }

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
                    }

                    break;

                default:
                    return;
            }
        }

        /// <summary>
        /// The width the body is drawn at: the unit's announced slot size when it has an animator, else the live
        /// rect (a plain slot container has no animator and keeps its own width).
        /// </summary>
        private static float BodyWidth(CharacterAnimator unit, RectTransform fallback)
        {
            if (unit != null && unit.SlotSize.x > 0f)
            {
                return unit.SlotSize.x;
            }

            return fallback != null ? fallback.rect.width : 0f;
        }

        /// <summary>Fast launch, gentle landing — the run reads as a dart, not a slide.</summary>
        private static float EaseOutCubic(float t)
        {
            float p = 1f - t;
            return 1f - p * p * p;
        }
    }
}