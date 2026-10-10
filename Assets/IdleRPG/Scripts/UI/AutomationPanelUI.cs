using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Progression;

namespace IdleRPG.UI
{
    /// <summary>
    /// The AUTO strip on the UPGRADES page (B6 Step 2): one card row per automation, built in code at Start.
    ///
    /// ELI5: owned cards get a simple control panel - ON/OFF and, for the auto-buy machine, the "never touch X% of my
    /// gold" dial. Cards you haven't bought yet show as a teaser: LOCKED + price + the rebirths needed. Everything here
    /// talks to <see cref="AutomationService"/>; the service owns the rules.
    /// </summary>
    public sealed class AutomationPanelUI : MonoBehaviour
    {
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color dimColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color lockedColor = new Color(1f, 0.8f, 0.5f, 1f);

        private GameManager manager;
        private bool built;
        private readonly System.Collections.Generic.List<CardRow> rows =
            new System.Collections.Generic.List<CardRow>();

        private sealed class CardRow
        {
            public AutomationDef def;
            public TextMeshProUGUI titleLabel;
            public TextMeshProUGUI infoLabel;
            public Button toggleButton;
            public Button buyButton;
            public Button minusButton;
            public Button plusButton;
            public TextMeshProUGUI dialHintLabel;
        }

        /// <summary>The one automation whose budget can be dialled (how much gold the auto-buy manager may spend).</summary>
        private const string DialAutomationId = "autoBuy";

        private void Start()
        {
            // Same boot-race pattern as UpgradePanelUI: the GameManager may not have finished wiring yet,
            // so try now and again on every enable until the systems exist.
            TryBind();
        }

        private void OnEnable()
        {
            if (!built)
            {
                TryBind();
            }
        }

        private void TryBind()
        {
            if (built || manager != null)
            {
                return;
            }

            GameManager[] managers = Object.FindObjectsByType<GameManager>(
                FindObjectsInactive.Include);

            if (managers == null || managers.Length == 0 || managers[0].Automation == null)
            {
                return;
            }

            manager = managers[0];

            if (manager.Economy == null)
            {
                return;
            }

            manager.Automation.Changed += OnAutomationChanged;
            manager.Economy.CurrencyChanged += OnCurrencyChanged;
            Build(manager.Automation);
            built = true;
        }

        private void OnDestroy()
        {
            if (manager == null)
            {
                return;
            }

            if (manager.Automation != null)
            {
                manager.Automation.Changed -= OnAutomationChanged;
            }

            if (manager.Economy != null)
            {
                manager.Economy.CurrencyChanged -= OnCurrencyChanged;
            }
        }

        private void OnAutomationChanged()
        {
            Refresh();
        }

        private void OnCurrencyChanged(Economy.CurrencyType currency, double amount)
        {
            Refresh();
        }

        private void Build(AutomationService automation)
        {
            rows.Clear();

            int cardCount = automation.Defs.Count;
            int dialCount = 0;

            for (int i = 0; i < cardCount; i++)
            {
                if (IsDialAutomation(automation.Defs[i]))
                {
                    dialCount++;
                }
            }

            // One band per row: a heading, one card per automation, plus a reserve-dial row where one exists.
            int bands = 1 + cardCount + dialCount;
            Resize(bands);
            CreateHeading(bands);

            int band = 1;

            for (int i = 0; i < cardCount; i++)
            {
                AutomationDef def = automation.Defs[i];
                CardRow cardRow = CreateCardBand(def, band++, bands);

                if (IsDialAutomation(def))
                {
                    CreateDialBand(transform, cardRow, band++, bands);
                }

                rows.Add(cardRow);
            }

            Refresh();
        }

        /// <summary>The one automation with a dial (how much gold the auto-buy manager may spend).</summary>
        private static bool IsDialAutomation(AutomationDef def)
        {
            return def != null && def.AutomationID == DialAutomationId;
        }

        /// <summary>
        /// Grows the section so every band is exactly <see cref="UiTouch.RowHeight"/>. The component owns its own
        /// height because it is the only thing that knows how many rows it will build (cards + dial rows).
        /// </summary>
        private void Resize(int bands)
        {
            float height = bands * UiTouch.RowHeight;

            LayoutElement layout = GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<LayoutElement>();
            }

            layout.minHeight = height;
            layout.preferredHeight = height;
        }

        private static void AnchorBand(RectTransform rect, int bandIndex, int bands, float pad = 4f, float vPad = 2f)
        {
            float band = 1f / bands;
            float minY = 1f - (bandIndex + 1) * band;
            float maxY = 1f - bandIndex * band;
            UiRuntime.Anchor(rect, new Vector2(0f, minY), new Vector2(1f, maxY), pad, vPad, pad, vPad);
        }

        private void CreateHeading(int bands)
        {
            TextMeshProUGUI heading = UiRuntime.CreateText(transform, "Heading", "AUTOMATION", 20f,
                TextAlignmentOptions.MidlineLeft, dimColor);
            AnchorBand(heading.rectTransform, 0, bands, 8f, 0f);
        }

        /// <summary>A card band: title + description on the left, ON/OFF and BUY on the right (both finger-sized).</summary>
        private CardRow CreateCardBand(AutomationDef def, int bandIndex, int bands)
        {
            GameObject row = UiRuntime.CreateNode("Card" + def.AutomationID, transform);
            AnchorBand(row.GetComponent<RectTransform>(), bandIndex, bands);

            TextMeshProUGUI title = UiRuntime.CreateText(row.transform, "Title", def.DisplayName, 24f,
                TextAlignmentOptions.MidlineLeft, textColor);
            UiRuntime.Anchor(title.rectTransform, new Vector2(0.02f, 0.56f), new Vector2(0.58f, 0.96f));

            TextMeshProUGUI info = UiRuntime.CreateText(row.transform, "Info", string.Empty, 15f,
                TextAlignmentOptions.TopLeft, dimColor);
            info.textWrappingMode = TextWrappingModes.Normal;
            UiRuntime.Anchor(info.rectTransform, new Vector2(0.02f, 0.08f), new Vector2(0.58f, 0.54f));

            CardRow cardRow = new CardRow
            {
                def = def,
                titleLabel = title,
                infoLabel = info,
                toggleButton = UiRuntime.CreateButton(row.transform, "Toggle", "ON",
                    new Vector2(0.60f, 0.08f), new Vector2(0.78f, 0.92f), () => OnToggle(def)),
                buyButton = UiRuntime.CreateButton(row.transform, "Buy", "Buy",
                    new Vector2(0.80f, 0.08f), new Vector2(0.98f, 0.92f), () => OnBuy(def))
            };

            return cardRow;
        }

        /// <summary>The reserve dial's own band, so its two buttons can also be finger-sized.</summary>
        private void CreateDialBand(Transform parent, CardRow cardRow, int bandIndex, int bands)
        {
            GameObject row = UiRuntime.CreateNode("Dial" + cardRow.def.AutomationID, parent);
            AnchorBand(row.GetComponent<RectTransform>(), bandIndex, bands);

            TextMeshProUGUI hint = UiRuntime.CreateText(row.transform, "Hint", string.Empty, 16f,
                TextAlignmentOptions.MidlineLeft, dimColor);
            hint.textWrappingMode = TextWrappingModes.Normal;
            UiRuntime.Anchor(hint.rectTransform, new Vector2(0.02f, 0.20f), new Vector2(0.58f, 0.80f));
            cardRow.dialHintLabel = hint;

            cardRow.minusButton = UiRuntime.CreateButton(row.transform, "Minus", "-5%",
                new Vector2(0.60f, 0.08f), new Vector2(0.75f, 0.92f), () => OnDial(cardRow.def, -0.05f));
            cardRow.plusButton = UiRuntime.CreateButton(row.transform, "Plus", "+5%",
                new Vector2(0.77f, 0.08f), new Vector2(0.92f, 0.92f), () => OnDial(cardRow.def, 0.05f));
        }

        private void OnToggle(AutomationDef def)
        {
            if (manager == null)
            {
                return;
            }

            manager.Automation.SetEnabled(def, !manager.Automation.IsEnabled(def));
        }

        private void OnBuy(AutomationDef def)
        {
            if (manager != null)
            {
                manager.Automation.TryBuy(def);
            }

            Refresh();
        }

        private void OnDial(AutomationDef def, float delta)
        {
            if (manager == null)
            {
                return;
            }

            manager.Automation.SetBudgetFraction(def, manager.Automation.BudgetFraction(def) + delta);
        }

        private void Refresh()
        {
            if (manager == null || manager.Automation == null)
            {
                return;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                CardRow row = rows[i];
                AutomationService automation = manager.Automation;
                bool owned = automation.Owned(row.def);
                bool enabled = automation.IsEnabled(row.def);

                if (owned)
                {
                    row.titleLabel.text = row.def.DisplayName + (enabled ? "  (on)" : "  (off)");
                    row.titleLabel.color = textColor;
                    row.infoLabel.text = row.def.Description;
                    row.toggleButton.gameObject.SetActive(true);
                    Label(row.toggleButton, enabled ? "ON" : "OFF");
                    row.buyButton.gameObject.SetActive(false);

                    bool isAutoBuy = row.def.AutomationID == DialAutomationId;
                    Show(row.minusButton, isAutoBuy);
                    Show(row.plusButton, isAutoBuy);

                    if (row.dialHintLabel != null)
                    {
                        row.dialHintLabel.gameObject.SetActive(true);
                        row.dialHintLabel.text = isAutoBuy
                            ? "Auto-buy keeps " + Mathf.RoundToInt(automation.BudgetFraction(row.def) * 100f) + "% of your gold in reserve."
                            : string.Empty;
                    }

                    continue;
                }

                // Unowned: the teaser row.
                row.titleLabel.text = row.def.DisplayName + "  -  LOCKED";
                row.titleLabel.color = lockedColor;
                row.infoLabel.text = row.def.Description + "\nCosts " +
                                     automation.Cost(row.def).ToString("0") + " rebirth token(s)" +
                                     (row.def.MinAscensions > 0
                                         ? " after " + row.def.MinAscensions + " rebirth(s)"
                                         : "");
                row.toggleButton.gameObject.SetActive(false);
                Show(row.minusButton, false);
                Show(row.plusButton, false);

                if (row.dialHintLabel != null)
                {
                    row.dialHintLabel.gameObject.SetActive(true);
                    row.dialHintLabel.text = "Buy the card above to unlock this reserve.";
                }

                bool canBuy = automation.CanBuy(row.def, manager.AscensionCount) &&
                              manager.Economy.CanAfford(Economy.CurrencyType.PrestigeTokens, automation.Cost(row.def));
                row.buyButton.gameObject.SetActive(true);
                row.buyButton.interactable = canBuy;
                Label(row.buyButton, canBuy ? "BUY  " + automation.Cost(row.def).ToString("0") : "BUY");
            }
        }

        /// <summary>
        /// Shows or hides a button that a row may not own. Only the auto-buy card gets a reserve dial, so every
        /// other card row has no dial buttons at all - touching them unchecked threw a NullReferenceException on
        /// every gold change, which aborted the combat tick that paid the gold.
        /// </summary>
        private static void Show(Button button, bool show)
        {
            if (button != null)
            {
                button.gameObject.SetActive(show);
            }
        }

        private static void Label(Button button, string text)
        {
            if (button != null && button.transform != null && button.transform.childCount > 0)
            {
                TextMeshProUGUI label = button.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = text;
                }
            }
        }
    }
}