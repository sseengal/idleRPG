using System.Text;
using UnityEditor;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Services;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Mock-IAP development controls (B7 S1). The cash register is fake until the real store is wired, so this
    /// menu is how the seam gets exercised without spending a real penny.
    /// </summary>
    public static class IapDebugMenu
    {
        [MenuItem("Tools/Idle RPG/Debug/Mock IAP/Log State", priority = 120)]
        public static void LogState()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[IapDebugMenu] Catalog:");

            for (int i = 0; i < IapCatalog.All.Count; i++)
            {
                IapProduct p = IapCatalog.All[i];
                sb.AppendLine($"  {p.Sku,-24} {p.ProductType,-13} {p.DisplayName} (gems {p.GemsGranted:0}, offline +{p.OfflineCapBonusMinutes:0} min, noAds {p.IsNoAds})");
            }

            bool owned = PlayerPrefs.GetInt(MockIapService.NoAdsPrefsKey, 0) == 1;
            sb.AppendLine($"  no-ads owned (device): {owned}");
            Debug.Log(sb.ToString());
        }

        [MenuItem("Tools/Idle RPG/Debug/Mock IAP/Grant No-Ads (Play)", priority = 121)]
        public static void GrantNoAds()
        {
            MockIapService mock = FindMock();
            if (mock == null)
            {
                return;
            }

            mock.SetNoAdsOwned(true);

            GameManager manager = FindManager();
            manager?.Save?.MarkDirty("iap");
            Debug.Log("[IapDebugMenu] No-Ads granted (device).");
        }

        [MenuItem("Tools/Idle RPG/Debug/Mock IAP/Clear No-Ads (Play)", priority = 122)]
        public static void ClearNoAds()
        {
            MockIapService mock = FindMock();
            if (mock == null)
            {
                return;
            }

            mock.SetNoAdsOwned(false);
            Debug.Log("[IapDebugMenu] No-Ads cleared (device).");
        }

        [MenuItem("Tools/Idle RPG/Debug/Mock IAP/Buy 30 Gems (Play)", priority = 123)]
        public static void BuyGemsMedium()
        {
            GameManager manager = FindManager();
            if (manager != null)
            {
                manager.PurchaseIap(IapCatalog.SkuGemsMedium);
            }
        }

        [MenuItem("Tools/Idle RPG/Debug/Mock IAP/Buy Starter Pack (Play)", priority = 124)]
        public static void BuyStarterPack()
        {
            GameManager manager = FindManager();
            if (manager != null)
            {
                manager.PurchaseIap(IapCatalog.SkuStarterPack);
            }
        }

        private static GameManager FindManager()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[IapDebugMenu] Run in Play mode; services only exist in a live game.");
                return null;
            }

            GameManager manager = Object.FindAnyObjectByType<GameManager>();

            if (manager == null)
            {
                Debug.LogWarning("[IapDebugMenu] No GameManager in the scene.");
            }

            return manager;
        }

        private static MockIapService FindMock()
        {
            GameManager manager = FindManager();
            MockIapService mock = manager != null ? manager.Iap as MockIapService : null;

            if (mock == null)
            {
                Debug.LogWarning("[IapDebugMenu] The running game is not using MockIapService.");
            }

            return mock;
        }
    }
}