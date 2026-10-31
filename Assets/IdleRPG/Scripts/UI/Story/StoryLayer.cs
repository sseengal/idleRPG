using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Data;

namespace IdleRPG.UI
{
    /// <summary>
    /// The screen a story scene plays on: up to six actor images, one speech bubble, one skip overlay.
    ///
    /// ELI5: the stage hands and the bubble. <see cref="StoryPlayer"/> says "Knight walks to here" or
    /// "Mage shows a question mark"; this class moves the pictures. It owns no timing and no game state.
    ///
    /// It is built at runtime under <c>BattleStageSurface.ContentRoot</c>, so it draws inside the battle
    /// viewport and is clipped by the stage camera.
    /// </summary>
    public sealed class StoryLayer : MonoBehaviour
    {
        /// <summary>Actor slots. The scene asset may define fewer; extra slots stay hidden.</summary>
        public const int MaxActors = 6;

        /// <summary>Base actor slot size in stage units. The actor's own scale multiplies this.</summary>
        private const float ActorSlot = 160f;

        private const float BubbleLift = 0.11f;
        private const float BubbleEdge = 0.14f;

        private sealed class ActorView
        {
            public Image Image;
            public RectTransform Rect;
            public CharacterAnimator Animator;
            public CharacterArtSet Art;
            public Vector2 From;
            public Vector2 To;
            public float Duration;
            public float Elapsed;
            public bool Moving;
        }

        private readonly List<ActorView> actors = new List<ActorView>(MaxActors);

        private RectTransform root;
        private Image bubble;
        private TextMeshProUGUI bubbleText;
        private GameObject skipRoot;
        private GameObject skipHint;
        private Action skipAction;

        private StorySceneDef scene;
        private bool begun;

        private bool hidBoard;
        private bool hidEnemies;
        private bool boardWasActive;
        private bool enemiesWereActive;

        /// <summary>Builds the actor slots, the bubble and the skip overlay. Called once by the player.</summary>
        public void Build(RectTransform stageRoot, RectTransform overlayRoot, Action onSkip)
        {
            root = transform as RectTransform;

            if (root == null || stageRoot == null)
            {
                Debug.LogError("[StoryLayer] Needs a stage root. The scene cannot draw.");
                enabled = false;
                return;
            }

            root.SetParent(stageRoot, false);
            UiRuntime.Stretch(root);
            root.SetAsLastSibling();

            for (int i = 0; i < MaxActors; i++)
            {
                Image image = UiRuntime.CreateIcon("StoryActor" + i, root);
                CharacterAnimator animator = image.gameObject.AddComponent<CharacterAnimator>();
                animator.SetSlotSize(new Vector2(ActorSlot, ActorSlot));
                image.gameObject.SetActive(false);

                actors.Add(new ActorView
                {
                    Image = image,
                    Rect = image.rectTransform,
                    Animator = animator
                });
            }

            BuildBubble();
            BuildSkip(overlayRoot, onSkip);

            skipHint = UiRuntime.CreateText(root, "SkipHint", "TAP TO SKIP",
                UiType.Role.Small, TextAlignmentOptions.Center, UiTheme.TextDim).gameObject;
            UiRuntime.Anchor(skipHint.GetComponent<RectTransform>(), new Vector2(0.2f, 0.05f), new Vector2(0.8f, 0.09f));
            skipHint.SetActive(false);
        }

        /// <summary>Prepares one scene: binds the art, hides every actor and dims the fight behind it.</summary>
        public void Begin(StorySceneDef definition)
        {
            scene = definition;

            if (scene == null)
            {
                return;
            }

            HideBubble();

            for (int i = 0; i < actors.Count; i++)
            {
                ActorView view = actors[i];
                view.Moving = false;
                view.Art = null;
                view.Image.gameObject.SetActive(false);
                view.Animator.Clear();

                if (i >= scene.Actors.Length)
                {
                    continue;
                }

                StoryActorDef actor = scene.Actors[i];
                view.Art = actor.art;
                view.Animator.SetSlotSize(new Vector2(ActorSlot, ActorSlot) * actor.SafeScale);
                view.Animator.SetArt(actor.art);
                view.Animator.FacingLeft = actor.facesLeft;
                SetSpot(view.Rect, new Vector2(0.5f, 0.5f));
            }

            HideBattleViews();

            if (skipRoot != null)
            {
                skipRoot.SetActive(scene.Skippable);
            }

            if (skipHint != null)
            {
                skipHint.SetActive(scene.Skippable);
            }

            begun = true;
        }

        /// <summary>Hides every actor and bubble, restores the fight, and drops the skip overlay.</summary>
        public void End()
        {
            HideBubble();

            for (int i = 0; i < actors.Count; i++)
            {
                ActorView view = actors[i];
                view.Moving = false;
                view.Image.gameObject.SetActive(false);
            }

            if (skipRoot != null)
            {
                skipRoot.SetActive(false);
            }

            if (skipHint != null)
            {
                skipHint.SetActive(false);
            }

            RestoreBattleViews();
            scene = null;
            begun = false;
        }

        /// <summary>True while a scene owns the layer (used by tests and the player).</summary>
        public bool IsBegun => begun;

        /// <summary>Walks an actor in from the nearest screen edge to <paramref name="spot"/>.</summary>
        public void Enter(int actorIndex, Vector2 spot, float duration)
        {
            ActorView view = Get(actorIndex);

            if (view == null)
            {
                return;
            }

            Vector2 start = new Vector2(spot.x < 0.5f ? -0.15f : 1.15f, spot.y);
            view.Image.gameObject.SetActive(true);
            SetSpot(view.Rect, start);
            HideBubble();

            view.From = start;
            view.To = spot;
            view.Duration = Mathf.Max(0f, duration);
            view.Elapsed = 0f;
            view.Moving = view.Duration > 0f;

            if (view.Moving)
            {
                view.Animator.PlayWalk();
            }
            else
            {
                view.Animator.PlayIdle();
            }
        }

        /// <summary>Walks an actor from where it stands to <paramref name="spot"/>.</summary>
        public void WalkTo(int actorIndex, Vector2 spot, float duration)
        {
            ActorView view = Get(actorIndex);

            if (view == null || !view.Image.gameObject.activeSelf)
            {
                return;
            }

            HideBubble();

            view.From = view.Rect.anchorMin;
            view.To = spot;
            view.Duration = Mathf.Max(0f, duration);
            view.Elapsed = 0f;
            view.Moving = view.Duration > 0f;

            if (view.Moving)
            {
                view.Animator.PlayWalk();
            }
            else
            {
                SetSpot(view.Rect, spot);
                view.Animator.PlayIdle();
            }
        }

        /// <summary>Plays one named clip (Attack01, Ability, Hurt, Death). Empty or unknown falls back to Idle.</summary>
        public void PlayClip(int actorIndex, string slot)
        {
            ActorView view = Get(actorIndex);

            if (view == null || view.Art == null)
            {
                return;
            }

            Sprite[] frames = view.Art.FramesFor(slot);

            if (frames == null || frames.Length == 0)
            {
                view.Animator.PlayIdle();
                return;
            }

            view.Animator.PlayClip(frames, view.Art.FpsFor(slot));
        }

        /// <summary>Shows the bubble above an actor. The bubble never follows a walking actor.</summary>
        public void Bubble(int actorIndex, BubbleKind kind, string text)
        {
            ActorView view = Get(actorIndex);

            if (view == null || kind == BubbleKind.None)
            {
                HideBubble();
                return;
            }

            string content;
            UiType.Role role;

            switch (kind)
            {
                case BubbleKind.Dots:
                    content = "…";
                    role = UiType.Role.Display;
                    break;

                case BubbleKind.Bang:
                    content = "!";
                    role = UiType.Role.Display;
                    break;

                case BubbleKind.Question:
                    content = "?";
                    role = UiType.Role.Display;
                    break;

                default:
                    content = text ?? string.Empty;
                    role = UiType.Role.Sentence;
                    break;
            }

            if (string.IsNullOrEmpty(content))
            {
                HideBubble();
                return;
            }

            bubbleText.SetText(content);
            UiType.ApplyAuto(bubbleText, role);

            Vector2 size = kind == BubbleKind.Word ? new Vector2(280f, 96f) : new Vector2(120f, 88f);
            Vector2 at = view.Rect.anchorMin;
            Vector2 point = new Vector2(
                Mathf.Clamp(at.x, BubbleEdge, 1f - BubbleEdge),
                Mathf.Clamp(at.y + BubbleLift, BubbleEdge, 1f - BubbleEdge));

            UiRuntime.CenterOn(bubble.rectTransform, point, size);
            bubble.gameObject.SetActive(true);
        }

        /// <summary>Hides one actor and any bubble that belonged to it.</summary>
        public void Hide(int actorIndex)
        {
            ActorView view = Get(actorIndex);

            if (view == null)
            {
                return;
            }

            view.Moving = false;
            view.Image.gameObject.SetActive(false);
            HideBubble();
        }

        /// <summary>Moves walking actors. Uses unscaled time, so a paused fight cannot stop a scene.</summary>
        private void Update()
        {
            if (!begun)
            {
                return;
            }

            for (int i = 0; i < actors.Count; i++)
            {
                ActorView view = actors[i];

                if (!view.Moving)
                {
                    continue;
                }

                view.Elapsed += Time.unscaledDeltaTime;
                float t = view.Duration <= 0f ? 1f : Mathf.Clamp01(view.Elapsed / view.Duration);
                SetSpot(view.Rect, Vector2.Lerp(view.From, view.To, t));

                if (t >= 1f)
                {
                    view.Moving = false;
                    view.Animator.PlayIdle();
                }
            }
        }

        private ActorView Get(int index)
        {
            if (index < 0 || index >= actors.Count)
            {
                return null;
            }

            return actors[index];
        }

        private static void SetSpot(RectTransform rect, Vector2 spot)
        {
            rect.anchorMin = spot;
            rect.anchorMax = spot;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        private void HideBubble()
        {
            if (bubble != null)
            {
                bubble.gameObject.SetActive(false);
            }
        }

        private void BuildBubble()
        {
            bubble = UiRuntime.CreatePanel(root, UiSkin.Current.panelSprite, UiTheme.PanelLight, raycast: false);
            bubble.gameObject.name = "StoryBubble";
            UiRuntime.CenterOn(bubble.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(120f, 88f));

            bubbleText = UiRuntime.CreateText(bubble.transform, "Text", string.Empty,
                UiType.Role.Display, TextAlignmentOptions.Center, UiTheme.Text);
            UiRuntime.Stretch(bubbleText.rectTransform, UiSpace.Small, UiSpace.Tight, UiSpace.Small, UiSpace.Tight);
            bubble.gameObject.SetActive(false);
        }

        private void BuildSkip(RectTransform overlayRoot, Action onSkip)
        {
            skipAction = onSkip;

            if (overlayRoot == null)
            {
                return;
            }

            skipRoot = UiRuntime.CreateNode("StorySkip", overlayRoot);
            RectTransform rect = skipRoot.GetComponent<RectTransform>();
            UiRuntime.Stretch(rect);
            skipRoot.transform.SetAsLastSibling();

            Image hit = UiRuntime.CreatePanel(skipRoot.transform, null, new Color(0f, 0f, 0f, 0.003f), raycast: true);
            UiRuntime.Stretch(hit.rectTransform);

            Button button = hit.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(SkipClicked);

            skipRoot.SetActive(false);
        }

        private void SkipClicked()
        {
            skipAction?.Invoke();
        }

        private void HideBattleViews()
        {
            HudController hud = HudController.Instance;

            if (hud == null)
            {
                return;
            }

            if (hud.FormationBoard != null)
            {
                boardWasActive = hud.FormationBoard.gameObject.activeSelf;
                hidBoard = true;
                hud.FormationBoard.gameObject.SetActive(false);
            }

            if (hud.EnemyStack != null)
            {
                enemiesWereActive = hud.EnemyStack.gameObject.activeSelf;
                hidEnemies = true;
                hud.EnemyStack.gameObject.SetActive(false);
            }
        }

        private void RestoreBattleViews()
        {
            HudController hud = HudController.Instance;

            if (hud != null)
            {
                if (hidBoard && hud.FormationBoard != null)
                {
                    hud.FormationBoard.gameObject.SetActive(boardWasActive);
                }

                if (hidEnemies && hud.EnemyStack != null)
                {
                    hud.EnemyStack.gameObject.SetActive(enemiesWereActive);
                }
            }

            hidBoard = false;
            hidEnemies = false;
            boardWasActive = false;
            enemiesWereActive = false;
        }

        private void OnDestroy()
        {
            // Do not restore the battle views here. On scene unload the HUD is already being destroyed, and
            // SetActive on a dying object logs "GameObjects can not be made active when they are being destroyed".
            // Normal endings restore in End(); a destroyed layer means the whole HUD is going away.
            if (skipRoot != null)
            {
                Destroy(skipRoot);
                skipRoot = null;
            }
        }
    }
}
