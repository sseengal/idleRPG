using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Progression;
using IdleRPG.Services;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// Panel C: shop, styled to match the ASCEND page. A headline action button (watch an ad) sits in the same
    /// header band as the ASCEND button, and every offer is drawn as a card like a prestige upgrade row:
    /// name + description on the left, value in the middle, one BUY button on the right.
    ///
    /// ELI5: the shop now looks like the toy-store version of the ascend tree - same shape, different coins.
    /// </summary>
    public sealed partial class ShopPanelUI : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private TextMeshProUGUI gemsLabel;
        [SerializeField] private Button watchAdButton;
        [SerializeField] private TextMeshProUGUI watchAdLabel;
        [Tooltip("Scroll content the offer rows are built into (provided by Build MVP Scene).")]
        [SerializeField] private RectTransform offerRoot;
        [Tooltip("Card sprite for offer rows (ui_panel), set by the scene builder.")]
        [SerializeField] private Sprite rowSprite;
        [Tooltip("Button sprite for offer rows (ui_button), set by the scene builder.")]
        [SerializeField] private Sprite buttonSprite;

        private GameManager manager;
        private bool bound;
        private bool boundToIap;

        private readonly List<ShopRow> rows = new List<ShopRow>();

        private const float OfferRowHeight = 116f;

        // Palette shared with the scene builder (MvpSceneBuilder.TextColor / DimTextColor).
        private static readonly Color TextColor = new Color(0.94f, 0.96f, 1f, 1f);
        private static readonly Color DimTextColor = new Color(0.75f, 0.78f, 0.86f, 1f);
        private static readonly Color CardColor = new Color(1f, 1f, 1f, 0.30f);

        private enum RowKind
        {
            OfflineCap = 0,
            InstantIncome = 1,
            Iap = 2,

            /// <summary>A gems-priced permanent track (B7 S4) - built from data, not hardcoded.</summary>
            GemTrack = 3
        }

        private sealed class ShopRow
        {
            public readonly RowKind Kind;
            public readonly string Sku;
            public readonly Button Button;
            public readonly TextMeshProUGUI Name;
            public readonly TextMeshProUGUI Effect;
            public readonly TextMeshProUGUI Value;

            /// <summary>Set for <see cref="RowKind.GemTrack"/> rows: which track this card buys.</summary>
            public readonly ProgressionTrack Track;

            public ShopRow(RowKind kind, string sku, Button button, TextMeshProUGUI name,
                TextMeshProUGUI effect, TextMeshProUGUI value, ProgressionTrack track = default)
            {
                Kind = kind;
                Sku = sku;
                Button = button;
                Name = name;
                Effect = effect;
                Value = value;
                Track = track;
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

            // Rows are built while this page is hidden, so the scroll layout has not run yet - force it once
            // so the cards stack correctly the moment the shop opens (the ascend page builds its rows in the
            // scene, so it never needed this).
            if (offerRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(offerRoot);
            }

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
                string streak = "";

                if (manager.DailyStreak != null && manager.Balance != null)
                {
                    int day = manager.DailyStreak.CurrentDay;

                    if (day > 0)
                    {
                        int cap = manager.Balance.DailyStreakCap;
                        streak = string.Format("  ·  Day-{0} streak (next +{1})",
                            day, manager.Balance.DailyStreakGemsForDay(day == cap ? cap : day + 1));
                    }
                    else
                    {
                        streak = string.Format("  ·  today +{0} gems", manager.Balance.DailyStreakGemsForDay(1));
                    }
                }

                gemsLabel.SetText(string.Format("{0} gems{1}", NumberFormatter.Format(manager.Economy.Gems), streak));
            }

            if (watchAdLabel != null)
            {
                if (noAds)
                {
                    watchAdLabel.SetText("ADS REMOVED");
                }
                else if (active)
                {
                    watchAdLabel.SetText("BOOST ACTIVE");
                }
                else
                {
                    double waitSeconds = 0d;
                    bool capsOk = manager.AdCaps != null &&
                                  manager.AdCaps.CanShow(IdleRPG.Data.AdPlacementId.GoldBoost, out waitSeconds);
                    int left = manager.AdCaps != null
                        ? manager.AdCaps.RemainingToday(IdleRPG.Data.AdPlacementId.GoldBoost)
                        : 0;

                    watchAdLabel.SetText(!capsOk
                        ? (waitSeconds > 0d
                            ? string.Format("WATCH AD (in {0:0}m)", waitSeconds / 60d)
                            : "DAILY AD LIMIT")
                        : string.Format("WATCH AD ({0} left)", left));
                }
            }

            if (watchAdButton != null)
            {
                bool capsOk = manager.AdCaps != null &&
                              manager.AdCaps.CanShow(IdleRPG.Data.AdPlacementId.GoldBoost, out _);
                watchAdButton.interactable = !noAds && capsOk && manager.Ads != null && manager.Ads.IsRewardedAdReady;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                ShopRow row = rows[i];

                if (row.Kind == RowKind.OfflineCap)
                {
                    row.Name.SetText(manager.Shop != null && manager.Shop.IsOfflineCapMaxed ? "Offline Cap (max)" : "Offline Cap");
                    row.Effect.SetText(manager.Shop != null ? manager.Shop.DescribeOfflineCapOffer() : "");
                    row.Value.SetText(manager.Shop != null ? string.Format("{0:0} gems", manager.Shop.NextOfflineCapExtensionCost) : "");
                    row.Button.interactable = manager.Shop != null && manager.Shop.CanAffordOfflineCapExtension;
                }
                else if (row.Kind == RowKind.InstantIncome)
                {
                    row.Effect.SetText(manager.Shop != null ? manager.Shop.DescribeInstantIncomeOffer() : "");
                    row.Value.SetText(manager.Shop != null ? string.Format("{0:0} gems", manager.Shop.InstantIncomeGemCost) : "");
                    row.Button.interactable = manager.Shop != null && manager.Shop.CanAffordInstantIncome;
                }
                else if (row.Kind == RowKind.GemTrack)
                {
                    ProgressionTrack track = row.Track;
                    int level = manager.Tracks != null ? manager.Tracks.GetLevel(track, null) : 0;
                    bool maxed = manager.Tracks != null && manager.Tracks.IsMaxed(track, null);
                    double cost = manager.Tracks != null ? manager.Tracks.Cost(track, null, 1) : 0d;

                    row.Name.SetText(maxed
                        ? string.Format("{0} (max)", track.DisplayName)
                        : track.DisplayName);
                    row.Value.SetText(string.Format("Lv {0}", level));
                    SetButtonLabel(row.Button, maxed ? "MAX" : string.Format("{0:0} gems", cost));
                    row.Button.interactable = !maxed && manager.Tracks != null && manager.Tracks.CanAfford(track, null, 1);
                }
                else
                {
                    bool owned = manager.Iap != null && manager.Iap.IsOwned(row.Sku);
                    bool isNoAds = row.Sku == IapCatalog.SkuNoAds;

                    row.Name.SetText(isNoAds && owned ? "No Ads (owned)" : row.Name.text);
                    row.Value.SetText(DescribeValue(row.Sku));
                    SetButtonLabel(row.Button, isNoAds && owned ? "OWNED" : "BUY");
                    row.Button.interactable = !owned || !isNoAds;
                }
            }
        }

        private static string DescribeValue(string sku)
        {
            switch (sku)
            {
                case IapCatalog.SkuNoAds:
                    return "1-time";
                case IapCatalog.SkuGemsSmall:
                    return "+5";
                case IapCatalog.SkuGemsMedium:
                    return "+30";
                case IapCatalog.SkuGemsLarge:
                    return "+110";
                case IapCatalog.SkuStarterPack:
                    return "50 gems";
                default:
                    return "";
            }
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.SetText(label);
            }
        }
    }
}
