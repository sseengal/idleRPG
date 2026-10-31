namespace IdleRPG.Sim
{
    /// <summary>
    /// Randomness source for the simulation. <see cref="DeterministicRng"/> is the only implementation:
    /// a stable, splittable generator (deterministic across Unity versions).
    /// </summary>
    public interface IRng
    {
        /// <summary>Uniform value in [0,1).</summary>
        double NextDouble();

        /// <summary>Uniform integer in [0, exclusiveMax).</summary>
        int Next(int exclusiveMax);
    }
}
