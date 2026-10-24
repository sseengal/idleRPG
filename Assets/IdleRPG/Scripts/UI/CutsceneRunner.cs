using UnityEngine;
using UnityEngine.InputSystem;
using IdleRPG.Combat;
using IdleRPG.Sim;

namespace IdleRPG.UI
{
    /// <summary>
    /// Plays a <see cref="CutsceneDef"/>: a fixed, animation-locked list of beats.
    ///
    /// ELI5: a small projector with no screen of its own. It pauses the fight, walks the list at the times written on it
    /// (moving a unit, playing a clip), and then puts the fight back. It draws nothing and owns nothing, so a cutscene
    /// cannot damage, heal, grant loot or move the player's formation - if it could, it would be a game rule in a
    /// costume.
    ///
    /// Time is REAL time (<see cref="Time.unscaledDeltaTime"/>), never the fight's clock: a cutscene's length comes
    /// from its own animation lengths, which is what "animation-locked" means.
    /// </summary>
    public sealed class CutsceneRunner : MonoBehaviour
    {
        public static CutsceneRunner Instance { get; private set; }

        /// <summary>True while a cutscene owns the fight.</summary>
        public bool IsPlaying { get; private set; }

        private CutsceneDef current;
        private float clock;
        private int nextStep;
        private RectTransform space;

        /// <summary>
        /// The screen-sized rect a shot's 0..1 positions are measured in. The HUD hands it over once; if that never
        /// happens the runner falls back to the canvas it lives under.
        /// </summary>
        public void SetSpace(RectTransform host)
        {
            if (host != null)
            {
                space = host;
            }
        }

        private RectTransform Space
        {
            get
            {
                if (space == null)
                {
                    Canvas canvas = GetComponentInParent<Canvas>();
                    space = canvas != null ? canvas.transform as RectTransform : null;
                }

                return space;
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Starts a play. Ignored while another is running - one cutscene at a time, always.</summary>
        public void Play(CutsceneDef def)
        {
            if (IsPlaying || def == null)
            {
                return;
            }

            current = def;
            clock = 0f;
            nextStep = 0;
            IsPlaying = true;

            if (def.pauseBattle)
            {
                SetBattlePaused(true);
            }
        }

        /// <summary>Ends the play immediately (skip or hard cap). Idempotent.</summary>
        public void Finish()
        {
            if (!IsPlaying)
            {
                return;
            }

            IsPlaying = false;
            current = null;
            nextStep = 0;
            SetBattlePaused(false);
        }

        private void Update()
        {
            if (!IsPlaying || current == null)
            {
                return;
            }

            clock += Time.unscaledDeltaTime;

            // A long frame (or a backgrounded app) jumps to the beat it should already be on instead of stretching.
            if (current.hardCapSeconds > 0f && clock >= current.hardCapSeconds)
            {
                Finish();
                return;
            }

            while (nextStep < current.steps.Length && current.steps[nextStep].time <= clock)
            {
                Apply(current.steps[nextStep]);
                nextStep++;
            }

            if (current.skippable && WasSkipPressed())
            {
                Finish();
            }
        }

        private static bool WasSkipPressed()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                return true;
            }

            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
        }

        private void Apply(CutsceneStep step)
        {
            switch (step.verb)
            {
                case CutsceneVerb.MoveUnit:
                    MoveUnit(step);
                    break;

                case CutsceneVerb.PlayClip:
                    PlayClip(step);
                    break;
            }
        }

        private void MoveUnit(CutsceneStep step)
        {
            RectTransform host = Space;

            if (host == null)
            {
                return;
            }

            // A screen fraction, so a shot survives any resolution: 0.5 / 0.5 is the middle of the HUD.
            Rect area = host.rect;
            Vector3 spot = host.TransformPoint(new Vector3(
                area.xMin + area.width * Mathf.Clamp01(step.spot.x),
                area.yMin + area.height * Mathf.Clamp01(step.spot.y),
                0f));

            if (step.side == CombatantSide.Party)
            {
                HudController.Instance?.FormationBoard?.GetHeroView(step.index)?.RunTo(spot, false);
                return;
            }

            HudController.Instance?.EnemyStack?.GetEnemyView(step.index)?.RunTo(spot);
        }

        private void PlayClip(CutsceneStep step)
        {
            if (step.side == CombatantSide.Party)
            {
                HudController.Instance?.FormationBoard?.GetHeroView(step.index)?.PlaySwing(string.Empty);
                return;
            }

            HudController.Instance?.EnemyStack?.GetEnemyView(step.index)?.PlaySwing(string.Empty);
        }

        private static void SetBattlePaused(bool paused)
        {
            CombatManager combat = HudController.Instance?.GameManager?.Combat;

            if (combat != null)
            {
                combat.Paused = paused;
            }
        }
    }
}
