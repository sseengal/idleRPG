using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Services;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// Panel C: shop. One uniform list of offers built from data: the rewarded-ad gold boost, the two gem
    /// machines and - since B7 - every purchasable product in <see cref="IapCatalog"/>. A real store only has
    /// to implement <see cref="Services.IIapService"/>; the rows do not change.
    ///
    /// ELI5: the shop is a shelf. Every row is one toy with its own button; all buttons are wired to the same
    /// cashier, so a new toy (gem pack, starter box) is a data change, not a code change.
    /// </summary>
    public sealed class ShopPanelUI : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private TextMeshProUGUI gemsLabel;
        [Tooltip("Scroll content the offer rows are built into (provided by Build MVP Scene).")]
        [SerializeField] private RectTransform offerRoot;

        private GameManager manager;
        private bool bound;
        private bool boundToIap;

        private readonly List<ShopRow> rows = new List<ShopRow>();

        private const float OfferRowHeight = 64f;

        private enum RowKind
        {
            Ad = 0,
            OfflineCap = 1,
            InstantIncome = 2,
            Iap = 3
        }

        private sealed class ShopRow
        {
            public readonly RowKind Kind;
            public readonly string Sku;
            public readonly Button Button;
            public readonly TextMeshProUGUI Label;

            public ShopRow(RowKind kind, string sku, Button button, TextMeshProUGUI label)
            {
                Kind = kind;
                Sku = sku;
                Button = button;
                Label = label;
            }
        }

        private void Start()
        {
            EnsureBound();
        }

        /// <summary>Binds lazily: this panel lives on a page that starts hidden.</summary>
        private void EnsureBound()
        {
            if (bound && manager != null)
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

            bound = true;
            EnsureOfferRows();
            Refresh();
        }

        private void OnEnable()
        {
            EnsureBound();

            if (!boundToIap && manager != null && manager.Iap != null)
            {
                manager.Iap.PurchasesChanged += OnIapChanged;
                boundToIap = true;
            }

            GameEvents.GoldBoostChanged += OnGoldBoostChanged;
            GameEvents.CurrencyChanged += OnCurrencyChanged;
        }

        private void OnDisable()
        {
            if (boundToIap && manager != null && manager.Iap != null)
            {
                manager.Iap.PurchasesChanged -= OnIapChanged;
                boundToIap = false;
            }

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

        private void OnIapChanged()
        {
            Refresh();
        }

        /// <summary>Builds every offer row in code (the scene only hosts the scroll view).</summary>
        private void EnsureOfferRows()
        {
            if (rows.Count > 0)
            {
                return;
            }

            rows.Add(new ShopRow(RowKind.Ad, null, CreateOfferRow("AdRow", OnWatchAdClicked, out TextMeshProUGUI adLabel), adLabel));
            rows.Add(new ShopRow(RowKind.OfflineCap, null, CreateOfferRow("OfflineCapRow", OnOfflineCapClicked, out TextMeshProUGUI capLabel), capLabel));
            rows.Add(new ShopRow(RowKind.InstantIncome, null, CreateOfferRow("InstantIncomeRow", OnInstantIncomeClicked, out TextMeshProUGUI incomeLabel), incomeLabel));

            for (int i = 0; i < IapCatalog.All.Count; i++)
            {
                IapProduct product = IapCatalog.All[i];
                rows.Add(new ShopRow(RowKind.Iap, product.Sku,
                    CreateOfferRow("Iap_" + product.Sku, delegate { OnIapClicked(product.Sku); }, out TextMeshProUGUI iapLabel), iapLabel));
            }
        }

        /// <summary>One shop offer row: a tappable background with a centred label. The scroll layout owns the
        /// position; the row only sets its own height.</summary>
        private Button CreateOfferRow(string name, UnityAction onClick, out TextMeshProUGUI label)
        {
            RectTransform root = offerRoot != null ? offerRoot : (RectTransform)transform;

            GameObject row = new GameObject(name);
            row.transform.SetParent(root, false);

            RectTransform rowRect = row.AddComponent<RectTransform>();
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
            label.fontSize = 20f;
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

        /// <summary>Any product row: the store confirms, then the GameManager grants the contents.</summary>
        private void OnIapClicked(string sku)
        {
            if (manager == null)
            {
                return;
            }

            manager.PurchaseIap(sku);
            Refresh();
        }

        private void Refresh()
        {
            if (manager == null)
            {
                return;
            }

            var boost = manager.Boost;
            bool active = boost != null && boost.IsActive;
            bool noAds = manager.AdsDisabledByNoAds;

            if (gemsLabel != null && manager.Economy != null)
            {
                gemsLabel.SetText(string.Format("{0} gems", NumberFormatter.Format(manager.Economy.Gems)));
            }

            for (int i = 0; i < rows.Count; i++)
            {
                ShopRow row = rows[i];
                TextMeshProUGUI label = row.Label;
                Button button = row.Button;

                switch (row.Kind)
                {
                    case RowKind.Ad:
                        if (label != null)
                        {
                            if (noAds)
                            {
                                label.SetText("ADS REMOVED");
                            }
                            else if (active)
                            {
                                label.SetText(string.Format("BOOST ACTIVE - Gold x{0} ({1})",
                                    boost.GoldMultiplier.ToString("0.#"),
                                    NumberFormatter.FormatDuration(boost.RemainingSeconds)));
                            }
                            else
                            {
                                double multiplier = manager.Balance != null ? manager.Balance.AdGoldBoostMultiplier : 2d;
                                double minutes = manager.Balance != null ? manager.Balance.AdGoldBoostDurationSec / 60d : 60d;
                                label.SetText(string.Format("WATCH AD - Gold x{0} for {1:0} min", multiplier.ToString("0.#"), minutes));
                            }
                        }

                        if (button != null)
                        {
                            button.interactable = !noAds && manager.Ads != null && manager.Ads.IsRewardedAdReady;
                        }
                        break;

                    case RowKind.OfflineCap:
                        if (label != null && manager.Shop != null)
                        {
                            label.SetText(manager.Shop.DescribeOfflineCapOffer());
                        }

                        if (button != null && manager.Shop != null)
                        {
                            button.interactable = manager.Shop.CanAffordOfflineCapExtension;
                        }
                        break;

                    case RowKind.InstantIncome:
                        if (label != null && manager.Shop != null)
                        {
                            label.SetText(manager.Shop.DescribeInstantIncomeOffer());
                        }

                        if (button != null && manager.Shop != null)
                        {
                            button.interactable = manager.Shop.CanAffordInstantIncome;
                        }
                        break;

                    case RowKind.Iap:
                        bool owned = manager.Iap != null && manager.Iap.IsOwned(row.Sku);

                        if (label != null)
                        {
                            label.SetText(owned && row.Sku == IapCatalog.SkuNoAds
                                ? "No Ads - OWNED"
                                : DescribeProduct(row.Sku));
                        }

                        if (button != null)
                        {
                            button.interactable = !owned || row.Sku != IapCatalog.SkuNoAds;
                        }
                        break;
                }
            }
        }

        private static string DescribeProduct(string sku)
        {
            switch (sku)
            {
                case IapCatalog.SkuNoAds:
                    return "No Ads - remove ads forever";
                case IapCatalog.SkuStarterPack:
                    return "Starter Pack - 50 gems + 1h offline cap";
                default:
                    IapProduct product = IapCatalog.Find(sku);
                    return product != null ? product.DisplayName : sku;
            }
        }
    }
}
