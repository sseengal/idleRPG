using System;

namespace IdleRPG.Core
{
    /// <summary>
    /// One sentence for the battle feed ("news"): wave headers, clears, wipes, boss outcomes,
    /// and anything a future system wants to say (item drops, perks, alerts). The log renders
    /// it verbatim with the colour hinted by <see cref="Kind"/>; it is never budget-dropped.
    /// </summary>
    public enum LogMessageKind
    {
        Event,
        Defeat,
        Reward,
        System,
    }

    public readonly struct LogMessage
    {
        public string Text { get; }

        public LogMessageKind Kind { get; }

        public LogMessage(string text, LogMessageKind kind = LogMessageKind.Event)
        {
            Text = text;
            Kind = kind;
        }

        public override string ToString() => Text;
    }
}
