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

namespace IdleRPG.UI
{
    /// <summary>
    /// Procedural UI construction: every offer card is built here
    /// </summary>
    public sealed partial class ShopPanelUI : MonoBehaviour
    {

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
    }
}
