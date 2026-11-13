using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using IdleRPG.Data;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// The journey band lab: three copies of the title band on one sheet, true size, so the land name,
    /// the trail, the camp flame and the boss diamond can be judged together.
    ///
    /// ELI5: three wooden signposts in a row. Same land, three different points on the road. We take one
    /// picture and pick the shapes from it.
    ///
    /// Menus:
    ///   Tools &gt; Idle RPG &gt; UI &gt; Build Ribbon Scene
    ///   Tools &gt; Idle RPG &gt; UI &gt; Capture Ribbon Sheet
    ///
    /// The trail comes from <see cref="JourneyResolver.BuildTrail"/>, not from numbers typed in this file,
    /// so the picture proves the real model.
    /// </summary>
    public static partial class UiCompareSceneBuilder
    {
        private const string RibbonScenePath = "Assets/IdleRPG/Scenes/UiRibbon.unity";
        private const string RibbonShotPath = "Assets/Screenshots/compare/ribbon-sheet.png";

        private const float RibbonCanvasWidth = 1080f;
        private const float RibbonCanvasHeight = 640f;
        private const float RibbonWidth = 1052f;
        private const float RibbonHeight = 100f;
        private const float RibbonStepGap = 12f;
        /// <summary>One point on the road: what the band shows.</summary>
        private sealed class RibbonState
        {
            public string Caption;
            public string LandName;
            public string BossName;
            public int NormalSteps;
            public int CurrentStep;
            public int CampEvery;
            public bool OnBoss;
        }

        private static readonly RibbonState[] RibbonStates =
        {
            new RibbonState
            {
                Caption = "1  -  six steps, step two   (stage 1 shape)",
                LandName = "GREENWAKE", BossName = "THE SLIME KING",
                NormalSteps = 6, CurrentStep = 2, CampEvery = 3
            },
            new RibbonState
            {
                Caption = "2  -  nine steps, step five, three camps   (default shape)",
                LandName = "GREENWAKE", BossName = "THE SLIME KING",
                NormalSteps = 9, CurrentStep = 5, CampEvery = 3
            },
            new RibbonState
            {
                Caption = "3  -  boss step: the name slot swaps to the boss",
                LandName = "GREENWAKE", BossName = "THE SLIME KING",
                NormalSteps = 9, CurrentStep = 10, CampEvery = 3, OnBoss = true
            }
        };

        [MenuItem("Tools/Idle RPG/UI/Build Ribbon Scene", priority = 32)]
        public static void BuildRibbonScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = CreateCamera();
            RectTransform canvasRoot = CreateRibbonCanvas(camera);

            GameObject sheetObject = UiFactory.Node("Sheet", canvasRoot);
            RectTransform sheet = sheetObject.GetComponent<RectTransform>();
            sheet.anchorMin = new Vector2(0.5f, 0.5f);
            sheet.anchorMax = new Vector2(0.5f, 0.5f);
            sheet.pivot = new Vector2(0.5f, 0.5f);
            sheet.sizeDelta = new Vector2(RibbonCanvasWidth, RibbonCanvasHeight);
            sheet.anchoredPosition = Vector2.zero;

            Fill(sheet, "Background", Looks[0].Deep);

            TMP_FontAsset font = LoadFont(Looks[0].Font);

            for (int i = 0; i < RibbonStates.Length; i++)
            {
                float y = 24f + (i * 190f);

                PixelText(Box(sheet, "Caption" + i, 14f, y, RibbonWidth, 28f), "Text",
                    RibbonStates[i].Caption, 22f, TextAlignmentOptions.MidlineLeft, Looks[0].Dim, font);

                BuildRibbon(sheet, 14f, y + 34f, RibbonStates[i], Looks[0], font);
            }

            EnsureFolder(System.IO.Path.GetDirectoryName(RibbonScenePath).Replace('\\', '/'));

            if (!EditorSceneManager.SaveScene(scene, RibbonScenePath))
            {
                Debug.LogError("[UiCompare] Failed to save " + RibbonScenePath + ".");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[UiCompare] Built " + RibbonScenePath + ". Next: Tools > Idle RPG > UI > Capture Ribbon Sheet.");
        }

        private static RectTransform CreateRibbonCanvas(Camera camera)
        {
            GameObject canvasObject = UiFactory.Node("Canvas", camera.transform);
            canvasObject.transform.SetParent(null, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 100f;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RibbonCanvasWidth, RibbonCanvasHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            return canvasObject.GetComponent<RectTransform>();
        }

        private static void BuildRibbon(Transform parent, float x, float y, RibbonState state, Look look,
            TMP_FontAsset font)
        {
            RectTransform band = OutlineBox(parent, "Band", x, y, RibbonWidth, RibbonHeight,
                look.PanelLight, look.Panel, look.Border);

            float inner = band.rect.width;

            // One slot, two states: the land on a normal step, the boss on the boss step.
            string title = state.OnBoss && !string.IsNullOrEmpty(state.BossName)
                ? state.BossName.ToUpperInvariant()
                : state.LandName;

            PixelText(Box(band, "Land", 0f, 2f, inner, 54f), "Text", title, 40f,
                TextAlignmentOptions.Center, look.Text, font);

            BuildTrail(band, state, look, 58f, inner);
        }

        private static void BuildTrail(RectTransform band, RibbonState state, Look look, float rowTop,
            float innerWidth)
        {
            List<TrailMark> marks = JourneyResolver.BuildTrail(state.NormalSteps, state.CurrentStep,
                state.CampEvery);

            float total = 0f;

            for (int i = 0; i < marks.Count; i++)
            {
                total += MarkWidth(marks[i].Kind);

                if (i > 0)
                {
                    total += GapBetween(marks[i - 1].Kind, marks[i].Kind);
                }
            }

            float x = (innerWidth - total) * 0.5f;

            for (int i = 0; i < marks.Count; i++)
            {
                TrailMark mark = marks[i];

                if (i > 0)
                {
                    x += GapBetween(marks[i - 1].Kind, mark.Kind);
                }

                float height = MarkHeight(mark.Kind);
                float y = rowTop + ((30f - height) * 0.5f);

                switch (mark.Kind)
                {
                    case TrailMarkKind.Camp:
                        BuildTent(band, "Camp" + i, x, y, mark.Cleared || mark.Available, mark.Available, look);
                        break;

                    case TrailMarkKind.Boss:
                        BuildDiamond(band, "Boss", x, y, mark.Cleared, mark.Current, look);
                        break;

                    default:
                        BuildStepMark(band, "Step" + mark.Step, x, y, mark.Cleared, mark.Current, look);
                        break;
                }

                x += MarkWidth(mark.Kind);
            }
        }

        /// <summary>A camp is a rest stop, so it gets more air on both sides than a plain step.</summary>
        private static float GapBetween(TrailMarkKind left, TrailMarkKind right)
        {
            return left == TrailMarkKind.Camp || right == TrailMarkKind.Camp ? 20f : RibbonStepGap;
        }

        private static float MarkWidth(TrailMarkKind kind)
        {
            switch (kind)
            {
                case TrailMarkKind.Boss:
                    return 30f;
                case TrailMarkKind.Camp:
                    return 22f;
                default:
                    return 20f;
            }
        }

        private static float MarkHeight(TrailMarkKind kind)
        {
            switch (kind)
            {
                case TrailMarkKind.Boss:
                    return 30f;
                case TrailMarkKind.Camp:
                    return 16f;
                default:
                    return 20f;
            }
        }

        /// <summary>A step: filled when cleared, a muted chip ahead, a ring on the current one.</summary>
        private static void BuildStepMark(RectTransform band, string name, float x, float y, bool cleared,
            bool current, Look look)
        {
            if (current)
            {
                Fill(Box(band, name + "Ring", x - 3f, y - 3f, 26f, 26f), "Ring", look.AccentLight);
            }

            RectTransform chip = Box(band, name, x, y, 20f, 20f);
            Fill(chip, "Edge", look.BorderColor);
            Fill(Box(chip, "Face", 1f, 1f, 18f, 18f), "Fill",
                cleared ? look.Accent : Muted(look.Dim, 0.35f));
        }

        /// <summary>
        /// A camp: a stepped triangle, read as a tent. Zero art, and a silhouette no step or boss shares.
        /// A flame was tried first and read as a spike at this size.
        /// </summary>
        private static void BuildTent(RectTransform band, string name, float x, float y, bool lit,
            bool available, Look look)
        {
            if (available)
            {
                // The one soft ring: a rest is due, and only then.
                Fill(Box(band, name + "Ring", x - 3f, y - 3f, 28f, 22f), "Ring", Muted(look.Accent, 0.35f));
            }

            RectTransform tent = Box(band, name, x, y, 22f, 16f);
            Color cloth = lit ? look.Accent : Muted(look.Dim, 0.35f);

            Fill(Box(tent, "Row0", 0f, 13f, 22f, 3f), "Fill", cloth);
            Fill(Box(tent, "Row1", 2f, 10f, 18f, 3f), "Fill", cloth);
            Fill(Box(tent, "Row2", 4f, 7f, 14f, 3f), "Fill", cloth);
            Fill(Box(tent, "Row3", 7f, 4f, 8f, 3f), "Fill", cloth);
        }

        /// <summary>The boss: a rotated square. A different silhouette, so no colour is needed to read it.</summary>
        private static void BuildDiamond(RectTransform band, string name, float x, float y, bool cleared,
            bool current, Look look)
        {
            RectTransform holder = Box(band, name, x, y, 30f, 30f);

            if (current)
            {
                Fill(Box(holder, "Ring", -3f, -3f, 36f, 36f), "Ring", look.AccentLight);
            }

            RectTransform diamond = UiFactory.Node("Diamond", holder).GetComponent<RectTransform>();
            diamond.anchorMin = new Vector2(0.5f, 0.5f);
            diamond.anchorMax = new Vector2(0.5f, 0.5f);
            diamond.pivot = new Vector2(0.5f, 0.5f);
            diamond.anchoredPosition = Vector2.zero;
            diamond.sizeDelta = new Vector2(26f, 26f);
            diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);

            Fill(diamond, "Edge", look.BorderColor);
            Fill(Box(diamond, "Face", 2f, 2f, 22f, 22f), "Fill",
                cleared ? look.Accent : Muted(look.Dim, 0.35f));
        }

        private static Color Muted(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        [MenuItem("Tools/Idle RPG/UI/Capture Ribbon Sheet", priority = 33)]
        public static void CaptureRibbonSheet()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();

            if (canvas == null || canvas.worldCamera == null)
            {
                Debug.LogError("[UiCompare] No canvas with a camera. Open Scenes/UiRibbon.unity first.");
                return;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();

            if (scaler == null)
            {
                Debug.LogError("[UiCompare] The ribbon scene has no CanvasScaler. Rebuild it.");
                return;
            }

            EnsureFolder(System.IO.Path.GetDirectoryName(RibbonShotPath).Replace('\\', '/'));

            CanvasScaler.ScaleMode savedMode = scaler.uiScaleMode;
            float savedFactor = scaler.scaleFactor;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            Canvas.ForceUpdateCanvases();

            RenderToPng(canvas.worldCamera, (int)RibbonCanvasWidth, (int)RibbonCanvasHeight, RibbonShotPath);

            scaler.uiScaleMode = savedMode;
            scaler.scaleFactor = savedFactor;
            Canvas.ForceUpdateCanvases();
            AssetDatabase.Refresh();

            Debug.Log("[UiCompare] Wrote " + RibbonShotPath + ".");
        }
    }
}



