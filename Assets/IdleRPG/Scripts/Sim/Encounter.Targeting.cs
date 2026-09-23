using System;
using IdleRPG.Progression;

namespace IdleRPG.Sim
{
    /// <summary>
    /// Target selection: row weights, front-most/back-most/random rules and the living-row helpers.
    /// </summary>
    public sealed partial class Encounter
    {
        /// <summary>
        /// Row-aware, weighted target selection. The front rank is the preferred target; the back rank is picked
        /// with probability <c>weight / (weight + 1)</c> per swing, so standing behind the tank means being hit
        /// *less often* - never for less damage. Inside the chosen rank the hits are spread evenly, so one front
        /// rank shares the beating instead of the first hero soaking all of it.
        ///
        /// Once a rank has no living member the other rank takes every swing (nobody becomes untargetable), which
        /// is why this can never return null while anyone is alive.
        /// </summary>
        private Combatant SelectByRowWeight(Combatant[] candidates)
        {
            bool frontAlive = HasLivingRow(candidates, CombatRow.Front);
            bool backAlive = HasLivingRow(candidates, CombatRow.Back);

            if (!frontAlive && !backAlive)
            {
                return null;
            }

            if (!backAlive)
            {
                return SelectRandomOfRow(candidates, CombatRow.Front);
            }

            if (!frontAlive)
            {
                return SelectRandomOfRow(candidates, CombatRow.Back);
            }

            double weight = context.Rules.BackRowTargetWeight;

            if (weight <= 0d)
            {
                return SelectRandomOfRow(candidates, CombatRow.Front);
            }

            if (weight >= 1d)
            {
                // Equal odds: coin flip between the ranks, then an even pick inside the chosen rank.
                CombatRow pick = context.Rng.NextDouble() < 0.5d ? CombatRow.Front : CombatRow.Back;
                return SelectRandomOfRow(candidates, pick);
            }

            double backChance = weight / (weight + 1d);
            CombatRow chosen = context.Rng.NextDouble() < backChance ? CombatRow.Back : CombatRow.Front;
            return SelectRandomOfRow(candidates, chosen);
        }

        private static bool HasLivingRow(Combatant[] combatants, CombatRow row)
        {
            if (combatants == null)
            {
                return false;
            }

            for (int i = 0; i < combatants.Length; i++)
            {
                Combatant candidate = combatants[i];

                if (candidate != null && candidate.IsAlive && candidate.Row == row)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Picks a target for a swing (deterministic; uses the sim RNG for the Random rule).</summary>
        private Combatant SelectTarget(Combatant[] candidates, TargetRule rule)
        {
            if (candidates == null || candidates.Length == 0)
            {
                return null;
            }

            if (rule == TargetRule.FrontMost)
            {
                return SelectByRowWeight(candidates);
            }

            if (rule == TargetRule.BacklineFirst)
            {
                return SelectRandomOfRow(candidates, CombatRow.Back) ?? SelectRandomOfRow(candidates, CombatRow.Front);
            }

            Combatant best = null;

            for (int i = 0; i < candidates.Length; i++)
            {
                Combatant candidate = candidates[i];
                if (candidate == null || !candidate.IsAlive)
                {
                    continue;
                }

                if (best == null)
                {
                    best = candidate;
                    continue;
                }

                if (rule == TargetRule.LowestHealthPercent && candidate.HealthPercent < best.HealthPercent)
                {
                    best = candidate;
                }
            }

            if (best == null || rule != TargetRule.Random)
            {
                return best;
            }

            int alive = CountAlive(candidates);
            if (alive <= 1)
            {
                return best;
            }

            int pick = context.Rng.Next(alive);
            int seen = 0;

            for (int i = 0; i < candidates.Length; i++)
            {
                Combatant candidate = candidates[i];
                if (candidate == null || !candidate.IsAlive)
                {
                    continue;
                }

                if (seen == pick)
                {
                    return candidate;
                }

                seen++;
            }

            return best;
        }

        /// <summary>
        /// Picks a living member of a rank, **evenly at random** among them: the whole front line shares the
        /// incoming hits instead of the first hero soaking every swing. Uses the sim RNG, so live, offline and
        /// fast-forward all produce the same sequence.
        ///
        /// A rank with a single living member draws nothing (no RNG consumed), which keeps a one-hero board
        /// byte-identical to before.
        /// </summary>
        private Combatant SelectRandomOfRow(Combatant[] candidates, CombatRow row)
        {
            if (candidates == null)
            {
                return null;
            }

            int alive = 0;

            for (int i = 0; i < candidates.Length; i++)
            {
                Combatant candidate = candidates[i];

                if (candidate != null && candidate.IsAlive && candidate.Row == row)
                {
                    alive++;
                }
            }

            if (alive <= 0)
            {
                return null;
            }

            int pick = alive == 1 ? 0 : context.Rng.Next(alive);
            int seen = 0;

            for (int i = 0; i < candidates.Length; i++)
            {
                Combatant candidate = candidates[i];

                if (candidate == null || !candidate.IsAlive || candidate.Row != row)
                {
                    continue;
                }

                if (seen == pick)
                {
                    return candidate;
                }

                seen++;
            }

            return null;
        }
    }
}
