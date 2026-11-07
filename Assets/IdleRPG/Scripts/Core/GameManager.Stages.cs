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
                GameEvents.RaiseToast($"Reach stage {balanceConfig.MinStageToAscend} to end the run.");
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
            LogFlow($"Ended run for {tokens:0} Perk Point(s) from stage {endedStage} | " +
                    $"{Ascension.DescribeMultipliers()} | {Economy}");

            ResetProgressForAscension(HighestStageReached);

            GameEvents.RaiseToast(string.Format("Run ended - stage {0}  ·  +{1:0} tokens  ·  {2} gold",
                endedStage, tokens, NumberFormatter.Format(endedGold)));
            return true;
        }

        /// <summary>True while this run still has its one rewarded-ad revive available.</summary>
        public bool CanRevive => !reviveUsedThisRun;

        private bool reviveUsedThisRun;

        /// <summary>What ending the run right now would bank (same value the run-over card / PERKS page shows).</summary>
        public double RunEndPreviewTokens
        {
            get
            {
                int depth = Mathf.Max(RunBestStage, CurrentStage);
                return RunStagesCleared >= 1 ? Math.Max(1d, UngatedYield(depth)) : 0d;
            }
        }

        /// <summary>
        /// Ends the CURRENT run on purpose: grant the meta (prestige tokens) for the depth reached, wipe gold and hero
        /// levels, then start a fresh run at stage 1. This is the pivot's "run over" path - the same reset ascension
        /// uses, but WITHOUT the manual stage gate, so death can call it. Returns false only when not wired.
        ///
        /// ELI5: cash the run in, keep a little forever, go again. A very short run (below the gate) still ends
        /// cleanly and simply pays nothing.
        /// </summary>
        public bool EndRun()
        {
            if (!isWired)
            {
                return false;
            }

            defeatBeatActive = false;

            int endedStage = RunBestStage;
            double endedGold = RunGoldEarned;

            combatManager.StopRun();
            SetState(State, GameState.Ascension);

            double tokens = BankRunReward();

            LogFlow($"Run ended at stage {endedStage} -> +{tokens:0} Perk Points | {Ascension.DescribeMultipliers()} | {Economy}");

            GameEvents.RaiseToast(tokens > 0d
                ? string.Format("Run ended - stage {0}  ·  +{1:0} Perk Points  ·  {2} gold", endedStage, tokens, NumberFormatter.Format(endedGold))
                : string.Format("Run ended - stage {0}  ·  {1} gold", endedStage, NumberFormatter.Format(endedGold)));

            reviveUsedThisRun = false;
            StartRun(1);
            return true;
        }

        /// <summary>
        /// Party wipe under the pivot: the run ENDS. Bank the reward, show the run-over card, then WAIT for the player
        /// (NEXT RUN). No bounce, no auto-resume.
        /// </summary>
        private void EndRunOnWipe()
        {
            defeatBeatActive = false;

            combatManager.StopRun();
            SetState(State, GameState.Defeat);
            RaiseStageChanged();

            // Do NOT bank yet: the card offers REVIVE (ad) or NEXT RUN (which banks + restarts). Banking only when
            // the run truly ends keeps the revive from ever double-paying.
            GameEvents.RaiseDefeatShown(CurrentStage, CurrentStage);
            LogFlow($"RUN OVER on stage {CurrentStage} (REVIVE or NEXT RUN).");
        }

        /// <summary>NEXT RUN on the run-over card: bank the reward, then start a fresh run from stage 1.</summary>
        public void ConfirmEndRun()
        {
            EndRun();
        }

        /// <summary>Run-over card REVIVE (rewarded ad): resume the SAME stage and mark the revive used for this run.</summary>
        private void ReviveRun()
        {
            reviveUsedThisRun = true;
            StartRun(CurrentStage);
            LogFlow($"REVIVED on stage {CurrentStage} (ad).");
        }

        /// <summary>
        /// The pivot reward: a run that cleared at least one stage banks at least one Perk Point, scaled by the deepest
        /// stage reached. Grants, resets hero levels (gold is PERMANENT now), resets the run counters, and returns it.
        /// </summary>
        private double BankRunReward()
        {
            int depth = Mathf.Max(RunBestStage, CurrentStage);
            double tokens = RunStagesCleared >= 1 ? Math.Max(1d, UngatedYield(depth)) : 0d;

            if (tokens > 0d)
            {
                Economy.AddTokens(tokens);
            }

            // Gold is PERMANENT now (spent on Training) - a run end no longer wipes it.
            if (balanceConfig == null || balanceConfig.ResetHeroLevelsOnAscension)
            {
                Resolver.ResetHeroLevels();
            }

            if (tokens > 0d)
            {
                GameEvents.RaiseAscensionCompleted(tokens, depth);
            }

            CurrentStage = 1;
            RunBestStage = 1;
            RunGoldEarned = 0d;
            RunKills = 0;
            RunStagesCleared = 0;
            runStartBinary = GameClock.NowBinary;
            ResetRunCampState();
            combatManager.SetStage(1, healParty: true);

            return tokens;
        }

        /// <summary>Token curve without the manual-ascend stage gate (the "always pay something" pivot reward).</summary>
        private double UngatedYield(int depth)
        {
            if (balanceConfig == null)
            {
                return 0d;
            }

            return FormulaUtility.PrestigeTokenReward(depth, balanceConfig.PrestigeStageDivisor, balanceConfig.PrestigeExponent);
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
            ResetRunCampState();

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
            int nextStage = StageRules.NextStage(stage);
            bool isNewBest = StageRules.IsNewBest(nextStage, HighestStageReached);

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

            // Training "Finish Gold %": a small bonus on every cleared stage (placeholder formula; tune later).
            double finishPct = Training != null ? Training.TotalFor(TrainingEffect.FinishGoldPercent) : 0d;
            if (finishPct > 0d && Rewards != null)
            {
                Rewards.GrantGold((10d + 10d * stage) * finishPct, RewardService.Source.Combat);
            }

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
            if (balanceConfig != null && balanceConfig.RunEndsOnDeath)
            {
                EndRunOnWipe();
                return;
            }

            int rollback = balanceConfig != null ? balanceConfig.StageRollbackOnDefeat : 1;
            int defeatedStage = CurrentStage;

            CurrentStage = StageRules.RollbackStage(CurrentStage, rollback);

            // The stage is replayed: its camps must be offered again. The token and training are kept.
            ResetCampCheckpoint();

            combatManager.StopRun();
            SetState(GameState.Boss, GameState.Defeat);
            RaiseStageChanged();

            LogFlow($"DEFEAT on stage {defeatedStage}. Falling back to stage {CurrentStage} (wave 1) - resuming automatically.");

            // The full summary modal only lands on a NEW stage, or after the cooldown - a hard wall bounces over and
            // over, so a card on every wipe would spam the player (and slow the idle bounce) for nothing.
            bool showModal = StageRules.ShouldShowDefeatModal(
                defeatedStage,
                lastDefeatModalStage,
                Time.unscaledTime,
                lastDefeatModalAt,
                balanceConfig != null ? balanceConfig.DefeatModalCooldownSeconds : 60f);

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
            // Story bind guard: releases a held wave if the HUD never appears. No-op otherwise.
            Story?.Tick(Time.unscaledDeltaTime);

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
