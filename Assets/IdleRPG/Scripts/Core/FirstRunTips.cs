using System;
using IdleRPG.Economy;
using IdleRPG.Progression;
using IdleRPG.Save;

namespace IdleRPG.Core
{
    /// <summary>
    /// Three first-run hints (B8').
    ///
    /// Plain words: the game already has a little message line at the bottom of the screen. This service speaks
    /// through it, three times, to a brand-new player: "tap UPGRADE to hit harder", "you can buy an upgrade — buy
    /// it", "ascension is ready — ascend and keep a bonus". Each hint fires once and is remembered in the save's
    /// existing keyed list (the same notebook the automation settings use), so it never repeats and comes back
    /// after a full reset — a fresh save is, again, a new player.
    ///
    /// Tip 1 fires when the session starts. Tips 2 and 3 are checked on the 1-second tick, so they fire the moment
    /// the game first has something to show (an affordable upgrade, a ready ascension).
    /// </summary>
    public sealed class FirstRunTips
    {
        /// <summary>Bit i of the mask: hint i has already been shown.</summary>
        public const int TipWelcome = 1 << 0;
        public const int TipAffordUpgrade = 1 << 1;
        public const int TipAscension = 1 << 2;
        public const int AllMask = TipWelcome | TipAffordUpgrade | TipAscension;

        private readonly EconomyManager economy;
        private readonly TrackService tracks;
        private readonly Func<bool> ascensionReady;

        private int mask;

        public FirstRunTips(EconomyManager economy, TrackService tracks, Func<bool> ascensionReady)
        {
            this.economy = economy;
            this.tracks = tracks;
            this.ascensionReady = ascensionReady;
        }

        /// <summary>Which hints have been shown (persisted by the caller as a keyed level).</summary>
        public int Mask => mask;

        /// <summary>All three hints are done - the toast queue stays quiet for this player.</summary>
        public bool AllShown => mask == AllMask;

        /// <summary>Re-applies the remembered mask after a save load.</summary>
        public void Restore(int shownMask)
        {
            mask = shownMask & AllMask;
        }

        /// <summary>Tip 1: fired once the session is up (the save has just been loaded or created).</summary>
        public void OnSessionStart()
        {
            if ((mask & TipWelcome) != 0)
            {
                return;
            }

            mask |= TipWelcome;
            GameEvents.RaiseToast("Tap UPGRADE to hit harder - then push on to the next stage.");
        }

        /// <summary>Checked on the 1-second tick: the first time an upgrade is clearly affordable, and the first time ascension unlocks.</summary>
        public void Tick()
        {
            if (economy == null)
            {
                return;
            }

            if ((mask & TipAffordUpgrade) == 0 && CheapUpgradeAffordable())
            {
                mask |= TipAffordUpgrade;
                GameEvents.RaiseToast("You can afford an upgrade - buy it and watch the fight speed up.");
            }

            if ((mask & TipAscension) == 0 && ascensionReady != null && ascensionReady())
            {
                mask |= TipAscension;
                GameEvents.RaiseToast("Ascension is ready - ascend and keep a bonus for the next run.");
            }
        }

        /// <summary>
        /// True when the player's gold already covers the cheapest gold-priced upgrade, with a little room to spare,
        /// so the hint arrives when it can actually be followed.
        /// </summary>
        private bool CheapUpgradeAffordable()
        {
            if (tracks == null)
            {
                return false;
            }

            double cheapest = double.MaxValue;

            for (int i = 0; i < tracks.Count; i++)
            {
                ProgressionTrack track = tracks.Tracks[i];

                if (track.Currency == CurrencyType.Gold)
                {
                    cheapest = System.Math.Min(cheapest, track.BaseCost);
                }
            }

            return cheapest != double.MaxValue && economy.Gold >= cheapest * 1.05d;
        }
    }
}