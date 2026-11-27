using System;
using IdleRPG.Combat;
using IdleRPG.Core;
using IdleRPG.Data;

namespace IdleRPG.Progression
{
    /// <summary>
    /// The camp interlude: three choices between waves, then the fight resumes.
    ///
    /// ELI5: after waves 3, 6 and 9 the party stops at a fire. Short Rest is a small free heal, Long Rest is the
    /// bigger heal after a rewarded ad, and Train alternates +1 ATK / +1 DEF for the rest of the run. A camp heal
    /// also brings a fallen hero back at the healed fraction. The camp only decides; healing and stats belong to
    /// the game, not to this class.
    /// </summary>
    public sealed class CampService
    {
        private readonly GameManager manager;
        private readonly MonetizationService monetization;

        private int pendingStage;
        private int pendingWave;
        private bool pausedCombatForDebug;

        public CampService(GameManager manager, MonetizationService monetization)
        {
            this.manager = manager;
            this.monetization = monetization;
        }

        /// <summary>True while the camp panel owns the screen.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>The camp took the screen. The panel shows.</summary>
        public event Action Opened;

        /// <summary>The camp handed the screen back. The director resumes the wave.</summary>
        public event Action Closed;

        /// <summary>Train alternates ATK and DEF, based on the two run bonuses.</summary>
        public bool TrainNextIsAttack =>
            manager != null && (manager.RunTrainAttackBonus + manager.RunTrainDefenseBonus) % 2 == 0;

        /// <summary>
        /// Called by <see cref="CombatDirector"/> before a wave spawns. True = hold the wave; the camp panel
        /// opens and <see cref="Finish"/> resumes the director later.
        /// </summary>
        public bool ShouldHold(int stage, int nextWave)
        {
            if (manager == null || manager.Balance == null || !manager.Balance.CampEnabled || IsOpen)
            {
                return false;
            }

            if (manager.Combat == null || manager.Combat.Simulator == null)
            {
                return false;
            }

            int clearedWave = nextWave - 1;

            if (clearedWave < 1 || clearedWave >= manager.Combat.WavesPerStage)
            {
                return false;
            }

            if (clearedWave % manager.Balance.CampEveryWaves != 0)
            {
                return false;
            }

            if (stage == manager.RunLastCampStage && clearedWave == manager.RunLastCampWave)
            {
                return false;
            }

            pendingStage = stage;
            pendingWave = clearedWave;
            IsOpen = true;
            AnnounceCamp();
            Opened?.Invoke();
            return true;
        }

        /// <summary>
        /// The camp owns the log while it is open: the fight's history is wiped and one line says why the screen
        /// stopped. The rest/train lines are written only after the choice closes the camp.
        /// </summary>
        private static void AnnounceCamp()
        {
            BattleLog.Clear();
            BattleLog.Append("The party camps for the night..", BattleLogKind.Heal);
        }

        /// <summary>True while the rewarded ad is playing; the camp ignores taps until it ends.</summary>
        public bool IsAdPending { get; private set; }

        /// <summary>Free 10% heal. The fallen come back at the same fraction.</summary>
        public void ChooseShortRest()
        {
            if (!IsOpen || IsAdPending)
            {
                return;
            }

            ApplyShortRest(manager.Balance.CampShortRestPercent + TrainingCampHealBonus());
        }

        /// <summary>20% heal after a rewarded ad. The fallen come back at the same fraction. Every camp.</summary>
        public void ChooseLongRest()
        {
            if (!IsOpen || IsAdPending)
            {
                return;
            }

            IsAdPending = true;

            bool started = monetization != null && monetization.WatchAdForCampRest(
                ApplyLongRest,
                () => IsAdPending = false);

            if (!started)
            {
                IsAdPending = false;
            }
        }

        private void ApplyLongRest()
        {
            double fraction = manager.Balance.CampLongRestPercent + TrainingCampHealBonus();
            manager.HealPartyFraction(fraction, fraction);
            Finish();
        }

        /// <summary>+1 ATK or +1 DEF to every hero, alternating. Free, no cap.</summary>
        public void ChooseTrain()
        {
            if (!IsOpen || IsAdPending)
            {
                return;
            }

            bool trainedAttack = TrainNextIsAttack;
            manager.ApplyRunTrain(trainedAttack);
            BattleLog.Append($"The party trains (+1 {(trainedAttack ? "ATK" : "DEF")}).", BattleLogKind.Event);
            Finish();
        }

        /// <summary>Debug: open the camp now, without waiting for a checkpoint.</summary>
        public void DebugOpen()
        {
            if (IsOpen || manager == null || manager.Combat == null)
            {
                return;
            }

            pendingStage = manager.CurrentStage;
            pendingWave = manager.Combat.CurrentWave;
            IsOpen = true;
            pausedCombatForDebug = true;
            manager.Combat.Pause(CombatManager.PauseReasons.Camp);
            AnnounceCamp();
            Opened?.Invoke();
        }

        /// <summary>Training "Camp Heal +N" nodes add percentage points of camp healing (as a fraction).</summary>
        private double TrainingCampHealBonus()
        {
            return manager != null && manager.Training != null
                ? manager.Training.TotalFor(IdleRPG.Data.TrainingEffect.CampHealPercent) / 100.0
                : 0d;
        }

        private void ApplyShortRest(double fraction)
        {
            if (!IsOpen)
            {
                return;
            }

            IsAdPending = false;
            manager.HealPartyFraction(fraction, fraction);
            BattleLog.Append("The party takes a short rest.", BattleLogKind.Heal);
            Finish();
        }

        private void Finish()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            IsAdPending = false;
            manager.RecordCampResolved(pendingStage, pendingWave);

            if (pausedCombatForDebug)
            {
                pausedCombatForDebug = false;
                manager.Combat.Resume(CombatManager.PauseReasons.Camp);
            }

            manager.Save?.SaveNow("camp");
            Closed?.Invoke();
            manager.Combat.ResumeAfterCamp();
        }
    }
}
