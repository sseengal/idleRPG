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

        /// <summary>Drop shadow for the sheet's buttons: a close, stronger step and a faint lower one.</summary>
        private const float ButtonShadowNear = -5f;
        private const float ButtonShadowFar = -12f;

        private GameManager manager;
        private GameObject canvasRoot;
        private GameObject normalGroup;
        private GameObject confirmGroup;
        private TextMeshProUGUI title;
        private TextMeshProUGUI bannerLabel;
        private TextMeshProUGUI status;
        private TextMeshProUGUI soundLabel;
        private TextMeshProUGUI confirmBody;
        private UiPlateButton resumeButton;
        private UiPlateButton soundButton;
        private UiPlateButton quitButton;
        private UiPlateButton restoreButton;
        private UiPlateButton privacyButton;
        private UiPlateButton closeButton;
        private bool pausedByMenu;

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

            LayoutButtons(lobby);
            SetHeading(lobby ? "SETTINGS" : "GAME PAUSED");

            UpdateSoundLabel();

            // No body line in a run: the ribbon says it. The lobby keeps its version, and both keep feedback.
            SetStatus(lobby ? "v" + Application.version : string.Empty);

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
            UiRuntime.Anchor(scrim.rectTransform, Vector2.zero, Vector2.one);

            Image panel = UiRuntime.Plate(canvasRoot.transform, "Panel",
                new Vector2(0.10f, 0.20f), new Vector2(0.90f, 0.80f), UiTheme.SheetFace, raycast: true);

            // The sheet art carries its own colour, so it is drawn white (no tint) at its own border scale.
            UiSkin skin = UiSkin.Current;
            if (skin.sheetSprite != null)
            {
                panel.sprite = skin.sheetSprite;
                panel.type = Image.Type.Sliced;
                panel.color = Color.white;
                panel.pixelsPerUnitMultiplier = skin.sheetBorderScale;
            }

            BuildBanner(panel.transform);

            normalGroup = new GameObject("Normal", typeof(RectTransform));
            normalGroup.transform.SetParent(panel.transform, false);
            UiRuntime.Anchor((RectTransform)normalGroup.transform, Vector2.zero, Vector2.one);

            // Fallback only: when the skin has a ribbon, the ribbon carries the heading instead of this label.
            title = UiRuntime.CreateText(normalGroup.transform, "Title", "SETTINGS", UiType.Role.Title,
                TextAlignmentOptions.Center, UiTheme.Text);
            UiRuntime.Anchor(title.rectTransform, new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.90f));
            title.gameObject.SetActive(bannerLabel == null);

            status = UiRuntime.CreateText(normalGroup.transform, "Status", string.Empty, UiType.Role.Sentence,
                TextAlignmentOptions.Center, UiTheme.TextMuted);
            UiRuntime.Anchor(status.rectTransform, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.08f));
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
            // Anchors here are placeholders: LayoutButtons() stacks the visible rows at Open time.
            resumeButton = MakeButton(normalGroup.transform, "Resume", "RESUME",
                Vector2.zero, Vector2.one, ButtonFace.Primary, Close);

            soundButton = MakeButton(normalGroup.transform, "Sound", "SOUND: ON",
                Vector2.zero, Vector2.one, ButtonFace.Neutral, OnSound);
            soundLabel = soundButton.GetComponentInChildren<TextMeshProUGUI>();

            quitButton = MakeButton(normalGroup.transform, "QuitRun", "QUIT RUN",
                Vector2.zero, Vector2.one, ButtonFace.Danger, OnQuit);

            // Lobby mode reuses the same stack: sound, restore, privacy, close. The pause rows are hidden then.
            restoreButton = MakeButton(normalGroup.transform, "Restore", "RESTORE PURCHASES",
                Vector2.zero, Vector2.one, ButtonFace.Neutral, OnRestore);
            privacyButton = MakeButton(normalGroup.transform, "Privacy", "PRIVACY",
                Vector2.zero, Vector2.one, ButtonFace.Neutral, OnPrivacy);
            closeButton = MakeButton(normalGroup.transform, "Close", "CLOSE",
                Vector2.zero, Vector2.one, ButtonFace.Primary, Close);
        }

        /// <summary>
        /// Stacks the rows that are visible in this mode. Run = 3 rows (resume, sound, quit), lobby = 4 rows
        /// (sound, restore, privacy, close). The count is passed in, so no two buttons can share a slot.
        /// </summary>
        private void LayoutButtons(bool lobby)
        {
            if (lobby)
            {
                Place(soundButton, 0, 4);
                Place(restoreButton, 1, 4);
                Place(privacyButton, 2, 4);
                Place(closeButton, 3, 4);
            }
            else
            {
                Place(resumeButton, 0, 3);
                Place(soundButton, 1, 3);
                Place(quitButton, 2, 3);
            }
        }

        /// <summary>One row of the stack, in panel space. Top below the ribbon, bottom above the rim.</summary>
        private static void Place(UiPlateButton button, int index, int count)
        {
            if (button == null)
            {
                return;
            }

            const float Top = 0.85f;
            const float Bottom = 0.11f;
            const float Fill = 0.71f;

            float slot = (Top - Bottom) / Mathf.Max(1, count);
            float height = slot * Fill;
            float yMax = Top - index * slot;
            float yMin = yMax - height;

            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(0.12f, yMin);
            rect.anchorMax = new Vector2(0.88f, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void BuildConfirm(Transform panel)
        {
            confirmGroup = new GameObject("Confirm", typeof(RectTransform));
            confirmGroup.transform.SetParent(panel, false);
            UiRuntime.Anchor((RectTransform)confirmGroup.transform, Vector2.zero, Vector2.one);

            TextMeshProUGUI confirmTitle = UiRuntime.CreateText(confirmGroup.transform, "Title", "END THIS RUN?",
                UiType.Role.Title, TextAlignmentOptions.Center, UiTheme.Text);
            UiRuntime.Anchor(confirmTitle.rectTransform, new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.92f));

            confirmBody = UiRuntime.CreateText(confirmGroup.transform, "Body", string.Empty, UiType.Role.Sentence,
                TextAlignmentOptions.Center, UiTheme.TextMuted);
            UiRuntime.Anchor(confirmBody.rectTransform, new Vector2(0.08f, 0.56f), new Vector2(0.92f, 0.76f));

            MakeButton(confirmGroup.transform, "EndRun", "END RUN",
                new Vector2(0.12f, 0.30f), new Vector2(0.88f, 0.44f), ButtonFace.Danger, OnConfirmEnd);
            MakeButton(confirmGroup.transform, "KeepFighting", "KEEP FIGHTING",
                new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.26f), ButtonFace.Neutral, OnConfirmKeep);

            confirmGroup.SetActive(false);
        }

        private static UiPlateButton MakeButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, ButtonFace face, UnityEngine.Events.UnityAction onClick)
        {
            UiPlateButton view = UiPlateButton.Create(parent, name, anchorMin, anchorMax, label, UiType.Role.Section,
                Color.white, Color.white, onClick, face);

            // Two offset copies fake a soft drop shadow under the button, same trick as the ribbon. One hard
            // copy reads as a second button; two steps read as depth on the sheet.
            Shadow near = view.gameObject.AddComponent<Shadow>();
            near.effectColor = new Color(0f, 0f, 0f, 0.38f);
            near.effectDistance = new Vector2(0f, ButtonShadowNear);
            near.useGraphicAlpha = true;

            Shadow far = view.gameObject.AddComponent<Shadow>();
            far.effectColor = new Color(0f, 0f, 0f, 0.18f);
            far.effectDistance = new Vector2(0f, ButtonShadowFar);
            far.useGraphicAlpha = true;

            return view;
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
        }

        private void OnConfirmKeep()
        {
            confirmGroup.SetActive(false);
            normalGroup.SetActive(true);
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
