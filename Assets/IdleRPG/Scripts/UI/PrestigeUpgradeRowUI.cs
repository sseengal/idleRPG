using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// One permanent upgrade row on the ascension panel: name, effect, level, token cost and
    /// a buy button. Button listeners are bound in code so the scene stays simple.
    /// </summary>
    public sealed class PrestigeUpgradeRowUI : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI effectLabel;
        [SerializeField] private TextMeshProUGUI levelLabel;
        [SerializeField] private TextMeshProUGUI costLabel;
        [SerializeField] private Button buyButton;

        private AscensionManager ascension;
        private PrestigeUpgradeData upgrade;

        public void Configure(AscensionManager ascensionManager, PrestigeUpgradeData data)
        {
            ascension = ascensionManager;
            upgrade = data;

            if (nameLabel != null && data != null)
            {
                nameLabel.SetText(data.DisplayName);
            }

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(Buy);
            }
        }

        private void Buy()
        {
            if (ascension != null && upgrade != null)
            {
                ascension.TryBuyPrestigeUpgrade(upgrade, 1);
            }
        }

        /// <summary>Refreshes labels and interactability for the current token balance.</summary>
        public void Refresh()
        {
            if (ascension == null || upgrade == null)
            {
                return;
            }

            int level = ascension.GetPrestigeLevel(upgrade);
            bool maxed = ascension.IsPrestigeMaxed(upgrade);
            double cost = ascension.GetPrestigeCost(upgrade, 1);
            bool canAfford = !maxed && ascension.CanAffordPrestige(upgrade, 1);

            if (effectLabel != null)
            {
                effectLabel.SetText(string.Format("{0}  <size=80%>({1} / lvl)</size>",
                    upgrade.EffectType.ToDisplayName(),
                    NumberFormatter.FormatPercent(upgrade.EffectPerLevel)));
            }

            if (levelLabel != null)
            {
                levelLabel.SetText(string.Format("Lv {0}/{1}", level, upgrade.HasLevelCap ? upgrade.MaxLevel : 0));
            }

            if (costLabel != null)
            {
                costLabel.SetText(string.Format(maxed ? "MAX" : "{0} tokens", NumberFormatter.Format(cost)));
            }

            if (buyButton != null)
            {
                buyButton.interactable = canAfford;
            }
        }
    }
}