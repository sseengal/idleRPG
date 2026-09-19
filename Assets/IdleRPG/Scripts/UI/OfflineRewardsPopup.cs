using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Save;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// Offline earnings popup. Built in Step 4; Step 5's OfflineProgressManager raises
    /// <see cref="GameEvents.OfflineRewardsReady"/> and this panel shows the claim screen.
    /// </summary>
    public sealed class OfflineRewardsPopup : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI timeLabel;
        [SerializeField] private TextMeshProUGUI goldLabel;
        [SerializeField] private TextMeshProUGUI capNoteLabel;
        [SerializeField] private Button claimButton;

        private OfflineRewardResult pendingReward;

        private void Awake()
        {
            if (root != null)
            {
                root.SetActive(false);
            }

            if (claimButton != null)
            {
                claimButton.onClick.AddListener(OnClaimClicked);
            }
        }

        private void OnEnable()
        {
            GameEvents.OfflineRewardsReady += OnOfflineRewardsReady;
        }

        private void OnDisable()
        {
            GameEvents.OfflineRewardsReady -= OnOfflineRewardsReady;
        }

        private void OnOfflineRewardsReady(OfflineRewardResult result)
        {
            if (!result.HasReward)
            {
                return;
            }

            pendingReward = result;

            if (titleLabel != null)
            {
                titleLabel.SetText("Welcome back!");
            }

            if (timeLabel != null)
            {
                timeLabel.SetText(string.Format("You were away for {0}", NumberFormatter.FormatDuration(result.RawSeconds)));
            }

            if (goldLabel != null)
            {
                goldLabel.SetText(string.Format("+{0} gold", NumberFormatter.Format(result.Gold)));
            }

            if (capNoteLabel != null)
            {
                capNoteLabel.SetText(result.WasCapped
                    ? string.Format("Capped at {0} of battle income (8h max away).",
                        NumberFormatter.FormatDuration(result.CappedSeconds))
                    : string.Format("Earned at {0}/s for {1}.",
                        NumberFormatter.Format(result.GoldPerSecond),
                        NumberFormatter.FormatDuration(result.CappedSeconds)));
            }

            if (root != null)
            {
                root.SetActive(true);
                root.transform.SetAsLastSibling();
            }
        }

        private void OnClaimClicked()
        {
            if (!pendingReward.HasReward)
            {
                Hide();
                return;
            }

            GameEvents.RaiseOfflineRewardsClaimed(pendingReward.Gold);
            pendingReward = OfflineRewardResult.None;
            Hide();
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }
    }
}