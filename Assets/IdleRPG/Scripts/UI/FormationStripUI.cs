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
    /// The party board on the battle screen: one tappable view per formation slot, laid out in rows.
    ///
    /// ELI5: instead of three fixed lanes, this draws the actual pitch - a front row and a back row - and lets you
    /// move a hero by tapping it and then tapping where it should stand. Like the shop rows, it builds itself in
    /// code from the formation data, so a board that grows or changes shape needs no scene rebuild.
    /// </summary>
    public sealed class FormationStripUI : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("Slot size in reference pixels. The battle strip is compact; the Team board is wider.")]
        [SerializeField] private Vector2 slotSize = new Vector2(150f, 186f);

        [SerializeField] private float rowSpacing = 18f;
        [SerializeField] private float columnSpacing = 14f;
        [SerializeField] private float rowLabelWidth = 92f;

        [Header("Options")]
        [Tooltip("Show HP bars (battle strip) or keep slots clean (Team board).")]
        [SerializeField] private bool showHealthBars = true;

        [Tooltip("Show the hero's role under its name (Team board).")]
        [SerializeField] private bool showRoleTags;

        [Tooltip("Allow tap-hero-then-tap-slot swapping.")]
        [SerializeField] private bool allowSwapping = true;

        [Header("Art (optional)")]
        [Tooltip("Background sprite for slots; empty means a flat colour, which is fine as a placeholder.")]
        [SerializeField] private Sprite slotBackgroundSprite;

        [SerializeField] private Color slotColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.55f, 0.55f, 0.55f, 0.45f);

        private GameManager manager;
        private FloatingDamageTextPool damagePool;
        private Formation formation;

        private Image[] slotBackgrounds;
        private HeroUnitView[] slotViews;
        private TextMeshProUGUI[] slotLabels;
        private Button[] slotButtons;
        private RectTransform[] slotRects;

        private int selectedSlot = -1;

        /// <summary>
        /// Display switch for the two board flavours: the battle strip shows health, the Team board shows roles.
        /// Called before <see cref="Build"/>.
        /// </summary>
        public void SetDisplayOptions(bool health, bool roles)
        {
            showHealthBars = health;
            showRoleTags = roles;
        }

        /// <summary>
        /// Builds (or rebuilds) the board and keeps it in sync with the formation. A null <paramref name="pool"/> means
        /// "display only": the board will not touch the floating damage numbers (the Team board does this).
        /// </summary>
        public void Build(GameManager gameManager, FloatingDamageTextPool pool)
        {
            manager = gameManager;
            damagePool = pool;
            formation = manager != null ? manager.Formation : null;

            if (formation == null)
            {
                Debug.LogError("[FormationStripUI] No formation on the GameManager; the board cannot be drawn.");
                return;
            }

            BuildSlots();
            RefreshAll();

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
            RefreshAll();
        }

        // ------------------------------------------------------------------
        // Layout
        // ------------------------------------------------------------------
        private void BuildSlots()
        {
            FormationData data = formation.Data;
            int slots = formation.SlotCount;

            slotBackgrounds = new Image[slots];
            slotViews = new HeroUnitView[slots];
            slotLabels = new TextMeshProUGUI[slots];
            slotButtons = new Button[slots];
            slotRects = new RectTransform[slots];

            for (int row = 0; row < data.Rows; row++)
            {
                BuildRowLabel(row);
            }

            for (int row = 0; row < data.Rows; row++)
            {
                for (int column = 0; column < data.Columns; column++)
                {
                    BuildSlot(row * data.Columns + column, row, column);
                }
            }

            RectTransform root = (RectTransform)transform;
            root.sizeDelta = new Vector2(
                rowLabelWidth + data.Columns * (slotSize.x + columnSpacing),
                data.Rows * slotSize.y + (data.Rows - 1) * rowSpacing);
        }

        private void BuildRowLabel(int row)
        {
            GameObject label = UiRuntime.CreateNode("RowLabel" + row, transform);
            RectTransform rect = label.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(rowLabelWidth, slotSize.y);
            rect.anchoredPosition = new Vector2(0f, -row * (slotSize.y + rowSpacing));

            string rowName = row == 0 ? "FRONT" : (row == 1 ? "BACK" : "ROW " + row);
            TextMeshProUGUI text = UiRuntime.CreateText(label.transform, "Text", rowName, 20f,
                TextAlignmentOptions.MidlineLeft, new Color(1f, 1f, 1f, 0.75f));
            UiRuntime.Stretch(text.rectTransform);
        }

        private void BuildSlot(int slotIndex, int row, int column)
        {
            GameObject slot = UiRuntime.CreateNode("Slot" + slotIndex, transform);
            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = slotSize;
            rect.anchoredPosition = new Vector2(
                rowLabelWidth + column * (slotSize.x + columnSpacing),
                -row * (slotSize.y + rowSpacing));

            Image background = UiRuntime.CreatePanel(slot.transform, slotBackgroundSprite, slotColor, raycast: true);
            UiRuntime.Stretch(background.rectTransform);

            Button button = slot.AddComponent<Button>();
            button.targetGraphic = background;
            int captured = slotIndex;
            button.onClick.AddListener(() => OnSlotTapped(captured));

            Image icon = UiRuntime.CreateIcon("Icon", slot.transform);
            UiRuntime.CenterOn(icon.rectTransform, new Vector2(0.5f, 0.66f),
                new Vector2(slotSize.x * 0.58f, slotSize.x * 0.58f));

            // Two texts on purpose: HeroUnitView owns "Name" (it rewrites it with the hero's name on every
            // refresh), and the strip owns "Caption" for the role tag / row hint / LOCKED.
            TextMeshProUGUI nameLabel = UiRuntime.CreateText(slot.transform, "Name", "-", 17f,
                TextAlignmentOptions.Center, Color.white);
            UiRuntime.Anchor(nameLabel.rectTransform, new Vector2(0f, 0.36f), new Vector2(1f, 0.52f));

            TextMeshProUGUI caption = UiRuntime.CreateText(slot.transform, "Caption", string.Empty, 14f,
                TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.7f));
            UiRuntime.Anchor(caption.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.16f));

            HpBarView hpBar = BuildHpBar(slot.transform);

            CanvasGroup group = slot.AddComponent<CanvasGroup>();
            HeroUnitView view = slot.AddComponent<HeroUnitView>();
            view.ConfigureRuntime(-1, icon, hpBar, nameLabel, group);
            view.SetHealthVisible(showHealthBars);

            slotBackgrounds[slotIndex] = background;
            slotViews[slotIndex] = view;
            slotLabels[slotIndex] = caption;
            slotButtons[slotIndex] = button;
            slotRects[slotIndex] = rect;
        }

        private HpBarView BuildHpBar(Transform parent)
        {
            GameObject bar = UiRuntime.CreateNode("HpBar", parent);
            UiRuntime.Anchor(bar.GetComponent<RectTransform>(), new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.30f));

            Image track = UiRuntime.CreatePanel(bar.transform, null, new Color(0f, 0f, 0f, 0.55f));
            UiRuntime.Stretch(track.rectTransform);

            Image ghost = UiRuntime.CreateFilledImage("Ghost", bar.transform, new Color(1f, 0.85f, 0.35f, 0.55f));
            Image fill = UiRuntime.CreateFilledImage("Fill", bar.transform, new Color(0.35f, 0.85f, 0.45f, 1f));

            TextMeshProUGUI value = UiRuntime.CreateText(bar.transform, "Value", string.Empty, 14f,
                TextAlignmentOptions.Center, Color.white);
            UiRuntime.Stretch(value.rectTransform);

            HpBarView view = bar.AddComponent<HpBarView>();
            view.ConfigureRuntime(fill, ghost, value);
            return view;
        }

        // ------------------------------------------------------------------
        // Taps
        // ------------------------------------------------------------------
        /// <summary>Tap a hero, then tap where it should stand. Tapping the same slot again cancels.</summary>
        private void OnSlotTapped(int slotIndex)
        {
            if (!allowSwapping || formation == null)
            {
                return;
            }

            int stage = manager != null ? manager.HighestStageReached : 1;

            if (!formation.Data.IsSlotUnlocked(slotIndex, stage))
            {
                GameEvents.RaiseToast("That spot opens later");
                return;
            }

            if (selectedSlot < 0)
            {
                // Nothing selected yet: only a hero can start a move.
                if (formation.HeroAt(slotIndex) >= 0)
                {
                    selectedSlot = slotIndex;
                    RefreshAll();
                }

                return;
            }

            if (selectedSlot == slotIndex)
            {
                selectedSlot = -1;
                RefreshAll();
                return;
            }

            // Formation.Changed marks the save dirty and re-stamps the live fight, so nothing else is needed here.
            formation.TrySwapSlots(selectedSlot, slotIndex);
            selectedSlot = -1;
            RefreshAll();
        }

        // ------------------------------------------------------------------
        // Refresh
        // ------------------------------------------------------------------
        /// <summary>Repaints every slot from the formation (also used by the Team panel after a preset).</summary>
        public void RefreshAll()
        {
            if (formation == null || slotViews == null)
            {
                return;
            }

            int stage = manager != null ? manager.HighestStageReached : 1;
            PartyConfig party = manager != null ? manager.Party : null;
            int heroSlots = party != null ? party.Heroes.Count : 0;
            RectTransform[] anchors = new RectTransform[Mathf.Max(1, heroSlots)];

            for (int slot = 0; slot < slotViews.Length; slot++)
            {
                bool unlocked = formation.Data.IsSlotUnlocked(slot, stage);
                int heroIndex = formation.HeroAt(slot);
                HeroData hero = heroIndex >= 0 && party != null ? party.GetHero(heroIndex) : null;

                if (slotButtons[slot] != null)
                {
                    slotButtons[slot].interactable = unlocked;
                }

                if (slotBackgrounds[slot] != null)
                {
                    slotBackgrounds[slot].color = !unlocked
                        ? lockedColor
                        : (selectedSlot == slot ? selectedColor : slotColor);
                }

                if (slotLabels[slot] != null)
                {
                    slotLabels[slot].SetText(LabelFor(unlocked, hero, slot));
                }

                HeroUnitView view = slotViews[slot];

                if (view == null)
                {
                    continue;
                }

                if (hero != null)
                {
                    view.enabled = true;

                    // Index only: the icon/HP/name references were wired when the slot was built.
                    view.Configure(heroIndex);
                    view.Apply(hero);
                    anchors[Mathf.Clamp(heroIndex, 0, anchors.Length - 1)] = slotRects[slot];
                }
                else
                {
                    view.enabled = false;
                }
            }

            // Damage numbers follow the hero, not the slot, so the pool gets fresh anchors after every change.
            damagePool?.SetHeroAnchors(anchors);
        }

        /// <summary>
        /// The small line under the hero: the role on the Team board, the row name on an empty slot, or LOCKED.
        /// It never repeats the hero's name - that label belongs to the view.
        /// </summary>
        private string LabelFor(bool unlocked, HeroData hero, int slot)
        {
            if (!unlocked)
            {
                return "LOCKED";
            }

            if (hero != null)
            {
                return showRoleTags ? hero.Role.ToString().ToUpperInvariant() : string.Empty;
            }

            return formation.Data.RowForSlot(slot) == CombatRow.Front ? "front" : "back";
        }

        private void OnValidate()
        {
            slotSize.x = Mathf.Max(40f, slotSize.x);
            slotSize.y = Mathf.Max(40f, slotSize.y);
        }
    }
}
