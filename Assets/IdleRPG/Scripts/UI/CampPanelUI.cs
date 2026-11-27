using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Sim;

namespace IdleRPG.UI
{
    /// <summary>
    /// The camp interlude as part of the battle page: a still scene in the viewport, choices in the
    /// ability band, and the battle log untouched below.
    ///
    /// ELI5: while the party camps, the fighting area becomes a campfire scene and the ability buttons
    /// become camp buttons. When a choice is made, the camp buttons go away and the ability buttons return.
    /// The camp waits for a tap; nothing auto-proceeds.
    /// </summary>
    public sealed class CampPanelUI : MonoBehaviour
    {
        [Header("Battle page")]
        [SerializeField] private GameObject campView;
        [SerializeField] private GameObject campStrip;
        [SerializeField] private GameObject abilityStrip;
        [SerializeField] private GameObject formationBoard;
        [SerializeField] private GameObject enemyStack;
        [SerializeField] private CanvasGroup campStripGroup;

        [Header("Camp view")]
        [SerializeField] private Image[] actorImages;
        [SerializeField] private TextMeshProUGUI[] hpLabels;
        [SerializeField] private HpBarView[] hpBars;

        [Header("Camp strip")]
        [SerializeField] private Button shortFreeButton;
        [SerializeField] private Button longRestButton;
        [SerializeField] private Button trainButton;
        [SerializeField] private TextMeshProUGUI longRestLabel;
        [SerializeField] private TextMeshProUGUI trainButtonLabel;

        /// <summary>Seconds between the three camp idle starts, so they never breathe in lockstep.</summary>
        private const float CampPhaseStep = 0.12f;

        private GameManager manager;
        private CampService camp;
        private MonetizationService monetization;

        private void Start()
        {
            manager = HudController.Instance != null ? HudController.Instance.GameManager : null;
            camp = manager != null ? manager.Camp : null;
            monetization = manager != null ? manager.Monetization : null;

            BuildActors();
            WireButtons();

            if (camp != null)
            {
                camp.Opened += OnCampOpened;
                camp.Closed += OnCampClosed;
            }

            ApplyState(camp != null && camp.IsOpen);

            if (camp != null && camp.IsOpen)
            {
                Refresh();
            }
        }

        private void OnDestroy()
        {
            if (camp != null)
            {
                camp.Opened -= OnCampOpened;
                camp.Closed -= OnCampClosed;
            }

            RemoveButtonListeners();
        }

        /// <summary>Only syncs the ad guard. There is deliberately no timer and no auto-choice.</summary>
        private void Update()
        {
            if (camp == null || !camp.IsOpen || campStripGroup == null)
            {
                return;
            }

            bool ready = !camp.IsAdPending;

            if (campStripGroup.interactable != ready)
            {
                campStripGroup.interactable = ready;
            }
        }

        private void OnCampOpened()
        {
            ApplyState(true);
            Refresh();
        }

        private void OnCampClosed()
        {
            ApplyState(false);
        }

        private void ApplyState(bool open)
        {
            if (campView != null)
            {
                campView.SetActive(open);
            }

            if (campStrip != null)
            {
                campStrip.SetActive(open);
            }

            if (abilityStrip != null)
            {
                abilityStrip.SetActive(!open);
            }

            if (formationBoard != null)
            {
                formationBoard.SetActive(!open);
            }

            if (enemyStack != null)
            {
                enemyStack.SetActive(!open);
            }

            if (open && campStripGroup != null)
            {
                campStripGroup.interactable = camp == null || !camp.IsAdPending;
            }
        }

        private void BuildActors()
        {
            if (actorImages == null || manager == null || manager.Party == null)
            {
                return;
            }

            for (int i = 0; i < actorImages.Length; i++)
            {
                Image actor = actorImages[i];

                if (actor == null || i >= manager.Party.Heroes.Count)
                {
                    continue;
                }

                CharacterAnimator animator = actor.GetComponent<CharacterAnimator>();

                if (animator == null)
                {
                    animator = actor.gameObject.AddComponent<CharacterAnimator>();
                }

                animator.SetSlotSize(actor.rectTransform.sizeDelta);
                // The left hero faces right; the mage (third) is flipped to face the fire; the centre keeps its
                // native facing (side-view art cannot look down at a fire below it).
                animator.FacingLeft = i == 2;
                animator.PhaseOffset = i * CampPhaseStep;

                HeroData hero = manager.Party.GetHero(i);

                if (hero != null)
                {
                    animator.SetArt(hero.ArtSet);
                }
            }
        }

        private void WireButtons()
        {
            RemoveButtonListeners();

            if (shortFreeButton != null)
            {
                shortFreeButton.onClick.AddListener(() => camp?.ChooseShortRest());
            }

            if (longRestButton != null)
            {
                longRestButton.onClick.AddListener(() => camp?.ChooseLongRest());
            }

            if (trainButton != null)
            {
                trainButton.onClick.AddListener(() => camp?.ChooseTrain());
            }
        }

        private void RemoveButtonListeners()
        {
            if (shortFreeButton != null)
            {
                shortFreeButton.onClick.RemoveAllListeners();
            }

            if (longRestButton != null)
            {
                longRestButton.onClick.RemoveAllListeners();
            }

            if (trainButton != null)
            {
                trainButton.onClick.RemoveAllListeners();
            }
        }

        private void Refresh()
        {
            Combatant[] heroes = manager != null && manager.Combat != null && manager.Combat.Simulator != null
                ? manager.Combat.Simulator.Heroes
                : null;

            if (hpLabels != null)
            {
                for (int i = 0; i < hpLabels.Length; i++)
                {
                    Combatant hero = heroes != null && i < heroes.Length ? heroes[i] : null;

                    if (hpLabels[i] != null)
                    {
                        hpLabels[i].SetText(hero == null
                            ? "-"
                            : hero.IsAlive
                                ? hero.DisplayName.ToUpperInvariant()
                                : "FALLEN");
                    }

                    if (hpBars != null && i < hpBars.Length && hpBars[i] != null)
                    {
                        if (hero != null && hero.MaxHealth > 0d)
                        {
                            hpBars[i].SetFill((float)(hero.CurrentHealth / hero.MaxHealth), instant: true);
                            hpBars[i].SetValueLabel(hero.CurrentHealth, hero.MaxHealth);
                        }
                        else
                        {
                            hpBars[i].SetFill(0f, instant: true);
                            hpBars[i].SetLabel("-");
                        }
                    }

                    if (actorImages != null && i < actorImages.Length && actorImages[i] != null)
                    {
                        actorImages[i].color = hero == null || !hero.IsAlive
                            ? UiTheme.InkDisabled
                            : Color.white;
                    }
                }
            }

            if (longRestLabel != null)
            {
                bool free = monetization != null && monetization.AdsDisabledByNoAds;
                longRestLabel.SetText(free ? "+20% HP (Free)" : "+20% HP (Ad)");
            }

            if (camp != null && trainButtonLabel != null)
            {
                trainButtonLabel.SetText(camp.TrainNextIsAttack ? "+1 ATK" : "+1 DEF");
            }
        }
    }
}
