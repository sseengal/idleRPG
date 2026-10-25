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
    /// The camp panel: three heroes around a placeholder fire, their HP, and three choices.
    /// It is a view. The camp service owns the timing, the token, the heal and the training.
    /// </summary>
    public sealed class CampPanelUI : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image[] actorImages;
        [SerializeField] private TextMeshProUGUI[] hpLabels;
        [SerializeField] private TextMeshProUGUI tokenLabel;
        [SerializeField] private TextMeshProUGUI trainingLabel;
        [SerializeField] private TextMeshProUGUI autoLabel;
        [SerializeField] private TextMeshProUGUI shortAdLabel;
        [SerializeField] private TextMeshProUGUI longRestLabel;
        [SerializeField] private TextMeshProUGUI trainButtonLabel;
        [SerializeField] private TextMeshProUGUI trainNextLabel;
        [SerializeField] private Button shortFreeButton;
        [SerializeField] private Button shortAdButton;
        [SerializeField] private Button longRestButton;
        [SerializeField] private Button trainButton;

        private GameManager manager;
        private CampService camp;
        private MonetizationService monetization;
        private float autoRemaining;

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

            if (camp != null && camp.IsOpen)
            {
                OnCampOpened();
            }
            else
            {
                SetVisible(false);
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

        private void Update()
        {
            if (camp == null || !camp.IsOpen || group == null || group.alpha <= 0f)
            {
                return;
            }

            if (camp.IsAdPending)
            {
                group.interactable = false;

                if (autoLabel != null)
                {
                    autoLabel.SetText("ad playing…");
                }

                return;
            }

            group.interactable = true;
            autoRemaining -= Time.unscaledDeltaTime;

            if (autoLabel != null)
            {
                autoLabel.SetText($"auto in {Mathf.CeilToInt(autoRemaining)}…");
            }

            if (autoRemaining <= 0f)
            {
                camp.AutoChoose();
            }
        }

        private void OnCampOpened()
        {
            autoRemaining = camp != null ? camp.AutoSeconds : 10f;
            SetVisible(true);
            Refresh();
        }

        private void OnCampClosed()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
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
                animator.FacingLeft = false;

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
                shortFreeButton.onClick.AddListener(() => camp?.ChooseShortRest(false));
            }

            if (shortAdButton != null)
            {
                shortAdButton.onClick.AddListener(() => camp?.ChooseShortRest(true));
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
            shortFreeButton?.onClick.RemoveAllListeners();
            shortAdButton?.onClick.RemoveAllListeners();
            longRestButton?.onClick.RemoveAllListeners();
            trainButton?.onClick.RemoveAllListeners();
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
                            ? "—"
                            : hero.IsAlive
                                ? $"{hero.DisplayName}  {hero.CurrentHealth:0}/{hero.MaxHealth:0}"
                                : $"{hero.DisplayName}  FALLEN");
                    }

                    if (actorImages != null && i < actorImages.Length && actorImages[i] != null)
                    {
                        actorImages[i].color = hero == null || !hero.IsAlive
                            ? new Color(0.55f, 0.55f, 0.55f, 1f)
                            : Color.white;
                    }
                }
            }

            if (tokenLabel != null && camp != null)
            {
                tokenLabel.SetText(camp.Tokens == 1
                    ? "1 LONG-REST TOKEN"
                    : $"{camp.Tokens} LONG-REST TOKENS");
            }

            if (trainingLabel != null && manager != null)
            {
                trainingLabel.SetText($"TRAINED +{manager.RunTrainAttackBonus} ATK · +{manager.RunTrainDefenseBonus} DEF");
            }

            if (longRestButton != null && camp != null)
            {
                longRestButton.interactable = camp.CanLongRest;

                if (longRestLabel != null)
                {
                    longRestLabel.SetText(camp.CanLongRest ? "30% + REVIVE" : "TOKEN SPENT");
                }
            }

            if (shortAdLabel != null)
            {
                shortAdLabel.SetText(monetization != null && monetization.AdsDisabledByNoAds
                    ? "20% FREE"
                    : "20% (AD)");
            }

            if (camp != null)
            {
                bool nextAttack = camp.TrainNextIsAttack;

                if (trainButtonLabel != null)
                {
                    trainButtonLabel.SetText(nextAttack ? "+1 ATK" : "+1 DEF");
                }

                if (trainNextLabel != null)
                {
                    trainNextLabel.SetText(nextAttack ? "next: DEF" : "next: ATK");
                }
            }

            if (autoLabel != null)
            {
                autoLabel.SetText($"auto in {Mathf.CeilToInt(autoRemaining)}…");
            }
        }
    }
}
