using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Save;

namespace IdleRPG.DebugTools
{
    /// <summary>
    /// A local ring buffer of "what actually happened" during a session.
    ///
    /// ELI5: the game's diary. It keeps the last couple of hundred notable moments (stage ups, purchases,
    /// offline claims, wipes, autosaves) in memory so you can ask "what did the player actually do?" without
    /// any network, accounts or analytics service. Nothing leaves the device.
    ///
    /// In-memory only and allocation-light: entries are formatted strings created at event time, never per frame.
    /// </summary>
    public sealed class TelemetryFeed
    {
        public const int Capacity = 200;

        private readonly Queue<Entry> entries = new Queue<Entry>(Capacity);

        private int totalEvents;

        public struct Entry
        {
            public float TimeSeconds;
            public string Kind;
            public string Message;
        }

        public int Count => entries.Count;

        public int TotalEvents => totalEvents;

        /// <summary>Subscribes to the game event bus. Safe to call once.</summary>
        public void Attach()
        {
            GameEvents.StageChanged += OnStageChanged;
            GameEvents.CurrencyChanged += OnCurrencyChanged;
            GameEvents.UpgradePurchased += OnUpgradePurchased;
            GameEvents.AscensionCompleted += OnAscensionCompleted;
            GameEvents.PartyWiped += OnPartyWiped;
            GameEvents.BossFailed += OnBossFailed;
            GameEvents.OfflineRewardsReady += OnOfflineReady;
            GameEvents.OfflineRewardsClaimed += OnOfflineClaimed;
            GameEvents.SaveWritten += OnSaved;
            GameEvents.SaveLoaded += OnLoaded;
            GameEvents.GoldBoostChanged += OnBoost;
        }

        public void Detach()
        {
            GameEvents.StageChanged -= OnStageChanged;
            GameEvents.CurrencyChanged -= OnCurrencyChanged;
            GameEvents.UpgradePurchased -= OnUpgradePurchased;
            GameEvents.AscensionCompleted -= OnAscensionCompleted;
            GameEvents.PartyWiped -= OnPartyWiped;
            GameEvents.BossFailed -= OnBossFailed;
            GameEvents.OfflineRewardsReady -= OnOfflineReady;
            GameEvents.OfflineRewardsClaimed -= OnOfflineClaimed;
            GameEvents.SaveWritten -= OnSaved;
            GameEvents.SaveLoaded -= OnLoaded;
            GameEvents.GoldBoostChanged -= OnBoost;
        }

        public void Record(string kind, string message)
        {
            if (entries.Count >= Capacity)
            {
                entries.Dequeue();
            }

            entries.Enqueue(new Entry
            {
                TimeSeconds = Time.realtimeSinceStartup,
                Kind = kind,
                Message = message
            });

            totalEvents++;
        }

        /// <summary>Newest entries first, at most <paramref name="count"/>.</summary>
        public List<Entry> Latest(int count)
        {
            List<Entry> result = new List<Entry>(Mathf.Min(count, entries.Count));
            Entry[] all = entries.ToArray();

            for (int i = all.Length - 1; i >= 0 && result.Count < count; i--)
            {
                result.Add(all[i]);
            }

            return result;
        }

        public string DescribeLatest(int count)
        {
            StringBuilder builder = new StringBuilder();
            List<Entry> latest = Latest(count);

            for (int i = 0; i < latest.Count; i++)
            {
                builder.Append(latest[i].TimeSeconds.ToString("0.0")).Append("s ")
                       .Append(latest[i].Kind).Append(": ").AppendLine(latest[i].Message);
            }

            return builder.ToString();
        }

        // --- Event handlers ---
        private void OnStageChanged(int stage, int wave, bool isBoss)
        {
            Record("stage", $"stage {stage} wave {wave}{(isBoss ? " (boss)" : string.Empty)}");
        }

        private void OnCurrencyChanged(CurrencyType currency, double amount)
        {
            // Gold changes on every kill; only note the rarer currencies.
            if (currency != CurrencyType.Gold)
            {
                Record(currency.ToString().ToLowerInvariant(), $"{currency} -> {amount:0.##}");
            }
        }

        private void OnUpgradePurchased(int heroIndex, HeroStatType statType, int newLevel, double goldCost)
        {
            Record("upgrade", $"hero {heroIndex} {statType} -> {newLevel} (-{goldCost:0} gold)");
        }

        private void OnAscensionCompleted(double tokens, int highestStage)
        {
            Record("ascend", $"+{tokens:0} tokens (best stage {highestStage})");
        }

        private void OnPartyWiped()
        {
            Record("defeat", "party wiped");
        }

        private void OnBossFailed()
        {
            Record("defeat", "boss failed");
        }

        private void OnOfflineReady(OfflineRewardResult result)
        {
            Record("offline", $"offered {result.Gold:0} gold for {result.RawSeconds:0}s away");
        }

        private void OnOfflineClaimed(double gold)
        {
            Record("offline", $"claimed {gold:0} gold");
        }

        private void OnSaved()
        {
            Record("save", "written");
        }

        private void OnLoaded()
        {
            Record("save", "loaded");
        }

        private void OnBoost(bool active, float secondsRemaining, double multiplier)
        {
            Record("boost", active ? $"x{multiplier:0.#} for {secondsRemaining:0}s" : "ended");
        }
    }
}
