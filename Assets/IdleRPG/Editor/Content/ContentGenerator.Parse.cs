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
                default:
                    return HeroStatType.Attack;
            }
        }

        /// <summary>"multiplicative" | "compounding" | anything else = additive (the shipped model).</summary>
        private static StatEffectMode ParseEffectMode(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "multiplicative":
                case "compounding":
                    return StatEffectMode.Multiplicative;
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
