using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Economy;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// Top bar: gold, gems and the active gold-boost chip.
    /// Updates only when a relevant event fires — no per-frame polling.
    /// </summary>
    public sealed class HudHeaderUI : MonoBehaviour
    {
        [Header("Currency")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI gemsText;

        [Header("Boost chip")]
        [Tooltip("Hidden until an ad boost is running. It is a row item, so it takes a third block beside the chips.")]
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

            RestyleCurrencyIcons();
            BuildMenuButton();
            ApplyLobbyState(gameManager == null || gameManager.State == GameState.Lobby);

            if (gameManager != null)
            {
                gearMenu = gameObject.AddComponent<GearMenuUI>();
                gearMenu.Initialize(gameManager);
                gearMenu.SetCurrencyIcons(CurrencyIcon("GoldChip"), CurrencyIcon("GemChip"), UiSkin.Current.tokenIcon);
            }

            RefreshAll();
        }

        private void OnEnable()
        {
            GameEvents.CurrencyChanged += OnCurrencyChanged;
            GameEvents.GoldBoostChanged += OnGoldBoostChanged;
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.CurrencyChanged -= OnCurrencyChanged;
            GameEvents.GoldBoostChanged -= OnGoldBoostChanged;
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        /// <summary>The buy affordance belongs to the lobby only, so the plates follow the game state (owner, 2026-11-25).</summary>
        private void OnGameStateChanged(GameState previous, GameState next)
        {
            ApplyLobbyState(next == GameState.Lobby);
        }

        /// <summary>
        /// Turns the two currency plates into shop buttons between runs, and shows their green plus. In a fight the
        /// same plates are inert: no plus, no button, and the raycast is off so a tap falls through to the battle.
        /// </summary>
        private void ApplyLobbyState(bool isLobby)
        {
            ApplyPlateState(goldText, isLobby);
            ApplyPlateState(gemsText, isLobby);
        }

        /// <summary>One plate: badge on or off, and the button made live or dead.</summary>
        private void ApplyPlateState(TextMeshProUGUI value, bool isBuyable)
        {
            if (value == null)
            {
                return;
            }

            Transform plate = value.transform.parent;
            Transform badge = plate.Find("BuyBadge");

            if (badge != null)
            {
                badge.gameObject.SetActive(isBuyable);
            }

            Image face = plate.GetComponent<Image>();

            if (face != null)
            {
                face.raycastTarget = isBuyable;
            }

            Button button = plate.GetComponent<Button>();

            if (button == null)
            {
                button = plate.gameObject.AddComponent<Button>();
                button.targetGraphic = face;
                button.transition = Selectable.Transition.ColorTint;

                ColorBlock colours = button.colors;
                colours.normalColor = Color.white;
                colours.highlightedColor = Color.white;
                colours.selectedColor = Color.white;
                colours.pressedColor = UiTheme.Text;
                colours.disabledColor = Color.white;
                button.colors = colours;
                button.onClick.AddListener(OnPlateClicked);
            }

            button.interactable = isBuyable;
        }

        /// <summary>
        /// A plate tap opens the lobby's SHOP page. Refused mid-fight and while a story scene owns the screen, so the
        /// buy affordance can never appear where it cannot be honoured.
        /// </summary>
        private void OnPlateClicked()
        {
            if (gameManager == null || gameManager.State != GameState.Lobby)
            {
                return;
            }

            if (gameManager.Story != null && gameManager.Story.IsPlaying)
            {
                return;
            }

            StartScreenUI lobby = StartScreenUI.Instance;

            if (lobby != null)
            {
                lobby.OpenShopFromHeader();
            }
        }

        /// <summary>
        /// The currency icon the header already draws, so the pause sheet's "you keep" list wears the same three
        /// pictures as the HUD instead of shipping a second copy of each.
        /// </summary>
        private Sprite CurrencyIcon(string chipName)
        {
            Transform icon = transform.Find("ChipRow/" + chipName + "/Icon");

            if (icon == null)
            {
                return null;
            }

            Image image = icon.GetComponent<Image>();
            return image != null ? image.sprite : null;
        }

        /// <summary>
        /// The menu button at the right end of the chip row. It opens SETTINGS between runs and the PAUSE menu in a
        /// fight. One button, one meaning per state; it never opens an upgrade screen during a fight.
        ///
        /// Two rects, on purpose: the DRAWN block is one chip tall and one touch minimum wide, and an invisible pad
        /// under it is what actually takes the tap, reaching from the bar's bottom up to the safe-area edge. The old
        /// button was 110 x 51 (about 44 x 20 dp - half a finger) and its position came from fractions the layout
        /// group then overwrote.
        /// </summary>
        private void BuildMenuButton()
        {
            // The menu sits OUTSIDE the plate row, pinned to the bar's right edge. The row stops one icon plus one
            // page margin short of it, so the icon lands exactly halfway between the last plate and the screen edge
            // (owner, 2026-11-25).
            GameObject block = UiRuntime.CreateNode("MenuButton", transform);
            RectTransform blockRect = (RectTransform)block.transform;
            blockRect.anchorMin = new Vector2(1f, 0.5f);
            blockRect.anchorMax = new Vector2(1f, 0.5f);
            blockRect.pivot = new Vector2(1f, 0.5f);
            blockRect.sizeDelta = new Vector2(UiSpace.ChipIconSize, UiSpace.ChipHeight);
            blockRect.anchoredPosition = Vector2.zero;

            // The tap pad: the touch minimum square. Its right edge sits on the screen edge (one page margin past the
            // bar), so the whole target is on the glass; that leaves it 8 units off the icon's centre, which the eye
            // cannot see.
            Image pad = UiRuntime.CreatePanel(block.transform, null, Color.clear, raycast: true);
            pad.gameObject.name = "HitPad";
            RectTransform padRect = pad.rectTransform;
            padRect.anchorMin = new Vector2(1f, 0.5f);
            padRect.anchorMax = new Vector2(1f, 0.5f);
            padRect.pivot = new Vector2(1f, 0.5f);
            padRect.sizeDelta = new Vector2(UiTouch.IconButtonHitSize, UiTouch.IconButtonHitSize);
            padRect.anchoredPosition = new Vector2(UiSpace.PageMargin, 0f);

            Button button = pad.gameObject.AddComponent<Button>();
            button.targetGraphic = pad;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnGearClicked);

            // The menu icon carries NO plate (owner, 2026-11-25): it is three bars on the page background.
            Image icon = UiRuntime.CreateIcon("Menu", block.transform);
            icon.sprite = UiRuntime.HamburgerSprite;
            icon.preserveAspect = true;
            icon.color = UiTheme.Text;
            UiRuntime.CenterOn(icon.rectTransform, new Vector2(0.5f, 0.5f),
                new Vector2(UiSpace.ChipIconSize, UiSpace.ChipIconSize));
        }

        /// <summary>Swaps the currency chip icons for the skin's art (the scene keeps placeholder icons).</summary>
        private void RestyleCurrencyIcons()
        {
            UiSkin skin = UiSkin.Current;
            SetChipIcon("GoldChip", skin.goldIcon);
            SetChipIcon("GemChip", skin.gemIcon);
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

        private void OnGoldBoostChanged(bool active, float remainingSeconds, double multiplier)
        {
            if (boostChip != null)
            {
                boostChip.SetActive(active);
            }

            // The boost takes the third row block while it runs; the two chips share the row the rest of the time.
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