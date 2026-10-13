using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using IdleRPG.Data;
using IdleRPG.Progression;

namespace IdleRPG.EditorTools.Content
{
    /// <summary>
    /// Spec string -> enum parsers (target rule, stat type, effect mode/type, currency).
    /// </summary>
    public static partial class ContentGenerator
    {

        /// <summary>"frontmost" | "lowesthealthpercent" | "random" | "backlinefirst" | anything else = inherit.</summary>
        private static EnemyTargetingMode ParseTargetRule(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "frontmost":
                case "front":
                    return EnemyTargetingMode.FrontMost;
                case "lowesthealthpercent":
                    return EnemyTargetingMode.LowestHealthPercent;
                case "random":
                    return EnemyTargetingMode.Random;
                case "backlinefirst":
                    return EnemyTargetingMode.BacklineFirst;
                default:
                    return EnemyTargetingMode.Inherit;
            }
        }

        /// <summary>"shield" | "heal" | "damage" | anything else = damage.</summary>
        private static AbilityKind ParseAbilityKind(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "shield":
                    return AbilityKind.Shield;
                case "heal":
                    return AbilityKind.Heal;
                default:
                    return AbilityKind.Damage;
            }
        }

        /// <summary>Spec spelling for a kind, so the exporter writes back what the parser reads.</summary>
        private static string AbilityKindName(AbilityKind kind)
        {
            switch (kind)
            {
                case AbilityKind.Shield:
                    return "shield";
                case AbilityKind.Heal:
                    return "heal";
                default:
                    return "damage";
            }
        }

        /// <summary>"self" | "party" | "lowestally" | "frontenemy" | "fronthero" | anything else = frontenemy.</summary>
        private static AbilityTarget ParseAbilityTarget(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "self":
                    return AbilityTarget.Self;
                case "party":
                    return AbilityTarget.Party;
                case "lowestally":
                case "lowesthealthally":
                    return AbilityTarget.LowestAlly;
                case "fronthero":
                    return AbilityTarget.FrontHero;
                default:
                    return AbilityTarget.FrontEnemy;
            }
        }

        /// <summary>Spec spelling for a target, so the exporter writes back what the parser reads.</summary>
        private static string AbilityTargetName(AbilityTarget target)
        {
            switch (target)
            {
                case AbilityTarget.Self:
                    return "self";
                case AbilityTarget.Party:
                    return "party";
                case AbilityTarget.LowestAlly:
                    return "lowestally";
                case AbilityTarget.FrontHero:
                    return "fronthero";
                default:
                    return "frontenemy";
            }
        }

        private static HeroStatType ParseStatType(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "health":
                case "healthpercent":
                    return HeroStatType.Health;
                case "defense":
                case "defence":
                case "defensepercent":
                    return HeroStatType.Defense;
                case "crit":
                case "critrate":
                    return HeroStatType.CritRate;
                case "critdamage":
                case "critdmg":
                    return HeroStatType.CritDamage;
                default:
                    return HeroStatType.Attack;
            }
        }

        /// <summary>"multiplicative" | "compounding" = compounding; "flat" | "flatadditive" = flat points; else additive.</summary>
        private static StatEffectMode ParseEffectMode(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "multiplicative":
                case "compounding":
                    return StatEffectMode.Multiplicative;

                case "flat":
                case "flatadditive":
                    return StatEffectMode.FlatAdditive;

                default:
                    return StatEffectMode.AdditiveBase;
            }
        }

        private static PrestigeEffectType ParseEffectType(string value)
        {
            // Accepts both "damage" and the exported "damagepercent" spelling; anything unknown is gold.
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "damage":
                case "damagepercent":
                    return PrestigeEffectType.DamagePercent;
                case "health":
                case "healthpercent":
                    return PrestigeEffectType.HealthPercent;
                default:
                    return PrestigeEffectType.GoldPercent;
            }
        }

        /// <summary>Parses a spec cost currency; anything unknown (or "prestigetokens") is tokens (B7 S4).</summary>
        private static IdleRPG.Economy.CurrencyType ParseCurrency(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "gems":
                case "gem":
                    return IdleRPG.Economy.CurrencyType.Gems;
                case "gold":
                    return IdleRPG.Economy.CurrencyType.Gold;
                default:
                    return IdleRPG.Economy.CurrencyType.PrestigeTokens;
            }
        }
    }
}
