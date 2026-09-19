using System.Text;
using UnityEditor;
using UnityEngine;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Player/Build settings for a portrait 2D mobile idle game.
    /// Menu: Tools > Idle RPG > Mobile > ...
    ///
    /// Verify is read-only and safe to run any time; Apply only touches mobile-facing values and the
    /// build scene list (dev scenes stay in the project, just disabled from builds).
    /// </summary>
    public static class MobileBuildSettingsUtility
    {
        private const string MainScenePath = "Assets/IdleRPG/Scenes/Main.unity";
        private const string DebugScenePath = "Assets/IdleRPG/Scenes/CombatDebug.unity";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Tools/Idle RPG/Mobile/Verify Settings", priority = 80)]
        public static void VerifySettings()
        {
            StringBuilder report = new StringBuilder();
            int problems = 0;

            report.AppendLine("=== Idle RPG mobile settings ===");
            report.AppendLine($"product        : {PlayerSettings.productName} {PlayerSettings.bundleVersion} ({PlayerSettings.companyName})");

            problems += Check(report, "orientation",
                PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait,
                $"{PlayerSettings.defaultInterfaceOrientation} (want Portrait)");

            bool noAutoRotate = !PlayerSettings.allowedAutorotateToPortrait
                                && !PlayerSettings.allowedAutorotateToPortraitUpsideDown
                                && !PlayerSettings.allowedAutorotateToLandscapeLeft
                                && !PlayerSettings.allowedAutorotateToLandscapeRight;
            problems += Check(report, "autorotate", noAutoRotate, "all auto-rotate flags should be off");

            problems += Check(report, "color space",
                PlayerSettings.colorSpace == ColorSpace.Linear, PlayerSettings.colorSpace.ToString());

            problems += Check(report, "android backend",
                PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP,
                PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android).ToString());

            problems += Check(report, "ios backend",
                PlayerSettings.GetScriptingBackend(BuildTargetGroup.iOS) == ScriptingImplementation.IL2CPP,
                PlayerSettings.GetScriptingBackend(BuildTargetGroup.iOS).ToString());

            problems += Check(report, "android arch",
                (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0,
                PlayerSettings.Android.targetArchitectures.ToString());

            problems += Check(report, "android min sdk",
                (int)PlayerSettings.Android.minSdkVersion >= 26, PlayerSettings.Android.minSdkVersion.ToString());

            problems += Check(report, "ios device",
                PlayerSettings.iOS.targetDevice == iOSTargetDevice.iPhoneAndiPad,
                PlayerSettings.iOS.targetDevice.ToString());

            problems += Check(report, "gc incremental", PlayerSettings.gcIncremental, "should be ON on mobile");

            string androidId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            string iosId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS);
            problems += Check(report, "android bundle id",
                IsRealId(androidId), androidId + " (set a real id, e.g. com.siddharth.idlerpg)");

            problems += Check(report, "ios bundle id",
                IsRealId(iosId), iosId + " (set a real id, e.g. com.siddharth.idlerpg)");

            problems += CheckSceneList(report);

            report.AppendLine(problems == 0
                ? "RESULT: all good."
                : $"RESULT: {problems} item(s) need attention (see '--' lines). Run Apply Recommended Settings.");

            if (problems == 0)
            {
                Debug.Log(report.ToString());
            }
            else
            {
                Debug.LogWarning(report.ToString());
            }
        }

        private static bool IsRealId(string identifier)
        {
            return !string.IsNullOrEmpty(identifier)
                   && !identifier.Contains("DefaultCompany")
                   && !identifier.Contains("com.unity");
        }

        [MenuItem("Tools/Idle RPG/Mobile/Apply Recommended Settings", priority = 81)]
        public static void ApplyRecommendedSettings()
        {
            if (!EditorUtility.DisplayDialog(
                    "Apply mobile settings?",
                    "Sets portrait lock, IL2CPP + ARM64 (Android), min API 26 / iOS 15, and keeps only " +
                    "Main.unity enabled in the build list.\n\nBundle identifiers are NOT changed.",
                    "Apply",
                    "Cancel"))
            {
                return;
            }

            ApplyNow();
        }

        /// <summary>
        /// Same as the menu item but without the modal confirmation, so tooling and automation can
        /// call it (a dialog would block an unattended editor).
        /// </summary>
        public static void ApplyNow()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)26;
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.gcIncremental = true;
            PlayerSettings.stripEngineCode = true;

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainScenePath, true),
                new EditorBuildSettingsScene(DebugScenePath, false),
                new EditorBuildSettingsScene(SampleScenePath, false),
            };

            AssetDatabase.SaveAssets();
            Debug.Log("[MobileBuildSettingsUtility] Applied recommended mobile settings.");
            VerifySettings();
        }

        private static int CheckSceneList(StringBuilder report)
        {
            bool mainEnabled = false;
            bool mainFirst = false;

            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];

                if (string.Equals(scene.path, MainScenePath, System.StringComparison.OrdinalIgnoreCase))
                {
                    mainEnabled = scene.enabled;
                    mainFirst = i == 0;
                    continue;
                }

                if (scene.enabled)
                {
                    return Check(report, "build scenes", false,
                        $"{System.IO.Path.GetFileName(scene.path)} is still enabled (only Main.unity should ship)");
                }
            }

            return Check(report, "build scenes", mainEnabled && mainFirst,
                mainEnabled
                    ? (mainFirst ? "Main.unity only, first" : "Main.unity is enabled but not first")
                    : "Main.unity is missing or disabled");
        }

        private static int Check(StringBuilder report, string label, bool ok, string detail)
        {
            report.AppendLine(string.Format("  {0} {1,-16}: {2}", ok ? "OK" : "--", label, detail));
            return ok ? 0 : 1;
        }
    }
}