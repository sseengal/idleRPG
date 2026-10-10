using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// The defeat summary modal (Run-Identity pass). On a party wipe the loop still restarts by itself after a short
    /// beat - this card makes that beat legible and puts the run's end where the player is already thinking about it:
    /// restart the fight now, or cash the run in and ascend.
    ///
    /// It is opened by <see cref="GameEvents.DefeatShown"/> (which the GameManager raises only when its cooldown
    /// allows the full card - a hard wall bounces repeatedly, so a card on every wipe would spam the player) and hides
    /// itself when the game state leaves <see cref="GameState.Defeat"/>, which is exactly when the run restarts.
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
        private float countdownRemaining;
        private float countdownTotal;

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

        private void Update()
        {
            if (root == null || !root.activeSelf || countdownTotal <= 0f)
            {
                return;
            }

            countdownRemaining -= Time.deltaTime;

            if (countdownLabel != null)
            {
                int whole = Mathf.CeilToInt(countdownRemaining);
                countdownLabel.SetText(whole <= 0 ? "Resuming…" : string.Format("resuming in {0}…", whole));
            }
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
                titleLabel.SetText(string.Format("DEFEAT — stage {0}", defeatedStage));
            }

            if (summaryLabel != null)
            {
                summaryLabel.SetText(string.Format("Best this run: stage {0}   ·   {1} gold",
                    manager.RunBestStage, NumberFormatter.Format(manager.RunGoldEarned)));
            }

            if (ascendLabel != null)
            {
                ascendLabel.SetText(manager.CanAscend
                    ? string.Format("Ascend for {0} tokens", NumberFormatter.Format(manager.PrestigeTokenYield))
                    : string.Format("Reach stage {0} to ascend", manager.Balance != null ? manager.Balance.MinStageToAscend : 10));
            }

            if (ascendButton != null)
            {
                ascendButton.interactable = manager.CanAscend;
            }

            countdownTotal = manager.Balance != null ? manager.Balance.DefeatModalSeconds : 4f;
            countdownRemaining = countdownTotal;

            if (countdownLabel != null)
            {
                countdownLabel.SetText(string.Format("resuming in {0}…", Mathf.CeilToInt(countdownTotal)));
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
                manager.ResumeAfterDefeat();
            }
        }

        private void OnAscendClicked()
        {
            EnsureManager();
            if (manager != null)
            {
                manager.RequestAscension();
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
            countdownTotal = 0f;

            if (root != null)
            {
                root.SetActive(false);
            }
        }
    }
}
