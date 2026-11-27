using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// One camp choice card: a neutral button with a title, a value and an optional note. The card owns its look
    /// (neutral face, type roles, tokenised stack) so the camp screen only supplies three strings and a click.
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

        private void Awake()
        {
            ConfigureLayout();
            StyleButton();
        }

        /// <summary>Sets the copy once, at scene build time. The layout and the face are idempotent.</summary>
        public void Apply(string title, string sub, string note)
        {
            ConfigureLayout();
            StyleButton();

            if (titleLabel != null)
            {
                titleLabel.SetText(title);
                UiType.Apply(titleLabel, UiType.Role.Section);
                titleLabel.color = UiTheme.Text;
            }

            if (subLabel != null)
            {
                subLabel.SetText(sub);
                UiType.Apply(subLabel, UiType.Role.Sentence);
                subLabel.color = UiTheme.Text;
            }

            if (noteLabel != null)
            {
                bool hasNote = !string.IsNullOrEmpty(note);
                noteLabel.SetText(note);
                UiType.Apply(noteLabel, UiType.Role.Small);
                noteLabel.color = UiTheme.Text;
                noteLabel.gameObject.SetActive(hasNote);
            }
        }

        /// <summary>A tokenised vertical stack: padding Space8, gap Space8, centred. Replaces the old hand-picked boxes.</summary>
        private void ConfigureLayout()
        {
            VerticalLayoutGroup layout = GetComponent<VerticalLayoutGroup>();

            if (layout == null)
            {
                layout = gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.padding = UiSpace.Padding(UiSpace.Space8);
            layout.spacing = UiSpace.Space8;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        /// <summary>The shared neutral button face + press/disable behaviour. One look for all three cards.</summary>
        private void StyleButton()
        {
            if (button == null)
            {
                return;
            }

            button.transition = Selectable.Transition.None;

            Image face = button.targetGraphic as Image;
            UiSkin skin = UiSkin.Current;

            if (face != null && skin.neutralButtonSprite != null)
            {
                face.sprite = skin.neutralButtonSprite;
                face.type = Image.Type.Sliced;
                face.pixelsPerUnitMultiplier = skin.buttonBorderScale;
                face.color = Color.white;
            }

            if (GetComponent<UiPlateButton>() == null)
            {
                UIEffect effect = gameObject.AddComponent<UIEffect>();
                UiPlateButton plate = gameObject.AddComponent<UiPlateButton>();
                plate.Bind((RectTransform)transform, face, null, effect, button, Color.white, Color.white,
                    slimBorder: false);
            }
        }
    }
}
