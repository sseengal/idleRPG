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

        // Layout bands, as fractions of screen height.
        private const float HeaderBottom = 0.88f;   // header: HeaderBottom .. 1
        private const float PagesBottom = 0.12f;    // page area: PagesBottom .. HeaderBottom
        private const float NavBarTop = 0.12f;      // nav bar: 0 .. NavBarTop

        // Bands inside the battle page.
        private const float BattleViewportBottom = 0.38f; // log 0..0.38, viewport 0.38..1

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

            PlaceholderSpriteGenerator.GenerateAll();
            DataAssetGenerator.GenerateAll();

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
            RectTransform modalRoot = CreateModalCanvas(hudRoot);
            RectTransform safeArea = CreateSafeArea(canvasRoot);

            HudHeaderUI header = BuildHeader(safeArea);

            GameObject battlePage = CreatePage("BattlePage", safeArea);
            GameObject managementPage = CreatePage("ManagementPage", safeArea);

            BuildViewport(battlePage.GetComponent<RectTransform>(), damageRoot,
                out FormationBoardView formationBoard, out EnemyStackView enemyStack,
                out FloatingDamageTextPool damagePool, out RectTransform enemyAnchor);
            CombatLogUI combatLog = BuildCombatLog(battlePage.GetComponent<RectTransform>());
            TabController tabs = BuildManagementPage(managementPage.GetComponent<RectTransform>(), partyConfig, prestigeUpgrades);
            ScreenController screens = BuildNavBar(safeArea, battlePage, managementPage, tabs, damageRoot.gameObject);
            OfflineRewardsPopup offlinePopup = BuildOfflinePopup(modalRoot);
            ToastUI toast = BuildToast(canvasRoot);
            EnsureEventSystem();

            SceneWiringUtility.SetField(hud, "gameManager", gameManager);
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
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvasObject.GetComponent<RectTransform>();
        }

        /// <summary>A full-page container that overlays the shared header/nav bands.</summary>
        private static GameObject CreatePage(string name, RectTransform safeArea)
        {
            GameObject page = UiFactory.Node(name, safeArea);
            UiFactory.Anchor(page.GetComponent<RectTransform>(), new Vector2(0f, PagesBottom), new Vector2(1f, HeaderBottom));
            return page;
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
            if (module == null || module.actionsAsset == null || EditorUtility.IsPersistent(module.actionsAsset))
            {
                return;
            }

            const string actionsPath = "Assets/IdleRPG/Settings/UiInputActions.inputactions";
            EnsureFolder("Assets/IdleRPG/Settings");

            if (AssetDatabase.LoadAssetAtPath<InputActionAsset>(actionsPath) == null)
            {
                AssetDatabase.CreateAsset(module.actionsAsset, actionsPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[MvpSceneBuilder] Saved UI input actions to {actionsPath}");
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
