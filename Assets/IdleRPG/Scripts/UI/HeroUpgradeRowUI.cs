using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// One hero's upgrade section: the hero's name, then ONE ROW PER STAT (ATK / HP / DEF / CRIT / CRIT DMG).
    ///
    /// ELI5: each row is one stat, with two big buttons on the right - "+1" buys one level, "x10" buys ten. The row
    /// itself is not a button, so dragging the list past a row can never buy by accident. It used to be five little
    /// tiles crammed into a row, which left every button about 9 dp tall (a quarter of a centimetre) - impossible
    /// to hit on a phone. See <see cref="UiTouch"/>.
    /// </summary>
    public sealed class HeroUpgradeRowUI : MonoBehaviour
    {
        /// <summary>One stat row. Public so scene builders can create it.</summary>
        [System.Serializable]
        public sealed class StatRow
        {
            public HeroStatType statType = HeroStatType.Attack;

            /// <summary>The row's face. Decoration only: it is not clickable, so a scroll flick cannot buy.</summary>
            public Image background;

            /// <summary>Buys one level. Finger-sized (see <see cref="UiTouch"/>).</summary>
            public Button buyOneButton;

            public Button plusTenButton;
            public TextMeshProUGUI nameLabel;
            public TextMeshProUGUI effectLabel;
            public TextMeshProUGUI levelLabel;
            public TextMeshProUGUI costLabel;
        }

        [Header("Wiring")]
        [SerializeField] private int heroIndex;
        [SerializeField] private TextMeshProUGUI heroNameLabel;
        [SerializeField] private StatRow[] rows = new StatRow[5];
        [SerializeField] private Color affordableColor = Color.white;
        [SerializeField] private Color unaffordableColor = new Color(1f, 1f, 1f, 0.45f);
        [SerializeField] private Color rowReadyColor = new Color(0.20f, 0.30f, 0.46f, 0.95f);
        [SerializeField] private Color rowDimColor = new Color(0.15f, 0.17f, 0.23f, 0.9f);

        private UpgradeManager upgrades;
        private StatResolver resolver;

        public int HeroIndex => heroIndex;

        /// <summary>
        /// Binds the row. Wiring the buttons here (and clearing first) keeps them working no matter
        /// which order the page is activated in — Awake only fires on first activation.
        /// </summary>
        public void Configure(int index, UpgradeManager upgradeManager, StatResolver statResolver)
        {
            heroIndex = index;
            upgrades = upgradeManager;
            resolver = statResolver;

            for (int i = 0; i < rows.Length; i++)
            {
                StatRow row = rows[i];

                if (row == null)
                {
                    continue;
                }

                HeroStatType statType = row.statType;

                // "+1" buys one level; the right-hand button buys ten.
                if (row.buyOneButton != null)
                {
                    row.buyOneButton.onClick.RemoveAllListeners();
                    row.buyOneButton.onClick.AddListener(() => Buy(statType, 1));
                }

                if (row.plusTenButton != null)
                {
                    row.plusTenButton.onClick.RemoveAllListeners();
                    row.plusTenButton.onClick.AddListener(() => Buy(statType, 10));
                }
            }
        }

        private void Buy(HeroStatType statType, int levels)
        {
            if (upgrades == null)
            {
                return;
            }

            upgrades.TryUpgrade(heroIndex, statType, levels);
        }

        /// <summary>Refreshes every label/button from the current progression state.</summary>
        public void Refresh(string heroName)
        {
            if (heroNameLabel != null)
            {
                heroNameLabel.SetText(heroName);
            }

            if (upgrades == null || rows == null)
            {
                return;
            }

            for (int i = 0; i < rows.Length; i++)
            {
                StatRow row = rows[i];
                if (row == null)
                {
                    continue;
                }

                HeroStatType statType = row.statType;
                bool maxed = upgrades.IsAtMaxLevel(heroIndex, statType);
                bool canAffordOne = !maxed && upgrades.CanAfford(heroIndex, statType, 1);
                bool canAffordTen = !maxed && upgrades.CanAfford(heroIndex, statType, 10);

                if (row.levelLabel != null)
                {
                    row.levelLabel.SetText(maxed
                        ? "MAX"
                        : string.Format("Lv {0}", upgrades.GetLevel(heroIndex, statType)));
                }

                if (row.costLabel != null)
                {
                    row.costLabel.SetText(maxed
                        ? string.Empty
                        : string.Format("{0}  ·  x10 {1}",
                            NumberFormatter.Format(upgrades.GetCost(heroIndex, statType, 1)),
                            NumberFormatter.Format(upgrades.GetCost(heroIndex, statType, 10))));
                    row.costLabel.color = canAffordOne ? affordableColor : unaffordableColor;
                }

                if (row.effectLabel != null && resolver != null)
                {
                    row.effectLabel.SetText(FormatEffect(statType, resolver.GetEffectMode(statType),
                        resolver.GetStatGainFraction(statType)));
                }

                if (row.nameLabel != null)
                {
                    row.nameLabel.color = canAffordOne ? affordableColor : unaffordableColor;
                }

                if (row.background != null)
                {
                    row.background.color = canAffordOne ? rowReadyColor : rowDimColor;
                }

                if (row.buyOneButton != null)
                {
                    row.buyOneButton.interactable = canAffordOne;
                }

                if (row.plusTenButton != null)
                {
                    row.plusTenButton.interactable = canAffordTen;
                }
            }
        }

        /// <summary>
        /// How one level reads, per effect mode: compounding shows a multiplier, flat points show the actual
        /// points gained (crit chance in % points, crit damage in x), and the shipped additive model shows the
        /// share of base it adds.
        /// </summary>
        private static string FormatEffect(HeroStatType statType, StatEffectMode mode, double gain)
        {
            switch (mode)
            {
                case StatEffectMode.Multiplicative:
                    return string.Format("x{0:0.###} / lvl", 1d + gain);

                case StatEffectMode.FlatAdditive:
                    return statType == HeroStatType.CritRate
                        ? string.Format("+{0:0.#}% / lvl", gain * 100d)
                        : string.Format("+{0:0.##}x / lvl", gain);

                default:
                    return string.Format("+{0:0.#}% base / lvl", gain * 100d);
            }
        }
    }
}