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
    /// Monetisation funnels: thin forwards to <see cref="MonetizationService"/>, so the public surface stays on the
    /// manager. The ad guard and the cash register themselves live in that service.
    /// </summary>
    public sealed partial class GameManager : MonoBehaviour
    {
        /// <summary>True when the player owns the "no ads" purchase: every watch-ad button must disappear.</summary>
        public bool AdsDisabledByNoAds => monetization != null && monetization.AdsDisabledByNoAds;

        /// <summary>
        /// The ONE gate for every rewarded ad (B7 S3). The guard itself lives in <see cref="MonetizationService"/>.
        /// </summary>
        public bool TryShowAdPlacement(AdPlacementId placement, Action onReward)
        {
            return monetization != null && monetization.TryShowAdPlacement(placement, onReward);
        }

        /// <summary>Shows the GoldBoost ad, then activates the 2x gold boost (Shop CTA + the B hotkey).</summary>
        public bool WatchAdForGoldBoost()
        {
            return monetization != null && monetization.WatchAdForGoldBoost();
        }

        /// <summary>
        /// The single IAP funnel (B7 S1). The store confirms payment (<see cref="IIapService"/>), then the goods
        /// are granted through the normal till (gems) and shop (offline cap) - never directly.
        /// Lives in <see cref="MonetizationService"/>.
        /// </summary>
        public void PurchaseIap(string sku)
        {
            monetization?.PurchaseIap(sku);
        }
    }
}
