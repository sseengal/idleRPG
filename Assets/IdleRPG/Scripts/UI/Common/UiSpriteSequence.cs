using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// Plays a short sprite sequence on one uGUI <see cref="Image"/>: the camp fire's flicker, and anything else
    /// that is a handful of frames rather than a character with states.
    ///
    /// ELI5: a flip book. Give it the frames and a speed; it swaps the picture on a timer and loops. No Animator, no
    /// AnimatorController, no state machine - a UI picture that changes is not worth that machinery.
    ///
    /// Why it exists: <see cref="CharacterAnimator"/> is built around a character's art set, slot sizing and
    /// attack/hurt/death states. A camp fire has none of that, and pulling a whole Animator into a uGUI canvas
    /// would add a controller asset, a clip and a runtime binding for six pictures.
    /// </summary>
    public sealed class UiSpriteSequence : MonoBehaviour
    {
        [Tooltip("The picture that changes.")]
        [SerializeField] private Image target;

        [Tooltip("Frames in order. One entry means a still picture and no work.")]
        [SerializeField] private Sprite[] frames;

        [Tooltip("Frames per second. A camp fire flickers around 10.")]
        [SerializeField] private float framesPerSecond = 10f;

        [Tooltip("Loop forever (a fire does) or stop on the last frame.")]
        [SerializeField] private bool loop = true;

        private float timer;
        private int cursor;

        /// <summary>Wires the sequence from code, for objects a scene builder creates.</summary>
        public void Bind(Image image, Sprite[] clip, float fps, bool loopFrames)
        {
            target = image;
            frames = clip;
            framesPerSecond = Mathf.Max(0.01f, fps);
            loop = loopFrames;
            cursor = 0;
            timer = 0f;
            Apply();
        }

        private void OnEnable()
        {
            cursor = 0;
            timer = 0f;
            Apply();
        }

        private void Update()
        {
            if (target == null || frames == null || frames.Length < 2)
            {
                return;
            }

            float step = 1f / Mathf.Max(0.01f, framesPerSecond);
            timer += Time.deltaTime;

            if (timer < step)
            {
                return;
            }

            // Consume the whole step, not the whole frame: a long frame cannot make the animation run fast.
            timer -= step;
            cursor++;

            if (cursor >= frames.Length)
            {
                if (!loop)
                {
                    cursor = frames.Length - 1;
                    enabled = false;
                }
                else
                {
                    cursor = 0;
                }
            }

            Apply();
        }

        private void Apply()
        {
            if (target != null && frames != null && frames.Length > 0)
            {
                target.sprite = frames[Mathf.Clamp(cursor, 0, frames.Length - 1)];
            }
        }
    }
}
