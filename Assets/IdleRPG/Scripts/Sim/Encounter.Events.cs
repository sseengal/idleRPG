using System;
using IdleRPG.Progression;

namespace IdleRPG.Sim
{
    // ------------------------------------------------------------------
    // The vocabulary of one fight: what the sim announces, in the order it happens.
    //
    //   TargetRule        how a side picks whom to hit
    //   DamageEvent       one blow landed (with indices, so views and the log can attribute it)
    //   SwingStartedEvent an attack was announced - the blow lands after that unit's own swing window
    //   PendingSwing      a blow travelling from announcement to impact (internal)
    //   DeathEvent        a combatant died, and what it paid
    //
    // Moved out of Encounter.cs so the fight's vocabulary is readable on its own; the types and the namespace
    // are unchanged, so nothing that uses them had to move.
    // ------------------------------------------------------------------
    /// <summary>How a side picks its target (data-driven per archetype/ability from Step 11 on).</summary>
    public enum TargetRule
    {
        /// <summary>Closest to the enemy: lowest row first (front row tanks), then left to right.</summary>
        FrontMost = 0,

        LowestHealthPercent = 1,

        Random = 2,

        /// <summary>Reaches over the front row and hits the back row first - the ranged counter (Step 10).</summary>
        BacklineFirst = 3
    }

    /// <summary>One hit, with explicit indices so views and the log can attribute it.</summary>
    public struct DamageEvent
    {
        public int AttackerIndex;
        public CombatantSide AttackerSide;
        public int TargetIndex;
        public CombatantSide TargetSide;
        public double Damage;
        public double TargetHealth;
        public double TargetMaxHealth;
        public bool IsCritical;
    }

    /// <summary>
    /// An attack was announced: the attacker commits, is arming its next swing, and the blow will land after
    /// <see cref="Combatant.SwingSeconds"/> sim-seconds - up to and including that unit's own strike frame, or
    /// <see cref="SimRules.SwingDurationSec"/> when it has no art. Damage is NOT applied here - the UI uses this
    /// to run the unit in visually; the actual hit (and its <see cref="DamageEvent"/>) arrives at impact.
    /// </summary>
    public struct SwingStartedEvent
    {
        public CombatantSide AttackerSide;
        public int AttackerIndex;
        public CombatantSide TargetSide;
        public int TargetIndex;
    }

    /// <summary>A blow travelling from announcement to impact.</summary>
    internal struct PendingSwing
    {
        public Combatant Attacker;
        public Combatant Target;
        public CombatantSide AttackerSide;
        public double Damage;
        public bool IsCritical;
        public double Remaining;
    }

    /// <summary>
    /// A unit cast its ability. This is only "the cast happened": the clock has already restarted, and whatever the
    /// ability DOES resolves separately, so the view can play the clip without waiting for an effect.
    /// </summary>
    public struct AbilityCastEvent
    {
        public CombatantSide Side;
        public int Index;
        public string DisplayName;
        public string AbilityId;
        public string AbilityName;

        /// <summary>Which art slot plays it ("Ability", "Attack02"), so the view needs no lookup to find the frames.</summary>
        public string ClipSlot;

        /// <summary>The cooldown that just started, in seconds - the UI needs it for the clock.</summary>
        public double CooldownSeconds;
    }

    /// <summary>A combatant died.</summary>
    public struct DeathEvent
    {
        public int Index;
        public CombatantSide Side;
        public string DisplayName;
        public double GoldReward;
    }
}
