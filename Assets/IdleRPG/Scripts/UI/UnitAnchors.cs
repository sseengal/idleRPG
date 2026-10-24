using System.Collections.Generic;
using UnityEngine;
using IdleRPG.Sim;

namespace IdleRPG.UI
{
    /// <summary>
    /// Where a unit currently is, for anything that has to appear on it: damage numbers, GUARD words, impact bursts.
    ///
    /// ELI5: the damage text used to be pinned to the seat a hero started in. When the Knight dashes in front of an
    /// ally, the seat is empty and the number shows up in the wrong place. This is the "where is he now?" desk: each
    /// unit view registers itself, and anything that needs a position asks here instead of remembering a seat.
    ///
    /// It scales: any future move (dash, knockback, swap) works for free, because the view is the source of truth.
    /// </summary>
    public static class UnitAnchors
    {
        private static readonly Dictionary<int, RectTransform> Heroes = new Dictionary<int, RectTransform>();
        private static readonly Dictionary<int, RectTransform> Enemies = new Dictionary<int, RectTransform>();

        public static void Register(CombatantSide side, int index, RectTransform rect)
        {
            if (rect == null || index < 0)
            {
                return;
            }

            Dictionary<int, RectTransform> table = side == CombatantSide.Party ? Heroes : Enemies;
            table[index] = rect;
        }

        public static void Unregister(CombatantSide side, int index, RectTransform rect)
        {
            Dictionary<int, RectTransform> table = side == CombatantSide.Party ? Heroes : Enemies;

            if (table.TryGetValue(index, out RectTransform current) && current == rect)
            {
                table.Remove(index);
            }
        }

        /// <summary>The live rect of one unit, or null when it is not on screen right now.</summary>
        public static RectTransform Get(CombatantSide side, int index)
        {
            if (index < 0)
            {
                return null;
            }

            Dictionary<int, RectTransform> table = side == CombatantSide.Party ? Heroes : Enemies;

            if (table.TryGetValue(index, out RectTransform rect) && rect != null)
            {
                return rect;
            }

            return null;
        }

        /// <summary>Drops every entry (scene load, run reset).</summary>
        public static void Clear()
        {
            Heroes.Clear();
            Enemies.Clear();
        }
    }
}
