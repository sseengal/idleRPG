using System.Collections.Generic;
using System.Reflection;
using IdleRPG.Data;
using IdleRPG.Equipment;
using NUnit.Framework;
using UnityEngine;

namespace IdleRPG.Tests
{
    /// <summary>Headless fixtures for the pure-logic suites: scriptable data made in memory (no assets),
    /// an ItemService with a null economy (the service is null-safe there) and reflection for the few
    /// serialized fields tests need to pin. Invariant-based tests only, so no RNG seeding is required.</summary>
    internal static class TestGearFixture
    {
        public static BalanceConfig MakeBalance(float dropChance = 1f, int cap = 5)
        {
            BalanceConfig balance = ScriptableObject.CreateInstance<BalanceConfig>();
            Set(balance, "bossGearDropChance", dropChance);
            Set(balance, "inventoryCap", cap);
            return balance;
        }

        public static PartyConfig MakeParty(params HeroRole[] roles)
        {
            PartyConfig party = ScriptableObject.CreateInstance<PartyConfig>();
            List<HeroData> heroes = new List<HeroData>();

            for (int i = 0; i < roles.Length; i++)
            {
                HeroData hero = ScriptableObject.CreateInstance<HeroData>();
                Set(hero, "role", roles[i]);
                Set(hero, "heroName", "Hero" + i);
                heroes.Add(hero);
            }

            Set(party, "heroes", heroes);
            return party;
        }

        public static ItemService MakeService(BalanceConfig balance, PartyConfig party, bool autoEquip, bool autoSalvage)
        {
            // Economy is null on purpose: ItemService never dereferences it (verified in Salvage).
            return new ItemService(balance, party, null,
                effect => effect == PrestigeEffectType.AutoEquipGear
                    ? (autoEquip ? 1 : 0)
                    : effect == PrestigeEffectType.AutoSalvageGear
                        ? (autoSalvage ? 1 : 0)
                        : 0);
        }

        /// <summary>Counts every owned instance - worn gear (deduped) + the bag.</summary>
        public static int CountAll(ItemService service)
        {
            return service.AllInstances().Count;
        }

        private static void Set(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "field " + fieldName + " on " + target.GetType().Name);
            field.SetValue(target, value);
        }
    }
}
