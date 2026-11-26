using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Progression;

namespace IdleRPG.UI
{
    /// <summary>
    /// The journey band: one land name, and a trail of marks for the stage's steps, camps and boss.
    ///
    /// ELI5: the signpost above the fight. It says where the party is and draws the road ahead as dots,
    /// a ring where you can rest, and a diamond where the boss waits. No numbers.
    ///
    /// Rules it keeps (Docs/Journey.md):
    ///  * the trail is built from the stage plan and the camp cadence, never from a fixed ten;
    ///  * marks are rebuilt only when the shape of the stage changes, so a step change never rebuilds
    ///    layout during a fight;
    ///  * it only reads events and the manager, never Update().
    ///
    /// The camp and boss marks are PLACEHOLDERS: a hollow ring and a rotated square. Real 16 x 16 pixel
    /// icons replace them later without touching this code.
    /// </summary>
    public sealed class JourneyBandUI : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private TextMeshProUGUI landLabel;
        [SerializeField] private RectTransform trailRoot;
        [SerializeField] private JourneyCatalog catalog;

        [Tooltip("Filled circle. White, so the band tints it.")]
        [SerializeField] private Sprite dotSprite;

        [Tooltip("Hollow circle. Placeholder for a camp icon.")]
        [SerializeField] private Sprite ringSprite;

        [Tooltip("Halo for the current step. Placeholder art: soft, stepped or burst.")]
        [SerializeField] private Sprite haloSprite;

        [Tooltip("Glow recipe shared by every mark.")]
        [SerializeField] private GlowStyle glowStyle;

        [Header("Fallbacks (used only when the live config is missing)")]
        [SerializeField] private int fallbackNormalSteps = 9;
        [SerializeField] private int fallbackCampEvery = 3;

        private const float StepSize = 20f;
        private const float CampSize = 24f;
        private const float BossSize = 30f;

        /// <summary>One gap for every pair of marks, from the ladder. A camp is not further away than a wave.</summary>
        private const float Gap = UiSpace.Space16;

        /// <summary>The mark you are ON is 1.25x bigger, so "here" reads without relying on colour alone.</summary>
        private const float HereScale = 1.25f;

        /// <summary>One mark on the trail, with the halo that can breathe behind it.</summary>
        private sealed class MarkView
        {
            public RectTransform Node;
            public Image Halo;
            public Image Body;
            public GlowView Glow;
            public int Step;
            public TrailMarkKind Kind;
        }

        private readonly List<MarkView> views = new List<MarkView>();
        private CampService camp;
        private int builtSteps = -1;
        private int builtCamps = -1;
        private int lastStage = -1;
        private int lastStep = -1;

        private void OnEnable()
        {
            GameEvents.StageChanged += OnStageChanged;
            BindCamp();
        }

        private void OnDisable()
        {
            GameEvents.StageChanged -= OnStageChanged;

            if (camp != null)
            {
                camp.Opened -= OnCampChanged;
                camp.Closed -= OnCampChanged;
                camp = null;
            }
        }

        private void Start()
        {
            BindCamp();
            Refresh();
        }

        /// <summary>
        /// The camp panel is the only honest signal for "you are ON the camp": no stage change fires when it takes the
        /// screen, so the band listens to the panel itself. One subscription, one refresh.
        /// </summary>
        private void BindCamp()
        {
            if (camp != null)
            {
                return;
            }

            HudController hud = HudController.Instance;
            GameManager manager = hud != null ? hud.GameManager : null;
            camp = manager != null ? manager.Camp : null;

            if (camp == null)
            {
                return;
            }

            camp.Opened += OnCampChanged;
            camp.Closed += OnCampChanged;
        }

        private void OnCampChanged()
        {
            Refresh();
        }

        private void OnStageChanged(int stage, int wave, bool isBossWave)
        {
            Refresh();
        }

        /// <summary>Reads the live stage and step and shows them. Safe to call often.</summary>
        public void Refresh()
        {
            HudController hud = HudController.Instance;
            GameManager manager = hud != null ? hud.GameManager : null;

            if (manager == null || landLabel == null || trailRoot == null)
            {
                return;
            }

            int stage = Mathf.Max(1, manager.CurrentStage);
            int step = Mathf.Max(1, manager.CurrentWave);
            int normalSteps = ResolveNormalSteps(manager, stage);
            int campEvery = manager.Balance != null
                ? manager.Balance.CampEveryWaves
                : Mathf.Max(1, fallbackCampEvery);

            JourneyLabel label = JourneyResolver.Resolve(catalog, stage, step);

            // The band names the LAND, always (owner, 2026-11-26). The old rule swapped the boss name in for the last
            // step, so the signpost read "SLIME KING" while the party was still in Greenwake.
            landLabel.SetText(label.Display.ToUpperInvariant());

            EnsureBuilt(normalSteps, campEvery);
            Apply(JourneyResolver.BuildTrail(normalSteps, step, campEvery), camp != null && camp.IsOpen, step);

            // A cleared step flashes once. Only on a single-step advance inside the same stage, so a
            // stage change, a debug jump or a reload never fires a stray flash.
            if (lastStage == stage && lastStep >= 1 && step == lastStep + 1)
            {
                FlashStep(lastStep);
            }

            lastStage = stage;
            lastStep = step;
        }

        private void FlashStep(int step)
        {
            for (int i = 0; i < views.Count; i++)
            {
                if (views[i].Kind == TrailMarkKind.Step && views[i].Step == step)
                {
                    // A finished step pops once. Its halo is normally asleep, so wake it for the pop.
                    views[i].Halo.enabled = true;
                    Juice.Flash(views[i].Glow);
                    return;
                }
            }
        }

        private int ResolveNormalSteps(GameManager manager, int stage)
        {
            int fallback = manager.Balance != null
                ? manager.Balance.NormalWavesPerStage
                : Mathf.Max(1, fallbackNormalSteps);

            return manager.WaveData != null
                ? manager.WaveData.NormalWaveCountFor(stage, fallback)
                : fallback;
        }

        private void EnsureBuilt(int normalSteps, int campEvery)
        {
            if (builtSteps == normalSteps && builtCamps == campEvery)
            {
                return;
            }

            Clear();
            builtSteps = normalSteps;
            builtCamps = campEvery;

            List<TrailMark> marks = JourneyResolver.BuildTrail(normalSteps, 1, campEvery);
            float total = 0f;

            for (int i = 0; i < marks.Count; i++)
            {
                total += SizeOf(marks[i].Kind);

                if (i > 0)
                {
                    total += Gap;
                }
            }

            float cursor = -total * 0.5f;

            for (int i = 0; i < marks.Count; i++)
            {
                if (i > 0)
                {
                    cursor += Gap;
                }

                float size = SizeOf(marks[i].Kind);
                MarkView view = CreateView(marks[i].Kind, marks[i].Step);
                view.Node.anchoredPosition = new Vector2(cursor + (size * 0.5f), 0f);
                views.Add(view);
                cursor += size;
            }
        }

        private MarkView CreateView(TrailMarkKind kind, int step)
        {
            GameObject node = UiRuntime.CreateNode(kind + "Mark", trailRoot);
            RectTransform rect = node.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(SizeOf(kind), SizeOf(kind));

            // Halo first, so it renders behind the mark. It is larger than the mark on purpose.
            Image halo = UiRuntime.CreateIcon("Halo", rect);
            halo.sprite = haloSprite;
            halo.color = Color.clear;
            CenterOn(halo.rectTransform, SizeOf(kind) * (glowStyle != null ? glowStyle.HaloScale : 2.6f));
            halo.rectTransform.SetAsFirstSibling();

            // Asleep until this mark is the one you are on, so a trail of 13 marks costs 13 draws, not 26.
            halo.enabled = false;

            Image body = UiRuntime.CreateIcon("Body", rect);
            body.sprite = BodySpriteFor(kind);
            CenterOn(body.rectTransform, SizeOf(kind));

            if (kind == TrailMarkKind.Boss)
            {
                // Placeholder boss: a square turned 45 degrees. No art needed.
                body.sprite = null;
                body.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                body.rectTransform.sizeDelta = new Vector2(SizeOf(kind) * 0.72f, SizeOf(kind) * 0.72f);
            }

            GlowView glow = node.AddComponent<GlowView>();
            glow.Bind(halo, glowStyle);

            return new MarkView
            {
                Node = rect, Halo = halo, Body = body, Glow = glow, Step = step, Kind = kind
            };
        }

        private Sprite BodySpriteFor(TrailMarkKind kind)
        {
            return kind == TrailMarkKind.Camp ? ringSprite : dotSprite;
        }

        private static void CenterOn(RectTransform rect, float size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(size, size);
        }

        /// <summary>
        /// Paints the trail. One rule for every mark (owner, 2026-11-25):
        ///   colour = have I done it?  passed or here is gold; ahead keeps the default dim dot;
        ///   shape  = what is it?      filled dot, hollow ring (camp), diamond (boss);
        ///   motion = where am I?      exactly one mark is 1.25x bigger and breathes.
        ///
        /// A camp is "here" only while its panel owns the screen, and then nothing else breathes - so the trail can
        /// never show two places at once (the old fault: a camp turned gold the moment you reached its wave).
        /// </summary>
        private void Apply(List<TrailMark> marks, bool onCamp, int currentStep)
        {
            int campStep = onCamp ? CampStepOn(marks, currentStep) : -1;
            int count = Mathf.Min(marks.Count, views.Count);

            for (int i = 0; i < count; i++)
            {
                TrailMark mark = marks[i];
                MarkView view = views[i];

                // The wave a camp sits on counts as finished the moment the camp screen opens.
                bool passed = mark.Cleared
                    || (mark.Kind == TrailMarkKind.Step && mark.Step == campStep);

                bool here = mark.Kind == TrailMarkKind.Camp
                    ? onCamp && mark.Step == campStep
                    : mark.Current && !onCamp;

                view.Body.color = passed || here ? UiTheme.Accent : Muted(UiTheme.TextDim, 0.35f);
                view.Node.localScale = here ? new Vector3(HereScale, HereScale, 1f) : Vector3.one;
                view.Halo.enabled = here;
                Juice.Glow(view.Glow, here ? GlowState.Pulse : GlowState.Off);
            }
        }

        /// <summary>The camp you are standing on: the last camp at or behind the current wave, or -1 when closed.</summary>
        private static int CampStepOn(List<TrailMark> marks, int currentStep)
        {
            int best = -1;

            for (int i = 0; i < marks.Count; i++)
            {
                if (marks[i].Kind == TrailMarkKind.Camp && marks[i].Step <= currentStep)
                {
                    best = marks[i].Step;
                }
            }

            return best;
        }

        private void Clear()
        {
            for (int i = 0; i < views.Count; i++)
            {
                if (views[i].Node != null)
                {
                    Destroy(views[i].Node.gameObject);
                }
            }

            views.Clear();
        }

        private static float SizeOf(TrailMarkKind kind)
        {
            switch (kind)
            {
                case TrailMarkKind.Boss:
                    return BossSize;
                case TrailMarkKind.Camp:
                    return CampSize;
                default:
                    return StepSize;
            }
        }

        private static Color Muted(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}


