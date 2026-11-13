using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;

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
        private const float Gap = 12f;
        private const float CampGap = 20f;
        private const float RingPadding = 6f;

        /// <summary>One mark on the trail, with the two images that colour it.</summary>
        private sealed class MarkView
        {
            public RectTransform Node;
            public Image Ring;
            public Image Body;
            public GlowView Glow;
            public int Step;
            public TrailMarkKind Kind;
        }

        private readonly List<MarkView> views = new List<MarkView>();
        private int builtSteps = -1;
        private int builtCamps = -1;
        private int lastStage = -1;
        private int lastStep = -1;

        private void OnEnable()
        {
            GameEvents.StageChanged += OnStageChanged;
        }

        private void OnDisable()
        {
            GameEvents.StageChanged -= OnStageChanged;
        }

        private void Start()
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
            bool onBoss = step > normalSteps;

            string title = onBoss && !string.IsNullOrEmpty(label.Boss) ? label.Boss : label.Display;
            landLabel.SetText(title.ToUpperInvariant());

            EnsureBuilt(normalSteps, campEvery);
            Apply(JourneyResolver.BuildTrail(normalSteps, step, campEvery));

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
                    total += GapBetween(marks[i - 1].Kind, marks[i].Kind);
                }
            }

            float cursor = -total * 0.5f;

            for (int i = 0; i < marks.Count; i++)
            {
                if (i > 0)
                {
                    cursor += GapBetween(marks[i - 1].Kind, marks[i].Kind);
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

            Image ring = UiRuntime.CreateIcon("Ring", rect);
            ring.sprite = ringSprite;
            CenterOn(ring.rectTransform, SizeOf(kind) + RingPadding);
            ring.gameObject.SetActive(false);

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
                Node = rect, Ring = ring, Body = body, Glow = glow, Step = step, Kind = kind
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

        private void Apply(List<TrailMark> marks)
        {
            int count = Mathf.Min(marks.Count, views.Count);

            for (int i = 0; i < count; i++)
            {
                TrailMark mark = marks[i];
                MarkView view = views[i];

                Color on = UiTheme.Accent;
                Color off = Muted(UiTheme.TextDim, 0.35f);

                switch (mark.Kind)
                {
                    case TrailMarkKind.Camp:
                        view.Body.color = mark.Cleared || mark.Available ? on : off;
                        view.Ring.gameObject.SetActive(mark.Available);
                        view.Ring.color = Muted(UiTheme.Accent, 0.35f);
                        Juice.Glow(view.Glow, mark.Available ? GlowState.Steady : GlowState.Off);
                        break;

                    case TrailMarkKind.Boss:
                        view.Body.color = mark.Cleared ? on : off;
                        view.Ring.gameObject.SetActive(mark.Current);
                        view.Ring.color = UiTheme.AccentLight;
                        Juice.Glow(view.Glow, mark.Current ? GlowState.Pulse : GlowState.Off);
                        break;

                    default:
                        view.Body.color = mark.Cleared ? on : off;
                        view.Ring.gameObject.SetActive(mark.Current);
                        view.Ring.color = UiTheme.AccentLight;
                        Juice.Glow(view.Glow, mark.Current ? GlowState.Pulse : GlowState.Off);
                        break;
                }
            }
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

        private static float GapBetween(TrailMarkKind left, TrailMarkKind right)
        {
            return left == TrailMarkKind.Camp || right == TrailMarkKind.Camp ? CampGap : Gap;
        }

        private static Color Muted(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}


