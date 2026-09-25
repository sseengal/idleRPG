using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// One purchasable automation card (B6): the "auto-buy manager" or the "speed button".
    ///
    /// ELI5: a card is bought ONCE with rebirth-tokens and then lives forever; what it actually does is up to
    /// <see cref="IdleRPG.Progression.AutomationService"/>. Cards are tracks under the hood (B5), so they use the
    /// same menu, the same save keys and the same token checkout as every other upgrade.
    /// </summary>
    [CreateAssetMenu(fileName = "AutomationDef", menuName = "Idle RPG/Data/Automation Def", order = 15)]
    public class AutomationDef : ScriptableObject
    {
        [SerializeField] private string automationID = "";

        [SerializeField] private string displayName = "";

        [TextArea(2, 4)]
        [SerializeField] private string description = "";

        [Tooltip("Token cost of the one-time unlock.")]
        [SerializeField] private double baseCostTokens = 4d;

        [Tooltip("Rebirths required before the card may be bought. 0 = buyable as soon as tokens exist.")]
        [SerializeField] private int minAscensions = 0;

        public string AutomationID => string.IsNullOrEmpty(automationID) ? name : automationID;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        public string Description => description;

        public double BaseCostTokens => baseCostTokens < 0d ? 0d : baseCostTokens;

        public int MinAscensions => Mathf.Max(0, minAscensions);

        private void OnValidate()
        {
            if (baseCostTokens < 0d)
            {
                baseCostTokens = 0d;
            }

            if (minAscensions < 0)
            {
                minAscensions = 0;
            }

            if (string.IsNullOrEmpty(automationID))
            {
                automationID = name;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = name;
            }
        }
    }
}