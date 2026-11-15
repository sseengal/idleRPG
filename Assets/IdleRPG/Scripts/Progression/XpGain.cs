namespace IdleRPG.Progression
{
    /// <summary>
    /// One XP payment, written in the terms a bar needs to animate it: what was paid, where the level stood before,
    /// and where it stands after.
    ///
    /// ELI5: a receipt for one XP payout. The bar reads the receipt and plays the movement. It never works the
    /// numbers out from a stage, a boss flag or an enemy type, so a new XP source needs no change on the bar.
    ///
    /// Why it exists: the bar used to see only the new total. A payment that crossed a level, or two, could not be
    /// told apart from a plain refill, so the fill jumped and the reward moment was silent. This record keeps the
    /// start point, the end point and the amount in one place, and it is produced by the same call that banks the
    /// XP (<see cref="PartyProgression.AddXp"/>), so the view and the save can never disagree.
    /// </summary>
    public readonly struct XpGain
    {
        public XpGain(double amount, int startLevel, double startXp, int endLevel, double endXp,
            int levelsCrossed, bool atMax)
        {
            Amount = amount;
            StartLevel = startLevel;
            StartXp = startXp;
            EndLevel = endLevel;
            EndXp = endXp;
            LevelsCrossed = levelsCrossed;
            AtMax = atMax;
        }

        /// <summary>XP paid by this one event, before any level was taken from it.</summary>
        public double Amount { get; }

        /// <summary>Level before the payment.</summary>
        public int StartLevel { get; }

        /// <summary>XP banked towards the next level, before the payment.</summary>
        public double StartXp { get; }

        /// <summary>Level after the payment, with every level it crossed already taken.</summary>
        public int EndLevel { get; }

        /// <summary>XP banked towards the next level, after the payment.</summary>
        public double EndXp { get; }

        /// <summary>How many levels the payment crossed. 0 for most kills.</summary>
        public int LevelsCrossed { get; }

        /// <summary>True when the run sits at its level ceiling, so the bar stays full and reads MAX.</summary>
        public bool AtMax { get; }

        /// <summary>True when there is something to show. A payment blocked by the ceiling is not.</summary>
        public bool HasXp => Amount > 0d;
    }
}
