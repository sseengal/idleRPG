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
    /// One hero's upgrade row: name plus ATK/HP/DEF blocks, each with +1 and +10 buttons,
    /// current level and the cost of the next purchase.
    /// </summary>
    public sealed class HeroUpgradeRowUI : MonoBehaviour
    {
        /// <summary>One stat column (ATK / HP / DEF).</summary>
        /// <summary>One stat column (ATK / HP / DEF), public so scene builders can create it.</summary>
        [System.Serializable]
        public sealed class StatBlock
        {
            public HeroStatType statType = HeroStatType.Attack;
            public Button plusOneButton;
            public Button plusTenButton;
            public TextMeshProUGUI levelLabel;
            public TextMeshProUGUI costLabel;
            public TextMeshProUGUI effectLabel;
            public Image iconImage;
            public Sprite iconSprite;
        }

        [Header("Wiring")]
        [SerializeField] private int heroIndex;
        [SerializeField] private TextMeshProUGUI heroNameLabel;
        [SerializeField] private StatBlock[] blocks = new StatBlock[3];
        [SerializeField] private Color affordableColor = Color.white;
        [SerializeField] private Color unaffordableColor = new Color(1f, 1f, 1f, 0.45f);

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

            for (int i = 0; i < blocks.Length; i++)
            {
                StatBlock block = blocks[i];

                if (block == null)
                {
                    continue;
                }

                HeroStatType statType = block.statType;

                if (block.plusOneButton != null)
                {
                    block.plusOneButton.onClick.RemoveAllListeners();
                    block.plusOneButton.onClick.AddListener(() => Buy(statType, 1));
                }

                if (block.plusTenButton != null)
                {
                    block.plusTenButton.onClick.RemoveAllListeners();
                    block.plusTenButton.onClick.AddListener(() => Buy(statType, 10));
                }

                if (block.iconImage != null && block.iconSprite != null)
                {
                    block.iconImage.sprite = block.iconSprite;
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

            if (upgrades == null)
            {
                return;
            }

            for (int i = 0; i < blocks.Length; i++)
            {
                StatBlock block = blocks[i];
                if (block == null)
                {
                    continue;
                }

                int level = upgrades.GetLevel(heroIndex, block.statType);
                HeroStatType statType = block.statType;

                if (block.levelLabel != null)
                {
                    block.levelLabel.SetText(string.Format("Lv {0}", level));
                }

                bool maxed = upgrades.IsAtMaxLevel(heroIndex, statType);

                double costOne = upgrades.GetCost(heroIndex, statType, 1);
                double costTen = upgrades.GetCost(heroIndex, statType, 10);
                bool canAffordOne = !maxed && upgrades.CanAfford(heroIndex, statType, 1);
                bool canAffordTen = !maxed && upgrades.CanAfford(heroIndex, statType, 10);

                if (block.costLabel != null)
                {
                    block.costLabel.SetText(string.Format(maxed ? "MAX" : "{0}  |  x10 {1}",
                        NumberFormatter.Format(costOne), NumberFormatter.Format(costTen)));
                    block.costLabel.color = canAffordOne ? affordableColor : unaffordableColor;
                }

                if (block.plusOneButton != null)
                {
                    block.plusOneButton.interactable = canAffordOne;
                }

                if (block.plusTenButton != null)
                {
                    block.plusTenButton.interactable = canAffordTen;
                }

                if (block.effectLabel != null && resolver != null)
                {
                    double gain = resolver.GetStatGainFraction(statType);

                    // B3d: a compounding track reads as a multiplier (x1.09 / lvl); additive stays a flat share.
                    block.effectLabel.SetText(resolver.GetEffectMode(statType) == StatEffectMode.Multiplicative
                        ? string.Format("x{0:0.###} / lvl", 1d + gain)
                        : string.Format("+{0:0.#}% base / lvl", gain * 100d));
                }
            }
        }
    }
}