using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// Focuses the battle page for a story scene: the log grows into the ability band, and the ability band
    /// fades away. Both changes are driven by one progress value, so they can never disagree.
    ///
    /// ELI5: while a scene plays, the log gets the whole lower half and the combat buttons step aside.
    /// Everything returns exactly as it was when the scene ends.
    /// </summary>
    public sealed class BattlePageFocus : MonoBehaviour
    {
        [SerializeField] private float focusSeconds = 0.30f;
        [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private CombatLogUI log;
        private RectTransform logRect;
        private AbilityPanelUI strip;
        private CanvasGroup stripGroup;

        private float normalTop;
        private float focusTop;
        private float progress;
        private float target;
        private bool busy;
        private bool focused;

        /// <summary>True while the log owns the lower area.</summary>
        public bool IsFocused => focused;

        /// <summary>Binds the log and the ability band. Returns false when a reference is missing.</summary>
        public bool Setup(CombatLogUI combatLog, AbilityPanelUI abilityStrip)
        {
            log = combatLog;
            strip = abilityStrip;
            logRect = combatLog != null ? combatLog.transform as RectTransform : null;
            RectTransform stripRect = abilityStrip != null ? abilityStrip.transform as RectTransform : null;

            if (logRect == null || stripRect == null)
            {
                Debug.LogError("[BattlePageFocus] Needs the combat log and the ability strip; focus is disabled.");
                enabled = false;
                return false;
            }

            normalTop = logRect.anchorMax.y;
            focusTop = stripRect.anchorMax.y;

            stripGroup = abilityStrip.GetComponent<CanvasGroup>();

            if (stripGroup == null)
            {
                stripGroup = abilityStrip.gameObject.AddComponent<CanvasGroup>();
            }

            progress = 0f;
            target = 0f;
            busy = false;
            focused = false;
            Apply(0f, allowDeactivate: false);
            return true;
        }

        /// <summary>Expands the log and fades the ability band out. Called while the arena is black.</summary>
        public void Enter()
        {
            if (focused)
            {
                return;
            }

            focused = true;

            if (strip != null)
            {
                strip.gameObject.SetActive(true);
            }

            StartTween(1f);
        }

        /// <summary>Collapses the log and returns the ability band. Called while the arena is black.</summary>
        public void Exit()
        {
            focused = false;

            if (strip != null)
            {
                strip.gameObject.SetActive(true);
            }

            StartTween(0f);
        }

        private void StartTween(float to)
        {
            target = to;
            busy = true;
        }

        private void Update()
        {
            if (!busy)
            {
                return;
            }

            float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, focusSeconds);
            progress = Mathf.MoveTowards(progress, target, step);

            bool arrived = Mathf.Approximately(progress, target);
            Apply(ease != null ? ease.Evaluate(progress) : progress, arrived);

            if (arrived)
            {
                busy = false;
            }
        }

        private void Apply(float t, bool allowDeactivate)
        {
            if (logRect != null)
            {
                Vector2 max = logRect.anchorMax;
                max.y = Mathf.Lerp(normalTop, focusTop, t);
                logRect.anchorMax = max;
            }

            if (stripGroup != null)
            {
                stripGroup.alpha = 1f - t;
                stripGroup.interactable = t < 1f;
                stripGroup.blocksRaycasts = t < 1f;
            }

            log?.PinToBottom();

            if (allowDeactivate && strip != null && t >= 1f)
            {
                strip.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (!busy && progress <= 0f)
            {
                return;
            }

            progress = 0f;
            target = 0f;
            busy = false;
            focused = false;

            if (strip != null)
            {
                strip.gameObject.SetActive(true);
            }

            Apply(0f, allowDeactivate: false);
        }
    }
}
