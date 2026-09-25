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
            public TextMeshProUGUI dialLabel;
        }

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
            int count = Mathf.Max(1, automation.Defs.Count);

            for (int i = 0; i < automation.Defs.Count; i++)
            {
                AutomationDef def = automation.Defs[i];
                GameObject row = UiRuntime.CreateNode("Card" + def.AutomationID, transform);
                float minY = 1f - (i + 1) * (1f / count);
                float maxY = 1f - i * (1f / count);
                UiRuntime.Anchor(row.GetComponent<RectTransform>(),
                    new Vector2(0f, minY), new Vector2(1f, maxY), 4f, 2f, 4f, 2f);

                TextMeshProUGUI title = UiRuntime.CreateText(row.transform, "Title", def.DisplayName, 24f,
                    TextAlignmentOptions.MidlineLeft, textColor);
                UiRuntime.Anchor(title.rectTransform, new Vector2(0.02f, 0.72f), new Vector2(0.6f, 0.98f));

                TextMeshProUGUI info = UiRuntime.CreateText(row.transform, "Info", "", 15f,
                    TextAlignmentOptions.TopLeft, dimColor);
                info.textWrappingMode = TextWrappingModes.Normal;
                UiRuntime.Anchor(info.rectTransform, new Vector2(0.02f, 0.36f), new Vector2(0.75f, 0.70f));

                CardRow cardRow = new CardRow
                {
                    def = def,
                    titleLabel = title,
                    infoLabel = info,
                    toggleButton = UiRuntime.CreateButton(row.transform, "Toggle", "ON",
                        new Vector2(0.79f, 0.70f), new Vector2(0.98f, 0.96f), () => OnToggle(def)),
                    buyButton = UiRuntime.CreateButton(row.transform, "Buy", "Buy",
                        new Vector2(0.79f, 0.34f), new Vector2(0.98f, 0.64f), () => OnBuy(def)),
                    minusButton = UiRuntime.CreateButton(row.transform, "Minus", "-",
                        new Vector2(0.74f, 0.34f), new Vector2(0.86f, 0.64f), () => OnDial(def, -0.05f)),
                    plusButton = UiRuntime.CreateButton(row.transform, "Plus", "+",
                        new Vector2(0.90f, 0.34f), new Vector2(0.98f, 0.64f), () => OnDial(def, 0.05f)),
                    dialLabel = UiRuntime.CreateText(row.transform, "Dial", "Keep 50% gold", 14f,
                        TextAlignmentOptions.MidlineRight, textColor)
                };

                UiRuntime.Anchor(cardRow.dialLabel.rectTransform, new Vector2(0.74f, 0.02f), new Vector2(0.98f, 0.30f));
                rows.Add(cardRow);
            }

            Refresh();
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

                    bool isAutoBuy = row.def.AutomationID == "autoBuy";
                    row.minusButton.gameObject.SetActive(isAutoBuy);
                    row.plusButton.gameObject.SetActive(isAutoBuy);
                    row.dialLabel.gameObject.SetActive(isAutoBuy);

                    if (isAutoBuy)
                    {
                        int keepPct = Mathf.RoundToInt(automation.BudgetFraction(row.def) * 100f);
                        row.dialLabel.text = "Keep " + keepPct + "% gold";
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
                row.minusButton.gameObject.SetActive(false);
                row.plusButton.gameObject.SetActive(false);
                row.dialLabel.gameObject.SetActive(false);

                bool canBuy = automation.CanBuy(row.def, manager.AscensionCount) &&
                              manager.Economy.CanAfford(Economy.CurrencyType.PrestigeTokens, automation.Cost(row.def));
                row.buyButton.gameObject.SetActive(true);
                row.buyButton.interactable = canBuy;
                Label(row.buyButton, canBuy ? "BUY  " + automation.Cost(row.def).ToString("0") : "BUY");
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