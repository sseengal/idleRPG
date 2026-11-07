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
    public sealed class StartScreenUI : MonoBehaviour
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

        // Diamond-tree view (two tracks).
        private RectTransform treeContent;
        private ScrollRect treeScroll;
        private bool treeBuilt;
        private Image goldZone;
        private float treeContentHeight;
        private float treeRowH = 220f;
        private float treeTopPad = 80f;
        private const float NodeSize = 112f;
        private const float LeftX = 350f;
        private const float RightX = 752f;

        private readonly System.Collections.Generic.List<TreeView> treeViews = new System.Collections.Generic.List<TreeView>();

        private struct TreeView
        {
            public IdleRPG.Data.TrainingNode Node;
            public Image Diamond;
            public Image Connector; // line ABOVE this node (to the next one)
            public Image Badge;
            public RectTransform Rect;
            public bool IsLeft;
        }

        private GameObject treePopup;
        private TextMeshProUGUI popupTitle;
        private TextMeshProUGUI popupBody;
        private Button popupBuy;
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
            Text(card.transform, "ChapterLabel", new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.85f), "Chapter 1: Greenwake", 40f, Color.white);
            Text(card.transform, "Hint", new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.16f), "(chapters coming)", 24f, new Color(0.6f, 0.63f, 0.72f, 1f));

            Btn(lobby.transform, "StartButton", new Vector2(0.20f, 0.38f), new Vector2(0.80f, 0.46f), "START", new Color(0.96f, 0.80f, 0.28f, 1f), 46f, StartGame);
            statusLabel = Text(lobby.transform, "Status", new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.38f), string.Empty, 28f, new Color(0.75f, 0.78f, 0.86f, 1f));

            for (int i = 0; i < Tabs.Length; i++)
            {
                float width = 1f / Tabs.Length;
                float xMin = i * width;
                string tab = Tabs[i];
                Button navButton = Btn(lobby.transform, "Nav" + tab, new Vector2(xMin, 0.03f), new Vector2(xMin + width, 0.11f), tab, NavIdle, 26f, () => OnTab(tab));
                navButtons[tab] = navButton.targetGraphic as Image;
            }

            HighlightNav("BATTLE");

            Canvas menuCanvas = MakeCanvas("MiniMenuCanvas", 251, true);
            menuButton = menuCanvas.gameObject;
            Btn(menuCanvas.transform, "MenuButton", new Vector2(0.78f, 0.90f), new Vector2(0.97f, 0.97f), "MENU", new Color(0.11f, 0.13f, 0.19f, 1f), 26f, ShowLobby);
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

            if (tab == "TRAINING")
            {
                ShowTraining();
                return;
            }

            HideTraining();

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
        // Training tab (vertical scroll tree)
        // ------------------------------------------------------------------
        private void ShowTraining()
        {
            EnsureManager();

            if (!treeBuilt)
            {
                BuildTrainingTree();
            }

            if (trainingPanel != null)
            {
                trainingPanel.SetActive(true);
            }

            if (statusLabel != null)
            {
                statusLabel.SetText(string.Empty);
            }

            RefreshTree();

            if (treeScroll != null && treeContent != null)
            {
                Canvas.ForceUpdateCanvases();

                float viewH = treeScroll.viewport != null ? treeScroll.viewport.rect.height : 0f;
                treeContent.anchoredPosition = new Vector2(0f, Mathf.Max(0f, treeContentHeight - viewH));
                treeScroll.verticalNormalizedPosition = 0f;   // start at the bottom; scroll up to progress
            }
        }

        private void HideTraining()
        {
            ClosePopup();

            if (trainingPanel != null && trainingPanel.activeSelf)
            {
                trainingPanel.SetActive(false);
            }
        }

        private void BuildTrainingTree()
        {
            treeBuilt = true;

            GameObject panel = new GameObject("TrainingPanel", typeof(RectTransform), typeof(Image));
            RectTransform pr = panel.GetComponent<RectTransform>();
            pr.SetParent(lobby.transform, false);
            pr.anchorMin = new Vector2(0.02f, 0.13f);
            pr.anchorMax = new Vector2(0.98f, 0.885f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.13f, 0.98f);
            trainingPanel = panel;

            GameObject scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            RectTransform sr = scrollGO.GetComponent<RectTransform>();
            sr.SetParent(panel.transform, false);
            sr.anchorMin = new Vector2(0f, 0f);
            sr.anchorMax = new Vector2(1f, 1f);
            sr.offsetMin = Vector2.zero;
            sr.offsetMax = Vector2.zero;
            scrollGO.GetComponent<Image>().color = new Color(0.13f, 0.14f, 0.16f, 1f);
            treeScroll = scrollGO.GetComponent<ScrollRect>();
            treeScroll.horizontal = false;
            treeScroll.vertical = true;
            treeScroll.movementType = ScrollRect.MovementType.Clamped;
            treeScroll.scrollSensitivity = 40f;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            RectTransform vr = viewport.GetComponent<RectTransform>();
            vr.SetParent(scrollGO.transform, false);
            vr.anchorMin = Vector2.zero;
            vr.anchorMax = Vector2.one;
            vr.offsetMin = Vector2.zero;
            vr.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            treeScroll.viewport = vr;

            var tree = manager != null && manager.Training != null ? manager.Training.Tree : null;
            int leftCount = 0;
            int rightCount = 0;
            if (tree != null)
            {
                var ns = tree.Nodes;
                for (int i = 0; i < ns.Count; i++)
                {
                    if (ns[i].Branch == IdleRPG.Data.TrainingBranch.Left) leftCount++; else rightCount++;
                }
            }

            float totalH = treeTopPad * 2f + Mathf.Max(leftCount, rightCount) * treeRowH;
            treeContentHeight = totalH;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            treeContent = content.GetComponent<RectTransform>();
            treeContent.SetParent(viewport.transform, false);
            treeContent.anchorMin = new Vector2(0f, 1f);
            treeContent.anchorMax = new Vector2(1f, 1f);
            treeContent.pivot = new Vector2(0.5f, 1f);
            treeContent.anchoredPosition = Vector2.zero;
            treeContent.sizeDelta = new Vector2(0f, totalH);
            treeScroll.content = treeContent;

            GameObject zone = new GameObject("GoldZone", typeof(RectTransform), typeof(Image));
            RectTransform zr = zone.GetComponent<RectTransform>();
            zr.SetParent(treeContent, false);
            zr.anchorMin = new Vector2(0f, 0f);
            zr.anchorMax = new Vector2(1f, 0f);
            zr.pivot = new Vector2(0.5f, 0f);
            zr.anchoredPosition = Vector2.zero;
            zr.sizeDelta = new Vector2(0f, 0f);
            goldZone = zone.GetComponent<Image>();
            goldZone.color = new Color(0.98f, 0.78f, 0.2f, 0.07f);

            if (tree != null)
            {
                var nodes = tree.Nodes;

                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].Branch != IdleRPG.Data.TrainingBranch.Left) continue;
                    int order = nodes[i].Order;
                    int rowFromTop = leftCount - 1 - order;

                    if (order % 8 == 0)
                    {
                        TextMeshProUGUI mk = Text(treeContent, "Lvl", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), "LEVEL " + (order / 8 + 1), 22f, new Color(0.98f, 0.8f, 0.28f, 0.35f));
                        mk.rectTransform.sizeDelta = new Vector2(240f, 30f);
                        mk.rectTransform.anchoredPosition = new Vector2(0f, -(treeTopPad + rowFromTop * treeRowH + treeRowH * 0.5f));
                    }

                    MakeNode(nodes[i], LeftX, rowFromTop, true, 1);
                }

                var rightNodes = new System.Collections.Generic.List<IdleRPG.Data.TrainingNode>();
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].Branch == IdleRPG.Data.TrainingBranch.Right)
                    {
                        rightNodes.Add(nodes[i]);
                    }
                }

                for (int i = 0; i < rightNodes.Count; i++)
                {
                    int gate = Mathf.Clamp(rightNodes[i].RequiresLeftNodes, 0, Mathf.Max(0, leftCount - 1));
                    int nextGate = i + 1 < rightNodes.Count
                        ? Mathf.Clamp(rightNodes[i + 1].RequiresLeftNodes, 0, Mathf.Max(0, leftCount - 1))
                        : gate;

                    MakeNode(rightNodes[i], RightX, leftCount - 1 - gate, false, nextGate - gate);
                }
            }

            Btn(panel.transform, "ScrollDown", new Vector2(0.90f, 0.005f), new Vector2(0.99f, 0.075f), "\u25BC", new Color(0.18f, 0.19f, 0.22f, 1f), 24f, ScrollTreeToBottom);

            BuildPopup(treeContent);
            trainingPanel.SetActive(false);
        }

        private void ScrollTreeToBottom()
        {
            if (treeScroll != null)
            {
                treeScroll.verticalNormalizedPosition = 0f;
            }
        }

        private void MakeNode(IdleRPG.Data.TrainingNode node, float xPx, int rowFromTop, bool isLeft, int connectorRows)
        {
            float centerY = treeTopPad + rowFromTop * treeRowH + treeRowH * 0.5f;

            Image connector = null;

            if (connectorRows > 0)
            {
                GameObject conGO = new GameObject("Con", typeof(RectTransform), typeof(Image));
                RectTransform cr = conGO.GetComponent<RectTransform>();
                cr.SetParent(treeContent, false);
                cr.anchorMin = new Vector2(0f, 1f);
                cr.anchorMax = new Vector2(0f, 1f);
                cr.pivot = new Vector2(0.5f, 0.5f);
                cr.sizeDelta = new Vector2(12f, connectorRows * treeRowH);
                cr.anchoredPosition = new Vector2(xPx, -(centerY - connectorRows * treeRowH * 0.5f));
                cr.SetSiblingIndex(1);   // connectors always sit behind the diamonds
                connector = conGO.GetComponent<Image>();
                connector.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            }

            GameObject nodeGO = new GameObject("Node_" + node.NodeID, typeof(RectTransform));
            RectTransform nr = nodeGO.GetComponent<RectTransform>();
            nr.SetParent(treeContent, false);
            nr.anchorMin = new Vector2(0f, 1f);
            nr.anchorMax = new Vector2(0f, 1f);
            nr.pivot = new Vector2(0.5f, 0.5f);
            nr.sizeDelta = new Vector2(NodeSize, NodeSize);
            nr.anchoredPosition = new Vector2(xPx, -centerY);

            GameObject diaGO = new GameObject("Diamond", typeof(RectTransform), typeof(Image));
            RectTransform dr = diaGO.GetComponent<RectTransform>();
            dr.SetParent(nodeGO.transform, false);
            dr.anchorMin = Vector2.zero;
            dr.anchorMax = Vector2.one;
            dr.offsetMin = new Vector2(13f, 13f);
            dr.offsetMax = new Vector2(-13f, -13f);
            dr.localRotation = UnityEngine.Quaternion.Euler(0f, 0f, 45f);
            Image diamond = diaGO.GetComponent<Image>();
            diamond.color = new Color(0.35f, 0.36f, 0.4f, 1f);

            TextMeshProUGUI icon = Text(nodeGO.transform, "Icon", Vector2.zero, Vector2.one, IconLetter(node.Effect), 44f, Color.white);
            icon.raycastTarget = false;

            GameObject badgeGO = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            RectTransform br = badgeGO.GetComponent<RectTransform>();
            br.SetParent(nodeGO.transform, false);
            br.anchorMin = new Vector2(0.5f, 0.5f);
            br.anchorMax = new Vector2(0.5f, 0.5f);
            br.pivot = new Vector2(0.5f, 0.5f);
            br.sizeDelta = new Vector2(32f, 32f);
            br.anchoredPosition = new Vector2(40f, -40f);
            Image badge = badgeGO.GetComponent<Image>();
            badge.color = new Color(0.3f, 0.9f, 0.4f, 1f);
            TextMeshProUGUI badgeText = Text(badgeGO.transform, "T", Vector2.zero, Vector2.one, "\u2191", 24f, new Color(0.05f, 0.2f, 0.05f, 1f));
            badgeText.raycastTarget = false;
            badgeGO.SetActive(false);

            Button b = nodeGO.AddComponent<Button>();
            b.targetGraphic = diamond;
            b.transition = Selectable.Transition.None;
            IdleRPG.Data.TrainingNode captured = node;
            b.onClick.AddListener(() => OnNodeTap(captured));

            treeViews.Add(new TreeView { Node = node, Diamond = diamond, Connector = connector, Badge = badge, Rect = nr, IsLeft = isLeft });
        }

        private static string IconLetter(IdleRPG.Data.TrainingEffect effect)
        {
            switch (effect)
            {
                case IdleRPG.Data.TrainingEffect.AttackFlat: return "A";
                case IdleRPG.Data.TrainingEffect.ArmorFlat: return "D";
                case IdleRPG.Data.TrainingEffect.HpFlat: return "H";
                case IdleRPG.Data.TrainingEffect.CampHealPercent: return "C";
                case IdleRPG.Data.TrainingEffect.UnlockFirstAbility: return "F";
                case IdleRPG.Data.TrainingEffect.BattleGoldPercent: return "$";
                case IdleRPG.Data.TrainingEffect.FinishGoldPercent: return "$";
                default: return "?";
            }
        }

        private static string FriendlyName(IdleRPG.Data.TrainingEffect effect)
        {
            switch (effect)
            {
                case IdleRPG.Data.TrainingEffect.AttackFlat: return "Attack";
                case IdleRPG.Data.TrainingEffect.ArmorFlat: return "Armor";
                case IdleRPG.Data.TrainingEffect.HpFlat: return "Health";
                case IdleRPG.Data.TrainingEffect.CampHealPercent: return "Camp heal";
                case IdleRPG.Data.TrainingEffect.BattleGoldPercent: return "Battle gold";
                case IdleRPG.Data.TrainingEffect.FinishGoldPercent: return "Finish gold";
                case IdleRPG.Data.TrainingEffect.UnlockFirstAbility: return "Active skill";
                case IdleRPG.Data.TrainingEffect.CampExperience: return "Camp XP";
                case IdleRPG.Data.TrainingEffect.LevelUpHealPercent: return "Level-up heal";
                case IdleRPG.Data.TrainingEffect.SurviveDeathShield: return "Survive once";
                default: return "Training";
            }
        }

        private static string EffectLine(IdleRPG.Data.TrainingNode node)
        {
            string amount = node.Amount.ToString("0.##");

            switch (node.Effect)
            {
                case IdleRPG.Data.TrainingEffect.AttackFlat: return "Attack +" + amount;
                case IdleRPG.Data.TrainingEffect.ArmorFlat: return "Armor +" + amount;
                case IdleRPG.Data.TrainingEffect.HpFlat: return "Max health +" + amount;
                case IdleRPG.Data.TrainingEffect.CampHealPercent: return "Camp healing +" + amount;
                case IdleRPG.Data.TrainingEffect.BattleGoldPercent: return "Battle gold +" + (node.Amount * 100d).ToString("0.##") + "%";
                case IdleRPG.Data.TrainingEffect.FinishGoldPercent: return "Finish gold +" + (node.Amount * 100d).ToString("0.##") + "%";
                case IdleRPG.Data.TrainingEffect.UnlockFirstAbility: return "Unlocks the first ability";
                default: return node.Description;
            }
        }

        private static Sprite coinSprite;

        private static Sprite CircleSprite()
        {
            if (coinSprite != null)
            {
                return coinSprite;
            }

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.5f;
            Color clear = new Color(1f, 1f, 1f, 0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    texture.SetPixel(x, y, d <= radius - 0.5f ? Color.white : clear);
                }
            }

            texture.Apply();
            coinSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
            return coinSprite;
        }

        private void RefreshTree()
        {
            EnsureManager();

            if (manager == null || manager.Training == null)
            {
                return;
            }

            int left = manager.Training.LeftUnlockedCount;

            if (goldZone != null)
            {
                goldZone.rectTransform.sizeDelta = new Vector2(0f, Mathf.Max(0f, treeTopPad + left * treeRowH - treeRowH * 0.5f));
            }

            IdleRPG.Data.TrainingNode nextLeft = null;
            IdleRPG.Data.TrainingNode nextRight = null;

            for (int i = 0; i < treeViews.Count; i++)
            {
                TreeView v = treeViews[i];

                if (manager.Training.CanUnlock(v.Node))
                {
                    if (v.IsLeft)
                    {
                        if (nextLeft == null) nextLeft = v.Node;
                    }
                    else if (nextRight == null)
                    {
                        nextRight = v.Node;
                    }
                }
            }

            for (int i = 0; i < treeViews.Count; i++)
            {
                TreeView v = treeViews[i];
                bool unlocked = manager.Training.IsUnlocked(v.Node);
                bool isNext = !unlocked && (v.IsLeft ? nextLeft : nextRight) == v.Node;

                Color c;

                if (unlocked)
                {
                    c = new Color(0.98f, 0.80f, 0.22f, 1f);
                }
                else if (v.Node.IsDeferred)
                {
                    c = new Color(0.12f, 0.12f, 0.14f, 1f);
                }
                else if (isNext)
                {
                    c = new Color(0.45f, 0.46f, 0.5f, 1f);
                }
                else
                {
                    c = new Color(0.30f, 0.31f, 0.35f, 1f);
                }

                if (v.Diamond != null)
                {
                    v.Diamond.color = c;
                }

                if (v.Badge != null)
                {
                    v.Badge.gameObject.SetActive(isNext);
                }

                if (v.Connector != null)
                {
                    v.Connector.color = unlocked ? new Color(0.9f, 0.72f, 0.15f, 1f) : new Color(0.30f, 0.31f, 0.35f, 1f);
                }
            }

            if (treePopup != null && treePopup.activeSelf)
            {
                UpdatePopup();
            }
        }

        private void BuildPopup(Transform parent)
        {
            GameObject card = new GameObject("TreePopup", typeof(RectTransform), typeof(Image));
            RectTransform cr = card.GetComponent<RectTransform>();
            cr.SetParent(parent, false);
            cr.anchorMin = new Vector2(0f, 1f);
            cr.anchorMax = new Vector2(0f, 1f);
            cr.pivot = new Vector2(0f, 0.5f);
            cr.sizeDelta = new Vector2(470f, 214f);
            card.GetComponent<Image>().color = new Color(0.96f, 0.92f, 0.80f, 1f);
            treePopup = card;

            popupTitle = Text(card.transform, "Title", new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.94f), "", 32f, new Color(0.12f, 0.12f, 0.14f, 1f));
            popupBody = Text(card.transform, "Body", new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.62f), "", 26f, new Color(0.28f, 0.28f, 0.31f, 1f));
            popupBuy = Btn(card.transform, "Buy", new Vector2(0.16f, 0.08f), new Vector2(0.84f, 0.34f), "Buy", new Color(0.95f, 0.55f, 0.15f, 1f), 28f, BuyPopup);

            GameObject coinGO = new GameObject("Coin", typeof(RectTransform), typeof(Image));
            RectTransform co = coinGO.GetComponent<RectTransform>();
            co.SetParent(popupBuy.transform, false);
            co.anchorMin = new Vector2(1f, 0.5f);
            co.anchorMax = new Vector2(1f, 0.5f);
            co.pivot = new Vector2(1f, 0.5f);
            co.sizeDelta = new Vector2(34f, 34f);
            co.anchoredPosition = new Vector2(-18f, 0f);
            Image coin = coinGO.GetComponent<Image>();
            coin.sprite = CircleSprite();
            coin.color = new Color(0.98f, 0.80f, 0.20f, 1f);
            coin.raycastTarget = false;

            treePopup.SetActive(false);
        }

        private void OnNodeTap(IdleRPG.Data.TrainingNode node)
        {
            if (treePopup == null || manager == null || manager.Training == null)
            {
                return;
            }

            if (popupNode == node && treePopup.activeSelf)
            {
                ClosePopup();
                return;
            }

            popupNode = node;

            RectTransform card = treePopup.GetComponent<RectTransform>();
            bool onLeft = true;
            RectTransform nodeRect = null;

            for (int i = 0; i < treeViews.Count; i++)
            {
                if (treeViews[i].Node == node)
                {
                    onLeft = treeViews[i].IsLeft;
                    nodeRect = treeViews[i].Rect;
                    break;
                }
            }

            if (nodeRect != null)
            {
                Vector2 p = nodeRect.anchoredPosition;
                float gap = NodeSize * 0.5f + 18f;
                card.pivot = onLeft ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);
                card.anchoredPosition = new Vector2(p.x + (onLeft ? gap : -gap), p.y);
            }

            treePopup.transform.SetAsLastSibling();
            treePopup.SetActive(true);
            UpdatePopup();
        }

        private void UpdatePopup()
        {
            if (popupNode == null || manager == null || manager.Training == null)
            {
                return;
            }

            if (popupTitle != null)
            {
                popupTitle.SetText(FriendlyName(popupNode.Effect));
            }

            if (popupBody != null)
            {
                popupBody.SetText(EffectLine(popupNode));
            }

            bool owned = manager.Training.IsUnlocked(popupNode);

            if (popupBuy != null)
            {
                popupBuy.gameObject.SetActive(!owned);
                popupBuy.interactable = manager.Training.CanUnlock(popupNode);

                TextMeshProUGUI lbl = popupBuy.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null)
                {
                    lbl.SetText("Buy " + popupNode.CostGold.ToString("0"));
                }
            }

            if (treePopup != null)
            {
                RectTransform card = treePopup.GetComponent<RectTransform>();
                card.sizeDelta = new Vector2(470f, owned ? 150f : 214f);
            }
        }

        private void BuyPopup()
        {
            if (popupNode == null || manager == null || manager.Training == null)
            {
                return;
            }

            if (manager.Training.TryUnlock(popupNode))
            {
                RefreshTree();
                ClosePopup();
            }
        }

        private void ClosePopup()
        {
            if (treePopup != null && treePopup.activeSelf)
            {
                treePopup.SetActive(false);
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

        private static Image Panel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Color color)
        {
            RectTransform rt = Rect(name, parent, aMin, aMax);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, Vector2 aMin, Vector2 aMax, string text, float size, Color color)
        {
            RectTransform rt = Rect(name, parent, aMin, aMax);
            TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();

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

        private static Button Btn(Transform parent, string name, Vector2 aMin, Vector2 aMax, string label, Color bg, float size, UnityEngine.Events.UnityAction onClick)
        {
            Image img = Panel(parent, name, aMin, aMax, bg);
            Button b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;

            TextMeshProUGUI t = Text(img.transform, "Label", Vector2.zero, Vector2.one, label, size, Color.white);
            t.raycastTarget = false;

            b.onClick.AddListener(onClick);
            return b;
        }
    }
}
