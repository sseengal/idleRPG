using System;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Save;
using IdleRPG.Services;

namespace IdleRPG.Core
{
    /// <summary>
    /// The money door: the one gate for rewarded ads, and the one cash register for in-app purchases.
    ///
    /// ELI5: every "watch an ad for a reward" button in the game goes through one guard. The guard checks the
    /// player did not buy the no-ads pack, checks the daily limit and the short cooldown, checks an ad is loaded,
    /// then shows it - and writes down the redemption *before* paying the reward, so tap-spam or a crash cannot
    /// pay the same ad twice. In-app purchases work the same way: the store confirms the payment, then the goods
    /// are handed out through the normal till and shop.
    ///
    /// Why it is not left in <c>GameManager</c>: none of it needs combat, the run, or the UI - only the two store
    /// services, the ad police, the boost, the shop, the till and the save. Pulling it out keeps the composition
    /// root about wiring, and makes the ad gate and the purchase funnel reachable without booting a scene.
    /// </summary>
    public sealed class MonetizationService
    {
        private readonly IAdService ads;
        private readonly AdCapsService adCaps;
        private readonly IIapService iap;
        private readonly BoostManager boost;
        private readonly ShopService shop;
        private readonly RewardService rewards;
        private readonly SaveManager save;
        private readonly Action<string> log;

        public MonetizationService(IAdService ads, AdCapsService adCaps, IIapService iap, BoostManager boost,
            ShopService shop, RewardService rewards, SaveManager save, Action<string> log)
        {
            this.ads = ads;
            this.adCaps = adCaps;
            this.iap = iap;
            this.boost = boost;
            this.shop = shop;
            this.rewards = rewards;
            this.save = save;
            this.log = log;
        }

        /// <summary>True when the player owns the "no ads" purchase: every watch-ad button must disappear.</summary>
        public bool AdsDisabledByNoAds => iap != null && iap.IsOwned(IapCatalog.SkuNoAds);

        /// <summary>
        /// The ONE gate for every rewarded ad (B7 S3): no-ads -> caps + cooldown -> ready check -> show. On a
        /// completed ad the redemption is recorded and saved before <paramref name="onReward"/> runs, so a whirlwind
        /// tap-spam or crash can never over-pay a placement.
        /// </summary>
        public bool TryShowAdPlacement(AdPlacementId placement, Action onReward)
        {
            if (ads == null || adCaps == null)
            {
                return false;
            }

            if (AdsDisabledByNoAds)
            {
                GameEvents.RaiseToast("Ads removed - enjoy the quiet!");
                return false;
            }

            if (!adCaps.CanShow(placement, out double waitSeconds))
            {
                if (waitSeconds > 0d)
                {
                    GameEvents.RaiseToast(string.Format("Ad recently used - try again in about {0:0} min.", waitSeconds / 60d));
                }
                else
                {
                    GameEvents.RaiseToast("Daily ad limit reached - come back tomorrow.");
                }

                return false;
            }

            if (!ads.IsRewardedAdReady)
            {
                GameEvents.RaiseToast("Ad not ready yet.");
                return false;
            }

            ads.ShowRewardedAd(success =>
            {
                if (!success)
                {
                    GameEvents.RaiseToast("Ad skipped - no reward.");
                    return;
                }

                adCaps.MarkShown(placement);
                save?.SaveNow("ad");
                onReward?.Invoke();
            });

            return true;
        }
        /// <summary>Shows the GoldBoost ad, then activates the 2x gold boost (Shop CTA + the B hotkey).</summary>
        public bool WatchAdForGoldBoost()
        {
            return TryShowAdPlacement(AdPlacementId.GoldBoost, () =>
            {
                if (boost != null)
                {
                    boost.Activate();
                    log?.Invoke($"Ad boost active: x{boost.GoldMultiplier:0.#} gold for {boost.RemainingSeconds / 60f:0.#} min.");
                }
            });
        }

        /// <summary>
        /// Camp Short Rest+: the same 20% heal with or without an ad. No Ads buyers get it free; everyone else
        /// watches one rewarded ad. No daily cap on purpose - the camp is the pacing beat, not an ad wall.
        /// </summary>
        public bool WatchAdForCampRest(Action onReward, Action onFinished = null)
        {
            if (onReward == null)
            {
                onFinished?.Invoke();
                return false;
            }

            if (AdsDisabledByNoAds)
            {
                onReward();
                onFinished?.Invoke();
                return true;
            }

            if (ads == null)
            {
                GameEvents.RaiseToast("Ads are not available.");
                onFinished?.Invoke();
                return false;
            }

            if (!ads.IsRewardedAdReady)
            {
                GameEvents.RaiseToast("Ad not ready yet.");
                onFinished?.Invoke();
                return false;
            }

            ads.ShowRewardedAd(success =>
            {
                if (success)
                {
                    onReward();
                }
                else
                {
                    GameEvents.RaiseToast("Ad skipped - no rest.");
                }

                onFinished?.Invoke();
            });

            return true;
        }

        /// <summary>
        /// The single IAP funnel (B7 S1). The store confirms payment (<see cref="IIapService"/>), then this
        /// grants the product's contents through the normal till (gems) and shop (offline cap) - never directly.
        /// </summary>
        public void PurchaseIap(string sku)
        {
            if (iap == null || !iap.IsInitialized)
            {
                GameEvents.RaiseToast("Store is not ready.");
                return;
            }

            IapProduct product = IapCatalog.Find(sku);

            if (product == null)
            {
                GameEvents.RaiseToast("Unknown product.");
                return;
            }

            if (iap.IsOwned(sku))
            {
                GameEvents.RaiseToast("Already owned.");
                return;
            }

            iap.Purchase(sku, success =>
            {
                if (!success)
                {
                    GameEvents.RaiseToast("Purchase failed - nothing charged.");
                    return;
                }

                if (product.IsNoAds)
                {
                    log?.Invoke($"IAP bought: {product.DisplayName} (ads removed).");
                    GameEvents.RaiseToast("Ads removed - thank you!");
                    return;
                }

                if (product.GemsGranted > 0)
                {
                    rewards?.GrantGems(product.GemsGranted, RewardService.Source.Milestone);
                }

                if (product.OfflineCapBonusMinutes > 0d)
                {
                    shop?.TryGrantOfflineCapBonus(product.OfflineCapBonusMinutes);
                }

                save?.MarkDirty("iap");
                log?.Invoke($"IAP bought: {product.DisplayName} (+{product.GemsGranted:0} gems, +{product.OfflineCapBonusMinutes:0} min offline cap).");
                GameEvents.RaiseToast($"{product.DisplayName} purchased!");
            });
        }
    }
}
