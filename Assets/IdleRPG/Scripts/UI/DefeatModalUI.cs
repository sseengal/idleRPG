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
            }

            if (ascendButton != null)
            {
                ascendButton.onClick.AddListener(OnAscendClicked);
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
                titleLabel.SetText(string.Format("RUN OVER — stage {0}", defeatedStage));
            }

            if (summaryLabel != null)
            {
                summaryLabel.SetText(string.Format("Best this run: stage {0}   ·   {1} gold",
                    manager.RunBestStage, NumberFormatter.Format(manager.RunGoldEarned)));
            }

            if (ascendLabel != null)
            {
                ascendLabel.SetText(string.Format("+{0} Perk Points on the next run", NumberFormatter.Format(manager.RunEndPreviewTokens)));
            }

            // Two choices: REVIVE (rewarded ad, once per run) or NEXT RUN (bank + restart). Neither auto-fires.
            if (ascendButton != null)
            {
                ascendButton.gameObject.SetActive(true);
                ascendButton.interactable = manager.CanRevive;
            }

            if (countdownLabel != null)
            {
                countdownLabel.SetText(manager.CanRevive
                    ? "Watch an ad to revive this run once."
                    : "Revive used this run - NEXT RUN to go again.");
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
                manager.WatchAdForRunRevive();
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
