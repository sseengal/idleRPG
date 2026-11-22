using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// The run-over card. On a party wipe the run ENDS: the reward is banked and this card shows what was earned.
    /// There is no auto-resume - the player taps NEXT RUN to start a fresh run (or the card simply stays). It is
    /// opened by <see cref="GameEvents.DefeatShown"/> and hides when the game state leaves the run-over state.
    /// </summary>
    public sealed class DefeatModalUI : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI summaryLabel;
        [SerializeField] private TextMeshProUGUI ascendLabel;
        [SerializeField] private TextMeshProUGUI countdownLabel;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button ascendButton;

        private GameManager manager;

        /// <summary>Modal dialogs must sit above every other canvas, whatever the scene was built with.</summary>
        private const int ModalSortingOrder = 100;

        private void Awake()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.sortingOrder < ModalSortingOrder)
            {
                canvas.sortingOrder = ModalSortingOrder;
            }

            if (root != null)
            {
                root.SetActive(false);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);

                // The run cannot be rejoined; the card returns to the hall, where START begins the next run.
                TextMeshProUGUI label = continueButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.SetText("END RUN");
                }
            }

            if (ascendButton != null)
            {
                ascendButton.onClick.AddListener(OnAscendClicked);

                TextMeshProUGUI reviveLabel = ascendButton.GetComponentInChildren<TextMeshProUGUI>();
                if (reviveLabel != null)
                {
                    reviveLabel.SetText("REVIVE");
                }
            }
        }

        private void OnEnable()
        {
            GameEvents.DefeatShown += OnDefeatShown;
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.DefeatShown -= OnDefeatShown;
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        private void OnDefeatShown(int defeatedStage, int resumeStage)
        {
            EnsureManager();
            if (manager == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.SetText(string.Format("RUN OVER - stage {0}", defeatedStage));
            }

            if (summaryLabel != null)
            {
                summaryLabel.SetText(string.Format("Best this run: stage {0}   |   {1} gold",
                    manager.RunBestStage, NumberFormatter.Format(manager.RunGoldEarned)));
            }

            if (ascendLabel != null)
            {
                ascendLabel.SetText(string.Format("+{0} Perk Points if you end the run now", NumberFormatter.Format(manager.RunEndPreviewTokens)));
            }

            // Two choices: REVIVE (Dawn Ward perk, one per run) or END RUN. A revive button that cannot be used is
            // hidden, not greyed - a dim button on a defeat card reads as a nag.
            if (ascendButton != null)
            {
                bool canRevive = manager.CanRevive;
                ascendButton.gameObject.SetActive(canRevive);
                ascendButton.interactable = canRevive;
            }

            if (countdownLabel != null)
            {
                countdownLabel.SetText(manager.CanRevive
                    ? "One revive left this run."
                    : manager.HasRevivePerk
                        ? "Revive used this run - END RUN to go again."
                        : "Revive is an ASCEND perk.");
            }

            if (root != null)
            {
                root.SetActive(true);
                root.transform.SetAsLastSibling();
            }
        }

        private void OnGameStateChanged(GameState previous, GameState next)
        {
            if (next != GameState.Defeat)
            {
                Hide();
            }
        }

        private void OnContinueClicked()
        {
            EnsureManager();
            if (manager != null)
            {
                manager.ConfirmEndRun();
            }
        }

        private void OnAscendClicked()
        {
            EnsureManager();
            if (manager != null)
            {
                manager.ReviveFromDefeat();
            }
        }

        private void EnsureManager()
        {
            if (manager != null)
            {
                return;
            }

            HudController hud = HudController.Instance;
            manager = hud != null ? hud.GameManager : null;
        }

        private void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }
    }
}
