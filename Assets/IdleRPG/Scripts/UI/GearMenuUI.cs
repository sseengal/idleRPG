using IdleRPG.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// The header gear. In the lobby it opens SETTINGS (sound, restore, privacy, version). In a fight it opens a
    /// minimal PAUSE menu (resume, sound, quit). It never offers an upgrade path, so it cannot be used to shop
    /// mid-fight. QUIT asks a second time and then ends the run.
    ///
    /// ELI5: one button, two jobs. Between runs it is the settings door. During a fight it stops the fight and
    /// offers "keep going", "sound", or "end this run".
    /// </summary>
    public sealed class GearMenuUI : MonoBehaviour
    {
        public static GearMenuUI Instance { get; private set; }

        /// <summary>TODO: owner supplies the hosted privacy-policy URL. Empty shows a status line, never a dead tap.</summary>
        private const string PrivacyUrl = "";

        private GameManager manager;
        private GameObject canvasRoot;
        private GameObject normalGroup;
        private GameObject confirmGroup;
        private RectTransform panelRect;
        private TextMeshProUGUI title;
        private TextMeshProUGUI bannerLabel;
        private TextMeshProUGUI status;
        private TextMeshProUGUI soundLabel;
        private TextMeshProUGUI confirmTitle;
        private TextMeshProUGUI confirmBody;
        private UiPlateButton resumeButton;
        private UiPlateButton soundButton;
        private UiPlateButton quitButton;
        private UiPlateButton restoreButton;
        private UiPlateButton privacyButton;
        private UiPlateButton closeButton;
        private UiPlateButton endRunButton;
        private UiPlateButton keepFightingButton;
        private bool pausedByMenu;

        /// <summary>The sheet's left and right edges, as a fraction of the screen: a dialog, not a full-bleed band.</summary>
        private const float SheetLeft = 0.14f;
        private const float SheetRight = 0.86f;

        /// <summary>The reference height the anchors are measured against (the canvas is 1080x1920).</summary>
        private const float ScreenHeight = 1920f;

        /// <summary>How many lines the confirm body is allowed, so the sheet reserves the right height.</summary>
        private const int ConfirmBodyLines = 2;

        public bool IsOpen => canvasRoot != null && canvasRoot.activeSelf;

        public void Initialize(GameManager gameManager)
        {
            manager = gameManager;
            Instance = this;

            if (canvasRoot == null)
            {
                Build();
            }
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            if (canvasRoot == null)
            {
                Build();
            }

            if (manager == null)
            {
                return;
            }

            canvasRoot.SetActive(true);

            bool lobby = manager.State == GameState.Lobby;
            normalGroup.SetActive(true);
            confirmGroup.SetActive(false);

            resumeButton.gameObject.SetActive(!lobby);
            quitButton.gameObject.SetActive(!lobby);
            restoreButton.gameObject.SetActive(lobby);
            privacyButton.gameObject.SetActive(lobby);
            closeButton.gameObject.SetActive(lobby);

            PlaceSheet(lobby);
            PlaceHeading();
            SetHeading(lobby ? "SETTINGS" : "GAME PAUSED");

            UpdateSoundLabel();

            // The lobby shows its version under the stack; a run has nothing to say there (the ribbon says it).
            status.SetText(lobby ? "v" + Application.version : string.Empty);

            if (!lobby && !pausedByMenu)
            {
                Time.timeScale = 0f;
                pausedByMenu = true;
            }
        }

        public void Close()
        {
            if (canvasRoot != null)
            {
                canvasRoot.SetActive(false);
            }

            if (pausedByMenu)
            {
                Time.timeScale = 1f;
                pausedByMenu = false;
            }
        }

        private void Build()
        {
            canvasRoot = new GameObject("GearMenuCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));

            Canvas canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;

            CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            Image scrim = UiRuntime.CreatePanel(canvasRoot.transform, null, UiTheme.ScrimSoft, raycast: true);
            scrim.gameObject.name = "Scrim";
            UiRuntime.Anchor(scrim.rectTransform, Vector2.zero, Vector2.one);

            Image panel = UiParts.Panel(canvasRoot.transform, "Panel",
                new Vector2(SheetLeft, 0.32f), new Vector2(SheetRight, 0.66f), PanelKind.Modal, raycast: true);
            panelRect = panel.rectTransform;

            BuildBanner(panel.transform);

            normalGroup = new GameObject("Normal", typeof(RectTransform));
            normalGroup.transform.SetParent(panel.transform, false);
            UiRuntime.Anchor((RectTransform)normalGroup.transform, Vector2.zero, Vector2.one);

            // Fallback only: when the skin has a ribbon, the ribbon carries the heading instead of this label.
            title = UiRuntime.CreateText(normalGroup.transform, "Title", "SETTINGS", UiType.Role.Title,
                TextAlignmentOptions.Center, UiTheme.Text);
            title.gameObject.SetActive(bannerLabel == null);

            status = UiRuntime.CreateText(normalGroup.transform, "Status", string.Empty, UiType.Role.Small,
                TextAlignmentOptions.Center, UiTheme.Text);
            status.gameObject.SetActive(false);

            BuildButtons();
            BuildConfirm(panel.transform);
            canvasRoot.SetActive(false);
        }

        /// <summary>The ribbon across the panel's top edge. One shared builder, so every sheet's heading matches.</summary>
        private void BuildBanner(Transform panel)
        {
            bannerLabel = UiBanner.Build(panel, "GAME PAUSED");
        }

        private void BuildButtons()
        {
            // Anchors here are placeholders: PlaceSheet() stacks the visible rows at Open time.
            resumeButton = MakeButton(normalGroup.transform, "Resume", "RESUME", ButtonFace.Neutral, Close);

            soundButton = MakeButton(normalGroup.transform, "Sound", "SOUND: ON", ButtonFace.Neutral, OnSound);
            soundLabel = soundButton.GetComponentInChildren<TextMeshProUGUI>();

            quitButton = MakeButton(normalGroup.transform, "QuitRun", "QUIT RUN", ButtonFace.Danger, OnQuit);

            // Lobby mode reuses the same stack: sound, restore, privacy, close. The pause rows are hidden then.
            restoreButton = MakeButton(normalGroup.transform, "Restore", "RESTORE PURCHASES", ButtonFace.Neutral, OnRestore);
            privacyButton = MakeButton(normalGroup.transform, "Privacy", "PRIVACY", ButtonFace.Neutral, OnPrivacy);
            closeButton = MakeButton(normalGroup.transform, "Close", "CLOSE", ButtonFace.Neutral, Close);
        }

        /// <summary>
        /// Stacks the rows that are visible in this mode. Run = 3 rows (resume, sound, quit), lobby = 4 rows
        /// (sound, restore, privacy, close). The array is the order, so no two buttons can share a slot.
        /// </summary>
        private void PlaceSheet(bool lobby)
        {
            if (lobby)
            {
                ArrangeSheet(new[] { soundButton, restoreButton, privacyButton, closeButton }, withStatus: true);
            }
            else
            {
                ArrangeSheet(new[] { resumeButton, soundButton, quitButton }, withStatus: false);
            }
        }

        /// <summary>
        /// Sizes the panel to its content and stacks the buttons from the bottom pad upward. Every number comes from
        /// <see cref="UiTouch"/> and <see cref="UiType"/>, so a sheet can never pick its own button size again.
        /// </summary>
        private void ArrangeSheet(UiPlateButton[] buttons, bool withStatus)
        {
            float statusUnits = withStatus ? UiType.LineHeight(UiType.Role.Small) : 0f;
            float statusBlock = withStatus ? statusUnits + UiSpace.Space8 : 0f;

            SizeSheet(UiTouch.SheetStackHeight(buttons.Length) + statusBlock);
            PlaceStack(buttons, statusBlock);
            PlaceStatus(withStatus, statusUnits);
        }

        /// <summary>
        /// Makes the panel exactly as tall as what it shows: half a ribbon inside the top edge, the gap under it,
        /// the content, and the bottom pad. Centred, so no view sits off-centre.
        /// </summary>
        private void SizeSheet(float contentUnits)
        {
            if (panelRect == null)
            {
                return;
            }

            float height = UiBanner.Height * 0.5f + UiTouch.SheetTopGap + contentUnits + UiTouch.SheetBottomPad;
            float fraction = Mathf.Clamp(height / ScreenHeight, 0.15f, 0.90f);

            panelRect.anchorMin = new Vector2(SheetLeft, 0.5f - fraction * 0.5f);
            panelRect.anchorMax = new Vector2(SheetRight, 0.5f + fraction * 0.5f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
        }

        /// <summary>One height, one padding and one gap for every sheet button, stacked up from the bottom pad.</summary>
        private void PlaceStack(UiPlateButton[] buttons, float extraBottom)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                UiPlateButton button = buttons[i];

                if (button == null)
                {
                    continue;
                }

                RectTransform rect = (RectTransform)button.transform;
                float fromBottom = UiTouch.SheetBottomPad + extraBottom +
                                   (buttons.Length - 1 - i) * (UiTouch.SheetButtonHeight + UiTouch.SheetButtonGap);

                // Full inner width: the panel's own padding decides the button's edges, so the air on the left
                // always equals the air on the right and always matches the bottom (owner, 2026-11-23).
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = new Vector2(UiTouch.SheetPadX, fromBottom);
                rect.offsetMax = new Vector2(-UiTouch.SheetPadX, fromBottom + UiTouch.SheetButtonHeight);
            }
        }

        /// <summary>The status line lives in the bottom pad, under the stack, and only when it has something to say.</summary>
        private void PlaceStatus(bool visible, float statusUnits)
        {
            if (status == null)
            {
                return;
            }

            status.gameObject.SetActive(visible);

            if (!visible)
            {
                return;
            }

            RectTransform rect = status.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(UiTouch.SheetPadX, UiSpace.Space8);
            rect.offsetMax = new Vector2(-UiTouch.SheetPadX, UiSpace.Space8 + statusUnits);
        }

        /// <summary>The heading fallback sits where the ribbon's face is, so a skin without a ribbon still reads.</summary>
        private void PlaceHeading()
        {
            if (title == null || !title.gameObject.activeSelf)
            {
                return;
            }

            RectTransform rect = title.rectTransform;
            rect.anchorMin = new Vector2(0.05f, 1f);
            rect.anchorMax = new Vector2(0.95f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, UiType.LineHeight(UiType.Role.Title));
            rect.anchoredPosition = new Vector2(0f, -(UiBanner.Height * 0.5f + UiTouch.SheetTopGap));
        }

        private void BuildConfirm(Transform panel)
        {
            confirmGroup = new GameObject("Confirm", typeof(RectTransform));
            confirmGroup.transform.SetParent(panel, false);
            UiRuntime.Anchor((RectTransform)confirmGroup.transform, Vector2.zero, Vector2.one);

            confirmTitle = UiRuntime.CreateText(confirmGroup.transform, "Title", "END THIS RUN?",
                UiType.Role.Title, TextAlignmentOptions.Center, UiTheme.Text);

            confirmBody = UiRuntime.CreateText(confirmGroup.transform, "Body", string.Empty, UiType.Role.Sentence,
                TextAlignmentOptions.Center, UiTheme.Text);

            endRunButton = MakeButton(confirmGroup.transform, "EndRun", "END RUN", ButtonFace.Danger, OnConfirmEnd);
            keepFightingButton = MakeButton(confirmGroup.transform, "KeepFighting", "KEEP FIGHTING",
                ButtonFace.Neutral, OnConfirmKeep);

            confirmGroup.SetActive(false);
        }

        /// <summary>
        /// Sizes the sheet for the confirm view and lays out its heading, its two lines of body and its two buttons.
        /// Same stack as the pause view, so the sheet shrinks to fit instead of leaving a hole at the foot.
        /// </summary>
        private void PlaceConfirmSheet()
        {
            float titleLine = UiType.LineHeight(UiType.Role.Title);
            float bodyBlock = UiType.LineHeight(UiType.Role.Sentence) * ConfirmBodyLines;
            float text = titleLine + UiSpace.Space16 + bodyBlock;

            SizeSheet(text + UiSpace.Space16 + UiTouch.SheetStackHeight(2));
            PlaceConfirmText(titleLine, bodyBlock);
            PlaceStack(new[] { endRunButton, keepFightingButton }, 0f);
        }

        /// <summary>Heading and body hang from the ribbon, in the same band the pause view leaves empty above its stack.</summary>
        private void PlaceConfirmText(float titleLine, float bodyBlock)
        {
            float top = UiBanner.Height * 0.5f + UiTouch.SheetTopGap;

            if (confirmTitle != null)
            {
                RectTransform rect = confirmTitle.rectTransform;
                rect.anchorMin = new Vector2(0.05f, 1f);
                rect.anchorMax = new Vector2(0.95f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(0f, titleLine);
                rect.anchoredPosition = new Vector2(0f, -top);
            }

            if (confirmBody != null)
            {
                RectTransform rect = confirmBody.rectTransform;
                rect.anchorMin = new Vector2(0.08f, 1f);
                rect.anchorMax = new Vector2(0.92f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(0f, bodyBlock);
                rect.anchoredPosition = new Vector2(0f, -(top + titleLine + UiSpace.Space16));
            }
        }

        /// <summary>
        /// One sheet button. Anchors are placeholders - <see cref="PlaceStack"/> sets them - and the caption is the
        /// TITLE role, so a 144-tall button is filled by its word instead of carrying a small label in a big box.
        /// </summary>
        private static UiPlateButton MakeButton(Transform parent, string name, string label, ButtonFace face,
            UnityEngine.Events.UnityAction onClick)
        {
            // The shared shadow now comes from UiPlateButton.Create, so every button matches.
            return UiPlateButton.Create(parent, name, Vector2.zero, Vector2.one, label, UiType.Role.Title,
                Color.white, Color.white, onClick, face);
        }

        private void UpdateSoundLabel()
        {
            if (soundLabel == null)
            {
                return;
            }

            bool muted = manager != null && manager.Audio != null && manager.Audio.IsMuted;
            soundLabel.SetText(muted ? "SOUND: OFF" : "SOUND: ON");
        }

        private void SetStatus(string text)
        {
            if (status == null)
            {
                return;
            }

            bool show = !string.IsNullOrEmpty(text);
            status.SetText(text);
            status.gameObject.SetActive(show);
        }

        /// <summary>The ribbon carries the heading when the skin has one; the label is the fallback.</summary>
        private void SetHeading(string text)
        {
            if (bannerLabel != null)
            {
                bannerLabel.SetText(text);
            }
            else if (title != null)
            {
                title.SetText(text);
            }
        }

        private void OnSound()
        {
            if (manager == null || manager.Audio == null)
            {
                return;
            }

            manager.Audio.ToggleMuted();
            UpdateSoundLabel();
            // No status line: the button label already says SOUND: ON / OFF (owner request 2026-11-22).
        }

        private void OnRestore()
        {
            if (manager == null || manager.Iap == null)
            {
                SetStatus("Store not available.");
                return;
            }

            SetStatus("Checking purchases...");
            manager.Iap.RestorePurchases(restored => SetStatus(restored ? "Purchases restored." : "Nothing to restore."));
        }

        private void OnPrivacy()
        {
            if (string.IsNullOrEmpty(PrivacyUrl))
            {
                SetStatus("Privacy link coming soon.");
                return;
            }

            Application.OpenURL(PrivacyUrl);
        }

        private void OnQuit()
        {
            if (manager == null)
            {
                return;
            }

            int perks = Mathf.RoundToInt((float)manager.RunEndPreviewTokens);
            confirmBody.SetText("You keep +" + perks + " Perk Points. This ends the run.");
            normalGroup.SetActive(false);
            confirmGroup.SetActive(true);
            PlaceConfirmSheet();
        }

        private void OnConfirmKeep()
        {
            confirmGroup.SetActive(false);
            normalGroup.SetActive(true);
            PlaceSheet(lobby: false);
        }

        private void OnConfirmEnd()
        {
            GameManager ending = manager;
            Close();
            ending?.EndRun();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (canvasRoot != null)
            {
                Destroy(canvasRoot);
            }

            if (pausedByMenu)
            {
                Time.timeScale = 1f;
            }
        }
    }
}
