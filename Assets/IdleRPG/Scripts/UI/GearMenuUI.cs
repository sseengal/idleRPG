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
        private TextMeshProUGUI title;
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
            title.SetText(lobby ? "SETTINGS" : "PAUSED");
            normalGroup.SetActive(true);
            confirmGroup.SetActive(false);

            resumeButton.gameObject.SetActive(!lobby);
            quitButton.gameObject.SetActive(!lobby);
            restoreButton.gameObject.SetActive(lobby);
            privacyButton.gameObject.SetActive(lobby);
            closeButton.gameObject.SetActive(lobby);

            UpdateSoundLabel();
            SetStatus(lobby
                ? "v" + Application.version
                : "The fight is paused.");

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
                new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.86f), UiTheme.SheetFace, raycast: true);

            normalGroup = new GameObject("Normal", typeof(RectTransform));
            normalGroup.transform.SetParent(panel.transform, false);
            UiRuntime.Anchor((RectTransform)normalGroup.transform, Vector2.zero, Vector2.one);

            title = UiRuntime.CreateText(normalGroup.transform, "Title", "SETTINGS", UiType.Role.Title,
                TextAlignmentOptions.Center, UiTheme.Text);
            UiRuntime.Anchor(title.rectTransform, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.96f));

            status = UiRuntime.CreateText(normalGroup.transform, "Status", string.Empty, UiType.Role.Sentence,
                TextAlignmentOptions.Center, UiTheme.TextMuted);
            UiRuntime.Anchor(status.rectTransform, new Vector2(0.06f, 0.77f), new Vector2(0.94f, 0.85f));

            BuildButtons();
            BuildConfirm(panel.transform);
            canvasRoot.SetActive(false);
        }

        private void BuildButtons()
        {
            resumeButton = MakeButton(normalGroup.transform, "Resume", "RESUME",
                new Vector2(0.12f, 0.61f), new Vector2(0.88f, 0.71f), UiTheme.ButtonBlue, Close);

            soundButton = MakeButton(normalGroup.transform, "Sound", "SOUND: ON",
                new Vector2(0.12f, 0.48f), new Vector2(0.88f, 0.58f), UiTheme.ButtonBlue, OnSound);
            soundLabel = soundButton.GetComponentInChildren<TextMeshProUGUI>();

            quitButton = MakeButton(normalGroup.transform, "QuitRun", "QUIT RUN",
                new Vector2(0.12f, 0.35f), new Vector2(0.88f, 0.45f), UiTheme.Orange, OnQuit);

            // Lobby mode reuses the same three slots: sound, restore, close. The pause rows are hidden then.
            restoreButton = MakeButton(normalGroup.transform, "Restore", "RESTORE PURCHASES",
                new Vector2(0.12f, 0.61f), new Vector2(0.88f, 0.71f), UiTheme.ButtonBlue, OnRestore);
            privacyButton = MakeButton(normalGroup.transform, "Privacy", "PRIVACY",
                new Vector2(0.12f, 0.48f), new Vector2(0.88f, 0.58f), UiTheme.ButtonBlue, OnPrivacy);
            closeButton = MakeButton(normalGroup.transform, "Close", "CLOSE",
                new Vector2(0.12f, 0.35f), new Vector2(0.88f, 0.45f), UiTheme.Gold, Close);
        }

        private void BuildConfirm(Transform panel)
        {
            confirmGroup = new GameObject("Confirm", typeof(RectTransform));
            confirmGroup.transform.SetParent(panel, false);
            UiRuntime.Anchor((RectTransform)confirmGroup.transform, Vector2.zero, Vector2.one);

            TextMeshProUGUI confirmTitle = UiRuntime.CreateText(confirmGroup.transform, "Title", "END THIS RUN?",
                UiType.Role.Title, TextAlignmentOptions.Center, UiTheme.Text);
            UiRuntime.Anchor(confirmTitle.rectTransform, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.95f));

            confirmBody = UiRuntime.CreateText(confirmGroup.transform, "Body", string.Empty, UiType.Role.Sentence,
                TextAlignmentOptions.Center, UiTheme.TextMuted);
            UiRuntime.Anchor(confirmBody.rectTransform, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.80f));

            MakeButton(confirmGroup.transform, "EndRun", "END RUN",
                new Vector2(0.12f, 0.42f), new Vector2(0.88f, 0.54f), UiTheme.Orange, OnConfirmEnd);
            MakeButton(confirmGroup.transform, "KeepFighting", "KEEP FIGHTING",
                new Vector2(0.12f, 0.26f), new Vector2(0.88f, 0.38f), UiTheme.ButtonBlue, OnConfirmKeep);

            confirmGroup.SetActive(false);
        }

        private static UiPlateButton MakeButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Color face, UnityEngine.Events.UnityAction onClick)
        {
            return UiPlateButton.Create(parent, name, anchorMin, anchorMax, label, UiType.Role.Section,
                face, face, onClick);
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
            if (status != null)
            {
                status.SetText(text);
            }
        }

        private void OnSound()
        {
            if (manager == null || manager.Audio == null)
            {
                return;
            }

            bool muted = manager.Audio.ToggleMuted();
            UpdateSoundLabel();
            SetStatus(muted ? "Sound off." : "Sound on.");
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
