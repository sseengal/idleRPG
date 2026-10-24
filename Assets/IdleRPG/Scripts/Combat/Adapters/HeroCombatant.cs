using IdleRPG.Data;
using IdleRPG.Sim;

namespace IdleRPG.Combat
{
    /// <summary>
    /// A party member: the shared <see cref="Combatant"/> sheet plus the art/data reference.
    ///
    /// ELI5: all the fighting rules live in the shared sheet (`Combatant`); this class only adds the
    /// "which hero is it" identity that the UI needs for its icon and name.
    /// </summary>
    public sealed class HeroCombatant : Combatant
    {
        /// <summary>Static definition (icon, id, name).</summary>
        public readonly HeroData Data;

        public HeroCombatant(int index, HeroData data, double maxHealth, double attack, double defense, double attackIntervalSec)
            : base(
                data != null ? data.HeroID : "hero" + index,
                data != null ? data.HeroName : "Hero " + index,
                CombatantSide.Party,
                new StatBlock(ClampHealth(maxHealth), ClampStat(attack), ClampStat(defense)),
                attackIntervalSec,
                HeroData.MinAttackIntervalSec)
        {
            Data = data;
            SlotIndex = index;

            // The art owns the swing length, so the blow lands on this hero's own strike frame. No art = 0,
            // which means "use the rules' window" (a placeholder).
            SwingSeconds = data != null && data.ArtSet != null ? data.ArtSet.SwingSeconds : 0d;
        }

        /// <summary>Lane index (legacy alias for <see cref="Combatant.SlotIndex"/>).</summary>
        public int Index => SlotIndex;

        /// <summary>
        /// Re-applies derived stats (hero levels / prestige upgrades) from raw values.
        /// Kept for existing callers; the stat-block overload lives on the base class.
        /// </summary>
        public void ApplyStats(double maxHealth, double attack, double defense, double attackIntervalSec)
        {
            ApplyStats(new StatBlock(ClampHealth(maxHealth), ClampStat(attack), ClampStat(defense)), attackIntervalSec);
        }

        private static double ClampHealth(double value)
        {
            return value < 1d ? 1d : value;
        }

        private static double ClampStat(double value)
        {
            return value < 0d ? 0d : value;
        }
    }
}
