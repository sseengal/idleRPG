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
    /// The party board: six pads (two columns = front and back rank) plus **one view per hero**.
    ///
    /// ELI5: pads are parking spaces; heroes are cars. A car can drive to another space without becoming a different
    /// car. That is why the view belongs to the hero and only its position belongs to a pad - the earlier version put
    /// a hero's view inside a pad, so a hero who moved had to vanish here and reappear there, and every effect that
    /// asked "where is he" got the empty space instead of the hero.
    ///
    /// Moving is a two-tap tactic on this board (tap a hero, tap a free pad). Free pads brighten while a hero is
    /// held; any other tap drops the selection. The data change goes through <see cref="Formation.TryMove"/>, whose
    /// Changed event re-stamps the live fight, marks the save dirty and repaints this board.
    /// </summary>
    public sealed class FormationBoardView : MonoBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("Visual hero slot. Built by the scene builder; the board only places it and binds its tap.")]
        [SerializeField] private HeroUnitView heroPrefab;

        [Header("Layout")]
        [SerializeField] private Vector2 slotSize = new Vector2(132f, 150f);
        [SerializeField] private float columnGap = 26f;
        [SerializeField] private float rowGap = 12f;

        [Header("Art (optional)")]
        [SerializeField] private Sprite slotSprite;
        [SerializeField] private Color slotColor = new Color(1f, 1f, 1f, 0.16f);
        [Tooltip("A free pad while a hero is held: this is the cue that says \"tap a free pad\".")]
        [SerializeField] private Color emptyPadColor = new Color(1f, 0.93f, 0.72f, 0.42f);

        private GameManager manager;
        private Formation formation;

        private Image[] padImages;
        private RectTransform[] padRects;
        private HeroUnitView[] heroViews;
        private RectTransform[] heroRects;
        private int[] heroPad = new int[0];
        private int heroCount;

        private int selectedHero = -1;

        /// <summary>Builds the board and keeps it in sync with the formation and the fight.</summary>
        public void Build(GameManager gameManager, FloatingDamageTextPool pool)
        {
            manager = gameManager;
            formation = manager != null ? manager.Formation : null;

            if (formation == null)
            {
                Debug.LogError("[FormationBoardView] No formation on the GameManager; the board cannot be drawn.");
                return;
            }

            BuildPads();
            RebuildHeroes();
            Refresh(instant: true);

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
            Refresh(instant: false);
        }

        // ------------------------------------------------------------------
        // Build
        // ------------------------------------------------------------------
        private void BuildPads()
        {
            FormationData data = formation.Data;
            int slots = formation.SlotCount;

            ((RectTransform)transform).sizeDelta = FormationBoardLayout.SizeOf(data, slotSize, columnGap, rowGap);

            // A catcher behind the pads: a tap that is not a hero and not a pad only drops the selection.
            GameObject catcher = UiRuntime.CreateNode("BoardBackground", transform);
            Image catcherImage = UiRuntime.CreatePanel(catcher.transform, null, new Color(0f, 0f, 0f, 0f), true);
            UiRuntime.Stretch(catcherImage.rectTransform);
            Button catcherButton = catcher.AddComponent<Button>();
            catcherButton.targetGraphic = catcherImage;
            catcherButton.transition = Selectable.Transition.None;
            catcherButton.onClick.AddListener(OnBackgroundTapped);

            padImages = new Image[slots];
            padRects = new RectTransform[slots];

            for (int slot = 0; slot < slots; slot++)
            {
                GameObject pad = UiRuntime.CreateNode("Pad" + slot, transform);
                RectTransform rect = pad.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = slotSize;
                rect.anchoredPosition = FormationBoardLayout.PositionOfSlot(data, slot, slotSize, columnGap, rowGap);

                Image background = UiRuntime.CreatePanel(pad.transform, slotSprite, slotColor, true);
                UiRuntime.Stretch(background.rectTransform);

                int captured = slot;
                Button button = pad.AddComponent<Button>();
                button.targetGraphic = background;
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => OnPadTapped(captured));

                padImages[slot] = background;
                padRects[slot] = rect;
            }
        }

        /// <summary>Builds one view per party hero. Views are reused across refreshes - a hero never loses his view.</summary>
        private void RebuildHeroes()
        {
            int count = manager != null && manager.Party != null ? manager.Party.Heroes.Count : 0;

            heroViews = new HeroUnitView[Mathf.Max(1, count)];
            heroRects = new RectTransform[Mathf.Max(1, count)];
            heroPad = new int[Mathf.Max(1, count)];

            for (int i = 0; i < heroPad.Length; i++)
            {
                heroPad[i] = -1;
            }

            heroCount = count;
        }

        private HeroUnitView EnsureHeroView(int heroIndex)
        {
            if (heroViews[heroIndex] != null)
            {
                return heroViews[heroIndex];
            }

            if (heroPrefab == null)
            {
                Debug.LogError("[FormationBoardView] Hero prefab is not assigned; the battle board cannot draw heroes.");
                return null;
            }

            HeroUnitView view = Instantiate(heroPrefab, transform);
            view.name = "Hero" + heroIndex;

            RectTransform rect = (RectTransform)view.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = slotSize;

            int captured = heroIndex;
            Button button = view.GetComponent<Button>();

            if (button == null)
            {
                Debug.LogError("[FormationBoardView] Hero prefab needs a Button for stage input.");
            }
            else
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnHeroTapped(captured));
            }

            view.Configure(heroIndex);
            heroViews[heroIndex] = view;
            heroRects[heroIndex] = rect;
            return view;
        }

        // ------------------------------------------------------------------
        // Repaint
        // ------------------------------------------------------------------
        /// <summary>
        /// Places every hero on his pad and refreshes his data. <paramref name="instant"/> is true for the first
        /// layout; after that a hero the player moved eases across, so the eye can follow who went where.
        /// </summary>
        private void Refresh(bool instant)
        {
            if (formation == null || heroViews == null)
            {
                return;
            }

            PartyConfig party = manager != null ? manager.Party : null;
            int count = party != null ? party.Heroes.Count : 0;

            if (count > heroViews.Length)
            {
                RebuildHeroes();
            }

            heroCount = count;

            for (int hero = 0; hero < heroCount; hero++)
            {
                HeroData data = party.GetHero(hero);
                HeroUnitView view = EnsureHeroView(hero);

                if (view == null)
                {
                    continue;
                }

                if (data == null)
                {
                    view.ClearVisual();
                    continue;
                }

                view.enabled = true;
                view.Configure(hero);

                // Place the hero FIRST: creating his dash (inside Apply) captures its home from where he stands now,
                // so a view that is still at the board origin would send him back to the corner after every swing.
                int slot = formation.SlotOfHero(hero);

                if (slot >= 0 && slot < padRects.Length)
                {
                    bool firstPlacement = heroPad[hero] < 0 || heroPad[hero] == slot;
                    view.SlideTo(padRects[slot], instant || firstPlacement);
                    heroPad[hero] = slot;
                }

                view.Apply(data);
                view.SetHealthVisible(true);   // a living hero shows his bar
                view.RefreshStats();           // and the LIVE hp immediately - no zero flash after a move
            }

            UpdatePadVisuals();
        }

        /// <summary>Free pads brighten only while a hero is held. That is the whole instruction for the tactic.</summary>
        private void UpdatePadVisuals()
        {
            if (padImages == null || formation == null)
            {
                return;
            }

            for (int slot = 0; slot < padImages.Length; slot++)
            {
                if (padImages[slot] == null)
                {
                    continue;
                }

                bool isFreeDestination = selectedHero >= 0 && formation.HeroAt(slot) < 0;
                padImages[slot].color = isFreeDestination ? emptyPadColor : slotColor;
            }
        }

        // ------------------------------------------------------------------
        // Taps: tap a hero, tap a free pad. Anything else drops the hold.
        // ------------------------------------------------------------------
        private void OnHeroTapped(int heroIndex)
        {
            if (AnyHeroSliding())
            {
                return;
            }

            if (selectedHero >= 0)
            {
                ClearSelection();
                return;
            }

            selectedHero = heroIndex;
            UpdatePadVisuals();
        }

        private void OnPadTapped(int slotIndex)
        {
            if (AnyHeroSliding() || selectedHero < 0)
            {
                ClearSelection();
                return;
            }

            // A free pad is the only destination: moving onto an occupied pad is not offered.
            if (formation.HeroAt(slotIndex) >= 0)
            {
                ClearSelection();
                return;
            }

            int hero = selectedHero;
            ClearSelection();

            // TryMove fires Formation.Changed, which re-stamps the fight, saves the layout and calls Refresh().
            formation.TryMove(hero, slotIndex);
        }

        private void OnBackgroundTapped()
        {
            ClearSelection();
        }

        private void ClearSelection()
        {
            selectedHero = -1;
            UpdatePadVisuals();
        }

        private bool AnyHeroSliding()
        {
            if (heroViews == null)
            {
                return false;
            }

            for (int i = 0; i < heroViews.Length; i++)
            {
                if (heroViews[i] != null && heroViews[i].IsSliding)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The view of one hero (used by the beat planner). Null when out of range.</summary>
        public HeroUnitView GetHeroView(int heroIndex)
        {
            if (heroViews == null || heroIndex < 0 || heroIndex >= heroViews.Length)
            {
                return null;
            }

            return heroViews[heroIndex];
        }

        /// <summary>The view of one hero - what a dash should aim at (an enemy lunging at the back rank).</summary>
        public RectTransform GetHeroSlotRect(int heroIndex)
        {
            if (heroRects == null || heroIndex < 0 || heroIndex >= heroRects.Length)
            {
                return null;
            }

            return heroRects[heroIndex];
        }
    }
}
