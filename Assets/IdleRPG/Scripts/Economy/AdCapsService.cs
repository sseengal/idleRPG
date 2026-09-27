using System;
using System.Collections.Generic;
using IdleRPG.Data;
using IdleRPG.Save;

namespace IdleRPG.Economy
{
    /// <summary>
    /// Per-day police for rewarded ads (B7 S3): each placement has a daily cap and a cooldown, both data-driven
    /// (<see cref="AdPlacementDef"/> in BalanceConfig). Pure C# with an injected clock, same shape as the daily
    /// streak, so the debug menu can fake "tomorrow" and verify the day rollover without waiting 24 hours.
    ///
    /// ELI5: the piggy bank only accepts a few cartoons per day. This is the bank teller who counts and says no.
    /// </summary>
    public sealed class AdCapsService
    {
        private readonly BalanceConfig balance;
        private readonly Func<DateTime> now;
        private readonly Dictionary<int, AdRedemptionRecord> records = new Dictionary<int, AdRedemptionRecord>();

        public AdCapsService(BalanceConfig balance, Func<DateTime> now)
        {
            this.balance = balance;
            this.now = now ?? (() => DateTime.Now);
        }

        /// <summary>How many redemptions of this placement are still allowed today (0 = none, cap reached).</summary>
        public int RemainingToday(AdPlacementId placementId)
        {
            AdPlacementDef def = balance != null ? balance.GetAdPlacement(placementId) : null;
            if (def == null || def.DailyCap <= 0)
            {
                return 0;
            }

            AdRedemptionRecord record = GetRecord(placementId);
            int day = DayStamp(now());

            if (record.dayStamp != day)
            {
                return def.DailyCap;
            }

            return Math.Max(0, def.DailyCap - record.redemptions);
        }

        /// <summary>True when this placement may be shown right now; a cooldown wait (seconds) is reported out.</summary>
        public bool CanShow(AdPlacementId placementId, out double cooldownSecondsLeft)
        {
            cooldownSecondsLeft = 0d;

            AdPlacementDef def = balance != null ? balance.GetAdPlacement(placementId) : null;
            if (def == null)
            {
                return false;
            }

            AdRedemptionRecord record = GetRecord(placementId);
            int day = DayStamp(now());

            if (record.dayStamp != day)
            {
                return true;
            }

            if (record.redemptions >= def.DailyCap)
            {
                return false;
            }

            if (def.CooldownSec <= 0f)
            {
                return true;
            }

            long elapsedTicks = now().Ticks - (long)record.lastRedeemedBinary;
            long needed = (long)(def.CooldownSec * TimeSpan.TicksPerSecond);

            if (elapsedTicks < needed)
            {
                cooldownSecondsLeft = (needed - elapsedTicks) / (double)TimeSpan.TicksPerSecond;
                return false;
            }

            return true;
        }

        /// <summary>Records a redemption (the ad actually completed). Resets the day automatically on rollover.</summary>
        public void MarkShown(AdPlacementId placementId)
        {
            AdRedemptionRecord record = GetRecord(placementId);
            int day = DayStamp(now());

            if (record.dayStamp != day)
            {
                record.dayStamp = day;
                record.redemptions = 0;
            }

            record.redemptions++;
            record.lastRedeemedBinary = (double)now().Ticks;
        }

        /// <summary>Wipes all counters (debug reset / day-rollover tests).</summary>
        public void ClearAll()
        {
            records.Clear();
        }

        public void Restore(IEnumerable<AdRedemptionRecord> saved)
        {
            records.Clear();

            if (saved == null)
            {
                return;
            }

            foreach (AdRedemptionRecord record in saved)
            {
                if (record == null || records.ContainsKey(record.placementId))
                {
                    continue;
                }

                records[record.placementId] = new AdRedemptionRecord
                {
                    placementId = record.placementId,
                    dayStamp = record.dayStamp,
                    redemptions = record.redemptions < 0 ? 0 : record.redemptions,
                    lastRedeemedBinary = record.lastRedeemedBinary
                };
            }
        }

        public void WriteToSave(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            data.adRedemptions = new List<AdRedemptionRecord>();

            foreach (AdRedemptionRecord record in records.Values)
            {
                data.adRedemptions.Add(new AdRedemptionRecord
                {
                    placementId = record.placementId,
                    dayStamp = record.dayStamp,
                    redemptions = record.redemptions,
                    lastRedeemedBinary = record.lastRedeemedBinary
                });
            }
        }

        private AdRedemptionRecord GetRecord(AdPlacementId placementId)
        {
            int key = (int)placementId;

            if (!records.TryGetValue(key, out AdRedemptionRecord record))
            {
                record = new AdRedemptionRecord { placementId = key };
                records[key] = record;
            }

            return record;
        }

        private static int DayStamp(DateTime date)
        {
            return date.Year * 10000 + date.Month * 100 + date.Day;
        }
    }
}
