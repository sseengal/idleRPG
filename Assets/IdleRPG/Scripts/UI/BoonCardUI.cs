using System.Collections.Generic;
using IdleRPG.Core;
using IdleRPG.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// The level-up card: the fight stops, three boons are dealt, one is taken.
    ///
    /// ELI5: a big dark screen with "LEVEL UP!" and three cards. Tap a card and it is yours for the rest of the
    /// run. One free re-roll button sits at the bottom.
    ///
    /// It builds its own widgets, so the builder only places one empty node - a scene rebuild cannot leave half a
    /// card behind. It draws only what the game says: <see cref="GameManager.BoonOffer"/> is the single truth, and
    /// <see cref="GameEvents.BoonOfferChanged"/> is the only reason it redraws.
    /// </summary>
    public sealed class BoonCardUI : MonoBehaviour
    {
        private const int RowCount = 3;

        /// <summary>
        /// Above every other canvas in the scene (the modal layer is 100, the lobby 250, the MENU button 251).
        /// A choice that stops the fight must not have a button of the fight peeking over it.
        /// </summary>
        private const int CardSortingOrder = 300;

        private sealed class Row
        {
            public GameObject Root;
            public Image Background;
            public TextMeshProUGUI Rarity;
            public Image Icon;
            public TextMeshProUGUI IconLetter;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Target;
            public TextMeshProUGUI Description;
            public Button Button;
            public PerkData Card;
        }

        private GameObject root;
        private TextMeshProUGUI subtitle;
        private Button refreshButton;
        private TextMeshProUGUI refreshLabel;
        private readonly List<Row> rows = new List<Row>();

        private GameManager manager;

        private void Awake()
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            if (canvas != null && canvas.sortingOrder < CardSortingOrder)
            {
                canvas.sortingOrder = CardSortingOrder;
            }

            Build();
            root.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.BoonOfferChanged += Refresh;
            GameEvents.PartyLevelGained += OnLevelGained;
        }

        private void OnDisable()
        {
            GameEvents.BoonOfferChanged -= Refresh;
            GameEvents.PartyLevelGained -= OnLevelGained;
        }

        private void Start()
        {
            // A resumed run may already owe a pick: the manager restores it from the save during wiring, before
            // this view exists, so the first look happens here.
            EnsureManager();
            manager?.OfferBoonChoiceIfNeeded();
            Refresh();
        }

        private void OnLevelGained(int level, int pendingPicks)
        {
            Refresh();
        }

        private void EnsureManager()
        {
            if (manager == null && HudController.Instance != null)
            {
                manager = HudController.Instance.GameManager;
            }
        }

        /// <summary>Draws the current offer, or hides the card when there is none.</summary>
        public void Refresh()
        {
            EnsureManager();

            if (root == null)
            {
                return;
            }

            bool open = manager != null && manager.BoonCardOpen;
            root.SetActive(open);

            if (!open)
            {
                return;
            }

            IReadOnlyList<PerkData> offer = manager.BoonOffer;
            int picks = manager.Progression != null ? manager.Progression.PendingPicks : 0;

            if (subtitle != null)
            {
                subtitle.SetText(picks > 1 ? "CHOOSE A BOON  -  " + picks + " PICKS WAITING" : "CHOOSE A BOON");
            }

            for (int i = 0; i < rows.Count; i++)
            {
                Apply(rows[i], i < offer.Count ? offer[i] : null);
            }

            RefreshButton();
        }

        private void RefreshButton()
        {
            if (refreshButton == null)
            {
                return;
            }

            int left = manager != null ? manager.BoonRerollsLeft : 0;
            bool canReroll = left > 0;

            refreshButton.interactable = canReroll;

            if (refreshLabel != null)
            {
                refreshLabel.SetText(canReroll ? "REFRESH  " + left + "/" + Mathf.Max(left, 1) : "NO RE-ROLL LEFT");
            }

            // The image the button shades is the text's parent slab.
            Image slab = refreshButton.targetGraphic as Image;

            if (slab != null)
            {
                slab.color = canReroll ? UiTheme.Accent : UiTheme.PanelLight;
            }
        }

        private void OnRerollClicked()
        {
            manager?.RerollBoonOffer();
        }

        // ------------------------------------------------------------------
        // Drawing one card
        // ------------------------------------------------------------------
        private void Apply(Row row, PerkData card)
        {
            if (row == null)
            {
                return;
            }

            if (card == null)
            {
                row.Root.SetActive(false);
                return;
            }

            row.Card = card;
            row.Root.SetActive(true);

            Color tint = RarityColor(card.Rarity);

            if (row.Background != null)
            {
                row.Background.color = new Color(0.13f, 0.15f, 0.21f, 1f);
            }

            if (row.Rarity != null)
            {
                row.Rarity.SetText(RarityLabel(card.Rarity));
                row.Rarity.color = tint;
            }

            if (row.Icon != null)
            {
                row.Icon.color = new Color(tint.r * 0.45f, tint.g * 0.45f, tint.b * 0.45f, 1f);
            }

            if (row.IconLetter != null)
            {
                // Placeholder art: the card's first letter on a rarity-tinted badge. Swap for a real icon later.
                string name = card.DisplayName;
                row.IconLetter.SetText(string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1));
                row.IconLetter.color = tint;
            }

            if (row.Name != null)
            {
                row.Name.SetText(card.DisplayName);
            }

            if (row.Target != null)
            {
                row.Target.SetText(card.TargetLabel);
                row.Target.color = UiTheme.TextDim;
            }

            if (row.Description != null)
            {
                row.Description.SetText(card.Description);
            }
        }

        private static string RarityLabel(PerkRarity rarity)
        {
            switch (rarity)
            {
                case PerkRarity.Rare: return "RARE";
                case PerkRarity.Epic: return "EPIC";
                case PerkRarity.Legendary: return "LEGENDARY";
                default: return "COMMON";
            }
        }

        private static Color RarityColor(PerkRarity rarity)
        {
            switch (rarity)
            {
                case PerkRarity.Rare: return new Color(0.45f, 0.72f, 1f, 1f);
                case PerkRarity.Epic: return new Color(0.78f, 0.55f, 1f, 1f);
                case PerkRarity.Legendary: return new Color(1f, 0.78f, 0.30f, 1f);
                default: return new Color(0.72f, 0.76f, 0.84f, 1f);
            }
        }

        private void OnCardClicked(Row row)
        {
            if (manager != null && row.Card != null)
            {
                manager.ChooseBoon(row.Card);
            }
        }

        // ------------------------------------------------------------------
        // Build (once, at Awake)
        // ------------------------------------------------------------------
        private void Build()
        {
            if (root != null)
            {
                return;
            }

            RectTransform self = (RectTransform)transform;

            root = new GameObject("Card", typeof(RectTransform));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.SetParent(self, false);
            UiRuntime.Stretch(rootRect);

            // Scrim: also the raycast catcher, so a tap can never reach the fight behind the card.
            Image scrim = UiRuntime.CreatePanel(root.transform, null, new Color(0f, 0f, 0f, 0.88f), raycast: true);
            UiRuntime.Stretch(scrim.rectTransform);

            Image panel = UiRuntime.CreatePanel(scrim.transform, null, UiTheme.PanelDeep);
            UiRuntime.Anchor(panel.rectTransform, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f));

            TextMeshProUGUI title = UiRuntime.CreateText(panel.transform, "Title", "LEVEL UP!",
                UiType.Role.Section, TextAlignmentOptions.Center, UiTheme.AccentLight);
            UiRuntime.Anchor(title.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f));

            subtitle = UiRuntime.CreateText(panel.transform, "Subtitle", "CHOOSE A BOON",
                UiType.Role.Micro, TextAlignmentOptions.Center, UiTheme.TextDim);
            UiRuntime.Anchor(subtitle.rectTransform, new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.88f));

            const float rowHeight = 0.235f;
            const float rowTop = 0.81f;

            for (int i = 0; i < RowCount; i++)
            {
                float top = rowTop - i * rowHeight;
                rows.Add(BuildRow(panel.transform, top, top - (rowHeight - 0.02f), i));
            }

            refreshButton = UiRuntime.CreateButton(panel.transform, "Refresh", "REFRESH  1/1",
                new Vector2(0.20f, 0.02f), new Vector2(0.80f, 0.10f), OnRerollClicked,
                UiTheme.Accent, null, UiType.Role.Small);

            refreshLabel = refreshButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        /// <summary>One card: rarity tag, placeholder badge, name, target, effects.</summary>
        private Row BuildRow(Transform parent, float top, float bottom, int index)
        {
            Row row = new Row();

            Image background = UiRuntime.CreatePanel(parent, null, new Color(0.13f, 0.15f, 0.21f, 1f), raycast: true);
            UiRuntime.Anchor(background.rectTransform, new Vector2(0.05f, bottom), new Vector2(0.95f, top));
            background.gameObject.name = "Boon" + index;
            row.Background = background;
            row.Root = background.gameObject;

            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => OnCardClicked(row));
            row.Button = button;

            row.Rarity = UiRuntime.CreateText(background.transform, "Rarity", "COMMON",
                UiType.Role.Micro, TextAlignmentOptions.MidlineLeft, UiTheme.TextDim);
            UiRuntime.Anchor(row.Rarity.rectTransform, new Vector2(0.04f, 0.84f), new Vector2(0.32f, 0.96f));

            row.Icon = UiRuntime.CreatePanel(background.transform, null, new Color(0.3f, 0.3f, 0.4f, 1f));
            UiRuntime.Anchor(row.Icon.rectTransform, new Vector2(0.04f, 0.12f), new Vector2(0.26f, 0.78f));

            row.IconLetter = UiRuntime.CreateText(row.Icon.transform, "Letter", "?",
                UiType.Role.Section, TextAlignmentOptions.Center, UiTheme.Text);
            UiRuntime.Stretch(row.IconLetter.rectTransform);

            row.Name = UiRuntime.CreateText(background.transform, "Name", "Boon",
                UiType.Role.Small, TextAlignmentOptions.MidlineLeft, UiTheme.Text);
            UiRuntime.Anchor(row.Name.rectTransform, new Vector2(0.30f, 0.62f), new Vector2(0.97f, 0.88f));

            row.Target = UiRuntime.CreateText(background.transform, "Target", "All heroes",
                UiType.Role.Micro, TextAlignmentOptions.MidlineLeft, UiTheme.TextDim);
            UiRuntime.Anchor(row.Target.rectTransform, new Vector2(0.30f, 0.46f), new Vector2(0.97f, 0.62f));

            row.Description = UiRuntime.CreateText(background.transform, "Description", "+10% attack",
                UiType.Role.Micro, TextAlignmentOptions.TopLeft, UiTheme.Text);
            UiRuntime.Anchor(row.Description.rectTransform, new Vector2(0.30f, 0.08f), new Vector2(0.97f, 0.44f));

            return row;
        }
    }
}
