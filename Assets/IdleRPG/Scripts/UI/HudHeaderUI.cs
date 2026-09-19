using TMPro;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Economy;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// Top bar: gold, gems, tokens, stage/wave and the active gold-boost chip.
    /// Updates only when a relevant event fires — no per-frame polling.
    /// </summary>
    public sealed class HudHeaderUI : MonoBehaviour
    {
        [Header("Currency")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI gemsText;
        [SerializeField] private TextMeshProUGUI tokensText;

        [Header("Progress")]
        [SerializeField] private TextMeshProUGUI stageText;
        [SerializeField] private TextMeshProUGUI yieldText;

        [Header("Boost chip")]
        [SerializeField] private GameObject boostChip;
        [SerializeField] private TextMeshProUGUI boostText;

        private EconomyManager economy;

        private void Start()
        {
            HudController hud = HudController.Instance;
            GameManager manager = hud != null ? hud.GameManager : null;

            economy = manager != null ? manager.Economy : null;

            if (economy == null)
            {
                Debug.LogError("[HudHeaderUI] No economy available; header will stay empty.");
                return;
            }

            RefreshAll();
        }

        private void OnEnable()
        {
            GameEvents.CurrencyChanged += OnCurrencyChanged;
            GameEvents.StageChanged += OnStageChanged;
            GameEvents.GoldBoostChanged += OnGoldBoostChanged;
            GameEvents.PrestigeYieldChanged += OnPrestigeYieldChanged;
        }

        private void OnDisable()
        {
            GameEvents.CurrencyChanged -= OnCurrencyChanged;
            GameEvents.StageChanged -= OnStageChanged;
            GameEvents.GoldBoostChanged -= OnGoldBoostChanged;
            GameEvents.PrestigeYieldChanged -= OnPrestigeYieldChanged;
        }

        private void OnCurrencyChanged(CurrencyType currencyType, double amount)
        {
            switch (currencyType)
            {
                case CurrencyType.Gold:
                    SetText(goldText, amount);
                    break;
                case CurrencyType.Gems:
                    SetText(gemsText, amount);
                    break;
                case CurrencyType.PrestigeTokens:
                    SetText(tokensText, amount);
                    break;
            }
        }

        private void OnStageChanged(int stage, int wave, bool isBossWave)
        {
            if (stageText == null)
            {
                return;
            }

            stageText.SetText(string.Format(isBossWave ? "Stage {0}  <size=75%><color=#FF6B6B>BOSS</color></size>" : "Stage {0}  <size=75%>wave {1}</size>", stage, wave));
        }

        private void OnGoldBoostChanged(bool active, float remainingSeconds, double multiplier)
        {
            if (boostChip != null)
            {
                boostChip.SetActive(active);
            }

            if (boostText != null)
            {
                boostText.SetText(string.Format("x{0}  {1}", multiplier.ToString("0.#"), NumberFormatter.FormatCountdown(remainingSeconds)));
            }
        }

        private void OnPrestigeYieldChanged(double tokenYield)
        {
            if (yieldText != null)
            {
                yieldText.SetText(string.Format("Ascend: {0}", NumberFormatter.Format(tokenYield)));
            }
        }

        private void RefreshAll()
        {
            SetText(goldText, economy.Gold);
            SetText(gemsText, economy.Gems);
            SetText(tokensText, economy.PrestigeTokens);

            if (boostChip != null)
            {
                boostChip.SetActive(false);
            }
        }

        private static void SetText(TextMeshProUGUI label, double value)
        {
            if (label != null)
            {
                label.SetText(NumberFormatter.Format(value));
            }
        }
    }
}