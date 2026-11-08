using System.Collections.Generic;
using IdleRPG.Data;
using IdleRPG.Sim;

namespace IdleRPG.Progression
{
    /// <summary>
    /// Compiles the run's hand into the fight's own vocabulary.
    ///
    /// ELI5: turns the cards the player holds into the numbers the fight can act on, filed by hero. It is the only
    /// place that knows both worlds - the cards (a ScriptableObject on the Unity side) and the fight (plain data).
    ///
    /// Why it runs on every change: a card taken between waves must be live in the next swing, and this is cheap -
    /// a handful of small structs per pick, not per frame.
    ///
    /// Stat cards do not come through here: they are folded in by <see cref="StatResolver"/>, which is why this
    /// factory returns only mechanics.
    /// </summary>
    public static class BoonRuntimeFactory
    {
        public static PerkRuntime Build(PerkLoadout loadout, PartyConfig party)
        {
            if (loadout == null || party == null || party.Heroes == null || party.Heroes.Count == 0)
            {
                return PerkRuntime.Empty;
            }

            List<PerkEffect>[] slots = new List<PerkEffect>[party.Heroes.Count];

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = null;
            }

            IReadOnlyList<PerkData> chosen = loadout.Chosen;

            for (int i = 0; i < chosen.Count; i++)
            {
                PerkData boon = chosen[i];

                if (boon == null || boon.Effects == null)
                {
                    continue;
                }

                for (int effectIndex = 0; effectIndex < boon.Effects.Count; effectIndex++)
                {
                    PerkEffectSpec spec = boon.Effects[effectIndex];

                    if (spec == null || !spec.IsTrigger)
                    {
                        continue;
                    }

                    PerkEffect effect = new PerkEffect(
                        ToSimTrigger(spec.Trigger),
                        ToSimOp(spec.Operation),
                        spec.Amount,
                        spec.Chance);

                    for (int slot = 0; slot < slots.Length; slot++)
                    {
                        HeroData hero = party.Heroes[slot];

                        if (hero == null || !boon.AppliesTo(hero.HeroID))
                        {
                            continue;
                        }

                        slots[slot] = slots[slot] ?? new List<PerkEffect>();
                        slots[slot].Add(effect);
                    }
                }
            }

            return PerkRuntime.FromSlots(slots);
        }

        /// <summary>Content word -> fight word. One switch, so a new trigger is added in exactly two places.</summary>
        private static PerkTrigger ToSimTrigger(PerkTriggerKind kind)
        {
            switch (kind)
            {
                case PerkTriggerKind.OnKill: return PerkTrigger.OnKill;
                case PerkTriggerKind.OnHurt: return PerkTrigger.OnHurt;
                case PerkTriggerKind.OnWaveStart: return PerkTrigger.OnWaveStart;
                default: return PerkTrigger.OnHit;
            }
        }

        private static PerkEffectOp ToSimOp(PerkEffectSpec.Op op)
        {
            switch (op)
            {
                case PerkEffectSpec.Op.ExtraStrike: return PerkEffectOp.ExtraHit;
                case PerkEffectSpec.Op.ReflectDamage: return PerkEffectOp.Reflect;
                case PerkEffectSpec.Op.Lifesteal: return PerkEffectOp.Lifesteal;
                case PerkEffectSpec.Op.HealSelf: return PerkEffectOp.HealSelf;
                case PerkEffectSpec.Op.ShieldSelf: return PerkEffectOp.ShieldSelf;
                default: return PerkEffectOp.DamageOthers;
            }
        }
    }
}
