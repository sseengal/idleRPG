using UnityEngine;
using IdleRPG.Sim;

namespace IdleRPG.Core
{
    /// <summary>
    /// Wires <see cref="SimLog"/> to the Unity console.
    ///
    /// The pure simulation assembly has no engine references, so it cannot log by itself. This bridge is
    /// the single place that connects sim diagnostics to the Editor/player log, and it also keeps the
    /// door open for a future tool that captures sim output into a file instead.
    /// </summary>
    public static class SimLogBridge
    {
        /// <summary>Editor tooling (Balance Lab) calls this so sim diagnostics are never swallowed.</summary>
        public static void EnsureInstalled()
        {
            Install();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            SimLog.Error = message => Debug.LogError(message);
            SimLog.Warning = message => Debug.LogWarning(message);
        }
    }
}
