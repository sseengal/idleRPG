using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// One hero's ability cell, built in code so the layout has ONE owner.
    ///
    /// ELI5: a small button. Top line: the hero's name on the left, the state word (READY / QUEUED / 12s) on the
    /// right. Below it: the move's picture on the left, the move's name beside it, and a thin bar along the bottom
    /// that fills while the move rests.
    ///
    /// Why it builds itself instead of using a prefab: the prefab's bands were placed by hand and could not be
    /// reviewed, so a badge added in code landed on top of the text. Now the scene builder AND the running game call
    /// this one method, so a rebuild and play mode cannot disagree. Gaps come from <see cref="UiSpace"/> and every
    /// colour from <see cref="UiTheme"/>, so the guard checks stay green.
    /// </summary>
    public sealed class AbilityCellView : MonoBehaviour
    {
        /// <summary>The ability picture size, in canvas units at the 1080x1920 reference.</summary>
        public const float IconSize = 72f;

        /// <summary>Height of the name/state band. One Micro line plus room for the pixel font's cap.</summary>
        private const float NameBand = 28f;

        /// <summary>Room kept on the right of the name for the longest state word (GUARD 3).</summary>
        private const float StateWidth = 84f;

        /// <summary>The cooldown line under the content: thin, so it reads as an underline, not a second button.</summary>
        private const float BarHeight = UiSpace.Tight;

        [SerializeField] private Button castButton;
        [SerializeField] private UiPlateButton plate;
        [SerializeField] private Image abilityIcon;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI abilityLabel;
        [SerializeField] private TextMeshProUGUI clockLabel;
        [SerializeField] private Image clockTrack;
        [SerializeField] private Image clockFill;

        /// <summary>Builds the runtime row the ability panel drives for this cell.</summary>
        public AbilityPanelUI.Row ToRow(int heroIndex)
        {
            return new AbilityPanelUI.Row
            {
                heroIndex = heroIndex,
                plate = plate,
                abilityIcon = abilityIcon,
                nameLabel = nameLabel,
                abilityLabel = abilityLabel,
                clockLabel = clockLabel,
                clockTrack = clockTrack,
                clockFill = clockFill,
                castButton = castButton
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

            // Top band: hero name on the left, state word on the right. Same baseline, same inset.
            cell.nameLabel = UiRuntime.CreateText(face.transform, "Name", string.Empty, UiType.Role.Micro,
                TextAlignmentOptions.TopLeft, UiTheme.TextDim);
            TopBand(cell.nameLabel.rectTransform, UiSpace.Medium, UiSpace.Medium + StateWidth + UiSpace.Small);

            cell.clockLabel = UiRuntime.CreateText(face.transform, "State", string.Empty, UiType.Role.Micro,
                TextAlignmentOptions.TopRight, UiTheme.Ready);
            RectTransform stateRect = cell.clockLabel.rectTransform;
            stateRect.anchorMin = new Vector2(1f, 1f);
            stateRect.anchorMax = new Vector2(1f, 1f);
            stateRect.pivot = new Vector2(1f, 1f);
            stateRect.sizeDelta = new Vector2(StateWidth, NameBand);
            stateRect.anchoredPosition = new Vector2(-UiSpace.Medium, -UiSpace.Small);

            // Left: the picture. A hair below the optical centre so the name band never crowds it.
            cell.abilityIcon = UiRuntime.CreateIcon("Icon", face.transform);
            RectTransform iconRect = cell.abilityIcon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(IconSize, IconSize);
            iconRect.anchoredPosition = new Vector2(UiSpace.Medium, -UiSpace.Tight);
            cell.abilityIcon.enabled = false;

            // Middle: the move's name, on the icon's centre line and starting after it.
            cell.abilityLabel = UiRuntime.CreateText(face.transform, "Ability", string.Empty, UiType.Role.Small,
                TextAlignmentOptions.Left, UiTheme.Text);
            RectTransform abilityRect = cell.abilityLabel.rectTransform;
            abilityRect.anchorMin = new Vector2(0f, 0.5f);
            abilityRect.anchorMax = new Vector2(1f, 0.5f);
            abilityRect.pivot = new Vector2(0f, 0.5f);
            abilityRect.offsetMin = new Vector2(UiSpace.Medium + IconSize + UiSpace.Small, -NameBand * 0.5f - UiSpace.Tight);
            abilityRect.offsetMax = new Vector2(-UiSpace.Medium, NameBand * 0.5f - UiSpace.Tight);
            UiType.ApplyAuto(cell.abilityLabel, UiType.Role.Small);

            // Bottom: the cooldown line. Track first, fill on top, both hidden until the move is resting.
            cell.clockTrack = UiRuntime.CreatePanel(face.transform, null, UiTheme.PanelLight);
            cell.clockTrack.gameObject.name = "CooldownTrack";
            PlaceBar(cell.clockTrack.rectTransform);
            cell.clockTrack.raycastTarget = false;
            cell.clockTrack.enabled = false;

            cell.clockFill = UiRuntime.CreateFilledImage("Cooldown", face.transform, UiTheme.GoldSoft);
            PlaceBar(cell.clockFill.rectTransform);
            cell.clockFill.enabled = false;

            return cell;
        }

        /// <summary>A top-anchored band with its own left and right insets, so two labels share one baseline.</summary>
        private static void TopBand(RectTransform rect, float left, float right)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-(left + right), NameBand);
            rect.anchoredPosition = new Vector2((left - right) * 0.5f, -UiSpace.Small);
        }

        /// <summary>A thin line along the bottom edge, inset like the text above it.</summary>
        private static void PlaceBar(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(-2f * UiSpace.Medium, BarHeight);
            rect.anchoredPosition = new Vector2(0f, UiSpace.Small);
        }
    }
}
