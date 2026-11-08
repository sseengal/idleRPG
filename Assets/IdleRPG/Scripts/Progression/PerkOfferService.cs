using System;
using System.Collections.Generic;
using IdleRPG.Data;

namespace IdleRPG.Progression
{
    /// <summary>
    /// Deals the three cards on the level-up screen.
    ///
    /// ELI5: it picks three different cards out of the deck, weighted by rarity, and never a card you have maxed.
    ///
    /// Why the draw is seeded: the offer is written to the save. If the draw depended on live randomness, quitting
    /// the app and coming back would re-deal the same pick until a good hand appeared - a free re-roll. With a
    /// seed saved next to the ids, the same offer comes back.
    /// </summary>
    public sealed class PerkOfferService
    {
        private readonly PerkPoolDef pool;

        public PerkOfferService(PerkPoolDef pool)
        {
            this.pool = pool;
        }

        /// <summary>True when there is at least one card that could be dealt right now.</summary>
        public bool HasAnythingToOffer(PerkLoadout loadout)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                PerkData boon = pool.At(i);

                if (boon != null && !(loadout != null && loadout.IsMaxed(boon)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Draws a fresh offer: <c>perkPool.OfferSize</c> different cards, weighted by rarity, excluding maxed ones.
        /// Returns an empty list when the deck is exhausted.
        /// </summary>
        public List<PerkData> Roll(PerkLoadout loadout, int seed)
        {
            List<PerkData> offers = new List<PerkData>();

            if (pool == null || pool.Count == 0)
            {
                return offers;
            }

            List<PerkData> eligible = new List<PerkData>();

            for (int i = 0; i < pool.Count; i++)
            {
                PerkData boon = pool.At(i);

                if (boon != null && !(loadout != null && loadout.IsMaxed(boon)))
                {
                    eligible.Add(boon);
                }
            }

            // A seeded source: the same seed always deals the same hand (see the class note).
            Random random = new Random(seed);
            int wanted = Math.Min(pool.OfferSize, eligible.Count);

            while (offers.Count < wanted && eligible.Count > 0)
            {
                int picked = PickWeighted(eligible, random);

                if (picked < 0)
                {
                    break;
                }

                offers.Add(eligible[picked]);
                eligible.RemoveAt(picked);
            }

            return offers;
        }

        /// <summary>Index of the chosen card, or -1 when every remaining card has weight 0.</summary>
        private int PickWeighted(List<PerkData> candidates, Random random)
        {
            int total = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                total += pool.WeightFor(candidates[i].Rarity);
            }

            if (total <= 0)
            {
                return -1;
            }

            int roll = random.Next(0, total);
            int running = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                running += pool.WeightFor(candidates[i].Rarity);

                if (roll < running)
                {
                    return i;
                }
            }

            return candidates.Count - 1;
        }
    }
}
