using UnityEngine;
using UnityEngine.InputSystem;

namespace IdleRPG.Art
{
    /// <summary>
    /// Dev/QA keyboard driver for a combat-loop animated character.
    ///
    /// Fires the controller's combat triggers directly so the animation pipeline can be checked in an editor Play
    /// session without wiring real combat systems yet:
    ///   A = Attack   (Attack trigger -> Attack01, returns to Idle on exit time)
    ///   H = Hurt     (Hurt trigger, returns to Idle on exit time)
    ///   D = Dead     (Dead trigger, AnyState -> Death, holds the fallen pose)
    ///   R = Restart  (Play("Idle") — leaves Death / any state back to the idle loop)
    ///
    /// Uses the Input System package (the project's Active Input Handling is "Input System Package (New)" only, so
    /// UnityEngine.Input must not be used). The Context Menu items below mirror the keys for Inspector-driven reviews.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GoblinCombatTester : MonoBehaviour
    {
        [Tooltip("Animator driven by the combat trigger parameters. Falls back to the one on this object.")]
        [SerializeField] private Animator animator;

        private bool _hintLogged;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.aKey.wasPressedThisFrame)
            {
                Fire("Attack", "Attack");
            }
            else if (keyboard.hKey.wasPressedThisFrame)
            {
                Fire("Hurt", "Hurt");
            }
            else if (keyboard.dKey.wasPressedThisFrame)
            {
                Fire("Dead", "Dead (R restarts)");
            }
            else if (keyboard.rKey.wasPressedThisFrame)
            {
                Restart();
            }
        }

        [ContextMenu("Trigger Attack (A)")]
        public void TriggerAttack() => Fire("Attack", "Attack");

        [ContextMenu("Trigger Hurt (H)")]
        public void TriggerHurt() => Fire("Hurt", "Hurt");

        [ContextMenu("Trigger Dead (D) — holds")]
        public void TriggerDead() => Fire("Dead", "Dead (R restarts)");

        [ContextMenu("Restart to Idle (R)")]
        public void RestartToIdle() => Restart();

        private void Fire(string trigger, string label)
        {
            if (animator == null)
            {
                Debug.LogWarning("[GoblinCombatTester] no Animator assigned; nothing to fire.", this);
                return;
            }

            animator.SetTrigger(trigger);
            HintOnce(label);
        }

        private void Restart()
        {
            if (animator == null)
            {
                Debug.LogWarning("[GoblinCombatTester] no Animator assigned; can't restart.", this);
                return;
            }

            animator.Play("Idle", 0, 0f);
            animator.Update(0f);
        }

        private void HintOnce(string what)
        {
            if (_hintLogged)
            {
                return;
            }

            Debug.Log($"[GoblinCombatTester] {what} fired. Keys: A=Attack, H=Hurt, D=Dead, R=Restart to Idle.", this);
            _hintLogged = true;
        }
    }
}