using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    public static partial class MvpSceneBuilder
    {
        public const string ScenePath = "Assets/IdleRPG/Scenes/Main.unity";
        private const string BootScenePath = "Assets/IdleRPG/Scenes/Boot.unity";

        private const float ReferenceWidth = 1080f;
        private const float ReferenceHeight = 1920f;

        // The two chrome bands are FIXED heights, not screen fractions.
        //
        // Why: a fraction makes the bars change size with the screen. On a tall phone the extra height was split
        // between the bars and the page, and on a short screen the top bar could fall below the 56-unit currency
        // numbers. One rule now: extra height belongs to the page, the bars never move.
        // The values reproduce the 1080x1920 design (header 6% and nav 12% of 1920), so nothing shifts on 9:16.
        private const float HeaderBandHeight = 115f;   // top bar: the currency chips + boost, pinned to the top

        // Party XP bar band: a thin strip between the bottom of the arena and the top of the ability strip.
        // The arena starts at XP band top, so the bar can never sit over the fight or hide the hero buttons.
        private const float XpBandBottom = BattleViewportBottom;   // sits on top of the ability strip
        private const float XpBandTop = 0.417f;                    // the arena starts here

        // Retired nav bar height. BuildNavBar is no longer called (the lobby owns navigation), but the method is
        // kept for reference, so its constant stays.
        private const float NavBandHeight = 230f;

        // Bands inside the battle page (page coords): log 0..LogTop, ability strip 0.28..0.39, party XP bar
        // 0.39..0.417, viewport 0.417..StageRowMinY, stage/wave row 0.93..0.99.
        //
        // LogTop used to be 0.34, with a 79-unit tab bar above the log. The bar is gone and one hero button
        // lives in its place, so the band had to grow: 0.11 of the page is ~174 units, enough for a portrait and
        // three short labels without them touching. The log gives up ~90 units it did not need - it scrolls.
        private const float LogTop = 0.28f;
        private const float BattleViewportBottom = 0.39f;
        private const float StageRowMinY = 0.93f;
        private const float StageRowMaxY = 0.99f;

        /// <summary>Modal canvas order. Above the main canvas (0) and the damage canvas (10).</summary>
        private const int ModalSortingOrder = 100;

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

            // The builder READS balance data, it never writes it. This line used to regenerate every data asset,
            // so each rebuild silently reset live numbers: enemy stats went back to generator values (Slime 57 -> 60)
            // and the stage ladder lost its wave entries. Generate data on purpose, never as a side effect of a scene.
            PlaceholderSpriteGenerator.GenerateAll();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            PrepareCamera();

            // Loaded *after* the scene swap: a reimport in the same tick destroys asset
            // instances loaded earlier, which silently produced an unwired scene before.
            BalanceConfig balance = SceneWiringUtility.LoadBalance();
            WaveConfig waveConfig = SceneWiringUtility.LoadWaveConfig();
            PartyConfig partyConfig = SceneWiringUtility.LoadPartyConfig();
            FormationData formationConfig = SceneWiringUtility.LoadFormationConfig();
            List<StatUpgradeData> statTracks = SceneWiringUtility.LoadStatTracks();
            List<PrestigeUpgradeData> prestigeUpgrades = SceneWiringUtility.LoadPrestigeUpgrades();

            if (balance == null || waveConfig == null || partyConfig == null || statTracks.Count == 0 || prestigeUpgrades.Count == 0)
            {
                Debug.LogError("[MvpSceneBuilder] Data assets incomplete; run Tools > Idle RPG > Generate Data Assets.");
                return;
            }

            GameManager gameManager = SceneWiringUtility.CreateGameManagerObject(
                balance, waveConfig, partyConfig, statTracks, prestigeUpgrades,
                enableDebugLogger: false, enableHotkeys: true, formationConfig);

            if (gameManager.Balance == null || gameManager.Combat == null || gameManager.Party == null)
            {
                Debug.LogError("[MvpSceneBuilder] GameManager wiring failed - aborting build so the existing scene is not overwritten.");
                return;
            }

            GameObject hudRoot = new GameObject("HUD");
            HudController hud = hudRoot.AddComponent<HudController>();

            // The lobby (start screen + TRAINING tab). It builds its own canvas at Awake and borrows the shared
            // header. It belongs here, in the builder, so a scene rebuild can never drop it again.
            GameObject startScreen = new GameObject("StartScreen");
            StartScreenUI startScreenUi = startScreen.AddComponent<StartScreenUI>();

            // Chapter art for the lobby centre panel. Referenced directly: it lives in the Art root, not
            // the Placeholder folder, so UiFactory.LoadSprite cannot find it.
            Sprite chapterArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/IdleRPG/Art/Greenwake1-clean 1.png");
            if (chapterArt != null)
            {
                SceneWiringUtility.SetField(startScreenUi, "chapterArt", chapterArt, required: false);
            }

            Sprite chapterTitleArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/IdleRPG/Art/Greenwake title 1.png");
            if (chapterTitleArt != null)
            {
                SceneWiringUtility.SetField(startScreenUi, "chapterTitleArt", chapterTitleArt, required: false);
            }

            // Bottom-tab icons, sliced from one sheet. Order in the sheet (reading order): battle, ascend,
            // train, shop, party; each field below takes the matching sub-sprite by name.
            System.Func<string, Sprite> tabIcon = (spriteName) =>
            {
                UnityEngine.Object[] sheet = AssetDatabase.LoadAllAssetsAtPath("Assets/IdleRPG/Art/Button Icons.png");
                for (int i = 0; i < sheet.Length; i++)
                {
                    Sprite candidate = sheet[i] as Sprite;
                    if (candidate != null && candidate.name == spriteName) { return candidate; }
                }
                return null;
            };
            SceneWiringUtility.SetField(startScreenUi, "tabBattleIcon", tabIcon("icon_battle"), required: false);
            SceneWiringUtility.SetField(startScreenUi, "tabAscendIcon", tabIcon("icon_ascend"), required: false);
            SceneWiringUtility.SetField(startScreenUi, "tabTrainIcon", tabIcon("icon_train"), required: false);
            SceneWiringUtility.SetField(startScreenUi, "tabShopIcon", tabIcon("icon_shop"), required: false);
            SceneWiringUtility.SetField(startScreenUi, "tabPartyIcon", tabIcon("icon_party"), required: false);

            RectTransform canvasRoot = CreateCanvas(hudRoot);
            RectTransform damageRoot = CreateDamageCanvas(hudRoot);
            RectTransform modalCanvas = CreateModalCanvas(hudRoot);
            RectTransform safeArea = CreateSafeArea(canvasRoot);

            RectTransform battleStage = CreateBattleStage(
                out Camera stageCamera, out Canvas stageCanvas, out GraphicRaycaster stageRaycaster);

            // The modal canvas gets its own safe box. A card near a screen edge (the ranks sheet, the offline claim)
            // must clear a notch or a gesture bar just like the HUD bars do, so every card hangs off this node and
            // never off the raw canvas.
            RectTransform modalRoot = CreateSafeArea(modalCanvas);

            HudHeaderUI header = BuildHeader(safeArea);

            GameObject battlePage = CreatePage("BattlePage", safeArea);

            BuildXpBar(battlePage.GetComponent<RectTransform>());
            BuildStageRow(battlePage.GetComponent<RectTransform>());

            BuildViewport(battlePage.GetComponent<RectTransform>(), damageRoot,
                out FormationBoardView formationBoard, out EnemyStackView enemyStack,
                out FloatingDamageTextPool damagePool, out RectTransform enemyAnchor,
                out RectTransform viewportRect, out RawImage stageDisplay);

            BattleStageSurface stageSurface = stageDisplay.gameObject.AddComponent<BattleStageSurface>();
            SceneWiringUtility.SetField(stageSurface, "stageCamera", stageCamera);
            SceneWiringUtility.SetField(stageSurface, "stageCanvas", stageCanvas);
            SceneWiringUtility.SetField(stageSurface, "stageRoot", battleStage);
            SceneWiringUtility.SetField(stageSurface, "viewport", viewportRect);
            SceneWiringUtility.SetField(stageSurface, "display", stageDisplay);
            SceneWiringUtility.SetField(stageSurface, "stageRaycaster", stageRaycaster);
            SceneWiringUtility.SetField(stageSurface, "damageTextPool", damagePool);
            RectTransform battleRect = battlePage.GetComponent<RectTransform>();
            CombatLogUI combatLog = BuildCombatLog(battleRect);

            // The ability strip sits in the band between the viewport and the log. It used to be a second tab
            // beside the log, which meant the buttons were invisible during a fight - the one moment they matter.
            AbilityPanelUI abilityStrip = BuildAbilityStrip(battleRect, partyConfig.ValidHeroCount);
            // The old management page (UPGRADES / PERKS / SHOP) and its nav bar are RETIRED - the lobby owns the
            // shell now. Their builders stay in MvpSceneBuilder.Panels.cs for reference; nothing calls them, and the
            // camp layer takes a null ScreenController (it only used it to return to the battle page).
            BuildCampLayer(battleRect, abilityStrip, formationBoard, enemyStack, null, hudRoot);
            OfflineRewardsPopup offlinePopup = BuildOfflinePopup(modalRoot);
            DefeatModalUI defeatModal = BuildDefeatModal(modalRoot);
            BuildBoonCard(modalRoot);
            ToastUI toast = BuildToast(safeArea);
            EnsureEventSystem();

            SceneWiringUtility.SetField(hud, "gameManager", gameManager);
            SceneWiringUtility.SetField(hud, "modalRoot", modalRoot);
            SceneWiringUtility.SetField(hud, "vfxRoot", damageRoot);
            SceneWiringUtility.SetField(hud, "formationBoard", formationBoard);
            SceneWiringUtility.SetField(hud, "enemyStack", enemyStack);
            SceneWiringUtility.SetField(hud, "damageTextPool", damagePool);
            SceneWiringUtility.SetField(damagePool, "enemyAnchor", enemyAnchor);
            SceneWiringUtility.SetField(formationBoard, "damagePool", damagePool, required: false);

            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(combatLog);
            EditorUtility.SetDirty(header);
            EditorUtility.SetDirty(offlinePopup);
            EditorUtility.SetDirty(defeatModal);
            EditorUtility.SetDirty(toast);

            EnsureFolder(Path.GetDirectoryName(ScenePath).Replace('\\', '/'));

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"[MvpSceneBuilder] Failed to save {ScenePath}.");
                return;
            }

            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[MvpSceneBuilder] Built {ScenePath}: header + viewport (3 heroes, 1-3 stacked enemies) + dock (Upgrades/Ascension/Shop) + damage canvas + popups.");
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

        /// <summary>
        /// Builds the off-screen battle stage. The stage canvas is a separate root because a nested
        /// canvas would inherit the HUD's Overlay mode. Its camera renders only UI-layer graphics
        /// into the runtime render texture assigned by <see cref="BattleStageSurface"/>.
        /// </summary>
        private static RectTransform CreateBattleStage(out Camera stageCamera, out Canvas stageCanvas,
            out GraphicRaycaster stageRaycaster)
        {
            GameObject stageObject = new GameObject("BattleStage",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                stageObject.layer = uiLayer;
            }

            stageCanvas = stageObject.GetComponent<Canvas>();
            stageCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            stageCanvas.planeDistance = 100f;
            stageCanvas.sortingOrder = 0;

            CanvasScaler scaler = stageObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            GameObject cameraObject = new GameObject("BattleStageCamera");
            // Keep the camera OUT of the canvas hierarchy. A Screen Space - Camera canvas drives its
            // root transform, so a child camera inherits the canvas scale (0.01 here) and moves with it.

            if (uiLayer >= 0)
            {
                cameraObject.layer = uiLayer;
            }

            stageCamera = cameraObject.AddComponent<Camera>();
            stageCamera.orthographic = true;
            stageCamera.orthographicSize = 5f;
            stageCamera.clearFlags = CameraClearFlags.SolidColor;
            stageCamera.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
            stageCamera.cullingMask = uiLayer >= 0 ? 1 << uiLayer : ~0;
            stageCamera.depth = -2f;
            stageCamera.allowHDR = false;
            stageCamera.allowMSAA = false;
            stageCamera.nearClipPlane = 0.3f;
            stageCamera.farClipPlane = 1000f;
            stageCamera.transform.position = new Vector3(0f, 0f, -10f);
            stageCanvas.worldCamera = stageCamera;

            // URP is an optional package assembly from the editor assembly's point of view. Add its
            // camera data by name so this builder stays usable in a Built-in debug project too.
            System.Type cameraDataType = System.Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");

            if (cameraDataType != null && cameraObject.GetComponent(cameraDataType) == null)
            {
                cameraObject.AddComponent(cameraDataType);
            }

            stageRaycaster = stageObject.AddComponent<GraphicRaycaster>();
            stageRaycaster.enabled = false;

            return stageObject.GetComponent<RectTransform>();
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

            // Match WIDTH (0), not a width/height blend.
            //
            // Every fixed size in the UI (a 144-unit button, a 56-unit number, a 14-unit margin) is calibrated to the
            // 1080-wide reference, and every band is flexible. Matching width therefore keeps taps and text the same
            // on every phone, and a taller phone shows MORE content instead of bigger bars.
            // Downside, stated plainly: on a short, wide screen (a tablet in portrait) the bands get less height than
            // designed, so pages scroll more. Phones are the target; tablets are a separate job if they ever matter.
            scaler.matchWidthOrHeight = 0f;

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
            scaler.matchWidthOrHeight = 0f;   // match width, same reason as the main canvas

            // No GraphicRaycaster: damage numbers must never eat input.
            return canvasObject.GetComponent<RectTransform>();
        }

        /// <summary>
        /// Modal overlays (the offline claim) get their own canvas with the highest sorting order, so nothing on
        /// the HUD - or on the damage canvas above it - can draw over a dialog. It keeps a GraphicRaycaster so the
        /// dialog's buttons work; the scrim inside blocks clicks on everything behind it.
        /// </summary>
        private static RectTransform CreateModalCanvas(GameObject host)
        {
            GameObject canvasObject = UiFactory.Node("ModalCanvas", host.transform);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ModalSortingOrder;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;   // match width, same reason as the main canvas

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvasObject.GetComponent<RectTransform>();
        }

        /// <summary>
        /// A full-page container under the fixed header. There is no bottom bar any more (the lobby owns navigation),
        /// so the page runs to the bottom of the safe area and every extra unit of screen height lands in it.
        /// </summary>
        private static GameObject CreatePage(string name, RectTransform safeArea)
        {
            GameObject page = UiFactory.Node(name, safeArea);
            UiFactory.Anchor(page.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                0f, UiSpace.Large, 0f, HeaderBandHeight);
            return page;
        }

        /// <summary>
        /// The party XP bar: a thin band between the header and the page. Built here, not hand-placed, so a scene
        /// rebuild can never drop it; <see cref="IdleRPG.UI.XpBarUI"/> draws its own level / fill / numbers.
        /// </summary>
        private static void BuildXpBar(RectTransform page)
        {
            GameObject bar = UiFactory.Node("XpBar", page);

            UiFactory.Anchor(bar.GetComponent<RectTransform>(),
                new Vector2(0f, XpBandBottom), new Vector2(1f, XpBandTop),
                UiSpace.ScreenMargin, UiSpace.Hairline, UiSpace.ScreenMargin, UiSpace.Hairline);

            bar.AddComponent<IdleRPG.UI.XpBarUI>();
        }

        /// <summary>
        /// Pins a full-width bar of a FIXED height to the top or the bottom of its parent, with the shared side
        /// margin and edge gap. The header uses this so it cannot change size with the screen.
        /// </summary>
        private static void PinBar(RectTransform rect, bool top, float height, float sideMargin, float edgeGap)
        {
            rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rect.anchoredPosition = new Vector2(0f, top ? -edgeGap : edgeGap);
            rect.sizeDelta = new Vector2(-2f * sideMargin, height - edgeGap);
        }

        /// <summary>
        /// A stretched safe box under <paramref name="canvasRoot"/>: the HUD bars and every card hang off one of
        /// these, so a notch, a Dynamic Island or a gesture bar can never clip them.
        ///
        /// The rect is authored STRETCHED (full parent) on purpose. Before, it kept Unity's default 100x100 rect
        /// and only became correct once <see cref="SafeAreaFitter"/> ran in Play Mode, so the HUD was unreadable in
        /// Edit Mode. Now the authored rect is the no-notch case, and the fitter only tightens it on a real device.
        /// </summary>
        private static RectTransform CreateSafeArea(RectTransform canvasRoot)
        {
            GameObject safeArea = UiFactory.Node("SafeArea", canvasRoot);
            RectTransform rect = safeArea.GetComponent<RectTransform>();
            UiFactory.Stretch(rect);
            safeArea.AddComponent<SafeAreaFitter>();
            return rect;
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

            // The project uses the new Input System exclusively, so the UI module needs its own
            // action asset. Generated defaults are fine, but they MUST be persisted as a real asset:
            // an in-memory asset serializes as a dangling reference and silently kills all clicks.
            module.AssignDefaultActions();
            PersistUiActionsAsset(module);

            // Runtime safety net for the same failure mode.
            eventSystemObject.AddComponent<IdleRPG.UI.UiInputBootstrap>();
        }

        /// <summary>Saves the generated UI action asset so the EventSystem reference survives.</summary>
        private static void PersistUiActionsAsset(InputSystemUIInputModule module)
        {
            if (module == null || module.actionsAsset == null)
            {
                return;
            }

            const string actionsPath = "Assets/IdleRPG/Settings/UiInputActions.inputactions";
            EnsureFolder("Assets/IdleRPG/Settings");

            if (!EditorUtility.IsPersistent(module.actionsAsset))
            {
                // Write via ToJson + import. AssetDatabase.CreateAsset refuses '.inputactions' files, and an
                // in-memory asset cannot be serialized into the scene: it used to save as a dangling reference
                // that made InputSystemUIInputModule assert on enable ("Map must be contained in state") and
                // froze the first frame of play mode.
                File.WriteAllText(actionsPath, module.actionsAsset.ToJson());
                AssetDatabase.ImportAsset(actionsPath);
            }

            InputActionAsset persisted = AssetDatabase.LoadAssetAtPath<InputActionAsset>(actionsPath);

            if (persisted != null && module.actionsAsset != persisted)
            {
                module.actionsAsset = persisted;
                Debug.Log($"[MvpSceneBuilder] UI input actions wired to {actionsPath}");
            }
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

        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    scenes.RemoveAt(i);
                    break;
                }
            }

            // Boot is the build entry point (it loads the main scene). This used to insert the main scene at
            // index 0, so every MVP rebuild silently pushed the splash to second place and a build booted
            // straight into the game. Only go first when there is no Boot scene to protect.
            int index = scenes.Count > 0 && scenes[0].path == BootScenePath ? 1 : 0;
            scenes.Insert(index, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
