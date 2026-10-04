using IdleRPG.Combat;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Equipment;
using IdleRPG.Leaderboard;
using IdleRPG.Progression;
using IdleRPG.Save;
using IdleRPG.Services;
using IdleRPG.Sim;

namespace IdleRPG.Core
{
    /// <summary>
    /// One box holding every wired system, built once at boot and handed to whoever needs it.
    ///
    /// ELI5: before, each screen hunted around the scene for the GameManager ("is anyone there?") and got
    /// confused when it was not ready yet. Now there is a single box of labelled plugs that is filled in once,
    /// so nothing has to search, and nothing is ever half-connected.
    /// </summary>
    public sealed class GameContext
    {
        public BalanceConfig Balance { get; set; }

        public WaveConfig Waves { get; set; }

        public PartyConfig Party { get; set; }

        public Formation Formation { get; set; }

        public CombatManager Combat { get; set; }

        public EconomyManager Economy { get; set; }

        public StatResolver Resolver { get; set; }

        public UpgradeManager Upgrade { get; set; }

        public AscensionManager Ascension { get; set; }

        public BoostManager Boost { get; set; }

        public IAdService Ads { get; set; }

        public IIapService Iap { get; set; }

        public IAudioService Audio { get; set; }

        public SimLedger Ledger { get; set; }

        public RewardService Rewards { get; set; }

        public ShopService Shop { get; set; }

        /// <summary>Gear the party owns and wears (boss drops, inventory, slots). Schema v6.</summary>
        public ItemService Gear { get; set; }

        /// <summary>Global ranks (mock field + the local player).</summary>
        public LeaderboardService Leaderboard { get; set; }

        public SaveManager Save { get; set; }

        public IdleTimeService Idle { get; set; }

        public AutomationService Automation { get; set; }

        /// <summary>The one checkout for progression rows (B5) - the dev overlay reads gem-track levels from it.</summary>
        public TrackService Tracks { get; set; }

        /// <summary>Every-morning gift calendar (B7 S2).</summary>
        public DailyStreakService DailyStreak { get; set; }

        /// <summary>Per-day ad police (B7 S3).</summary>
        public AdCapsService AdCaps { get; set; }

        /// <summary>Three first-run hints, spoken once each through the toast line (B8').</summary>
        public FirstRunTips FirstRunTips { get; set; }

        /// <summary>True once the minimum set of systems is present.</summary>
        public bool IsReady => Balance != null && Waves != null && Party != null && Combat != null && Economy != null;

        public override string ToString()
        {
            return $"GameContext(ready={IsReady}, combat={(Combat != null ? "yes" : "no")}, save={(Save != null ? "yes" : "no")})";
        }
    }
}
