using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using IdleRPG.Combat;
using IdleRPG.Data;
using IdleRPG.DebugTools;
using IdleRPG.Economy;
using IdleRPG.Sim;
using IdleRPG.Progression;
using IdleRPG.Save;
using IdleRPG.Services;
using IdleRPG.Utils;

namespace IdleRPG.Core
{
    /// <summary>
    /// Wiring: the composition root — every service is created here, then the context box is filled and the run controller attached.
    /// </summary>
    public sealed partial class GameManager : MonoBehaviour
    {

        private bool WireUp()
        {
            if (formationConfig == null)
            {
                Debug.LogError("[GameManager] No FormationData assigned; the party falls back to fixed lanes.");
            }

            if (balanceConfig == null || waveConfig == null || partyConfig == null)
            {
                Debug.LogError("[GameManager] BalanceConfig / WaveConfig / PartyConfig must be assigned in the inspector.");
                return false;
            }

            if (combatManager == null)
            {
                Debug.LogError("[GameManager] CombatManager reference is missing.");
                return false;
            }

            Economy = new EconomyManager(balanceConfig);
            Economy.ApplyStartingBalances();

            Resolver = new StatResolver(balanceConfig, statUpgrades, prestigeUpgrades);
            Resolver.StatsChanged += OnStatsChanged;

            // Step 14a (B5 session 1): one checkout for every progression row. StatResolver still owns the levels.
            float fallbackGrowth = balanceConfig != null ? balanceConfig.UpgradeCostGrowth : 1.07f;
            Tracks = new TrackService(Economy, Resolver, statUpgrades, prestigeUpgrades, automationDefs, fallbackGrowth);

            Upgrade = new UpgradeManager(Economy, Resolver, partyConfig, Tracks);
            Ascension = new AscensionManager(balanceConfig, Economy, Resolver, prestigeUpgrades, Tracks);

            // B6: the automation engine. Cards are bought with tokens; the auto-buy machine runs on the 1s tick.
            Automation = new AutomationService(Tracks, Economy, partyConfig, automationDefs);
            Automation.Changed += OnAutomationChanged;
            Boost = new BoostManager(balanceConfig);
            Ads = new MockAdService(this, 3f, true);

            // B7 S1: the toy shop's cash register. Mock today; Unity IAP implements the same seam at launch.
            Iap = new MockIapService(this, 1f);

            // Step 9b-3: placeholder SFX (procedural tones) driven by the event bus.
            Audio = new PlaceholderAudioService(this);
            AudioDirector.Create(gameObject, Audio);

            Ledger = new SimLedger(balanceConfig != null ? balanceConfig.GoldPerSecondSampleWindowSec : 60f);
            Rewards = new RewardService(Economy, Ledger, ResolveGoldReward);
            Shop = new ShopService(balanceConfig, Economy);

            // B7 S2/S3: the every-morning gift + the ad police. One injected clock (fake-able by the debug menu).
            DailyStreak = new Economy.DailyStreakService(balanceConfig, ResolveClockNow);
            AdCaps = new Economy.AdCapsService(balanceConfig, ResolveClockNow);
            FirstRunTips = new Core.FirstRunTips(Economy, Tracks, () => Ascension != null && Ascension.CanAscend(RunBestStage));

            // Step 10a/10b: the board the party stands on. Created **before** the load so a saved layout can be
            // restored into it; with no saved layout the default placement (the MVP's fixed lanes) is used, so a
            // fresh game - and an upgraded v2 save - behaves exactly as before.
            if (formationConfig != null)
            {
                Formation = new Formation(formationConfig);
                Formation.PlaceInDefaultSlots(partyConfig.ValidHeroCount);

                // Any swap (the Party screen, or a debug tool) marks the save dirty and re-stamps the fight; Save
                // is null during this first placement, which the null-conditional handles.
                Formation.Changed += OnFormationChanged;
            }

            Save = new SaveManager(new SaveSystem(), balanceConfig, CaptureSnapshot);
            LoadedFromSave = Save.TryLoad(out SaveData loaded);

            if (LoadedFromSave)
            {
                ApplySnapshot(loaded);

                if (Save.RecoveredFromBackup)
                {
                    // Rewrite straight away so the damaged file is replaced by good data.
                    Save.SaveNow("recovery");
                }
            }

            if (!combatManager.Initialize(balanceConfig, waveConfig, partyConfig, Formation))
            {
                Debug.LogError("[GameManager] CombatManager failed to initialise (check PartyConfig heroes).");
                return false;
            }

            combatManager.SetStatProvider(Resolver);

            if (LoadedFromSave)
            {
                // Resume exactly where the player left off, healing the party like a retry would.
                combatManager.SetProgress(CurrentStage, RestoredWave, healParty: true);
            }
            else
            {
                combatManager.SetStage(CurrentStage, healParty: true);
            }

            // Step 9b-2: one owner for time-based payouts (offline window + instant income).
            Idle = new IdleTimeService(balanceConfig, waveConfig, Rewards, Ledger, ResolveGoldReward)
            {
                BonusEquivalentCapSeconds = Shop != null ? Shop.OfflineCapBonusSeconds : 0d
            };
            Idle.Attach();
            Idle.RewardPaid += OnIdleRewardPaid;

            SubscribeCombat();
            SubscribeProgression();
            SetState(GameState.Boot, GameState.Combat);

            Debug.Log($"[GameManager] Save load: {Save.LastLoadSource} | stage {CurrentStage} wave {RestoredWave} | {Economy}");

            BuildContext();

            return true;
        }
#if UNITY_EDITOR

        /// <summary>
        /// Editor-only wiring hook used by the authoring tools. Keeps the serialized
        /// fields private while avoiding reflection/SerializedObject in scene builders.
        /// </summary>
        public void EditorInitialize(BalanceConfig balance, WaveConfig waves, PartyConfig party, CombatManager combat,
            FormationData formation = null)
        {
            balanceConfig = balance;
            waveConfig = waves;
            partyConfig = party;
            combatManager = combat;

            if (formation != null)
            {
                formationConfig = formation;
            }
        }
#endif

        /// <summary>One-second tick for time-based systems (boost expiry; autosave joins in Step 5).</summary>
        /// <summary>
        /// Fills the <see cref="GameContext"/> and starts the single heartbeat. Everything exists by this
        /// point, so the box is complete the moment anything can read it.
        /// </summary>
        private void BuildContext()
        {
            Context = new GameContext
            {
                Balance = balanceConfig,
                Waves = waveConfig,
                Party = partyConfig,
                Formation = Formation,
                Combat = combatManager,
                Economy = Economy,
                Resolver = Resolver,
                Upgrade = Upgrade,
                Ascension = Ascension,
                Automation = Automation,
                Tracks = Tracks,
                DailyStreak = DailyStreak,
                AdCaps = AdCaps,
                FirstRunTips = FirstRunTips,
                Boost = Boost,
                Ads = Ads,
                Iap = Iap,
                Audio = Audio,
                Ledger = Ledger,
                Rewards = Rewards,
                Shop = Shop,
                Save = Save,
                Idle = Idle
            };

            if (runner == null)
            {
                runner = gameObject.GetComponent<RunController>();

                if (runner == null)
                {
                    runner = gameObject.AddComponent<RunController>();
                }
            }

            Telemetry = new TelemetryFeed();
            Telemetry.Attach();

            runner.Attach(Context, true);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Dev-only dashboard (F3); never compiled into a release build.
            DevOverlay.Create(gameObject, Context, Telemetry);
#endif

            LogFlow($"Run controller attached | {Context}");
        }
    }
}
