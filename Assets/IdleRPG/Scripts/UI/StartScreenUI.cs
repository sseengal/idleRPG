using IdleRPG.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// Runtime-built start screen (lobby): gem/gold bar, a placeholder chapter carousel, START, and the five
    /// sections (SHOP / PARTY / BATTLE / ASCENSION / TRAINING) with BATTLE highlighted. Shown at boot with the game
    /// paused; START resumes and begins a run; a small MENU button reopens it. Placeholder panels for now.
    /// </summary>
    public sealed partial class StartScreenUI : MonoBehaviour
    {
        public static StartScreenUI Instance { get; private set; }

        private static readonly string[] Tabs = { "SHOP", "PARTY", "BATTLE", "ASCEND", "TRAIN" };

        private GameManager manager;
        private ScreenController screens;
        private GameObject lobby;
        private GameObject menuButton;

        [Tooltip("Chapter art for the lobby centre panel. Null falls back to the plain plate.")]
        [SerializeField] private Sprite chapterArt;

        [Tooltip("Chapter title wordmark. Null keeps the text title.")]
        [SerializeField] private Sprite chapterTitleArt;

        [Header("Tab icons (Button Icons.png)")]
        [SerializeField] private Sprite tabShopIcon;
        [SerializeField] private Sprite tabPartyIcon;
        [SerializeField] private Sprite tabBattleIcon;
        [SerializeField] private Sprite tabAscendIcon;
        [SerializeField] private Sprite tabTrainIcon;

        // Bottom nav: one rich tab per section so the open page can be highlighted + effects driven.
        private readonly System.Collections.Generic.Dictionary<string, FantasyTabButton> navTabs =
            new System.Collections.Generic.Dictionary<string, FantasyTabButton>();

        // The shared battle header (HudHeaderUI), borrowed while the lobby is open, put back when a run starts.
        private RectTransform sharedHeader;
        private Transform sharedHeaderHome;
        private Vector2 sharedHeaderAnchorMin;
        private Vector2 sharedHeaderAnchorMax;
        private Vector2 sharedHeaderOffsetMin;
        private Vector2 sharedHeaderOffsetMax;
        private int sharedHeaderSibling;

        private GameObject trainingPanel;

        // ---- Training tree (implementation lives in StartScreenUI.Training.cs) ----
        private RectTransform treeContent;
        private ScrollRect treeScroll;
        private bool treeBuilt;
        private float treeContentHeight;
        private float treeRowH = 220f;
        private float treeTopPad = 80f;
        private const float NodeSize = 144f;
        private const float TreeHeaderFraction = 0.896f;   // scroll sits under the pinned column headers
        private static readonly float[] ColumnX = { 129f, 388f, 648f, 907f };

        private readonly System.Collections.Generic.List<TreeView> treeViews =
            new System.Collections.Generic.List<TreeView>();
        private readonly System.Collections.Generic.List<TreeColumn> treeColumns =
            new System.Collections.Generic.List<TreeColumn>();

        private struct TreeView
        {
            public IdleRPG.Data.TrainingNode Node;
            public Image Diamond;
            public UiPlateButton Plate;   // shared press/disabled behaviour on the diamond node
            public Image Connector;   // line ABOVE this node (to the one above it)
            public Image Badge;
            public Image Ring;        // gold rim, shown once the node is owned
            public RectTransform Rect;
            public int Column;
        }

        private struct TreeColumn
        {
            public string HeroId;
            public int FrontierOrder;
        }

        private GameObject treePopup;
        private TextMeshProUGUI popupTitle;
        private TextMeshProUGUI popupBody;
        private UiPlateButton popupBuy;
        private Image popupCoin;
        private Image popupStar;
        private IdleRPG.Data.TrainingNode popupNode;

        private void Awake()
        {
            Instance = this;
            Build();
        }

        private void Start()
        {
            EnsureManager();
            AttachSharedHeader();
            SetGamePaused(true);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Build()
        {
            Canvas lobbyCanvas = MakeCanvas("StartScreenCanvas", 250, true);
            lobby = lobbyCanvas.gameObject;

            Atmosphere(lobby.transform, "BG", Vector2.zero, Vector2.one, VerticalGradient, Color.white);
            Atmosphere(lobby.transform, "Vignette", Vector2.zero, Vector2.one, Vignette, Color.white);
            Atmosphere(lobby.transform, "ChapterGlow", new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.70f), SoftGlow, UiTheme.HaloGreen);

            Image card = chapterArt != null
                ? ArtPanel(lobby.transform, "ChapterCard", new Vector2(0.10f, 0.4144f), new Vector2(0.90f, 0.66f), chapterArt)
                : Panel(lobby.transform, "ChapterCard", new Vector2(0.10f, 0.38f), new Vector2(0.90f, 0.66f), UiTheme.NavIdle, Frame);

            // Chapter title: small faded "Chapter 1" over a big "Greenwake". Rect centres sit ~0.02 apart
            // so the two read as ONE tight kicker, and hug the panel top (proximity grouping).
            Text(lobby.transform, "ChapterLabel", new Vector2(0.05f, 0.717f), new Vector2(0.95f, 0.737f), "Chapter 1", UiType.Role.Small, UiTheme.TextDim);
            if (chapterTitleArt != null)
            {
                // Wordmark art (transparent, centred); sized to its aspect so the letters read clean.
                ArtPanel(lobby.transform, "ChapterTitle", new Vector2(0.32f, 0.6361f), new Vector2(0.68f, 0.7465f), chapterTitleArt);
            }
            else
            {
                Text(lobby.transform, "ChapterTitle", new Vector2(0.05f, 0.669f), new Vector2(0.95f, 0.701f), "Greenwake", UiType.Role.Title, UiTheme.Text);
            }

            Atmosphere(lobby.transform, "StartGlow", new Vector2(0.14f, 0.21f), new Vector2(0.86f, 0.39f), SoftGlow, UiTheme.HaloGold);
            Btn(lobby.transform, "StartButton", new Vector2(0.20f, 0.26f), new Vector2(0.80f, 0.34f), "START", UiTheme.Gold, UiType.Role.Title, StartGame);

            for (int i = 0; i < Tabs.Length; i++)
            {
                float width = 1f / Tabs.Length;
                float xMin = i * width;
                string tab = Tabs[i];
                navTabs[tab] = BuildFantasyTab(tab, new Vector2(xMin, 0.03f), new Vector2(xMin + width, 0.14f), () => OnTab(tab));
            }

            HighlightNav("BATTLE");

            Canvas menuCanvas = MakeCanvas("MiniMenuCanvas", 251, true);
            menuButton = menuCanvas.gameObject;
            Btn(menuCanvas.transform, "MenuButton", new Vector2(0.80f, 0.90f), new Vector2(0.97f, 0.975f), "MENU", UiTheme.FaceDim, UiType.Role.Small, ShowLobby);
            menuButton.SetActive(false);
        }

        /// <summary>Open the lobby and pause the run (used by the MENU button and the F1 debug key).</summary>
        public void ShowLobby()
        {
            if (lobby != null)
            {
                lobby.SetActive(true);
            }

            if (menuButton != null)
            {
                menuButton.SetActive(false);
            }

            AttachSharedHeader();
            HighlightNav("BATTLE");
            RaiseNav();
            SetGamePaused(true);
        }

        private void StartGame()
        {
            EnsureManager();

            if (lobby != null)
            {
                lobby.SetActive(false);
            }

            if (menuButton != null)
            {
                menuButton.SetActive(true);
            }

            HideRunNav();
            DetachSharedHeader();
            SetGamePaused(false);

            if (manager != null)
            {
                manager.StartRun(1);
            }
        }

        private void OnTab(string tab)
        {
            HighlightNav(tab);

            // One page at a time: close every lobby page first, then open the chosen one.
            HideTraining();
            HideParty();
            HidePerks();

            switch (tab)
            {
                case "TRAIN":
                    ShowTraining();
                    RaiseNav();
                    return;

                case "PARTY":
                    ShowParty();
                    RaiseNav();
                    return;

                case "ASCEND":
                    ShowPerks();
                    RaiseNav();
                    return;
            }

        }

        private void HighlightNav(string tab)
        {
            foreach (System.Collections.Generic.KeyValuePair<string, FantasyTabButton> pair in navTabs)
            {
                if (pair.Value != null)
                {
                    pair.Value.SetSelected(pair.Key == tab);
                }
            }
        }

        /// <summary>Keeps the bottom bar above whatever page panel was just opened.</summary>
        private void RaiseNav()
        {
            foreach (System.Collections.Generic.KeyValuePair<string, FantasyTabButton> pair in navTabs)
            {
                if (pair.Value != null && pair.Value.transform.parent != null)
                {
                    pair.Value.transform.parent.SetAsLastSibling();
                }
            }
        }

        /// <summary>Builds one bottom tab; the view owns the four states, glow and UIEffect.</summary>
        private FantasyTabButton BuildFantasyTab(string tab, Vector2 aMin, Vector2 aMax,
            UnityEngine.Events.UnityAction onClick)
        {
            RectTransform tabRoot = Rect("Nav" + tab, lobby.transform, aMin, aMax);

            RectTransform glowRect = Rect("Glow", tabRoot, Vector2.zero, Vector2.one);
            glowRect.offsetMin = new Vector2(-16f, -16f);
            glowRect.offsetMax = new Vector2(16f, 16f);
            Image glow = glowRect.gameObject.AddComponent<Image>();
            glow.raycastTarget = false;

            RectTransform frameRect = Rect("Frame", tabRoot, Vector2.zero, Vector2.one);
            Image frame = frameRect.gameObject.AddComponent<Image>();
            frame.sprite = Frame;
            frame.type = Image.Type.Sliced;
            frame.color = UiTheme.NavIdle;
            frame.raycastTarget = true;

            Button button = frameRect.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(onClick);

            RectTransform iconRect = Rect("Icon", frameRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(100f, 100f);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = TabIcon(tab);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Color iconColor = icon.color;
            iconColor.a = icon.sprite != null ? 1f : 0f;
            icon.color = iconColor;

            TextMeshProUGUI text = Text(frameRect, "Label", new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.32f), tab,
                UiType.Role.Section, UiTheme.Text);
            UiType.Apply(text, UiType.Role.Section, 36f);   // slightly under Section (40) for a tighter bar
            text.raycastTarget = false;

            Coffee.UIEffects.UIEffect effect = frameRect.gameObject.AddComponent<Coffee.UIEffects.UIEffect>();

            FantasyTabButton view = frameRect.gameObject.AddComponent<FantasyTabButton>();
            view.Bind(tabRoot, frame, glow, text, effect, button,
                UiTheme.NavIdle, UiTheme.NavSelected, UiTheme.TextMuted, Color.white, icon, TabIconScale(tab),
                TabIconSelectedScale(tab), TabIconVertical(tab), TabIconSelectedVertical(tab));
            return view;
        }

        /// <summary>Idle height nudge (selected uses TabIconSelectedScale).</summary>
        private static float TabIconScale(string tab)
        {
            switch (tab)
            {
                case "PARTY": return 0.97f;
                case "ASCEND": return 0.94f;
                case "TRAIN": return 0.94f;
                default: return 1f;
            }
        }

        /// <summary>Selected height nudge. The swords read small beside the word, so it gets a little more.</summary>
        private static float TabIconSelectedScale(string tab)
        {
            switch (tab)
            {
                case "PARTY": return 1.03f;
                case "BATTLE": return 1.10f;
                default: return 1f;
            }
        }

        /// <summary>Idle vertical nudge (fraction of icon height) to centre an off-balance silhouette.</summary>
        private static float TabIconVertical(string tab)
        {
            switch (tab)
            {
                case "BATTLE": return 0.07f;
                case "ASCEND": return 0.03f;
                case "TRAIN": return 0.03f;
                default: return 0f;
            }
        }

        /// <summary>Selected vertical nudge (fraction of icon height) - lifts a low icon above its word.</summary>
        private static float TabIconSelectedVertical(string tab)
        {
            return tab == "TRAIN" ? 0.07f : 0f;
        }

        /// <summary>The sprite for a tab icon; null if the sheet has not been wired yet.</summary>
        private Sprite TabIcon(string tab)
        {
            switch (tab)
            {
                case "SHOP": return tabShopIcon;
                case "PARTY": return tabPartyIcon;
                case "BATTLE": return tabBattleIcon;
                case "ASCEND": return tabAscendIcon;
                case "TRAIN": return tabTrainIcon;
                default: return null;
            }
        }



        // ------------------------------------------------------------------
        // Shared header: the battle page's HudHeaderUI, borrowed while the lobby is open.
        // Reparented, never rebuilt - so both pages show one header and one source of truth.
        // ------------------------------------------------------------------
        private void AttachSharedHeader()
        {
            if (sharedHeader != null)
            {
                return;
            }

            HudHeaderUI header = HudHeaderUI.Instance;
            RectTransform headerRect = header != null ? header.transform as RectTransform : null;

            if (headerRect == null || lobby == null)
            {
                return;
            }

            sharedHeader = headerRect;
            sharedHeaderHome = headerRect.parent;
            sharedHeaderAnchorMin = headerRect.anchorMin;
            sharedHeaderAnchorMax = headerRect.anchorMax;
            sharedHeaderOffsetMin = headerRect.offsetMin;
            sharedHeaderOffsetMax = headerRect.offsetMax;
            sharedHeaderSibling = headerRect.GetSiblingIndex();

            headerRect.SetParent(lobby.transform, false);
            headerRect.anchorMin = new Vector2(0.03f, 0.90f);
            headerRect.anchorMax = new Vector2(0.97f, 0.985f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
            headerRect.SetAsLastSibling();
        }

        private void DetachSharedHeader()
        {
            if (sharedHeader == null || sharedHeaderHome == null)
            {
                return;
            }

            sharedHeader.SetParent(sharedHeaderHome, false);
            sharedHeader.anchorMin = sharedHeaderAnchorMin;
            sharedHeader.anchorMax = sharedHeaderAnchorMax;
            sharedHeader.offsetMin = sharedHeaderOffsetMin;
            sharedHeader.offsetMax = sharedHeaderOffsetMax;
            sharedHeader.SetSiblingIndex(sharedHeaderSibling);
            sharedHeader = null;
        }

        private void EnsureManager()
        {
            if (manager == null)
            {
                HudController hud = HudController.Instance;
                manager = hud != null ? hud.GameManager : null;
            }

            if (screens == null)
            {
                screens = FindAnyObjectByType<ScreenController>();
            }
        }

        private void HideRunNav()
        {
            if (screens != null)
            {
                screens.gameObject.SetActive(false);
            }
        }

        private void SetGamePaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
        }

        // ------------------------------------------------------------------
        // Tiny runtime UI factory (no editor tooling needed)
        // ------------------------------------------------------------------
        private static Canvas MakeCanvas(string name, int order, bool raycaster)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            if (raycaster)
            {
                go.AddComponent<GraphicRaycaster>();
            }

            return canvas;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>The tintable bevel frame, loaded once and reused by every lobby panel and button.</summary>
        private static Sprite frameSprite;

        private static Sprite Frame
        {
            get
            {
                if (frameSprite == null)
                {
                    frameSprite = Resources.Load<Sprite>("ui_frame");
                }

                return frameSprite;
            }
        }

        // Atmosphere sprites are built in code and cached: no new art, one tiny gradient + two small radial
        // textures. They add depth (light + shadow) without touching the chapter art or the START button.
        private static Sprite gradientSprite;
        private static Sprite glowSprite;
        private static Sprite vignetteSprite;

        private static Sprite VerticalGradient
        {
            get
            {
                if (gradientSprite != null) { return gradientSprite; }

                const int height = 256;
                Texture2D tex = new Texture2D(1, height, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;

                Color bottom = UiTheme.AtmosphereBottom;
                Color mid = UiTheme.AtmosphereMid;
                Color top = UiTheme.AtmosphereTop;

                for (int y = 0; y < height; y++)
                {
                    float t = y / (float)(height - 1);
                    Color c = t < 0.55f
                        ? Color.Lerp(bottom, mid, t / 0.55f)
                        : Color.Lerp(mid, top, (t - 0.55f) / 0.45f);
                    tex.SetPixel(0, y, c);
                }

                tex.Apply();
                gradientSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, height), new Vector2(0.5f, 0.5f), 100f);
                gradientSprite.name = "UiVerticalGradient";
                return gradientSprite;
            }
        }

        private static Sprite SoftGlow
        {
            get
            {
                if (glowSprite != null) { return glowSprite; }

                const int size = 128;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                Vector2 centre = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
                float maxR = size * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), centre) / maxR;
                        float a = Mathf.Clamp01(1f - d);
                        a *= a;
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }

                tex.Apply();
                glowSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
                glowSprite.name = "UiSoftGlow";
                return glowSprite;
            }
        }

        private static Sprite Vignette
        {
            get
            {
                if (vignetteSprite != null) { return vignetteSprite; }

                const int size = 256;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                Vector2 centre = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
                float maxR = size * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), centre) / maxR);
                        float a = Mathf.SmoothStep(0.35f, 1f, d);
                        tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
                    }
                }

                tex.Apply();
                vignetteSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
                vignetteSprite.name = "UiVignette";
                return vignetteSprite;
            }
        }

        /// <summary>A non-interactive, stretched sprite used for the background, vignette and glows.</summary>
        private static Image Atmosphere(Transform parent, string name, Vector2 aMin, Vector2 aMax, Sprite sprite, Color color)
        {
            RectTransform rt = Rect(name, parent, aMin, aMax);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.preserveAspect = false;
            img.raycastTarget = false;
            return img;
        }

        private static Image Panel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Color color,
            Sprite sprite = null)
        {
            RectTransform rt = Rect(name, parent, aMin, aMax);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;

            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }

            return img;
        }

        /// <summary>An illustration that keeps its own aspect (the chapter art ships with its wooden border).</summary>
        private static Image ArtPanel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Sprite art)
        {
            RectTransform rt = Rect(name, parent, aMin, aMax);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = art;
            img.color = Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, Vector2 aMin, Vector2 aMax, string text,
            UiType.Role role, Color color)
        {
            RectTransform rt = Rect(name, parent, aMin, aMax);
            TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();

            UiType.Apply(t, role);
            t.text = text;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            return t;
        }

        private static UiPlateButton Btn(Transform parent, string name, Vector2 aMin, Vector2 aMax, string label, Color bg,
            UiType.Role role, UnityEngine.Events.UnityAction onClick)
        {
            // One shared button: plate face, press squash, grayscale disable. idle == active keeps the face
            // steady for plain action buttons; the tabs are the ones that change colour when chosen.
            return UiPlateButton.Create(parent, name, aMin, aMax, label, role, bg, bg, onClick);
        }
    }
}
