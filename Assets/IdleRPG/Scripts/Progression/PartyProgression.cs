using System;
using IdleRPG.Data;
using IdleRPG.Save;

namespace IdleRPG.Progression
{
    /// <summary>
    /// The party's level and XP **for the current run**, and the boon choices those levels pay for.
    ///
    /// ELI5: the three heroes share one XP bar. When it fills, the party goes up a level and the player gets to
    /// pick one boon. A big kill can fill the bar more than once, so the spare levels wait in a queue instead of
    /// being lost.
    ///
    /// Why it is plain C# and not a MonoBehaviour: it is run state, so it has to survive a save/load, be testable
    /// on its own, and be drivable by the balance robot without a scene. The view reads it; it never reads a view.
    ///
    /// Lifetimes: levels, XP and queued picks are **run-scoped** - cleared with the run in
    /// <c>GameManager.ResetProgressForAscension</c>, exactly like run gold.
    /// </summary>
    public sealed class PartyProgression
    {
        private readonly double xpBase;
        private readonly double xpGrowth;
        private readonly double xpPerKill;
        private readonly double xpPerKillStageGrowth;
        private readonly double xpBossMultiplier;
        private readonly int maxLevel;

        /// <summary>Straight numbers, so a test or a tool can build one without a balance asset.</summary>
        public PartyProgression(double xpBase, double xpGrowth, double xpPerKill,
            double xpPerKillStageGrowth, double xpBossMultiplier, int maxLevel)
        {
            this.xpBase = xpBase > 0d ? xpBase : 20d;
            this.xpGrowth = xpGrowth >= 1d ? xpGrowth : 1d;
            this.xpPerKill = xpPerKill > 0d ? xpPerKill : 5d;
            this.xpPerKillStageGrowth = xpPerKillStageGrowth >= 1d ? xpPerKillStageGrowth : 1d;
            this.xpBossMultiplier = xpBossMultiplier > 0d ? xpBossMultiplier : 1d;
            this.maxLevel = Math.Max(0, maxLevel);
        }

        /// <summary>Built from the balance asset at boot.</summary>
        public PartyProgression(BalanceConfig balance)
            : this(balance != null ? balance.PartyXpBase : 20d,
                   balance != null ? balance.PartyXpGrowth : 1.18d,
                   balance != null ? balance.PartyXpPerKill : 5d,
                   balance != null ? balance.PartyXpPerKillStageGrowth : 1.1d,
                   balance != null ? balance.PartyXpBossMultiplier : 5d,
                   balance != null ? balance.PartyMaxLevelPerRun : 20)
        {
        }

        /// <summary>Current run level. Starts at 1.</summary>
        public int Level { get; private set; } = 1;

        /// <summary>XP banked towards the next level.</summary>
        public double Xp { get; private set; }

        /// <summary>Boon choices earned but not spent yet. The card deals them one at a time.</summary>
        public int PendingPicks { get; private set; }

        /// <summary>Levels gained this run (one level always pays exactly one pick).</summary>
        public int LevelsGained { get; private set; }

        /// <summary>True when the run has reached its level ceiling; XP no longer accumulates.</summary>
        public bool AtMaxLevel => maxLevel > 0 && Level >= maxLevel;

        /// <summary>XP needed for the next level. 0 at the ceiling.</summary>
        public double RequiredXp => AtMaxLevel ? 0d : xpBase * Math.Pow(xpGrowth, Level - 1);

        /// <summary>0..1 fill for the XP bar (1 at the ceiling).</summary>
        public double Progress
        {
            get
            {
                double required = RequiredXp;

                if (required <= 0d)
                {
                    return 1d;
                }

                double ratio = Xp / required;
                return ratio < 0d ? 0d : (ratio > 1d ? 1d : ratio);
            }
        }

        /// <summary>
        /// XP paid for one kill. Stage scaling keeps deep stages moving the bar; bosses pay a multiple.
        /// </summary>
        public double XpForKill(int stage, bool isBoss)
        {
            int safeStage = stage < 1 ? 1 : stage;
            double amount = xpPerKill * Math.Pow(xpPerKillStageGrowth, safeStage - 1);

            if (isBoss)
            {
                amount *= xpBossMultiplier;
            }

            return amount;
        }

        /// <summary>
        /// Banks XP and levels up as often as the bar fills. Returns the number of levels gained (0 for most kills).
        /// A surplus carries over, so one huge kill can queue several picks.
        /// </summary>
        public int AddXp(double amount)
        {
            if (amount <= 0d || AtMaxLevel)
            {
                return 0;
            }

            Xp += amount;

            int gained = 0;

            while (!AtMaxLevel)
            {
                double required = RequiredXp;

                if (required <= 0d || Xp < required)
                {
                    break;
                }

                Xp -= required;
                Level++;
                PendingPicks++;
                LevelsGained++;
                gained++;
            }

            // A run at its ceiling keeps no leftover XP: the bar is full and stays full.
            if (AtMaxLevel)
            {
                Xp = 0d;
            }

            return gained;
        }

        /// <summary>Spends one queued choice. False when none are waiting.</summary>
        public bool TrySpendPick()
        {
            if (PendingPicks <= 0)
            {
                return false;
            }

            PendingPicks--;
            return true;
        }

        /// <summary>Back to level 1 with an empty bar - used when the run ends and a new one begins.</summary>
        public void ResetForNewRun()
        {
            Level = 1;
            Xp = 0d;
            PendingPicks = 0;
            LevelsGained = 0;
        }

        // ------------------------------------------------------------------
        // Save contract
        // ------------------------------------------------------------------
        public void WriteToSave(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            data.runPartyLevel = Level;
            data.runPartyXp = Xp;
            data.runPartyPicks = PendingPicks;
        }

        public void LoadFromSave(SaveData data)
        {
            if (data == null)
            {
                ResetForNewRun();
                return;
            }

            Level = data.runPartyLevel < 1 ? 1 : data.runPartyLevel;
            Xp = data.runPartyXp < 0d ? 0d : data.runPartyXp;
            PendingPicks = data.runPartyPicks < 0 ? 0 : data.runPartyPicks;
            LevelsGained = Level - 1;

            if (maxLevel > 0 && Level > maxLevel)
            {
                Level = maxLevel;
            }

            if (AtMaxLevel)
            {
                Xp = 0d;
            }
        }
    }
}

