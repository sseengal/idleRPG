using System;

namespace IdleRPG.Sim
{
    /// <summary>
    /// Logging hook for the pure simulation.
    ///
    /// `Sim/` has no engine references (`noEngineReferences` in the asmdef), so it cannot call
    /// `UnityEngine.Debug`. The runtime registers handlers once at boot
    /// (`Core/SimLogBridge`), which keeps tooling/tests free to capture sim output too.
    /// </summary>
    public static class SimLog
    {
        /// <summary>Set by the runtime assembly; null = silent.</summary>
        public static Action<string> Error;

        /// <summary>Set by the runtime assembly; null = silent.</summary>
        public static Action<string> Warning;

        public static bool IsCaptured => Error != null || Warning != null;

        public static void LogError(string message)
        {
            Action<string> handler = Error;
            if (handler != null)
            {
                handler(message);
            }
        }

        public static void LogWarning(string message)
        {
            Action<string> handler = Warning;
            if (handler != null)
            {
                handler(message);
            }
        }
    }
}
