using System.Collections.Generic;

namespace IdleRPG.Services
{
    /// <summary>How a product is treated after purchase (mirrors the store's consumable / non-consumable split).</summary>
    public enum IapProductType
    {
        /// <summary>Bought once, stays owned forever (no-ads). Restored on reinstall.</summary>
        NonConsumable = 0,

        /// <summary>Bought any number of times, grants its contents each time (gem packs).</summary>
        Consumable = 1
    }

    /// <summary>
    /// One purchasable product. Everything the game needs to know: the store SKU, the type, and what
    /// the player receives. The real price lives in the store dashboard (App Store / Play Console), not here.
    /// </summary>
    public sealed class IapProduct
    {
        public readonly string Sku;
        public readonly string DisplayName;
        public readonly IapProductType ProductType;
        public readonly double GemsGranted;
        public readonly double OfflineCapBonusMinutes;
        public readonly bool RemovesAds;

        public IapProduct(string sku, string displayName, IapProductType productType,
            double gemsGranted = 0d, double offlineCapBonusMinutes = 0d, bool removesAds = false)
        {
            Sku = sku;
            DisplayName = displayName;
            ProductType = productType;
            GemsGranted = gemsGranted;
            OfflineCapBonusMinutes = offlineCapBonusMinutes;
            RemovesAds = removesAds;
        }

        public bool IsNoAds => RemovesAds;
    }

    /// <summary>
    /// Single source of truth for every purchasable product (B7 S1). A real store (Unity IAP -> App Store /
    /// Play Billing) still needs matching product records, but the game only ever sees this list — so the mock
    /// and the real store can never disagree about what a purchase gives.
    ///
    /// ELI5: this is the shop's shelf. The shelves never move, even when the cash register is swapped for a real one.
    /// </summary>
    public static class IapCatalog
    {
        public const string SkuNoAds = "idlerpg.no_ads";
        public const string SkuGemsSmall = "idlerpg.gems_small";
        public const string SkuGemsMedium = "idlerpg.gems_medium";
        public const string SkuGemsLarge = "idlerpg.gems_large";
        public const string SkuStarterPack = "idlerpg.starter_pack";

        private static readonly IapProduct[] Products =
        {
            new IapProduct(SkuNoAds, "No Ads", IapProductType.NonConsumable, removesAds: true),
            new IapProduct(SkuGemsSmall, "5 Gems", IapProductType.Consumable, gemsGranted: 5d),
            new IapProduct(SkuGemsMedium, "30 Gems", IapProductType.Consumable, gemsGranted: 30d),
            new IapProduct(SkuGemsLarge, "110 Gems", IapProductType.Consumable, gemsGranted: 110d),
            new IapProduct(SkuStarterPack, "Starter Pack", IapProductType.Consumable, gemsGranted: 50d, offlineCapBonusMinutes: 60d)
        };

        public static IReadOnlyList<IapProduct> All => Products;

        public static IapProduct Find(string sku)
        {
            for (int i = 0; i < Products.Length; i++)
            {
                if (Products[i].Sku == sku)
                {
                    return Products[i];
                }
            }

            return null;
        }
    }
}