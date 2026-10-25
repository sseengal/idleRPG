using UnityEngine;
using IdleRPG.Progression;

namespace IdleRPG.Core
{
    /// <summary>
    /// The camp's run-scoped state, kept on the manager so the save can reach it before the camp service exists.
    /// </summary>
    public sealed partial class GameManager
    {
        /// <summary>-1 means "old save; fill to the run's capacity on load".</summary>
        public int RunCampTokens { get; private set; } = -1;

        public int RunLastCampStage { get; private set; }

        public int RunLastCampWave { get; private set; }

        public int RunTrainAttackBonus { get; private set; }

        public int RunTrainDefenseBonus { get; private set; }

        /// <summary>The camp interlude. Created in <see cref="WireUp"/>.</summary>
        public CampService Camp { get; private set; }

        /// <summary>The money door (ads / IAP). Exposed for the camp's optional ad rest.</summary>
        public MonetizationService Monetization => monetization;

        /// <summary>Old saves have -1; give them the run's full token count.</summary>
        private void EnsureCampStateLoaded()
        {
            int capacity = balanceConfig != null ? balanceConfig.CampTokensPerRun : 1;

            if (RunCampTokens < 0)
            {
                RunCampTokens = capacity;
            }
            else if (RunCampTokens > capacity)
            {
                RunCampTokens = capacity;
            }
        }

        /// <summary>A wipe replays the stage, so its checkpoints must be offered again.</summary>
        public void ResetCampCheckpoint()
        {
            RunLastCampStage = 0;
            RunLastCampWave = 0;
            Save?.MarkDirty("camp");
        }

        public bool TryConsumeCampToken()
        {
            if (RunCampTokens <= 0)
            {
                return false;
            }

            RunCampTokens--;
            Save?.MarkDirty("camp");
            return true;
        }

        public void RecordCampResolved(int stage, int wave)
        {
            RunLastCampStage = Mathf.Max(1, stage);
            RunLastCampWave = Mathf.Max(0, wave);
            Save?.MarkDirty("camp");
        }

        /// <summary>Train alternates ATK / DEF, flat and run-only.</summary>
        public void ApplyRunTrain(bool attack)
        {
            int step = balanceConfig != null
                ? (attack ? balanceConfig.CampTrainAttackPerTrain : balanceConfig.CampTrainDefensePerTrain)
                : 1;

            if (attack)
            {
                RunTrainAttackBonus += step;
            }
            else
            {
                RunTrainDefenseBonus += step;
            }

            Resolver?.SetRunBonuses(RunTrainAttackBonus, RunTrainDefenseBonus);
            Save?.MarkDirty("camp");
        }

        /// <summary>New run / ascension: token back, training cleared.</summary>
        public void ResetRunCampState()
        {
            RunCampTokens = balanceConfig != null ? balanceConfig.CampTokensPerRun : 1;
            RunLastCampStage = 0;
            RunLastCampWave = 0;
            RunTrainAttackBonus = 0;
            RunTrainDefenseBonus = 0;
            Resolver?.ResetRunBonuses();
        }

        /// <summary>Load path: restore the run's camp state and apply the training bonuses.</summary>
        public void SetRunCampState(int tokens, int lastStage, int lastWave, int attack, int defense)
        {
            RunCampTokens = tokens < -1 ? -1 : tokens;
            RunLastCampStage = Mathf.Max(0, lastStage);
            RunLastCampWave = Mathf.Max(0, lastWave);
            RunTrainAttackBonus = Mathf.Max(0, attack);
            RunTrainDefenseBonus = Mathf.Max(0, defense);
            Resolver?.SetRunBonuses(RunTrainAttackBonus, RunTrainDefenseBonus);
        }

        /// <summary>Heal the party by a fraction of max HP; revive the fallen by a fraction.</summary>
        public void HealPartyFraction(double healFraction, double reviveFraction)
        {
            combatManager?.HealPartyFraction(healFraction, reviveFraction);
        }
    }
}
