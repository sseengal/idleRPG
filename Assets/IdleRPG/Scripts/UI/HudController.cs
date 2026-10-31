using System;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;

namespace IdleRPG.UI
{
    /// <summary>
    /// Scene-level composition root for the HUD: owns the reference to the game systems and
    /// pushes the party data into the unit views once. Views stay decoupled from each other.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudController : MonoBehaviour
    {
        /// <summary>Current HUD instance, or null when the scene has no HUD (e.g. debug scenes).</summary>
        public static HudController Instance { get; private set; }

        [Header("Systems")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("The modal canvas: every card (ranks, defeat, offline) is parented here so it sits above the HUD " +
                 "and the damage numbers.")]
        [SerializeField] private RectTransform modalRoot;

        [Tooltip("HUD overlay layer for effects that must leave the battle stage (the gold flight to the header).")]
        [SerializeField] private RectTransform vfxRoot;

        [Header("Views")]
        [Tooltip("The battle board (display only): one view per formation slot, built at runtime.")]
        [SerializeField] private FormationBoardView formationBoard;
        [Tooltip("The enemy side: up to 3 stacked slots, each with its own HP bar.")]
        [SerializeField] private EnemyStackView enemyStack;

        [Tooltip("The ability band. Hidden while a story scene owns the battle page.")]
        [SerializeField] private AbilityPanelUI abilityStrip;

        [Tooltip("The battle log. Its box grows while a story scene plays.")]
        [SerializeField] private CombatLogUI combatLog;
        [SerializeField] private FloatingDamageTextPool damageTextPool;

        [Header("Story")]
        [Tooltip("Dev only: the scene the P hotkey replays. The schedule drives real playback later.")]
        [SerializeField] private StorySceneDef debugStoryScene;

        private StoryPlayer storyPlayer;
        private BattlePageFocus battleFocus;

        public GameManager GameManager => gameManager;

        /// <summary>The modal canvas - where every card belongs (above the HUD and the damage numbers).</summary>
        public RectTransform ModalRoot => modalRoot;

        /// <summary>The damage-number pool (the Team board feeds it its anchors too).</summary>
        public FloatingDamageTextPool DamageTextPool => damageTextPool;

        public EnemyStackView EnemyStack => enemyStack;

        /// <summary>The battle board: one view per hero (which can move between pads) and six pads to move to.</summary>
        public FormationBoardView FormationBoard => formationBoard;

        /// <summary>The story projector. Null when the scene has no battle stage (debug scenes).</summary>
        public StoryPlayer Story => storyPlayer;

        /// <summary>The ability band (hidden while a scene plays).</summary>
        public AbilityPanelUI AbilityStrip => abilityStrip;

        /// <summary>The page focus: log grows, ability band fades, while a scene plays.</summary>
        public BattlePageFocus BattleFocus => battleFocus;

        /// <summary>Shows any story line that is still typing. Called when a scene ends or is skipped.</summary>
        public void CompleteStoryText()
        {
            if (combatLog != null)
            {
                combatLog.CompleteTyping();
            }
        }

        /// <summary>Dev only: replay the wired debug scene over the live fight.</summary>
        public bool DebugPlayStory()
        {
            if (storyPlayer == null || debugStoryScene == null)
            {
                return false;
            }

            return storyPlayer.Play(debugStoryScene);
        }

        private void Awake()
        {
            Instance = this;

            if (gameManager == null)
            {
                Debug.LogError("[HudController] GameManager reference is missing; HUD disabled.");
                enabled = false;
            }
        }

        private void Start()
        {
            if (gameManager == null)
            {
                return;
            }

            // The board owns one view per slot and refreshes itself on every board change.
            if (formationBoard != null)
            {
                formationBoard.Build(gameManager, damageTextPool);
            }

            if (enemyStack != null)
            {
                enemyStack.Build(gameManager, damageTextPool);

                // The first wave can already have spawned (this Start may run after the GameManager's), so lay the
                // stack out now instead of waiting for the next spawn - otherwise the enemy area stays blank for a
                // whole wave. Refresh() is safe when nothing has spawned yet: it just leaves the slots off.
                enemyStack.Refresh();
            }

            // Gold-fly effect: coins launch from a dead enemy into the top-bar counter. The node stays on
            // the HUD damage canvas so it can leave the battle stage and reach the header; the stage start
            // point is converted to screen space by BattleStageSurface.
            if (damageTextPool != null)
            {
                Transform hostParent = vfxRoot != null
                    ? vfxRoot
                    : damageTextPool.transform.parent != null ? damageTextPool.transform.parent : transform;
                GameObject host = UiRuntime.CreateNode("GoldFlyVfx", hostParent);

                // Coin positions are resolved as local points of this rect, so give it the full parent size.
                if (host.transform is RectTransform hostRect)
                {
                    UiRuntime.Stretch(hostRect);
                }

                GoldFlyVfx vfx = host.AddComponent<GoldFlyVfx>();
                vfx.Initialize(damageTextPool);
            }

            // The one planner of the fight's moving picture: it decides who stands where for every beat. Without it
            // nobody stages a swing at all, so it is created here, on the object that owns the views.
            if (GetComponent<BeatDirector>() == null)
            {
                gameObject.AddComponent<BeatDirector>();
            }

            // The cutscene projector: a step walker with no visuals of its own. Created here with the planner, so a
            // future shot needs no scene wiring - just a CutsceneDef and one Play call. It gets the screen rect because
            // a shot is written in screen fractions (0..1), which is what makes it independent of the device size.
            CutsceneRunner runner = GetComponent<CutsceneRunner>();

            if (runner == null)
            {
                runner = gameObject.AddComponent<CutsceneRunner>();
            }

            // Shots are authored in screen fractions, so they use a full-screen HUD layer. The damage
            // pool now lives inside the battle stage, which is only the viewport rect.
            RectTransform space = vfxRoot != null
                ? vfxRoot
                : damageTextPool != null && damageTextPool.transform.parent is RectTransform root
                    ? root
                    : modalRoot;

            runner.SetSpace(space);

            // Story projector: one scene at a time, drawn inside the battle viewport. It is created here for the
            // same reason as the cutscene projector - a future scene needs no scene wiring. The debug asset is
            // wired on this component so the P hotkey can replay it without loading from Resources.
            RectTransform stageRoot = BattleStageSurface.Instance != null ? BattleStageSurface.Instance.ContentRoot : null;

            if (stageRoot != null)
            {
                // Page focus first: the story player drives it, so it must exist before the player binds.
                if (combatLog != null && abilityStrip != null)
                {
                    battleFocus = gameObject.AddComponent<BattlePageFocus>();
                    battleFocus.Setup(combatLog, abilityStrip);
                }

                storyPlayer = gameObject.AddComponent<StoryPlayer>();

                if (storyPlayer.Setup(gameManager, stageRoot, modalRoot != null ? modalRoot : vfxRoot, battleFocus))
                {
                    // Bind after Setup: a scene requested during GameManager.Start is held until this line.
                    gameManager.Story?.SetPlayer(PlayStory);
                }
                else
                {
                    storyPlayer = null;
                }
            }
        }

        /// <summary>Shows one scene. The service calls this and resumes the wave through the callback.</summary>
        private bool PlayStory(StorySceneDef scene, Action onFinished)
        {
            return storyPlayer != null && storyPlayer.Play(scene, onFinished);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}