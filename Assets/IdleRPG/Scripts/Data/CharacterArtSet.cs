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
        public bool ArtFacesLeft = true;

        /// <summary>Static first frame for thumbnails and non-animated fallback.</summary>
        public Sprite Poster => Idle != null && Idle.Length > 0 ? Idle[0] : null;

        /// <summary>Picks the attack strip the character actually has (prefer the second, longer one).</summary>
        public Sprite[] GetAttack()
        {
            if (Attack02 != null && Attack02.Length > 0)
            {
                return Attack02;
            }

            return Attack01 != null && Attack01.Length > 0 ? Attack01 : Idle;
        }

        public bool HasAnimation => Idle != null && Idle.Length > 0;
    }
}