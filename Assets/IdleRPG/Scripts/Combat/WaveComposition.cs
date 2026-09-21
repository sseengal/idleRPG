using IdleRPG.Data;

namespace IdleRPG.Combat
{
    /// <summary>
    /// Decides **how many enemies** a wave holds - the recipe, not the fight.
    ///
    /// ELI5: fights should not all be the same size. This looks at (stage, wave), turns that pair into a fixed
    /// random-looking number, and reads the recipe in `BalanceConfig` to answer "1, 2 or 3?". Recipe A is
    /// 1 x25 / 2 x50 / 3 x25, so out of 20 fights: 5 singles, 10 doubles, 5 triples - average two enemies.
    ///
    /// Three rules this deliberately follows:
    /// 1. **Derived, not stored.** The answer depends only on (stage, wave), so the save file needs nothing new
    ///    and resuming mid-stage shows the same fight the player left.
    /// 2. **Never the sim RNG.** Drawing here would shift crit/loot rolls and entangle balance with wave size.
    /// 3. **Boss waves are always one** enemy: a boss is a moment, not a crowd.
    /// </summary>
    public static class WaveComposition
    {
        /// <summary>Enemy count for one wave. Always inside [MinEnemiesPerWave, MaxEnemiesPerWave] and at least 1.</summary>
        public static int ResolveCount(BalanceConfig balance, int stage, int wave, bool isBoss)
        {
            if (isBoss)
            {
                return 1;
            }

            if (balance == null)
            {
                return 1;
            }

            int min = balance.MinEnemiesPerWave;
            int max = balance.MaxEnemiesPerWave;

            if (max <= min)
            {
                return min;
            }

            var recipe = balance.WaveCountWeights;
            int totalWeight = 0;

            for (int i = 0; i < recipe.Count; i++)
            {
                if (IsUsable(recipe[i], min, max))
                {
                    totalWeight += recipe[i].weight;
                }
            }

            if (totalWeight <= 0)
            {
                return min;   // misconfigured recipe: fall back to the safest size
            }

            int roll = (int)(Hash(stage, wave) % (uint)totalWeight);

            for (int i = 0; i < recipe.Count; i++)
            {
                if (!IsUsable(recipe[i], min, max))
                {
                    continue;
                }

                roll -= recipe[i].weight;

                if (roll < 0)
                {
                    return Clamp(recipe[i].count, min, max);
                }
            }

            return min;
        }

        /// <summary>Every wave size the recipe can produce, for tools and validators.</summary>
        public static string Describe(BalanceConfig balance)
        {
            if (balance == null)
            {
                return "(no balance config)";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            var recipe = balance.WaveCountWeights;
            int total = 0;

            for (int i = 0; i < recipe.Count; i++)
            {
                if (IsUsable(recipe[i], balance.MinEnemiesPerWave, balance.MaxEnemiesPerWave))
                {
                    total += recipe[i].weight;
                }
            }

            for (int i = 0; i < recipe.Count; i++)
            {
                if (!IsUsable(recipe[i], balance.MinEnemiesPerWave, balance.MaxEnemiesPerWave))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                double percent = total <= 0 ? 0d : 100d * recipe[i].weight / total;
                builder.Append($"{recipe[i].count}x {percent:0.#}%");
            }

            builder.Append($" (mean {balance.MeanEnemiesPerWave:0.##}, boss always 1)");
            return builder.ToString();
        }

        private static bool IsUsable(WaveCountWeight entry, int min, int max)
        {
            return entry.weight > 0 && entry.count >= min && entry.count <= max;
        }

        /// <summary>
        /// Mixes (stage, wave) into a well-spread 32-bit value. A plain `(stage * 31 + wave) % 3` would cycle
        /// 1,2,3,1,2,3 - players notice that instantly - so the pair goes through an integer hash first.
        /// </summary>
        private static uint Hash(int stage, int wave)
        {
            uint value = (uint)((stage < 1 ? 1 : stage) * 73856093) ^ (uint)((wave < 1 ? 1 : wave) * 19349663);
            return Mix(Mix(value));
        }

        private static uint Mix(uint value)
        {
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            value ^= value >> 16;
            return value;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
