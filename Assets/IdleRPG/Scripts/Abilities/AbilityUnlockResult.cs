namespace IdleRPG.Abilities
{
    /// <summary>
    /// Why an unlock attempt ended the way it did. Separate from the tracks' <c>PurchaseResult</c> because a player
    /// needs to be told the DIFFERENCE between "you cannot afford it" and "come back at stage 12" - the track rows
    /// only ever quote a price.
    /// </summary>
    public enum AbilityUnlockResult
    {
        /// <summary>Paid for and switched on.</summary>
        Bought = 0,

        /// <summary>Already the player's; nothing was spent.</summary>
        AlreadyUnlocked = 1,

        /// <summary>Progress has not reached the ability's stage gate yet.</summary>
        StageLocked = 2,

        /// <summary>The gate is passed but the tokens are short.</summary>
        CannotAfford = 3,

        /// <summary>Nothing to unlock (no ability given).</summary>
        Invalid = 4
    }
}
