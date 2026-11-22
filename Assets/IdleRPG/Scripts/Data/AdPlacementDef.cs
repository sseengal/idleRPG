using System;
using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>Every rewarded-ad slot in the game (B7 S3). One id per button the player can press to watch an ad.</summary>
    public enum AdPlacementId
    {
        /// <summary>Shop headline: x2 gold for the boost window (caps keep it honest).</summary>
        GoldBoost = 0,

        /// <summary>Welcome-back popup: double the quoted offline gold.</summary>
        DoubleOffline = 1
    }

    /// <summary>
    /// Per-day limits for one ad placement. Everything a designer needs to tune: how many redemptions per local
    /// day, and the minimum gap between two (so the boost cannot be kept on forever by fast taps).
    /// </summary>
    [Serializable]
    public class AdPlacementDef
    {
        public AdPlacementId PlacementId;
        public int DailyCap = 5;
        public float CooldownSec = 300f;

        public AdPlacementDef()
        {
        }

        public AdPlacementDef(AdPlacementId placementId, int dailyCap, float cooldownSec)
        {
            PlacementId = placementId;
            DailyCap = dailyCap;
            CooldownSec = cooldownSec;
        }
    }
}