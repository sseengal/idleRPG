using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// Shared rules for every temporary widget (dropdown menus, docked sheets):
    ///  * instant close - deactivate FIRST so a dead widget can never swallow a tap, then destroy;
    ///  * a full-page transparent catcher that closes the widget on an outside tap.
    /// One page keeps at most one transient open at a time (callers close before opening).
    /// </summary>
    public static class UiTransient
    {
        /// <summary>Deactivates then destroys - the standard "gone right now" close.</summary>
        public static void Close(GameObject node)
        {
            if (node == null)
            {
                return;
            }

            node.SetActive(false);
            Object.Destroy(node);
        }

        /// <summary>
        /// A full-stretch transparent Image + Button. Parent it FIRST, then build the widget's content
        /// AFTER it (later siblings render on top). An outside tap triggers <paramref name="onClick"/>.
        /// Note: UiRuntime.CreatePanel defaults raycast OFF; this catcher forces it ON.
        /// </summary>
        public static Image CreateTapCatcher(Transform parent, UnityAction onClick)
        {
            Image catcher = UiRuntime.CreatePanel(parent, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            RectTransform rect = catcher.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Button button = catcher.gameObject.AddComponent<Button>();
            button.targetGraphic = catcher;
            button.onClick.AddListener(onClick);
            return catcher;
        }
    }
}