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
        private GameObject lobby;

        // The box the lobby's centre and tab bar live in (owner, 2026-11-25). The notch and the home bar cut this box,
        // so the content moves instead of hiding under them.
        private RectTransform lobbySafe;

        // ---- The lobby centre (chapter kicker, wordmark, card, START) ----
        // Sizes are fixed canvas units at the 1080 reference, so the block looks the same on every phone and only the
        // air above and below it grows. The two art aspects keep their pictures un-letterboxed.
        private const float ChapterArtAspect = 1400f / 764f;      // Greenwake1-clean 1.png
        private const float TitleArtAspect = 1258f / 208f;        // greenwake_title.png, cropped to its letters

        /// <summary>
        /// The chapter card's width. Narrower than the page column on purpose: the 76-unit gutter each side is where
        /// the carousel's left and right arrows will live once there is a second chapter (owner, 2026-11-25). START
        /// shares the card's edges, so the picture and the action read as one unit.
        /// </summary>
        private const float ChapterCardWidth = 864f;

        /// <summary>The wordmark's width: 70% of the card, which prints its letters about 40 dp tall.</summary>
        private const float ChapterTitleWidth = 605f;

        /// <summary>
        /// START's width: three quarters of the card. Narrower than the picture, so the action reads as a button under
        /// the hero instead of a second slab (owner, 2026-11-25).
        /// </summary>
        private const float LobbyActionWidth = 648f;

        /// <summary>Letter spacing on the kicker, so a two-word label reads as a label (TMP units).</summary>
        private const float KickerTracking = 8f;

        /// <summary>The heavy-rim font variant (outline 0.3, the shared one is 0.2), inside a Resources folder.</summary>
        private const string HeavyOutlineMaterialPath = "Fonts/Lilita One Heavy";

        /// <summary>START's height: 61 dp, comfortable for a thumb.</summary>
        private const float LobbyActionHeight = 154f;

        private const float TabBandTopFraction = 0.14f;           // the tab bar's top edge, as a fraction of the box
        private const float TabBandBottomFraction = 0.03f;

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

        [Tooltip("Icons for the training tree nodes. Null falls back to the letter badges.")]
        [SerializeField] private IdleRPG.Data.TrainingIconSet trainingIcons;

        // Bottom nav: one rich tab per section so the open page can be highlighted + effects driven.
        private readonly System.Collections.Generic.Dictionary<string, FantasyTabButton> navTabs =
            new System.Collections.Generic.Dictionary<string, FantasyTabButton>();

        // The shared battle header (HudHeaderUI), borrowed while the lobby is open, put back when a run starts.
        private RectTransform sharedHeader;
        private Transform sharedHeaderHome;
        private int sharedHeaderSibling;
        private GameObject sharedHeaderBox;

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
            public Image Glow;        // affordability halo behind the medallion
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

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        /// <summary>
        /// The lobby is tied to the run state, not to a button. Lobby = between runs, so it shows. Any live
        /// fight hides it. That is what makes "no upgrades during a fight" a rule instead of a promise.
        /// </summary>
        private void OnGameStateChanged(GameState previous, GameState next)
        {
            if (next == GameState.Lobby)
            {
                ShowLobby();
            }
            else if (next == GameState.Combat || next == GameState.Boss)
            {
                HideLobby();
            }
        }

        private void Start()
        {
            EnsureManager();

            // A run in progress goes straight back to the fight. The lobby only exists between runs.
            if (manager != null && manager.LoadedFromSave && manager.RunIsActive)
            {
                HideLobby();
                return;
            }

            ShowLobby();
        }

        private void OnDestroy()
        {
            DetachShopEvents();

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

            // Everything between the top bar and the tab bar lives in ONE safe box, on one column, at fixed sizes
            // (owner, 2026-11-25). The notch and the home bar now cut the box instead of the artwork.
            lobbySafe = Rect("LobbySafeArea", lobby.transform, Vector2.zero, Vector2.one);
            lobbySafe.gameObject.AddComponent<SafeAreaFitter>();

            BuildLobbyCentre(lobbySafe);

            for (int i = 0; i < Tabs.Length; i++)
            {
                float width = 1f / Tabs.Length;
                float xMin = i * width;
                string tab = Tabs[i];
                navTabs[tab] = BuildFantasyTab(tab, new Vector2(xMin, TabBandBottomFraction),
                    new Vector2(xMin + width, TabBandTopFraction), () => OnTab(tab));
            }

            HighlightNav("BATTLE");

            // The old top-right MENU button is retired (2026-11-14): the header's three-bar menu returns to the
            // lobby now, so there is one menu affordance instead of two overlapping ones.
        }

        /// <summary>
        /// The lobby's centre: chapter kicker, wordmark, chapter card and START, on ONE column, centred between the top
        /// bar and the tab bar so the air above and below is the same number (owner, 2026-11-25).
        ///
        /// ELI5: three things stacked in the middle of the screen: a small "Chapter 1", the big chapter name, the
        /// picture, and the START button. They are all the same width and all 32 units from each edge, so the eye sees
        /// one column instead of four different margins. Their sizes never change; on a taller phone only the empty
        /// space above and below the column grows, equally.
        /// </summary>
        private void BuildLobbyCentre(RectTransform safe)
        {
            float safeW = safe.rect.width;
            float safeH = safe.rect.height;
            float column = safeW - 2f * UiSpace.PageMargin;

            float kickerH = UiType.LineHeight(UiType.Role.Title);
            float titleW = Mathf.Min(ChapterTitleWidth, column);
            float titleH = titleW / TitleArtAspect;
            float cardW = Mathf.Min(ChapterCardWidth, column);
            float cardH = cardW / ChapterArtAspect;

            // Gap hierarchy: 8 inside the title unit, then ONE air gap used twice - 64 between the name and the
            // picture and 64 between the picture and the action - so the two breathers match (owner, 2026-11-25).
            float blockH = kickerH + UiSpace.Space8 + titleH + UiSpace.Space64
                + cardH + UiSpace.Space64 + LobbyActionHeight;

            float available = safeH - UiSpace.HeaderBandHeight - safeH * TabBandTopFraction;
            float gap = Mathf.Max(0f, (available - blockH) * 0.5f);

            RectTransform block = Rect("LobbyCentre", safe, new Vector2(0f, 1f), new Vector2(1f, 1f));
            block.pivot = new Vector2(0.5f, 1f);
            block.sizeDelta = new Vector2(0f, blockH);
            block.anchoredPosition = new Vector2(0f, -(UiSpace.HeaderBandHeight + gap));

            // A vertical band inside the block, measured from the block's top edge in canvas units.
            Vector2 Y(float top, float height)
            {
                return new Vector2(1f - (top + height) / blockH, 1f - top / blockH);
            }

            // The card and START share these edges; the gutters outside them are the carousel's arrow room.
            float cardX0 = (safeW - cardW) * 0.5f / safeW;
            float cardX1 = 1f - cardX0;

            float titleTop = kickerH + UiSpace.Space8;
            float cardTop = titleTop + titleH + UiSpace.Space64;
            float startTop = cardTop + cardH + UiSpace.Space64;

            // Glows first, so they draw behind the art they light instead of over it.
            Vector2 cardGlow = Y(cardTop - UiSpace.Space32, cardH + 2f * UiSpace.Space32);
            Atmosphere(block, "ChapterGlow", new Vector2(0f, cardGlow.x), new Vector2(1f, cardGlow.y),
                SoftGlow, UiTheme.HaloGreen);

            Vector2 startGlow = Y(startTop - UiSpace.Space32, LobbyActionHeight + 2f * UiSpace.Space32);
            Atmosphere(block, "StartGlow", new Vector2(0f, startGlow.x), new Vector2(1f, startGlow.y),
                SoftGlow, UiTheme.HaloGold);

            // "Chapter 1" is a label, not a sentence: a type step up from the old size, spaced letters, quieter ink.
            Vector2 kickerY = Y(0f, kickerH);
            TextMeshProUGUI kicker = Text(block, "ChapterLabel", new Vector2(cardX0, kickerY.x),
                new Vector2(cardX1, kickerY.y), "Chapter 1", UiType.Role.Title, UiTheme.InkSoft);
            kicker.characterSpacing = KickerTracking;

            Vector2 titleY = Y(titleTop, titleH);
            float titleX0 = (safeW - titleW) * 0.5f / safeW;

            if (chapterTitleArt != null)
            {
                ArtPanel(block, "ChapterTitle", new Vector2(titleX0, titleY.x), new Vector2(1f - titleX0, titleY.y),
                    chapterTitleArt);
            }
            else
            {
                Text(block, "ChapterTitle", new Vector2(cardX0, titleY.x), new Vector2(cardX1, titleY.y),
                    "Greenwake", UiType.Role.Title, UiTheme.Text);
            }

            Vector2 cardY = Y(cardTop, cardH);

            if (chapterArt != null)
            {
                Image card = ArtPanel(block, "ChapterCard", new Vector2(cardX0, cardY.x),
                    new Vector2(cardX1, cardY.y), chapterArt);

                // The shared two-step shadow lifts the picture off the background, the same way every panel does.
                UiParts.AddShadow(card.gameObject);
            }
            else
            {
                UiParts.Panel(block, "ChapterCard", new Vector2(cardX0, cardY.x), new Vector2(cardX1, cardY.y),
                    PanelKind.Card);
            }

            // The gold bar art carries its own colour, so it is drawn white (no tint) at its own border scale. It is
            // narrower than the card, so the action reads as a button under the hero, not a second slab.
            Vector2 startY = Y(startTop, LobbyActionHeight);
            float actionW = Mathf.Min(LobbyActionWidth, cardW);
            float startX0 = (safeW - actionW) * 0.5f / safeW;

            UiPlateButton start = UiPlateButton.Create(block, "StartButton", new Vector2(startX0, startY.x),
                new Vector2(1f - startX0, startY.y), "START", UiType.Role.Hero, Color.white, Color.white,
                StartGame, ButtonFace.Primary);

            // A slightly heavier dark rim than the shared font material (0.3 against 0.2): enough for the primary
            // action to hold its edge on the bright gold bar without looking inked-on.
            TextMeshProUGUI startLabel = start.GetComponentInChildren<TextMeshProUGUI>(true);
            Material heavyInk = Resources.Load<Material>(HeavyOutlineMaterialPath);

            if (startLabel != null && heavyInk != null)
            {
                startLabel.fontSharedMaterial = heavyInk;
            }
        }

        /// <summary>
        /// Shows the between-runs screen. There is no fight to pause: the manager only enters this state when no
        /// run is active. Also hides the in-run quit control, so the header holds one meaning at a time.
        /// </summary>
        public void ShowLobby()
        {
            if (lobby != null)
            {
                lobby.SetActive(true);
            }

            // The lobby opens on its card, never on the page that was last open before a run. One page at a time.
            HideTraining();
            HideParty();
            HidePerks();
            HideShop();

            AttachSharedHeader();
            HighlightNav("BATTLE");
            RaiseNav();
            Time.timeScale = 1f;
        }

        /// <summary>Leaves the lobby for a live fight. Idempotent: both START and the BATTLE tab call it.</summary>
        private void HideLobby()
        {
            if (lobby != null && lobby.activeSelf)
            {
                DetachSharedHeader();
                lobby.SetActive(false);
            }

            Time.timeScale = 1f;
        }

        /// <summary>
        /// Opens the lobby's SHOP page. The header's currency plates call this (owner, 2026-11-25), so a tap on gold,
        /// gems or tokens lands on the shelf. It refuses when the lobby is not on screen: there is no way to shop in
        /// a fight.
        /// </summary>
        public void OpenShopFromHeader()
        {
            if (lobby == null || !lobby.activeSelf)
            {
                return;
            }

            // OnTab closes every lobby page first, so this is the same path the SHOP tab takes.
            OnTab("SHOP");
        }

        /// <summary>
        /// START begins a new run. The lobby only exists between runs, so there is no live run to reset here.
        /// </summary>
        private void StartGame()
        {
            EnsureManager();

            if (manager != null && !manager.RunIsActive)
            {
                manager.StartRun(1);
            }

            HideLobby();
        }

        private void OnTab(string tab)
        {
            HighlightNav(tab);

            // One page at a time: close every lobby page first, then open the chosen one.
            HideTraining();
            HideParty();
            HidePerks();
            HideShop();

            switch (tab)
            {
                case "SHOP":
                    ShowShop();
                    RaiseNav();
                    return;

                case "BATTLE":
                    // The BATTLE tab returns to the lobby's centre: the chapter card and START. It must NOT
                    // begin a run - only the START button does that (owner report 2026-11-22).
                    return;

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
            UiSkin skin = UiSkin.Current;
            // The tab bar lives in the same safe box as the centre, so the home bar never sits on top of it.
            RectTransform tabRoot = Rect("Nav" + tab, lobbySafe != null ? lobbySafe : lobby.transform, aMin, aMax);

            RectTransform glowRect = Rect("Glow", tabRoot, Vector2.zero, Vector2.one);
            glowRect.offsetMin = new Vector2(-16f, -16f);
            glowRect.offsetMax = new Vector2(16f, 16f);
            Image glow = glowRect.gameObject.AddComponent<Image>();
            glow.raycastTarget = false;

            RectTransform frameRect = Rect("Frame", tabRoot, Vector2.zero, Vector2.one);
            Image frame = frameRect.gameObject.AddComponent<Image>();
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

            // A dark copy under every icon so gold art still reads on the gold selected face. Not a plate:
            // an outline that also helps on the slate face.
            Shadow iconShadow = icon.gameObject.AddComponent<Shadow>();
            iconShadow.effectColor = UiTheme.ShadowInk;
            iconShadow.effectDistance = new Vector2(0f, -2f);
            iconShadow.useGraphicAlpha = true;

            TextMeshProUGUI text = Text(frameRect, "Label", new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.32f), tab,
                UiType.Role.Small, UiTheme.Text);
            UiType.Apply(text, UiType.Role.Small);   // a real scale step, not the old hard-coded 36
            text.raycastTarget = false;

            Coffee.UIEffects.UIEffect effect = frameRect.gameObject.AddComponent<Coffee.UIEffects.UIEffect>();

            FantasyTabButton view = frameRect.gameObject.AddComponent<FantasyTabButton>();

            // Idle = the slate bar, selected = the same bar at full brightness (no gold: gold means currency).
            // The icon keeps its own colours (with a shadow), and the word is the one white ink in both states.
            view.Bind(tabRoot, frame, glow, text, effect, button,
                UiTheme.NavIdle, UiTheme.NavSelected, UiTheme.Text, UiTheme.Text, icon, TabIconScale(tab),
                TabIconSelectedScale(tab), TabIconVertical(tab), TabIconSelectedVertical(tab),
                skin.neutralButtonSprite, skin.neutralButtonSprite, skin.buttonBorderScale);
            return view;
        }

        /// <summary>Idle height nudge (selected uses TabIconSelectedScale).</summary>
        private static float TabIconScale(string tab)
        {
            switch (tab)
            {
                case "PARTY": return 0.88f;   // owner: the three figures read heavy next to the square icons
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

        /// <summary>
        /// Idle vertical nudge (fraction of icon height). These are MEASURED from a 1080x1920 capture (2026-11-22),
        /// not guessed: after the common lift, the bag drew 4.5 px high and the arrow 1.5 px low, so each raw
        /// sprite carries its own offset inside its cell. Re-measure if an icon's art changes.
        /// </summary>
        private static float TabIconVertical(string tab)
        {
            switch (tab)
            {
                case "SHOP": return -0.032f;
                case "ASCEND": return 0.011f;
                case "TRAIN": return 0.043f;
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
        //
        // The RECT IS NOT TOUCHED. It used to be re-stretched to 0.03-0.97 x 0.90-0.985 of the screen, which turned
        // the same header into a 163-tall bar in the lobby and an 83-tall one in battle. One bar, one size, every
        // screen (2026-11-25). A safe box keeps the notch behaving as it does in battle, because this canvas is not
        // safe-area fitted the way the HUD canvas is.
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
            sharedHeaderSibling = headerRect.GetSiblingIndex();

            RectTransform box = Rect("HeaderSafeBox", lobby.transform, Vector2.zero, Vector2.one);
            box.gameObject.AddComponent<SafeAreaFitter>();
            sharedHeaderBox = box.gameObject;

            headerRect.SetParent(box, false);
            box.SetAsLastSibling();
        }

        private void DetachSharedHeader()
        {
            if (sharedHeader == null || sharedHeaderHome == null)
            {
                return;
            }

            sharedHeader.SetParent(sharedHeaderHome, false);
            sharedHeader.SetSiblingIndex(sharedHeaderSibling);
            sharedHeader = null;

            if (sharedHeaderBox != null)
            {
                Destroy(sharedHeaderBox);
                sharedHeaderBox = null;
            }
        }

        private void EnsureManager()
        {
            if (manager == null)
            {
                HudController hud = HudController.Instance;
                manager = hud != null ? hud.GameManager : null;
            }
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

        // Atmosphere sprites are built in code and cached: no new art, one tiny gradient + two small radial
        // textures. They add depth (light + shadow) without touching the chapter art or the START button.
        private static Sprite gradientSprite;
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

        /// <summary>The shared soft glow from UiRuntime - one texture for every halo.</summary>
        private static Sprite SoftGlow => UiRuntime.SoftGlowSprite;

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

        /// <summary>A lobby page surface: through UiParts, so every page shares one container shape.</summary>
        private static Image Panel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Color color,
            Sprite sprite = null)
        {
            if (sprite == null)
            {
                return UiParts.Panel(parent, name, aMin, aMax, PanelKind.Card, tint: color);
            }

            RectTransform rt = Rect(name, parent, aMin, aMax);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
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
            // Every text action button in the lobby pages is a primary action (BUY). The skin owns the art, ink
            // and border scale, so no page passes a sprite or a colour for the face.
            return UiPlateButton.Create(parent, name, aMin, aMax, label, role, bg, bg, onClick, ButtonFace.Primary);
        }
    }
}
