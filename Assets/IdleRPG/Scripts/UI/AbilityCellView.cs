using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// One hero's ability cell, built in code so the layout has ONE owner.
    ///
    /// ELI5: one big button per hero. The move's picture sits in the button, the move's name sits under it in one
    /// small line, and a thin line along the bottom edge says the state. Green line = press me now. A gold bar
    /// growing left to right = the move is resting, and its length IS the countdown. Dark line = nothing to press.
    ///
    /// Why it builds itself instead of using a prefab: the prefab's bands were placed by hand and could not be
    /// reviewed, so a badge added in code landed on top of the text. Now the scene builder AND the running game call
    /// this one method, so a rebuild and play mode cannot disagree. Gaps come from <see cref="UiSpace"/> and every
    /// colour from <see cref="UiTheme"/>, so the guard checks stay green.
    /// </summary>
    public sealed class AbilityCellView : MonoBehaviour
    {
        /// <summary>The picture's source size. All 11 move icons are 34x34 pixels.</summary>
        private const float ArtPixels = 34f;

        /// <summary>Zoom applied to that art. A whole number, so no pixel column ends up half a pixel wide.</summary>
        private const float IconZoom = 3f;

        /// <summary>The picture size in canvas units: 34 x 3 = 102. One zoom down from 4, so the move's name fits.</summary>
        public const float IconSize = ArtPixels * IconZoom;

        /// <summary>The state line: thin enough to read as an underline, not as a second button.</summary>
        private const float BarHeight = UiSpace.Snug;

        /// <summary>One line of the move's name: the floor size, because the name is a caption, not a control.</summary>
        private static readonly float NameHeight = UiType.LineHeight(UiType.Role.Micro);

        [SerializeField] private Button castButton;
        [SerializeField] private UiPlateButton plate;
        [SerializeField] private Image abilityIcon;
        [SerializeField] private TextMeshProUGUI abilityName;
        [SerializeField] private Image stateRail;
        [SerializeField] private Image stateFill;

        /// <summary>Builds the runtime row the ability panel drives for this cell.</summary>
        public AbilityPanelUI.Row ToRow(int heroIndex)
        {
            return new AbilityPanelUI.Row
            {
                heroIndex = heroIndex,
                plate = plate,
                abilityIcon = abilityIcon,
                abilityName = abilityName,
                stateRail = stateRail,
                stateFill = stateFill,
                castButton = castButton,

                // The line is built full and dark, so the first frame shows the state it was built in, not a
                // half-drawn bar rising out of nowhere.
                shownFill = 1f
            };
        }

        /// <summary>
        /// Builds one cell under <paramref name="parent"/>. The whole cell is the button, so a tap anywhere casts.
        /// </summary>
        public static AbilityCellView Build(Transform parent, int index)
        {
            Image face = UiRuntime.Plate(parent, "Hero" + index, Vector2.zero, Vector2.one, UiTheme.CardDark, raycast: true);
            face.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            Button button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;

            UIEffect effect = face.gameObject.AddComponent<UIEffect>();
            UiPlateButton plateView = face.gameObject.AddComponent<UiPlateButton>();
            plateView.Bind(face.rectTransform, face, null, effect, button, UiTheme.CardDark, UiTheme.CardDark);

            AbilityCellView cell = face.gameObject.AddComponent<AbilityCellView>();
            cell.castButton = button;
            cell.plate = plateView;

            // The picture sits above the name. Both are pinned to the bottom edge, so the name and the state line
            // stay welded together whatever height the strip hands the cell.
            cell.abilityIcon = UiRuntime.CreateIcon("Icon", face.transform);
            RectTransform iconRect = cell.abilityIcon.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0f);
            iconRect.anchorMax = new Vector2(0.5f, 0f);
            iconRect.pivot = new Vector2(0.5f, 0f);
            iconRect.sizeDelta = new Vector2(IconSize, IconSize);
            iconRect.anchoredPosition = new Vector2(0f, IconBottom);
            cell.abilityIcon.enabled = false;

            // The move's name. A picture alone cannot say WHICH move this is, so the cell says it once, small and
            // dim, in the same words the party page uses.
            cell.abilityName = UiRuntime.CreateText(face.transform, "Name", string.Empty, UiType.Role.Micro,
                TextAlignmentOptions.Center, UiTheme.TextDim);
            RectTransform nameRect = cell.abilityName.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.sizeDelta = new Vector2(-2f * UiSpace.Medium, NameHeight);
            nameRect.anchoredPosition = new Vector2(0f, NameBottom);

            // The state line. The rail is laid first and never moves; the fill sits inside it and covers it
            // completely whenever the fill is full, so the panel drives ONE graphic instead of two on a normal cell.
            cell.stateRail = UiRuntime.CreatePanel(face.transform, null, UiTheme.PanelLight);
            cell.stateRail.gameObject.name = "StateRail";
            PlaceBar(cell.stateRail.rectTransform);
            cell.stateRail.raycastTarget = false;

            // Starts full and dark, so an empty cell reads "off" instead of flashing a half-drained bar before the
            // fight data arrives.
            //
            // The fill's LENGTH comes from its right anchor, not from Image.fillAmount: a Filled image without a
            // sprite ignores fillAmount and always draws the whole rect - which is why the line used to sit full and
            // unmoving through the whole cooldown. Anchors clip by geometry, so no sprite is needed.
            cell.stateFill = UiRuntime.CreatePanel(cell.stateRail.transform, null, UiTheme.Locked);
            cell.stateFill.gameObject.name = "State";
            PlaceFill(cell.stateFill.rectTransform);

            return cell;
        }

        /// <summary>
        /// The name's bottom edge, in canvas units from the cell's bottom: the plate inset, the state line, and one
        /// hairline between the two, so the line never touches the letters.
        /// </summary>
        private static float NameBottom => UiSpace.Small + BarHeight + UiSpace.Hairline;

        /// <summary>The picture's bottom edge: the name's top plus one tight gap.</summary>
        private static float IconBottom => NameBottom + NameHeight + UiSpace.Tight;

        /// <summary>A thin line along the bottom edge, inset like the text above it.</summary>
        private static void PlaceBar(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(-2f * UiSpace.Medium, BarHeight);
            rect.anchoredPosition = new Vector2(0f, UiSpace.Small);
        }

        /// <summary>The fill covers its whole rail to begin with; the panel moves only its right edge.</summary>
        private static void PlaceFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
