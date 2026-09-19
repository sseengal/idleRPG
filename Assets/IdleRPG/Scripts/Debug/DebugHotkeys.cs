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
    /// 1 = +1 ATK (all heroes)   2 = +10 ATK      3 = +10 HP       4 = +10 DEF
    /// G = +100K gold            T = +10 tokens   B = ad gold boost
    /// A = ascend                R = retry after defeat             S = skip one stage
    /// Tab = cycle pages (Battle / Upgrades / Ascend / Shop)      L = log status
    /// F5 = save now            F9 = delete save            F10 = rewind logout by 3h
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

            if (keyboard.aKey.wasPressedThisFrame)
            {
                bool ascended = gameManager.RequestAscension();
                Log(ascended ? "Ascension requested." : "Ascension refused (stage gate or no tokens).");
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                gameManager.RetryAfterDefeat();
            }

            if (keyboard.sKey.wasPressedThisFrame)
            {
                int nextStage = gameManager.CurrentStage + 1;
                gameManager.StartRun(nextStage);
                Log($"Skipped to stage {nextStage}.");
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

            if (keyboard.f9Key.wasPressedThisFrame)
            {
                gameManager.DeleteSave();
                Log("Save deleted (F9). Progress stays in memory until the next save.");
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
                $"boost={(gameManager.Boost.IsActive ? "ON " + gameManager.Boost.RemainingSeconds + "s" : "off")}");
        }

        private void Log(string message)
        {
            Debug.Log($"[DebugHotkeys] {message}");
        }
    }
}