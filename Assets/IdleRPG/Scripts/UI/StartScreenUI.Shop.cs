using System.Collections.Generic;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Progression;
using IdleRPG.Services;
using IdleRPG.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// The SHOP page between runs. v1.0 sells a lean shelf: No Ads, one 30-gem pack, the Starter Pack (50 gems +
    /// a 30-minute gold boost) and the Golden Foundry gem track. Prices are shown by the store at checkout; the
    /// button says BUY, never a made-up price.
    /// </summary>
    public sealed partial class StartScreenUI
    {
        private GameObject shopPanel;
        private bool shopBuilt;
        private TextMeshProUGUI shopGemsLabel;
        private ProgressionTrack foundryTrack;
        private readonly List<ShopCard> shopCards = new List<ShopCard>();

        private sealed class ShopCard
        {
            public string BaseName;
            public string Sku;              // null for the gem-track card
            public ProgressionTrack Track;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Value;
            public UiPlateButton Buy;
            public TextMeshProUGUI BuyLabel;
        }

        private void ShowShop()
        {
            EnsureManager();

            if (!shopBuilt)
            {
                BuildShop();
            }

            if (shopPanel != null)
            {
                shopPanel.SetActive(true);
            }

            RefreshShop();
        }

        private void HideShop()
        {
            if (shopPanel != null && shopPanel.activeSelf)
            {
                shopPanel.SetActive(false);
            }
        }

        private void BuildShop()
        {
            shopBuilt = true;
            shopPanel = CreateLobbyPanel("ShopPanel");

            Text(shopPanel.transform, "Title", new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.98f),
                "SHOP", UiType.Role.Title, UiTheme.Text);

            shopGemsLabel = Text(shopPanel.transform, "Gems", new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.87f),
                "GEMS 0", UiType.Role.Sentence, UiTheme.Text);

            BuildShopCard("NO ADS", "Remove every ad. Camp rest stays free.", "1-TIME",
                IapCatalog.SkuNoAds, 0.60f, 0.76f);

            BuildShopCard("30 GEMS", "Adds 30 gems to the purse.", "+30",
                IapCatalog.SkuGemsMedium, 0.43f, 0.59f);

            BuildShopCard("STARTER PACK", "50 gems + a 30-minute gold boost.", "50 + 2x GOLD",
                IapCatalog.SkuStarterPack, 0.26f, 0.42f);

            foundryTrack = FindFoundryTrack();

            if (foundryTrack.Id != null)
            {
                BuildShopCard(foundryTrack.DisplayName, DescribeFoundry(foundryTrack), "Lv 0",
                    null, 0.09f, 0.25f);
            }

            Text(shopPanel.transform, "Note", new Vector2(0.04f, 0.01f), new Vector2(0.96f, 0.07f),
                "Prices are shown at checkout.", UiType.Role.Small, UiTheme.Text);

            if (manager != null && manager.Iap != null)
            {
                manager.Iap.PurchasesChanged += RefreshShop;
            }

            GameEvents.CurrencyChanged += OnShopCurrencyChanged;
        }

        private void BuildShopCard(string name, string effect, string value, string sku,
            float yMin, float yMax)
        {
            ProgressionTrack track = sku == null ? foundryTrack : default;
            bool isFoundry = sku == null;

            Image card = UiParts.Panel(shopPanel.transform, "Card_" + name,
                new Vector2(0.03f, yMin), new Vector2(0.97f, yMax), PanelKind.Card, raycast: true,
                tint: UiTheme.InkHint);

            TextMeshProUGUI nameLabel = UiRuntime.CreateText(card.transform, "Name", name,
                UiType.Role.Section, TextAlignmentOptions.MidlineLeft, UiTheme.Text);
            UiRuntime.Anchor(nameLabel.rectTransform, new Vector2(0.04f, 0.58f), new Vector2(0.58f, 0.96f));

            TextMeshProUGUI effectLabel = UiRuntime.CreateText(card.transform, "Effect", effect,
                UiType.Role.Sentence, TextAlignmentOptions.MidlineLeft, UiTheme.Text);
            UiRuntime.Anchor(effectLabel.rectTransform, new Vector2(0.04f, 0.10f), new Vector2(0.58f, 0.55f));

            TextMeshProUGUI valueLabel = UiRuntime.CreateText(card.transform, "Value", value,
                UiType.Role.Section, TextAlignmentOptions.Center, UiTheme.Text);
            UiRuntime.Anchor(valueLabel.rectTransform, new Vector2(0.58f, 0.58f), new Vector2(0.77f, 0.96f));

            UnityEngine.Events.UnityAction onClick = isFoundry
                ? (UnityEngine.Events.UnityAction)(() => BuyFoundry(track))
                : () => BuyIap(sku);

            UiPlateButton buy = UiPlateButton.Create(card.transform, "Buy",
                new Vector2(0.79f, 0.16f), new Vector2(0.97f, 0.84f),
                "BUY", UiType.Role.Section, UiTheme.Orange, UiTheme.Orange, onClick, ButtonFace.Primary);

            shopCards.Add(new ShopCard
            {
                BaseName = name,
                Sku = sku,
                Track = track,
                Name = nameLabel,
                Value = valueLabel,
                Buy = buy,
                BuyLabel = buy.GetComponentInChildren<TextMeshProUGUI>()
            });
        }

        private void RefreshShop()
        {
            if (manager == null)
            {
                return;
            }

            if (shopGemsLabel != null && manager.Economy != null)
            {
                shopGemsLabel.SetText("GEMS " + NumberFormatter.Format(manager.Economy.Gems));
            }

            for (int i = 0; i < shopCards.Count; i++)
            {
                ShopCard card = shopCards[i];

                if (card.Sku == null)
                {
                    if (manager.Tracks == null)
                    {
                        continue;
                    }

                    int level = manager.Tracks.GetLevel(card.Track, null);
                    bool maxed = manager.Tracks.IsMaxed(card.Track, null);
                    double cost = manager.Tracks.Cost(card.Track, null, 1);

                    card.Value.SetText("Lv " + level);
                    card.BuyLabel.SetText(maxed ? "MAX" : NumberFormatter.Format(cost) + " GEMS");
                    card.Buy.SetInteractable(!maxed && manager.Tracks.CanAfford(card.Track, null, 1));
                    continue;
                }

                bool owned = manager.Iap != null && manager.Iap.IsOwned(card.Sku);
                bool noAds = card.Sku == IapCatalog.SkuNoAds;

                card.Name.SetText(noAds && owned ? card.BaseName + " (OWNED)" : card.BaseName);
                card.BuyLabel.SetText(noAds && owned ? "OWNED" : "BUY");
                card.Buy.SetInteractable(!(noAds && owned));
            }
        }

        private void BuyIap(string sku)
        {
            if (manager == null || string.IsNullOrEmpty(sku))
            {
                return;
            }

            manager.PurchaseIap(sku);
            RefreshShop();
        }

        private void BuyFoundry(ProgressionTrack track)
        {
            if (manager == null || manager.Tracks == null)
            {
                return;
            }

            PurchaseResult result = manager.Tracks.TryBuy(track, null, 1, out double spent, out int newLevel);

            if (result == PurchaseResult.Bought)
            {
                manager.Save?.MarkDirty("gem-sink");
                GameEvents.RaiseToast(string.Format("{0} -> Lv {1} ({2} gems)",
                    track.DisplayName, newLevel, NumberFormatter.Format(spent)));
            }
            else if (result == PurchaseResult.Maxed)
            {
                GameEvents.RaiseToast(track.DisplayName + " is already maxed.");
            }
            else if (result == PurchaseResult.CannotAfford)
            {
                GameEvents.RaiseToast("Not enough gems (" + NumberFormatter.Format(manager.Tracks.Cost(track, null, 1)) + " needed).");
            }

            RefreshShop();
        }

        private ProgressionTrack FindFoundryTrack()
        {
            if (manager == null || manager.Tracks == null)
            {
                return default;
            }

            foreach (ProgressionTrack track in manager.Tracks.Tracks)
            {
                if (track.Currency == CurrencyType.Gems &&
                    !track.IsAutomation &&
                    track.EffectKind == TrackEffectKind.GlobalPercent)
                {
                    return track;
                }
            }

            return default;
        }

        private static string DescribeFoundry(ProgressionTrack track)
        {
            float perLevel = track.GainPerLevel * 100f;

            string stat;
            switch (track.GlobalEffect)
            {
                case PrestigeEffectType.GoldPercent: stat = "gold"; break;
                case PrestigeEffectType.DamagePercent: stat = "damage"; break;
                case PrestigeEffectType.HealthPercent: stat = "max HP"; break;
                default: stat = track.GlobalEffect.ToDisplayName(); break;
            }

            string line = string.Format("+{0:0.#}% {1} per level", perLevel, stat);

            if (track.RawMaxLevel > 0)
            {
                line += string.Format(" (max +{0:0.#}%)", perLevel * track.RawMaxLevel);
            }

            return line;
        }

        private void OnShopCurrencyChanged(CurrencyType currencyType, double amount)
        {
            RefreshShop();
        }

        /// <summary>Unhooks the shop's listeners. Called when the lobby object goes away.</summary>
        private void DetachShopEvents()
        {
            if (manager != null && manager.Iap != null)
            {
                manager.Iap.PurchasesChanged -= RefreshShop;
            }

            GameEvents.CurrencyChanged -= OnShopCurrencyChanged;
        }
    }
}
