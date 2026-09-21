using System.Collections.Generic;
using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// One entry of the wave-size recipe: "this many enemies, this often".
    ///
    /// ELI5: the recipe for how often a fight has 1, 2 or 3 enemies. Weights are relative, so
    /// (1 x25)(2 x50)(3 x25) means "out of 20 fights: 5 singles, 10 doubles, 5 triples".
    /// </summary>
    [System.Serializable]
    public struct WaveCountWeight
    {
        public int count;
        public int weight;

        public WaveCountWeight(int count, int weight)
        {
            this.count = count;
            this.weight = weight;
        }
    }
}
