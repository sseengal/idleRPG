using System;
using System.Collections.Generic;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Save;

namespace IdleRPG.Progression
{
    /// <summary>Outcome of a purchase attempt, so each caller keeps its own player-facing wording.</summary>
    public enum PurchaseResult
    {
        Bought = 0,
        Invalid = 1,
        Maxed = 2,
        CannotAfford = 3
    }

    /// <summary>
    /// The single place where progression levels are bought.
    ///
    /// ELI5: the game has two checkouts today - one for gold upgrades, one for token upgrades - and each repeats the
    /// same six steps (may I? clamp to the cap, work out the price, take the money, add the level, tell everyone).
    /// Two copies of one rule is how they drift apart. This class is the one checkout; the other two now just talk to it.
    ///
    /// Scope of this session: levels still live in <see cref="StatResolver"/> (it stays the owner) and the menu rows are
    /// built from the assets we already ship, so nothing the player sees changes. Later sessions make the rows data
    /// (so a new row needs no code) and move the save keys.
    /// </summary>
    public sealed class TrackService
    {
        private readonly EconomyManager economy;
        private readonly StatResolver resolver;
        private readonly List<ProgressionTrack> tracks = new List<ProgressionTrack>();
        private readonly Dictionary<string, int> indexById = new Dictionary<string, int>();
        private readonly Dictionary<HeroStatType, int> indexByStat = new Dictionary<HeroStatType, int>();
        private readonly Dictionary<string, int> automationLevels = new Dictionary<string, int>();

        /// <summary>Raised after a purchase: the row, the new level and what it cost.</summary>
        public event Action<ProgressionTrack, int, double> Purchased;

        public TrackService(EconomyManager economy, StatResolver resolver,
            IEnumerable<StatUpgradeData> heroTracks, IEnumerable<PrestigeUpgradeData> globalTracks,
            IEnumerable<AutomationDef> automationDefs, float fallbackCostGrowth)
        {
            this.economy = economy;
            this.resolver = resolver;

            if (heroTracks != null)
            {
                foreach (StatUpgradeData data in heroTracks)
                {
                    if (data == null)
                    {
                        continue;
                    }

                    Add(ProgressionTrack.FromStatUpgrade(data, fallbackCostGrowth));

                    if (!indexByStat.ContainsKey(data.StatType))
                    {
                        indexByStat[data.StatType] = tracks.Count - 1;
                    }
                }
            }

            if (globalTracks != null)
            {
                foreach (PrestigeUpgradeData data in globalTracks)
                {
                    if (data != null)
                    {
                        Add(ProgressionTrack.FromPrestige(data));
                    }
                }
            }

            if (automationDefs != null)
            {
                foreach (AutomationDef data in automationDefs)
                {
                    if (data != null)
                    {
                        Add(ProgressionTrack.FromAutomation(data));
                    }
                }
            }
        }

        /// <summary>Every row, in authoring order.</summary>
        public IReadOnlyList<ProgressionTrack> Tracks => tracks;

        /// <summary>Automation card ownership, loaded with the save (B6).</summary>
        public void FillFromSave(SaveData data)
        {
            automationLevels.Clear();

            if (data == null)
            {
                return;
            }

            for (int i = 0; i < tracks.Count; i++)
            {
                if (tracks[i].IsAutomation)
                {
                    automationLevels[tracks[i].Id] = data.GetLevel(tracks[i].Id);
                }
            }
        }

        /// <summary>Automation card ownership into the save's keyed levels (B6).</summary>
        public void WriteToSave(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            for (int i = 0; i < tracks.Count; i++)
            {
                if (tracks[i].IsAutomation)
                {
                    int level = automationLevels.TryGetValue(tracks[i].Id, out int value) ? value : 0;
                    data.SetLevel(tracks[i].Id, level);
                }
            }
        }

        public int Count => tracks.Count;

        private void Add(ProgressionTrack track)
        {
            if (string.IsNullOrEmpty(track.Id))
            {
                return;
            }

            tracks.Add(track);

            if (!indexById.ContainsKey(track.Id))
            {
                indexById[track.Id] = tracks.Count - 1;
            }
        }

        public bool TryGetTrack(string id, out ProgressionTrack track)
        {
            track = default;

            if (string.IsNullOrEmpty(id) || !indexById.TryGetValue(id, out int index))
            {
                return false;
            }

            track = tracks[index];
            return true;
        }

        /// <summary>The gold-bought hero stat row (ATK / HP / DEF).</summary>
        public bool TryGetHeroTrack(HeroStatType statType, out ProgressionTrack track)
        {
            track = default;

            if (!indexByStat.TryGetValue(statType, out int index))
            {
                return false;
            }

            track = tracks[index];
            return true;
        }

        // ------------------------------------------------------------------
        // Levels + pricing (the values still live in StatResolver)
        // ------------------------------------------------------------------
        /// <summary>Current level of a row, for one hero (hero rows) or the account (global rows).</summary>
        public int GetLevel(ProgressionTrack track, HeroData hero)
        {
            if (resolver == null && !track.IsAutomation)
            {
                return 0;
            }

            if (track.IsAutomation)
            {
                return automationLevels.TryGetValue(track.Id, out int own) ? own : 0;
            }

            if (track.Scope == TrackScope.Global && track.GlobalSource != null)
            {
                return resolver.GetPrestigeLevel(track.GlobalSource);
            }

            if (track.Scope == TrackScope.Hero && hero != null && track.StatSource != null)
            {
                return resolver.GetHeroLevel(hero, track.Stat);
            }

            return 0;
        }

        /// <summary>Writes a level. Hero rows need the hero; global rows ignore it.</summary>
        public void SetLevel(ProgressionTrack track, HeroData hero, int level)
        {
            if (track.IsAutomation)
            {
                automationLevels[track.Id] = level < 0 ? 0 : level;
                return;
            }

            if (resolver == null || !track.HasSource)
            {
                return;
            }

            if (track.Scope == TrackScope.Global && track.GlobalSource != null)
            {
                resolver.SetPrestigeLevel(track.GlobalSource, level);
                return;
            }

            if (track.Scope == TrackScope.Hero && hero != null && track.StatSource != null)
            {
                resolver.SetHeroLevel(hero, track.Stat, level);
            }
        }

        /// <summary>Cost of the next <paramref name="levels"/> levels of a row.</summary>
        public double Cost(ProgressionTrack track, HeroData hero, int levels = 1)
        {
            return track.CostFor(GetLevel(track, hero), levels);
        }

        public bool IsMaxed(ProgressionTrack track, HeroData hero)
        {
            return track.IsAtMaxLevel(GetLevel(track, hero));
        }

        /// <summary>True when the current balance covers the next <paramref name="levels"/> levels.</summary>
        public bool CanAfford(ProgressionTrack track, HeroData hero, int levels = 1)
        {
            return economy != null && economy.CanAfford(track.Currency, Cost(track, hero, levels));
        }

        // ------------------------------------------------------------------
        // The one purchase path
        // ------------------------------------------------------------------
        /// <summary>
        /// Buys <paramref name="levels"/> of a row: clamp to the cap, price it, spend it, write the level, announce it.
        /// This is the only method in the game that turns a currency into a progression level, so "shop maths" can
        /// never disagree between callers again.
        /// </summary>
        public PurchaseResult TryBuy(ProgressionTrack track, HeroData hero, int levels,
            out double spent, out int newLevel)
        {
            spent = 0d;
            newLevel = 0;

            if (economy == null || resolver == null || string.IsNullOrEmpty(track.Id) || levels <= 0)
            {
                return PurchaseResult.Invalid;
            }

            if (track.Scope == TrackScope.Hero && (hero == null || track.StatSource == null))
            {
                return PurchaseResult.Invalid;
            }

            if (track.Scope == TrackScope.Global && !track.IsAutomation && track.GlobalSource == null)
            {
                return PurchaseResult.Invalid;
            }

            int currentLevel = GetLevel(track, hero);
            if (track.IsAtMaxLevel(currentLevel))
            {
                return PurchaseResult.Maxed;
            }

            int wanted = track.ClampLevels(currentLevel, levels);
            if (wanted <= 0)
            {
                return PurchaseResult.Maxed;
            }

            double cost = track.CostFor(currentLevel, wanted);
            if (!economy.SpendCurrency(track.Currency, cost))
            {
                return PurchaseResult.CannotAfford;
            }

            newLevel = currentLevel + wanted;
            spent = cost;

            SetLevel(track, hero, newLevel);
            Purchased?.Invoke(track, newLevel, cost);

            return PurchaseResult.Bought;
        }
    }
}
