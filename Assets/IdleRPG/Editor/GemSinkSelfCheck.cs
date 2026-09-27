using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Progression;
using IdleRPG.Save;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Headless checks for the gems-priced permanent track (B7 S4). Pure C# services only - no play mode:
    ///   Unity -batchmode -quit -projectPath ... -executeMethod IdleRPG.EditorTools.GemSinkSelfCheck.RunAll
    /// Exit code 0 = all green.
    /// </summary>
    public static class GemSinkSelfCheck
    {
        private static int failures;

        [MenuItem("Tools/Idle RPG/Debug/Gem Sinks/Run Gem-Sink Self Check", priority = 140)]
        public static void RunAll()
        {
            failures = 0;

            BalanceConfig balance = BalanceLabMenu.Load<BalanceConfig>("BalanceConfig");
            StatUpgradeData atk = BalanceLabMenu.Load<StatUpgradeData>("StatUpgrade_ATK");
            StatUpgradeData hp = BalanceLabMenu.Load<StatUpgradeData>("StatUpgrade_HP");
            StatUpgradeData def = BalanceLabMenu.Load<StatUpgradeData>("StatUpgrade_DEF");
            PrestigeUpgradeData gemTrack = BalanceLabMenu.Load<PrestigeUpgradeData>("Track_GemGold");
            PartyConfig partyConfig = BalanceLabMenu.Load<PartyConfig>("PartyConfig");

            if (balance == null || atk == null || gemTrack == null || partyConfig == null)
            {
                Debug.LogError("[GemSinkSelfCheck] Missing BalanceConfig / StatUpgrade_ATK / Track_GemGold asset.");
                Report();
                return;
            }

            Check("the gem track pays with GEMS", gemTrack.CostCurrency == CurrencyType.Gems);
            Check("the gem track is capped", gemTrack.HasLevelCap && gemTrack.MaxLevel == 10);
            Debug.Log(string.Format("[GemSinkSelfCheck] {0}: {1:0} gems base, growth x{2:0.##}, +{3:0.#}%/lvl, cap {4}",
                gemTrack.UpgradeID, gemTrack.BaseCostTokens, gemTrack.CostGrowth, gemTrack.EffectPerLevel * 100f, gemTrack.MaxLevel));

            List<StatUpgradeData> stats = new List<StatUpgradeData> { atk, hp, def };
            List<PrestigeUpgradeData> global = new List<PrestigeUpgradeData> { gemTrack };
            List<AutomationDef> cards = new List<AutomationDef>();

            EconomyManager economy = new EconomyManager(balance);
            economy.ApplyStartingBalances();
            economy.AddGems(100000d);

            StatResolver resolver = new StatResolver(balance, stats, global);
            TrackService tracks = new TrackService(economy, resolver, stats, global, cards, balance.UpgradeCostGrowth);

            ProgressionTrack track = default;
            IReadOnlyList<ProgressionTrack> all = tracks.Tracks;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Currency == CurrencyType.Gems) { track = all[i]; break; }
            }

            Check("the shop can enumerate a gems track", track.Id == gemTrack.UpgradeID);
            Check("tokens are NOT accepted for it", economy.PrestigeTokens == 0d);

            double gemsBefore = economy.Gems;
            PurchaseResult first = tracks.TryBuy(track, null, 1, out double spent, out int level);
            Check("level 1 buys", first == PurchaseResult.Bought && level == 1);
            Check("gems were spent (not gold/tokens)",
                System.Math.Abs(gemsBefore - economy.Gems - spent) < 0.001d && economy.Gold == balance.StartingGold);
            Check("level-1 cost = base", System.Math.Abs(spent - gemTrack.BaseCostTokens) < 0.001d);
            Check("+5% gold at level 1", System.Math.Abs(resolver.GlobalGoldMultiplier - 1.05d) < 0.0001d);

            double second = tracks.Cost(track, null, 1);
            Check("level-2 cost grows x1.6", System.Math.Abs(second - gemTrack.BaseCostTokens * 1.6d) < 0.01d);

            for (int i = 1; i < gemTrack.MaxLevel; i++)
            {
                tracks.TryBuy(track, null, 1, out _, out _);
            }

            Check("cap reached at 10", tracks.GetLevel(track, null) == gemTrack.MaxLevel && tracks.IsMaxed(track, null));
            Check("+50% gold at the cap", System.Math.Abs(resolver.GlobalGoldMultiplier - 1.5d) < 0.0001d);

            PurchaseResult over = tracks.TryBuy(track, null, 1, out _, out _);
            Check("buying past the cap is refused", over == PurchaseResult.Maxed);

            SaveData data = SaveData.CreateDefault();
            resolver.WriteToSave(data, partyConfig);
            tracks.WriteToSave(data);
            StatResolver resolver2 = new StatResolver(balance, stats, global);
            resolver2.FillFromSave(data, partyConfig);
            TrackService reloaded = new TrackService(new EconomyManager(balance), resolver2, stats, global, cards, balance.UpgradeCostGrowth);
            reloaded.FillFromSave(data);
            ProgressionTrack again = default;
            IReadOnlyList<ProgressionTrack> all2 = reloaded.Tracks;
            for (int i = 0; i < all2.Count; i++) { if (all2[i].Currency == CurrencyType.Gems) { again = all2[i]; break; } }
            Check("a reload keeps the level (resolver save path)", reloaded.GetLevel(again, null) == gemTrack.MaxLevel);

            Report();
        }

        private static void Check(string what, bool ok)
        {
            if (ok)
            {
                Debug.Log($"[GemSinkSelfCheck] OK   {what}");
                return;
            }

            failures++;
            Debug.LogError($"[GemSinkSelfCheck] FAIL {what}");
        }

        private static void Report()
        {
            if (failures == 0)
            {
                Debug.Log("[GemSinkSelfCheck] RESULT: PASS - gem track spends gems, costs grow, cap holds, level survives a reload.");
            }
            else
            {
                Debug.LogError($"[GemSinkSelfCheck] RESULT: FAIL - {failures} check(s) failed.");
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(failures == 0 ? 0 : 1);
            }
        }
    }
}
