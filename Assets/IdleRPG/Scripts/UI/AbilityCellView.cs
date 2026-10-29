using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// The visual half of one ability cell. The prefab owns the portrait, labels, clock and cooldown bar; the
    /// scene builder hands these references to <see cref="AbilityPanelUI"/> as one <see cref="AbilityPanelUI.Row"/>.
    ///
    /// The panel still owns the text and fill values every frame. This component only carries the references, so
    /// the layout can change in the prefab without touching code.
    /// </summary>
    public sealed class AbilityCellView : MonoBehaviour
    {
        [SerializeField] private Button castButton;
        [SerializeField] private Image portrait;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI abilityLabel;
        [SerializeField] private TextMeshProUGUI clockLabel;
        [SerializeField] private Image clockFill;

        /// <summary>Builds the runtime row the ability panel drives for this cell.</summary>
        public AbilityPanelUI.Row ToRow(int heroIndex)
        {
            return new AbilityPanelUI.Row
            {
                heroIndex = heroIndex,
                portrait = portrait,
                nameLabel = nameLabel,
                abilityLabel = abilityLabel,
                clockLabel = clockLabel,
                clockFill = clockFill,
                castButton = castButton
            };
        }
    }
}
