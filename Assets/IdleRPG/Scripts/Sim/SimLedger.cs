using System.Collections.Generic;

namespace IdleRPG.Sim
{
    /// <summary>
    /// Measures what the game actually produces while played, so every time-based payout can be priced from
    /// reality instead of a second set of formulas.
    ///
    /// ELI5: the speedometer. It watches how much gold and how many kills the party really achieves, averaged
    /// over a rolling minute, and remembers how long a stage takes. Offline payouts, expeditions and the dev
    /// overlay all ask the speedometer instead of guessing - so "away" income can never drift away from
    /// "playing" income.
    ///
    /// Pure C#: time arrives through <see cref="Tick"/> (no Unity clock), so live play, fast-forward and an
    /// offline estimate all measure identically.
    /// </summary>
    public sealed class SimLedger
    {
        private readonly Queue<Sample> samples = new Queue<Sample>(256);

        private readonly double windowSeconds;
        private double windowTime;
        private double windowTotal;
        private double firstSampleTime = -1d;
        private int windowKills;
        private int totalKills;
        private double totalGold;

        public SimLedger(double windowSeconds = 60d)
        {
            this.windowSeconds = windowSeconds < 5d ? 5d : windowSeconds;
        }

        /// <summary>Gold per second over the rolling window (0 until the first payout).</summary>
        public double GoldPerSecond { get; private set; }

        /// <summary>Kills per second over the rolling window.</summary>
        public double KillsPerSecond { get; private set; }

        /// <summary>Seconds the last completed stage took (0 when unknown).</summary>
        public double SecondsPerStage { get; private set; }

        /// <summary>Fastest stage seen this session (a quick "how well can we farm" number).</summary>
        public double BestSecondsPerStage { get; private set; }

        /// <summary>Gold earned this session, including external grants (offline claims, debug grants).</summary>
        public double SessionGold => totalGold;

        public int SessionKills => totalKills;

        /// <summary>Seconds of usable samples collected.</summary>
        public double SampledSeconds => firstSampleTime < 0d ? 0d : windowTime - firstSampleTime;

        /// <summary>True once the measurement is worth trusting.</summary>
        public bool HasEnoughSamples => SampledSeconds >= 10d && GoldPerSecond > 0d;

        /// <summary>Advances the ledger clock (call once per frame with the same dt the sim gets).</summary>
        public void Tick(double deltaTime)
        {
            if (deltaTime <= 0d)
            {
                return;
            }

            windowTime += deltaTime;
            Trim();
        }

        /// <summary>Records an earned payout. External = offline claim / debug grant (excluded from the rate).</summary>
        public void RecordGold(double amount, bool external = false)
        {
            if (amount <= 0d)
            {
                return;
            }

            totalGold += amount;

            if (external)
            {
                return;
            }

            if (firstSampleTime < 0d)
            {
                firstSampleTime = windowTime;
            }

            samples.Enqueue(new Sample(windowTime, amount));
            windowTotal += amount;
            Trim();
        }

        public void RecordKill()
        {
            totalKills++;
            windowKills++;
        }

        /// <summary>Records a finished stage/encounter duration.</summary>
        public void RecordStage(double seconds)
        {
            if (seconds <= 0d)
            {
                return;
            }

            SecondsPerStage = seconds;

            if (BestSecondsPerStage <= 0d || seconds < BestSecondsPerStage)
            {
                BestSecondsPerStage = seconds;
            }
        }

        /// <summary>Seeds the gold rate (from a save) so payouts work before any live sample exists.</summary>
        public void SeedGoldPerSecond(double goldPerSecond)
        {
            if (goldPerSecond > 0d && GoldPerSecond <= 0d)
            {
                GoldPerSecond = goldPerSecond;
                firstSampleTime = windowTime - windowSeconds;
            }
        }

        /// <summary>Clears the session counters (ascension / new run).</summary>
        public void ResetSession()
        {
            totalGold = 0d;
            totalKills = 0;
        }

        public override string ToString()
        {
            return $"SimLedger(gold/s {GoldPerSecond:0.##}, kills/s {KillsPerSecond:0.##}, " +
                   $"stage {SecondsPerStage:0}s, session {totalGold:0} gold / {totalKills} kills)";
        }

        private void Trim()
        {
            double cutoff = windowTime - windowSeconds;

            while (samples.Count > 0 && samples.Peek().Time < cutoff)
            {
                windowTotal -= samples.Dequeue().Amount;
            }

            if (windowTotal < 0d)
            {
                windowTotal = 0d;
            }

            // Average over the sampled span (not the whole window) so a short session still measures.
            double span = SampledSeconds;
            if (span > windowSeconds)
            {
                span = windowSeconds;
            }

            if (span <= 0.5d)
            {
                return;
            }

            GoldPerSecond = windowTotal / span;
            KillsPerSecond = windowKills / span;
        }

        private readonly struct Sample
        {
            public readonly double Time;
            public readonly double Amount;

            public Sample(double time, double amount)
            {
                Time = time;
                Amount = amount;
            }
        }
    }
}
