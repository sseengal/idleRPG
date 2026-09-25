using System;
using System.Collections.Generic;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Save;
using UnityEngine;

namespace IdleRPG.Progression
{
    /// <summary>
    /// The automation engine (B6): runs the owned automation cards on the 1-second tick.
    ///
    /// ELI5: a card is a small machine you buy once. The auto-buy machine buys the cheapest upgrade for you, but it
    /// promises to never touch a slice of your wallet that YOU choose (the reserve dial). Speed arrives in Step 3.
    ///
    /// Design rules that keep the game worth playing (Roadmap B6):
    /// - EARNED: cards are bought with rebirth tokens, never given away free.
    /// - VISIBLE: every machine action shows a toast ("Auto-Buy: 3 levels") - nothing runs silently.
    /// - INCOMPLETE ON PURPOSE: the reserve dial is the player's hand on the machine.
    /// Ascension has NO card: the big reset stays 100% manual by design.
    /// </summary>
    public sealed class AutomationService
    {
        /// <summary>Default "keep this much gold safe": half the wallet.</summary>
        public const float DefaultBudgetFraction = 0.5f;

        /// <summary>Safety valve: never buy more than this per tick (a broken cost curve must not drain the wallet).</summary>
        private const int MaxBuysPerTick = 40;

        private readonly TrackService tracks;
        private readonly EconomyManager economy;
        private readonly PartyConfig party;
        private readonly List<AutomationDef> defs = new List<AutomationDef>();
        private readonly Dictionary<string, AutomationSetting> settings = new Dictionary<string, AutomationSetting>();

        private static readonly HeroStatType[] UpgradeStats =
        {
            HeroStatType.Attack,
            HeroStatType.Health,
            HeroStatType.Defense
        };

        /// <summary>Raised when card ownership or the settings change (the game marks the save dirty).</summary>
        public event Action Changed;

        public AutomationService(TrackService tracks, EconomyManager economy, PartyConfig party,
            IEnumerable<AutomationDef> automationDefs)
        {
            this.tracks = tracks;
            this.economy = economy;
            this.party = party;

            if (automationDefs != null)
            {
                foreach (AutomationDef def in automationDefs)
                {
                    if (def != null)
                    {
                        defs.Add(def);
                    }
                }
            }
        }

        /// <summary>Every card, in authoring order (the panel lists these).</summary>
        public IReadOnlyList<AutomationDef> Defs => defs;

        public bool TryGetDef(string id, out AutomationDef def)
        {
            for (int i = 0; i < defs.Count; i++)
            {
                if (defs[i] != null && defs[i].AutomationID == id)
                {
                    def = defs[i];
                    return true;
                }
            }

            def = null;
            return false;
        }

        /// <summary>Card bought? (level 1 on its keyed save record = owned.)</summary>
        public bool Owned(AutomationDef def)
        {
            return Level(def) >= 1;
        }

        public int Level(AutomationDef def)
        {
            if (def == null || tracks == null || !tracks.TryGetTrack(def.AutomationID, out ProgressionTrack track))
            {
                return 0;
            }

            return tracks.GetLevel(track, null);
        }

        /// <summary>Token price of a card.</summary>
        public double Cost(AutomationDef def)
        {
            if (def == null || tracks == null || !tracks.TryGetTrack(def.AutomationID, out ProgressionTrack track))
            {
                return 0d;
            }

            return tracks.Cost(track, null, 1);
        }

        /// <summary>True when the player has enough rebirths and tokens, and does not own it yet.</summary>
        public bool CanBuy(AutomationDef def, int ascensionCount)
        {
            return def != null && !Owned(def) && ascensionCount >= def.MinAscensions &&
                   economy != null && economy.CanAfford(CurrencyType.PrestigeTokens, Cost(def));
        }

        /// <summary>Buys a card through the single checkout (TrackService). Returns true when owned after the call.</summary>
        public bool TryBuy(AutomationDef def)
        {
            if (def == null || tracks == null || !tracks.TryGetTrack(def.AutomationID, out ProgressionTrack track))
            {
                return false;
            }

            PurchaseResult result = tracks.TryBuy(track, null, 1, out _, out _);
            bool bought = result == PurchaseResult.Bought;

            if (bought)
            {
                GameEvents.RaiseToast($"Automation unlocked: {def.DisplayName}");
                Changed?.Invoke();
            }

            return bought;
        }

        // ------------------------------------------------------------------
        // Settings (the panel reads/writes these; they persist in the save)
        // ------------------------------------------------------------------
        public bool IsEnabled(AutomationDef def)
        {
            return def != null && settings.TryGetValue(def.AutomationID, out AutomationSetting s) && s.enabled;
        }

        /// <summary>Fraction of the wallet the machine must keep safe (0 = keep nothing, 1 = never buy).</summary>
        public float BudgetFraction(AutomationDef def)
        {
            if (def != null && settings.TryGetValue(def.AutomationID, out AutomationSetting s))
            {
                return Mathf.Clamp01(s.budgetFraction);
            }

            return DefaultBudgetFraction;
        }

        /// <summary>Sets the enabled flag; the game marks the save dirty.</summary>
        public void SetEnabled(AutomationDef def, bool enabled)
        {
            if (def == null || !Owned(def))
            {
                return;
            }

            AutomationSetting setting = GetSetting(def.AutomationID);
            setting.enabled = enabled;
            Changed?.Invoke();
        }

        /// <summary>Sets the reserve dial ("never touch this fraction of my gold").</summary>
        public void SetBudgetFraction(AutomationDef def, float fraction)
        {
            if (def == null)
            {
                return;
            }

            AutomationSetting setting = GetSetting(def.AutomationID);
            setting.budgetFraction = Mathf.Clamp01(fraction);
            Changed?.Invoke();
        }

        // ------------------------------------------------------------------
        // Save (schema v5 additive; ownership lives in the keyed levels via TrackService)
        // ------------------------------------------------------------------
        public void FillFromSave(SaveData data)
        {
            settings.Clear();

            if (data == null || data.automation == null)
            {
                return;
            }

            for (int i = 0; i < data.automation.Count; i++)
            {
                AutomationSetting setting = data.automation[i];
                if (setting != null && !string.IsNullOrEmpty(setting.ruleId))
                {
                    settings[setting.ruleId] = setting;
                }
            }
        }

        public void WriteToSave(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            data.automation = new List<AutomationSetting>(settings.Count);

            foreach (KeyValuePair<string, AutomationSetting> pair in settings)
            {
                data.automation.Add(new AutomationSetting(pair.Key, pair.Value.enabled, pair.Value.budgetFraction));
            }
        }

        private AutomationSetting GetSetting(string id)
        {
            if (!settings.TryGetValue(id, out AutomationSetting setting) || setting == null)
            {
                setting = new AutomationSetting(id, false, DefaultBudgetFraction);
                settings[id] = setting;
            }

            return setting;
        }

        /// <summary>Combat tempo while the Speed Button is owned and on (Step 3).</summary>
        public const double SpeedFactor = 2d;

        /// <summary>True when the Speed Button card is owned and switched on.</summary>
        public bool SpeedEnabled
        {
            get
            {
                for (int i = 0; i < defs.Count; i++)
                {
                    AutomationDef def = defs[i];
                    if (def != null && def.AutomationID == "fastForward" && Owned(def) && IsEnabled(def))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Multiplier applied to combat pace (1 = normal, 2 = the speed button).</summary>
        public double SpeedMultiplier => SpeedEnabled ? SpeedFactor : 1d;

        // ------------------------------------------------------------------
        // The engine: runs on the 1-second tick
        // ------------------------------------------------------------------
        public void Tick(double deltaTime)
        {
            if (tracks == null || economy == null || party == null || defs.Count == 0)
            {
                return;
            }

            for (int i = 0; i < defs.Count; i++)
            {
                AutomationDef def = defs[i];

                if (def == null || !Owned(def) || !IsEnabled(def))
                {
                    continue;
                }

                if (def.AutomationID == "autoBuy")
                {
                    TryAutoBuy(def);
                }

                // "fastForward" changes combat tempo - lands in Step 3.
            }
        }

        /// <summary>
        /// Buys the cheapest affordable stat upgrade (all heroes, ATK/HP/DEF) while staying above the reserved slice
        /// of the wallet. One aggregated toast per tick, so automation is visible without spamming.
        /// </summary>
        private void TryAutoBuy(AutomationDef def)
        {
            double gold = economy.Gold;
            double reserve = gold * BudgetFraction(def);
            int buys = 0;

            while (buys < MaxBuysPerTick)
            {
                int bestHero = -1;
                ProgressionTrack bestTrack = default;
                double bestCost = double.MaxValue;

                for (int h = 0; h < party.ValidHeroCount; h++)
                {
                    HeroData hero = party.GetHero(h);
                    if (hero == null)
                    {
                        continue;
                    }

                    for (int s = 0; s < UpgradeStats.Length; s++)
                    {
                        HeroStatType stat = UpgradeStats[s];

                        if (!tracks.TryGetHeroTrack(stat, out ProgressionTrack track))
                        {
                            continue;
                        }

                        if (tracks.IsMaxed(track, hero))
                        {
                            continue;
                        }

                        double cost = tracks.Cost(track, hero, 1);

                        if (cost <= gold - reserve && cost <= gold && cost < bestCost)
                        {
                            bestCost = cost;
                            bestHero = h;
                            bestTrack = track;
                        }
                    }
                }

                if (bestHero < 0)
                {
                    break;
                }

                if (tracks.TryBuy(bestTrack, party.GetHero(bestHero), 1, out double spent, out _) == PurchaseResult.Bought)
                {
                    gold -= spent;
                    buys++;
                }
                else
                {
                    break;
                }
            }

            if (buys > 0)
            {
                GameEvents.RaiseToast($"Auto-Buy: {buys} level(s)");
            }
        }
    }
}