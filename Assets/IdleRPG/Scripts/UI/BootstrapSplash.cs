using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleRPG.UI
{
    /// <summary>
    /// Boot-scene splash: shows "Kingsguard: Tiny RPG" and a progress bar while the main game scene loads
    /// asynchronously, then activates it.
    ///
    /// ELI5: a title card that waits for the whole game to be ready (loading the main scene also loads every asset
    /// it references), filling a real bar from the load's own progress, then hands over.
    /// </summary>
    public sealed class BootstrapSplash : MonoBehaviour
    {
        [SerializeField] private string mainSceneName = "Main";

        [SerializeField] private TextMeshProUGUI titleLabel;

        [Tooltip("A RectTransform whose width is driven 0..1 from the left (anchored min.x=0).")]
        [SerializeField] private RectTransform progressFill;

        private const string Title = "Kingsguard: Tiny RPG";

        private AsyncOperation loadOperation;

        private void Start()
        {
            if (titleLabel != null)
            {
                titleLabel.SetText(Title);
            }

            loadOperation = SceneManager.LoadSceneAsync(mainSceneName);

            if (loadOperation == null)
            {
                Debug.LogError($"[BootstrapSplash] Could not load '{mainSceneName}'. Is it in Build Settings?");
            }
        }

        private void Update()
        {
            if (loadOperation == null)
            {
                return;
            }

            // op.progress runs 0..0.9 while the scene loads; the scene then activates on its own.
            SetFill(Mathf.Clamp01(loadOperation.progress / 0.9f));
        }

        private void SetFill(float amount)
        {
            if (progressFill == null)
            {
                return;
            }

            Vector2 max = progressFill.anchorMax;
            max.x = Mathf.Clamp01(amount);
            progressFill.anchorMax = max;
        }
    }
}
