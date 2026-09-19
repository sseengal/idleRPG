using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.UI;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Generates the full MVP game scene (Main.unity): canvas, header, combat viewport,
    /// control dock with the three panels, damage-number canvas and popups — all wired.
    ///
    /// Menu: Tools > Idle RPG > Build MVP Scene
    ///
    /// The scene is a generated artifact (re-running rebuilds it) while the data assets and
    /// sprites it references are independent, so balance/art changes survive a rebuild.
    /// </summary>
    public static class MvpSceneBuilder
    {
        public const string ScenePath = "Assets/IdleRPG/Scenes/Main.unity";

        private const float ReferenceWidth = 1080f;
        private const float ReferenceHeight = 1920f;

        // Layout bands, as fractions of screen height.
        private const float HeaderBottom = 0.88f;
        private const float ViewportBottom = 0.38f;

        private static readonly Color TextColor = new Color(0.94f, 0.96f, 1f, 1f);
        private static readonly Color DimTextColor = new Color(0.75f, 0.78f, 0.86f, 1f);

        [MenuItem("Tools/Idle RPG/Build MVP Scene", priority = 1)]
        public static void BuildMvpScene()
        {
            if (!TmpBootstrapper.EnsureEssentials())
            {
                Debug.LogError("[MvpSceneBuilder] TMP essentials are required before building the UI.");
                return;
            }

            PlaceholderSpriteGenerator.GenerateAll();
            DataAssetGenerator.GenerateAll();

            BalanceConfig balance = SceneWiringUtility.LoadBalance();
            WaveConfig waveConfig = SceneWiringUtility.LoadWaveConfig();
            PartyConfig partyConfig = SceneWiringUtility.LoadPartyConfig();
            List<StatUpgradeData> statTracks = SceneWiringUtility.LoadStatTracks();
            List<PrestigeUpgradeData> prestigeUpgrades = SceneWiringUtility.LoadPrestigeUpgrades();

            if (balance == null || waveConfig == null || partyConfig == null || statTracks.Count == 0 || prestigeUpgrades.Count == 0)
            {
                Debug.LogError("[MvpSceneBuilder] Data assets incomplete; run Tools > Idle RPG > Generate Data Assets.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            PrepareCamera();

            GameManager gameManager = SceneWiringUtility.CreateGameManagerObject(
                balance, waveConfig, partyConfig, statTracks, prestigeUpgrades,
                enableDebugLogger: false, enableHotkeys: true);

            GameObject hudRoot = new GameObject("HUD");
            HudController hud = hudRoot.AddComponent<HudController>();

            RectTransform canvasRoot = CreateCanvas(hudRoot);
            RectTransform damageRoot = CreateDamageCanvas(hudRoot);
            RectTransform safeArea = CreateSafeArea(canvasRoot);

            HudHeaderUI header = BuildHeader(safeArea);
            BuildViewport(safeArea, damageRoot, out HeroUnitView[] heroViews, out EnemyUnitView enemyView,
                out FloatingDamageTextPool damagePool, out RectTransform enemyAnchor, out RectTransform[] heroAnchors);
            TabController tabs = BuildDock(safeArea, partyConfig, prestigeUpgrades);
            OfflineRewardsPopup offlinePopup = BuildOfflinePopup(canvasRoot);
            ToastUI toast = BuildToast(canvasRoot);
            EnsureEventSystem();

            SceneWiringUtility.SetField(hud, "gameManager", gameManager);
            SceneWiringUtility.SetField(hud, "heroViews", heroViews);
            SceneWiringUtility.SetField(hud, "enemyView", enemyView);
            SceneWiringUtility.SetField(hud, "damageTextPool", damagePool);
            SceneWiringUtility.SetField(damagePool, "enemyAnchor", enemyAnchor);
            SceneWiringUtility.SetField(damagePool, "heroAnchors", heroAnchors);

            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(header);
            EditorUtility.SetDirty(tabs);
            EditorUtility.SetDirty(offlinePopup);
            EditorUtility.SetDirty(toast);

            EnsureFolder(Path.GetDirectoryName(ScenePath).Replace('\\', '/'));

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"[MvpSceneBuilder] Failed to save {ScenePath}.");
                return;
            }

            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[MvpSceneBuilder] Built {ScenePath}: header + viewport (3 heroes, 1 enemy) + dock (Upgrades/Ascension/Shop) + damage canvas + popups.");
        }

        // ------------------------------------------------------------------
        // Canvas scaffolding
        // ------------------------------------------------------------------
        private static void PrepareCamera()
        {
            Camera camera = Object.FindAnyObjectByType<Camera>();

            if (camera == null)
            {
                return;
            }

            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static RectTransform CreateCanvas(GameObject host)
        {
            GameObject canvasObject = UiFactory.Node("Canvas", host.transform);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvasObject.GetComponent<RectTransform>();
        }

        /// <summary>
        /// Damage numbers live on their own overlay canvas: a hit then dirtyes only this
        /// canvas instead of the whole HUD.
        /// </summary>
        private static RectTransform CreateDamageCanvas(GameObject host)
        {
            GameObject canvasObject = UiFactory.Node("DamageCanvas", host.transform);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // No GraphicRaycaster: damage numbers must never eat input.
            return canvasObject.GetComponent<RectTransform>();
        }

        private static RectTransform CreateSafeArea(RectTransform canvasRoot)
        {
            GameObject safeArea = UiFactory.Node("SafeArea", canvasRoot);
            safeArea.AddComponent<SafeAreaFitter>();
            return safeArea.GetComponent<RectTransform>();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();

            InputSystemUIInputModule module = eventSystemObject.AddComponent<InputSystemUIInputModule>();

            // The project uses the new Input System exclusively, so the UI module needs its
            // own action asset; generated defaults are enough for buttons.
            module.AssignDefaultActions();
        }

        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
        }

        // ------------------------------------------------------------------
        // Header
        // ------------------------------------------------------------------
        private static HudHeaderUI BuildHeader(RectTransform safeArea)
        {
            Image header = UiFactory.Panel("Header", safeArea, "ui_panel", new Color(1f, 1f, 1f, 0.98f));
            UiFactory.Anchor(header.rectTransform, new Vector2(0f, HeaderBottom), Vector2.one, 14f, 0f, 14f, 14f);
            HudHeaderUI view = header.gameObject.AddComponent<HudHeaderUI>();

            // Currency chips (icon + value), evenly spaced across the top row.
            TextMeshProUGUI goldText = CreateCurrencyChip(header.transform, "GoldChip", "ui_icon_gold", 0.03f, 0.32f);
            TextMeshProUGUI gemText = CreateCurrencyChip(header.transform, "GemChip", "ui_icon_gem", 0.35f, 0.62f);
            TextMeshProUGUI tokenText = CreateCurrencyChip(header.transform, "TokenChip", "ui_icon_token", 0.65f, 0.97f);

            TextMeshProUGUI stageText = UiFactory.Text("StageLabel", header.transform, "Stage 1", 34f,
                TextAlignmentOptions.MidlineRight, TextColor);
            UiFactory.Anchor(stageText.rectTransform, new Vector2(0.55f, 0.56f), new Vector2(0.98f, 0.92f));

            TextMeshProUGUI yieldText = UiFactory.Text("YieldLabel", header.transform, "Ascend: 0", 26f,
                TextAlignmentOptions.MidlineRight, DimTextColor);
            UiFactory.Anchor(yieldText.rectTransform, new Vector2(0.55f, 0.14f), new Vector2(0.98f, 0.5f));

            // Boost chip: hidden until an ad boost is running.
            Image boostChip = UiFactory.Panel("BoostChip", header.transform, "ui_button_gold", new Color(1f, 1f, 1f, 0.9f));
            UiFactory.Anchor(boostChip.rectTransform, new Vector2(0.03f, 0.06f), new Vector2(0.53f, 0.46f));

            TextMeshProUGUI boostText = UiFactory.Text("BoostLabel", boostChip.transform, "x2", 28f,
                TextAlignmentOptions.Center, new Color(0.15f, 0.1f, 0.02f, 1f));
            UiFactory.Stretch(boostText.rectTransform, 12f, 4f, 12f, 4f);
            boostChip.gameObject.SetActive(false);

            SceneWiringUtility.SetField(view, "goldText", goldText);
            SceneWiringUtility.SetField(view, "gemsText", gemText);
            SceneWiringUtility.SetField(view, "tokensText", tokenText);
            SceneWiringUtility.SetField(view, "stageText", stageText);
            SceneWiringUtility.SetField(view, "yieldText", yieldText);
            SceneWiringUtility.SetField(view, "boostChip", boostChip.gameObject);
            SceneWiringUtility.SetField(view, "boostText", boostText);

            return view;
        }

        /// <summary>Creates one "icon + value" chip inside the header.</summary>
        private static TextMeshProUGUI CreateCurrencyChip(Transform parent, string name, string iconSprite,
            float xMin, float xMax)
        {
            Image chip = UiFactory.Panel(name, parent, "ui_panel_light", new Color(1f, 1f, 1f, 0.55f));
            UiFactory.Anchor(chip.rectTransform, new Vector2(xMin, 0.55f), new Vector2(xMax, 0.95f));

            Image icon = UiFactory.Icon("Icon", chip.transform, Color.white);
            icon.sprite = UiFactory.LoadSprite(iconSprite);
            UiFactory.Anchor(icon.rectTransform, new Vector2(0.04f, 0.12f), new Vector2(0.30f, 0.88f));

            TextMeshProUGUI value = UiFactory.Text("Value", chip.transform, "0", 32f,
                TextAlignmentOptions.MidlineLeft, TextColor);
            UiFactory.Anchor(value.rectTransform, new Vector2(0.32f, 0f), new Vector2(0.98f, 1f));

            return value;
        }

        // ------------------------------------------------------------------
        // Health bars (shared by units)
        // ------------------------------------------------------------------
        private static HpBarView CreateHpBar(Transform parent, string name)
        {
            GameObject barObject = UiFactory.Node(name, parent);
            HpBarView view = barObject.AddComponent<HpBarView>();

            Image background = UiFactory.Panel("Background", barObject.transform, "ui_panel", new Color(0f, 0f, 0f, 0.65f));
            UiFactory.Stretch(background.rectTransform);

            Image ghost = CreateFilledImage("Ghost", background.transform, Color.white);
            Image fill = CreateFilledImage("Fill", background.transform, new Color(0.35f, 0.85f, 0.45f, 1f));

            TextMeshProUGUI value = UiFactory.Text("Value", barObject.transform, "0 / 0", 22f,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Stretch(value.rectTransform, 6f, 0f, 6f, 0f);

            SceneWiringUtility.SetField(view, "fillImage", fill);
            SceneWiringUtility.SetField(view, "ghostImage", ghost);
            SceneWiringUtility.SetField(view, "valueLabel", value);

            return view;
        }

        /// <summary>A horizontal Filled image that covers the whole parent rect.</summary>
        private static Image CreateFilledImage(string name, Transform parent, Color color)
        {
            Image image = UiFactory.Panel(name, parent, "ui_panel", color);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 1f;
            UiFactory.Stretch(image.rectTransform);
            return image;
        }

        // ------------------------------------------------------------------
        // Combat viewport
        // ------------------------------------------------------------------
        /// <summary>Maps a viewport-relative Y (0 bottom .. 1 top) to a full-screen Y.</summary>
        private static float ScreenY(float viewportFraction)
        {
            return ViewportBottom + (HeaderBottom - ViewportBottom) * viewportFraction;
        }

        private static void BuildViewport(RectTransform safeArea, RectTransform damageRoot,
            out HeroUnitView[] heroViews, out EnemyUnitView enemyView, out FloatingDamageTextPool damagePool,
            out RectTransform enemyAnchor, out RectTransform[] heroAnchors)
        {
            GameObject viewport = UiFactory.Node("Viewport", safeArea);
            UiFactory.Anchor(viewport.GetComponent<RectTransform>(), new Vector2(0f, ViewportBottom), new Vector2(1f, HeaderBottom), 14f, 6f, 14f, 6f);

            Image background = UiFactory.Icon("Background", viewport.transform, new Color(1f, 1f, 1f, 0.9f), false);
            background.sprite = UiFactory.LoadSprite("combat_bg");
            UiFactory.Stretch(background.rectTransform);

            heroViews = new HeroUnitView[PartyConfig.DesiredPartySize];
            heroAnchors = new RectTransform[heroViews.Length];

            for (int i = 0; i < heroViews.Length; i++)
            {
                float laneHeight = 1f / heroViews.Length;
                float laneBottom = 1f - (i + 1) * laneHeight;
                float laneTop = 1f - i * laneHeight;

                GameObject lane = UiFactory.Node($"HeroLane{i}", viewport.transform);
                RectTransform laneRect = lane.GetComponent<RectTransform>();
                UiFactory.Anchor(laneRect, new Vector2(0.02f, laneBottom + 0.02f), new Vector2(0.52f, laneTop - 0.02f));
                CanvasGroup group = lane.AddComponent<CanvasGroup>();

                Image icon = UiFactory.Icon("Icon", lane.transform, Color.white);
                UiFactory.CenterOn(icon.rectTransform, new Vector2(0.22f, 0.58f), new Vector2(120f, 120f));

                TextMeshProUGUI nameLabel = UiFactory.Text("Name", lane.transform, "Hero", 26f,
                    TextAlignmentOptions.MidlineLeft, TextColor);
                UiFactory.Anchor(nameLabel.rectTransform, new Vector2(0.40f, 0.64f), new Vector2(1f, 0.98f));

                HpBarView hpBar = CreateHpBar(lane.transform, "HpBar");
                UiFactory.Anchor(hpBar.GetComponent<RectTransform>(), new Vector2(0.40f, 0.16f), new Vector2(1f, 0.60f));

                HeroUnitView unit = lane.AddComponent<HeroUnitView>();
                SceneWiringUtility.SetField(unit, "heroIndex", i);
                SceneWiringUtility.SetField(unit, "iconImage", icon);
                SceneWiringUtility.SetField(unit, "hpBar", hpBar);
                SceneWiringUtility.SetField(unit, "nameLabel", nameLabel);
                SceneWiringUtility.SetField(unit, "canvasGroup", group);
                heroViews[i] = unit;

                GameObject heroAnchor = UiFactory.Node($"HeroDamageAnchor{i}", damageRoot);
                UiFactory.CenterOn(heroAnchor.GetComponent<RectTransform>(),
                    new Vector2(0.16f, ScreenY(1f - (i + 0.5f) * laneHeight)), new Vector2(1f, 1f));
                heroAnchors[i] = heroAnchor.GetComponent<RectTransform>();
            }

            enemyView = BuildEnemySlot(viewport.transform);

            GameObject enemyAnchorObject = UiFactory.Node("EnemyDamageAnchor", damageRoot);
            UiFactory.CenterOn(enemyAnchorObject.GetComponent<RectTransform>(), new Vector2(0.78f, ScreenY(0.5f)), new Vector2(1f, 1f));
            enemyAnchor = enemyAnchorObject.GetComponent<RectTransform>();

            damagePool = BuildDamageTextPool(damageRoot);
        }

        private static EnemyUnitView BuildEnemySlot(Transform parent)
        {
            GameObject slot = UiFactory.Node("EnemySlot", parent);
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            UiFactory.Anchor(slotRect, new Vector2(0.54f, 0.10f), new Vector2(0.98f, 0.92f));

            Image bossFrame = UiFactory.Panel("BossFrame", slot.transform, "ui_panel_bordered", new Color(1f, 0.85f, 0.35f, 0.85f));
            UiFactory.Stretch(bossFrame.rectTransform);
            bossFrame.enabled = false;

            Image icon = UiFactory.Icon("Icon", slot.transform, Color.white);
            UiFactory.CenterOn(icon.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(220f, 220f));
            icon.enabled = false;

            TextMeshProUGUI nameLabel = UiFactory.Text("Name", slot.transform, "Enemy", 28f,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(nameLabel.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 1f));

            HpBarView hpBar = CreateHpBar(slot.transform, "HpBar");
            UiFactory.Anchor(hpBar.GetComponent<RectTransform>(), new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.20f));

            EnemyUnitView view = slot.AddComponent<EnemyUnitView>();
            SceneWiringUtility.SetField(view, "spriteImage", icon);
            SceneWiringUtility.SetField(view, "hpBar", hpBar);
            SceneWiringUtility.SetField(view, "nameLabel", nameLabel);
            SceneWiringUtility.SetField(view, "root", slotRect);
            SceneWiringUtility.SetField(view, "bossFrame", bossFrame);

            return view;
        }

        private static FloatingDamageTextPool BuildDamageTextPool(RectTransform damageRoot)
        {
            GameObject template = UiFactory.Node("DamageTextTemplate", damageRoot);
            FloatingDamageTextView textView = template.AddComponent<FloatingDamageTextView>();
            TextMeshProUGUI label = UiFactory.Text("Label", template.transform, "0", 40f,
                TextAlignmentOptions.Center, Color.white);
            UiFactory.Stretch(label.rectTransform);

            SceneWiringUtility.SetField(textView, "label", label);
            SceneWiringUtility.SetField(textView, "rectTransform", template.GetComponent<RectTransform>());
            template.SetActive(false);

            GameObject poolObject = UiFactory.Node("DamageTextPool", damageRoot);
            FloatingDamageTextPool pool = poolObject.AddComponent<FloatingDamageTextPool>();
            SceneWiringUtility.SetField(pool, "prefab", textView);
            return pool;
        }

        // ------------------------------------------------------------------
        // Control dock
        // ------------------------------------------------------------------
        private static TabController BuildDock(RectTransform safeArea, PartyConfig partyConfig,
            List<PrestigeUpgradeData> prestigeUpgrades)
        {
            Image dock = UiFactory.Panel("Dock", safeArea, "ui_panel_light", new Color(1f, 1f, 1f, 0.98f));
            UiFactory.Anchor(dock.rectTransform, Vector2.zero, new Vector2(1f, ViewportBottom), 14f, 14f, 14f, 6f);
            TabController tabs = dock.gameObject.AddComponent<TabController>();

            GameObject panelsRoot = UiFactory.Node("Panels", dock.transform);
            UiFactory.Anchor(panelsRoot.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.86f), 12f, 8f, 12f, 6f);

            GameObject upgradePanel = BuildUpgradePanel(panelsRoot.transform, partyConfig);
            GameObject ascensionPanel = BuildAscensionPanel(panelsRoot.transform, prestigeUpgrades);
            GameObject shopPanel = BuildShopPanel(panelsRoot.transform);

            // Tab bar sits at the bottom of the dock (nearest the thumb on mobile).
            GameObject tabBar = UiFactory.Node("TabBar", dock.transform);
            UiFactory.Anchor(tabBar.GetComponent<RectTransform>(), new Vector2(0f, 0.87f), Vector2.one, 12f, 6f, 12f, 8f);

            List<TabController.TabDefinition> definitions = new List<TabController.TabDefinition>
            {
                CreateTab(tabBar.transform, "Upgrades", 0, upgradePanel),
                CreateTab(tabBar.transform, "Ascension", 1, ascensionPanel),
                CreateTab(tabBar.transform, "Shop", 2, shopPanel)
            };

            SetTabs(tabs, definitions);
            return tabs;
        }

        private static void SetTabs(TabController tabs, List<TabController.TabDefinition> definitions)
        {
            SceneWiringUtility.SetField(tabs, "tabs", definitions);
            SceneWiringUtility.SetField(tabs, "activeTabSprite", UiFactory.LoadSprite("ui_tab_on"), required: false);
            SceneWiringUtility.SetField(tabs, "inactiveTabSprite", UiFactory.LoadSprite("ui_tab_off"), required: false);
        }

        private static TabController.TabDefinition CreateTab(Transform parent, string label, int index, GameObject panel)
        {
            float width = 1f / 3f;
            float xMin = index * width;

            Button button = UiFactory.Button($"Tab{label}", parent, label, "ui_tab_off", 30f, TextColor, null);
            UiFactory.Anchor(button.GetComponent<RectTransform>(), new Vector2(xMin, 0f), new Vector2(xMin + width, 1f), 4f, 0f, 4f, 0f);

            return new TabController.TabDefinition
            {
                tabName = label,
                button = button,
                buttonBackground = button.targetGraphic as Image,
                buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>(),
                panel = panel
            };
        }

        private static GameObject BuildUpgradePanel(Transform parent, PartyConfig partyConfig)
        {
            GameObject panel = UiFactory.Node("UpgradesPanel", parent);
            UiFactory.Stretch(panel.GetComponent<RectTransform>());
            UpgradePanelUI ui = panel.AddComponent<UpgradePanelUI>();

            TextMeshProUGUI title = UiFactory.Text("Title", panel.transform,
                "Spend gold to raise hero ATK / HP / DEF", 22f, TextAlignmentOptions.MidlineLeft, DimTextColor);
            UiFactory.Anchor(title.rectTransform, new Vector2(0.02f, 0.93f), new Vector2(0.98f, 1f));

            GameObject list = UiFactory.Node("Rows", panel.transform);
            UiFactory.Anchor(list.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.92f));
            UiFactory.VerticalStack(list, 10f, new RectOffset(2, 2, 2, 2));

            HeroUpgradeRowUI[] rows = new HeroUpgradeRowUI[PartyConfig.DesiredPartySize];

            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = CreateHeroUpgradeRow(list.transform, i, partyConfig);
            }

            SceneWiringUtility.SetField(ui, "rows", rows);
            return panel;
        }

        private static HeroUpgradeRowUI CreateHeroUpgradeRow(Transform parent, int index, PartyConfig partyConfig)
        {
            GameObject row = UiFactory.Node($"HeroRow{index}", parent);
            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.minHeight = 240f;
            layout.preferredHeight = 240f;

            Image background = UiFactory.Panel("Background", row.transform, "ui_panel", new Color(1f, 1f, 1f, 0.30f));
            UiFactory.Stretch(background.rectTransform);

            HeroData hero = partyConfig != null ? partyConfig.GetHero(index) : null;
            TextMeshProUGUI nameLabel = UiFactory.Text("Name", row.transform,
                hero != null ? hero.HeroName : $"Hero {index + 1}", 28f, TextAlignmentOptions.MidlineLeft, TextColor);
            UiFactory.Anchor(nameLabel.rectTransform, new Vector2(0.03f, 0.76f), new Vector2(0.97f, 0.99f));

            GameObject blocks = UiFactory.Node("Blocks", row.transform);
            UiFactory.Anchor(blocks.GetComponent<RectTransform>(), new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.74f));
            UiFactory.HorizontalStack(blocks, 8f);

            HeroUpgradeRowUI.StatBlock[] statBlocks =
            {
                CreateStatBlock(blocks.transform, HeroStatType.Attack, "ui_icon_gold"),
                CreateStatBlock(blocks.transform, HeroStatType.Health, "ui_icon_gem"),
                CreateStatBlock(blocks.transform, HeroStatType.Defense, "ui_icon_token")
            };

            HeroUpgradeRowUI ui = row.AddComponent<HeroUpgradeRowUI>();
            SceneWiringUtility.SetField(ui, "heroIndex", index);
            SceneWiringUtility.SetField(ui, "heroNameLabel", nameLabel);
            SceneWiringUtility.SetField(ui, "blocks", statBlocks);
            return ui;
        }

        private static HeroUpgradeRowUI.StatBlock CreateStatBlock(Transform parent, HeroStatType statType, string iconSprite)
        {
            GameObject block = UiFactory.Node($"Stat{statType}", parent);

            Image background = UiFactory.Panel("Background", block.transform, "ui_panel_light", new Color(1f, 1f, 1f, 0.28f));
            UiFactory.Stretch(background.rectTransform);

            TextMeshProUGUI levelLabel = UiFactory.Text("Level", block.transform, "Lv 0", 26f,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(levelLabel.rectTransform, new Vector2(0.04f, 0.70f), new Vector2(0.96f, 0.98f));

            TextMeshProUGUI effectLabel = UiFactory.Text("Effect", block.transform, "+0%", 18f,
                TextAlignmentOptions.Center, DimTextColor);
            UiFactory.Anchor(effectLabel.rectTransform, new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.70f));

            TextMeshProUGUI costLabel = UiFactory.Text("Cost", block.transform, "0", 20f,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(costLabel.rectTransform, new Vector2(0.04f, 0.32f), new Vector2(0.96f, 0.52f));

            Button plusOne = UiFactory.Button("PlusOne", block.transform, "+1", "ui_button", 22f, TextColor, null);
            UiFactory.Anchor(plusOne.GetComponent<RectTransform>(), new Vector2(0.05f, 0.04f), new Vector2(0.47f, 0.30f));

            Button plusTen = UiFactory.Button("PlusTen", block.transform, "+10", "ui_button_gold", 22f, TextColor, null);
            UiFactory.Anchor(plusTen.GetComponent<RectTransform>(), new Vector2(0.53f, 0.04f), new Vector2(0.95f, 0.30f));

            return new HeroUpgradeRowUI.StatBlock
            {
                statType = statType,
                plusOneButton = plusOne,
                plusTenButton = plusTen,
                levelLabel = levelLabel,
                costLabel = costLabel,
                effectLabel = effectLabel,
                iconImage = null,
                iconSprite = UiFactory.LoadSprite(iconSprite)
            };
        }

        private static GameObject BuildAscensionPanel(Transform parent, List<PrestigeUpgradeData> prestigeUpgrades)
        {
            GameObject panel = UiFactory.Node("AscensionPanel", parent);
            UiFactory.Stretch(panel.GetComponent<RectTransform>());
            AscensionPanelUI ui = panel.AddComponent<AscensionPanelUI>();

            TextMeshProUGUI yieldLabel = UiFactory.Text("Yield", panel.transform, "Ascend for 0 tokens", 30f,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(yieldLabel.rectTransform, new Vector2(0.03f, 0.88f), new Vector2(0.97f, 1f));

            TextMeshProUGUI requirementLabel = UiFactory.Text("Requirement", panel.transform, "Best stage 1", 22f,
                TextAlignmentOptions.Center, DimTextColor);
            UiFactory.Anchor(requirementLabel.rectTransform, new Vector2(0.03f, 0.79f), new Vector2(0.97f, 0.88f));

            Button ascendButton = UiFactory.Button("AscendButton", panel.transform, "ASCEND", "ui_button_gold", 34f, TextColor, null);
            UiFactory.Anchor(ascendButton.GetComponent<RectTransform>(), new Vector2(0.24f, 0.60f), new Vector2(0.76f, 0.77f));
            TextMeshProUGUI ascendButtonLabel = ascendButton.GetComponentInChildren<TextMeshProUGUI>();

            GameObject list = UiFactory.Node("PrestigeRows", panel.transform);
            UiFactory.Anchor(list.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.57f));
            UiFactory.VerticalStack(list, 8f, new RectOffset(2, 2, 2, 2));

            PrestigeUpgradeRowUI[] rows = new PrestigeUpgradeRowUI[prestigeUpgrades.Count];

            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = CreatePrestigeRow(list.transform, i, prestigeUpgrades[i]);
            }

            SceneWiringUtility.SetField(ui, "yieldLabel", yieldLabel);
            SceneWiringUtility.SetField(ui, "requirementLabel", requirementLabel);
            SceneWiringUtility.SetField(ui, "ascendButton", ascendButton);
            SceneWiringUtility.SetField(ui, "ascendButtonLabel", ascendButtonLabel);
            SceneWiringUtility.SetField(ui, "prestigeRows", rows);
            return panel;
        }

        private static PrestigeUpgradeRowUI CreatePrestigeRow(Transform parent, int index, PrestigeUpgradeData data)
        {
            GameObject row = UiFactory.Node($"PrestigeRow{index}", parent);
            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.minHeight = 116f;
            layout.preferredHeight = 116f;

            Image background = UiFactory.Panel("Background", row.transform, "ui_panel", new Color(1f, 1f, 1f, 0.30f));
            UiFactory.Stretch(background.rectTransform);

            TextMeshProUGUI nameLabel = UiFactory.Text("Name", row.transform,
                data != null ? data.DisplayName : $"Upgrade {index + 1}", 26f, TextAlignmentOptions.MidlineLeft, TextColor);
            UiFactory.Anchor(nameLabel.rectTransform, new Vector2(0.03f, 0.58f), new Vector2(0.56f, 0.96f));

            TextMeshProUGUI effectLabel = UiFactory.Text("Effect", row.transform, "+0%", 20f,
                TextAlignmentOptions.MidlineLeft, DimTextColor);
            UiFactory.Anchor(effectLabel.rectTransform, new Vector2(0.03f, 0.12f), new Vector2(0.56f, 0.55f));

            TextMeshProUGUI levelLabel = UiFactory.Text("Level", row.transform, "Lv 0", 24f,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(levelLabel.rectTransform, new Vector2(0.56f, 0.58f), new Vector2(0.76f, 0.96f));

            TextMeshProUGUI costLabel = UiFactory.Text("Cost", row.transform, "1 token", 20f,
                TextAlignmentOptions.Center, DimTextColor);
            UiFactory.Anchor(costLabel.rectTransform, new Vector2(0.56f, 0.12f), new Vector2(0.76f, 0.55f));

            Button buyButton = UiFactory.Button("BuyButton", row.transform, "BUY", "ui_button", 24f, TextColor, null);
            UiFactory.Anchor(buyButton.GetComponent<RectTransform>(), new Vector2(0.78f, 0.16f), new Vector2(0.97f, 0.86f));

            PrestigeUpgradeRowUI ui = row.AddComponent<PrestigeUpgradeRowUI>();
            SceneWiringUtility.SetField(ui, "nameLabel", nameLabel);
            SceneWiringUtility.SetField(ui, "effectLabel", effectLabel);
            SceneWiringUtility.SetField(ui, "levelLabel", levelLabel);
            SceneWiringUtility.SetField(ui, "costLabel", costLabel);
            SceneWiringUtility.SetField(ui, "buyButton", buyButton);
            return ui;
        }

        private static GameObject BuildShopPanel(Transform parent)
        {
            GameObject panel = UiFactory.Node("ShopPanel", parent);
            UiFactory.Stretch(panel.GetComponent<RectTransform>());
            ShopPanelUI ui = panel.AddComponent<ShopPanelUI>();

            TextMeshProUGUI title = UiFactory.Text("Title", panel.transform, "Shop", 30f,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(title.rectTransform, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 1f));

            TextMeshProUGUI statusLabel = UiFactory.Text("Status", panel.transform, "Watch an ad for Gold x2", 22f,
                TextAlignmentOptions.Center, DimTextColor);
            UiFactory.Anchor(statusLabel.rectTransform, new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.83f));

            Button watchAdButton = UiFactory.Button("WatchAdButton", panel.transform, "WATCH AD", "ui_button_gold", 32f, TextColor, null);
            UiFactory.Anchor(watchAdButton.GetComponent<RectTransform>(), new Vector2(0.18f, 0.30f), new Vector2(0.82f, 0.56f));
            TextMeshProUGUI watchAdLabel = watchAdButton.GetComponentInChildren<TextMeshProUGUI>();

            TextMeshProUGUI gemsLabel = UiFactory.Text("Gems", panel.transform, "0 gems", 22f,
                TextAlignmentOptions.Center, DimTextColor);
            UiFactory.Anchor(gemsLabel.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.26f));

            SceneWiringUtility.SetField(ui, "watchAdButton", watchAdButton);
            SceneWiringUtility.SetField(ui, "watchAdLabel", watchAdLabel);
            SceneWiringUtility.SetField(ui, "statusLabel", statusLabel);
            SceneWiringUtility.SetField(ui, "gemsLabel", gemsLabel);
            return panel;
        }

        private static OfflineRewardsPopup BuildOfflinePopup(RectTransform canvasRoot)
        {
            GameObject host = UiFactory.Node("OfflineRewardsPopup", canvasRoot);
            OfflineRewardsPopup ui = host.AddComponent<OfflineRewardsPopup>();

            GameObject dialog = UiFactory.Node("Dialog", host.transform);
            UiFactory.Anchor(dialog.GetComponent<RectTransform>(), new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.72f));

            Image background = UiFactory.Panel("Background", dialog.transform, "ui_panel_bordered",
                new Color(1f, 1f, 1f, 0.99f), raycast: true);
            UiFactory.Stretch(background.rectTransform);

            TextMeshProUGUI title = UiFactory.Text("Title", dialog.transform, "Welcome back!", 38f,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(title.rectTransform, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.98f));

            TextMeshProUGUI timeLabel = UiFactory.Text("Time", dialog.transform, "You were away for 1h", 26f,
                TextAlignmentOptions.Center, DimTextColor);
            UiFactory.Anchor(timeLabel.rectTransform, new Vector2(0.05f, 0.66f), new Vector2(0.95f, 0.82f));

            TextMeshProUGUI goldLabel = UiFactory.Text("Gold", dialog.transform, "+0 gold", 42f,
                TextAlignmentOptions.Center, new Color(0.98f, 0.82f, 0.30f, 1f));
            UiFactory.Anchor(goldLabel.rectTransform, new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.64f));

            TextMeshProUGUI capNote = UiFactory.Text("CapNote", dialog.transform, "", 20f,
                TextAlignmentOptions.Center, DimTextColor);
            UiFactory.Anchor(capNote.rectTransform, new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.44f));

            Button claimButton = UiFactory.Button("ClaimButton", dialog.transform, "CLAIM", "ui_button_gold", 32f, TextColor, null);
            UiFactory.Anchor(claimButton.GetComponent<RectTransform>(), new Vector2(0.20f, 0.06f), new Vector2(0.80f, 0.28f));

            SceneWiringUtility.SetField(ui, "root", dialog);
            SceneWiringUtility.SetField(ui, "titleLabel", title);
            SceneWiringUtility.SetField(ui, "timeLabel", timeLabel);
            SceneWiringUtility.SetField(ui, "goldLabel", goldLabel);
            SceneWiringUtility.SetField(ui, "capNoteLabel", capNote);
            SceneWiringUtility.SetField(ui, "claimButton", claimButton);

            dialog.SetActive(false);
            return ui;
        }

        private static ToastUI BuildToast(RectTransform canvasRoot)
        {
            GameObject host = UiFactory.Node("Toast", canvasRoot);
            CanvasGroup group = host.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            TextMeshProUGUI label = UiFactory.Text("Message", host.transform, "", 26f,
                TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.6f, 1f));
            UiFactory.Anchor(label.rectTransform, new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.40f));

            ToastUI ui = host.AddComponent<ToastUI>();
            SceneWiringUtility.SetField(ui, "canvasGroup", group);
            SceneWiringUtility.SetField(ui, "label", label);
            return ui;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // Main scene first so it becomes the build entry point.
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    scenes.RemoveAt(i);
                    break;
                }
            }

            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}