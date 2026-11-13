namespace IdleRPG.UI
{
    /// <summary>
    /// The one door every screen uses to ask for motion. Screens name a state; this layer decides what
    /// that means, and enforces the accessibility switch.
    ///
    /// ELI5: the front desk for animation. A screen says "breathe" or "flash"; the desk knows whether
    /// the player asked for less motion, and answers the same way everywhere.
    ///
    /// Rules (Docs/Motion.md):
    ///  * a screen never picks a duration;
    ///  * reduced motion turns a pulse into a steady glow and a flash into nothing;
    ///  * new cues are added here, one method at a time, never inline in a screen.
    /// </summary>
    public static class Juice
    {
        /// <summary>
        /// True when the player asked for less motion. A settings toggle will write this later; one
        /// switch, one place.
        /// </summary>
        public static bool ReducedMotion { get; set; }

        /// <summary>Asks a glow to hold a state, respecting the reduced-motion setting.</summary>
        public static void Glow(GlowView view, GlowState state)
        {
            if (view == null)
            {
                return;
            }

            if (!ReducedMotion)
            {
                view.SetState(state);
                return;
            }

            // Calm mode: still readable, never moving.
            view.SetState(state == GlowState.Pulse ? GlowState.Steady : GlowState.Off);
        }

        /// <summary>One bright pop. Ignored in calm mode - the state still reads on its own.</summary>
        public static void Flash(GlowView view)
        {
            if (view == null || ReducedMotion)
            {
                return;
            }

            view.Flash();
        }
    }
}
