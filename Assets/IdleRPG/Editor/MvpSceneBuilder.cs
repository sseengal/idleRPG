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

        private const float ReferenceWidth = 1080f;
        private const float ReferenceHeight = 1920f;

        // The two chrome bands are FIXED heights, not screen fractions.
        //
        // Why: a fraction makes the bars change size with the screen. On a tall phone the extra height was split
        // between the bars and the page, and on a short screen the top bar could fall below the 56-unit currency
        // numbers. One rule now: extra height belongs to the page, the bars never move.
        // The values reproduce the 1080x1920 design (header 6% and nav 12% of 1920), so nothing shifts on 9:16.
        private const float HeaderBandHeight = 115f;   // top bar: the currency chips + boost, pinned to the top
        private const float NavBandHeight = 230f;      // bottom bar: the five nav buttons, pinned to the bottom

        // Bands inside the battle page (page coords): log 0..LogTop, log-header 0.34..0.39,
        // viewport 0.39..StageRowMinY, stage/wave row 0.93..0.99.
        private const float LogTop = 0.34f;
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

            RectTransform canvasRoot = CreateCanvas(hudRoot);
            RectTransform damageRoot = CreateDamageCanvas(hudRoot);
            RectTransform modalCanvas = CreateModalCanvas(hudRoot);
            RectTransform safeArea = CreateSafeArea(canvasRoot);

            // The modal canvas gets its own safe box. A card near a screen edge (the ranks sheet, the offline claim)
            // must clear a notch or a gesture bar just like the HUD bars do, so every card hangs off this node and
            // never off the raw canvas.
            RectTransform modalRoot = CreateSafeArea(modalCanvas);

            HudHeaderUI header = BuildHeader(safeArea);

            GameObject battlePage = CreatePage("BattlePage", safeArea);
            BuildStageRow(battlePage.GetComponent<RectTransform>(), header);
            GameObject managementPage = CreatePage("ManagementPage", safeArea);

            BuildViewport(battlePage.GetComponent<RectTransform>(), damageRoot,
                out FormationBoardView formationBoard, out EnemyStackView enemyStack,
                out FloatingDamageTextPool damagePool, out RectTransform enemyAnchor);
            RectTransform battleRect = battlePage.GetComponent<RectTransform>();
            CombatLogUI combatLog = BuildCombatLog(battleRect);
            AbilityPanelUI abilityPanel = BuildAbilityPanel(battleRect, partyConfig.ValidHeroCount);
            BuildLogHeaderRow(battleRect, combatLog, abilityPanel);
            TabController tabs = BuildManagementPage(managementPage.GetComponent<RectTransform>(), partyConfig, prestigeUpgrades);
            ScreenController screens = BuildNavBar(safeArea, battlePage, managementPage, tabs, damageRoot.gameObject);
            OfflineRewardsPopup offlinePopup = BuildOfflinePopup(modalRoot);
            DefeatModalUI defeatModal = BuildDefeatModal(modalRoot);
            ToastUI toast = BuildToast(safeArea);
            EnsureEventSystem();

            SceneWiringUtility.SetField(hud, "gameManager", gameManager);
            SceneWiringUtility.SetField(hud, "modalRoot", modalRoot);
            SceneWiringUtility.SetField(hud, "formationBoard", formationBoard);
            SceneWiringUtility.SetField(hud, "enemyStack", enemyStack);
            SceneWiringUtility.SetField(hud, "damageTextPool", damagePool);
            SceneWiringUtility.SetField(damagePool, "enemyAnchor", enemyAnchor);
            SceneWiringUtility.SetField(formationBoard, "damagePool", damagePool, required: false);

            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(combatLog);
            EditorUtility.SetDirty(screens);
            EditorUtility.SetDirty(header);
            EditorUtility.SetDirty(tabs);
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
        /// A full-page container that sits BETWEEN the two fixed chrome bands: from the top of the nav bar to the
        /// bottom of the header. Every extra unit of screen height lands here, which is the point of the fixed bars.
        /// </summary>
        private static GameObject CreatePage(string name, RectTransform safeArea)
        {
            GameObject page = UiFactory.Node(name, safeArea);
            UiFactory.Anchor(page.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                0f, NavBandHeight, 0f, HeaderBandHeight);
            return page;
        }

        /// <summary>
        /// Pins a full-width bar of a FIXED height to the top or the bottom of its parent, with the shared side
        /// margin and edge gap. The header and the nav bar use this so neither can change size with the screen.
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
