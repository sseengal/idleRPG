using UnityEngine;

namespace IdleRPG.Core
{
    /// <summary>
    /// Device-level settings that must be in place before the first scene loads: frame pacing,
    /// screen sleep behaviour and a one-line device summary in the log.
    ///
    /// Runs automatically (no scene wiring needed) and never touches the Editor's own pacing so
    /// play-mode feel and profiling stay unaffected.
    /// </summary>
    public static class MobileRuntimeBootstrap
    {
        /// <summary>Target frame rate on device. 60 keeps the 2D scene smooth without cooking batteries.</summary>
        public const int TargetFrameRate = 60;

        /// <summary>
        /// An idle game is watched while it farms, so keep the screen on. Set false to let the OS
        /// dim the display instead.
        /// </summary>
        public const bool KeepScreenAwake = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            if (Application.isEditor)
            {
                // Leave the Editor alone: the game view and profiler keep their own pacing.
                return;
            }

            // vSync must be off for targetFrameRate to be honoured.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;

            if (KeepScreenAwake)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }

            Application.runInBackground = false;

            Debug.Log($"[MobileRuntimeBootstrap] {SystemInfo.deviceModel} | {SystemInfo.operatingSystem} | " +
                      $"{Screen.width}x{Screen.height} @ {Screen.dpi:0}dpi | {SystemInfo.systemMemorySize}MB RAM | " +
                      $"{SystemInfo.processorCount} cores | target {TargetFrameRate}fps | safeArea {Screen.safeArea}");
        }
    }
}