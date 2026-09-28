using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;

namespace IdleRPG.UI
{
    /// <summary>
    /// Swaps the battle-area backdrop when the stage changes (boss killed -> stage +1; a defeat
    /// rollback -> stage -1 so the world stays consistent with the stage header). The sprite is
    /// picked by stage number and wraps endlessly through the <see cref="BackdropCatalog"/>.
    /// The Image runs with PreserveAspect, so backdrops are never stretched.
    /// </summary>
    public sealed class BackdropPresenter : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Image background;
        [SerializeField] private BackdropCatalog catalog;

        private int shownIndex = -1;

        private void OnEnable()
        {
            GameEvents.StageChanged += OnStageChanged;
            Show(CurrentStage());
        }

        private void OnDisable()
        {
            GameEvents.StageChanged -= OnStageChanged;
        }

        private static int CurrentStage()
        {
            HudController hud = HudController.Instance;
            GameManager manager = hud != null ? hud.GameManager : null;
            return manager != null ? manager.CurrentStage : 1;
        }

        private void OnStageChanged(int stage, int wave, bool isBossWave)
        {
            Show(stage);
        }

        private void Show(int stage)
        {
            if (background == null || catalog == null || catalog.Count == 0)
            {
                return;
            }

            int index = (Mathf.Max(1, stage) - 1) % catalog.Count;
            if (index == shownIndex)
            {
                return;
            }

            Sprite next = catalog.GetBackdrop(stage);
            if (next == null)
            {
                return;
            }

            shownIndex = index;
            background.sprite = next;
        }
    }
}