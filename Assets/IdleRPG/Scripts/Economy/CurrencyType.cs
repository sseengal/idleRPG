namespace IdleRPG.Economy
{
    /// <summary>Every spendable/earnable currency in the game.</summary>
    public enum CurrencyType
    {
        Gold = 0,
        Gems = 1,
        PrestigeTokens = 2
    }

    public static class CurrencyTypeExtensions
    {
        /// <summary>UI label.</summary>
        public static string ToDisplayName(this CurrencyType currencyType)
        {
            switch (currencyType)
            {
                case CurrencyType.Gold:
                    return "Gold";
                case CurrencyType.Gems:
                    return "Gems";
                case CurrencyType.PrestigeTokens:
                    return "Tokens";
                default:
                    return currencyType.ToString();
            }
        }

        /// <summary>True when the currency survives an ascension reset.</summary>
        public static bool PersistsThroughAscension(this CurrencyType currencyType)
        {
            return currencyType == CurrencyType.Gems || currencyType == CurrencyType.PrestigeTokens;
        }
    }
}