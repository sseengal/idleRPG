using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Data;

namespace IdleRPG.UI
{
    /// <summary>
    /// One slot on the FORMATION board (the party page's editor tab).
    ///
    /// ELI5: this is a picture of a hero standing in a spot. It only ever shows the idle pose and the name -
    /// it never fights. That is the whole point: the formation tab is a changing room, not a second battle
    /// screen.
    ///
    /// It deliberately does NOT subscribe to <see cref="IdleRPG.Core.GameEvents"/>. The board used to host a
    /// <see cref="HeroUnitView"/> - the battle view - so every slot lunged, blinked, flashed and played hurt
    /// poses whenever a fight was running behind the management pages.
    /// </summary>
    public sealed class FormationSlotView : MonoBehaviour
    {
        private Image icon;
        private TextMeshProUGUI nameLabel;
        private CharacterAnimator animator;
        private HeroData appliedData;

        /// <summary>Runtime wiring (the board builds its slots in code).</summary>
        public void ConfigureRuntime(Image iconImage, TextMeshProUGUI label)
        {
            icon = iconImage;
            nameLabel = label;

            if (icon != null)
            {
                icon.preserveAspect = true;
                icon.color = Color.white;
            }

            Clear("empty");
        }

        /// <summary>Shows a hero: idle pose plus name. Idempotent - re-selecting a slot must not resize it.</summary>
        public void Show(HeroData hero)
        {
            if (hero == null)
            {
                Clear("empty");
                return;
            }

            if (hero == appliedData)
            {
                return;
            }

            appliedData = hero;

            if (icon == null)
            {
                return;
            }

            if (hero.ArtSet != null)
            {
                // Real art: animated frames, no tint (coloured sprites go muddy when tinted).
                EnsureAnimator().SetArt(hero.ArtSet);
                icon.color = Color.white;
            }
            else
            {
                if (animator != null)
                {
                    animator.Clear();
                }

                icon.sprite = hero.HeroIcon;
                icon.color = hero.PlaceholderTint;
            }

            if (nameLabel != null)
            {
                nameLabel.SetText(hero.HeroName);
            }
        }

        /// <summary>Blanks the slot (empty seat, or a hero who moved away).</summary>
        public void Clear(string label = "")
        {
            appliedData = null;

            if (animator != null)
            {
                animator.Clear();
            }

            if (icon != null)
            {
                icon.sprite = null;
                icon.color = new Color(1f, 1f, 1f, 0f);
            }

            if (nameLabel != null)
            {
                nameLabel.SetText(label);
            }
        }

        /// <summary>An idle-only animator: no attack, hurt or death playback on this page.</summary>
        private CharacterAnimator EnsureAnimator()
        {
            if (animator == null && icon != null)
            {
                animator = icon.GetComponent<CharacterAnimator>();
                if (animator == null)
                {
                    animator = icon.gameObject.AddComponent<CharacterAnimator>();
                }

                animator.SetSlotSize(icon.rectTransform.sizeDelta);
                animator.FacingLeft = false;
            }

            return animator;
        }
    }
}
