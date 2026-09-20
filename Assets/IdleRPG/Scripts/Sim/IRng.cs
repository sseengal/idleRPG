using System;

namespace IdleRPG.Sim
{
    /// <summary>
    /// Randomness source for the simulation. 7a ships <see cref="SystemRng"/> to keep behaviour
    /// identical to the MVP while the structures move; 7b replaces it with a stable,
    /// splittable generator (deterministic across Unity versions).
    /// </summary>
    public interface IRng
    {
        /// <summary>Uniform value in [0,1).</summary>
        double NextDouble();

        /// <summary>Uniform integer in [0, exclusiveMax).</summary>
        int Next(int exclusiveMax);
    }

    /// <summary>`System.Random`-backed generator - same sequence as the MVP build.</summary>
    public sealed class SystemRng : IRng
    {
        private readonly Random random;

        public SystemRng(int seed)
        {
            random = new Random(seed);
        }

        public double NextDouble()
        {
            return random.NextDouble();
        }

        public int Next(int exclusiveMax)
        {
            return exclusiveMax <= 0 ? 0 : random.Next(exclusiveMax);
        }
    }
}
