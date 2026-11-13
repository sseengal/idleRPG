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
    /// Side-by-side compare lab for the battle screen: three whole phone screens, one per look direction, true
    /// 1080 x 1920. This is not shipping UI. It exists so one person can judge three directions from PNG files.
    ///
    /// ELI5: three mock phones in a row. Same fight, same words, three different skins. We take a picture of each
    /// and one wide picture of all three, so the choice is made on pixels, not on a memory of the editor window.
    ///
    /// Menus:
    ///   Tools &gt; Idle RPG &gt; UI &gt; Build Compare Scene   (writes Scenes/UiCompare.unity)
    ///   Tools &gt; Idle RPG &gt; UI &gt; Capture Compare PNGs    (writes Screenshots/compare/*.png)
    ///
    /// The three looks:
    ///   A - Storybook Night : dark leather, parchment text, amber. Ink borders, cut corners.
    ///   B - Arcade Cabinet  : near-black, gold, chunky. Hard edges, loud type, glow.
    ///   C - Neon Ruins      : indigo, cyan, cold. Thin glowing edges.
    ///
    /// This file is a design lab, so it may pick a gap or a text size by hand - the same exception the older UI Lab
    /// has (see UiSpace and UiType). The shipping scene never does.
    /// </summary>
    public static partial class UiCompareSceneBuilder
    {
        private const string ScenePath = "Assets/IdleRPG/Scenes/UiCompare.unity";
        private const string ArtRoot = UiArtPresetApplier.UiArtRoot + "/9-Slice/";
        private const string FontRoot = "Assets/IdleRPG/Resources/Fonts/";
        private const string PlaceholderArt = "Assets/IdleRPG/Art/Placeholder/";
        private const string BackdropPath = "Assets/IdleRPG/Art/Backdrops/forest bg.png";
        private const string ShotFolder = "Assets/Screenshots/compare";

        // One phone screen. The row holds three of them plus two gutters.
        private const float ScreenWidth = 1080f;
        private const float ScreenHeight = 1920f;
        private const float ScreenGutter = 40f;
        private const float RowWidth = (ScreenWidth * 3f) + (ScreenGutter * 2f);

        // Bands copied from the live battle page (Docs/UI-Layout.md 2b).
        private const float HeaderTop = 14f;
        private const float HeaderHeight = 101f;
        private const float StageTop = 143f;
        private const float StageHeight = 100f;
        private const float ArenaTop = 251f;
        private const float ArenaHeight = 915f;
        private const float XpTop = 1166f;
        private const float XpHeight = 54f;
        private const float ActionsTop = 1220f;
        private const float ActionsHeight = 197f;
        private const float LogTop = 1417f;
        private const float LogHeight = 489f;

        /// <summary>One candidate look: a complete palette, a border weight, a font and its sizes.</summary>
        private sealed class Look
        {
            public string Letter;
            public string Caption;
            public string Font;

            public float Border;
            public float LogSize;
            public float NumberSize;
            public float TitleSize;
            public float LabelSize;

            public Color Deep;
            public Color Panel;
            public Color PanelLight;
            public Color BorderColor;
            public Color Text;
            public Color Dim;
            public Color Accent;
            public Color AccentLight;
            public Color Hero;
            public Color Enemy;
            public Color InkOnAccent;
            public Color BackdropTint;

            public bool CornerTicks;
            public bool SegmentedBar;
            public bool TopEdgeGlow;
        }

        private static readonly Look[] Looks =
        {
            new Look
            {
                Letter = "A",
                Caption = "STORYBOOK NIGHT  -  leather, parchment, amber",
                Font = "Silkscreen Pixel",
                Border = 3f, LogSize = 36f, NumberSize = 56f, TitleSize = 20f, LabelSize = 28f,
                Deep = Hex("171310"), Panel = Hex("221B15"), PanelLight = Hex("2E251C"),
                BorderColor = Hex("0B0908"), Text = Hex("E7D8B8"), Dim = Hex("A08F72"),
                Accent = Hex("E0A03C"), AccentLight = Hex("F0C05A"),
                Hero = Hex("7C9A58"), Enemy = Hex("C0503F"), InkOnAccent = Hex("241A0E"),
                BackdropTint = new Color(0.55f, 0.46f, 0.36f, 1f),
                CornerTicks = true, SegmentedBar = true
            },
            new Look
            {
                Letter = "B",
                Caption = "ARCADE CABINET  -  near-black, gold, hard edges",
                Font = "Silkscreen Pixel",
                Border = 4f, LogSize = 44f, NumberSize = 64f, TitleSize = 24f, LabelSize = 30f,
                Deep = Hex("0E1118"), Panel = Hex("1A2130"), PanelLight = Hex("26324A"),
                BorderColor = Hex("05070C"), Text = Hex("EFF4FF"), Dim = Hex("93A0BD"),
                Accent = Hex("FFC63D"), AccentLight = Hex("FFE28A"),
                Hero = Hex("5BE08A"), Enemy = Hex("FF5A5A"), InkOnAccent = Hex("1A1200"),
                BackdropTint = new Color(0.32f, 0.36f, 0.46f, 1f),
                TopEdgeGlow = true
            },
            new Look
            {
                Letter = "C",
                Caption = "NEON RUINS  -  indigo, cyan, cold light",
                Font = "Jersey10 Pixel",
                Border = 1f, LogSize = 34f, NumberSize = 60f, TitleSize = 22f, LabelSize = 26f,
                Deep = Hex("0A0A12"), Panel = Hex("16121F"), PanelLight = Hex("221A33"),
                BorderColor = Hex("32E0E8"), Text = Hex("E6F7FF"), Dim = Hex("7F8FAA"),
                Accent = Hex("32E0E8"), AccentLight = Hex("B45CFF"),
                Hero = Hex("4CFFC4"), Enemy = Hex("FF4C7A"), InkOnAccent = Hex("04161A"),
                BackdropTint = new Color(0.25f, 0.30f, 0.52f, 1f)
            }
        };

        [MenuItem("Tools/Idle RPG/UI/Build Compare Scene", priority = 30)]
        public static void BuildCompareScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = CreateCamera();
            RectTransform canvasRoot = CreateCanvas(camera);

            // The row of screens, centred on the canvas. Its anchored position moves for each 1:1 capture.
            GameObject rowObject = UiFactory.Node("Row", canvasRoot);
            RectTransform row = rowObject.GetComponent<RectTransform>();
            row.anchorMin = new Vector2(0.5f, 0.5f);
            row.anchorMax = new Vector2(0.5f, 0.5f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.sizeDelta = new Vector2(RowWidth, ScreenHeight);
            row.anchoredPosition = Vector2.zero;

            for (int i = 0; i < Looks.Length; i++)
            {
                float x = i * (ScreenWidth + ScreenGutter);
                RectTransform screen = UiFactory.Node("Screen_" + Looks[i].Letter, row).GetComponent<RectTransform>();
                screen.anchorMin = new Vector2(0f, 1f);
                screen.anchorMax = new Vector2(0f, 1f);
                screen.pivot = new Vector2(0f, 1f);
                screen.anchoredPosition = new Vector2(x, 0f);
                screen.sizeDelta = new Vector2(ScreenWidth, ScreenHeight);

                BuildScreen(screen, Looks[i]);
                BuildCaption(row, Looks[i], x);
            }

            EnsureFolder(Path.GetDirectoryName(ScenePath).Replace('\\', '/'));

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("[UiCompare] Failed to save " + ScenePath + ".");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[UiCompare] Built " + ScenePath + ": three full battle screens. " +
                      "Next: Tools > Idle RPG > UI > Capture Compare PNGs.");
        }

        private static void BuildCaption(RectTransform row, Look look, float x)
        {
            // Outside the screen rect on purpose, so it never appears in the 1:1 PNG.
            RectTransform caption = Box(row, "Caption_" + look.Letter, x, -56f, ScreenWidth, 44f);
            PixelText(caption, "Text", look.Letter + "  .  " + look.Caption, look.TitleSize,
                TextAlignmentOptions.MidlineLeft, look.Dim, LoadFont(look.Font));
        }

        // ------------------------------------------------------------------
        // One battle screen
        // ------------------------------------------------------------------
        private static void BuildScreen(RectTransform root, Look look)
        {
            TMP_FontAsset font = LoadFont(look.Font);

            Fill(root, "Background", look.Deep);

            BuildHeader(root, look, font);
            BuildStageRibbon(root, look, font);
            BuildArena(root, look, font);
            BuildXpBar(root, look, font);
            BuildActions(root, look, font);
            BuildFeed(root, look, font);
        }

        private static void BuildHeader(RectTransform root, Look look, TMP_FontAsset font)
        {
            RectTransform header = Box(root, "Header", 14f, HeaderTop, 1052f, HeaderHeight);

            // Storybook and neon let the art breathe: a wash, not a solid bar.
            Color wash = look.Deep;
            wash.a = 0.82f;
            Fill(header, "Wash", wash);

            // Two currency pills, one shape, one size.
            BuildPill(header, "GoldPill", 10f, 14f, 300f, look, font, "ui_icon_gold.png", "12.4K", false);
            BuildPill(header, "GemPill", 322f, 14f, 300f, look, font, "ui_icon_gem.png", "421", false);

            // Menu: a small square, not a third currency.
            RectTransform menu = OutlineBox(header, "Menu", 922f, 14f, 120f, 72f,
                look.BorderColor, look.PanelLight, look.Border);
            PixelText(menu, "Label", "MENU", look.TitleSize, TextAlignmentOptions.Center, look.Text, font);
        }

        private static void BuildPill(RectTransform parent, string name, float x, float y, float width,
            Look look, TMP_FontAsset font, string iconFile, string value, bool accent)
        {
            RectTransform pill = OutlineBox(parent, name, x, y, width, 72f,
                look.BorderColor, accent ? look.Accent : look.PanelLight, look.Border);

            Image iconImage = UiFactory.Icon("Icon", pill, Color.white, true);
            iconImage.sprite = LoadSprite(PlaceholderArt + iconFile);
            Place(iconImage.rectTransform, 14f, 14f, 44f, 44f);

            PixelText(Place(pill, "Value", 68f, 0f, width - 80f, 72f), "ValueText", value, look.NumberSize,
                TextAlignmentOptions.MidlineLeft, accent ? look.InkOnAccent : look.Text, font);
        }

        private static void BuildStageRibbon(RectTransform root, Look look, TMP_FontAsset font)
        {
            const float ribbonWidth = 460f;
            float x = (ScreenWidth - ribbonWidth) * 0.5f;

            RectTransform ribbon = OutlineBox(root, "StageRibbon", x, StageTop, ribbonWidth, StageHeight,
                look.BorderColor, look.Panel, look.Border);

            if (look.TopEdgeGlow)
            {
                // Arcade: a bright top edge - the "screen bezel" tell.
                Fill(Box(ribbon, "TopEdge", 0f, 0f, ribbonWidth, 3f), "Glow", look.Accent);
            }

            PixelText(ribbon, "Label", "STAGE 12   .   WAVE 3", look.LabelSize,
                TextAlignmentOptions.Center, look.Text, font);
        }

        private static void BuildArena(RectTransform root, Look look, TMP_FontAsset font)
        {
            RectTransform arena = OutlineBox(root, "Arena", 14f, ArenaTop, 1052f, ArenaHeight,
                look.BorderColor, look.Deep, look.Border);

            // The picture: the real forest backdrop, tinted to the look.
            Image backdrop = UiFactory.Icon("Backdrop", arena, look.BackdropTint, false);
            backdrop.sprite = LoadSprite(BackdropPath);
            Stretch(backdrop.rectTransform);

            float arenaWidth = arena.rect.width;
            float arenaHeight = arena.rect.height;

            // A dark seat at the top and bottom, so figures stand IN the frame instead of on it.
            Fill(Box(arena, "SeatTop", 0f, 0f, arenaWidth, 90f), "Shade", new Color(0f, 0f, 0f, 0.30f));
            Fill(Box(arena, "SeatBottom", 0f, arenaHeight - 140f, arenaWidth, 140f), "Shade",
                new Color(0f, 0f, 0f, 0.42f));

            if (look.CornerTicks)
            {
                // Storybook: four amber corner ticks, the "framed print" tell.
                CornerTick(arena, "TickTL", 12f, 12f, look.Accent, false, false);
                CornerTick(arena, "TickTR", arenaWidth - 52f, 12f, look.Accent, true, false);
                CornerTick(arena, "TickBL", 12f, arenaHeight - 52f, look.Accent, false, true);
                CornerTick(arena, "TickBR", arenaWidth - 52f, arenaHeight - 52f, look.Accent, true, true);
            }

            BuildHeroSlot(arena, "Hero1", 40f, 250f, "Knight", "Knight_Idle_0", 0.78f, look, font);
            BuildHeroSlot(arena, "Hero2", 40f, 440f, "Soldier", "Soldier_Idle_0", 0.62f, look, font);
            BuildHeroSlot(arena, "Hero3", 40f, 630f, "Mage", "Wizard_Idle_0", 0.45f, look, font);

            BuildEnemySlot(arena, "Enemy1", 660f, 250f, "Werewolf A", 0.55f, look, font);
            BuildEnemySlot(arena, "Enemy2", 660f, 440f, "Bat B", 0.30f, look, font);
            BuildEnemySlot(arena, "Enemy3", 660f, 630f, "Bat C", 0.85f, look, font);

            // One damage number, so the type and colour show.
            PixelText(Place(arena, "Damage", 560f, 190f, 180f, 60f), "Value", "128", look.NumberSize,
                TextAlignmentOptions.MidlineRight, look.AccentLight, font);
        }

        private static void CornerTick(RectTransform arena, string name, float x, float y, Color color,
            bool right, bool bottom)
        {
            RectTransform tick = Box(arena, name, x, y, 40f, 40f);

            Fill(Box(tick, "H", 0f, bottom ? 34f : 0f, 40f, 6f), "Fill", color);
            Fill(Box(tick, "V", right ? 34f : 0f, 0f, 6f, 40f), "Fill", color);
        }

        private static void BuildHeroSlot(RectTransform arena, string name, float x, float y, string label,
            string spriteName, float hpFraction, Look look, TMP_FontAsset font)
        {
            RectTransform slot = Box(arena, name, x, y, 340f, 160f);

            Image icon = UiFactory.Icon("Icon", slot, Color.white, true);
            icon.sprite = LoadSpriteByName(spriteName);
            Place(icon.rectTransform, 0f, 10f, 112f, 140f);

            PixelText(Place(slot, "Name", 122f, 34f, 200f, 44f), "Text", label,
                look.LabelSize, TextAlignmentOptions.MidlineLeft, look.Text, font);

            BuildBar(slot, 122f, 92f, 190f, 16f, hpFraction, look.Hero, look);
        }

        private static void BuildEnemySlot(RectTransform arena, string name, float x, float y, string label,
            float hpFraction, Look look, TMP_FontAsset font)
        {
            RectTransform slot = Box(arena, name, x, y, 340f, 160f);

            // A flat silhouette on purpose. Enemy sheets are full charts, not single frames, so a stand-in block
            // reads better here than a cropped sheet with a white margin.
            Image body = UiFactory.Icon("Body", slot, Color.Lerp(look.Deep, look.Enemy, 0.30f), false);
            Place(body.rectTransform, 0f, 10f, 112f, 140f);
            Fill(Box(slot, "BodyTrim", 0f, 10f, 112f, 6f), "Trim", look.Enemy);

            PixelText(Place(slot, "Name", 122f, 34f, 200f, 44f), "Text", label,
                look.LabelSize, TextAlignmentOptions.MidlineLeft, look.Text, font);

            BuildBar(slot, 122f, 92f, 190f, 16f, hpFraction, look.Enemy, look);
        }

        private static void BuildBar(RectTransform parent, float x, float y, float width, float height,
            float fraction, Color fillColor, Look look)
        {
            RectTransform track = Box(parent, "Bar", x, y, width, height);
            Fill(track, "Track", look.Deep);
            Fill(Box(track, "Fill", 0f, 0f, Mathf.Max(2f, width * Mathf.Clamp01(fraction)), height), "Ink", fillColor);
        }

        private static void BuildXpBar(RectTransform root, Look look, TMP_FontAsset font)
        {
            RectTransform xp = Box(root, "XpBar", 14f, XpTop, 1052f, XpHeight);

            // Level badge first: the reward should be readable at a glance.
            RectTransform badge = OutlineBox(xp, "Level", 0f, 0f, 132f, XpHeight,
                look.BorderColor, look.PanelLight, look.Border);
            PixelText(badge, "Text", "LV 9", look.LabelSize, TextAlignmentOptions.Center, look.Text, font);

            RectTransform track = OutlineBox(xp, "Track", 144f, 6f, 908f, XpHeight - 12f,
                look.BorderColor, look.Panel, look.Border);

            if (look.SegmentedBar)
            {
                // Storybook: ten leather segments, seven filled. A bar you can count.
                for (int i = 0; i < 10; i++)
                {
                    Color segment = i < 7 ? look.Accent : look.Deep;
                    Fill(Box(track, "Seg" + i, 6f + (i * 89f), 5f, 80f, XpHeight - 22f), "Fill", segment);
                }
            }
            else
            {
                Fill(Box(track, "Fill", 6f, 5f, 640f, XpHeight - 22f), "Fill", look.Accent);
            }
        }

        private static void BuildActions(RectTransform root, Look look, TMP_FontAsset font)
        {
            RectTransform strip = Box(root, "Actions", 14f, ActionsTop, 1052f, ActionsHeight);

            string[] names = { "Knight", "Soldier", "Mage" };
            string[] abilities = { "Guard", "Holy Slash", "Shatter" };
            string[] sprites = { "Knight_Idle_0", "Soldier_Idle_0", "Wizard_Idle_0" };
            float[] cool = { 0f, 0.45f, 0.15f };

            for (int i = 0; i < 3; i++)
            {
                float x = 8f + (i * 350f);
                RectTransform button = OutlineBox(strip, "Action" + i, x, 10f, 336f, 176f,
                    look.BorderColor, look.Panel, look.Border);

                Image portrait = UiFactory.Icon("Portrait", button, Color.white, true);
                portrait.sprite = LoadSpriteByName(sprites[i]);
                Place(portrait.rectTransform, 14f, 16f, 96f, 112f);

                PixelText(Place(button, "Name", 122f, 14f, 200f, 44f), "Text", names[i],
                    look.LabelSize, TextAlignmentOptions.MidlineLeft, look.Text, font);

                PixelText(Place(button, "Ability", 122f, 56f, 200f, 44f), "Text", abilities[i],
                    look.LabelSize, TextAlignmentOptions.MidlineLeft, look.Dim, font);

                bool ready = cool[i] <= 0f;
                PixelText(Place(button, "Clock", 122f, 100f, 200f, 44f), "Text", ready ? "READY" : "COOLING",
                    look.TitleSize, TextAlignmentOptions.MidlineLeft, ready ? look.Accent : look.Dim, font);

                // The cooldown strip: full when ready, a mouthful when not.
                float fill = ready ? 1f : (1f - cool[i]);
                Fill(Box(button, "CoolTrack", 8f, 160f, 320f, 8f), "Track", look.Deep);
                if (fill > 0f)
                {
                    Fill(Box(button, "CoolFill", 8f, 160f, 320f * fill, 8f), "Fill",
                        ready ? look.Accent : look.AccentLight);
                }
            }
        }

        private static void BuildFeed(RectTransform root, Look look, TMP_FontAsset font)
        {
            RectTransform feed = OutlineBox(root, "Feed", 14f, LogTop, 1052f, LogHeight,
                look.BorderColor, look.Deep, look.Border);

            string[] lines =
            {
                "-- Wave 6: Werewolf x2, Bat --",
                "Knight hits Werewolf A for 12.4K",
                "Mage CRITS Werewolf A for 21.7K",
                "Werewolf A hits Knight for 3.2K",
                "Bat B defeated  +3 gold",
                "Soldier hits Werewolf A for 8.1K",
                "Stage 12 wave 6 cleared"
            };

            bool[] hot = { true, false, true, false, true, false, false };
            bool[] dim = { false, false, false, true, false, false, true };

            float lineHeight = look.LogSize + 14f;

            for (int i = 0; i < lines.Length; i++)
            {
                Color color = hot[i] ? look.Accent : (dim[i] ? look.Dim : look.Text);
                PixelText(Place(feed, "Line" + i, 16f, 16f + (i * lineHeight), 1020f, lineHeight),
                    "Text", lines[i], look.LogSize, TextAlignmentOptions.MidlineLeft, color, font);
            }
        }

        // ------------------------------------------------------------------
        // Building blocks
        // ------------------------------------------------------------------

        /// <summary>A fixed-size box, top-left anchored inside its parent.</summary>
        private static RectTransform Box(Transform parent, string name, float x, float y, float width, float height)
        {
            RectTransform rect = UiFactory.Node(name, parent).GetComponent<RectTransform>();
            return Place(rect, x, y, width, height);
        }

        private static RectTransform Place(Transform parent, string name, float x, float y, float width, float height)
        {
            return Box(parent, name, x, y, width, height);
        }

        /// <summary>Positions an existing rect: top-left anchored, (x, y) from the parent's top-left.</summary>
        private static RectTransform Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        /// <summary>A flat colour child that fills its parent.</summary>
        private static Image Fill(Transform parent, string name, Color color)
        {
            Image image = UiFactory.Icon(name, parent, color, false);
            Stretch(image.rectTransform);
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// The ink-border trick, with no new art: a dark box, then the face inset by the border width. It is the one
        /// shape rule every look shares, and it shows at a glance how thick each direction wants its edges.
        /// </summary>
        private static RectTransform OutlineBox(Transform parent, string name, float x, float y, float width,
            float height, Color borderColor, Color faceColor, float borderPx)
        {
            RectTransform outer = Box(parent, name, x, y, width, height);
            Fill(outer, "Border", borderColor);

            RectTransform face = Box(outer, "Face", borderPx, borderPx, width - (borderPx * 2f),
                height - (borderPx * 2f));
            Fill(face, "Fill", faceColor);
            return face;
        }

        /// <summary>TMP text stretched into its parent, on the look's font, no auto-size, no wrap.</summary>
        private static TextMeshProUGUI PixelText(Transform parent, string name, string content, float size,
            TextAlignmentOptions alignment, Color color, TMP_FontAsset font)
        {
            TextMeshProUGUI text = UiFactory.RawText(name, parent, content, size, alignment, color);

            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }

            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            Stretch(text.rectTransform);
            return text;
        }

        // ------------------------------------------------------------------
        // Capture
        // ------------------------------------------------------------------

        [MenuItem("Tools/Idle RPG/UI/Capture Compare PNGs", priority = 31)]
        public static void CaptureComparePngs()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();

            if (canvas == null || canvas.worldCamera == null)
            {
                Debug.LogError("[UiCompare] No canvas with a camera. Open Scenes/UiCompare.unity first.");
                return;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            Transform rowTransform = canvas.transform.Find("Row");
            RectTransform row = rowTransform as RectTransform;

            if (scaler == null || row == null)
            {
                Debug.LogError("[UiCompare] The scene does not look like the compare lab. Rebuild it.");
                return;
            }

            EnsureFolder(ShotFolder);

            int width = (int)ScreenWidth;
            int height = (int)ScreenHeight;
            string[] paths = new string[Looks.Length];

            // 1:1 capture: one canvas pixel is one screen pixel, and the row slides so each look fills the frame.
            Vector2 savedPosition = row.anchoredPosition;
            CanvasScaler.ScaleMode savedMode = scaler.uiScaleMode;
            float savedFactor = scaler.scaleFactor;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            for (int i = 0; i < Looks.Length; i++)
            {
                row.anchoredPosition = new Vector2((1 - i) * (ScreenWidth + ScreenGutter), 0f);
                paths[i] = ShotFolder + "/compare-" + Looks[i].Letter + ".png";
                RenderToPng(canvas.worldCamera, width, height, paths[i]);
            }

            row.anchoredPosition = savedPosition;
            scaler.uiScaleMode = savedMode;
            scaler.scaleFactor = savedFactor;
            Canvas.ForceUpdateCanvases();

            WriteContactSheet(paths, width, height);
            AssetDatabase.Refresh();

            Debug.Log("[UiCompare] Wrote " + Looks.Length + " screens and compare-ABC.png to " + ShotFolder + "/.");
        }

        private static void RenderToPng(Camera camera, int width, int height, string path)
        {
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);

            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;

            camera.targetTexture = null;

            File.WriteAllBytes(path, image.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
        }

        /// <summary>Three screens in one wide picture with dark gutters, so they can be judged together.</summary>
        private static void WriteContactSheet(string[] paths, int width, int height)
        {
            int sheetWidth = (int)RowWidth;
            Texture2D sheet = new Texture2D(sheetWidth, height, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[sheetWidth * height];
            Color32 gutter = new Color32(10, 9, 8, 255);

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = gutter;
            }

            for (int i = 0; i < paths.Length; i++)
            {
                Texture2D screen = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                screen.LoadImage(File.ReadAllBytes(paths[i]));
                Color32[] source = screen.GetPixels32();
                int offsetX = i * (width + (int)ScreenGutter);

                for (int y = 0; y < height; y++)
                {
                    System.Array.Copy(source, y * width, pixels, (y * sheetWidth) + offsetX, width);
                }

                Object.DestroyImmediate(screen);
            }

            sheet.SetPixels32(pixels);
            sheet.Apply();

            string path = ShotFolder + "/compare-ABC.png";
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            Object.DestroyImmediate(sheet);
        }

        private static TMP_FontAsset LoadFont(string fontName)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontRoot + fontName + ".asset");

            if (font == null)
            {
                Debug.LogWarning("[UiCompare] Missing font '" + fontName + "'. " +
                                 "Run Tools > Idle RPG > Art > Generate Pixel Fonts.");
            }

            return font;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite == null)
            {
                Debug.LogWarning("[UiCompare] Missing sprite at " + path + ".");
            }

            return sprite;
        }

        private static Sprite LoadSpriteByName(string spriteName)
        {
            string[] guids = AssetDatabase.FindAssets(spriteName + " t:Sprite");

            foreach (string guid in guids)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));

                if (sprite != null && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            Debug.LogWarning("[UiCompare] No sprite named '" + spriteName + "'; the slot stays blank.");
            return null;
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("CompareCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(10, 9, 8, 255);
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
            scaler.referenceResolution = new Vector2(ScreenWidth, ScreenHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            return canvasObject.GetComponent<RectTransform>();
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

        private static Color Hex(string hex)
        {
            Color color;
            return ColorUtility.TryParseHtmlString("#" + hex, out color) ? color : Color.magenta;
        }
    }
}
