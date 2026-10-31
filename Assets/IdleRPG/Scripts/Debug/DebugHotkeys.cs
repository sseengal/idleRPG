using UnityEngine;
using UnityEngine.InputSystem;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Utils;

namespace IdleRPG.Debugging
{
    /// <summary>
    /// Development-only keyboard cheats for testing progression in Play mode without a UI.
    /// Uses the new Input System directly (the project has the legacy handler disabled),
    /// and reads only <c>wasPressedThisFrame</c>, so there is no polling cost concern.
    ///
    /// The printed list lives in <see cref="DebugHotkeyCatalog"/> (the F3 overlay shows it); keep the two in
    /// sync when a key is added or removed here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebugHotkeys : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        [Tooltip("Screen controller used by the Tab key (page cycling). Found automatically when empty.")]
        [SerializeField] private IdleRPG.UI.ScreenController screenController;

        [Tooltip("Gold granted by the G key.")]
        [SerializeField] private double goldGrantAmount = 100000d;

        [Tooltip("Prestige tokens granted by the T key.")]
        [SerializeField] private double tokenGrantAmount = 10d;

        [Tooltip("Gems granted by the C key (gem-sink testing).")]
        [SerializeField] private double gemGrantAmount = 100d;

        public void EditorInitialize(GameManager manager)
        {
            gameManager = manager;
        }

        private void Start()
        {
            if (screenController == null)
            {
                screenController = FindAnyObjectByType<IdleRPG.UI.ScreenController>();
            }

            if (screenController == null)
            {
                Debug.LogWarning("[DebugHotkeys] No ScreenController found; Tab page cycling is disabled.");
            }
        }

        /// <summary>
        /// Moves the run by <paramref name="delta"/> stages, clamped at stage 1. Dev builds only.
        /// Same call as the `S` key and the debug menu, so it tests the real path and adds no new state.
        /// </summary>
        public void StepStage(int delta)
        {
            if (gameManager == null)
            {
                return;
            }

            int stage = Mathf.Max(1, gameManager.CurrentStage + delta);
            gameManager.StartRun(stage);

            Log($"Stage {(delta >= 0 ? "+" : "-")}{Mathf.Abs(delta)} -> {stage} " +
                $"(pool: normal {gameManager.WaveData?.NormalEnemyCount ?? 0}, " +
                $"boss {gameManager.WaveData?.BossEnemyCount ?? 0}).");
        }

        /// <summary>
        /// Moves the run by <paramref name="delta"/> waves inside the current stage, clamped to that stage's waves
        /// (the last one is the boss wave). Dev builds only.
        ///
        /// Why this exists: a boss wave is the ELEVENTH wave of a stage, so stage stepping alone cannot reach one -
        /// you would have to fight ten waves to see a single boss. This jumps straight to it (and back).
        ///
        /// It calls the same <see cref="CombatManager.SetProgress"/> the resume path uses, so it adds no new state.
        /// </summary>
        public void StepWave(int delta)
        {
            if (gameManager == null || gameManager.Combat == null)
            {
                return;
            }

            int normalWaves = gameManager.Balance != null ? gameManager.Balance.NormalWavesPerStage : 10;
            int wavesPerStage = Mathf.Max(1, normalWaves) + 1;
            int wave = Mathf.Clamp(gameManager.Combat.CurrentWave + delta, 1, wavesPerStage);

            gameManager.Combat.SetProgress(gameManager.CurrentStage, wave, healParty: true);

            Log($"Wave {(delta >= 0 ? "+" : "-")}{Mathf.Abs(delta)} -> {wave}/{wavesPerStage} " +
                $"(stage {gameManager.CurrentStage}{(wave >= wavesPerStage ? " - BOSS WAVE" : string.Empty)}).");
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || gameManager == null)
            {
                return;
            }

            if (keyboard.tabKey.wasPressedThisFrame && screenController != null)
            {
                screenController.CyclePage();
                Log("page -> " + (screenController.IsManagementOpen ? "management" : "battle"));
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                BuyAll(HeroStatType.Attack, 1);
            }

            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                BuyAll(HeroStatType.Attack, 10);
            }

            if (keyboard.digit3Key.wasPressedThisFrame)
            {
                BuyAll(HeroStatType.Health, 10);
            }

            if (keyboard.digit4Key.wasPressedThisFrame)
            {
                BuyAll(HeroStatType.Defense, 10);
            }

            if (keyboard.f4Key.wasPressedThisFrame)
            {
                ToggleAllAbilities();
            }

            // Story slice: replay the scene wired on the HUD. No schedule yet, so this is the only way to see
            // the scene while the layer is being built.
            if (keyboard.pKey.wasPressedThisFrame)
            {
                IdleRPG.UI.HudController hud = IdleRPG.UI.HudController.Instance;
                bool played = hud != null && hud.DebugPlayStory();
                Log(played
                    ? "Story (P): playing the debug scene over the live fight."
                    : "Story (P): nothing played - missing scene asset, or a scene is already running.");
            }

            if (keyboard.gKey.wasPressedThisFrame)
            {
                gameManager.Economy.AddGold(goldGrantAmount);
                Log($"Granted {NumberFormatter.Format(goldGrantAmount)} gold.");
            }

            if (keyboard.tKey.wasPressedThisFrame)
            {
                gameManager.Economy.AddTokens(tokenGrantAmount);
                Log($"Granted {NumberFormatter.Format(tokenGrantAmount)} tokens.");
            }

            if (keyboard.bKey.wasPressedThisFrame)
            {
                gameManager.WatchAdForGoldBoost();
            }

            if (keyboard.cKey.wasPressedThisFrame)
            {
                gameManager.Economy.AddGems(gemGrantAmount);
                Log($"Granted {NumberFormatter.Format(gemGrantAmount)} gems (C).");
            }

            if (keyboard.fKey.wasPressedThisFrame)
            {
                // Gem sink #2: same call the shop button makes, so the key tests the real path.
                double goldBefore = gameManager.Economy.Gold;
                bool bought = gameManager.BuyInstantIncome();
                Log(bought
                    ? $"Instant income bought (F): +{NumberFormatter.Format(gameManager.Economy.Gold - goldBefore)} gold " +
                      $"for {gameManager.Shop.InstantIncomeGemCost:0} gems."
                    : "Instant income refused (F): needs gems and a measurable income.");
            }

            if (keyboard.aKey.wasPressedThisFrame)
            {
                bool ascended = gameManager.RequestAscension();
                Log(ascended ? "Ascension requested." : "Ascension refused (stage gate or no tokens).");
            }

            if (keyboard.dKey.wasPressedThisFrame)
            {
                gameManager.DebugForceDefeat();
                Log("Forced a party wipe (D) - the defeat card should open.");
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                gameManager.ResumeAfterDefeat();
                Log("Resume after defeat (R).");
            }

            if (keyboard.sKey.wasPressedThisFrame)
            {
                int nextStage = gameManager.CurrentStage + 1;
                gameManager.StartRun(nextStage);
                Log($"Skipped to stage {nextStage}.");
            }

            // Stage stepper: walking the 100-stage ladder by hand is impossible, so these two keys move the run one
            // stage at a time. `]` forward, `[` back (clamped at stage 1) - the same call `S` and the debug menu use.
            if (keyboard.rightBracketKey.wasPressedThisFrame)
            {
                StepStage(1);
            }

            if (keyboard.leftBracketKey.wasPressedThisFrame)
            {
                StepStage(-1);
            }

            // Wave stepper: `.` forward, `,` back, inside the current stage (the last wave is the boss). Stage
            // stepping cannot reach a boss - it is the eleventh wave - so this is how a boss fight gets tested.
            if (keyboard.periodKey.wasPressedThisFrame)
            {
                StepWave(1);
            }

            if (keyboard.commaKey.wasPressedThisFrame)
            {
                StepWave(-1);
            }

            if (keyboard.mKey.wasPressedThisFrame)
            {
                bool muted = gameManager.Audio.ToggleMuted();
                Log($"SFX {(muted ? "muted" : "unmuted")} (M) at {gameManager.Audio.Describe()}.");
            }

            if (keyboard.lKey.wasPressedThisFrame)
            {
                LogStatus();
            }

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                bool saved = gameManager.SaveNow();
                Log(saved ? "Save written (F5)." : "Save failed (F5).");
            }

            if (keyboard.f7Key.wasPressedThisFrame)
            {
                gameManager.Camp?.DebugOpen();
                Log("Camp (F7): opened for inspection.");
            }

            if (keyboard.f9Key.wasPressedThisFrame)
            {
                gameManager.DeleteSave();
                Log("Save deleted (F9). Progress stays in memory until the next save.");
            }

            if (keyboard.f8Key.wasPressedThisFrame)
            {
                // Fresh install: disk state wiped, PlayerPrefs wiped, scene reloaded (F9 only deletes the file).
                bool reset = gameManager.ResetGame();
                Log(reset ? "Full reset (F8): save + backups + prefs wiped, scene reloading." : "Full reset (F8): disk wiped, reload refused.");
            }

            if (keyboard.f10Key.wasPressedThisFrame)
            {
                // Rewind the logout clock and immediately re-run the offline calculation (3h away).
                double binary = IdleRPG.Core.GameClock.UtcNow.AddHours(-3).ToBinary();
                gameManager.Save?.DebugSetLastLogoutBinary(binary);
                IdleRPG.Save.OfflineRewardResult result = gameManager.EvaluateOffline();
                Log(result.HasReward
                    ? $"Offline (F10, 3h): paid {result.CappedSeconds:0}s -> {result.Gold:0.#} gold at {result.GoldPerSecond:0.##}/s"
                    : "Offline (F10): nothing to claim.");
            }
        }

        private void BuyAll(HeroStatType statType, int levels)
        {
            int purchased = gameManager.Upgrade.TryUpgradeAll(statType, levels);
            double nextCost = gameManager.Upgrade.GetCost(0, statType, levels);
            Log($"{statType.ToDisplayName()} +{levels}: {purchased} hero(es) upgraded | {gameManager.Economy} | " +
                $"next x{levels} costs {NumberFormatter.Format(nextCost)}");
        }

        /// <summary>
        /// DEV: unlocks every hero ability and equips each hero's first one; pressing again locks them all back.
        ///
        /// With no Party screen yet this is how an ability gets into a fight at all, and it goes through the same
        /// service that screen will use. The wave is restarted afterwards, because a fight takes the equipped
        /// abilities when it is built - without that, the key would look like it did nothing.
        /// </summary>
        public void ToggleAllAbilities()
        {
            if (gameManager == null || gameManager.Abilities == null)
            {
                return;
            }

            if (gameManager.Abilities.UnlockedCount > 0)
            {
                gameManager.Abilities.DebugReset();
                Log("Abilities: locked again (nothing equipped). Restarting the wave.");
            }
            else
            {
                var heroes = gameManager.Context != null && gameManager.Context.Party != null
                    ? gameManager.Context.Party.Heroes
                    : null;

                int equipped = gameManager.Abilities.DebugUnlockAll(heroes);
                Log($"Abilities: all unlocked, {equipped} hero(es) equipped. Restarting the wave so the fight picks them up.");
            }

            var combat = gameManager.Context != null ? gameManager.Context.Combat : null;
            if (combat != null)
            {
                combat.SetStage(gameManager.CurrentStage, healParty: true);
            }
        }

        private void LogStatus()
        {
            StatResolverSummary();
        }

        private void StatResolverSummary()
        {
            var resolver = gameManager.Resolver;
            Log($"Stage {gameManager.CurrentStage} (best {gameManager.HighestStageReached}) state={gameManager.State} | " +
                $"{gameManager.Economy} | {gameManager.Ascension.DescribeMultipliers()} | " +
                $"hero levels={resolver.TotalHeroLevels} | token yield={gameManager.PrestigeTokenYield:0} | " +
                $"boost={(gameManager.Boost.IsActive ? "ON " + gameManager.Boost.RemainingSeconds + "s" : "off")} | " +
                $"run: gold={NumberFormatter.Format(gameManager.RunGoldEarned)} kills={gameManager.RunKills} " +
                $"stages={gameManager.RunStagesCleared} secs={gameManager.RunElapsedSeconds:0}");
        }

        private void Log(string message)
        {
            Debug.Log($"[DebugHotkeys] {message}");
        }
    }
}