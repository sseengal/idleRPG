using UnityEngine;

namespace IdleRPG.UI
{
    /// <summary>
    /// Where two units stand when they meet. ONE place for the gap and side maths, used by the beat planner only.
    ///
    /// ELI5: two people standing side by side need a hand's width between their shoulders, or they overlap. This file
    /// owns that number. Nothing else in the game is allowed to invent it.
    /// </summary>
    public static class BattleSpots
    {
        /// <summary>Breathing room between two bodies, in canvas units.</summary>
        public const float ContactPadding = 18f;

        /// <summary>Half the drawn width of a unit's body (the icon rect, not its pad).</summary>
        public static float HalfWidth(RectTransform body)
        {
            if (body == null)
            {
                return 0f;
            }

            return body.rect.width * Mathf.Abs(body.lossyScale.x) * 0.5f;
        }

        /// <summary>
        /// Which side of the TARGET the attacker stands on: +1 when the attacker is to the target's right, -1 when it
        /// is to its left. Feed this straight to <see cref="Beside"/> so the attacker stops on its own near flank.
        /// </summary>
        public static float AttackerSide(Vector3 attacker, Vector3 target)
        {
            float dx = attacker.x - target.x;
            return Mathf.Abs(dx) < 0.001f ? 1f : Mathf.Sign(dx);
        }

        /// <summary>
        /// The point where a unit of <paramref name="selfHalfWidth"/> stands next to <paramref name="targetCentre"/>
        /// on <paramref name="side"/> (+1 = to the target's right, -1 = left), bodies never overlapping.
        /// </summary>
        public static Vector3 Beside(Vector3 targetCentre, float selfHalfWidth, float targetHalfWidth, float side)
        {
            float sign = Mathf.Abs(side) < 0.001f ? 1f : Mathf.Sign(side);
            float gap = selfHalfWidth + targetHalfWidth + ContactPadding;

            return new Vector3(targetCentre.x + sign * gap, targetCentre.y, targetCentre.z);
        }
    }
}
