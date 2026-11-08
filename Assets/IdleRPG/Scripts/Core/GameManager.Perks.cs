using IdleRPG.Progression;

namespace IdleRPG.Core
{
    /// <summary>
    /// Party XP and boon picks for the current run.
    ///
    /// ELI5: killing enemies fills a shared XP bar. Each time it fills, the party gains a level and the player is
    /// owed one boon choice. This file owns the wiring: the bar is fed from the same place that pays gold, the
    /// numbers live in <see cref="PartyProgression"/>, and both are cleared when the run ends.
    ///
    /// Why here and not in the tracker: the tracker is plain C# with no idea what a stage, a boss or an event bus
    /// is, so it stays testable. The manager knows all three, so it does the translating.
    /// </summary>
    public sealed partial class GameManager
    {
        /// <summary>The party's run level, XP and queued boon picks.</summary>
        public PartyProgression Progression { get; private set; }

        /// <summary>Levels a single run may reach (0 = no cap). Read by the HUD and the boon card.</summary>
        public int PartyLevelCap => balanceConfig != null ? balanceConfig.PartyMaxLevelPerRun : 20;

        /// <summary>
        /// Creates the tracker. Called by the composition root BEFORE the save is loaded, so a resumed run gets its
        /// level and XP back through the normal save path.
        /// </summary>
        private void BuildPartyProgression()
        {
            Progression = new PartyProgression(balanceConfig);
            RaisePartyXpChanged();
        }

        /// <summary>
        /// Pays XP for one kill and turns it into levels. Called from the kill reward path, so gold and XP always
        /// come from the same event - a kill can never pay one and not the other.
        /// </summary>
        private void AwardKillXp()
        {
            if (Progression == null)
            {
                return;
            }

            bool isBoss = combatManager != null && combatManager.IsBossWave;
            int levels = Progression.AddXp(Progression.XpForKill(CurrentStage, isBoss));

            RaisePartyXpChanged();

            if (levels > 0)
            {
                // One event per kill, not per level: the card reads PendingPicks, so crossing two levels at once
                // simply deals two choices back to back.
                GameEvents.RaisePartyLevelGained(Progression.Level, Progression.PendingPicks);
            }
        }

        /// <summary>
        /// Dev/testing: banks party XP through the SAME path a kill uses, so the bar and the pick counter move
        /// exactly as they would in play. Returns the number of levels gained.
        /// </summary>
        public int DebugGrantPartyXp(double amount)
        {
            if (Progression == null)
            {
                return 0;
            }

            int levels = Progression.AddXp(amount);
            RaisePartyXpChanged();

            if (levels > 0)
            {
                GameEvents.RaisePartyLevelGained(Progression.Level, Progression.PendingPicks);
            }

            return levels;
        }

        /// <summary>Tells the HUD where the bar stands (level, XP, XP needed).</summary>
        private void RaisePartyXpChanged()
        {
            if (Progression == null)
            {
                return;
            }

            GameEvents.RaisePartyXpChanged(Progression.Level, Progression.Xp, Progression.RequiredXp);
        }

        /// <summary>
        /// Clears this run's level, XP and queued picks. Called by the run reset next to the camp and gold reset -
        /// levels are run-scoped, exactly like run gold.
        /// </summary>
        private void ResetPartyProgressionForNewRun()
        {
            if (Progression == null)
            {
                return;
            }

            Progression.ResetForNewRun();
            RaisePartyXpChanged();

            // The bar is rebuilt for the new run whether or not a view is listening.
            GameEvents.RaisePartyLevelGained(Progression.Level, Progression.PendingPicks);
        }
    }
}
