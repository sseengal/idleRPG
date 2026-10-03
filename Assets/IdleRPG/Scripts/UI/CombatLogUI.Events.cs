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
    /// Game-event handlers: every line the battle log writes. One event, one line.
    /// </summary>
    public sealed partial class CombatLogUI : MonoBehaviour
    {
        private void OnEnemyDamaged(EnemyDamagedInfo info)
        {
            string attacker = HeroName(info.AttackerIndex);
            string target = EnemyName(info.EnemyIndex);

            Append(info.IsCritical
                ? string.Format("{0} CRITS {1} for {2}", attacker, target, NumberFormatter.Format(info.Damage))
                : string.Format("{0} hits {1} for {2}", attacker, target, NumberFormatter.Format(info.Damage)),
                info.IsCritical ? criticalColor : heroHitColor);
        }

        private void OnHeroDamaged(int heroIndex, double damage, double currentHealth, double maxHealth, int attackerEnemyIndex)
        {
            string attacker = EnemyName(attackerEnemyIndex);
            string target = HeroName(heroIndex);

            Append(string.Format("{0} hits {1} for {2}  ({3}/{4})", attacker, target,
                NumberFormatter.Format(damage),
                NumberFormatter.Format(currentHealth),
                NumberFormatter.Format(maxHealth)), incomingColor);
        }

        private void OnEnemyKilled(string enemyName, double goldReward, int enemyIndex)
        {
            Append(string.Format("{0} defeated  +{1} gold", enemyName, NumberFormatter.Format(goldReward)), goldColor);
        }

        private void OnHeroDied(int heroIndex)
        {
            Append(string.Format("{0} has fallen", HeroName(heroIndex)), killColor);
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

        /// <summary>Renders the news channel (wave/clear/wipe/boss/future systems). Never budget-dropped.</summary>
        private void OnCombatMessage(LogMessage message)
        {
            Color colour = eventColor;

            switch (message.Kind)
            {
                case LogMessageKind.Defeat:
                    colour = incomingColor;
                    break;
                case LogMessageKind.Reward:
                    colour = goldColor;
                    break;
            }

            Append(message.Text, colour);
        }

        /// <summary>New session (restart / offline load): clean feed plus a visible divider.</summary>
        private void OnSaveLoaded()
        {
            ClearLines();
            Append("-- session resumed --", eventColor);
        }
    }
}
