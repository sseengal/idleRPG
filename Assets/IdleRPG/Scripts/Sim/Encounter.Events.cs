using System;
using IdleRPG.Progression;

namespace IdleRPG.Sim
{
    // ------------------------------------------------------------------
    // The vocabulary of one fight: what the sim announces, in the order it happens.
    //
    //   TargetRule        how a side picks whom to hit
    //   DamageEvent       one blow landed (with indices, so views and the log can attribute it)
    //   SwingStartedEvent an action was announced (a swing, or an ability when ClipSlot is set) - the effect lands
    //                     after that unit's own window
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

        /// <summary>Impact effect id from the ability that caused this damage; empty for a plain swing.</summary>
        public string VfxId;
    }

    /// <summary>
    /// An action was announced (a basic swing, or an ability when <see cref="ClipSlot"/> is set): the attacker commits,
    /// is arming its next action, and the effect will land after <see cref="Combatant.SwingSeconds"/> sim-seconds - up
    /// to and including that unit's own strike frame, or <see cref="SimRules.SwingDurationSec"/> when it has no art.
    /// Damage is NOT applied here - the UI uses this to run the unit in visually; the actual hit (and its
    /// <see cref="DamageEvent"/>) arrives at impact.
    /// </summary>
    public struct SwingStartedEvent
    {
        public CombatantSide AttackerSide;
        public int AttackerIndex;
        public CombatantSide TargetSide;
        public int TargetIndex;

        /// <summary>
        /// Art slot to play instead of the ordinary attack strip ("Ability", "Attack02"). Empty = a basic attack, so
        /// the view has exactly one entry point and needs no ability lookup.
        /// </summary>
        public string ClipSlot;

        /// <summary>True when this action is an ability, not a basic attack (the log and the view may care).</summary>
        public bool IsAbility;
    }

    /// <summary>A blow or an ability travelling from announcement to impact.</summary>
    internal struct PendingSwing
    {
        public Combatant Attacker;
        public Combatant Target;
        public CombatantSide AttackerSide;
        public double Damage;
        public bool IsCritical;
        public double Remaining;

        /// <summary>True when this action is an ability (its effect, not a swing).</summary>
        public bool IsAbility;

        /// <summary>The ability being cast (valid only when <see cref="IsAbility"/> is true).</summary>
        public SimAbility Ability;

        /// <summary>Everyone the ability lands on. One entry for single-target, many for a party effect.</summary>
        public Combatant[] Targets;
    }

    /// <summary>
    /// An ability's impact effect is about to happen on a target. Raised at the announce, with the lead time until the
    /// art reaches the target, so the HUD can start the burst BEFORE the damage number lands.
    /// </summary>
    public struct ImpactVfxEvent
    {
        public CombatantSide Side;
        public int Index;
        public string VfxId;

        /// <summary>Seconds from now until the effect should start (the caster's contact time).</summary>
        public double LeadSeconds;

        /// <summary>Size relative to the shared base (1 = standard).</summary>
        public float Scale;
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

        /// <summary>What the cast does, and where it was aimed (the log line names the target).</summary>
        public SimAbilityKind Kind;

        public SimAbilityTarget Target;

        public CombatantSide TargetSide;

        public int TargetIndex;
    }

    /// <summary>One unit was healed (the ability's Heal kind). Amount is what actually went back into its health.</summary>
    public struct HealEvent
    {
        public CombatantSide Side;
        public int Index;
        public CombatantSide SourceSide;
        public int SourceIndex;
        public string AbilityName;
        public double Amount;
        public double TargetHealth;
        public double TargetMaxHealth;
    }

    /// <summary>One unit raised a guard (the ability's Shield kind): incoming damage is cut for a few seconds.</summary>
    public struct ShieldEvent
    {
        public CombatantSide Side;
        public int Index;
        public CombatantSide SourceSide;
        public int SourceIndex;
        public string AbilityName;
        public double Factor;
        public double Seconds;
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
