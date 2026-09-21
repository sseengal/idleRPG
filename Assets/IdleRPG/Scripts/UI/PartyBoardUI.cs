using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Combat;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Sim;

namespace IdleRPG.UI
{
    /// <summary>
    /// The party board as an **editor**: the only place in the game where a hero is moved.
    ///
    /// ELI5: the changing room board. Tap a hero (it lights up), tap where it should stand, done - the old slot is
    /// emptied and the new one filled. No drag, no presets, no automatic arranging: the player decides, and the
    /// battle screen only mirrors the result.
    /// </summary>
    public sealed class PartyBoardUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Vector2 slotSize = new Vector2(120f, 138f);
        [SerializeField] private float columnGap = 24f;
        [SerializeField] private float rowGap = 12f;

        [Header("Art (optional)")]
        [SerializeField] private Sprite slotSprite;
        [SerializeField] private Color slotColor = new Color(1f, 1f, 1f, 0.75f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.82f, 0.3f, 1f);

        private GameManager manager;
        private Formation formation;

        private Image[] slotBackgrounds;
        private HeroUnitView[] slotViews;
        private Button[] slotButtons;

        private int selectedSlot = -1;

        /// <summary>Builds the board and subscribes to the formation.</summary>
        public void Build(GameManager gameManager)
        {
            manager = gameManager;
            formation = manager != null ? manager.Formation : null;

            if (formation == null)
            {
                Debug.LogError("[PartyBoardUI] No formation on the GameManager; the board cannot be drawn.");
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
            selectedSlot = -1;
            Refresh();
        }

        private void BuildSlots()
        {
            FormationData data = formation.Data;
            int slots = formation.SlotCount;

            slotBackgrounds = new Image[slots];
            slotViews = new HeroUnitView[slots];
            slotButtons = new Button[slots];

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

            Image background = UiRuntime.CreatePanel(slot.transform, slotSprite, slotColor, raycast: true);
            UiRuntime.Stretch(background.rectTransform);

            Button button = slot.AddComponent<Button>();
            button.targetGraphic = background;
            int captured = slotIndex;
            button.onClick.AddListener(() => OnSlotTapped(captured));

            Image icon = UiRuntime.CreateIcon("Icon", slot.transform);
            UiRuntime.CenterOn(icon.rectTransform, new Vector2(0.5f, 0.60f), new Vector2(slotSize.x * 0.60f, slotSize.x * 0.60f));

            TextMeshProUGUI nameLabel = UiRuntime.CreateText(slot.transform, "Name", string.Empty, 16f,
                TextAlignmentOptions.Center, Color.white);
            UiRuntime.Anchor(nameLabel.rectTransform, new Vector2(0f, 0.10f), new Vector2(1f, 0.28f));

            CanvasGroup group = slot.AddComponent<CanvasGroup>();
            HeroUnitView view = slot.AddComponent<HeroUnitView>();
            view.ConfigureRuntime(-1, icon, null, nameLabel, group);
            view.ClearVisual("empty");

            slotBackgrounds[slotIndex] = background;
            slotViews[slotIndex] = view;
            slotButtons[slotIndex] = button;
        }

        // ------------------------------------------------------------------
        // Taps: pick a hero, then pick its slot
        // ------------------------------------------------------------------
        private void OnSlotTapped(int slotIndex)
        {
            if (formation == null)
            {
                return;
            }

            if (selectedSlot < 0)
            {
                if (formation.HeroAt(slotIndex) >= 0)
                {
                    selectedSlot = slotIndex;
                    Refresh();
                }

                return;
            }

            if (selectedSlot == slotIndex)
            {
                selectedSlot = -1;
                Refresh();
                return;
            }

            int heroIndex = formation.HeroAt(selectedSlot);

            if (heroIndex >= 0 && formation.TryMove(heroIndex, slotIndex))
            {
                GameEvents.RaiseToast(Describe(heroIndex) + " -> " + Describe(slotIndex));
            }

            selectedSlot = -1;
            Refresh();
        }

        private string Describe(int slotIndex)
        {
            int heroIndex = formation.HeroAt(slotIndex);
            HeroData hero = heroIndex >= 0 && manager != null && manager.Party != null ? manager.Party.GetHero(heroIndex) : null;
            string rank = formation.Data.RowOfSlot(slotIndex) == CombatRow.Front ? "front" : "back";

            return hero != null ? hero.HeroName : rank + " slot";
        }

        // ------------------------------------------------------------------
        // Refresh
        // ------------------------------------------------------------------
        /// <summary>Repaints the board. Empty slots are blanked, not just disabled.</summary>
        public void Refresh()
        {
            if (formation == null || slotViews == null)
            {
                return;
            }

            PartyConfig party = manager != null ? manager.Party : null;

            for (int slot = 0; slot < slotViews.Length; slot++)
            {
                HeroUnitView view = slotViews[slot];

                if (view == null)
                {
                    continue;
                }

                if (slotBackgrounds[slot] != null)
                {
                    slotBackgrounds[slot].color = selectedSlot == slot ? selectedColor : slotColor;
                }

                int heroIndex = formation.HeroAt(slot);
                HeroData hero = heroIndex >= 0 && party != null ? party.GetHero(heroIndex) : null;

                if (hero == null)
                {
                    view.ClearVisual("empty");
                    continue;
                }

                view.enabled = true;
                view.Configure(heroIndex);
                view.Apply(hero);
            }
        }

        /// <summary>Slot currently picked by the player (for the panel's hint line).</summary>
        public int SelectedSlot => selectedSlot;
    }
}
