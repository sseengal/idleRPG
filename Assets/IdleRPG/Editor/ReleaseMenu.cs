using UnityEditor;
using UnityEngine;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// The release gate (B9' - part of the shipping checklist).
    ///
    /// Plain words: nothing is allowed to be built from a broken game, so this is the go/no-go button. One press:
    /// 1) run EVERY check (the whole regression net), 2) if anything failed - stop, don't stamp anything,
    /// 3) if green - stamp the version and force the phone screens to portrait, 4) say what is still missing
    /// before the actual store builds can happen.
    ///
    /// The two store builds need the iOS and Android modules which are NOT installed on this machine yet (see the
    /// B9' checklist); the gate itself is what this script does, and it can be verified blind.
    /// </summary>
    public static class ReleaseMenu
    {
        /// <summary>Single source for the first release version. Change here, stamp with the menu.</summary>
        public const string ReleaseVersion = "1.0.0";

        [MenuItem("Tools/Idle RPG/Release/Go or No-Go (checks + stamp)", priority = 20)]
        public static void GateAndStampMenu()
        {
            GateAndStamp();
        }

        /// <summary>Green-checks then stamp. Returns false (and stamps nothing) when any check fails.</summary>
        public static bool GateAndStamp()
        {
            if (!RegressionCheckMenu.RunAllChecksWithResult())
            {
                Debug.LogError("[Release] NO-GO: a check failed, so nothing was stamped. Fix the errors, then run this again.");
                return false;
            }

            PlayerSettings.bundleVersion = ReleaseVersion;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            AssetDatabase.SaveAssets();
            Debug.Log($"[Release] GO: every check is green, version '{ReleaseVersion}' stamped, phone screens locked to portrait.");

            string missing = MissingStoreModules();
            if (missing.Length > 0)
            {
                Debug.Log("[Release] The STORE BUILDS still need (owner, see the B9' checklist): " + missing + ".");
            }
            else
            {
                Debug.Log("[Release] Both store modules are installed - the builds can run.");
            }

            return true;
        }

        /// <summary>Names what is not installed on this machine yet, so the missing part is spelled out.</summary>
        private static string MissingStoreModules()
        {
            string missing = "";

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                missing += "the Android build module (add in Unity Hub), ";
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                missing += "the iOS build module (add in Unity Hub), ";
            }

            return missing.TrimEnd(' ', ',');
        }
    }
}