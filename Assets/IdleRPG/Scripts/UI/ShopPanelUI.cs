using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// Panel C: shop. MVP offers the rewarded-ad gold boost (x2 for one hour).
    /// A real ad SDK only needs to implement <see cref="Services.IAdService"/>.
    /// </summary>
    public sealed class ShopPanelUI : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Button watchAdButton;
        [SerializeField] private TextMeshProUGUI watchAdLabel;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private TextMeshProUGUI gemsLabel;

        private GameManager manager;

        // Runtime-built gem-sink rows (placeholder UI: the shop gets a proper redesign in Step 19).
        // Both are built from the same builder so a third offer is one line, not one copied block.
        private Button offlineCapButton;
        private TextMeshProUGUI offlineCapLabel;
        private Button instantIncomeButton;
        private TextMeshProUGUI instantIncomeLabel;

        private const float OfferRowHeight = 72f;
        private const float OfferRowSpacing = 8f;

        private void Start()
        {
            EnsureBound();
        }

        /// <summary>Binds lazily: this panel lives on a page that starts hidden.</summary>
        private void EnsureBound()
        {
            if (manager != null)
            {
                return;
            }

            HudController hud = HudController.Instance;
            manager = hud != null ? hud.GameManager : null;

            if (manager == null)
            {
                Debug.LogWarning("[ShopPanelUI] GameManager not ready yet; will bind on next open.");
                return;
            }

            if (watchAdButton != null)
            {
                watchAdButton.onClick.RemoveAllListeners();
                watchAdButton.onClick.AddListener(OnWatchAdClicked);
            }

            EnsureOfferRows();
            Refresh();
        }

        private void OnEnable()
        {
            EnsureBound();
            GameEvents.GoldBoostChanged += OnGoldBoostChanged;
            GameEvents.CurrencyChanged += OnCurrencyChanged;
        }

        private void OnDisable()
        {
            GameEvents.GoldBoostChanged -= OnGoldBoostChanged;
            GameEvents.CurrencyChanged -= OnCurrencyChanged;
        }

        private void OnGoldBoostChanged(bool active, float remainingSeconds, double multiplier)
        {
            Refresh();
        }

        private void OnCurrencyChanged(Economy.CurrencyType currencyType, double amount)
        {
            if (currencyType == Economy.CurrencyType.Gems)
            {
                Refresh();
            }
        }

        /// <summary>
        /// Builds the gem-sink rows in code so the scene does not need a rebuild for every new offer.
        /// Placeholder styling on purpose - Step 19 gives the shop its real layout.
        /// Rows stack upwards from the bottom of the panel.
        /// </summary>
        private void EnsureOfferRows()
        {
            if (manager == null || manager.Shop == null)
            {
                return;
            }

            if (offlineCapButton == null)
            {
                offlineCapButton = CreateOfferRow("OfflineCapRow", 0, OnOfflineCapClicked, out offlineCapLabel);
            }

            if (instantIncomeButton == null)
            {
                instantIncomeButton = CreateOfferRow("InstantIncomeRow", 1, OnInstantIncomeClicked, out instantIncomeLabel);
            }
        }

        /// <summary>One shop offer row: a tappable background with a centred label.</summary>
        private Button CreateOfferRow(string name, int stackIndex, UnityEngine.Events.UnityAction onClick,
            out TextMeshProUGUI label)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(transform, false);

            RectTransform rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 0f);
            rowRect.anchorMax = new Vector2(1f, 0f);
            rowRect.pivot = new Vector2(0.5f, 0f);
            rowRect.anchoredPosition = new Vector2(0f, OfferRowSpacing + stackIndex * (OfferRowHeight + OfferRowSpacing));
            rowRect.sizeDelta = new Vector2(0f, OfferRowHeight);

            Image background = row.AddComponent<Image>();
            background.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);

            Button button = row.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(row.transform, false);

            RectTransform labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 4f);
            labelRect.offsetMax = new Vector2(-12f, -4f);

            label = labelObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            return button;
        }

        private void OnOfflineCapClicked()
        {
            if (manager == null || manager.Shop == null)
            {
                return;
            }

            if (manager.Shop.TryBuyOfflineCapExtension())
            {
                manager.Save?.MarkDirty("shop");
            }

            Refresh();
        }

        /// <summary>
        /// Fast-forward: GameManager owns the order of operations (quote -> charge -> pay) because the gems and
        /// the payout live in two different services.
        /// </summary>
        private void OnInstantIncomeClicked()
        {
            if (manager == null)
            {
                return;
            }

            manager.BuyInstantIncome();
            Refresh();
        }

        private void OnWatchAdClicked()
        {
            if (manager != null)
            {
                manager.WatchAdForGoldBoost();
            }
        }

        private void Refresh()
        {
            if (manager == null)
            {
                return;
            }

            var boost = manager.Boost;
            bool active = boost != null && boost.IsActive;

            if (watchAdLabel != null)
            {
                watchAdLabel.SetText(active ? "BOOST ACTIVE" : "WATCH AD");
            }

            if (watchAdButton != null)
            {
                watchAdButton.interactable = manager.Ads != null && manager.Ads.IsRewardedAdReady;
            }

            if (statusLabel != null)
            {
                if (active)
                {
                    statusLabel.SetText(string.Format("Gold x{0} for {1}", boost.GoldMultiplier.ToString("0.#"),
                        NumberFormatter.FormatDuration(boost.RemainingSeconds)));
                }
                else
                {
                    double multiplier = manager.Balance != null ? manager.Balance.AdGoldBoostMultiplier : 2d;
                    double minutes = manager.Balance != null ? manager.Balance.AdGoldBoostDurationSec / 60d : 60d;
                    statusLabel.SetText(string.Format("Watch an ad for Gold x{0} for {1:0} min.", multiplier.ToString("0.#"), minutes));
                }
            }

            if (gemsLabel != null && manager.Economy != null)
            {
                gemsLabel.SetText(string.Format("{0} gems", NumberFormatter.Format(manager.Economy.Gems)));
            }

            if (offlineCapLabel != null && manager.Shop != null)
            {
                offlineCapLabel.SetText(manager.Shop.DescribeOfflineCapOffer());
            }

            if (offlineCapButton != null && manager.Shop != null)
            {
                offlineCapButton.interactable = manager.Shop.CanAffordOfflineCapExtension;
            }

            if (instantIncomeLabel != null && manager.Shop != null)
            {
                instantIncomeLabel.SetText(manager.Shop.DescribeInstantIncomeOffer());
            }

            if (instantIncomeButton != null && manager.Shop != null)
            {
                instantIncomeButton.interactable = manager.Shop.CanAffordInstantIncome;
            }
        }
    }
}