using System.Collections.Generic;
using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// The player's starting party (3 heroes in fixed lanes, per design).
    /// </summary>
    [CreateAssetMenu(fileName = "PartyConfig", menuName = "Idle RPG/Data/Party Config", order = 12)]
    public class PartyConfig : ScriptableObject
    {
        /// <summary>
        /// Used only by tools when no FormationData asset is available yet (the board decides the real number).
        /// </summary>
        public const int FallbackPartySize = 3;

        [Tooltip("The roster. Hero index (0, 1, 2...) is the hero's identity for saves; where it stands is the " +
                 "formation's job (Step 10).")]
        [SerializeField] private List<HeroData> heroes = new List<HeroData>();

        public IReadOnlyList<HeroData> Heroes => heroes;

        /// <summary>Number of valid (non-null) heroes.</summary>
        public int ValidHeroCount
        {
            get
            {
                if (heroes == null)
                {
                    return 0;
                }

                int count = 0;
                for (int i = 0; i < heroes.Count; i++)
                {
                    if (heroes[i] != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Returns hero at index, or null when missing/invalid.</summary>
        public HeroData GetHero(int index)
        {
            if (heroes == null || index < 0 || index >= heroes.Count)
            {
                return null;
            }

            return heroes[index];
        }

        /// <summary>True when the party is usable at runtime.</summary>
        public bool IsValid
        {
            get
            {
                if (ValidHeroCount == 0)
                {
                    return false;
                }

                for (int i = 0; i < heroes.Count; i++)
                {
                    HeroData hero = heroes[i];
                    if (hero != null && !hero.IsValid)
                    {
                        Debug.LogError($"[PartyConfig] Hero '{hero.name}' has invalid stats (need HP > 0 and ATK > 0).");
                        return false;
                    }
                }

                return true;
            }
        }

        private void OnValidate()
        {
            if (heroes == null)
            {
                heroes = new List<HeroData>();
                return;
            }

            for (int i = 0; i < heroes.Count; i++)
            {
                if (heroes[i] == null)
                {
                    Debug.LogWarning($"[PartyConfig] Roster slot {i} is empty.");
                }
            }
        }
    }
}