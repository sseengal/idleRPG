using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// Definition of one currency. Currencies are data rows, not an enum, so adding one (shards, essence,
    /// materials, scrolls) never touches code - the same rule as heroes and enemies.
    ///
    /// ELI5: a labelled money jar. The label says what it is, whether it is premium-ish, and whether the game
    /// can actually earn/spend it yet. Jars marked "not implemented" are placeholders so later steps only have
    /// to flip a flag instead of inventing a new concept.
    /// </summary>
    [CreateAssetMenu(fileName = "CurrencyDef", menuName = "Idle RPG/Data/Currency", order = 15)]
    public class CurrencyDef : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string currencyID = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private Sprite icon;

        [Header("Behaviour")]
        [Tooltip("Premium-adjacent currency: never granted by the idle rate (gems today).")]
        [SerializeField] private bool isPremium;

        [Tooltip("False = placeholder row for a currency a later step introduces. Tools skip it.")]
        [SerializeField] private bool isImplemented = true;

        [Tooltip("Where the player earns it (documentation for tools and UI tooltips).")]
        [SerializeField] private string earnSource = "";

        [Tooltip("What it is spent on (documentation for tools and UI tooltips).")]
        [SerializeField] private string sinkDescription = "";

        public string CurrencyID => string.IsNullOrEmpty(currencyID) ? name : currencyID;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        public Sprite Icon => icon;

        public bool IsPremium => isPremium;

        public bool IsImplemented => isImplemented;

        public string EarnSource => earnSource;

        public string SinkDescription => sinkDescription;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(currencyID))
            {
                currencyID = name;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = name;
            }
        }
    }
}
