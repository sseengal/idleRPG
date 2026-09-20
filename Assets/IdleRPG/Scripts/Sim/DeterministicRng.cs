namespace IdleRPG.Sim
{
    /// <summary>
    /// Deterministic, splittable random generator (splitmix64).
    ///
    /// ELI5: dice that always land the same way for the same seed - on any device, in any Unity version,
    /// whether the fight is watched live or replayed offline. <see cref="Fork"/> gives each purpose
    /// (targeting, crits, loot) its own private dice cup, so adding a new roll never changes the old ones.
    ///
    /// Not cryptographic. The uint modulo in <see cref="Next"/> has negligible bias for our small ranges.
    /// </summary>
    public sealed class DeterministicRng : IRng
    {
        private const ulong Gamma = 0x9E3779B97F4A7C15UL;

        private ulong state;

        public DeterministicRng(long seed)
        {
            state = unchecked((ulong)seed);
            if (state == 0UL)
            {
                // splitmix64 maps 0 to 0 on the first draw; nudge it to stay well distributed.
                state = Gamma;
            }
        }

        /// <summary>Current internal state - persist it in the save so a reload continues the sequence.</summary>
        public long State
        {
            get => unchecked((long)state);
            set
            {
                state = unchecked((ulong)value);
                if (state == 0UL)
                {
                    state = Gamma;
                }
            }
        }

        /// <summary>Uniform 32-bit value.</summary>
        public uint NextUInt()
        {
            state += Gamma;

            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;

            return (uint)(z >> 32);
        }

        public double NextDouble()
        {
            // 2^-32 resolution is plenty for combat rolls and keeps the arithmetic exact.
            return NextUInt() / 4294967296d;
        }

        public int Next(int exclusiveMax)
        {
            if (exclusiveMax <= 1)
            {
                return 0;
            }

            return (int)(NextUInt() % (uint)exclusiveMax);
        }

        /// <summary>
        /// Derives an independent stream from this one for a named purpose
        /// (e.g. <c>Fork(RngStream.Critical)</c>), so unrelated rolls stay stable.
        /// </summary>
        public DeterministicRng Fork(long salt)
        {
            ulong mixed = state ^ (unchecked((ulong)salt) * Gamma);
            return new DeterministicRng(unchecked((long)mixed));
        }
    }

    /// <summary>Purpose tags for <see cref="DeterministicRng.Fork"/> - one stream per kind of roll.</summary>
    public static class RngStream
    {
        public const long Targeting = 1L;
        public const long Critical = 2L;
        public const long Loot = 3L;
        public const long Ability = 4L;
        public const long Spawn = 5L;
        public const long Status = 6L;
    }
}
