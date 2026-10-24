using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Sim;
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

        private void OnHeroDamaged(int heroIndex, double damage, double currentHealth, double maxHealth, int attackerEnemyIndex, bool isCritical, bool isGuarded)
        {
            string attacker = EnemyName(attackerEnemyIndex);
            string target = HeroName(heroIndex);

            // Incoming crits read the same as outgoing ones: "<who> CRITS <whom> for N".
            Append(isCritical
                ? string.Format("{0} CRITS {1} for {2}  ({3}/{4})", attacker, target,
                    NumberFormatter.Format(damage),
                    NumberFormatter.Format(currentHealth),
                    NumberFormatter.Format(maxHealth))
                : string.Format("{0} hits {1} for {2}  ({3}/{4})", attacker, target,
                    NumberFormatter.Format(damage),
                    NumberFormatter.Format(currentHealth),
                    NumberFormatter.Format(maxHealth)),
                isCritical ? criticalColor : incomingColor);
        }

        private void OnEnemyKilled(string enemyName, double goldReward, int enemyIndex)
        {
            Append(string.Format("{0} defeated  +{1} gold", enemyName, NumberFormatter.Format(goldReward)), goldColor);
        }

        /// <summary>
        /// A unit cast its ability: the "cause" line. It is written when the cast is announced, so it always appears
        /// before the impact line that follows - the same order a basic attack reads in.
        /// </summary>
        private void OnAbilityCast(CombatantSide side, int index, string abilityId, string abilityName, string clipSlot, double cooldownSeconds)
        {
            if (side == CombatantSide.Enemy)
            {
                // The enemy move is the thing the player must notice: loud verb, its own colour.
                Append(string.Format("{0} unleashes {1}!", EnemyName(index), abilityName), enemyAbilityColor);
                return;
            }

            string caster = HeroName(index);
            Append(string.Format("{0} casts {1}", caster, abilityName), eventColor);
        }

        private void OnHealed(HealEvent heal)
        {
            string target = heal.Side == CombatantSide.Enemy ? EnemyName(heal.Index) : HeroName(heal.Index);

            Append(string.Format("{0} healed for {1} by {2}  ({3}/{4})", target,
                NumberFormatter.Format(heal.Amount),
                heal.AbilityName,
                NumberFormatter.Format(heal.TargetHealth),
                NumberFormatter.Format(heal.TargetMaxHealth)), healColor);
        }

        private void OnShieldApplied(ShieldEvent shield)
        {
            string target = shield.Side == CombatantSide.Enemy ? EnemyName(shield.Index) : HeroName(shield.Index);

            Append(string.Format("{0} guards with {1}  ({2}% damage for {3}s)", target,
                shield.AbilityName,
                NumberFormatter.Format(shield.Factor * 100d),
                NumberFormatter.Format(shield.Seconds)), guardColor);
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
