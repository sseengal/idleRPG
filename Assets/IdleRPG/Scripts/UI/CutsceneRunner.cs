using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using IdleRPG.Combat;
using IdleRPG.Sim;

namespace IdleRPG.UI
{
    /// <summary>
    /// Plays a <see cref="CutsceneDef"/>: a fixed, animation-locked sequence of bars, text and unit moves.
    ///
    /// ELI5: a little film projector. It pauses the fight, draws two black bars, plays the list of steps at their own
    /// times, and puts everything back afterwards. It owns no game state, so a cutscene cannot damage, heal, grant
    /// loot or move the player's formation - if it did, it would be a game rule wearing a costume.
    ///
    /// Time is real time (<see cref="Time.unscaledDeltaTime"/>), never the fight's clock: a cutscene's length comes
    /// from its own animations, which is the whole point of "animation-locked".
    /// </summary>
    public sealed class CutsceneRunner : MonoBehaviour
    {
        private const float BarHeight = 170f;
        private const float BarsPerSecond = 4f;

        public static CutsceneRunner Instance { get; private set; }

        /// <summary>True while a cutscene owns the screen.</summary>
        public bool IsPlaying { get; private set; }

        private RectTransform layer;
        private RectTransform topBar;
        private RectTransform bottomBar;
        private TextMeshProUGUI title;

        private CutsceneDef current;
        private float clock;
        private int nextStep;
        private float barAmount;
        private float barTarget;

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

        /// <summary>Builds the bars and the title line on the given layer. Called once by the HUD.</summary>
        public void Initialize(RectTransform host)
        {
            if (layer != null || host == null)
            {
                return;
            }

            GameObject node = UiRuntime.CreateNode("CutsceneLayer", host);
            layer = node.GetComponent<RectTransform>();
            UiRuntime.Stretch(layer);

            topBar = BuildBar("BarTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            bottomBar = BuildBar("BarBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));

            title = UiRuntime.CreateText(layer, "Title", string.Empty, UiType.Role.Title,
                TextAlignmentOptions.Center, Color.white);
            UiRuntime.Anchor(title.rectTransform, new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.58f));

            SetBars(0f);
            title.SetText(string.Empty);
        }

        private RectTransform BuildBar(string name, Vector2 min, Vector2 max, Vector2 pivot)
        {
            Image image = UiRuntime.CreatePanel(layer, null, Color.black);
            image.rectTransform.gameObject.name = name;
            image.rectTransform.anchorMin = min;
            image.rectTransform.anchorMax = max;
            image.rectTransform.pivot = pivot;
            image.rectTransform.sizeDelta = new Vector2(0f, 0f);
            image.rectTransform.anchoredPosition = Vector2.zero;
            return image.rectTransform;
        }

        /// <summary>Starts a play. Ignored while another is running - one cutscene at a time, always.</summary>
        public void Play(CutsceneDef def)
        {
            if (IsPlaying || def == null || layer == null)
            {
                return;
            }

            current = def;
            clock = 0f;
            nextStep = 0;
            barTarget = 0f;
            IsPlaying = true;

            title.SetText(string.Empty);

            if (def.pauseBattle)
            {
                SetBattlePaused(true);
            }
        }

        /// <summary>Ends the play immediately (skip, hard cap, or a broken step). Idempotent.</summary>
        public void Finish()
        {
            if (!IsPlaying)
            {
                return;
            }

            IsPlaying = false;
            current = null;
            nextStep = 0;
            barAmount = 0f;
            barTarget = 0f;
            SetBars(0f);

            if (title != null)
            {
                title.SetText(string.Empty);
            }

            SetBattlePaused(false);
        }

        private void Update()
        {
            if (!IsPlaying || current == null)
            {
                return;
            }

            // Real time: a cutscene is locked to its own animation lengths, not to the fight's speed.
            clock += Time.unscaledDeltaTime;

            if (current.hardCapSeconds > 0f && clock >= current.hardCapSeconds)
            {
                Finish();
                return;
            }

            while (nextStep < current.steps.Length && current.steps[nextStep].time <= clock)
            {
                Apply(current.steps[nextStep]);
                nextStep++;

                if (!IsPlaying)
                {
                    return;
                }
            }

            if (current.skippable && WasSkipPressed())
            {
                Finish();
                return;
            }

            if (!Mathf.Approximately(barAmount, barTarget))
            {
                barAmount = Mathf.MoveTowards(barAmount, barTarget, BarsPerSecond * Time.unscaledDeltaTime);
                SetBars(barAmount);
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
                case CutsceneVerb.BarsIn:
                    barTarget = 1f;
                    break;

                case CutsceneVerb.BarsOut:
                    barTarget = 0f;

                    // The last bar step ends the play; the bars are already sliding out.
                    if (IsLastBarStep())
                    {
                        Finish();
                    }

                    break;

                case CutsceneVerb.Text:
                    title.SetText(step.text ?? string.Empty);
                    break;

                case CutsceneVerb.MoveUnit:
                    MoveUnit(step);
                    break;

                case CutsceneVerb.PlayClip:
                    PlayClip(step);
                    break;
            }
        }

        private bool IsLastBarStep()
        {
            for (int i = nextStep + 1; i < current.steps.Length; i++)
            {
                if (current.steps[i].verb == CutsceneVerb.BarsOut || current.steps[i].verb == CutsceneVerb.BarsIn)
                {
                    return false;
                }
            }

            return true;
        }

        private void MoveUnit(CutsceneStep step)
        {
            Rect world = layer.rect;
            Vector3 spot = layer.TransformPoint(new Vector3(
                world.xMin + world.width * Mathf.Clamp01(step.spot.x),
                world.yMin + world.height * Mathf.Clamp01(step.spot.y),
                0f));

            if (step.side == CombatantSide.Party)
            {
                HeroUnitView hero = HudController.Instance?.FormationBoard?.GetHeroView(step.index);
                hero?.RunTo(spot, false);
                return;
            }

            EnemyUnitView enemy = HudController.Instance?.EnemyStack?.GetEnemyView(step.index);
            enemy?.RunTo(spot);
        }

        private void PlayClip(CutsceneStep step)
        {
            if (step.side == CombatantSide.Party)
            {
                HeroUnitView hero = HudController.Instance?.FormationBoard?.GetHeroView(step.index);
                hero?.PlaySwing(string.Empty);
                return;
            }

            EnemyUnitView enemy = HudController.Instance?.EnemyStack?.GetEnemyView(step.index);
            enemy?.PlaySwing(string.Empty);
        }

        private void SetBars(float amount)
        {
            barAmount = amount;

            if (topBar == null || bottomBar == null)
            {
                return;
            }

            topBar.sizeDelta = new Vector2(0f, BarHeight * amount);
            bottomBar.sizeDelta = new Vector2(0f, BarHeight * amount);
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
