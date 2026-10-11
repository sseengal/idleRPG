using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// The hit reaction a unit shows when it takes a blow: the sprite flashes, and the unit squashes for a moment.
    ///
    /// ELI5: the "ouch" of one unit. Two numbers and two colours - how long the flash lasts and which colour it
    /// goes to, how deep the squash goes and how long it takes - driven once per frame.
    ///
    /// Why it exists: the hero view and the enemy view each had their own copy of this, about forty lines each, and
    /// the copies had already drifted apart - only the enemy could vary the flash length and the squash depth by
    /// crit, and the two colour lerps even ran in opposite directions. One behaviour, written once.
    ///
    /// Composition, not inheritance: both views own one of these and call it, so nothing about their scene wiring
    /// changes. The callers keep their own rules (the hero passes its resting tint as the flash's start, the enemy
    /// passes it as the end; the enemy's spawn pop still wins over a punch), so this changed no look on screen.
    /// </summary>
    public sealed class UnitHitJuice
    {
        private Color flashFrom = Color.white;
        private Color flashTo = Color.white;
        private float flashRemaining;
        private float flashDuration;

        private float punchFrom = 1f;
        private float punchRemaining;
        private float punchDuration;

        /// <summary>True while a flash is running.</summary>
        public bool IsFlashing => flashRemaining > 0f;

        /// <summary>Cancels both effects, e.g. when a slot is emptied or re-bound.</summary>
        public void Clear()
        {
            flashRemaining = 0f;
            punchRemaining = 0f;
        }

        /// <summary>Starts a flash that lerps from <paramref name="from"/> to <paramref name="to"/> over its life.</summary>
        public void Flash(Color from, Color to, float duration)
        {
            flashFrom = from;
            flashTo = to;
            flashDuration = Mathf.Max(0.0001f, duration);
            flashRemaining = flashDuration;
        }

        /// <summary>Starts a squash from <paramref name="from"/> (a scale below 1) back up to 1.</summary>
        public void Punch(float from, float duration)
        {
            punchFrom = from;
            punchDuration = Mathf.Max(0.0001f, duration);
            punchRemaining = punchDuration;
        }

        /// <summary>
        /// Advances the flash and tints <paramref name="target"/>. Returns true while the flash runs, so the caller
        /// knows not to restore its resting tint yet (a lunge owns the tint while it fades in and out).
        /// </summary>
        public bool TickFlash(float deltaTime, Graphic target)
        {
            if (flashRemaining <= 0f)
            {
                return false;
            }

            flashRemaining -= deltaTime;
            target.color = Color.Lerp(flashFrom, flashTo, Mathf.Clamp01(flashRemaining / flashDuration));
            return true;
        }

        /// <summary>Advances the squash on <paramref name="target"/>, and restores a scale of 1 when it ends.</summary>
        public void TickPunch(float deltaTime, Transform target)
        {
            if (punchRemaining > 0f)
            {
                punchRemaining -= deltaTime;
                float t = 1f - Mathf.Clamp01(punchRemaining / punchDuration);
                float scale = Mathf.Lerp(punchFrom, 1f, t);
                target.localScale = new Vector3(scale, scale, 1f);
            }
            else if (target.localScale != Vector3.one)
            {
                target.localScale = Vector3.one;
            }
        }
    }
}
