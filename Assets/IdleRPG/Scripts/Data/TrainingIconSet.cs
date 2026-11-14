using UnityEngine;

namespace IdleRPG.Data
{
    /// <summary>
    /// The art for the Training tree nodes: one sprite per <see cref="TrainingEffect"/>.
    ///
    /// ELI5: a little picture book. The tree used to draw a letter (A, D, H...) inside each diamond; this asset
    /// says which picture to draw instead. A missing picture falls back to the letter, so art can arrive in
    /// batches without ever leaving a blank node.
    /// </summary>
    [CreateAssetMenu(fileName = "TrainingIconSet", menuName = "Idle RPG/Training Icon Set")]
    public sealed class TrainingIconSet : ScriptableObject
    {
        [Tooltip("Indexed by TrainingEffect (0 = AttackFlat ... 9 = SurviveDeathShield).")]
        [SerializeField] private Sprite[] icons = new Sprite[0];

        public Sprite IconFor(TrainingEffect effect)
        {
            int index = (int)effect;
            return icons != null && index >= 0 && index < icons.Length ? icons[index] : null;
        }

        public int Count => icons != null ? icons.Length : 0;
    }
}
