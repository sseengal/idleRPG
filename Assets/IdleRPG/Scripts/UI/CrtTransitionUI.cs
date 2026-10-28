using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Combat;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Progression;

namespace IdleRPG.UI
{
    /// <summary>
    /// The CRT screen transition: two black bars close to a bright line, then open.
    /// A normal wave gets a short white-line flicker; a stage or camp gets the full close/open.
    ///
    /// ELI5: the picture folds shut like an old TV, then unfolds again. It is only a picture;
    /// combat keeps its own timing. All timings come from BalanceConfig, so tuning needs no code.
    /// </summary>
    public sealed class CrtTransitionUI : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private RectTransform root;
        [SerializeField] private Image topBar;
        [SerializeField] private Image bottomBar;
        [SerializeField] private Image beam;
        [SerializeField] private Image shade;
        [SerializeField] private GameObject inputBlocker;

        [Header("Fallback seconds (BalanceConfig wins)")]
        [SerializeField] private float closeSeconds = 0.12f;
        [SerializeField] private float openSeconds = 0.18f;
        [SerializeField] private float flickerSeconds = 0.10f;
        [SerializeField] private float flickerDim = 0.12f;

        private GameManager manager;
        private CampService camp;
        private CombatManager combat;

        private bool busy;
        private bool closed;
        private bool closing;
        private bool openQueued;

        private bool TransitionEnabled =>
            manager != null && manager.Balance != null && manager.Balance.CrtEnabled;

        private CrtWaveMode WaveStyle =>
            manager != null && manager.Balance != null ? manager.Balance.CrtWaveStyle : CrtWaveMode.Flicker;

        private float CloseDuration =>
            manager != null && manager.Balance != null ? manager.Balance.CrtCloseSeconds : closeSeconds;

        private float OpenDuration =>
            manager != null && manager.Balance != null ? manager.Balance.CrtOpenSeconds : openSeconds;

        private float FlickerDuration =>
            manager != null && manager.Balance != null ? manager.Balance.CrtFlickerSeconds : flickerSeconds;

        private float FlickerDarkness =>
            manager != null && manager.Balance != null ? manager.Balance.CrtFlickerDim : flickerDim;

        private void Start()
        {
            manager = HudController.Instance != null ? HudController.Instance.GameManager : null;
            camp = manager != null ? manager.Camp : null;
            combat = manager != null ? manager.Combat : null;

            if (combat != null)
            {
                combat.WaveTransitionStarted += OnWaveTransitionStarted;
            }

            if (camp != null)
            {
                camp.Opened += OnCampOpened;
                camp.Closed += OnCampClosed;
            }

            GameEvents.StageChanged += OnStageChanged;
            ResetVisuals();
        }

        private void OnDestroy()
        {
            if (combat != null)
            {
                combat.WaveTransitionStarted -= OnWaveTransitionStarted;
            }

            if (camp != null)
            {
                camp.Opened -= OnCampOpened;
                camp.Closed -= OnCampClosed;
            }

            GameEvents.StageChanged -= OnStageChanged;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            ResetVisuals();
        }

        /// <summary>
        /// Last enemy died. Boss and a wave that leads into camp get the full close; a normal wave gets
        /// the short line flicker. The mode comes from BalanceConfig.
        /// </summary>
        private void OnWaveTransitionStarted(int stage, int wave)
        {
            if (!TransitionEnabled)
            {
                return;
            }

            if (combat != null && combat.IsBossWave)
            {
                RequestClose();
                return;
            }

            if (camp != null && camp.WouldHold(stage, wave + 1))
            {
                RequestClose();
                return;
            }

            switch (WaveStyle)
            {
                case CrtWaveMode.Off:
                    return;
                case CrtWaveMode.Full:
                    RequestClose();
                    return;
                default:
                    RequestFlicker();
                    return;
            }
        }

        /// <summary>
        /// The next wave (or the next stage) has spawned. If the screen is closed, open it now.
        /// This is also the open side of every stage transition.
        /// </summary>
        private void OnStageChanged(int stage, int wave, bool isBossWave)
        {
            if (closed)
            {
                RequestOpen();
            }
        }

        /// <summary>Entering camp: reveal the camp if the wave transition already closed; otherwise blink into it.</summary>
        private void OnCampOpened()
        {
            if (!TransitionEnabled)
            {
                return;
            }

            if (closed)
            {
                RequestOpen();
                return;
            }

            RequestBlip();
        }

        /// <summary>Leaving camp: full close/open, so the camp does not just vanish.</summary>
        private void OnCampClosed()
        {
            if (TransitionEnabled)
            {
                RequestBlip();
            }
        }

        private void RequestClose()
        {
            if (!TransitionEnabled || busy || closed)
            {
                return;
            }

            StartCoroutine(CloseRoutine());
        }

        private void RequestOpen()
        {
            if (!TransitionEnabled)
            {
                return;
            }

            if (busy)
            {
                if (closing)
                {
                    openQueued = true;
                }

                return;
            }

            if (!closed)
            {
                return;
            }

            StartCoroutine(OpenRoutine());
        }

        private void RequestBlip()
        {
            if (!TransitionEnabled)
            {
                return;
            }

            if (closed)
            {
                RequestOpen();
                return;
            }

            if (busy)
            {
                return;
            }

            StartCoroutine(BlipRoutine());
        }

        private void RequestFlicker()
        {
            if (!TransitionEnabled || busy || closed)
            {
                return;
            }

            StartCoroutine(FlickerRoutine());
        }

        private IEnumerator CloseRoutine()
        {
            busy = true;
            closing = true;
            closed = true;
            openQueued = false;

            yield return CloseAnimation();

            closing = false;
            busy = false;

            if (openQueued)
            {
                openQueued = false;
                StartCoroutine(OpenRoutine());
            }
        }

        private IEnumerator OpenRoutine()
        {
            busy = true;
            closing = false;
            openQueued = false;

            yield return OpenAnimation();

            busy = false;
        }

        private IEnumerator BlipRoutine()
        {
            busy = true;
            closing = true;
            closed = true;
            openQueued = false;

            yield return CloseAnimation();

            closing = false;
            openQueued = false;
            yield return OpenAnimation();

            busy = false;
        }

        private IEnumerator FlickerRoutine()
        {
            busy = true;
            SetBeam(true);
            SetBeamAlpha(0f);
            SetShade(true);
            SetShadeAlpha(0f);

            float duration = Mathf.Max(0.02f, FlickerDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(progress * Mathf.PI);
                SetBeamAlpha(pulse);
                SetShadeAlpha(FlickerDarkness * pulse);
                yield return null;
            }

            SetBeam(false);
            SetShade(false);
            busy = false;
        }

        private IEnumerator CloseAnimation()
        {
            SetShade(false);
            SetFullBars(true);
            SetBarHeight(0f);
            SetBlocking(true);
            SetBeam(true);
            SetBeamAlpha(0f);

            float half = HalfHeight;
            float duration = Mathf.Max(0.02f, CloseDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = progress * progress;
                SetBarHeight(half * eased);
                SetBeamAlpha(eased);
                yield return null;
            }

            SetBarHeight(half);
            SetBeamAlpha(1f);

            const float hold = 0.03f;
            elapsed = 0f;

            while (elapsed < hold)
            {
                elapsed += Time.unscaledDeltaTime;
                SetBeamAlpha(1f - Mathf.Clamp01(elapsed / hold));
                yield return null;
            }

            SetBeam(false);
            SetBeamAlpha(0f);
        }

        private IEnumerator OpenAnimation()
        {
            float half = HalfHeight;
            SetFullBars(true);
            SetBarHeight(half);
            SetBlocking(true);
            SetBeam(true);
            SetBeamAlpha(1f);

            float duration = Mathf.Max(0.02f, OpenDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - progress) * (1f - progress);
                SetBarHeight(half * (1f - eased));
                SetBeamAlpha(1f - progress);
                yield return null;
            }

            SetBarHeight(0f);
            SetFullBars(false);
            SetBeam(false);
            SetBeamAlpha(0f);
            SetShade(false);
            SetBlocking(false);
            closed = false;
        }

        private void ResetVisuals()
        {
            busy = false;
            closed = false;
            closing = false;
            openQueued = false;

            SetBarHeight(0f);
            SetFullBars(false);
            SetBeam(false);
            SetBeamAlpha(0f);
            SetShade(false);
            SetShadeAlpha(0f);
            SetBlocking(false);
        }

        private float HalfHeight
        {
            get
            {
                float height = root != null ? root.rect.height : 0f;
                return Mathf.Max(1f, height) * 0.5f;
            }
        }

        private void SetBarHeight(float height)
        {
            if (topBar != null)
            {
                topBar.rectTransform.sizeDelta = new Vector2(0f, Mathf.Max(0f, height));
            }

            if (bottomBar != null)
            {
                bottomBar.rectTransform.sizeDelta = new Vector2(0f, Mathf.Max(0f, height));
            }
        }

        private void SetFullBars(bool active)
        {
            if (topBar != null)
            {
                topBar.enabled = active;
            }

            if (bottomBar != null)
            {
                bottomBar.enabled = active;
            }
        }

        private void SetBeam(bool active)
        {
            if (beam != null)
            {
                beam.enabled = active;
            }
        }

        private void SetBeamAlpha(float alpha)
        {
            if (beam != null)
            {
                beam.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
            }
        }

        private void SetShade(bool active)
        {
            if (shade != null)
            {
                shade.enabled = active;
            }
        }

        private void SetShadeAlpha(float alpha)
        {
            if (shade != null)
            {
                shade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
            }
        }

        private void SetBlocking(bool block)
        {
            if (inputBlocker != null && inputBlocker.activeSelf != block)
            {
                inputBlocker.SetActive(block);
            }
        }
    }
}
