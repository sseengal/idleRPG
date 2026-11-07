using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using IdleRPG.UI;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Generates the boot/splash scene (Assets/IdleRPG/Scenes/Boot.unity): a title + progress bar wired to
    /// <see cref="BootstrapSplash"/>, which async-loads Main.unity. Also sets Boot as the first build scene.
    ///
    /// Menu: Tools > Idle RPG > Build Boot Scene
    /// </summary>
    public static class BootSceneBuilder
    {
        public const string BootScenePath = "Assets/IdleRPG/Scenes/Boot.unity";
        private const string MainScenePath = "Assets/IdleRPG/Scenes/Main.unity";

        [MenuItem("Tools/Idle RPG/Build Boot Scene", priority = 0)]
        public static void BuildBootScene()
        {
            if (!TmpBootstrapper.EnsureEssentials())
            {
                Debug.LogError("[BootSceneBuilder] TMP essentials are required before building the UI.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            Camera cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.09f, 1f);
            camGO.tag = "MainCamera";

            GameObject canvasGO = new GameObject("SplashCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            Image bg = CreateImage("Background", canvasGO.transform, new Color(0.05f, 0.06f, 0.09f, 1f));
            Stretch(bg.rectTransform);

            TextMeshProUGUI title = CreateText("Title", canvasGO.transform, "Kingsguard: Tiny RPG", 72f, Color.white);
            RectTransform tr = title.rectTransform;
            tr.anchorMin = new Vector2(0.08f, 0.54f);
            tr.anchorMax = new Vector2(0.92f, 0.74f);
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            Image barBG = CreateImage("BarBG", canvasGO.transform, new Color(1f, 1f, 1f, 0.12f));
            RectTransform brt = barBG.rectTransform;
            brt.anchorMin = new Vector2(0.15f, 0.46f);
            brt.anchorMax = new Vector2(0.85f, 0.50f);
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;

            Image fill = CreateImage("BarFill", barBG.transform, new Color(0.96f, 0.80f, 0.28f, 1f));
            RectTransform frt = fill.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(0f, 1f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;

            GameObject ctrlGO = new GameObject("SplashController");
            BootstrapSplash ctrl = ctrlGO.AddComponent<BootstrapSplash>();
            SceneWiringUtility.SetFields(ctrl,
                ("titleLabel", title),
                ("progressFill", frt));

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(BootScenePath));
            EditorSceneManager.SaveScene(scene, BootScenePath);
            AssetDatabase.Refresh();

            EnsureBuildScenes();

            // Land the editor back on the main scene.
            EditorSceneManager.OpenScene(MainScenePath);

            Debug.Log("[BootSceneBuilder] Boot scene built and set as the first build scene.");
        }

        private static void EnsureBuildScenes()
        {
            List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == BootScenePath);
            list.Insert(0, new EditorBuildSettingsScene(BootScenePath, true));

            if (!list.Exists(s => s.path == MainScenePath))
            {
                list.Add(new EditorBuildSettingsScene(MainScenePath, true));
            }

            EditorBuildSettings.scenes = list.ToArray();
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();

            if (TMP_Settings.defaultFontAsset != null)
            {
                t.font = TMP_Settings.defaultFontAsset;
            }

            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
