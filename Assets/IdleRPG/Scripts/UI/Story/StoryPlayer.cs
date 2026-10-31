using System;
using UnityEngine;
using UnityEngine.InputSystem;
using IdleRPG.Core;
using IdleRPG.Data;

namespace IdleRPG.UI
{
    /// <summary>
    /// Walks one <see cref="StorySceneDef"/> at its own pace and puts the fight back when it ends.
    ///
    /// ELI5: the projector. It pauses the fight, reads the scene's beats by time, and tells
    /// <see cref="StoryLayer"/> what to show. It owns no visuals and no game state. A scene cannot damage,
    /// heal, grant loot or move the party.
    ///
    /// Time is REAL time (<see cref="Time.unscaledDeltaTime"/>), never the fight's clock: a scene's length
    /// comes from its own beats, so pausing the fight cannot stretch it.
    /// </summary>
    public sealed class StoryPlayer : MonoBehaviour
    {
        private GameManager manager;
        private StoryLayer layer;
        private StageTransition transition;
        private BattlePageFocus focus;
        private StorySceneDef scene;
        private Action finished;

        private float clock;
        private int nextBeat;
        private bool pausedByScene;

        private enum Phase
        {
            Idle,
            Opening,
            Playing,
            Ending
        }

        private Phase phase = Phase.Idle;

        /// <summary>True from the opening fade until the closing fade finishes.</summary>
        public bool IsPlaying => phase != Phase.Idle;

        /// <summary>
        /// Creates the layer under the battle stage. The overlay root hosts the skip hit area, so a tap
        /// anywhere ends the scene. Returns false when the stage is missing (debug scenes).
        /// </summary>
        public bool Setup(GameManager gameManager, RectTransform stageRoot, RectTransform overlayRoot,
            BattlePageFocus pageFocus)
        {
            manager = gameManager;
            focus = pageFocus;

            if (stageRoot == null)
            {
                Debug.LogError("[StoryPlayer] No battle stage root; story scenes are disabled.");
                return false;
            }

            GameObject node = UiRuntime.CreateNode("StoryLayer", stageRoot);
            node.transform.SetAsLastSibling();
            layer = node.AddComponent<StoryLayer>();
            layer.Build(stageRoot, overlayRoot, Skip);

            // Created after the layer, so the fade draws over actors and damage numbers. It is clipped to
            // the arena render texture, so the HUD can never dim with it.
            transition = StageTransition.Create(stageRoot);
            return layer.enabled;
        }

        /// <summary>
        /// Plays a scene. Ignored while another scene is running, so two scenes can never share the screen.
        /// <paramref name="onFinished"/> runs once, after the fight resumes.
        /// </summary>
        public bool Play(StorySceneDef definition, Action onFinished = null)
        {
            if (phase != Phase.Idle || definition == null || layer == null)
            {
                return false;
            }

            scene = definition;
            finished = onFinished;
            clock = 0f;
            nextBeat = 0;
            phase = Phase.Opening;

            pausedByScene = scene.PauseBattle && manager != null && manager.Combat != null;

            if (pausedByScene)
            {
                manager.Combat.Paused = true;
            }

            if (transition != null)
            {
                transition.Cover(OnCovered);
            }
            else
            {
                OnCovered();
            }

            return true;
        }

        /// <summary>Ends the scene's content. Ignored during a fade. The closing fade does the rest.</summary>
        public void Skip()
        {
            if (phase == Phase.Playing)
            {
                Finish();
            }
        }

        /// <summary>Black is on screen: swap the fight for the scene, then reveal.</summary>
        private void OnCovered()
        {
            if (phase != Phase.Opening || scene == null)
            {
                return;
            }

            layer.Begin(scene);
            focus?.Enter();
            clock = 0f;
            nextBeat = 0;
            phase = Phase.Playing;

            if (transition != null)
            {
                transition.Reveal(null);
            }
        }

        private void Update()
        {
            if (phase != Phase.Playing || scene == null)
            {
                return;
            }

            clock += Time.unscaledDeltaTime;

            if (clock >= scene.HardCapSeconds)
            {
                Finish();
                return;
            }

            StoryBeat[] beats = scene.Beats;

            while (nextBeat < beats.Length && beats[nextBeat].time <= clock)
            {
                Apply(beats[nextBeat]);
                nextBeat++;

                if (phase != Phase.Playing)
                {
                    return;
                }
            }

            if (scene.Skippable && WasSkipPressed())
            {
                Finish();
            }
        }

        private void Apply(StoryBeat beat)
        {
            int index = string.IsNullOrEmpty(beat.actorId) ? -1 : scene.IndexOfActor(beat.actorId);

            switch (beat.verb)
            {
                case StoryVerb.Enter:
                    RequireActor(index, beat);
                    layer.Enter(index, beat.spot, beat.duration);
                    break;

                case StoryVerb.WalkTo:
                    RequireActor(index, beat);
                    layer.WalkTo(index, beat.spot, beat.duration);
                    break;

                case StoryVerb.PlayClip:
                    RequireActor(index, beat);
                    layer.PlayClip(index, beat.clip);
                    break;

                case StoryVerb.Bubble:
                    RequireActor(index, beat);
                    layer.Bubble(index, beat.bubble, beat.text);
                    break;

                case StoryVerb.Say:
                    SayLine(index, beat.text);
                    break;

                case StoryVerb.Hide:
                    RequireActor(index, beat);
                    layer.Hide(index);
                    break;

                case StoryVerb.End:
                    Finish();
                    break;
            }
        }

        /// <summary>Writes one spoken line to the battle log. The log owns colour; the scene owns the words.</summary>
        private void SayLine(int actorIndex, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            string speaker = string.Empty;

            if (actorIndex >= 0 && actorIndex < scene.Actors.Length)
            {
                speaker = scene.Actors[actorIndex].displayName;
            }

            BattleLog.Say(speaker, text);
        }

        private void RequireActor(int index, StoryBeat beat)
        {
            if (index >= 0)
            {
                return;
            }

            Debug.LogWarning($"[StoryPlayer] '{scene.Id}' beat at {beat.time:0.0}s names actor '{beat.actorId}', " +
                             "which is not in the scene. The beat is skipped.");
        }

        private void Finish()
        {
            if (phase != Phase.Playing)
            {
                return;
            }

            // A scene may end while its last line is still typing. Finish the sentence before the curtain falls.
            HudController.Instance?.CompleteStoryText();

            phase = Phase.Ending;

            if (transition != null)
            {
                transition.Cover(OnCoveredForExit);
            }
            else
            {
                OnCoveredForExit();
            }
        }

        /// <summary>Black is on screen: drop the scene, then reveal the fight.</summary>
        private void OnCoveredForExit()
        {
            if (phase != Phase.Ending)
            {
                return;
            }

            if (layer != null)
            {
                layer.End();
            }

            focus?.Exit();

            if (transition != null)
            {
                transition.Reveal(OnExitDone);
            }
            else
            {
                OnExitDone();
            }
        }

        /// <summary>Reveal is done: resume combat, then tell the service the scene is over.</summary>
        private void OnExitDone()
        {
            if (phase != Phase.Ending)
            {
                return;
            }

            if (pausedByScene && manager != null && manager.Combat != null)
            {
                manager.Combat.Paused = false;
            }

            pausedByScene = false;
            scene = null;
            phase = Phase.Idle;

            Action callback = finished;
            finished = null;
            callback?.Invoke();
        }

        private static bool WasSkipPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
        }

        private void OnDestroy()
        {
            if (pausedByScene && manager != null && manager.Combat != null)
            {
                manager.Combat.Paused = false;
            }

            pausedByScene = false;
            phase = Phase.Idle;
        }
    }
}
