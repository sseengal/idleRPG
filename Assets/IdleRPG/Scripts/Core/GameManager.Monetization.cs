using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using IdleRPG.Combat;
using IdleRPG.Data;
using IdleRPG.DebugTools;
using IdleRPG.Economy;
using IdleRPG.Sim;
using IdleRPG.Progression;
using IdleRPG.Save;
using IdleRPG.Services;
using IdleRPG.Utils;

namespace IdleRPG.Core
{
    /// <summary>
    /// Monetisation funnels: the rewarded-ad gate and the single IAP cash register.
    /// </summary>
    public sealed partial class GameManager : MonoBehaviour
    {
        public bool AdsDisabledByNoAds => Iap != null && Iap.IsOwned(IapCatalog.SkuNoAds);

        /// <summary>
        /// The ONE gate for every rewarded ad (B7 S3): no-ads -> caps + cooldown -> ready check -> show. On a
        /// completed ad the redemption is recorded and saved before <paramref name="onReward"/> runs, so a whirlwind
        /// tap-spam or crash can never over-pay a placement.
        /// </summary>
        public bool TryShowAdPlacement(AdPlacementId placement, Action onReward)
        {
            if (Ads == null || AdCaps == null)
            {
                return false;
            }

            if (AdsDisabledByNoAds)
            {
                GameEvents.RaiseToast("Ads removed - enjoy the quiet!");
                return false;
            }

            if (!AdCaps.CanShow(placement, out double waitSeconds))
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

            if (!Ads.IsRewardedAdReady)
            {
                GameEvents.RaiseToast("Ad not ready yet.");
                return false;
            }

            Ads.ShowRewardedAd(success =>
            {
                if (!success)
                {
                    GameEvents.RaiseToast("Ad skipped - no reward.");
                    return;
                }

                AdCaps.MarkShown(placement);
                Save?.SaveNow("ad");
                onReward?.Invoke();
            });

            return true;
        }

        /// <summary>Shows the GoldBoost ad, then activates the 2x gold boost (Shop CTA + the B hotkey).</summary>
        public bool WatchAdForGoldBoost()
        {
            return TryShowAdPlacement(AdPlacementId.GoldBoost, () =>
            {
                if (Boost != null)
                {
                    Boost.Activate();
                    LogFlow($"Ad boost active: x{Boost.GoldMultiplier:0.#} gold for {Boost.RemainingSeconds / 60f:0.#} min.");
                }
            });
        }

        /// <summary>
        /// The single IAP funnel (B7 S1). The store confirms payment (<see cref="IIapService"/>), then this
        /// grants the product's contents through the normal till (gems) and shop (offline cap) — never directly.
        /// </summary>
        public void PurchaseIap(string sku)
        {
            if (Iap == null || !Iap.IsInitialized)
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

            if (Iap.IsOwned(sku))
            {
                GameEvents.RaiseToast("Already owned.");
                return;
            }

            Iap.Purchase(sku, success =>
            {
                if (!success)
                {
                    GameEvents.RaiseToast("Purchase failed - nothing charged.");
                    return;
                }

                if (product.IsNoAds)
                {
                    LogFlow($"IAP bought: {product.DisplayName} (ads removed).");
                    GameEvents.RaiseToast("Ads removed - thank you!");
                    return;
                }

                if (product.GemsGranted > 0)
                {
                    Rewards?.GrantGems(product.GemsGranted, RewardService.Source.Milestone);
                }

                if (product.OfflineCapBonusMinutes > 0d)
                {
                    Shop?.TryGrantOfflineCapBonus(product.OfflineCapBonusMinutes);
                }

                Save?.MarkDirty("iap");
                LogFlow($"IAP bought: {product.DisplayName} (+{product.GemsGranted:0} gems, +{product.OfflineCapBonusMinutes:0} min offline cap).");
                GameEvents.RaiseToast($"{product.DisplayName} purchased!");
            });
        }
    }
}
