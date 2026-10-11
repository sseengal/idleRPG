using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Combat;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Equipment;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// Fills what <c>PartyRosterView.Build</c> created: the cards, the big portrait, the stat sheet and the gear
    /// tiles. This is the change-churn half - a new stat or a new gear read edits this file only.
    /// </summary>
    public sealed partial class PartyRosterView : MonoBehaviour
    {
        /// <summary>Refreshes the three equipped-slot tiles for the selected hero.</summary>
        private void RefreshGear()
        {
            if (manager == null || manager.Gear == null || gearSlotBackgrounds == null)
            {
                return;
            }

            for (int i = 0; i < GearSlotCount; i++)
            {
                ItemInstance item = selectedHeroIndex >= 0
                    ? manager.Gear.GetEquipped(selectedHeroIndex, (ItemSlotType)i)
                    : null;

                gearSlotBackgrounds[i].color = item != null ? ItemVisuals.RarityColor(item.Rarity) : slotColor;

                if (gearSlotStatLabels[i] != null)
                {
                    gearSlotStatLabels[i].color = item != null ? Color.white : dimText;
                    gearSlotStatLabels[i].SetText(item != null
                        ? "+" + item.PercentText + " " + item.SlotType.PrimaryStatLabel()
                        : "empty");
                }
            }
        }

        // ------------------------------------------------------------------
        // Refresh
        // ------------------------------------------------------------------
        /// <summary>Binds each card's idle clip once; later refreshes only touch colours and names.</summary>
        private void RefreshCards()
        {
            if (cardAnimators == null)
            {
                return;
            }

            for (int i = 0; i < cardAnimators.Length; i++)
            {
                if (cardNameLabels[i] != null)
                {
                    cardNameLabels[i].color = i == selectedHeroIndex ? new Color(1f, 0.9f, 0.5f) : Color.white;
                }

                if (cardBackgrounds[i] != null)
                {
                    cardBackgrounds[i].color = i == selectedHeroIndex ? cardSelectedColor : cardColor;
                }

                if (cardAnimators[i] == null || cardIcons[i] == null)
                {
                    continue;
                }

                HeroData hero = manager.Party.GetHero(i);

                if (hero == null)
                {
                    cardAnimators[i].Clear();
                    if (cardNameLabels[i] != null)
                    {
                        cardNameLabels[i].SetText("empty");
                    }

                    continue;
                }

                if (cardNameLabels[i] != null)
                {
                    cardNameLabels[i].SetText(hero.HeroName);
                }

                if (!cardAnimators[i].HasArt)
                {
                    if (hero.ArtSet != null)
                    {
                        cardAnimators[i].SetArt(hero.ArtSet);
                    }
                    else if (hero.HeroIcon != null)
                    {
                        cardIcons[i].sprite = hero.HeroIcon;
                    }
                }
            }
        }

        /// <summary>Rebinds the big portrait only when the SELECTED HERO changes - taps must not resize it.</summary>
        private void RefreshBigPortrait()
        {
            if (bigAnimator == null || bigPortrait == null || bigNameLabel == null)
            {
                return;
            }

            if (boundBigHero == selectedHeroIndex)
            {
                return;
            }

            boundBigHero = selectedHeroIndex;

            HeroData hero = selectedHeroIndex >= 0 ? manager.Party.GetHero(selectedHeroIndex) : null;

            if (hero == null)
            {
                bigAnimator.Clear();
                bigNameLabel.SetText(string.Empty);
                return;
            }

            bigNameLabel.SetText(hero.HeroName);

            if (hero.ArtSet != null)
            {
                bigAnimator.SetArt(hero.ArtSet);
            }
            else
            {
                bigAnimator.Clear();
                if (hero.HeroIcon != null)
                {
                    bigPortrait.sprite = hero.HeroIcon;
                }
            }
        }

        private void RefreshStats()
        {
            if (statValues == null || statKeys == null || roleLabel == null)
            {
                return;
            }

            if (selectedHeroIndex < 0 || manager.Party.GetHero(selectedHeroIndex) == null)
            {
                roleLabel.SetText("select a hero");
                ClearStatRows();
                return;
            }

            HeroData hero = manager.Party.GetHero(selectedHeroIndex);
            int index = selectedHeroIndex;

            double health = Resolve(i => manager.Resolver.GetMaxHealth(hero, i), index, hero.BaseHealth);
            double attack = Resolve(i => manager.Resolver.GetAttack(hero, i), index, hero.BaseAttack);
            double defense = Resolve(i => manager.Resolver.GetDefense(hero, i), index, hero.BaseDefense);
            double crit = Resolve(i => manager.Resolver.GetCritChance(hero, i), index, hero.BaseCritChance);
            double critDamage = Resolve(i => manager.Resolver.GetCritDamage(hero, i), index, hero.BaseCritDamage);
            double interval = Resolve(i => manager.Resolver.GetAttackInterval(hero, i), index, hero.AttackIntervalSec);

            // True expected DPS: the resolved swing rate times the crit-weighted hit. The old line used the
            // BASE interval and ignored crit entirely, so it overstated nothing and understated crit builds.
            double critFactor = 1d + crit * Math.Max(0d, critDamage - 1d);
            double dps = (interval > 0.05d ? attack / interval : attack) * critFactor;

            ItemBonuses bonuses = manager.Gear != null
                ? manager.Gear.GetGearBonusFraction(index)
                : default;

            roleLabel.SetText(hero.Role.ToString().ToUpperInvariant());

            for (int i = 0; i < Rows.Length && i < statValues.Length; i++)
            {
                statValues[i].SetText(FormatMetric(Rows[i].Metric, health, attack, defense, crit, critDamage, dps));
                SetGearBonus(Rows[i], i, bonuses);
            }
        }

        /// <summary>How one roster row renders its number (numbers, a percent, or a multiplier).</summary>
        private static string FormatMetric(HeroStatMetric metric, double health, double attack, double defense,
            double crit, double critDamage, double dps)
        {
            switch (metric)
            {
                case HeroStatMetric.Health:
                    return NumberFormatter.Format(health);
                case HeroStatMetric.Attack:
                    return NumberFormatter.Format(attack);
                case HeroStatMetric.Defense:
                    return NumberFormatter.Format(defense);
                case HeroStatMetric.CritChance:
                    return (crit * 100d).ToString("0.#") + "%";
                case HeroStatMetric.CritDamage:
                    return "x" + critDamage.ToString("0.##");
                default:
                    return NumberFormatter.Format(dps);
            }
        }

        /// <summary>Gear shows as "+12%" of base - only meaningful for HP/ATK/DEF, so other rows stay blank.</summary>
        private void SetGearBonus(StatRow row, int rowIndex, ItemBonuses bonuses)
        {
            if (statBonusLabels == null || rowIndex >= statBonusLabels.Length || statBonusLabels[rowIndex] == null)
            {
                return;
            }

            if (!row.ShowGearBonus)
            {
                statBonusLabels[rowIndex].SetText(string.Empty);
                return;
            }

            double fraction = row.Metric == HeroStatMetric.Health ? bonuses.Hp
                : row.Metric == HeroStatMetric.Attack ? bonuses.Atk
                : bonuses.Def;

            if (fraction > 0.0005d)
            {
                statBonusLabels[rowIndex].SetText("+" + (fraction * 100d).ToString("0.#") + "%");
                statBonusLabels[rowIndex].color = bonusColor;
            }
            else
            {
                statBonusLabels[rowIndex].SetText(string.Empty);
            }
        }

        private void ClearStatRows()
        {
            if (statValues != null)
            {
                for (int i = 0; i < statValues.Length; i++)
                {
                    statValues[i].SetText("-");
                }
            }

            if (statBonusLabels != null)
            {
                for (int i = 0; i < statBonusLabels.Length; i++)
                {
                    if (statBonusLabels[i] != null)
                    {
                        statBonusLabels[i].SetText(string.Empty);
                    }
                }
            }
        }

        private static double Resolve(System.Func<int, double> read, int heroIndex, double fallback)
        {
            try
            {
                return read(heroIndex);
            }
            catch (System.Exception)
            {
                return fallback;
            }
        }
    }
}
