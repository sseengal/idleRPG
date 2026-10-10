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

        private void Start()
        {
            EnsureBound();
        }

        /// <summary>
        /// Binds to the game systems the first time it can. Called from Start *and* OnEnable because
        /// this panel lives on a page that starts hidden, so activation order is not guaranteed.
        ///
        /// "The GameManager exists" is NOT enough: this page can be enabled while the manager is still loading a
        /// save, so <c>Upgrade</c>/<c>Resolver</c> are still null. Binding then left every row with a null upgrade
        /// manager and the old <c>if (manager != null) return;</c> guard blocked every later retry, so the page
        /// showed placeholder text and its buttons did nothing for the rest of the session.
        /// </summary>
        private void EnsureBound()
        {
            if (manager != null && manager.Upgrade != null && manager.Resolver != null)
            {
                return;
            }

            HudController hud = HudController.Instance;
            GameManager candidate = hud != null ? hud.GameManager : null;

            if (candidate == null || candidate.Upgrade == null || candidate.Resolver == null)
            {
                return;
            }

            manager = candidate;
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
            if (currencyType == Economy.CurrencyType.Gold)
            {
                RefreshAll();
            }
        }

        private void OnUpgradePurchased(int heroIndex, HeroStatType statType, int newLevel, double cost)
        {
            RefreshAll();
        }

        private void OnHeroStatsChanged(int heroIndex)
        {
            RefreshAll();
        }

        private void OnSaveLoaded()
        {
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
            // Retry the bind from here too: this is the method the live events (gold changed, purchase made) call,
            // so a page that was enabled too early still comes alive on its own within a second or two.
            EnsureBound();

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