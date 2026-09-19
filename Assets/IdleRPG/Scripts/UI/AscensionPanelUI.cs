using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// Panel B: ascension. Shows the token yield, an "Ascend" button with a two-tap confirm,
    /// and the permanent upgrade tree.
    /// </summary>
    public sealed class AscensionPanelUI : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private TextMeshProUGUI yieldLabel;
        [SerializeField] private TextMeshProUGUI requirementLabel;
        [SerializeField] private Button ascendButton;
        [SerializeField] private TextMeshProUGUI ascendButtonLabel;
        [SerializeField] private PrestigeUpgradeRowUI[] prestigeRows = new PrestigeUpgradeRowUI[3];

        [Header("Behaviour")]
        [Tooltip("Seconds the confirm state stays armed after the first tap.")]
        [SerializeField] private float confirmTimeoutSec = 3f;

        private GameManager manager;
        private Coroutine confirmRoutine;
        private bool confirmArmed;

        private void Start()
        {
            HudController hud = HudController.Instance;
            manager = hud != null ? hud.GameManager : null;

            if (manager == null)
            {
                Debug.LogError("[AscensionPanelUI] GameManager unavailable; ascension panel disabled.");
                enabled = false;
                return;
            }

            if (ascendButton != null)
            {
                ascendButton.onClick.AddListener(OnAscendClicked);
            }

            BindRows();
            RefreshAll();
        }

        private void OnEnable()
        {
            GameEvents.CurrencyChanged += OnCurrencyChanged;
            GameEvents.PrestigeYieldChanged += OnPrestigeYieldChanged;
            GameEvents.AscensionCompleted += OnAscensionCompleted;
            GameEvents.SaveLoaded += OnSaveLoaded;
        }

        private void OnDisable()
        {
            GameEvents.CurrencyChanged -= OnCurrencyChanged;
            GameEvents.PrestigeYieldChanged -= OnPrestigeYieldChanged;
            GameEvents.AscensionCompleted -= OnAscensionCompleted;
            GameEvents.SaveLoaded -= OnSaveLoaded;
        }

        private void OnCurrencyChanged(Economy.CurrencyType currencyType, double amount)
        {
            if (currencyType == Economy.CurrencyType.PrestigeTokens)
            {
                RefreshRows();
            }
        }

        private void OnPrestigeYieldChanged(double tokenYield)
        {
            RefreshAll();
        }

        private void OnAscensionCompleted(double tokensEarned, int newHighestStage)
        {
            DisarmConfirm();
            RefreshAll();
        }

        private void OnSaveLoaded()
        {
            RefreshAll();
        }

        private void OnAscendClicked()
        {
            if (manager == null)
            {
                return;
            }

            if (!manager.CanAscend)
            {
                int required = manager.Balance != null ? manager.Balance.MinStageToAscend : 10;
                GameEvents.RaiseToast($"Reach stage {required} to ascend.");
                return;
            }

            if (!confirmArmed)
            {
                confirmArmed = true;

                if (ascendButtonLabel != null)
                {
                    ascendButtonLabel.SetText("CONFIRM?");
                }

                if (confirmRoutine != null)
                {
                    StopCoroutine(confirmRoutine);
                }

                confirmRoutine = StartCoroutine(ConfirmTimeout());
                return;
            }

            manager.RequestAscension();
            DisarmConfirm();
        }

        private IEnumerator ConfirmTimeout()
        {
            yield return new WaitForSeconds(confirmTimeoutSec);
            DisarmConfirm();
        }

        private void DisarmConfirm()
        {
            confirmArmed = false;

            if (confirmRoutine != null)
            {
                StopCoroutine(confirmRoutine);
                confirmRoutine = null;
            }

            if (ascendButtonLabel != null)
            {
                ascendButtonLabel.SetText("ASCEND");
            }
        }

        private void BindRows()
        {
            if (prestigeRows == null || manager.Ascension == null)
            {
                return;
            }

            var upgrades = manager.Ascension.Upgrades;

            for (int i = 0; i < prestigeRows.Length; i++)
            {
                if (prestigeRows[i] == null)
                {
                    continue;
                }

                PrestigeUpgradeData data = i < upgrades.Count ? upgrades[i] : null;
                prestigeRows[i].Configure(manager.Ascension, data);

                if (data == null)
                {
                    Debug.LogWarning($"[AscensionPanelUI] No prestige upgrade asset for row {i}.");
                }
            }
        }

        /// <summary>Rebuilds every label from the current progression state.</summary>
        public void RefreshAll()
        {
            if (manager == null)
            {
                return;
            }

            double yield = manager.PrestigeTokenYield;
            int required = manager.Balance != null ? manager.Balance.MinStageToAscend : 10;

            if (yieldLabel != null)
            {
                yieldLabel.SetText(string.Format("Ascend for <color=#D08CFF>{0}</color> tokens", NumberFormatter.Format(yield)));
            }

            if (requirementLabel != null)
            {
                requirementLabel.SetText(string.Format(
                    manager.CanAscend ? "Best stage {0}  -  ready" : "Best stage {0}  -  reach stage {1}",
                    manager.HighestStageReached,
                    required));
            }

            if (ascendButton != null)
            {
                ascendButton.interactable = manager.CanAscend && yield > 0d;
            }

            RefreshRows();
        }

        private void RefreshRows()
        {
            if (prestigeRows == null)
            {
                return;
            }

            for (int i = 0; i < prestigeRows.Length; i++)
            {
                if (prestigeRows[i] != null)
                {
                    prestigeRows[i].Refresh();
                }
            }
        }
    }
}