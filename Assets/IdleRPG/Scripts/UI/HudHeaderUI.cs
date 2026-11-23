using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Economy;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// Top bar: gold, gems, tokens, stage/wave and the active gold-boost chip.
    /// Updates only when a relevant event fires — no per-frame polling.
    /// </summary>
    public sealed class HudHeaderUI : MonoBehaviour
    {
        [Header("Currency")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI gemsText;
        [SerializeField] private TextMeshProUGUI tokensText;

        [Tooltip("The horizontal row the currency chips and the RANKS button share, so the layout group divides " +
                 "the width and they cannot overlap. Null falls back to the header itself (an older scene).")]
        [SerializeField] private RectTransform chipRow;

        [Header("Progress")]
        [SerializeField] private TextMeshProUGUI stageText;

        [Header("Boost chip")]
        [SerializeField] private GameObject boostChip;
        [SerializeField] private TextMeshProUGUI boostText;

        private EconomyManager economy;
        private GameManager gameManager;
        private GearMenuUI gearMenu;

        /// <summary>Current header, or null when the scene has no HUD header (debug scenes).</summary>
        public static HudHeaderUI Instance { get; private set; }

        /// <summary>The gold counter's rect, so effects (the gold-fly coins) can aim at it.</summary>
        public RectTransform GoldAnchor => goldText != null ? goldText.rectTransform : null;

        [Header("Gold count-up")]
        [Tooltip("How long the gold counter spends rising to a new higher value once coins arrive. Spending snaps instantly.")]
        [SerializeField] private float goldCountUpDurationSec = 0.35f;

        [Tooltip("Safety net: if no coin ever arrives (offline claim, debug scene), the held gain shows after this long.")]
        [SerializeField] private float goldHoldGraceSec = 0.6f;

        private double displayedGold;
        private double countUpFrom;
        private double countUpTo;
        private float countUpElapsed;

        private double pendingGold;
        private float heldTimer;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            HudController hud = HudController.Instance;
            GameManager manager = hud != null ? hud.GameManager : null;

            economy = manager != null ? manager.Economy : null;
            gameManager = manager;

            if (economy == null)
            {
                Debug.LogError("[HudHeaderUI] No economy available; header will stay empty.");
                return;
            }

            RestyleChunky();
            RestyleCurrencyIcons();
            BuildMenuButton();

            if (gameManager != null)
            {
                gearMenu = gameObject.AddComponent<GearMenuUI>();
                gearMenu.Initialize(gameManager);
            }

            if (stageText != null)
            {
                stageText.gameObject.SetActive(gameManager == null || gameManager.State != GameState.Lobby);
            }

            RefreshAll();
        }

        private void OnEnable()
        {
            GameEvents.CurrencyChanged += OnCurrencyChanged;
            GameEvents.StageChanged += OnStageChanged;
            GameEvents.GoldBoostChanged += OnGoldBoostChanged;
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.CurrencyChanged -= OnCurrencyChanged;
            GameEvents.StageChanged -= OnStageChanged;
            GameEvents.GoldBoostChanged -= OnGoldBoostChanged;
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        /// <summary>Width of the menu button, in canvas units. A size, not a gap, so it is not on the UiSpace ladder.</summary>
        private const float MenuButtonWidth = 110f;

        /// <summary>
        /// The gear button at the right end of the chip row. It opens SETTINGS between runs and the PAUSE menu in a
        /// fight. One button, one meaning per state; it never opens an upgrade screen during a fight.
        /// </summary>
        private void BuildMenuButton()
        {
            Transform parent = chipRow != null ? chipRow : transform;

            // Flat block on the header bar, same darker grey as the currency chips (owner, 2026-11-23).
            Image background = UiRuntime.CreatePanel(parent, null, UiTheme.SurfaceChip, raycast: true);
            background.gameObject.name = "MenuButton";
            UiRuntime.Anchor(background.rectTransform, new Vector2(0.88f, 0.10f), new Vector2(0.985f, 0.90f));

            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(OnGearClicked);

            if (chipRow != null)
            {
                LayoutElement element = background.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = MenuButtonWidth;
                element.flexibleWidth = 0f;
            }

            Image icon = UiRuntime.CreateIcon("Gear", background.transform);
            icon.sprite = UiRuntime.GearSprite;
            icon.preserveAspect = true;
            icon.color = UiTheme.Text;
            UiRuntime.Anchor(icon.rectTransform, new Vector2(0.20f, 0.16f), new Vector2(0.80f, 0.84f));
        }

        /// <summary>Swaps the currency chip icons for the skin's art (the scene keeps placeholder icons).</summary>
        private void RestyleCurrencyIcons()
        {
            UiSkin skin = UiSkin.Current;
            SetChipIcon("GoldChip", skin.goldIcon);
            SetChipIcon("GemChip", skin.gemIcon);
            SetChipIcon("TokenChip", skin.tokenIcon);
        }

        private void SetChipIcon(string chipName, Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            foreach (Image image in GetComponentsInChildren<Image>(true))
            {
                if (image.name != "Icon" || image.transform.parent == null)
                {
                    continue;
                }

                if (image.transform.parent.name != chipName)
                {
                    continue;
                }

                image.sprite = sprite;
                image.preserveAspect = true;
            }
        }

        /// <summary>
        /// The gear opens SETTINGS between runs and the PAUSE menu in a fight. It never opens an upgrade screen,
        /// so there is no way to shop mid-fight. A story scene owns the screen, so the gear waits for it.
        /// </summary>
        private void OnGearClicked()
        {
            if (gameManager == null)
            {
                return;
            }

            if (gameManager.Story != null && gameManager.Story.IsPlaying)
            {
                GameEvents.RaiseToast("Wait for the scene to finish.");
                return;
            }

            if (gearMenu == null)
            {
                gearMenu = gameObject.AddComponent<GearMenuUI>();
                gearMenu.Initialize(gameManager);
            }

            gearMenu.Toggle();
        }

        /// <summary>The stage chip belongs to a run; the lobby has none, so it is hidden there.</summary>
        private void OnGameStateChanged(GameState previous, GameState next)
        {
            if (stageText != null)
            {
                stageText.gameObject.SetActive(next != GameState.Lobby);
            }
        }

        /// <summary>Gives the header bar the shared container face, so the bar matches the log and camp bands.</summary>
        private void RestyleChunky()
        {
            Image plate = GetComponent<Image>();

            if (plate == null)
            {
                return;
            }

            if (UiSkin.Current.ApplyContainer(plate))
            {
                return;
            }
        }

        private void OnCurrencyChanged(CurrencyType currencyType, double amount)
        {
            switch (currencyType)
            {
                case CurrencyType.Gold:
                    OnGoldChanged(amount);
                    break;
                case CurrencyType.Gems:
                    SetText(gemsText, amount);
                    break;
                case CurrencyType.PrestigeTokens:
                    SetText(tokensText, amount);
                    break;
            }
        }

        /// <summary>
        /// Gains are held until the gold-fly coins arrive (the counter "receives" the money), then the number
        /// rises. Spending snaps instantly. A held gain also releases after <see cref="goldHoldGraceSec"/> so a
        /// gain without coins (offline claim, debug scene) never stays hidden.
        /// </summary>
        private void OnGoldChanged(double amount)
        {
            if (amount > displayedGold)
            {
                pendingGold = amount;
            }
            else
            {
                // A spend, an ascension reset or a restore: show the truth at once, never taunt with a count-down.
                displayedGold = amount;
                pendingGold = amount;
                countUpElapsed = 0f;
                SetText(goldText, amount);
            }
        }

        /// <summary>Called by the gold-fly effect when the first coin of a burst reaches the counter.</summary>
        public void SignalCoinsArrived()
        {
            if (pendingGold > displayedGold)
            {
                BeginCountUp();
            }
        }

        private void BeginCountUp()
        {
            if (pendingGold > displayedGold)
            {
                countUpFrom = displayedGold;
                countUpTo = pendingGold;
                countUpElapsed = Mathf.Epsilon; // start the first Update tick already moving
            }
        }

        private void Update()
        {
            if (countUpElapsed > 0f)
            {
                countUpElapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(countUpElapsed / Mathf.Max(0.001f, goldCountUpDurationSec));
                double value = countUpFrom + (countUpTo - countUpFrom) * t;
                SetText(goldText, value);

                if (t >= 1f)
                {
                    displayedGold = countUpTo;
                    countUpElapsed = 0f;
                }
                return;
            }

            // Safety net: a held gain that never got coins (offline, ad-claim, debug scene) must still surface.
            if (pendingGold > displayedGold)
            {
                heldTimer += Time.unscaledDeltaTime;

                if (heldTimer >= goldHoldGraceSec)
                {
                    BeginCountUp();
                }
            }
            else if (heldTimer > 0f)
            {
                heldTimer = 0f;
            }
        }

        private void OnStageChanged(int stage, int wave, bool isBossWave)
        {
            if (stageText == null)
            {
                return;
            }

            // One ink: BOSS is marked by the word and its size, not by a red that the palette does not own
            // (owner, 2026-11-23).
            stageText.SetText(string.Format(isBossWave ? "Stage {0}  <size=75%>BOSS</size>" : "Stage {0}  <size=75%>wave {1}</size>", stage, wave));
        }

        private void OnGoldBoostChanged(bool active, float remainingSeconds, double multiplier)
        {
            if (boostChip != null)
            {
                boostChip.SetActive(active);
            }

            if (boostText != null)
            {
                boostText.SetText(string.Format("x{0}  {1}", multiplier.ToString("0.#"), NumberFormatter.FormatCountdown(remainingSeconds)));
            }
        }

        private void RefreshAll()
        {
            displayedGold = economy.Gold;
            SetText(goldText, economy.Gold);
            SetText(gemsText, economy.Gems);
            SetText(tokensText, economy.PrestigeTokens);

            if (boostChip != null)
            {
                boostChip.SetActive(false);
            }
        }

        private static void SetText(TextMeshProUGUI label, double value)
        {
            if (label != null)
            {
                label.SetText(NumberFormatter.Format(value));
            }
        }
    }
}