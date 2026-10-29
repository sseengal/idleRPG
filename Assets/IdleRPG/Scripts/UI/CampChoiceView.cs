using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// One camp choice card. The prefab owns the button, title, value and note; the scene builder supplies the
    /// four different labels and tints, and <see cref="CampPanelUI"/> keeps the two live labels updated.
    /// </summary>
    public sealed class CampChoiceView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI subLabel;
        [SerializeField] private TextMeshProUGUI noteLabel;

        public Button Button => button;
        public TextMeshProUGUI SubLabel => subLabel;
        public TextMeshProUGUI NoteLabel => noteLabel;

        /// <summary>Sets the fixed copy and palette once, at scene build time.</summary>
        public void Apply(string title, string sub, string note, Color tint, Color textColor, Color noteColor)
        {
            if (titleLabel != null)
            {
                titleLabel.SetText(title);
                titleLabel.color = textColor;
            }

            if (subLabel != null)
            {
                subLabel.SetText(sub);
                subLabel.color = textColor;
            }

            if (noteLabel != null)
            {
                noteLabel.SetText(note);
                noteLabel.color = noteColor;
            }

            if (button != null && button.targetGraphic != null)
            {
                button.targetGraphic.color = tint;
            }
        }
    }
}
