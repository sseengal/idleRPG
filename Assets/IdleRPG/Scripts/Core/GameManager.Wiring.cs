using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using IdleRPG.Abilities;
using IdleRPG.Combat;
using IdleRPG.Data;
using IdleRPG.DebugTools;
using IdleRPG.Equipment;
using IdleRPG.Leaderboard;
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

        /// <summary>
        /// Builds every system. THE ORDER MATTERS - do not reorder these blocks:
        ///
        ///   1. the services: economy, resolver, gear, tracks, upgrade, ascension, automation, boost, ads, iap,
        ///      audio, ledger, rewards, shop, daily streak, ad caps, first-run tips.
        ///   2. Formation, then SaveManager, then the load. The load restores the shop's offline cap bonus and the
        ///      party's saved layout, so both must exist before it runs.
        ///   3. IdleTimeService is created AFTER the load on purpose: it copies Shop.OfflineCapBonusSeconds into
        ///      its own cap, so creating it earlier would silently ignore a bought cap extension.
        ///   4. combat initialise + progress restore, then the event subscriptions, then the context.
        ///
        /// <see cref="Context"/> is filled last because it needs every service, Save and Idle included. Nothing
        /// may read Context during the load - the backup-recovery path saves from inside this method - which is
        /// why the snapshot capture reads the manager's own fields and not the context.
        /// </summary>
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

            // Battle log: one store holds every line the player reads. Installed here, before the save load, so a
            // resumed session lands in the feed too. The names come from this object, so the store never needs the UI.
            BattleLog.Install();
            BattleLog.HeroNameResolver = HeroLogName;
            BattleLog.EnemyNameResolver = EnemyLogName;

            Economy = new EconomyManager(balanceConfig);
            Economy.ApplyStartingBalances();

            Resolver = new StatResolver(balanceConfig, statUpgrades, prestigeUpgrades);
            Resolver.StatsChanged += OnStatsChanged;

            // Step M1.6: gear owns boss drops + the inventory. The resolver reads its summed bonuses.
            Gear = new ItemService(balanceConfig, partyConfig, Economy, Resolver.GetUtilityLevel);
            Gear.Changed += OnGearChanged;
            Resolver.GearBonusReader = heroIndex => Gear != null ? Gear.GetGearBonusFraction(heroIndex) : default;

            // Leaderboard: deterministic mock field + the local player (real backend comes later).
            Leaderboard = new LeaderboardService(() => Resolver != null ? Resolver.TotalHeroLevels : 0, () => HighestStageReached);

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
            // Ability-Plan S2: abilities are bought with tokens and wear a stage gate, so the service asks for the
            // CURRENT stage at unlock time rather than caching one at construction.
            Abilities = new AbilityService(Economy, () => CurrentStage);

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

            // MVP stop-gap (24 Oct 2026): the moves are open from the first wave, because nothing in the game hands
            // them over yet. Runs after the save load (so a save's own unlocks are kept) and before the party is
            // built below (the fight takes equipped abilities when it is built).
            // TODO: ability unlock story moment - delete this call and unlock each ability at its story beat.
            if (unlockAllAbilitiesForMvp && Abilities != null && partyConfig != null)
            {
                int equipped = Abilities.OpenAllForMvp(partyConfig.Heroes);
                Debug.Log($"[GameManager] MVP ability unlock: {equipped} hero(es) equipped.");
            }

            // One owner for the money door: the rewarded-ad guard and the IAP cash register. Everything it needs
            // (ads, ad caps, iap, boost, shop, till, save) exists by now.
            monetization = new MonetizationService(Ads, AdCaps, Iap, Boost, Shop, Rewards, Save, LogFlow);

            if (!combatManager.Initialize(balanceConfig, waveConfig, partyConfig, Formation))
            {
                Debug.LogError("[GameManager] CombatManager failed to initialise (check PartyConfig heroes).");
                return false;
            }

            combatManager.SetStatProvider(Resolver);

            // Camp: the run's interlude. Created after combat so it can read the party, before StartRun so the
            // wave gate is armed for the first checkpoint.
            EnsureCampStateLoaded();
            Camp = new CampService(this, monetization);
            combatManager.SetWaveGate(Camp.ShouldHold);

            // Ability-Plan: the fight asks the ability service who is wearing what, by party index. Set BEFORE the
            // party is built below, so resuming a save arms the equipped abilities immediately.
            combatManager.AbilityProvider = index => Abilities.EquippedFor(partyConfig.GetHero(index));

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

            // One owner for the two time payouts: the offline window and instant income. It subscribes to
            // Idle.RewardPaid itself, so both reach the lifetime total through OnIdleGoldEarned.
            offline = new OfflineCoordinator(Idle, Ledger, Save, Shop, () => CurrentStage, OnIdleGoldEarned, LogFlow);

            SubscribeCombat();
            SubscribeProgression();
            SetState(GameState.Boot, GameState.Combat);

            Debug.Log($"[GameManager] Save load: {Save.LastLoadSource} | stage {CurrentStage} wave {RestoredWave} | {Economy}");

            BuildContext();

            return true;
        }

        /// <summary>Name of one hero in the battle log. One rule, used by every line.</summary>
        private string HeroLogName(int heroIndex)
        {
            HeroData hero = partyConfig != null ? partyConfig.GetHero(heroIndex) : null;

            if (hero != null && !string.IsNullOrEmpty(hero.HeroName))
            {
                return hero.HeroName;
            }

            return heroIndex < 0 ? "The party" : $"Hero {heroIndex + 1}";
        }

        /// <summary>
        /// Name of one enemy in the battle log. When the wave holds several of the same archetype the slots are
        /// suffixed A/B/C, so "Werewolf" x3 reads as Werewolf A, Werewolf B, Werewolf C in the log.
        /// </summary>
        private string EnemyLogName(int enemyIndex)
        {
            CombatSimulator simulator = combatManager != null ? combatManager.Simulator : null;
            EnemyCombatant[] enemies = simulator != null ? simulator.Enemies : null;

            if (enemies == null || enemies.Length == 0)
            {
                return "the enemy";
            }

            int index = Mathf.Clamp(enemyIndex, 0, enemies.Length - 1);
            EnemyCombatant enemy = enemies[index];

            if (enemy == null)
            {
                return "the enemy";
            }

            return enemies.Length > 1
                ? string.Format("{0} {1}", enemy.DisplayName, (char)('A' + index))
                : enemy.DisplayName;
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
                Abilities = Abilities,
                Boost = Boost,
                Ads = Ads,
                Iap = Iap,
                Audio = Audio,
                Ledger = Ledger,
                Rewards = Rewards,
                Shop = Shop,
                Gear = Gear,
                Leaderboard = Leaderboard,
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Dev instrumentation - nothing reads Telemetry in a release build (only the gated overlay does).
            Telemetry = new TelemetryFeed();
            Telemetry.Attach();
#endif

            runner.Attach(Context, true);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Dev-only dashboard (F3); never compiled into a release build.
            DevOverlay.Create(gameObject, Context, Telemetry);
#endif

            LogFlow($"Run controller attached | {Context}");
        }
    }
}
