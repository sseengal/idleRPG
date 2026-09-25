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

        /// <summary>Consecutive tick failures before the driver stops itself (G5 safe mode).</summary>
        private const int MaxConsecutiveFailures = 10;

        private GameContext context;
        private float slowAccumulator;
        private int consecutiveFailures;

        /// <summary>Number of combat ticks applied since the last frame (diagnostics only).</summary>
        public double LastDeltaSeconds { get; private set; }

        public int SlowTickCount { get; private set; }

        /// <summary>True when error containment stopped the driver.</summary>
        public bool SafeModeTriggered { get; private set; }

        public int TickFailures { get; private set; }

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

        /// <summary>
        /// Runs one combat tick with error containment (G5).
        ///
        /// ELI5: an idle game runs unattended, so one unexpected exception must not freeze it. The first failure
        /// is logged, saved and skipped; too many in a row stop the run so a broken game is visible instead of
        /// silently dead.
        /// </summary>
        private bool StepSafely(double deltaTime)
        {
            try
            {
                context.Combat.Tick(deltaTime);
                consecutiveFailures = 0;
                return true;
            }
            catch (Exception exception)
            {
                consecutiveFailures++;
                TickFailures++;

                if (consecutiveFailures == 1)
                {
                    Debug.LogError($"[RunController] Combat tick threw; skipping it and saving. {exception}");
                    context.Save?.SaveNow("tick-error");
                }

                if (consecutiveFailures >= MaxConsecutiveFailures)
                {
                    enabled = false;
                    SafeModeTriggered = true;
                    Debug.LogError($"[RunController] {consecutiveFailures} consecutive failures: safe mode, run stopped.");
                }

                return false;
            }
        }

        private void Update()
        {
            if (context == null || !context.IsReady)
            {
                return;
            }

            // 1) Gameplay: one accumulator-driven combat tick (no coroutines anywhere in the flow).
            //    B6 Step 3: the Speed Button scales combat time; the ledger still ticks in REAL time and the save
            //    normalises the measured rate, so fast-forward can never inflate offline/instant-income quotes.
            double deltaTime = Time.deltaTime;
            LastDeltaSeconds = deltaTime;

            double tempo = context.Automation != null ? context.Automation.SpeedMultiplier : 1d;

            if (!StepSafely(deltaTime * tempo))
            {
                return;
            }

            context.Ledger?.Tick(deltaTime);

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

            // B6: the automation engine (auto-buy etc.) runs on the same slow tick.
            context.Automation?.Tick(SlowTickIntervalSec);
        }
    }
}
