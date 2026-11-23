using IdleRPG.Core;
using IdleRPG.Utils;
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
        private TextMeshProUGUI keepCaption;
        private readonly TextMeshProUGUI[] keepValues = new TextMeshProUGUI[3];
        private readonly Image[] keepIcons = new Image[3];
        private readonly Sprite[] keepSprites = new Sprite[3];
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

            Image scrim = UiRuntime.CreatePanel(canvasRoot.transform, null, UiTheme.Scrim, raycast: true);
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

            quitButton = MakeButton(normalGroup.transform, "QuitRun", "END RUN", ButtonFace.Danger, OnQuit);

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
            rect.offsetMin = new Vector2(UiTouch.SheetPadX, UiSpace.Space32);
            rect.offsetMax = new Vector2(-UiTouch.SheetPadX, UiSpace.Space32 + statusUnits);
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

            // No heading label here: the ribbon carries "END THIS RUN?" (owner, 2026-11-23), so the body starts
            // with the receipt.
            keepCaption = UiRuntime.CreateText(confirmGroup.transform, "KeepCaption", "YOU KEEP:",
                UiType.Role.Section, TextAlignmentOptions.Center, UiTheme.Text);

            for (int i = 0; i < keepIcons.Length; i++)
            {
                BuildKeepRow(i);
            }

            endRunButton = MakeButton(confirmGroup.transform, "EndRun", "END RUN", ButtonFace.Danger, OnConfirmEnd);
            keepFightingButton = MakeButton(confirmGroup.transform, "KeepFighting", "KEEP FIGHTING",
                ButtonFace.Neutral, OnConfirmKeep);

            confirmGroup.SetActive(false);
        }

        /// <summary>
        /// One line of the "you keep" list: an icon and its amount. Built once and re-positioned with the panel, so a
        /// sheet that grows for another currency needs no new layout code - only a longer array.
        /// </summary>
        private void BuildKeepRow(int index)
        {
            GameObject row = new GameObject("Keep" + index, typeof(RectTransform));
            row.transform.SetParent(confirmGroup.transform, false);

            Image icon = UiRuntime.CreatePanel(row.transform, keepSprites[index], Color.white, raycast: false);
            icon.name = "Icon";
            icon.preserveAspect = true;
            keepIcons[index] = icon;

            keepValues[index] = UiRuntime.CreateText(row.transform, "Value", "× 0", UiType.Role.Title,
                TextAlignmentOptions.MidlineLeft, UiTheme.Text);
        }

        /// <summary>
        /// Sizes the sheet for the confirm view: heading, the "you keep" caption, one row per currency, then the two
        /// buttons. Every space comes from UiTouch/UiSpace, so this panel and the pause panel share one frame.
        /// </summary>
        private void PlaceConfirmSheet()
        {
            float captionLine = UiType.LineHeight(UiType.Role.Section);
            float rowsBlock = keepValues.Length * UiTouch.KeepRowHeight +
                              (keepValues.Length - 1) * UiTouch.KeepRowGap;

            float content = captionLine + UiSpace.Space16 + rowsBlock + UiSpace.Space32 +
                            UiTouch.SheetStackHeight(2);

            SizeSheet(content);

            float padX = UiTouch.SheetPadX;
            float top = UiBanner.Height * 0.5f + UiTouch.SheetTopGap;

            // The receipt is centred on the sheet: caption centred, rows centred as a block.
            PlaceTextBlock(keepCaption, top, captionLine, padX, TextAlignmentOptions.Center);

            float rowTop = top + captionLine + UiSpace.Space16;

            for (int i = 0; i < keepValues.Length; i++)
            {
                PlaceKeepRow(i, rowTop);
                rowTop += UiTouch.KeepRowHeight + UiTouch.KeepRowGap;
            }

            PlaceStack(new[] { endRunButton, keepFightingButton }, 0f);
            FillKeepValues();
        }

        /// <summary>One label hanging from the top of the panel, full inner width, one line tall.</summary>
        private static void PlaceTextBlock(TextMeshProUGUI label, float top, float height, float padX,
            TextAlignmentOptions alignment)
        {
            if (label == null)
            {
                return;
            }

            label.alignment = alignment;

            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(padX, 0f);
            rect.offsetMax = new Vector2(-padX, 0f);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -top);
        }

        /// <summary>Icon on the left, amount beside it, both inside the panel's padding.</summary>
        private void PlaceKeepRow(int index, float top)
        {
            if (keepIcons[index] == null)
            {
                return;
            }

            float padX = UiTouch.SheetPadX;
            RectTransform row = (RectTransform)keepIcons[index].transform.parent;

            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.offsetMin = new Vector2(padX, 0f);
            row.offsetMax = new Vector2(-padX, 0f);
            row.sizeDelta = new Vector2(row.sizeDelta.x, UiTouch.KeepRowHeight);
            row.anchoredPosition = new Vector2(row.anchoredPosition.x, -top);

            RectTransform icon = keepIcons[index].rectTransform;
            icon.anchorMin = new Vector2(0f, 0.5f);
            icon.anchorMax = new Vector2(0f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.sizeDelta = new Vector2(UiTouch.KeepIconSize, UiTouch.KeepIconSize);
            icon.anchoredPosition = Vector2.zero;

            RectTransform value = keepValues[index].rectTransform;
            value.anchorMin = Vector2.zero;
            value.anchorMax = Vector2.one;
            value.pivot = new Vector2(0.5f, 0.5f);
            value.offsetMin = new Vector2(UiTouch.KeepIconSize + UiSpace.Space16, 0f);
            value.offsetMax = Vector2.zero;
        }

        /// <summary>Fills "you keep" from the wallet plus whatever this run banks, so the numbers are the truth.</summary>
        private void FillKeepValues()
        {
            if (manager == null || manager.Economy == null)
            {
                return;
            }

            double tokens = manager.Economy.PrestigeTokens + manager.RunEndPreviewTokens;
            double[] amounts = { manager.Economy.Gold, manager.Economy.Gems, tokens };

            for (int i = 0; i < keepValues.Length && i < amounts.Length; i++)
            {
                if (keepValues[i] != null)
                {
                    keepValues[i].SetText("× " + NumberFormatter.Format(amounts[i]));
                }
            }
        }

        /// <summary>The header owns the three currency icons, so the sheet borrows them instead of shipping copies.</summary>
        public void SetCurrencyIcons(Sprite gold, Sprite gem, Sprite token)
        {
            keepSprites[0] = gold;
            keepSprites[1] = gem;
            keepSprites[2] = token;

            for (int i = 0; i < keepIcons.Length; i++)
            {
                if (keepIcons[i] != null)
                {
                    keepIcons[i].sprite = keepSprites[i];
                    keepIcons[i].enabled = keepSprites[i] != null;
                }
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

            // The ribbon carries the question, the body carries the receipt (owner, 2026-11-23).
            SetHeading("END THIS RUN?");
            normalGroup.SetActive(false);
            confirmGroup.SetActive(true);
            PlaceConfirmSheet();
        }

        private void OnConfirmKeep()
        {
            confirmGroup.SetActive(false);
            normalGroup.SetActive(true);
            SetHeading("GAME PAUSED");
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
