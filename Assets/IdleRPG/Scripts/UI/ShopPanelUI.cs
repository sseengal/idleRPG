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
    public sealed class ShopPanelUI : MonoBehaviour
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

        /// <summary>Builds every offer card at runtime (the scene only hosts the scroll view).</summary>
        private void EnsureOfferRows()
        {
            if (rows.Count > 0)
            {
                return;
            }

            rows.Add(CreateRow(RowKind.OfflineCap, null, "Offline Cap", "extra income while away", OnOfflineCapClicked));
            rows.Add(CreateRow(RowKind.InstantIncome, null, "Fast-Forward", "an hour of income right now", OnInstantIncomeClicked));
            rows.Add(CreateRow(RowKind.Iap, IapCatalog.SkuNoAds, "No Ads", "remove ads forever", delegate { OnIapClicked(IapCatalog.SkuNoAds); }));
            rows.Add(CreateRow(RowKind.Iap, IapCatalog.SkuGemsSmall, "5 Gems", "street-price gems", delegate { OnIapClicked(IapCatalog.SkuGemsSmall); }));
            rows.Add(CreateRow(RowKind.Iap, IapCatalog.SkuGemsMedium, "30 Gems", "most popular bundle", delegate { OnIapClicked(IapCatalog.SkuGemsMedium); }));
            rows.Add(CreateRow(RowKind.Iap, IapCatalog.SkuGemsLarge, "110 Gems", "best value bundle", delegate { OnIapClicked(IapCatalog.SkuGemsLarge); }));
            rows.Add(CreateRow(RowKind.Iap, IapCatalog.SkuStarterPack, "Starter Pack", "50 gems + 1h offline cap", delegate { OnIapClicked(IapCatalog.SkuStarterPack); }));

            // B7 S4: every gems-priced permanent track is a shop row - a new sink is a JSON card, not code.
            if (manager.Tracks != null)
            {
                IReadOnlyList<ProgressionTrack> tracks = manager.Tracks.Tracks;

                for (int i = 0; i < tracks.Count; i++)
                {
                    ProgressionTrack track = tracks[i];

                    if (track.Currency != CurrencyType.Gems || track.IsAutomation ||
                        track.EffectKind != TrackEffectKind.GlobalPercent)
                    {
                        continue;
                    }

                    ProgressionTrack captured = track;
                    rows.Add(CreateRow(RowKind.GemTrack, track.Id, track.DisplayName, DescribeGemTrack(track),
                        delegate { OnGemTrackClicked(captured); }, captured));
                }
            }
        }

        /// <summary>One offer card, laid out exactly like a prestige upgrade row (name/effect/value/buy).</summary>
        private ShopRow CreateRow(RowKind kind, string sku, string displayName, string description, UnityAction onClick,
            ProgressionTrack track = default)
        {
            RectTransform root = offerRoot != null ? offerRoot : (RectTransform)transform;

            GameObject row = new GameObject("Offer_" + sku ?? kind.ToString());
            row.transform.SetParent(root, false);

            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.minHeight = OfferRowHeight;
            layout.preferredHeight = OfferRowHeight;

            Image card = row.AddComponent<Image>();
            card.sprite = rowSprite;
            card.type = Image.Type.Sliced;
            card.color = CardColor;

            TextMeshProUGUI name = UiText("Name", row.transform,
                new Vector2(0.03f, 0.58f), new Vector2(0.56f, 0.96f), 26f, TextAlignmentOptions.MidlineLeft, TextColor);
            name.SetText(displayName);

            TextMeshProUGUI effect = UiText("Effect", row.transform,
                new Vector2(0.03f, 0.12f), new Vector2(0.56f, 0.55f), 18f, TextAlignmentOptions.MidlineLeft, DimTextColor);
            effect.SetText(description);

            TextMeshProUGUI value = UiText("Value", row.transform,
                new Vector2(0.56f, 0.58f), new Vector2(0.76f, 0.96f), 24f, TextAlignmentOptions.Center, TextColor);

            Button button = CreateActionButton(row.transform, onClick);
            return new ShopRow(kind, sku, button, name, effect, value, track);
        }

        /// <summary>Human line for a gem track, e.g. "+5% Gold per level (max +50%)".</summary>
        private static string DescribeGemTrack(ProgressionTrack track)
        {
            string effect = track.GlobalEffect.ToDisplayName();
            string perLevel = string.Format("+{0:0.#}% {1} per level", track.GainPerLevel * 100f, effect);
            bool capped = track.RawMaxLevel > 0;
            return capped
                ? string.Format("{0} (max +{1:0.#}%)", perLevel, track.GainPerLevel * 100f * track.RawMaxLevel)
                : perLevel;
        }

        /// <summary>Buys one level of a gems-priced track through the one checkout.</summary>
        private void OnGemTrackClicked(ProgressionTrack track)
        {
            if (manager?.Tracks == null)
            {
                return;
            }

            PurchaseResult result = manager.Tracks.TryBuy(track, null, 1, out double spent, out int newLevel);

            if (result == PurchaseResult.Bought)
            {
                manager.Save?.MarkDirty("gem-sink");
                GameEvents.RaiseToast(string.Format("{0} -> Lv {1} ({2:0} gems)", track.DisplayName, newLevel, spent));
            }
            else if (result == PurchaseResult.Maxed)
            {
                GameEvents.RaiseToast(string.Format("{0} is already maxed.", track.DisplayName));
            }
            else if (result == PurchaseResult.CannotAfford)
            {
                GameEvents.RaiseToast(string.Format("Not enough gems ({0:0} needed).",
                    manager.Tracks.Cost(track, null, 1)));
            }

            Refresh();
        }

        private TextMeshProUGUI UiText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            float size, TextAlignmentOptions alignment, Color color)
        {
            GameObject labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);

            RectTransform rect = labelObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>The BUY button on the right of each card (ui_button sprite, like the prestige rows).</summary>
        private Button CreateActionButton(Transform parent, UnityAction onClick)
        {
            GameObject buttonObject = new GameObject("BuyButton");
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.78f, 0.16f);
            rect.anchorMax = new Vector2(0.97f, 0.86f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image background = buttonObject.AddComponent<Image>();
            background.sprite = buttonSprite;
            background.type = Image.Type.Sliced;
            background.color = Color.white;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);

            RectTransform labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4f, 4f);
            labelRect.offsetMax = new Vector2(-4f, -4f);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = TextColor;
            label.raycastTarget = false;
            label.SetText("BUY");
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

        /// <summary>Fast-forward: GameManager owns the order of operations (quote -> charge -> pay).</summary>
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

        /// <summary>Any product card: the store confirms, then the GameManager grants the contents.</summary>
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
