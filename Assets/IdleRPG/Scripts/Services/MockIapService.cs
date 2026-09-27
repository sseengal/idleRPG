using System;
using System.Collections;
using UnityEngine;

namespace IdleRPG.Services
{
    /// <summary>
    /// Development implementation of <see cref="IIapService"/>: waits a simulated purchase duration and then
    /// reports success, exactly like <see cref="MockAdService"/>. Non-consumable ownership (no-ads) is
    /// PlayerPrefs-backed so it survives a relaunch; consumables always "succeed" and are granted by the caller.
    ///
    /// ELI5: the pretend cash register. It rings, takes a beat, and hands over the toy. Swap in Unity IAP later
    /// and the game does not notice.
    /// </summary>
    public sealed class MockIapService : IIapService
    {
        public const string NoAdsPrefsKey = "IdleRPG.iapNoAds";

        private readonly MonoBehaviour host;
        private readonly float simulatedDurationSec;
        private bool buying;

        /// <summary>Raised after any purchase or ownership change (the shop re-renders).</summary>
        public event Action PurchasesChanged;

        public MockIapService(MonoBehaviour host, float simulatedDurationSec = 1f)
        {
            this.host = host;
            this.simulatedDurationSec = Mathf.Max(0f, simulatedDurationSec);

            if (this.host == null)
            {
                Debug.LogError("[MockIapService] A MonoBehaviour host is required to run the simulated purchase.");
            }
        }

        public bool IsInitialized => host != null;

        public bool IsOwned(string sku)
        {
            return sku == IapCatalog.SkuNoAds && PlayerPrefs.GetInt(NoAdsPrefsKey, 0) == 1;
        }

        public void Purchase(string sku, Action<bool> onCompleted)
        {
            if (host == null)
            {
                onCompleted?.Invoke(false);
                return;
            }

            if (buying)
            {
                Debug.LogWarning("[MockIapService] A purchase is already in progress.");
                onCompleted?.Invoke(false);
                return;
            }

            if (IapCatalog.Find(sku) == null)
            {
                Debug.LogWarning($"[MockIapService] Unknown product '{sku}'.");
                onCompleted?.Invoke(false);
                return;
            }

            host.StartCoroutine(PurchaseRoutine(sku, onCompleted));
        }

        public void RestorePurchases(Action<bool> onCompleted)
        {
            // The mock has nothing to re-check: ownership is already in PlayerPrefs.
            onCompleted?.Invoke(true);
        }

        /// <summary>Fakes ownership of no-ads without a purchase (debug menu / tests).</summary>
        public void SetNoAdsOwned(bool owned)
        {
            PlayerPrefs.SetInt(NoAdsPrefsKey, owned ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[MockIapService] No-Ads ownership {(owned ? "granted" : "cleared")}.");
            PurchasesChanged?.Invoke();
        }

        private IEnumerator PurchaseRoutine(string sku, Action<bool> onCompleted)
        {
            buying = true;
            Debug.Log($"[MockIapService] Simulating purchase of '{sku}'...");

            if (simulatedDurationSec > 0f)
            {
                yield return new WaitForSeconds(simulatedDurationSec);
            }

            buying = false;

            // Non-consumables record ownership so a relaunch or "restore" still sees it.
            if (IapCatalog.Find(sku).IsNoAds)
            {
                PlayerPrefs.SetInt(NoAdsPrefsKey, 1);
                PlayerPrefs.Save();
            }

            Debug.Log($"[MockIapService] Purchase of '{sku}' succeeded.");
            onCompleted?.Invoke(true);
            PurchasesChanged?.Invoke();
        }
    }
}