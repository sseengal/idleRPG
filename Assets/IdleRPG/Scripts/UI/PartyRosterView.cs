using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Combat;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Equipment;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// The ROSTER tab of the Party page: which hero is selected, and the wiring that keeps the sheet fresh.
    /// Everything is read-only - equipping and discarding live on the INVENTORY tab (InventoryTabUI).
    ///
    /// The class is split by job, the house pattern (see BalanceConfig.Queries, DevOverlay.Readout):
    /// this file owns the data and the lifecycle, <c>PartyRosterView.Build</c> creates the widgets, and
    /// <c>PartyRosterView.Refresh</c> fills them.
    /// </summary>
    public sealed partial class PartyRosterView : MonoBehaviour
    {
        [SerializeField] private int abilitySlotPlaceholders = 3;

        private readonly Color cardColor = new Color(0.16f, 0.19f, 0.28f, 0.95f);
        private readonly Color cardSelectedColor = new Color(0.30f, 0.42f, 0.62f, 1f);
        private readonly Color slotColor = new Color(0.14f, 0.16f, 0.23f, 0.95f);
        private readonly Color bodyText = new Color(1f, 1f, 1f, 0.92f);
        private readonly Color dimText = new Color(1f, 1f, 1f, 0.55f);
        private readonly Color bonusColor = new Color(0.55f, 0.9f, 0.6f, 1f);
        private readonly Color accentColor = new Color(1f, 0.82f, 0.3f, 1f);

        /// <summary>One stat line on the roster sheet. Adding a stat = adding one row here (layout follows).</summary>
        private struct StatRow
        {
            public string Key;
            public HeroStatMetric Metric;

            /// <summary>True for HP/ATK/DEF: gear applies a percentage bonus that is worth showing.</summary>
            public bool ShowGearBonus;
        }

        /// <summary>Which number a roster row reads.</summary>
        private enum HeroStatMetric
        {
            Health,
            Attack,
            Defense,
            CritChance,
            CritDamage,
            Dps
        }

        // Rows render top-to-bottom; the layout maths adapts to the count (4, 6 or 10 - no re-anchoring).
        private static readonly StatRow[] Rows =
        {
            new StatRow { Key = "HP", Metric = HeroStatMetric.Health, ShowGearBonus = true },
            new StatRow { Key = "ATK", Metric = HeroStatMetric.Attack, ShowGearBonus = true },
            new StatRow { Key = "DEF", Metric = HeroStatMetric.Defense, ShowGearBonus = true },
            new StatRow { Key = "CRIT", Metric = HeroStatMetric.CritChance },
            new StatRow { Key = "CRIT DMG", Metric = HeroStatMetric.CritDamage },
            new StatRow { Key = "DPS", Metric = HeroStatMetric.Dps }
        };

        private const int GearSlotCount = 3;

        private GameManager manager;

        private Image[] cardBackgrounds;
        private Image[] cardIcons;
        private CharacterAnimator[] cardAnimators;
        private TextMeshProUGUI[] cardNameLabels;

        private Image bigPortrait;
        private CharacterAnimator bigAnimator;
        private TextMeshProUGUI bigNameLabel;
        private TextMeshProUGUI roleLabel;
        private TextMeshProUGUI[] statKeys;
        private TextMeshProUGUI[] statValues;
        private TextMeshProUGUI[] statBonusLabels;

        private Image[] gearSlotBackgrounds;
        private TextMeshProUGUI[] gearSlotStatLabels;

        private int selectedHeroIndex = -1;
        private int boundBigHero = -2;   // -2 = never bound, -1 = bound to "nothing"

        /// <summary>Kicks off again (slot-change refresh etc.) after a level-up or gear edit.</summary>
        public int SelectedHeroIndex
        {
            get { return selectedHeroIndex; }
        }

        public void Build(GameManager gameManager)
        {
            manager = gameManager;
            RectTransform root = (RectTransform)transform;

            BuildHeroCards(root);
            BuildGearRow(root);
            PartyPanelUI.BuildSlotRow(root, "ABILITIES", abilitySlotPlaceholders, 0.175f, 0.255f);

            if (selectedHeroIndex < 0)
            {
                selectedHeroIndex = FirstHeroIndex();
            }

            // A gold purchase on the UPGRADES page must be visible the moment this page is looked at again -
            // and immediately if it is already open. Without this the sheet only refreshed on tab open.
            if (manager.Resolver != null)
            {
                manager.Resolver.StatsChanged -= RefreshAll;
                manager.Resolver.StatsChanged += RefreshAll;
            }

            RefreshAll();
        }

        private void OnDestroy()
        {
            if (manager != null && manager.Resolver != null)
            {
                manager.Resolver.StatsChanged -= RefreshAll;
            }
        }

        private int FirstHeroIndex()
        {
            for (int i = 0; i < manager.Party.Heroes.Count; i++)
            {
                if (manager.Party.GetHero(i) != null)
                {
                    return i;
                }
            }

            return -1;
        }

        public void RefreshAll()
        {
            if (manager == null || manager.Party == null)
            {
                return;
            }

            RefreshCards();
            RefreshBigPortrait();
            RefreshStats();
            RefreshGear();
        }

        private void SelectHero(int heroIndex)
        {
            if (heroIndex < 0 || heroIndex == selectedHeroIndex)
            {
                return;
            }

            selectedHeroIndex = heroIndex;
            RefreshAll();
        }

    }
}
