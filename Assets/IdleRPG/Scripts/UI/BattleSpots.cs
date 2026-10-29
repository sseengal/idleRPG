using UnityEngine;

namespace IdleRPG.UI
{
    /// <summary>
    /// Where two units stand when they meet. ONE place for the gap and side maths, used by the beat planner only.
    ///
    /// ELI5: two people standing side by side need a hand's width between their shoulders, or they overlap. This file
    /// owns that number. Nothing else in the game is allowed to invent it.
    ///
    /// Widths are the ANNOUNCED slot widths (how wide the unit is drawn at rest), never the live icon rect: a swing
    /// grows that rect for its attack window, and a stretched rect would park two units a screen apart.
    /// </summary>
    public static class BattleSpots
    {
        /// <summary>Breathing room between two bodies, in DESIGN units (the 1080-wide canvas).</summary>
        public const float ContactPadding = 18f;

        /// <summary>
        /// Converts the design-unit padding into the world space of a unit. Body widths and
        /// <see cref="RectTransform.position"/> are world-space; the stage canvas is scaled, so adding
        /// 18 world units there would throw the attacker far outside the viewport.
        /// </summary>
        public static float PaddingFor(Transform space)
        {
            float scale = space != null ? Mathf.Abs(space.lossyScale.x) : 1f;
            return ContactPadding * scale;
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
        /// The point where a unit of <paramref name="selfWidth"/> stands next to <paramref name="targetCentre"/>
        /// on <paramref name="side"/> (+1 = to the target's right, -1 = left), bodies never overlapping.
        /// All widths, the centre and <paramref name="contactPadding"/> are in the same world space.
        /// </summary>
        public static Vector3 Beside(Vector3 targetCentre, float selfWidth, float targetWidth, float side,
            float contactPadding)
        {
            float sign = Mathf.Abs(side) < 0.001f ? 1f : Mathf.Sign(side);
            float gap = (Mathf.Max(0f, selfWidth) + Mathf.Max(0f, targetWidth)) * 0.5f + contactPadding;

            return new Vector3(targetCentre.x + sign * gap, targetCentre.y, targetCentre.z);
        }
    }
}
