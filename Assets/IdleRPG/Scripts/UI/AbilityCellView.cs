using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// One hero's ability cell, built in code so the layout has ONE owner.
    ///
    /// ELI5: one wide button per hero. On the left is a big square picture of that hero's move, as tall as the button
    /// itself. On the right are the hero's name, the hero's health numbers and the hero's health bar. The bar slides
    /// to its new length instead of jumping. The picture fills with colour as the move comes back, so a resting move
    /// reads grey and a ready one is bright. The button wears the SAME blue plate as every other button in the game.
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

        /// <summary>
        /// Zoom applied to that art. A whole number, so no pixel column ends up half a pixel wide. Five is the
        /// largest zoom whose picture still leaves room for two lines of words beside it: 34 x 5 = 170.
        /// </summary>
        private const float IconZoom = 5f;

        /// <summary>The picture size in canvas units: 34 x 5 = 170. It IS the cell's height, minus one inset.</summary>
        public const float IconSize = ArtPixels * IconZoom;

        /// <summary>
        /// The health bar's height comes from the gauge role: tall enough to read at a glance beside three other
        /// bars, and the same height the arena nameplates use.
        /// </summary>
        private const float BarHeight = UiSpace.HealthBarHeight;

        /// <summary>One line of Micro numbers: the health pair sits on its own line, never inside the bar.</summary>
        private static readonly float NumbersHeight = UiType.LineHeight(UiType.Role.Micro);

        /// <summary>Numbers to bar. They belong to the bar, so they sit closer to it than to the words.</summary>
        private const float NumbersGap = UiSpace.Hairline;

        /// <summary>
        /// The card's inset. The picture and the words both start this far inside the plate, so neither touches the
        /// plate's 9-slice border. Base (10), not CardPadding (12): at 12 the words column falls to 134 units and the
        /// health pair needs 133 - one unit of margin is not enough.
        /// </summary>
        private const float CellInset = UiSpace.Base;

        /// <summary>The words' left edge: the inset, the picture, then one gap - so the gap is the same all round.</summary>
        private const float ColumnLeft = CellInset + IconSize + CellInset;

        /// <summary>One line of the hero's name. Whose button this is: the only words the button carries.</summary>
        private static readonly float HeroNameHeight = UiType.LineHeight(UiType.Role.Small);

        /// <summary>
        /// The gap between the hero's name and the health block. It is the only gap between words and numbers, so it
        /// is one step wider than the hairline the two text lines used to share.
        /// </summary>
        private const float GroupGap = UiSpace.Base;

        /// <summary>
        /// The whole words block plus its health block, measured from the bottom of the bar to the top of the name.
        /// </summary>
        private static readonly float StackHeight =
            HeroNameHeight + GroupGap + NumbersHeight + NumbersGap + BarHeight;

        // The heights below are measured from the card's middle, so the block stays centred on the picture
        // whatever height the strip gives the card. Built bottom-up, so one gap change moves everything above it.

        /// <summary>The health bar's middle, from the card's middle.</summary>
        private static readonly float BarY = -StackHeight * 0.5f + BarHeight * 0.5f;

        /// <summary>The numbers' middle: one line above the bar, so the pair reads as that bar's value.</summary>
        private static readonly float NumbersY = BarY + BarHeight * 0.5f + NumbersGap + NumbersHeight * 0.5f;

        /// <summary>The hero's name's middle, from the card's middle.</summary>
        private static readonly float HeroNameY = NumbersY + NumbersHeight * 0.5f + GroupGap + HeroNameHeight * 0.5f;

        [SerializeField] private Button castButton;
        [SerializeField] private UiPlateButton plate;

        /// <summary>The hero's own name - identity, not state, so it does not dim with the countdown.</summary>
        [SerializeField] private TextMeshProUGUI heroName;

        /// <summary>The grey base copy of the picture. Its UIEffect drains it to luminance at build time.</summary>
        [SerializeField] private Image abilityIcon;

        /// <summary>The full-colour copy of the same art, cropped by <see cref="abilityIconWipe"/>.</summary>
        [SerializeField] private Image abilityIconColour;

        /// <summary>The curtain over the colour copy: its width IS the countdown, opening from the left edge.</summary>
        [SerializeField] private RectTransform abilityIconWipe;

        /// <summary>This hero's health: the same widget the arena units wear, so one hero never reads two ways.</summary>
        [SerializeField] private HpBarView hpBar;

        /// <summary>Builds the runtime row the ability panel drives for this cell.</summary>
        public AbilityPanelUI.Row ToRow(int heroIndex)
        {
            return new AbilityPanelUI.Row
            {
                heroIndex = heroIndex,
                plate = plate,
                abilityIcon = abilityIcon,
                abilityIconColour = abilityIconColour,
                abilityIconWipe = abilityIconWipe,
                heroName = heroName,
                hpBar = hpBar,
                castButton = castButton,

                // The picture starts with its colour showing, so the first frame shows a ready hero instead of a
                // half-drawn curtain rising out of nowhere.
                shownFill = 1f
            };
        }

        /// <summary>
        /// Builds one cell under <paramref name="parent"/>. The whole cell is the button, so a tap anywhere casts.
        /// </summary>
        public static AbilityCellView Build(Transform parent, int index)
        {
            // The cell is a BUTTON with the app's own face: the shared ui_frame plate in ButtonBlue, the colour every
            // other button in the game wears. It carries the press squash and the grayscale lock for free.
            Image face = UiRuntime.Plate(parent, "Hero" + index, Vector2.zero, Vector2.one, UiTheme.ButtonBlue, raycast: true);
            face.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            Button button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;

            UIEffect effect = face.gameObject.AddComponent<UIEffect>();
            UiPlateButton plateView = face.gameObject.AddComponent<UiPlateButton>();
            plateView.Bind(face.rectTransform, face, null, effect, button, UiTheme.ButtonBlue, UiTheme.SelectedBlue);

            AbilityCellView cell = face.gameObject.AddComponent<AbilityCellView>();
            cell.castButton = button;
            cell.plate = plateView;

            // The picture is TWO copies of the same art, stacked on one holder:
            //   Art    - the grey copy. One UIEffect drains it to luminance AT BUILD, then is never touched.
            //   Wipe   - a RectMask2D curtain whose width is the countdown.
            //     Colour - the full-colour copy, cropped by that curtain.
            //
            // Why a mask and not Image.fillAmount: fillAmount only crops U/Vs along the shape the picture was cut
            // with. A mask crops by geometry, so the colour copy keeps its true size and is REVEALED from the left
            // instead of being squashed as the move comes back. The base keeps the old .color dim rule, so the only
            // new cue is the colour arriving.
            //
            // The picture is a square as tall as the card's content area, pinned to the LEFT inset and centred
            // vertically. Nothing sits above or below it any more: the art owns the full height, and the words own
            // the strip to its right.
            RectTransform iconHolder = UiRuntime.CreateNode("Icon", face.transform).GetComponent<RectTransform>();
            iconHolder.anchorMin = new Vector2(0f, 0.5f);
            iconHolder.anchorMax = new Vector2(0f, 0.5f);
            iconHolder.pivot = new Vector2(0f, 0.5f);
            iconHolder.sizeDelta = new Vector2(IconSize, IconSize);
            iconHolder.anchoredPosition = new Vector2(CellInset, 0f);

            cell.abilityIcon = UiRuntime.CreateIcon("Art", iconHolder);
            UiRuntime.Stretch(cell.abilityIcon.rectTransform);
            cell.abilityIcon.enabled = false;

            // Set once. Changing toneFilter rebuilds the mesh, so doing it per frame would rebuild the strip every
            // frame; the resting look costs nothing after this line.
            UIEffect grey = cell.abilityIcon.gameObject.AddComponent<UIEffect>();
            grey.toneFilter = ToneFilter.Grayscale;
            grey.toneIntensity = 1f;

            cell.abilityIconWipe = UiRuntime.CreateNode("Wipe", iconHolder).GetComponent<RectTransform>();
            cell.abilityIconWipe.gameObject.AddComponent<RectMask2D>();
            PlaceWipe(cell.abilityIconWipe);

            cell.abilityIconColour = UiRuntime.CreateIcon("Colour", cell.abilityIconWipe);
            PlaceColour(cell.abilityIconColour.rectTransform);
            cell.abilityIconColour.enabled = false;

            // The words column: everything to the right of the picture, inset the same amount from the card's edge.
            // It is a plain node with no picture of its own, so the panel can hide the words and leave the plate.
            RectTransform column = UiRuntime.CreateNode("Words", face.transform).GetComponent<RectTransform>();
            column.anchorMin = Vector2.zero;
            column.anchorMax = Vector2.one;
            column.offsetMin = new Vector2(ColumnLeft, 0f);
            column.offsetMax = new Vector2(-CellInset, 0f);

            // Whose button this is. One line, centred vertically in the block: the player reads the hero first, and
            // three buttons in a row must be tellable apart at a glance.
            cell.heroName = UiRuntime.CreateText(column, "HeroName", string.Empty, UiType.Role.Small,
                TextAlignmentOptions.Left, UiTheme.Text);
            PlaceInColumn(cell.heroName.rectTransform, HeroNameY, HeroNameHeight);

            // The hero's health: the SAME widget the arena units wear (HpBarView), so a hero reads the same in both
            // places. The numbers sit on their own line just above the bar, right-aligned to its end: at Micro the
            // pair "493/494" is 133 units wide, which leaves no room for a bar beside it in a 146-unit column.
            TextMeshProUGUI value = UiRuntime.CreateText(column, "Value", string.Empty,
                UiType.Role.Micro, TextAlignmentOptions.MidlineRight, UiTheme.Text);
            PlaceInColumn(value.rectTransform, NumbersY, NumbersHeight);

            GameObject barObject = UiRuntime.CreateNode("HpBar", column);
            RectTransform barRect = barObject.GetComponent<RectTransform>();
            PlaceInColumn(barRect, BarY, BarHeight);

            // Near-black track: the empty part of a bar must still look like a bar on any backdrop, and the bright
            // fill must have an edge to stop against.
            Image track = UiRuntime.CreatePanel(barObject.transform, null, UiTheme.PlateShade);
            track.gameObject.name = "Track";
            track.raycastTarget = false;
            UiRuntime.Stretch(track.rectTransform);

            // The pale tail that chases a lost chunk, so the size of a hit is readable at a glance.
            Image ghost = UiRuntime.CreatePanel(barObject.transform, null, UiTheme.HpGhost);
            ghost.gameObject.name = "Ghost";
            ghost.raycastTarget = false;
            UiRuntime.Stretch(ghost.rectTransform);

            Image fill = UiRuntime.CreatePanel(barObject.transform, null, UiTheme.HpHealthy);
            fill.gameObject.name = "Fill";
            fill.raycastTarget = false;
            UiRuntime.Stretch(fill.rectTransform);

            cell.hpBar = barObject.AddComponent<HpBarView>();
            cell.hpBar.ConfigureRuntime(fill, ghost, value);

            return cell;
        }

        /// <summary>
        /// Lays one child across the whole words column at a height measured from the column's middle. Using the
        /// middle, not the bottom, keeps the block centred on the picture whatever height the strip gives the card;
        /// using a stretched width lets the words use every unit the picture leaves.
        /// </summary>
        private static void PlaceInColumn(RectTransform rect, float centreY, float height)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = new Vector2(0f, centreY);
        }

        /// <summary>
        /// The curtain covers the whole picture to begin with, so a cell whose data has not arrived shows its art
        /// whole rather than half-drawn. The panel moves only its right edge, from the left.
        /// </summary>
        private static void PlaceWipe(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// The colour copy keeps its TRUE width and is pinned to the curtain's left edge. That is the whole trick: a
        /// narrow curtain then CROPS the art instead of squashing it into a narrower picture.
        /// </summary>
        private static void PlaceColour(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(IconSize, 0f);
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
