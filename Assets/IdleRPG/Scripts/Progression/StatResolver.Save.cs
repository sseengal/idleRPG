using System;
using System.Collections.Generic;
using UnityEngine;
using IdleRPG.Data;
using IdleRPG.Save;

namespace IdleRPG.Progression
{
    /// <summary>
    /// Save mapping: levels in and out of the save contract.
    /// </summary>
    public sealed partial class StatResolver : ICombatStatProvider
    {
        /// <summary>
        /// Every upgradeable hero stat, in a stable order. The save round-trip walks this list, so adding a stat
        /// track needs no edit here (save keys are name-based: "knight.critrate").
        /// </summary>
        private static readonly HeroStatType[] HeroStatTypes =
        {
            HeroStatType.Attack,
            HeroStatType.Health,
            HeroStatType.Defense,
            HeroStatType.CritRate,
            HeroStatType.CritDamage
        };


        // ------------------------------------------------------------------
        // Save integration (Step 5 wires the file I/O; nothing here needs rework then)
        // ------------------------------------------------------------------
        public void FillFromSave(SaveData data, PartyConfig party)
        {
            if (data == null)
            {
                return;
            }

            heroLevels.Clear();

            if (party != null)
            {
                for (int i = 0; i < party.Heroes.Count; i++)
                {
                    HeroData hero = party.GetHero(i);
                    if (hero == null)
                    {
                        continue;
                    }

                    HeroProgressRecord record = data.GetOrCreateHero(hero.HeroID);

                    if (data.levels != null && data.levels.Count > 0)
                    {
                        // Schema v5 (B5 session 2): one keyed list is the source of truth. Every HeroStatType is
                        // read generically so a new stat track needs no edit here (keys are name-based).
                        foreach (HeroStatType statType in HeroStatTypes)
                        {
                            SetHeroLevel(hero, statType, data.GetLevel(SaveData.SaveKeys.HeroStat(hero.HeroID, statType)));
                        }
                    }
                    else
                    {
                        // Legacy path (a fresh v5 file always has the keyed list; this branch only covers a hand-made
                        // v5 payload that never populated it, so the fixed hero columns are still honoured).
                        SetHeroLevel(hero, HeroStatType.Attack, record.attackLevel);
                        SetHeroLevel(hero, HeroStatType.Health, record.healthLevel);
                        SetHeroLevel(hero, HeroStatType.Defense, record.defenseLevel);
                    }
                }
            }

            prestigeLevels.Clear();

            for (int i = 0; i < prestigeUpgrades.Count; i++)
            {
                PrestigeUpgradeData upgrade = prestigeUpgrades[i];
                if (upgrade != null)
                {
                    prestigeLevels[upgrade.UpgradeID] = data.GetLevel(SaveData.SaveKeys.Track(upgrade.UpgradeID));
                }
            }

            StatsChanged?.Invoke();
        }

        public void WriteToSave(SaveData data, PartyConfig party)
        {
            if (data == null)
            {
                return;
            }

            if (party != null)
            {
                for (int i = 0; i < party.Heroes.Count; i++)
                {
                    HeroData hero = party.GetHero(i);
                    if (hero == null)
                    {
                        continue;
                    }

                    foreach (HeroStatType statType in HeroStatTypes)
                    {
                        data.SetLevel(SaveData.SaveKeys.HeroStat(hero.HeroID, statType), GetHeroLevel(hero, statType));
                    }
                }
            }

            for (int i = 0; i < prestigeUpgrades.Count; i++)
            {
                PrestigeUpgradeData upgrade = prestigeUpgrades[i];
                if (upgrade == null)
                {
                    continue;
                }

                data.SetLevel(SaveData.SaveKeys.Track(upgrade.UpgradeID), GetPrestigeLevel(upgrade));
            }
        }
    }
}
