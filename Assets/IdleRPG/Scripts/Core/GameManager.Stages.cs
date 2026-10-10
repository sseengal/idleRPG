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
    /// Stage flow: run start, wipe fallback, stage advance, ascension, and the combat/progression event handlers that drive them.
    /// </summary>
    public sealed partial class GameManager
    {
        // ------------------------------------------------------------------
        // Run control
        // ------------------------------------------------------------------
        /// <summary>Starts (or restarts) the run from <paramref name="stage"/>.</summary>
        public void StartRun(int stage)
        {
            if (!isWired)
            {
                Debug.LogWarning("[GameManager] StartRun ignored: manager is not wired.");
                return;
            }

            CurrentStage = Mathf.Max(1, stage);
            if (CurrentStage > HighestStageReached)
            {
                HighestStageReached = CurrentStage;
            }

            if (CurrentStage > RunBestStage)
            {
                RunBestStage = CurrentStage;
            }

            combatManager.SetStage(CurrentStage, healParty: true);
            combatManager.StartRun();
            SetState(State == GameState.Boot ? GameState.Boot : State, combatManager.IsBossWave ? GameState.Boss : GameState.Combat);
            RaiseStageChanged();
        }

        /// <summary>
        /// Enters the ascension state. Reward grant + world reset are implemented in Step 3;
        /// the state transition itself is real so UI and combat pausing already work.
        /// </summary>
        public bool RequestAscension()
        {
            if (!isWired)
            {
                return false;
            }

            if (!CanAscend)
            {
                GameEvents.RaiseToast($"Reach stage {balanceConfig.MinStageToAscend} to ascend.");
                return false;
            }

            combatManager.StopRun();
            SetState(State, GameState.Ascension);

            if (!Ascension.TryAscend(RunBestStage, out double tokens))
            {
                Debug.LogWarning("[GameManager] Ascension requested but the token yield is 0.");
                return false;
            }

            // Snapshot the just-ended run BEFORE the reset wipes the counters, so the summary reads the run that ended.
            int endedStage = RunBestStage;
            double endedGold = RunGoldEarned;

            GameEvents.RaiseAscensionCompleted(tokens, endedStage);
            LogFlow($"Ascended for {tokens:0} token(s) from run best stage {endedStage} | " +
                    $"{Ascension.DescribeMultipliers()} | {Economy}");

            ResetProgressForAscension(HighestStageReached);

            GameEvents.RaiseToast(string.Format("Run ended - stage {0}  ·  +{1:0} tokens  ·  {2} gold",
                endedStage, tokens, NumberFormatter.Format(endedGold)));
            return true;
        }

        /// <summary>
        /// Called by the ascension flow: gold, stage and hero levels go back to zero. The lifetime best is kept
        /// (records, UI) but the run best restarts, which is what gates and prices the next ascension.
        /// </summary>
        public void ResetProgressForAscension(int highestStageToKeep)
        {
            CurrentStage = 1;
            RunBestStage = 1;
            HighestStageReached = Mathf.Max(1, highestStageToKeep);

            // A new run starts now: the per-run counters go back to zero.
            RunGoldEarned = 0d;
            RunKills = 0;
            RunStagesCleared = 0;
            runStartBinary = GameClock.NowBinary;

            SetState(State, GameState.Combat);
            combatManager.SetStage(CurrentStage, healParty: true);
            combatManager.StartRun();
            RaiseStageChanged();
        }

        /// <summary>Restarts the fight now, skipping the rest of the post-defeat beat (the modal's "Continue").</summary>
        public void ResumeAfterDefeat()
        {
            if (!defeatBeatActive)
            {
                return;
            }

            defeatBeatActive = false;

            if (isWired)
            {
                StartRun(CurrentStage);
            }
        }

        /// <summary>Debug/cheat: force the real party-wipe path (rolls back a stage and opens the defeat card).</summary>
        public void DebugForceDefeat()
        {
            if (isWired)
            {
                OnPartyWiped();
            }
        }

        /// <summary>Seeds the run clock the first time a run begins (fresh install or a save with no run record).</summary>
        private void EnsureRunStarted()
        {
            if (runStartBinary <= 0d)
            {
                runStartBinary = GameClock.NowBinary;
            }
        }

        // ------------------------------------------------------------------
        // Combat event handling
        // ------------------------------------------------------------------
        private void SubscribeProgression()
        {
            GameEvents.UpgradePurchased += OnUpgradePurchasedForSave;
            GameEvents.AscensionCompleted += OnAscensionCompletedForSave;
        }

        private void UnsubscribeProgression()
        {
            GameEvents.UpgradePurchased -= OnUpgradePurchasedForSave;
            GameEvents.AscensionCompleted -= OnAscensionCompletedForSave;
        }

        private void OnUpgradePurchasedForSave(int heroIndex, HeroStatType statType, int newLevel, double goldCost)
        {
            Save?.MarkDirty("upgrade");
        }

        private void OnAscensionCompletedForSave(double tokens, int highestStage)
        {
            AscensionCount++;
            Save?.MarkDirty("ascension");
        }

        private void SubscribeCombat()
        {
            combatManager.EnemyKilled += OnEnemyKilled;
            combatManager.WaveCleared += OnWaveCleared;
            combatManager.StageCleared += OnStageCleared;
            combatManager.PartyWiped += OnPartyWiped;
            combatManager.BossFailed += OnBossFailed;
            combatManager.WaveStarted += OnWaveStarted;
        }

        private void UnsubscribeCombat()
        {
            if (combatManager == null)
            {
                return;
            }

            combatManager.EnemyKilled -= OnEnemyKilled;
            combatManager.WaveCleared -= OnWaveCleared;
            combatManager.StageCleared -= OnStageCleared;
            combatManager.PartyWiped -= OnPartyWiped;
            combatManager.BossFailed -= OnBossFailed;
            combatManager.WaveStarted -= OnWaveStarted;
        }

        /// <summary>Applies prestige gold bonuses on top of the simulation's own reward.</summary>
        private void OnEnemyKilled(double goldReward)
        {
            if (Economy == null)
            {
                return;
            }

            double awarded = Rewards != null
                ? Rewards.GrantGold(goldReward, RewardService.Source.Combat)
                : ResolveGoldReward(goldReward);

            Rewards?.RecordKill();

            TotalKills++;
            TotalGoldEarned += awarded;
            RunKills++;
            RunGoldEarned += awarded;
            Save?.MarkDirty("kill");
        }

        private void OnWaveCleared(int stage, int wave)
        {
            if (combatManager.IsBossWave)
            {
                return;
            }

            LogFlow($"Wave {wave} cleared (stage {stage}).");
        }

        /// <summary>
        /// Boss died: advance the stage. Gems are paid only for the first-time clear of a milestone stage, so the
        /// fallback bounce (clear the stage below the ceiling, over and over) can never farm them.
        /// </summary>
        private void OnStageCleared(int stage)
        {
            int nextStage = stage + 1;
            bool isNewBest = nextStage > HighestStageReached;

            CurrentStage = nextStage;

            if (isNewBest)
            {
                HighestStageReached = nextStage;
                GameEvents.RaisePrestigeYieldChanged(PrestigeTokenYield);
            }

            if (nextStage > RunBestStage)
            {
                RunBestStage = nextStage;
            }

            int gems = CombatRewardCalculator.CalculateMilestoneGems(balanceConfig, stage, isNewBest);

            if (gems > 0 && Rewards != null)
            {
                Rewards.GrantGems(gems, RewardService.Source.Combat);
                LogFlow($"Milestone: stage {stage} cleared for the first time -> +{gems} gems.");
            }

            // A gear drop is rolled on EVERY boss clear (boss gems stay first-clear only so they cannot be
            // farmed; drops are the repeatable reward).
            Gear?.TryBossDrop(stage);

            SetState(GameState.Boss, GameState.Combat);
            combatManager.SetStage(CurrentStage, healParty: balanceConfig != null && balanceConfig.HealHeroesOnStageAdvance);
            RaiseStageChanged();

            RunStagesCleared++;
            Save?.MarkDirty("stage");
            LogFlow($"Stage {stage} complete -> now stage {CurrentStage} | {Economy}");
        }

        /// <summary>
        /// Party wipe: fall back one stage and keep fighting. The loop NEVER stops and never waits for input —
        /// the party always ends up on a stage it can beat, so gold keeps flowing while the player pushes their
        /// ceiling. Beating the ceiling moves it up; failing drops back one. That bounce IS the game.
        /// </summary>
        private void OnPartyWiped()
        {
            int rollback = balanceConfig != null ? balanceConfig.StageRollbackOnDefeat : 1;
            int defeatedStage = CurrentStage;

            CurrentStage = Mathf.Max(1, CurrentStage - rollback);

            combatManager.StopRun();
            SetState(GameState.Boss, GameState.Defeat);
            RaiseStageChanged();

            LogFlow($"DEFEAT on stage {defeatedStage}. Falling back to stage {CurrentStage} (wave 1) - resuming automatically.");

            // The full summary modal only lands on a NEW stage, or after the cooldown - a hard wall bounces over and
            // over, so a card on every wipe would spam the player (and slow the idle bounce) for nothing.
            bool showModal = defeatedStage != lastDefeatModalStage ||
                             Time.unscaledTime - lastDefeatModalAt >=
                             (balanceConfig != null ? balanceConfig.DefeatModalCooldownSeconds : 60f);

            if (showModal)
            {
                lastDefeatModalStage = defeatedStage;
                lastDefeatModalAt = Time.unscaledTime;
                GameEvents.RaiseDefeatShown(defeatedStage, CurrentStage);
            }

            defeatBeatRemaining = showModal
                ? (balanceConfig != null ? balanceConfig.DefeatModalSeconds : 10f)
                : (balanceConfig != null ? balanceConfig.DefeatPauseSeconds : 0.75f);
            defeatBeatActive = true;
        }

        /// <summary>
        /// The only per-frame work GameManager does: count down the short defeat beat, then put the party straight
        /// back into the fight. This is not gameplay polling — combat is ticked by <see cref="RunController"/>.
        /// </summary>
        private void Update()
        {
            if (!defeatBeatActive)
            {
                return;
            }

            defeatBeatRemaining -= Time.deltaTime;

            if (defeatBeatRemaining > 0f)
            {
                return;
            }

            ResumeAfterDefeat();
        }

        private void OnBossFailed()
        {
            LogFlow("Boss encounter failed.");
        }

        /// <summary>Reflects the encounter type in the state machine (Combat vs Boss).</summary>
        private void OnWaveStarted(int stage, int wave, bool isBoss)
        {
            SetState(State, isBoss ? GameState.Boss : GameState.Combat);
            RaiseStageChanged();
        }

        /// <summary>
        /// A board change is real progress: write it on the next autosave **and** re-stamp the live fight, so a
        /// swap always takes effect on the next swing (no caller has to remember to call ApplyFormation).
        /// </summary>
        private void OnFormationChanged()
        {
            Save?.MarkDirty("formation");
            combatManager?.Simulator?.ApplyFormation();
        }

        /// <summary>Called by the resolver after any level or multiplier change.</summary>
        private void OnStatsChanged()
        {
            if (combatManager != null && combatManager.Simulator != null)
            {
                combatManager.Simulator.RefreshHeroStats();
            }
        }

        /// <summary>B6: an automation card was bought or its settings changed - tell the save to write it soon.</summary>
        private void OnAutomationChanged()
        {
            Save?.MarkDirty("automation");
        }

        /// <summary>Item landed/equipped/discarded: write it soon and refresh combat + party stats.</summary>
        private void OnGearChanged()
        {
            Save?.MarkDirty("gear");
            Resolver?.NotifyGearChanged();
        }
    }
}
