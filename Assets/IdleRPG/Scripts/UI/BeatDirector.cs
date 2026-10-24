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

            RectTransform targetBody = UnitAnchors.Get(targetSide, targetIndex);

            if (targetBody == null)
            {
                // No view on screen (dead, not spawned): the sim still lands its damage. Nothing to stage.
                reservations.Clear();
                return;
            }

            Vector3 targetSpot = targetBody.position;
            float targetHalf = BattleSpots.HalfWidth(targetBody);

            // 1. A cover is planned FIRST: the guard's spot becomes the spot the blow lands on.
            //    In the sim's event the TARGET is the guard (the hit was redirected to him) and CoveredAllyIndex is
            //    the ally who was saved. Getting those two the right way round is the whole point of this block.
            if (coveredAllyIndex >= 0)
            {
                RectTransform allyBody = UnitAnchors.Get(CombatantSide.Party, coveredAllyIndex);
                HeroUnitView guard = hud.FormationBoard != null ? hud.FormationBoard.GetHeroView(targetIndex) : null;

                if (allyBody != null && guard != null)
                {
                    // Stand on the enemy side of the ally, so the guard is between the ally and the danger.
                    float side = hud.FormationBoard.EnemySideSign;
                    Vector3 guardSpot = BattleSpots.Beside(
                        allyBody.position,
                        BattleSpots.HalfWidth(guard.BodyRect),
                        BattleSpots.HalfWidth(allyBody),
                        side);

                    guard.RunTo(guardSpot, cover: true);

                    targetSpot = guardSpot;
                    targetHalf = BattleSpots.HalfWidth(guard.BodyRect);
                }
            }

            RectTransform attackerBody = UnitAnchors.Get(attackerSide, attackerIndex);

            if (attackerBody == null)
            {
                reservations.Clear();
                return;
            }

            // 2. Play the attacker's clip on the announce, exactly as before.
            PlayClip(attackerSide, attackerIndex, clipSlot);

            // 3. Then place the attacker: a reserved spot wins, then "stay planted", then the near flank.
            if (!reservations.TryGetValue(Key(attackerSide, attackerIndex), out Vector3 attackerSpot))
            {
                if (stayPlanted)
                {
                    reservations.Clear();
                    return;
                }

                float side = BattleSpots.AttackerSide(attackerBody.position, targetSpot);
                attackerSpot = BattleSpots.Beside(
                    targetSpot,
                    BattleSpots.HalfWidth(attackerBody),
                    targetHalf,
                    side);
            }

            RunTo(attackerSide, attackerIndex, attackerSpot, cover: false);
            reservations.Clear();
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
