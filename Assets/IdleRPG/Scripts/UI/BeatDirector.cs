using System.Collections.Generic;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Sim;

namespace IdleRPG.UI
{
    /// <summary>
    /// The ONE planner of a fight beat. It is the only subscriber to <see cref="GameEvents.SwingStarted"/> in the
    /// moving-picture layer, so no two views can disagree about where anybody stands.
    ///
    /// ELI5: one choreographer. When the fight says "this goblin is about to hit somebody", the choreographer decides
    /// the whole picture in one pass: who steps in front, where the blow lands, where the attacker stands. Nobody else
    /// moves a unit. Before this, each unit worked out its own spot from the same event, and the enemy aimed at a seat
    /// the guard had already left.
    ///
    /// The order is the whole trick: a cover is planned FIRST, so the attacker is told where the guard WILL be.
    /// </summary>
    public sealed class BeatDirector : MonoBehaviour
    {
        /// <summary>Extra spots reserved for the next beat (see <see cref="Interject"/>).</summary>
        private readonly Dictionary<int, Vector3> reservations = new Dictionary<int, Vector3>();

        /// <summary>
        /// Interjection hook: park a unit at a chosen spot for the NEXT beat. An ability that needs to place somebody
        /// before the blow lands (a taunt, a knockback, a forced swap) calls this and the planner respects it - no
        /// special case is added anywhere else. It applies to one beat only.
        /// </summary>
        public void Interject(CombatantSide side, int index, Vector3 worldSpot)
        {
            if (index < 0)
            {
                return;
            }

            reservations[Key(side, index)] = worldSpot;
        }

        private void OnEnable()
        {
            GameEvents.SwingStarted += OnSwingStarted;
        }

        private void OnDisable()
        {
            GameEvents.SwingStarted -= OnSwingStarted;
        }

        private void OnSwingStarted(int attackerIndex, CombatantSide attackerSide, int targetIndex,
            CombatantSide targetSide, string clipSlot, int coveredAllyIndex, bool stayPlanted)
        {
            HudController hud = HudController.Instance;

            if (hud == null)
            {
                return;
            }

            RectTransform attackerBody = UnitAnchors.Get(attackerSide, attackerIndex);

            if (attackerBody == null)
            {
                reservations.Clear();
                return;
            }

            // Play the clip on the announce, exactly as before: the strike frame still lands on the hit.
            PlayClip(attackerSide, attackerIndex, clipSlot);

            // A support act never travels. Guard (self), War Cry (party), enemy Block (self): the target is on the
            // caster's own side, so there is no distance to close. A ranged attack says so through stayPlanted.
            // (The old view had this check; losing it is why the knight hopped when GUARD UP appeared.)
            if (stayPlanted || targetSide == attackerSide)
            {
                reservations.Clear();
                return;
            }

            // The landing point: a covered hit lands on the GUARD, at a spot he will really occupy.
            if (!TryPlanLanding(targetSide, targetIndex, coveredAllyIndex, attackerBody.position,
                    out Vector3 targetSpot, out float targetWidth))
            {
                reservations.Clear();
                return;
            }

            // The attacker: a reserved spot wins, else its own near flank.
            if (!reservations.TryGetValue(Key(attackerSide, attackerIndex), out Vector3 attackerSpot))
            {
                float side = BattleSpots.AttackerSide(attackerBody.position, targetSpot);
                attackerSpot = BattleSpots.Beside(targetSpot, WidthOf(attackerSide, attackerIndex), targetWidth, side);
            }

            RunTo(attackerSide, attackerIndex, attackerSpot, cover: false);
            reservations.Clear();
        }

        /// <summary>
        /// Works out where the blow lands. Without a cover that is simply the target's body. With one, it is the guard:
        /// either the spot he is being sent to now, or - if he is already out on his own attack - the spot he really
        /// occupies. A plan may never contain a position no unit will stand on.
        /// </summary>
        private static bool TryPlanLanding(CombatantSide targetSide, int targetIndex, int coveredAllyIndex,
            Vector3 attackerSpot, out Vector3 spot, out float width)
        {
            // No cover: the blow lands on the target's own body.
            if (coveredAllyIndex < 0)
            {
                RectTransform body = UnitAnchors.Get(targetSide, targetIndex);

                if (body == null)
                {
                    // No view on screen (dead, not spawned): the sim still lands its damage. Nothing to stage.
                    spot = attackerSpot;
                    width = 0f;
                    return false;
                }

                spot = body.position;
                width = WidthOf(targetSide, targetIndex);
                return true;
            }

            // Covered: the event's TARGET is the guard, CoveredAllyIndex is the ally he saved.
            RectTransform guardBody = UnitAnchors.Get(CombatantSide.Party, targetIndex);
            RectTransform allyBody = UnitAnchors.Get(CombatantSide.Party, coveredAllyIndex);
            HeroUnitView guard = HudController.Instance?.FormationBoard?.GetHeroView(targetIndex);

            if (guardBody == null || allyBody == null || guard == null)
            {
                spot = guardBody != null ? guardBody.position : attackerSpot;
                width = WidthOf(CombatantSide.Party, targetIndex);
                return guardBody != null;
            }

            if (guard.IsMoving)
            {
                // Do not drag him: the blow lands where he really is right now.
                spot = guardBody.position;
                width = guard.BodyWidth;
                return true;
            }

            // Stand between the ally and the enemy that is coming - no board orientation needed.
            float side = BattleSpots.AttackerSide(attackerSpot, allyBody.position);
            spot = BattleSpots.Beside(allyBody.position, guard.BodyWidth,
                WidthOf(CombatantSide.Party, coveredAllyIndex), side);
            width = guard.BodyWidth;

            guard.RunTo(spot, cover: true);
            return true;
        }

        private static float WidthOf(CombatantSide side, int index)
        {
            if (side == CombatantSide.Party)
            {
                HeroUnitView view = HudController.Instance?.FormationBoard?.GetHeroView(index);
                return view != null ? view.BodyWidth : 0f;
            }

            EnemyUnitView enemy = HudController.Instance?.EnemyStack?.GetEnemyView(index);
            return enemy != null ? enemy.BodyWidth : 0f;
        }

        private static void PlayClip(CombatantSide side, int index, string clipSlot)
        {
            if (side == CombatantSide.Party)
            {
                HeroUnitView view = HudController.Instance?.FormationBoard?.GetHeroView(index);
                view?.PlaySwing(clipSlot);
                return;
            }

            EnemyUnitView enemy = HudController.Instance?.EnemyStack?.GetEnemyView(index);
            enemy?.PlaySwing(clipSlot);
        }

        private static void RunTo(CombatantSide side, int index, Vector3 worldSpot, bool cover)
        {
            if (side == CombatantSide.Party)
            {
                HeroUnitView view = HudController.Instance?.FormationBoard?.GetHeroView(index);

                if (view != null)
                {
                    view.RunTo(worldSpot, cover);
                }

                return;
            }

            EnemyUnitView enemy = HudController.Instance?.EnemyStack?.GetEnemyView(index);

            if (enemy != null)
            {
                enemy.RunTo(worldSpot);
            }
        }

        private static int Key(CombatantSide side, int index)
        {
            return (int)side * 1000 + index;
        }
    }
}
