using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// The one way to put a card over the game: a stretched root, a dimming scrim, a 9-slice card, a title, an empty
    /// content area and an X. Every card in the game uses this, and its look comes from <see cref="UiSkin"/> - so
    /// replacing the UI art is one file, not one screen at a time.
    ///
    /// ELI5: a pop-up frame. Give it a size and a title; it gives you back an empty middle to put your rows in. It
    /// handles the dim background, the close button, and being on top of everything else.
    ///
    /// Two rules it exists to enforce, both learned from the ranks sheet:
    ///  * the root is STRETCHED over the overlay, so child anchors are fractions of the SCREEN (a default 100x100
    ///    rect made the whole card microscopic);
    ///  * the card owns show/hide, so a page can never destroy the wrong object and leave itself dead.
    /// </summary>
    public sealed class UiCard
    {
        /// <summary>The stretched overlay node (scrim + card live inside it).</summary>
        public GameObject Root { get; private set; }

        /// <summary>Where a page builds its rows. Anchors inside are frame-relative, 0..1.</summary>
        public RectTransform Content { get; private set; }

        public TextMeshProUGUI TitleLabel { get; private set; }

        public Button CloseButton { get; private set; }

        /// <summary>The look this card was built from (sprites, colours, text sizes).</summary>
        public UiSkin Skin { get; private set; }

        /// <summary>Builds a card on the overlay canvas. The rectangle is a fraction of the screen (0..1).</summary>
        public static UiCard Create(string name, string title, Vector2 anchorMin, Vector2 anchorMax, bool scrimCloses = true)
        {
            UiCard card = new UiCard();
            card.Skin = UiSkin.Current;

            GameObject root = UiRuntime.CreateNode(name, ResolveOverlayParent());
            UiRuntime.Stretch(root.GetComponent<RectTransform>());
            card.Root = root;

            // Scrim: dims the game and eats every tap, so nothing behind a card can be pressed by accident.
            Image scrim = UiRuntime.CreatePanel(root.transform, null, card.Skin.scrim, raycast: true);
            UiRuntime.Stretch(scrim.rectTransform);

            if (scrimCloses)
            {
                Button dismiss = scrim.gameObject.AddComponent<Button>();
                dismiss.targetGraphic = scrim;
                dismiss.transition = Selectable.Transition.None;
                dismiss.onClick.AddListener(card.Hide);
            }

            // The card face: one 9-slice piece from the skin. Its import border (12 px) does the slicing, so no border
            // value is set here - a replacement PNG only has to be imported with the same preset.
            Image face = UiRuntime.CreatePanel(root.transform, card.Skin.panelSprite, card.Skin.card, raycast: true);
            UiRuntime.Anchor(face.rectTransform, anchorMin, anchorMax);

            card.TitleLabel = UiRuntime.CreateText(face.transform, "Title", title, card.Skin.titleSize,
                TextAlignmentOptions.MidlineLeft, card.Skin.title);
            UiTheme.ApplyFont(card.TitleLabel, UiTheme.Display, card.Skin.titleSize);
            UiRuntime.Anchor(card.TitleLabel.rectTransform, new Vector2(0.06f, 0.88f), new Vector2(0.80f, 0.98f));

            card.CloseButton = UiRuntime.CreateButton(face.transform, "CloseButton", "X",
                new Vector2(0.86f, 0.87f), new Vector2(0.95f, 0.97f), card.Hide,
                card.Skin.button, card.Skin.ResolveButtonSprite());

            TextMeshProUGUI closeLabel = card.CloseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (closeLabel != null)
            {
                UiTheme.ApplyFont(closeLabel, UiTheme.Display, 18f);
            }

            GameObject content = UiRuntime.CreateNode("Content", face.transform);
            card.Content = content.GetComponent<RectTransform>();
            UiRuntime.Anchor(card.Content, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.85f));

            return card;
        }

        /// <summary>Shows the card and brings it to the front of the overlay.</summary>
        public void Show()
        {
            if (Root == null)
            {
                return;
            }

            Root.SetActive(true);
            Root.transform.SetAsLastSibling();
        }

        /// <summary>Hides the card. It stays alive, so the same card can be opened again (cheaply).</summary>
        public void Hide()
        {
            if (Root != null)
            {
                Root.SetActive(false);
            }
        }

        /// <summary>Body text in the skin's sentence font - the pixel look for anything a page writes.</summary>
        public TextMeshProUGUI CreateBodyText(Transform parent, string name, string text, float size,
            TextAlignmentOptions alignment, Color color)
        {
            TextMeshProUGUI label = UiRuntime.CreateText(parent, name, text, size, alignment, color);
            UiTheme.ApplyFont(label, UiTheme.Sentence, size);
            return label;
        }

        /// <summary>A 9-slice button in the skin's style (accent = the gold call-to-action face).</summary>
        public Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax,
            UnityAction onClick, bool accent = false)
        {
            Button button = UiRuntime.CreateButton(parent, name, label, anchorMin, anchorMax, onClick,
                accent ? Skin.accent : Skin.button, Skin.ResolveButtonSprite());

            TextMeshProUGUI buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonLabel != null)
            {
                UiTheme.ApplyFont(buttonLabel, UiTheme.Display, Skin.buttonSize);
            }

            return button;
        }

        /// <summary>
        /// The overlay canvas every card belongs on: the modal canvas (highest layer, above the HUD and the damage
        /// numbers), reached through the HUD rather than by hunting the scene.
        /// </summary>
        private static Transform ResolveOverlayParent()
        {
            HudController hud = HudController.Instance;

            if (hud != null && hud.ModalRoot != null)
            {
                return hud.ModalRoot;
            }

            // Scenes without a HUD (the labs): any canvas will do, so a card still has somewhere to live.
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            return canvas != null ? canvas.transform : null;
        }
    }
}
