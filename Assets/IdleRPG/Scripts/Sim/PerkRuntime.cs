using System;
using System.Collections.Generic;

namespace IdleRPG.Sim
{
    /// <summary>
    /// Every boon effect the fight may fire, compiled per party slot.
    ///
    /// ELI5: the party's cards, written as numbers and filed by hero, so the fight can look one up in a
    /// heartbeat instead of asking the game about ScriptableObjects mid-swing.
    ///
    /// Why an array of slots and not hero ids: the sim has no idea what a hero id is. The Unity side knows the
    /// order of the party and files each hero's effects under its slot; the sim only ever asks "slot 1, on hit".
    ///
    /// Immutable on purpose: it is built once when an encounter starts (the same rule as <see cref="SimRules"/>),
    /// so a card taken mid-fight cannot change a fight that has already begun - it lands in the next one.
    /// </summary>
    public sealed class PerkRuntime
    {
        /// <summary>No cards at all. The default, so a fight without boons costs one reference check.</summary>
        public static readonly PerkRuntime Empty = new PerkRuntime(null);

        private readonly PerkEffect[][] byPartySlot;

        public PerkRuntime(PerkEffect[][] byPartySlot)
        {
            this.byPartySlot = byPartySlot;
        }

        /// <summary>Builds one snapshot from per-slot lists (used by the Unity layer and by tests).</summary>
        public static PerkRuntime FromSlots(params IReadOnlyList<PerkEffect>[] slots)
        {
            if (slots == null || slots.Length == 0)
            {
                return Empty;
            }

            PerkEffect[][] compiled = new PerkEffect[slots.Length][];
            bool any = false;

            for (int i = 0; i < slots.Length; i++)
            {
                IReadOnlyList<PerkEffect> list = slots[i];

                if (list == null || list.Count == 0)
                {
                    compiled[i] = Array.Empty<PerkEffect>();
                    continue;
                }

                compiled[i] = new PerkEffect[list.Count];

                for (int j = 0; j < list.Count; j++)
                {
                    compiled[i][j] = list[j];
                }

                any = true;
            }

            return any ? new PerkRuntime(compiled) : Empty;
        }

        public bool IsEmpty => byPartySlot == null;

        /// <summary>The effects filed under one party slot (never null).</summary>
        public PerkEffect[] For(int partySlot)
        {
            if (byPartySlot == null || partySlot < 0 || partySlot >= byPartySlot.Length)
            {
                return Array.Empty<PerkEffect>();
            }

            return byPartySlot[partySlot] ?? Array.Empty<PerkEffect>();
        }

        /// <summary>True when this slot has at least one effect on that trigger (a cheap early-out).</summary>
        public bool Has(int partySlot, PerkTrigger trigger)
        {
            PerkEffect[] effects = For(partySlot);

            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i].Trigger == trigger)
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            if (IsEmpty)
            {
                return "PerkRuntime(empty)";
            }

            int total = 0;

            for (int i = 0; i < byPartySlot.Length; i++)
            {
                total += byPartySlot[i] != null ? byPartySlot[i].Length : 0;
            }

            return $"PerkRuntime({total} effect(s) over {byPartySlot.Length} slot(s))";
        }
    }
}
