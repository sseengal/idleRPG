using System;
using UnityEngine;

namespace IdleRPG.Core
{
    /// <summary>
    /// The single heartbeat of the game: every frame it advances combat, and once a second it runs the "slow"
    /// chores (boost expiry, autosave cadence, play-time).
    ///
    /// ELI5: the game used to have a coroutine doing the slow chores and another coroutine doing the fighting,
    /// so two different clocks were ticking. Now one clock ticks everything in a fixed order - which is exactly
    /// what makes fast-forward and offline replay possible later.
    ///
    /// Per project rules there is no `Update()` polling of gameplay state in the systems themselves; this is the
    /// one place that calls into them.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunController : MonoBehaviour
    {
        private const float SlowTickIntervalSec = 1f;

        private GameContext context;
        private float slowAccumulator;

        /// <summary>Number of combat ticks applied since the last frame (diagnostics only).</summary>
        public double LastDeltaSeconds { get; private set; }

        public int SlowTickCount { get; private set; }

        public bool IsDriving => context != null && context.Combat != null;

        /// <summary>Called by GameManager once everything is wired.</summary>
        public void Attach(GameContext gameContext, bool enableDriving)
        {
            context = gameContext;
            slowAccumulator = 0f;
            enabled = enableDriving;
        }

        public void Detach()
        {
            context = null;
            enabled = false;
        }

        private void Update()
        {
            if (context == null || !context.IsReady)
            {
                return;
            }

            // 1) Gameplay: one accumulator-driven combat tick (no coroutines anywhere in the flow).
            double deltaTime = Time.deltaTime;
            LastDeltaSeconds = deltaTime;
            context.Combat.Tick(deltaTime);

            // 2) Slow chores, once per second, in a deterministic order.
            slowAccumulator += (float)deltaTime;

            if (slowAccumulator < SlowTickIntervalSec)
            {
                return;
            }

            slowAccumulator -= SlowTickIntervalSec;
            SlowTickCount++;

            if (context.Boost != null && context.Boost.Refresh())
            {
                // Parity with the old GameManager slow loop (same line, new owner).
                Debug.Log("[RunController] Ad gold boost expired.");
            }

            // Autosave cadence + play-time accrual (was GameManager's SlowTickLoop).
            context.Save?.Tick(SlowTickIntervalSec);
        }
    }
}
