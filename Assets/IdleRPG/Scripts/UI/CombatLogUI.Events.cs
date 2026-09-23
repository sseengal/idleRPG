using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// Game-event handlers: every line the battle log writes, and the wave summary that names each enemy.
    /// </summary>
    public sealed partial class CombatLogUI : MonoBehaviour
    {
        private void OnEnemySpawned(string enemyName, double maxHealth, bool isBoss, int enemyIndex)
        {
            // A wave spawns its enemies one event at a time. The log writes ONE line per wave (on the first
            // enemy) so a 3-enemy wave cannot flood the feed, and it always closes a pending aggregate so a
            // line can never merge across a wave boundary.
            CloseAggregate();

            if (enemyIndex != 0)
            {
                return;
            }

            HudController hud = HudController.Instance;
            GameManager manager = hud != null ? hud.GameManager : null;
            int wave = manager != null && manager.Combat != null ? manager.Combat.CurrentWave : 0;

            Append(string.Format("-- Wave {0}: {1}{2} --",
                wave, WaveSummary(), isBoss ? " (BOSS)" : string.Empty), eventColor);
        }

        /// <summary>
        /// Roster of the wave in plain words, duplicates collapsed: "Goblin x2, Slime".
        /// Reads the live wave so it always describes what is actually standing there.
        /// </summary>
        private static string WaveSummary()
        {
            HudController hud = HudController.Instance;
            GameManager manager = hud != null ? hud.GameManager : null;

            if (manager == null || manager.Combat == null || manager.Combat.Simulator == null)
            {
                return "the enemy";
            }

            var enemies = manager.Combat.Simulator.Enemies;

            if (enemies == null || enemies.Length == 0)
            {
                return "the enemy";
            }

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null || !IsFirstOfName(enemies, i))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                int count = CountOfName(enemies, enemies[i].DisplayName);
                builder.Append(count > 1
                    ? string.Format("{0} x{1}", enemies[i].DisplayName, count)
                    : enemies[i].DisplayName);
            }

            return builder.Length > 0 ? builder.ToString() : "the enemy";
        }

        private static bool IsFirstOfName(Sim.Combatant[] enemies, int index)
        {
            for (int j = 0; j < index; j++)
            {
                if (enemies[j] != null && enemies[j].DisplayName == enemies[index].DisplayName)
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountOfName(Sim.Combatant[] enemies, string name)
        {
            int count = 0;

            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null && enemies[i].DisplayName == name)
                {
                    count++;
                }
            }

            return count;
        }

        private void OnEnemyDamaged(EnemyDamagedInfo info)
        {
            string attacker = HeroName(info.AttackerIndex);
            string target = EnemyName(info.EnemyIndex);
            string key = string.Format("enemy:{0}:{1}", info.AttackerIndex, info.IsCritical);

            AddOrAggregate(key, info.Damage, info.IsCritical ? criticalColor : heroHitColor,
                count => info.IsCritical
                    ? string.Format("{0} CRITS {1} for {2}", attacker, target, NumberFormatter.Format(info.Damage))
                    : string.Format("{0} hits {1} for {2}{3}", attacker, target,
                        NumberFormatter.Format(info.Damage), count > 1 ? string.Format(" x{0}", count) : string.Empty));
        }

        private void OnHeroDamaged(int heroIndex, double damage, double currentHealth, double maxHealth, int attackerEnemyIndex)
        {
            // Keyed per (hero, enemy): hits from different enemies must never merge into one line, and the
            // attacker index is what names the enemy that actually swung.
            string attacker = EnemyName(attackerEnemyIndex);
            string target = HeroName(heroIndex);
            string key = string.Format("hero:{0}:{1}", heroIndex, attackerEnemyIndex);

            AddOrAggregate(key, damage, incomingColor,
                count => string.Format("{0} hits {1} for {2}  ({3}/{4}){5}", attacker, target,
                    NumberFormatter.Format(damage),
                    NumberFormatter.Format(currentHealth),
                    NumberFormatter.Format(maxHealth),
                    count > 1 ? string.Format(" x{0}", count) : string.Empty));
        }

        private void OnEnemyKilled(string enemyName, double goldReward, int enemyIndex)
        {
            Append(string.Format("{0} defeated  +{1} gold", enemyName, NumberFormatter.Format(goldReward)), goldColor);
        }

        private void OnHeroDied(int heroIndex)
        {
            Append(string.Format("{0} has fallen", HeroName(heroIndex)), killColor);
        }

        private void OnWaveCompleted(int stage, int wave)
        {
            Append(string.Format("Stage {0} wave {1} cleared", stage, wave), eventColor);
        }

        private void OnPartyWiped()
        {
            Append("Party wiped - falling back a stage", incomingColor);
        }

        private void OnBossFailed()
        {
            Append("Boss encounter failed", incomingColor);
        }

        private void OnAscensionCompleted(double tokensEarned, int newHighestStage)
        {
            Append(string.Format("Ascended - +{0} token(s), best stage {1}",
                NumberFormatter.Format(tokensEarned), newHighestStage), goldColor);
        }

        private void OnUpgradePurchased(int heroIndex, HeroStatType statType, int newLevel, double goldCost)
        {
            Append(string.Format("{0} {1} -> Lv {2}  (-{3} gold)", HeroName(heroIndex),
                statType.ToDisplayName(), newLevel, NumberFormatter.Format(goldCost)), eventColor);
        }

        private void OnOfflineRewardsClaimed(double gold)
        {
            Append(string.Format("Offline earnings claimed  +{0} gold", NumberFormatter.Format(gold)), goldColor);
        }
    }
}
