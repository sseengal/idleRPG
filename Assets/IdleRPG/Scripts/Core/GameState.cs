namespace IdleRPG.Core
{
    /// <summary>
    /// High level flow of the game. Owned/driven by <see cref="GameManager"/>.
    /// </summary>
    public enum GameState
    {
        /// <summary>Bootstrapping: services wiring up, no simulation running yet.</summary>
        Boot = 0,

        /// <summary>Auto-battling through normal enemy waves.</summary>
        Combat = 1,

        /// <summary>Active boss encounter with a countdown timer.</summary>
        Boss = 2,

        /// <summary>Party wiped or boss timer expired. Progress rolled back one stage.</summary>
        Defeat = 3,

        /// <summary>Combat paused, rewards calculated, world reset, prestige tokens granted.</summary>
        Ascension = 4
    }
}