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

        [Header("Progress")]
        [SerializeField] private TextMeshProUGUI stageText;

        [Header("Boost chip")]
        [SerializeField] private GameObject boostChip;
        [SerializeField] private TextMeshProUGUI boostText;

        private EconomyManager economy;
        private GameManager manager;
        private LeaderboardSheetUI leaderboardSheet;

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

            this.manager = manager;
            economy = manager != null ? manager.Economy : null;

            if (economy == null)
            {
                Debug.LogError("[HudHeaderUI] No economy available; header will stay empty.");
                return;
            }

            BuildLeaderboardButton();
            RefreshAll();
        }

        private void OnEnable()
        {
            GameEvents.CurrencyChanged += OnCurrencyChanged;
            GameEvents.StageChanged += OnStageChanged;
            GameEvents.GoldBoostChanged += OnGoldBoostChanged;
        }

        private void OnDisable()
        {
            GameEvents.CurrencyChanged -= OnCurrencyChanged;
            GameEvents.StageChanged -= OnStageChanged;
            GameEvents.GoldBoostChanged -= OnGoldBoostChanged;
        }

        /// <summary>Runtime trophy button (right edge of the top bar) - opens the global ranks sheet.</summary>
        private void BuildLeaderboardButton()
        {
            Button trophy = UiRuntime.CreateButton(transform, "LeaderboardButton", "RANKS",
                new Vector2(0.90f, 0.10f), new Vector2(0.985f, 0.90f), OpenLeaderboard,
                new Color(0.30f, 0.36f, 0.50f, 1f));
            TextMeshProUGUI label = trophy.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.fontSize = 13f;
            }
        }

        private void OpenLeaderboard()
        {
            if (manager == null || manager.Leaderboard == null)
            {
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            if (leaderboardSheet == null)
            {
                GameObject node = new GameObject("LeaderboardSheet", typeof(RectTransform));
                node.transform.SetParent(canvas.transform, false);
                leaderboardSheet = node.AddComponent<LeaderboardSheetUI>();
                leaderboardSheet.Build(manager);
            }
            else
            {
                leaderboardSheet.Show();
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

            stageText.SetText(string.Format(isBossWave ? "Stage {0}  <size=75%><color=#FF6B6B>BOSS</color></size>" : "Stage {0}  <size=75%>wave {1}</size>", stage, wave));
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