using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Ensures TextMeshPro's essential resources (default font + shaders) exist before any
    /// TMP object is created. Scriptable: imports the package asset silently, no dialog.
    /// </summary>
    public static class TmpBootstrapper
    {
        /// <summary>Where "TMP Essential Resources" puts the default font asset.</summary>
        public const string DefaultFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [MenuItem("Tools/Idle RPG/Setup/Import TMP Essentials", priority = 5)]
        public static void ImportEssentialsMenu()
        {
            EnsureEssentials();
        }

        /// <summary>
        /// Imports TMP essentials when missing. Returns true when the default font asset is
        /// available afterwards, so callers can hard-fail instead of creating broken text.
        /// </summary>
        public static bool EnsureEssentials()
        {
            if (IsReady())
            {
                return true;
            }

            string packagePath = ResolveEssentialsPackagePath();
            if (string.IsNullOrEmpty(packagePath))
            {
                Debug.LogError("[TmpBootstrapper] TMP Essential Resources package not found inside com.unity.ugui. " +
                               "Import it manually via Window > TextMeshPro > Import TMP Essential Resources.");
                return false;
            }

            Debug.Log($"[TmpBootstrapper] Importing TMP Essentials from {packagePath}");
            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh();

            bool ready = IsReady();
            Debug.Log(ready
                ? "[TmpBootstrapper] TMP Essentials ready."
                : "[TmpBootstrapper] Import finished but the default font asset is still missing.");

            return ready;
        }

        /// <summary>True when the default TMP font asset is importable from the project.</summary>
        public static bool IsReady()
        {
            return GetDefaultFont() != null;
        }

        /// <summary>The shared default font, or null when essentials are missing.</summary>
        public static TMP_FontAsset GetDefaultFont()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontAssetPath);
        }

        private static string ResolveEssentialsPackagePath()
        {
            UnityEditor.PackageManager.PackageInfo packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui/package.json");
            if (packageInfo == null)
            {
                return null;
            }

            string candidate = Path.Combine(packageInfo.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            return File.Exists(candidate) ? candidate : null;
        }
    }
}