using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// One boon: a stay-for-this-run upgrade the player picks on level up.
    ///
    /// ELI5: a card. It says what it gives ("+12% attack") and who it gives it to (everyone, or one hero). The
    /// run keeps the cards you pick and loses them when the run ends.
    ///
    /// Why a target and not just "party": a boon that only helps the Mage is the interesting kind, and making the
    /// target a field means "thorns for the Mage +5" is one asset, never a special case in code.
    /// </summary>
    [CreateAssetMenu(fileName = "Boon", menuName = "Idle RPG/Data/Boon", order = 6)]
    public class PerkData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable id used by saves. Empty = the asset name.")]
        [SerializeField] private string boonID = "";
        [SerializeField] private string displayName = "";
        [Tooltip("Optional. Empty = built from the effects, so the numbers can never disagree with the card.")]
        [TextArea]
        [SerializeField] private string description = "";

        [Header("Look")]
        [SerializeField] private PerkRarity rarity = PerkRarity.Common;
        [Tooltip("Placeholder for now: a coloured badge with the first letter is drawn when this is empty.")]
        [SerializeField] private Sprite icon;

        [Header("Who it helps")]
        [Tooltip("True = the whole party. False = only the hero whose id is below.")]
        [SerializeField] private bool allHeroes = true;
        [Tooltip("Hero id from the party config (e.g. knight, soldier, mage). Ignored when allHeroes is on.")]
        [SerializeField] private string heroID = "";

        [Header("Effect")]
        [SerializeField] private PerkEffectSpec[] effects = new PerkEffectSpec[0];

        [Tooltip("How many times this card may be taken in one run. 0 = unlimited.")]
        [SerializeField] private int maxStacks = 5;

        public string BoonID => string.IsNullOrEmpty(boonID) ? name : boonID;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        public PerkRarity Rarity => rarity;

        public Sprite Icon => icon;

        public bool AllHeroes => allHeroes;

        public string HeroID => heroID;

        public int MaxStacks => maxStacks < 0 ? 0 : maxStacks;

        public IReadOnlyList<PerkEffectSpec> Effects => effects;

        /// <summary>True when this boon helps the hero with that id.</summary>
        public bool AppliesTo(string candidateHeroId)
        {
            if (allHeroes)
            {
                return true;
            }

            return !string.IsNullOrEmpty(candidateHeroId) &&
                   string.Equals(heroID, candidateHeroId, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>"All heroes" / "Mage only" - the second line on the card.</summary>
        public string TargetLabel
        {
            get
            {
                if (allHeroes)
                {
                    return "All heroes";
                }

                if (string.IsNullOrEmpty(heroID))
                {
                    return "One hero";
                }

                // "hero_soldier" reads as "Soldier only": drop the internal prefix, then capitalise.
                string name = heroID.StartsWith("hero_") ? heroID.Substring(5) : heroID;

                return string.IsNullOrEmpty(name)
                    ? "One hero"
                    : char.ToUpperInvariant(name[0]) + name.Substring(1) + " only";
            }
        }

        /// <summary>
        /// The line under the title. Written by hand when provided, otherwise built from the effects so the
        /// numbers on the card and the numbers the card gives are the same numbers.
        /// </summary>
        public string Description
        {
            get
            {
                if (!string.IsNullOrEmpty(description))
                {
                    return description;
                }

                if (effects == null || effects.Length == 0)
                {
                    return "No effect";
                }

                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < effects.Length; i++)
                {
                    if (effects[i] == null)
                    {
                        continue;
                    }

                    if (builder.Length > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(effects[i].ToDisplayString());
                }

                return builder.ToString();
            }
        }

        /// <summary>Everything this card adds of one op, already summed (0 when it does not carry that op).</summary>
        public double Sum(PerkEffectSpec.Op operation)
        {
            double total = 0d;

            if (effects == null)
            {
                return total;
            }

            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i] != null && effects[i].Operation == operation)
                {
                    total += effects[i].Amount;
                }
            }

            return total;
        }
    }
}
