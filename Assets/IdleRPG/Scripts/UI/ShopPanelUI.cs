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
        }
    }
}