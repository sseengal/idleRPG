using System;
using UnityEngine;

namespace IdleRPG.UI
{
    /// <summary>
    /// The old-school battle swirl for the arena picture.
    ///
    /// ELI5: the battle viewport already shows a live picture (the stage render texture). This class puts
    /// the whirl shader on that picture and winds it into the centre until the arena is black. It stays
    /// black; the caller keeps it hidden with the stage curtain and fades the fight back in.
    ///
    /// The HUD never moves: the material is only on the arena display, so the header and the log stay put.
    /// Time is unscaled, so a paused fight cannot freeze the whirl.
    /// </summary>
    public sealed class BattleSwirl : MonoBehaviour
    {
        private const string MaterialResourcePath = "UI_Whirl";

        private static readonly int ProgressId = Shader.PropertyToID("_Progress");

        [Header("Timing")]
        [SerializeField] private float coverSeconds = 2f;

        private BattleStageSurface surface;
        private Material material;
        private Action covered;
        private bool playing;
        private float elapsed;

        /// <summary>True from the first frame of the whirl until the picture is released.</summary>
        public bool IsPlaying => playing;

        /// <summary>Binds the arena display and builds the runtime material. False when either is missing.</summary>
        public bool Setup(BattleStageSurface stage)
        {
            surface = stage;

            if (surface == null || surface.Display == null)
            {
                Debug.LogError("[BattleSwirl] No arena display; the battle swirl is disabled.");
                return false;
            }

            Material template = Resources.Load<Material>(MaterialResourcePath);

            if (template == null)
            {
                Debug.LogError($"[BattleSwirl] Missing {MaterialResourcePath}.mat in Resources; " +
                               "the battle swirl is disabled.");
                return false;
            }

            material = new Material(template)
            {
                name = "UI_Whirl (runtime)",
                hideFlags = HideFlags.DontSave
            };

            material.SetFloat(ProgressId, 0f);
            return true;
        }

        /// <summary>
        /// Winds the arena into black, then runs <paramref name="onCovered"/> once while it is black.
        /// It stays black until <see cref="Release"/>. Returns false when a whirl is already running.
        /// </summary>
        public bool Play(Action onCovered = null)
        {
            if (playing || material == null)
            {
                return false;
            }

            covered = onCovered;
            elapsed = 0f;
            playing = true;

            surface.SetDisplayMaterial(material);
            Apply(0f);
            return true;
        }

        /// <summary>
        /// Puts the arena picture back without animating. Call it while the stage curtain hides the arena,
        /// so the restored picture cannot pop.
        /// </summary>
        public void Release()
        {
            playing = false;
            covered = null;
            elapsed = 0f;

            Apply(0f);

            if (surface != null)
            {
                surface.SetDisplayMaterial(null);
            }
        }

        private void Update()
        {
            if (!playing)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;

            float seconds = Mathf.Max(0.0001f, coverSeconds);
            float t = Mathf.Clamp01(elapsed / seconds);
            Apply(t);

            if (t < 1f)
            {
                return;
            }

            Apply(1f);

            Action callback = covered;
            covered = null;

            // Stay at black. The caller keeps the curtain up and calls Release when it is hidden.
            playing = false;
            callback?.Invoke();
        }

        private void Apply(float value)
        {
            if (material != null)
            {
                material.SetFloat(ProgressId, Mathf.Clamp01(value));
            }
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                UnityEngine.Object.Destroy(material);
                material = null;
            }
        }
    }
}
