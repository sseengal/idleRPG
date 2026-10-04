namespace IdleRPG.Leaderboard
{
    /// <summary>One row of the global ranks board.</summary>
    public sealed class LeaderboardEntry
    {
        public string Tag { get; }

        /// <summary>Party level = total hero stat levels (the player's live value).</summary>
        public int PartyLevel { get; }

        /// <summary>Best stage ever reached.</summary>
        public int MaxStage { get; }

        /// <summary>True for the local player's own row.</summary>
        public bool IsYou { get; }

        public LeaderboardEntry(string tag, int partyLevel, int maxStage, bool isYou = false)
        {
            Tag = string.IsNullOrEmpty(tag) ? "?" : tag;
            PartyLevel = partyLevel < 0 ? 0 : partyLevel;
            MaxStage = maxStage < 1 ? 1 : maxStage;
            IsYou = isYou;
        }
    }
}
