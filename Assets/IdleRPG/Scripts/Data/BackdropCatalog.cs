using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// One backdrop per stage, in order. Stage N shows <see cref="entries"/>[(N-1) % Count],
    /// so the list cycles endlessly as stages outgrow the art budget. Adding a new backdrop =
    /// import it as a Sprite, then add it to this list. No code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "BackdropCatalog", menuName = "IdleRPG/Data/Backdrop Catalog")]
    public sealed class BackdropCatalog : ScriptableObject
    {
        [Header("Order (stage 1 = entry 1, stage 2 = entry 2, ... then wrap)")]
        [Tooltip("Backdrop sprites, in stage order. Never stretched at runtime (PreserveAspect).")]
        [SerializeField] private Sprite[] entries;

        public int Count => entries == null ? 0 : entries.Length;

        /// <summary>Backdrop for a 1-based stage, wrapping around when the list is shorter.</summary>
        public Sprite GetBackdrop(int stage)
        {
            if (entries == null || entries.Length == 0)
            {
                return null;
            }

            int index = (Mathf.Max(1, stage) - 1) % entries.Length;
            return entries[index];
        }
    }
}