using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// One character's animation frames from a sprite strip (TinyRPG etc.). The unit views animate their
    /// icon <see cref="UnityEngine.UI.Image"/> straight from these arrays - no AnimatorController, no
    /// per-frame scene objects. Idle/Walk loop; Attack/Hurt/Death are one-shots.
    /// Frame count = strip width / 100 (the import policy), so 600x100 Idle = 6 frames.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterArtSet", menuName = "Idle RPG/Data/Character Art Set", order = 2)]
    public sealed class CharacterArtSet : ScriptableObject
    {
        [Header("Frames (order matches the strip, left to right)")]
        public Sprite[] Idle;
        public Sprite[] Walk;
        public Sprite[] Attack01;
        public Sprite[] Attack02;
        public Sprite[] Attack03;
        public Sprite[] Hurt;
        public Sprite[] Death;

        [Header("Playback")]
        [Range(1, 30)] public int IdleFps = 6;
        [Range(1, 30)] public int WalkFps = 8;
        [Range(1, 30)] public int AttackFps = 12;
        [Range(1, 30)] public int HurtFps = 10;
        [Range(1, 30)] public int DeathFps = 10;

        [Header("Facing")]
        [Tooltip("Which direction the art was drawn facing. The views combine this with the hero/enemy rule:\nheroes face right, enemies face left - so this flag decides whether a sprite needs a mirror flip.")]
        public bool ArtFacesLeft = false;

        /// <summary>Static first frame for thumbnails and non-animated fallback.</summary>
        public Sprite Poster => Idle != null && Idle.Length > 0 ? Idle[0] : null;

        /// <summary>
        /// Picks the attack strip the character actually has: the FIRST attack, which is the pack's primary swing,
        /// falling back to the second, then the third, then idle.
        ///
        /// The second strip is a longer variant (Archer 12 frames vs 9, Knight 10 vs 7) and its swing reads long and
        /// late, so the primary strip is what the game uses. Whatever this returns is also what the sim's swing
        /// window is measured from (<see cref="SwingSeconds"/>), so the clip and the blow always agree.
        /// </summary>
        public Sprite[] GetAttack()
        {
            if (Attack01 != null && Attack01.Length > 0)
            {
                return Attack01;
            }

            if (Attack02 != null && Attack02.Length > 0)
            {
                return Attack02;
            }

            return Attack03 != null && Attack03.Length > 0 ? Attack03 : Idle;
        }

        /// <summary>
        /// Which frame of the attack strip the blow lands on: the strip's most EXTENDED frame (the sword at full
        /// reach, the bow released), measured from the art by the importer and written here.
        ///
        /// Why it is measured and not a fraction: the Knight reaches full extension at frame 3 of 7 and the Archer
        /// releases at frame 6 of 9, so no single fraction fits both. With the hit pinned to the LAST frame the
        /// enemy reacted after the swing's recovery frames (~0.25 s of visible delay for the Knight).
        ///
        /// -1 (or out of range) = fall back to the last frame, which can never land the hit before the swing.
        /// </summary>
        public int StrikeFrame = -1;

        /// <summary>
        /// How long this character's swing takes before the blow lands: up to and including its strike frame, at
        /// the strip's native frame rate. The sim uses this as the swing window, so the damage lands the moment the
        /// character's strike pose has played - no trailing delay - instead of on a fixed number of seconds (a
        /// 6-frame and a 12-frame swing differ, and so do their strike frames).
        /// </summary>
        public float SwingSeconds
        {
            get
            {
                Sprite[] attack = GetAttack();
                int frames = attack != null ? attack.Length : 0;

                if (frames == 0)
                {
                    return 0f;
                }

                int strike = StrikeFrame >= 0 && StrikeFrame < frames ? StrikeFrame : frames - 1;
                return (strike + 1) / (float)Mathf.Max(1, AttackFps);
            }
        }

        public bool HasAnimation => Idle != null && Idle.Length > 0;
    }
}