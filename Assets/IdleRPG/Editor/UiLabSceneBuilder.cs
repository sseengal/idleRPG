using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// UI Lab v3 - one full phone screen per look, only the font AND its palette differ between looks.
    ///
    /// v1 lessons applied: (1) no more stacked strips - one look per 1080x1920 screen; (2) tint only WHITE
    /// sprite faces, or the pack's baked colours shift the hue; (3) the fonts are SDF (see
    /// PixelFontAssetGenerator), so nothing garbles at any screen scale - including the editor Game view;
    /// (4) the unit placements mirror the REAL battle screen (heroes board on the left, enemy stack on the
    /// right - same bands as MvpSceneBuilder.Battle.cs); (5) each look has a distinct palette so the three
    /// PNGs are obviously different.
    ///
    /// Menu: Tools > Idle RPG > Build UI Lab Scene
    /// Menu: Tools > Idle RPG > Capture UI Lab Snapshot  (writes uilab-A/B/C.png at 1:1)
    /// </summary>
    public static class UiLabSceneBuilder
    {
        public const string ScenePath = "Assets/IdleRPG/Scenes/UiLab.unity";

        private const float ReferenceWidth = 1080f;
        private const float ReferenceHeight = 1920f;

        private const string ArtRoot = UiArtPresetApplier.UiArtRoot + "/9-Slice/";
        private const string FontRoot = "Assets/IdleRPG/Resources/Fonts/";
        private const string PlaceholderArt = "Assets/IdleRPG/Art/Placeholder/";
        private const string BackdropPath = "Assets/IdleRPG/Art/Backdrops/forest bg.png";

        // Shared tokens: everything that does not change between looks.
        private static readonly Color BgColor = new Color32(16, 19, 28, 255);
        private static readonly Color BackdropTint = new Color(0.30f, 0.32f, 0.42f, 1f);
        private static readonly Color Slab = new Color32(12, 15, 21, 255);
        private static readonly Color SlabTag = new Color32(126, 136, 160, 255);
        private static readonly Color DamageColor = new Color(0.98f, 0.78f, 0.35f, 1f);

        private static readonly string[] LogLines =
        {
            "-- Wave 6: Goblin x2, Bat --",
            "Knight hits Goblin A for 12.4K",
            "Archer hits Bat B for 8.1K",
            "Mage crits Goblin A for 21.7K",
            "Goblin A hits Knight for 3.2K"
        };

        private static readonly string[] NavLabels = { "BATTLE", "PARTY", "UPGRADES", "ASCEND", "SHOP" };

        /// <summary>One candidate look: a font, its sizes, and a complete palette for it.</summary>
        private sealed class Look
        {
            public string Caption;
            public string Font;
            public float LogSize;
            public float ChunkySize;
            public float LabelSize;
            public float NumberSize;
            public float TitleSize;

            public Color Panel;
            public Color PanelLight;
            public Color Accent;
            public Color Text;
            public Color Dim;
            public Color InkOnAccent;
            public Color Hp;
            public Color HpEnemy;
        }

        private static readonly Look[] Looks =
        {
            new Look
            {
                Caption = "A  ·  SILKSCREEN  ·  slate + gold", Font = "Silkscreen Pixel",
                LogSize = 24f, ChunkySize = 32f, LabelSize = 24f, NumberSize = 40f, TitleSize = 16f,

                Panel = new Color32(37, 43, 62, 255),
                PanelLight = new Color32(48, 56, 80, 255),
                Accent = new Color32(214, 158, 66, 255),
                Text = new Color32(236, 240, 248, 255),
                Dim = new Color32(146, 156, 180, 255),
                InkOnAccent = new Color32(38, 30, 12, 255),
                Hp = new Color32(224, 178, 74, 255),
                HpEnemy = new Color32(190, 74, 58, 255)
            },
            new Look
            {
                Caption = "B  ·  VT323  ·  dark parchment + amber", Font = "VT323 Pixel",
                LogSize = 32f, ChunkySize = 48f, LabelSize = 32f, NumberSize = 48f, TitleSize = 24f,

                Panel = new Color32(58, 46, 32, 255),
                PanelLight = new Color32(71, 58, 42, 255),
                Accent = new Color32(226, 172, 92, 255),
                Text = new Color32(244, 238, 224, 255),
                Dim = new Color32(166, 153, 133, 255),
                InkOnAccent = new Color32(52, 36, 14, 255),
                Hp = new Color32(230, 186, 100, 255),
                HpEnemy = new Color32(196, 86, 64, 255)
            },
            new Look
            {
                Caption = "C  ·  JERSEY 10  ·  indigo + cyan", Font = "Jersey10 Pixel",
                LogSize = 30f, ChunkySize = 40f, LabelSize = 20f, NumberSize = 40f, TitleSize = 20f,

                Panel = new Color32(35, 40, 68, 255),
                PanelLight = new Color32(46, 53, 88, 255),
                Accent = new Color32(94, 205, 214, 255),
                Text = new Color32(230, 238, 250, 255),
                Dim = new Color32(140, 152, 184, 255),
                InkOnAccent = new Color32(10, 24, 32, 255),
                Hp = new Color32(110, 210, 180, 255),
                HpEnemy = new Color32(230, 90, 80, 255)
            }
        };

        [MenuItem("Tools/Idle RPG/Build UI Lab Scene", priority = 2)]
        public static void BuildLabScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = CreateCamera();
            RectTransform canvasRoot = CreateCanvas(camera);

            // A fixed-size stage so every look is laid out in exact 1080x1920 coordinates.
            GameObject stageObject = UiFactory.Node("Stage", canvasRoot);
            RectTransform stage = stageObject.GetComponent<RectTransform>();
            stage.anchorMin = new Vector2(0.5f, 0.5f);
            stage.anchorMax = new Vector2(0.5f, 0.5f);
            stage.pivot = new Vector2(0.5f, 0.5f);
            stage.sizeDelta = new Vector2(ReferenceWidth, ReferenceHeight);

            for (int i = 0; i < Looks.Length; i++)
            {
                RectTransform lookRoot = UiFactory.Node("Look_" + Looks[i].Caption.Substring(0, 1), stage)
                    .GetComponent<RectTransform>();
                UiFactory.Stretch(lookRoot, 0f, 0f, 0f, 0f);

                BuildLook(lookRoot, Looks[i]);
                lookRoot.gameObject.SetActive(i == 0);
            }

            EnsureFolder(Path.GetDirectoryName(ScenePath).Replace('\\', '/'));

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"[UiLabSceneBuilder] Failed to save {ScenePath}.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[UiLabSceneBuilder] Built {ScenePath}: {Looks.Length} full-screen looks. " +
                      "SDF fonts render cleanly at any Game view size; the PNGs are still the verdict images.");
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("LabCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(10, 12, 18, 255);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static RectTransform CreateCanvas(Camera camera)
        {
            GameObject canvasObject = UiFactory.Node("Canvas", camera.transform);
            canvasObject.transform.SetParent(null, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 100f;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvasObject.GetComponent<RectTransform>();
        }

        // ------------------------------------------------------------------
        // One full-screen look
        // ------------------------------------------------------------------
        private static void BuildLook(RectTransform root, Look look)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontRoot + look.Font + ".asset");

            if (font == null)
            {
                Debug.LogWarning($"[UiLabSceneBuilder] Missing font '{look.Font}'. " +
                                 "Run Tools > Idle RPG > Art > Generate Pixel Fonts.");
            }

            Image bg = UiFactory.Icon("Bg", root, BgColor, false);
            UiFactory.Stretch(bg.rectTransform);

            PixelText(Band(root, "Caption", 16f, 36f, 28f, 28f), "Title", look.Caption, look.TitleSize,
                TextAlignmentOptions.MidlineLeft, look.Dim, font);

            BuildTopBar(root, look, font);
            BuildViewport(root, look, font);
            BuildLog(root, look, font);
            BuildNav(root, look, font);

            PixelText(Band(root, "Watermark", 1868f, 28f, 28f, 28f), "Text", "UI LAB  ·  NOT PRODUCTION",
                16f, TextAlignmentOptions.MidlineRight, look.Dim, null);
        }

        private static void BuildTopBar(RectTransform root, Look look, TMP_FontAsset font)
        {
            RectTransform bar = Band(root, "TopBar", 64f, 150f, 28f, 28f);
            Image9(bar, "Frame", "Colored/grey", look.Panel);

            string[] names = { "Gold", "Gems", "Tokens" };
            string[] icons = { "ui_icon_gold.png", "ui_icon_gem.png", "ui_icon_token.png" };
            string[] values = { "12.4K", "421", "36" };

            float chipWidth = 316f;

            for (int i = 0; i < 3; i++)
            {
                float x = 24f + (i * (chipWidth + 40f));
                RectTransform chip = Place(bar, names[i] + "Chip", x, 18f, chipWidth, 96f);

                // The token chip is the accent face (Colored/grey is white, so the tint is exact).
                bool accent = i == 2;
                Image9(chip, "Frame", "Colored/grey", accent ? look.Accent : look.PanelLight);
                IconImage(chip, "Icon", icons[i], 60f);

                PixelText(Place(chip, "Value", 96f, 12f, 190f, 72f), "ValueText", values[i], look.NumberSize,
                    TextAlignmentOptions.MidlineLeft, accent ? look.InkOnAccent : look.Text, font);
            }

            PixelText(Place(bar, "Stage", 24f, 110f, 1028f, 34f), "StageLabel", "STAGE 12  ·  WAVE 3",
                look.LabelSize, TextAlignmentOptions.MidlineRight, look.Dim, font);
        }

        /// <summary>
        /// Mirrors the real battle viewport bands (MvpSceneBuilder.Battle.cs): the hero formation board
        /// anchored left-centre, the enemy stack anchored on the right. Stand-in slabs keep those positions.
        /// </summary>
        private static void BuildViewport(RectTransform root, Look look, TMP_FontAsset font)
        {
            RectTransform viewport = Band(root, "Viewport", 230f, 872f, 28f, 28f);

            Image backdrop = UiFactory.Icon("Backdrop", viewport, BackdropTint, false);
            backdrop.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackdropPath);
            UiFactory.Stretch(backdrop.rectTransform);

            // Enemy stack: right anchored (real scene: 0.54..0.98 x, 0.10..0.92 y) = 3 stacked slabs.
            for (int i = 0; i < 3; i++)
            {
                RectTransform slab = Place(viewport, "Enemy" + (i + 1), 560f, 120f + (i * 230f), 444f, 200f);
                Image bgSlab = UiFactory.Icon("Slab", slab, Slab, false);
                UiFactory.Stretch(bgSlab.rectTransform);

                Solid(slab, "Hp", 14f, 12f, 416f, 10f, look.HpEnemy);
                PixelText(Place(slab, "Tag", 0f, 30f, 444f, 34f), "Text", "E" + (i + 1), look.LogSize * 0.5f,
                    TextAlignmentOptions.Center, SlabTag, font);
            }

            // Hero formation board: left anchored (real scene: 320x470 board, left-centre). Two columns,
            // front column nearest the enemies (right), two heroes, one behind.
            RectTransform board = Place(viewport, "HeroBoard", 20f, 200f, 340f, 470f);
            RectTransform[] heroSlots =
            {
                Place(board, "Hero1", 200f, 40f, 120f, 190f),
                Place(board, "Hero2", 200f, 250f, 120f, 190f),
                Place(board, "Hero3", 20f, 40f, 120f, 190f)
            };

            for (int i = 0; i < heroSlots.Length; i++)
            {
                Image bgSlab = UiFactory.Icon("Slab", heroSlots[i], Slab, false);
                UiFactory.Stretch(bgSlab.rectTransform);
                Solid(heroSlots[i], "Hp", 10f, 10f, 100f, 10f, look.Hp);
                PixelText(Place(heroSlots[i], "Tag", 0f, 26f, 120f, 30f), "Text", "H" + (i + 1),
                    look.LogSize * 0.5f, TextAlignmentOptions.Center, SlabTag, font);
            }

            // Damage numbers float on the enemy side - where the real pool is anchored.
            PixelText(Place(viewport, "Damage", 620f, 60f, 400f, 90f), "Text", "-12.4K",
                look.ChunkySize, TextAlignmentOptions.MidlineLeft, DamageColor, font);
        }

        private static void BuildLog(RectTransform root, Look look, TMP_FontAsset font)
        {
            RectTransform log = Band(root, "Log", 1118f, 460f, 28f, 28f);
            Image9(log, "Frame", "Colored/grey", look.Panel);

            PixelText(Place(log, "Caption", 20f, 18f, 600f, 30f), "CaptionText",
                "COMBAT LOG  ·  " + look.LogSize + "px", look.LabelSize * 0.6f,
                TextAlignmentOptions.MidlineLeft, look.Dim, font);

            float lineHeight = look.LogSize + 14f;
            float y = 58f;

            for (int i = 0; i < LogLines.Length; i++)
            {
                PixelText(Place(log, "Line" + i, 20f, y, 1000f, lineHeight), "Text", LogLines[i], look.LogSize,
                    TextAlignmentOptions.MidlineLeft, look.Text, font);
                y += lineHeight + 4f;
            }
        }

        private static void BuildNav(RectTransform root, Look look, TMP_FontAsset font)
        {
            RectTransform nav = Band(root, "Nav", 1604f, 150f, 28f, 28f);
            Image9(nav, "Frame", "Colored/grey", look.Panel);

            float gap = 8f;
            float slotWidth = (1024f - (4f * gap)) / 5f;

            for (int i = 0; i < NavLabels.Length; i++)
            {
                bool active = i == 0;
                RectTransform slot = Place(nav, "Nav" + NavLabels[i], i * (slotWidth + gap), 10f, slotWidth, 130f);
                Image9(slot, "Frame", "Colored/grey", active ? look.Accent : look.PanelLight);

                PixelText(Place(slot, "Label", 4f, 8f, slotWidth - 8f, 114f), "Text", NavLabels[i],
                    look.LabelSize, TextAlignmentOptions.Center, active ? look.InkOnAccent : look.Text, font);
            }
        }

        // ------------------------------------------------------------------
        // Snapshot: one 1:1 PNG per look. The PNGs are the verdict - they cannot smear.
        // ------------------------------------------------------------------
        [MenuItem("Tools/Idle RPG/Capture UI Lab Snapshot", priority = 3)]
        public static void CaptureSnapshot()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            CanvasScaler scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
            Transform stage = canvas != null ? canvas.transform.Find("Stage") : null;

            if (canvas == null || scaler == null || stage == null)
            {
                Debug.LogError("[UiLabSceneBuilder] Open UiLab.unity (Tools > Idle RPG > Build UI Lab Scene) first.");
                return;
            }

            var roots = new System.Collections.Generic.List<RectTransform>();

            foreach (Transform child in stage)
            {
                if (child.name.StartsWith("Look_"))
                {
                    roots.Add(child.GetComponent<RectTransform>());
                }
            }

            if (roots.Count == 0)
            {
                Debug.LogError("[UiLabSceneBuilder] Found no Look_* roots under Stage - rebuild the scene.");
                return;
            }

            const string folder = "Assets/Screenshots/ui-lab";
            EnsureFolder(folder);

            Camera camera = canvas.worldCamera != null
                ? canvas.worldCamera
                : Object.FindAnyObjectByType<Camera>();

            const int width = (int)ReferenceWidth;
            const int height = (int)ReferenceHeight;

            CanvasScaler.ScaleMode previousMode = scaler.uiScaleMode;
            float previousFactor = scaler.scaleFactor;

            foreach (RectTransform root in roots)
            {
                string letter = root.name.Substring("Look_".Length);

                foreach (RectTransform other in roots)
                {
                    other.gameObject.SetActive(other == root);
                }

                var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                var image = new Texture2D(width, height, TextureFormat.RGBA32, false);

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;

                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                camera.Render();

                RenderTexture previousActive = RenderTexture.active;
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply();
                RenderTexture.active = previousActive;

                camera.targetTexture = null;

                string path = folder + "/uilab-" + letter + ".png";
                File.WriteAllBytes(path, image.EncodeToPNG());
                AssetDatabase.ImportAsset(path);

                Object.DestroyImmediate(image);
                Object.DestroyImmediate(target);
            }

            scaler.uiScaleMode = previousMode;
            scaler.scaleFactor = previousFactor;

            Debug.Log($"[UiLabSceneBuilder] Wrote {roots.Count} full-screen look PNGs to {folder}/uilab-*.png");
        }

        // ------------------------------------------------------------------
        // Layout helpers
        // ------------------------------------------------------------------

        /// <summary>A band that stretches across its parent, top edge 'top' px below the parent's top.</summary>
        private static RectTransform Band(Transform parent, string name, float top, float height,
            float left, float right)
        {
            GameObject node = UiFactory.Node(name, parent);
            RectTransform rect = node.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -(top + height));
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>A fixed-size box whose top-left is (x, y) from the parent's top-left.</summary>
        private static RectTransform Place(Transform parent, string name, float x, float y,
            float width, float height)
        {
            GameObject node = UiFactory.Node(name, parent);
            RectTransform rect = node.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        /// <summary>A 9-sliced image stretched over its rect, sprite from the UI pack, tinted.</summary>
        private static Image Image9(Transform parent, string name, string slicePath, Color tint)
        {
            string path = slicePath.EndsWith(".png", System.StringComparison.Ordinal)
                ? ArtRoot + slicePath
                : ArtRoot + slicePath + ".png";

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Image image = UiFactory.Icon(name, parent, tint, false);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            UiFactory.Stretch(image.rectTransform);
            return image;
        }

        /// <summary>A fixed-size placeholder icon in the parent's top-left corner.</summary>
        private static void IconImage(Transform parent, string name, string placeholderFile, float size)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderArt + placeholderFile);
            Image image = UiFactory.Icon(name, parent, Color.white, true);
            image.sprite = sprite;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(16f, -18f);
            rect.sizeDelta = new Vector2(size, size);
        }

        /// <summary>A flat-colour solid rectangle at a fixed position/size.</summary>
        private static void Solid(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            RectTransform rect = Place(parent, name, x, y, w, h);
            rect.gameObject.AddComponent<CanvasRenderer>();
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
        }

        /// <summary>TMP text stretched into its parent rect, optionally swapped to a pixel font.</summary>
        private static TextMeshProUGUI PixelText(Transform parent, string name, string content, float size,
            TextAlignmentOptions alignment, Color color, TMP_FontAsset font)
        {
            TextMeshProUGUI text = UiFactory.Text(name, parent, content, size, alignment, color);

            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }

            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            UiFactory.Stretch(text.rectTransform);
            return text;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
        }
    }
}