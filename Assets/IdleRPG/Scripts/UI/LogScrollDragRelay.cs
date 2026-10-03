using UnityEngine;
using UnityEngine.EventSystems;

namespace IdleRPG.UI
{
    /// <summary>
    /// Tells <see cref="CombatLogUI"/> when the player is genuinely dragging the feed.
    ///
    /// ELI5: auto-follow must pause only for a real finger/mouse drag. Without this the log has to guess from
    /// the content position, and the ScrollRect's own clamping looks exactly like the player scrolling - which
    /// is what made the feed stop following "on its own".
    /// </summary>
    public sealed class LogScrollDragRelay : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] private CombatLogUI log;

        public void Bind(CombatLogUI target)
        {
            log = target;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (log != null)
            {
                log.UserDragging = true;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (log != null)
            {
                log.UserDragging = false;
            }
        }

        private void OnDisable()
        {
            // Never leave the feed stuck "held" if the drag handler goes away mid-drag.
            if (log != null)
            {
                log.UserDragging = false;
            }
        }
    }
}
