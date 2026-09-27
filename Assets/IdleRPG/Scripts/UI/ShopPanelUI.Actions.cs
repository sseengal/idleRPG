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
    /// Purchase handlers: one click action per offer row
    /// </summary>
    public sealed partial class ShopPanelUI : MonoBehaviour
    {

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
    }
}
