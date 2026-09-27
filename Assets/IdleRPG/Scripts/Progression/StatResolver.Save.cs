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
                        // Schema v5 (B5 session 2): one keyed list is the source of truth.
                        SetHeroLevel(hero, HeroStatType.Attack, data.GetLevel(SaveData.SaveKeys.HeroStat(hero.HeroID, HeroStatType.Attack)));
                        SetHeroLevel(hero, HeroStatType.Health, data.GetLevel(SaveData.SaveKeys.HeroStat(hero.HeroID, HeroStatType.Health)));
                        SetHeroLevel(hero, HeroStatType.Defense, data.GetLevel(SaveData.SaveKeys.HeroStat(hero.HeroID, HeroStatType.Defense)));
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

                    data.SetLevel(SaveData.SaveKeys.HeroStat(hero.HeroID, HeroStatType.Attack), GetHeroLevel(hero, HeroStatType.Attack));
                    data.SetLevel(SaveData.SaveKeys.HeroStat(hero.HeroID, HeroStatType.Health), GetHeroLevel(hero, HeroStatType.Health));
                    data.SetLevel(SaveData.SaveKeys.HeroStat(hero.HeroID, HeroStatType.Defense), GetHeroLevel(hero, HeroStatType.Defense));
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
