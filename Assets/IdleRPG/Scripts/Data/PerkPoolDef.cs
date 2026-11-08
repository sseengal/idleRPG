using System.Collections.Generic;
using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// The boon deck: every card that can be dealt, plus how often each rarity shows up.
    ///
    /// ELI5: the pile of cards. It also holds the odds: commons are common, legendaries are a treat. Tuning the
    /// odds is one array, not a code change.
    /// </summary>
    [CreateAssetMenu(fileName = "BoonPool", menuName = "Idle RPG/Data/Boon Pool", order = 7)]
    public class PerkPoolDef : ScriptableObject
    {
        [Tooltip("Every boon that can be dealt. A card the player has maxed is skipped automatically.")]
        [SerializeField] private PerkData[] boons = new PerkData[0];

        [Tooltip("Relative weight per rarity: Common, Rare, Epic, Legendary. 0 = this rarity never shows.")]
        [SerializeField] private int[] rarityWeights = { 65, 25, 8, 2 };

        [Tooltip("How many cards one offer deals.")]
        [SerializeField] private int offerSize = 3;

        [Tooltip("Free re-rolls per run (the REFRESH button).")]
        [SerializeField] private int rerollsPerRun = 1;

        public int Count => boons != null ? boons.Length : 0;

        public int OfferSize => Mathf.Clamp(offerSize, 1, 4);

        public int RerollsPerRun => Mathf.Max(0, rerollsPerRun);

        public PerkData At(int index)
        {
            if (boons == null || index < 0 || index >= boons.Length)
            {
                return null;
            }

            return boons[index];
        }

        public int WeightFor(PerkRarity rarity)
        {
            if (rarityWeights == null)
            {
                return 0;
            }

            int index = (int)rarity;

            return index >= 0 && index < rarityWeights.Length ? Mathf.Max(0, rarityWeights[index]) : 0;
        }

        /// <summary>Finds a boon by its save id (used when a save is loaded).</summary>
        public PerkData Find(string boonId)
        {
            if (string.IsNullOrEmpty(boonId) || boons == null)
            {
                return null;
            }

            for (int i = 0; i < boons.Length; i++)
            {
                PerkData boon = boons[i];

                if (boon != null && string.Equals(boon.BoonID, boonId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return boon;
                }
            }

            return null;
        }

        /// <summary>Read-only view for tools (the generator and the balance lab).</summary>
        public IReadOnlyList<PerkData> Boons => boons ?? new PerkData[0];
    }
}
