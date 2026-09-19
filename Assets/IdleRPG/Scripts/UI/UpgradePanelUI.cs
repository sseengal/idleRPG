using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;

namespace IdleRPG.UI
{
    /// <summary>
    /// Panel A: hero stat upgrades. One <see cref="HeroUpgradeRowUI"/> per party lane.
    /// Refreshes on currency changes (affordability) and on level purchases.
    /// </summary>
    public sealed class UpgradePanelUI : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private HeroUpgradeRowUI[] rows;

        private GameManager manager;
        private bool isVisible = true;

        private void Start()
        {
            EnsureBound();
        }

        /// <summary>
        /// Binds to the game systems the first time it can. Called from Start *and* OnEnable because
        /// this panel lives on a page that starts hidden, so activation order is not guaranteed.
        /// </summary>
        private void EnsureBound()
        {
            if (manager != null)
            {
                return;
            }

            HudController hud = HudController.Instance;
            manager = hud != null ? hud.GameManager : null;

            if (manager == null)
            {
                Debug.LogWarning("[UpgradePanelUI] GameManager not ready yet; will bind on next open.");
                return;
            }

            BindRows();
            RefreshAll();
        }

        private void OnEnable()
        {
            EnsureBound();
            GameEvents.CurrencyChanged += OnCurrencyChanged;
            GameEvents.UpgradePurchased += OnUpgradePurchased;
            GameEvents.HeroStatsChanged += OnHeroStatsChanged;
            GameEvents.SaveLoaded += OnSaveLoaded;
        }

        private void OnDisable()
        {
            GameEvents.CurrencyChanged -= OnCurrencyChanged;
            GameEvents.UpgradePurchased -= OnUpgradePurchased;
            GameEvents.HeroStatsChanged -= OnHeroStatsChanged;
            GameEvents.SaveLoaded -= OnSaveLoaded;
        }

        private void OnCurrencyChanged(Economy.CurrencyType currencyType, double amount)
        {
            if (isVisible && currencyType == Economy.CurrencyType.Gold)
            {
                RefreshAll();
            }
        }

        private void OnUpgradePurchased(int heroIndex, HeroStatType statType, int newLevel, double cost)
        {
            if (isVisible)
            {
                RefreshAll();
            }
        }

        private void OnHeroStatsChanged(int heroIndex)
        {
            if (isVisible)
            {
                RefreshAll();
            }
        }

        private void OnSaveLoaded()
        {
            RefreshAll();
        }

        /// <summary>Called by <see cref="TabController"/> style visibility changes (SetActive).</summary>
        private void OnBecameVisible()
        {
            isVisible = true;
            RefreshAll();
        }

        private void BindRows()
        {
            if (rows == null)
            {
                return;
            }

            for (int i = 0; i < rows.Length; i++)
            {
                HeroUpgradeRowUI row = rows[i];
                if (row != null)
                {
                    row.Configure(i, manager.Upgrade, manager.Resolver);
                }
            }
        }

        public void RefreshAll()
        {
            if (rows == null || manager == null)
            {
                return;
            }

            PartyConfig party = manager.Party;

            for (int i = 0; i < rows.Length; i++)
            {
                HeroUpgradeRowUI row = rows[i];
                if (row == null)
                {
                    continue;
                }

                HeroData hero = party != null ? party.GetHero(i) : null;
                row.Refresh(hero != null ? hero.HeroName : $"Hero {i + 1}");
            }
        }
    }
}