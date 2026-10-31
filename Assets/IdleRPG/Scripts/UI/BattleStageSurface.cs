using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// Draws the battle viewport through its own camera and render texture.
    ///
    /// The arena art is still uGUI. The stage canvas is a Screen Space - Camera canvas whose only
    /// camera renders into a RenderTexture, so the result can later be shaded (CRT, grain, flash)
    /// without touching the header, log or navigation. The HUD shows that texture through a
    /// RawImage inside the viewport mask.
    ///
    /// Input bridge: the stage does not receive normal EventSystem raycasts because it lives in a
    /// texture. A click on the viewport RawImage is converted from screen space into render-texture
    /// space, raycast against the stage raycaster, and forwarded to the hit control. That keeps the
    /// formation board's tap-to-move behaviour intact.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class BattleStageSurface : MonoBehaviour, IPointerClickHandler
    {
        [Header("Stage")]
        [SerializeField] private Camera stageCamera;
        [SerializeField] private Canvas stageCanvas;
        [SerializeField] private RectTransform stageRoot;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RawImage display;
        [SerializeField] private GraphicRaycaster stageRaycaster;

        [Header("Content")]
        [Tooltip("Damage numbers move into the stage so their positions match the stage-space unit anchors.")]
        [SerializeField] private FloatingDamageTextPool damageTextPool;

        private RenderTexture stageTexture;

        /// <summary>The live surface, used by overlay effects that need a stage-space point.</summary>
        public static BattleStageSurface Instance { get; private set; }

        /// <summary>
        /// Root the stage content lives under. Authored viewport children are moved here in Awake, so a
        /// runtime view (the story layer) must parent here to draw inside the battle viewport.
        /// </summary>
        public RectTransform ContentRoot => stageRoot;

        private void Awake()
        {
            if (stageCamera == null || stageCanvas == null || stageRoot == null || viewport == null || display == null)
            {
                Debug.LogError("[BattleStageSurface] Stage references are incomplete; the battle surface is disabled.");
                enabled = false;
                return;
            }

            Instance = this;

            // The stage raycaster is driven only by OnPointerClick below. Leaving it registered with
            // the EventSystem would make normal screen-space raycasts hit controls inside the texture.
            if (stageRaycaster != null)
            {
                stageRaycaster.enabled = false;
            }

            stageCanvas.worldCamera = stageCamera;
            MoveContentIntoStage();

            // URP camera data lives in an optional package assembly, so add it by type name. The
            // editor builder adds it to the scene; this is the runtime fallback for ad-hoc stages.
            System.Type cameraDataType = System.Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");

            if (cameraDataType != null && stageCamera.GetComponent(cameraDataType) == null)
            {
                stageCamera.gameObject.AddComponent(cameraDataType);
            }
        }

        private void Start()
        {
            Instance = this;
            RebuildTextureIfNeeded();
        }

        private void OnEnable()
        {
            Instance = this;
        }

        private void LateUpdate()
        {
            RebuildTextureIfNeeded();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            ReleaseTexture();
        }

        /// <summary>
        /// Moves every authored viewport child into the stage canvas. The RawImage stays behind in
        /// the viewport as the display surface.
        /// </summary>
        private void MoveContentIntoStage()
        {
            Transform displayTransform = display.transform;

            // Capture in order before moving. Reparenting removes each child from the viewport, so a
            // live index loop would skip every other child and reverse the rendering order.
            List<Transform> stageChildren = new List<Transform>(viewport.childCount);

            for (int i = 0; i < viewport.childCount; i++)
            {
                Transform child = viewport.GetChild(i);
                if (child != null && child != displayTransform && child != transform)
                {
                    stageChildren.Add(child);
                }
            }

            for (int i = 0; i < stageChildren.Count; i++)
            {
                stageChildren[i].SetParent(stageRoot, false);
            }

            if (damageTextPool != null && damageTextPool.transform.parent != stageRoot)
            {
                damageTextPool.transform.SetParent(stageRoot, false);
            }
        }

        private void RebuildTextureIfNeeded()
        {
            if (viewport == null || stageCamera == null || display == null)
            {
                return;
            }

            Vector2 pixelSize = viewport.rect.size * viewport.lossyScale;
            int width = Mathf.RoundToInt(pixelSize.x);
            int height = Mathf.RoundToInt(pixelSize.y);

            if (width < 64 || height < 64)
            {
                return;
            }

            if (stageTexture != null && stageTexture.width == width && stageTexture.height == height)
            {
                return;
            }

            ReleaseTexture();

            stageTexture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32)
            {
                name = "BattleStageRT",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1
            };
            stageTexture.Create();

            stageCamera.targetTexture = stageTexture;
            display.texture = stageTexture;
            display.enabled = true;
        }

        private void ReleaseTexture()
        {
            if (stageCamera != null && stageCamera.targetTexture == stageTexture)
            {
                stageCamera.targetTexture = null;
            }

            if (display != null && display.texture == stageTexture)
            {
                display.texture = null;
            }

            if (stageTexture != null)
            {
                stageTexture.Release();
                Destroy(stageTexture);
                stageTexture = null;
            }
        }

        /// <summary>
        /// Converts a stage-space point (for example an enemy anchor) into a screen point on the
        /// viewport display. Used by HUD-layer effects such as the gold flight.
        /// </summary>
        public static bool TryStageToScreen(Vector3 stageWorldPosition, out Vector2 screenPoint)
        {
            BattleStageSurface surface = Instance;
            if (surface == null)
            {
                screenPoint = default;
                return false;
            }

            return surface.TryStageToScreenInstance(stageWorldPosition, out screenPoint);
        }

        private bool TryStageToScreenInstance(Vector3 stageWorldPosition, out Vector2 screenPoint)
        {
            screenPoint = default;

            if (stageCamera == null || display == null || stageTexture == null)
            {
                return false;
            }

            Vector2 texturePoint = RectTransformUtility.WorldToScreenPoint(stageCamera, stageWorldPosition);
            float width = Mathf.Max(1, stageTexture.width);
            float height = Mathf.Max(1, stageTexture.height);
            float normalizedX = texturePoint.x / width;
            float normalizedY = texturePoint.y / height;

            Rect displayRect = display.rectTransform.rect;
            Vector2 local = new Vector2(
                displayRect.xMin + normalizedX * displayRect.width,
                displayRect.yMin + normalizedY * displayRect.height);

            screenPoint = RectTransformUtility.WorldToScreenPoint(
                null,
                display.rectTransform.TransformPoint(local));
            return true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (stageRaycaster == null || stageCamera == null || display == null || stageTexture == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    display.rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                return;
            }

            Rect displayRect = display.rectTransform.rect;
            if (displayRect.width <= 0f || displayRect.height <= 0f)
            {
                return;
            }

            float normalizedX = (local.x - displayRect.xMin) / displayRect.width;
            float normalizedY = (local.y - displayRect.yMin) / displayRect.height;

            if (normalizedX < 0f || normalizedX > 1f || normalizedY < 0f || normalizedY > 1f)
            {
                return;
            }

            Vector2 texturePoint = new Vector2(
                normalizedX * stageTexture.width,
                normalizedY * stageTexture.height);

            PointerEventData stageEvent = new PointerEventData(EventSystem.current)
            {
                position = texturePoint,
                button = eventData.button
            };

            List<RaycastResult> results = new List<RaycastResult>();
            stageRaycaster.Raycast(stageEvent, results);

            if (results.Count == 0)
            {
                return;
            }

            stageEvent.pointerCurrentRaycast = results[0];

            // Bubble to the control. The raycast hits the hit-area Image, while the Button lives on its
            // parent (Hero1, Pad0, BoardBackground), so a target-only Execute would hit nothing.
            ExecuteEvents.ExecuteHierarchy(
                results[0].gameObject, stageEvent, ExecuteEvents.pointerClickHandler);
        }
    }
}

