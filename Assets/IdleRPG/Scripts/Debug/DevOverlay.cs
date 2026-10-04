using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using IdleRPG.Combat;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Debugging;
using IdleRPG.Economy;
using IdleRPG.Progression;
using IdleRPG.Services;
using IdleRPG.Sim;

namespace IdleRPG.DebugTools
{
    /// <summary>
    /// Live numbers panel (F3) for tuning without guesswork.
    ///
    /// ELI5: a car dashboard for the fight - how hard the party hits (DPS), how much of a beating it can take
    /// (eHP), gold per minute, how long the current wave still needs, and the dice state so any bug report can
    /// be replayed. Editor/development builds only.
    ///
    /// Builds its own overlay canvas at runtime, so the scene needs no new object. Refreshes ~4x/second.
    /// </summary>
    public sealed partial class DevOverlay : MonoBehaviour
    {
        private const float RefreshIntervalSec = 0.25f;
        private const int FontSize = 13;

        private GameContext context;
        private TelemetryFeed telemetry;

        private GameObject panel;
        private RectTransform panelRect;
        private TextMeshProUGUI label;

        private GameObject hotkeyPanel;
        private TextMeshProUGUI hotkeyLabel;

        private const float PanelInset = 8f;
        private const float PanelGap = 6f;
        private const float PanelWidth = 340f;

        private float refreshTimer;
        private bool visible;

        /// <summary>Creates (or reuses) the overlay on a host object. Called by GameManager in dev builds.</summary>
        public static DevOverlay Create(GameObject host, GameContext context, TelemetryFeed telemetry)
        {
            DevOverlay overlay = host.GetComponent<DevOverlay>();

            if (overlay == null)
            {
                overlay = host.AddComponent<DevOverlay>();
            }

            overlay.Build(context, telemetry);
            return overlay;
        }

        public void Build(GameContext gameContext, TelemetryFeed feed)
        {
            context = gameContext;
            telemetry = feed;

            if (panel != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("DevOverlayCanvas");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            panel = new GameObject("Panel");
            panel.transform.SetParent(canvasObject.transform, false);

            panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(PanelInset, -PanelInset);
            panelRect.sizeDelta = new Vector2(PanelWidth, 260f);

            Image background = panel.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.72f);

            label = CreateLabel(panel.transform, "Label", new Color(0.85f, 1f, 0.85f));
            hotkeyPanel = CreateHotkeyPanel(canvasObject.transform);

            SetVisible(false);
        }

        /// <summary>Creates a full-panel text label (stats panel or hotkey legend).</summary>
        private static TextMeshProUGUI CreateLabel(Transform parent, string name, Color color)
        {
            GameObject labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);

            RectTransform labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(PanelInset, 6f);
            labelRect.offsetMax = new Vector2(-PanelInset, -6f);

            TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = FontSize;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// The hotkey legend panel. Built once from <see cref="DebugHotkeyCatalog"/>, so a key added in code shows
        /// up here (and in the F3 panel) without touching this file.
        /// </summary>
        private GameObject CreateHotkeyPanel(Transform parent)
        {
            GameObject host = new GameObject("HotkeyPanel");
            host.transform.SetParent(parent, false);

            RectTransform rect = host.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(PanelInset, -PanelInset);
            rect.sizeDelta = new Vector2(PanelWidth, 100f);

            Image background = host.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.72f);

            hotkeyLabel = CreateLabel(host.transform, "Label", new Color(1f, 0.92f, 0.7f));
            hotkeyLabel.SetText(DebugHotkeyCatalog.BuildLegend("DEV HOTKEYS  (F3 closes this)"));
            return host;
        }

        /// <summary>
        /// Grows a panel to fit its text and re-stacks the legend underneath, so adding a stats line or a hotkey
        /// can never overlap the panel below it.
        /// </summary>
        private void FitPanels(float contentHeight)
        {
            if (panelRect == null)
            {
                return;
            }

            panelRect.sizeDelta = new Vector2(PanelWidth, contentHeight);

            if (hotkeyPanel == null)
            {
                return;
            }

            RectTransform hotkeyRect = hotkeyPanel.GetComponent<RectTransform>();
            float hotkeyHeight = hotkeyLabel != null ? hotkeyLabel.preferredHeight + 12f : 100f;
            hotkeyRect.sizeDelta = new Vector2(PanelWidth, hotkeyHeight);
            hotkeyRect.anchoredPosition = new Vector2(PanelInset, -(PanelInset + contentHeight + PanelGap));
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            {
                SetVisible(!visible);
            }

            if (!visible || context == null)
            {
                return;
            }

            refreshTimer -= Time.deltaTime;

            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = RefreshIntervalSec;
            RefreshText();
        }

        private void OnDestroy()
        {
            if (panel != null)
            {
                Destroy(panel.transform.parent.gameObject);
                panel = null;
                label = null;
            }
        }

        private void SetVisible(bool value)
        {
            visible = value;
            refreshTimer = 0f;

            if (panel != null)
            {
                panel.SetActive(value);
            }

            if (hotkeyPanel != null)
            {
                hotkeyPanel.SetActive(value);
            }
        }

    }
}