using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleRPG.Leaderboard
{
    /// <summary>
    /// Global ranks: a deterministic mock field (there are no accounts/backend yet), merged with the local
    /// player's live entry (tag "YOU", party level = total hero levels, best stage). Rank = position after
    /// sorting by max stage descending; equal stages keep the mock ahead so the player sees themselves
    /// climbing past ties. Real backend later swaps the mock source behind the same surface.
    /// </summary>
    public sealed class LeaderboardService
    {
        private readonly Func<int> partyLevelReader;
        private readonly Func<int> maxStageReader;
        private readonly List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

        public event Action Changed;

        public LeaderboardService(Func<int> partyLevelReader, Func<int> maxStageReader)
        {
            this.partyLevelReader = partyLevelReader;
            this.maxStageReader = maxStageReader;
        }

        /// <summary>Ranked rows, best first.</summary>
        public IReadOnlyList<LeaderboardEntry> Ranked => entries;

        /// <summary>The local player's own row.</summary>
        public LeaderboardEntry You { get; private set; }

        /// <summary>Rebuilds the board from the mock pool + the player's live numbers. Idempotent.</summary>
        public void Refresh()
        {
            int partyLevel = partyLevelReader != null ? partyLevelReader() : 0;
            int maxStage = maxStageReader != null ? maxStageReader() : 1;
            You = new LeaderboardEntry("YOU", partyLevel, maxStage, isYou: true);

            entries.Clear();
            for (int i = 0; i < MockTags.Length; i++)
            {
                entries.Add(new LeaderboardEntry(
                    MockTags[i],
                    8 + i * 9 + (i % 5) * 4,
                    3 + i * 11 + (i % 7) * 7));
            }

            entries.Add(You);

            // Stable (LINQ) sort by best stage descending: the player's row stays behind equal-stage
            // mocks, so the climb through a tie is visible.
            List<LeaderboardEntry> sorted = entries
                .OrderByDescending(e => e.MaxStage)
                .ThenBy(e => e.IsYou ? 1 : 0)
                .ToList();

            entries.Clear();
            entries.AddRange(sorted);

            Changed?.Invoke();
        }

        private static readonly string[] MockTags =
        {
            "BladeQueen", "Ashbringer", "LootGoblin", "PetalTide", "BigSwing", "Moonlit", "DustBunny",
            "ProFrog", "NightCap", "TurboSnail", "GuildWren", "ColdSnap", "EmberFox", "TideRunner",
            "StarSeed", "Bashful", "RogueWave", "PixelMage", "IronHull", "FeralCat", "LuckyPenny",
            "SilentStep", "GoldRush", "CellarKey", "MeadowCup", "SharpFang", "SlowClap", "FrostByte",
            "DaringDo", "LastStand", "CakeBoss", "TinyTitan", "MistyReef", "SunWarden", "CloverEve",
            "HollowWind", "BrightOre", "DeepDig", "QuickHand", "StoneWall"
        };
    }
}
