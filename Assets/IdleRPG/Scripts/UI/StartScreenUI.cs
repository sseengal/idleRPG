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

        private static readonly string[] Tabs = { "SHOP", "PARTY", "BATTLE", "ASCENSION", "TRAINING" };

        private GameManager manager;
        private ScreenController screens;
        private GameObject lobby;
        private GameObject menuButton;
        private TextMeshProUGUI statusLabel;

        private static readonly Color NavSelected = new Color(0.96f, 0.80f, 0.28f, 1f);
        private static readonly Color NavIdle = new Color(0.11f, 0.13f, 0.19f, 1f);

        // Bottom nav: one image per tab so the open page can be highlighted.
        private readonly System.Collections.Generic.Dictionary<string, Image> navButtons =
            new System.Collections.Generic.Dictionary<string, Image>();

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
        private Button popupBuy;
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

            Panel(lobby.transform, "BG", Vector2.zero, Vector2.one, new Color(0.05f, 0.06f, 0.10f, 1f));

            Image card = Panel(lobby.transform, "ChapterCard", new Vector2(0.10f, 0.50f), new Vector2(0.90f, 0.78f), new Color(0.12f, 0.16f, 0.24f, 1f));
            Text(card.transform, "ChapterLabel", new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.85f), "Chapter 1: Greenwake", UiType.Role.Section, Color.white);
            Text(card.transform, "Hint", new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.16f), "(chapters coming)", UiType.Role.Micro, new Color(0.6f, 0.63f, 0.72f, 1f));

            Btn(lobby.transform, "StartButton", new Vector2(0.20f, 0.38f), new Vector2(0.80f, 0.46f), "START", new Color(0.96f, 0.80f, 0.28f, 1f), UiType.Role.Title, StartGame);
            statusLabel = Text(lobby.transform, "Status", new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.38f), string.Empty, UiType.Role.Small, new Color(0.75f, 0.78f, 0.86f, 1f));

            for (int i = 0; i < Tabs.Length; i++)
            {
                float width = 1f / Tabs.Length;
                float xMin = i * width;
                string tab = Tabs[i];
                Button navButton = Btn(lobby.transform, "Nav" + tab, new Vector2(xMin, 0.03f), new Vector2(xMin + width, 0.11f), tab, NavIdle, UiType.Role.Small, () => OnTab(tab));
                navButtons[tab] = navButton.targetGraphic as Image;
            }

            HighlightNav("BATTLE");

            Canvas menuCanvas = MakeCanvas("MiniMenuCanvas", 251, true);
            menuButton = menuCanvas.gameObject;
            Btn(menuCanvas.transform, "MenuButton", new Vector2(0.80f, 0.90f), new Vector2(0.97f, 0.975f), "MENU", new Color(0.11f, 0.13f, 0.19f, 1f), UiType.Role.Small, ShowLobby);
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
                case "TRAINING":
                    ShowTraining();
                    return;

                case "PARTY":
                    ShowParty();
                    return;

                case "ASCENSION":
                    ShowPerks();
                    return;
            }

            if (statusLabel != null)
            {
                statusLabel.SetText(tab == "BATTLE" ? "Press START to fight." : tab + " - coming soon.");
            }
        }

        private void HighlightNav(string tab)
        {
            foreach (System.Collections.Generic.KeyValuePair<string, Image> pair in navButtons)
            {
                if (pair.Value != null)
                {
                    pair.Value.color = pair.Key == tab ? NavSelected : NavIdle;
                }
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

        private static Image Panel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Color color)
        {
            RectTransform rt = Rect(name, parent, aMin, aMax);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
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

        private static Button Btn(Transform parent, string name, Vector2 aMin, Vector2 aMax, string label, Color bg,
            UiType.Role role, UnityEngine.Events.UnityAction onClick)
        {
            Image img = Panel(parent, name, aMin, aMax, bg);
            Button b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;

            TextMeshProUGUI t = Text(img.transform, "Label", Vector2.zero, Vector2.one, label, role, Color.white);
            UiType.ApplyAuto(t, role);
            t.raycastTarget = false;

            b.onClick.AddListener(onClick);
            return b;
        }
    }
}
