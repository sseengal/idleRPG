using System;
using IdleRPG.Core;
using IdleRPG.Data;

namespace IdleRPG.Progression
{
    /// <summary>
    /// The camp interlude: three choices between waves, then the fight resumes.
    ///
    /// ELI5: after waves 3, 6 and 9 the party stops at a fire. Short Rest is a small free heal, Long Rest is the
    /// big heal plus revive (one token per run), and Train alternates +1 ATK / +1 DEF for the rest of the run.
    /// The camp only decides; healing and stats belong to the game, not to this class.
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

        public int Tokens => manager != null ? manager.RunCampTokens : 0;

        public bool CanLongRest => Tokens > 0;

        /// <summary>Train alternates ATK and DEF, based on the two run bonuses.</summary>
        public bool TrainNextIsAttack =>
            manager != null && (manager.RunTrainAttackBonus + manager.RunTrainDefenseBonus) % 2 == 0;

        /// <summary>
        /// True when a camp would be offered before <paramref name="nextWave"/>. Pure: it does not open the
        /// camp or change any state. The UI uses it to pick the full CRT for a wave that leads into camp.
        /// </summary>
        public bool WouldHold(int stage, int nextWave)
        {
            if (manager == null || manager.Balance == null || !manager.Balance.CampEnabled)
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

            return true;
        }

        /// <summary>
        /// Called by <see cref="CombatDirector"/> before a wave spawns. True = hold the wave; the camp panel
        /// opens and <see cref="Finish"/> resumes the director later.
        /// </summary>
        public bool ShouldHold(int stage, int nextWave)
        {
            if (IsOpen || !WouldHold(stage, nextWave))
            {
                return false;
            }

            pendingStage = stage;
            pendingWave = nextWave - 1;
            IsOpen = true;
            Opened?.Invoke();
            return true;
        }

        /// <summary>True while the rewarded ad is playing; the camp ignores taps until it ends.</summary>
        public bool IsAdPending { get; private set; }

        /// <summary>Free 10%, or 20% after the ad (free for No Ads). Does not spend the token.</summary>
        public void ChooseShortRest(bool useAd)
        {
            if (!IsOpen || IsAdPending)
            {
                return;
            }

            if (!useAd || monetization == null)
            {
                ApplyShortRest(manager.Balance.CampShortRestPercent);
                return;
            }

            IsAdPending = true;

            bool started = monetization.WatchAdForCampRest(
                () => ApplyShortRest(manager.Balance.CampShortRestAdPercent),
                () => IsAdPending = false);

            if (!started)
            {
                IsAdPending = false;
            }
        }

        /// <summary>30% + revive. Spends the one run token.</summary>
        public void ChooseLongRest()
        {
            if (!IsOpen || IsAdPending || !manager.TryConsumeCampToken())
            {
                return;
            }

            manager.HealPartyFraction(manager.Balance.CampLongRestPercent, manager.Balance.CampRevivePercent);
            BattleLog.Append($"The party makes camp (+{manager.Balance.CampLongRestPercent * 100f:0}% HP, fallen revived).",
                BattleLogKind.Heal);
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
            manager.Combat.Paused = true;
            Opened?.Invoke();
        }

        private void ApplyShortRest(double fraction)
        {
            if (!IsOpen)
            {
                return;
            }

            IsAdPending = false;
            manager.HealPartyFraction(fraction, 0d);
            BattleLog.Append($"The party takes a short rest (+{fraction * 100f:0}% HP).", BattleLogKind.Heal);
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
                manager.Combat.Paused = false;
            }

            manager.Save?.SaveNow("camp");
            Closed?.Invoke();
            manager.Combat.ResumeAfterCamp();
        }
    }
}
