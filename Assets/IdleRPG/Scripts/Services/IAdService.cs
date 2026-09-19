using System;

namespace IdleRPG.Services
{
    /// <summary>
    /// Rewarded-ad abstraction. The MVP ships <see cref="MockAdService"/>; a real SDK
    /// (Unity Ads / AdMob) only has to implement this interface — no UI or economy changes.
    /// </summary>
    public interface IAdService
    {
        /// <summary>True when a rewarded ad could be shown right now.</summary>
        bool IsRewardedAdReady { get; }

        /// <summary>
        /// Shows a rewarded ad. <paramref name="onCompleted"/> receives true when the player
        /// earned the reward, false when they skipped, dismissed early, or it failed.
        /// Always invoked exactly once.
        /// </summary>
        void ShowRewardedAd(Action<bool> onCompleted);
    }
}