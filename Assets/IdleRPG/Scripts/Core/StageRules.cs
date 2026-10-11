using UnityEngine;

namespace IdleRPG.Core
{
    /// <summary>
    /// The rules of the stage bounce: how the run advances, how it falls back, when a clear counts as a first
    /// time, and when the defeat summary card opens.
    ///
    /// ELI5: the game never stops and never waits for you. Beat the stage at your ceiling and you move up one;
    /// get wiped and you drop back. That bounce IS the game, and these four decisions are the whole of it.
    ///
    /// Why they are their own class: they are where balance lives (the rollback size, the card's cooldown), and
    /// balance is the thing being rebuilt right now. As pure functions they can be swept over every stage and
    /// every cooldown boundary instead of being watched in a run. The event handlers that call them keep the
    /// sequencing - combat calls, save, logging, state changes - because that part is glue.
    ///
    /// Every expression here is the one that used to sit inline in <c>GameManager.Stages.cs</c>, moved verbatim.
    /// </summary>
    public static class StageRules
    {
        /// <summary>The stage reached after clearing <paramref name="stage"/>.</summary>
        public static int NextStage(int stage)
        {
            return stage + 1;
        }

        /// <summary>
        /// The stage the party falls back to after a wipe. Clamped at stage 1, so the bounce can never fall out
        /// of the game.
        /// </summary>
        public static int RollbackStage(int stage, int rollback)
        {
            return Mathf.Max(1, stage - rollback);
        }

        /// <summary>
        /// True when reaching <paramref name="nextStage"/> beats the lifetime best - the only case that pays
        /// milestone gems, so the fallback bounce can never farm them.
        /// </summary>
        public static bool IsNewBest(int nextStage, int highestStageReached)
        {
            return nextStage > highestStageReached;
        }

        /// <summary>
        /// Whether the defeat summary card opens. It lands on a NEW stage, or once the cooldown has passed - a
        /// hard wall bounces over and over, so a card on every wipe would spam the player for nothing.
        /// </summary>
        public static bool ShouldShowDefeatModal(int defeatedStage, int lastShownStage, float nowSeconds,
            float lastShownAt, float cooldownSeconds)
        {
            return defeatedStage != lastShownStage || nowSeconds - lastShownAt >= cooldownSeconds;
        }
    }
}
