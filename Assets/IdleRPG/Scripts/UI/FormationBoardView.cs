using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Combat;
using IdleRPG.Core;
using IdleRPG.Data;

namespace IdleRPG.UI
{
    /// <summary>
    /// The party board as a **display**: front and back rank as two vertical columns, one view per slot.
    ///
    /// ELI5: the scoreboard version of the formation - it shows where everyone stands and nothing more. No
    /// buttons, no swapping: moving heroes only happens on the Party screen, so a stray tap during a fight can
    /// never reshuffle your team. Built in code from FormationData, so the board shape is data, not scene surgery.
    /// </summary>
    public sealed class FormationBoardView : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Vector2 slotSize = new Vector2(132f, 150f);
        [SerializeField] private float columnGap = 26f;
        [SerializeField] private float rowGap = 12f;

        [Header("Art (optional)")]
        [SerializeField] private Sprite slotSprite;
        [SerializeField] private Color slotColor = new Color(1f, 1f, 1f, 0.16f);

        private GameManager manager;
        private FloatingDamageTextPool damagePool;
        private Formation formation;

        private Image[] slotBackgrounds;
        private HeroUnitView[] slotViews;
        private RectTransform[] slotRects;

        /// <summary>Builds the board and keeps it in sync with the formation and the fight.</summary>
        public void Build(GameManager gameManager, FloatingDamageTextPool pool)
        {
            manager = gameManager;
            damagePool = pool;
            formation = manager != null ? manager.Formation : null;

            if (formation == null)
            {
                Debug.LogError("[FormationBoardView] No formation on the GameManager; the board cannot be drawn.");
                return;
            }

            BuildSlots();
            Refresh();

            formation.Changed -= OnFormationChanged;
            formation.Changed += OnFormationChanged;
        }

        private void OnDestroy()
        {
            if (formation != null)
            {
                formation.Changed -= OnFormationChanged;
            }
        }

        private void OnFormationChanged()
        {
            Refresh();
        }

        private void BuildSlots()
        {
            FormationData data = formation.Data;
            int slots = formation.SlotCount;

            slotBackgrounds = new Image[slots];
            slotViews = new HeroUnitView[slots];
            slotRects = new RectTransform[slots];

            for (int slot = 0; slot < slots; slot++)
            {
                BuildSlot(data, slot);
            }

            ((RectTransform)transform).sizeDelta = FormationBoardLayout.SizeOf(data, slotSize, columnGap, rowGap);
        }

        private void BuildSlot(FormationData data, int slotIndex)
        {
            GameObject slot = UiRuntime.CreateNode("Slot" + slotIndex, transform);
            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = slotSize;
            rect.anchoredPosition = FormationBoardLayout.PositionOfSlot(data, slotIndex, slotSize, columnGap, rowGap);

            Image background = UiRuntime.CreatePanel(slot.transform, slotSprite, slotColor);
            UiRuntime.Stretch(background.rectTransform);

            Image icon = UiRuntime.CreateIcon("Icon", slot.transform);
            UiRuntime.CenterOn(icon.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(slotSize.x * 0.62f, slotSize.x * 0.62f));

            TextMeshProUGUI nameLabel = UiRuntime.CreateText(slot.transform, "Name", string.Empty, 16f,
                TextAlignmentOptions.Center, Color.white);
            UiRuntime.Anchor(nameLabel.rectTransform, new Vector2(0f, 0.14f), new Vector2(1f, 0.32f));

            HpBarView hpBar = BuildHpBar(slot.transform);

            CanvasGroup group = slot.AddComponent<CanvasGroup>();
            HeroUnitView view = slot.AddComponent<HeroUnitView>();
            view.ConfigureRuntime(-1, icon, hpBar, nameLabel, group);
            view.ClearVisual();

            slotBackgrounds[slotIndex] = background;
            slotViews[slotIndex] = view;
            slotRects[slotIndex] = rect;
        }

        private HpBarView BuildHpBar(Transform parent)
        {
            GameObject bar = UiRuntime.CreateNode("HpBar", parent);
            UiRuntime.Anchor(bar.GetComponent<RectTransform>(), new Vector2(0.10f, 0.02f), new Vector2(0.90f, 0.12f));

            Image track = UiRuntime.CreatePanel(bar.transform, null, new Color(0f, 0f, 0f, 0.5f));
            UiRuntime.Stretch(track.rectTransform);

            Image ghost = UiRuntime.CreateFilledImage("Ghost", bar.transform, new Color(1f, 0.85f, 0.35f, 0.5f));
            Image fill = UiRuntime.CreateFilledImage("Fill", bar.transform, new Color(0.35f, 0.85f, 0.45f, 1f));

            TextMeshProUGUI value = UiRuntime.CreateText(bar.transform, "Value", string.Empty, 13f,
                TextAlignmentOptions.Center, Color.white);
            UiRuntime.Stretch(value.rectTransform);

            HpBarView view = bar.AddComponent<HpBarView>();
            view.ConfigureRuntime(fill, ghost, value);
            return view;
        }

        /// <summary>
        /// Repaints every slot from the board. Empty slots are explicitly **blanked** (icon + name cleared), which
        /// is the bug that made a hero appear in two slots after a move.
        /// </summary>
        private void Refresh()
        {
            if (formation == null || slotViews == null)
            {
                return;
            }

            PartyConfig party = manager != null ? manager.Party : null;
            int heroCount = party != null ? party.Heroes.Count : 0;
            RectTransform[] anchors = new RectTransform[Mathf.Max(1, heroCount)];

            for (int slot = 0; slot < slotViews.Length; slot++)
            {
                HeroUnitView view = slotViews[slot];

                if (view == null)
                {
                    continue;
                }

                int heroIndex = formation.HeroAt(slot);
                HeroData hero = heroIndex >= 0 && party != null ? party.GetHero(heroIndex) : null;

                if (hero == null)
                {
                    view.ClearVisual();
                    continue;
                }

                view.enabled = true;
                view.Configure(heroIndex);
                view.Apply(hero);
                anchors[Mathf.Clamp(heroIndex, 0, anchors.Length - 1)] = slotRects[slot];
            }

            // Damage numbers follow the hero, so the pool gets fresh anchors after every board change.
            damagePool?.SetHeroAnchors(anchors);
        }
    }
}
