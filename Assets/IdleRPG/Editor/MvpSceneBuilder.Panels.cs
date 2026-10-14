using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.UI;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Management page construction: tabs, nav bar, party, upgrade, ascension and shop panels.
    /// </summary>
    public static partial class MvpSceneBuilder
    {
        // ------------------------------------------------------------------
        // Page bands - named once, used by the panels below
        //
        // A band is a fraction of the PAGE rect (the area between the two chrome bars). Naming them means a band
        // change is one edit, and the ASCEND and SHOP pages - which share one shape (title, sub-line, one action,
        // then a list) - cannot drift apart.
        //
        // They are NOT on the UiSpace ladder on purpose: the ladder is gaps in canvas units, these are proportions
        // of a flexible page. The trade-off is recorded in Docs/UI-Layout.md.
        // ------------------------------------------------------------------
        private const float BandTop = 1f;

        /// <summary>ASCEND + SHOP: title band, sub-line band, the action button, then the list below it.</summary>
        private const float BandTitleBottom = 0.88f;
        private const float BandSubLineBottom = 0.79f;
        private const float BandActionTop = 0.77f;
        private const float BandActionBottom = 0.60f;
        private const float BandListTop = 0.57f;

        /// <summary>UPGRADES: one title line and a long list, so its bands sit higher.</summary>
        private const float BandUpgradesTitleBottom = 0.945f;
        private const float BandUpgradesListTop = 0.93f;

        // ------------------------------------------------------------------
        // Control dock
        // ------------------------------------------------------------------
        private static TabController BuildManagementPage(RectTransform pageRoot, PartyConfig partyConfig,
            List<PrestigeUpgradeData> prestigeUpgrades)
        {
            Image shell = UiFactory.Panel("ManagementShell", pageRoot, "ui_panel", new Color(1f, 1f, 1f, 0.98f));
            UiFactory.Stretch(shell.rectTransform, UiSpace.ScreenMargin, UiSpace.Snug, UiSpace.ScreenMargin, UiSpace.Snug);
            TabController tabs = shell.gameObject.AddComponent<TabController>();

            GameObject panelsRoot = UiFactory.Node("Panels", shell.transform);
            UiFactory.Anchor(panelsRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, UiSpace.Base, UiSpace.Base, UiSpace.Base, UiSpace.Base);

            GameObject partyPanel = BuildPartyPanel(panelsRoot.transform);
            GameObject upgradePanel = BuildUpgradePanel(panelsRoot.transform, partyConfig);
            GameObject ascensionPanel = BuildAscensionPanel(panelsRoot.transform, prestigeUpgrades);
            GameObject shopPanel = BuildShopPanel(panelsRoot.transform);

            // No tab bar here on purpose: the bottom nav bar is the single navigation, and
            // ScreenController drives these panels through the TabController below.
            List<TabController.TabDefinition> definitions = new List<TabController.TabDefinition>
            {
                CreatePanelTab(partyPanel),
                CreatePanelTab(upgradePanel),
                CreatePanelTab(ascensionPanel),
                CreatePanelTab(shopPanel)
            };

            SetTabs(tabs, definitions);
            return tabs;
        }

        /// <summary>
        /// The PARTY tab panel. The board, the roster and the hero card are built at runtime by
        /// <see cref="PartyPanelUI"/>, so adding a row (a stat, an equipment slot) never means re-authoring a scene.
        /// </summary>
        private static GameObject BuildPartyPanel(Transform parent)
        {
            GameObject panel = UiFactory.Node("PartyPanel", parent);
            UiFactory.Stretch(panel.GetComponent<RectTransform>());
            panel.AddComponent<PartyPanelUI>();
            return panel;
        }

        /// <summary>Bottom nav: Battle + the four management tabs, with active highlighting.</summary>
        private static ScreenController BuildNavBar(RectTransform safeArea, GameObject battlePage,
            GameObject managementPage, TabController tabs, GameObject damageCanvas)
        {
            Image bar = KitPanel("NavBar", safeArea, UiTheme.Panel);
            PinBar(bar.rectTransform, top: false, NavBandHeight, UiSpace.ScreenMargin, UiSpace.Base);
            ScreenController controller = bar.gameObject.AddComponent<ScreenController>();

            string[] labels = { "BATTLE", "PARTY", "UPGRADES", "ASCEND", "SHOP" };
            Button[] buttons = new Button[labels.Length];
            Image[] backgrounds = new Image[labels.Length];
            TextMeshProUGUI[] buttonLabels = new TextMeshProUGUI[labels.Length];

            for (int i = 0; i < labels.Length; i++)
            {
                float width = 1f / labels.Length;
                float xMin = i * width;

                Image buttonImage = KitPanel("Nav" + labels[i], bar.transform, UiTheme.PanelLight, raycast: true);
                UiFactory.Anchor(buttonImage.rectTransform, new Vector2(xMin, 0.08f), new Vector2(xMin + width, 0.92f), UiSpace.Snug, UiSpace.None, UiSpace.Snug, UiSpace.None);

                Button button = buttonImage.gameObject.AddComponent<Button>();
                button.targetGraphic = buttonImage;

                TextMeshProUGUI label = UiFactory.Text("Label", buttonImage.transform, labels[i], UiType.Role.Small,
                    TextAlignmentOptions.Center, UiTheme.Text);
                // Auto-sized: "UPGRADES" is the longest tab word and a tab is only ~198 units wide, so a longer
                // label shrinks towards UiType.Micro instead of spilling over its neighbour.
                UiType.ApplyAuto(label, UiType.Role.Small);
                UiFactory.Stretch(label.rectTransform, UiSpace.Small, UiSpace.Tight, UiSpace.Small, UiSpace.Tight);

                buttons[i] = button;
                backgrounds[i] = button.targetGraphic as Image;
                buttonLabels[i] = label;
            }

            // Palette A: the active tab is gold with dark ink; the rest are raised slate on the deep bar.
            SceneWiringUtility.SetField(controller, "activeNavColor", UiTheme.AccentLight);
            SceneWiringUtility.SetField(controller, "inactiveNavColor", UiTheme.PanelLight);
            SceneWiringUtility.SetField(controller, "activeNavLabelColor", UiTheme.InkOnAccent);
            SceneWiringUtility.SetField(controller, "inactiveNavLabelColor", UiTheme.TextDim);
            SceneWiringUtility.SetField(controller, "battlePage", battlePage);
            SceneWiringUtility.SetField(controller, "managementPage", managementPage);
            SceneWiringUtility.SetField(controller, "tabs", tabs);
            SceneWiringUtility.SetField(controller, "navButtons", buttons);
            SceneWiringUtility.SetField(controller, "navButtonBackgrounds", backgrounds);
            SceneWiringUtility.SetField(controller, "navButtonLabels", buttonLabels);
            SceneWiringUtility.SetField(controller, "hideWhileBrowsing", new[] { damageCanvas });
            return controller;
        }

        private static void SetTabs(TabController tabs, List<TabController.TabDefinition> definitions)
        {
            SceneWiringUtility.SetField(tabs, "tabs", definitions);
            SceneWiringUtility.SetField(tabs, "activeTabSprite", UiFactory.LoadSprite("ui_tab_on"), required: false);
            SceneWiringUtility.SetField(tabs, "inactiveTabSprite", UiFactory.LoadSprite("ui_tab_off"), required: false);
        }

        /// <summary>A tab whose only job is toggling a panel: navigation lives in the bottom bar.</summary>
        private static TabController.TabDefinition CreatePanelTab(GameObject panel)
        {
            return new TabController.TabDefinition
            {
                tabName = panel != null ? panel.name : "Panel",
                button = null,
                buttonBackground = null,
                buttonLabel = null,
                panel = panel
            };
        }

        private static GameObject BuildUpgradePanel(Transform parent, PartyConfig partyConfig)
        {
            GameObject panel = UiFactory.Node("UpgradesPanel", parent);
            UiFactory.Stretch(panel.GetComponent<RectTransform>());
            UpgradePanelUI ui = panel.AddComponent<UpgradePanelUI>();

            TextMeshProUGUI title = UiFactory.Text("Title", panel.transform,
                "Spend gold to raise each hero's stats (scroll for more)", UiType.Role.Sentence,
                TextAlignmentOptions.MidlineLeft, DimTextColor);
            UiFactory.Anchor(title.rectTransform, new Vector2(0.02f, BandUpgradesTitleBottom), new Vector2(0.98f, BandTop));

            ScrollRect scroll = UiFactory.CreateScrollView(panel.transform, "UpgradeScroll", UiSpace.Base, UiSpace.Padding(UiSpace.Tight), out RectTransform list);
            UiFactory.Anchor(scroll.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, BandUpgradesListTop), UiSpace.Hairline, UiSpace.Hairline, UiSpace.Hairline, UiSpace.Hairline);

            HeroUpgradeRowUI[] rows = new HeroUpgradeRowUI[Mathf.Max(1, partyConfig != null ? partyConfig.ValidHeroCount : 1)];

            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = CreateHeroUpgradeRow(list, i, partyConfig);
            }

            SceneWiringUtility.SetField(ui, "rows", rows);

            // AUTOMATION is the last section INSIDE the scroll. It used to be a pinned strip at the bottom of the
            // page, but its controls cannot reach finger size (UiTouch.MinTarget) inside a short strip - and an
            // owning component sizes its own rows (cards + the auto-buy reserve dial).
            GameObject automationSection = UiFactory.Node("AutomationSection", list);
            automationSection.AddComponent<AutomationPanelUI>();

            return panel;
        }

        private static HeroUpgradeRowUI CreateHeroUpgradeRow(Transform parent, int index, PartyConfig partyConfig)
        {
            const int statCount = 5;

            GameObject row = UiFactory.Node($"HeroRow{index}", parent);

            // The list band is 0.02..0.86 of the section: size the section so five finger-sized rows fit exactly.
            const float listTop = 0.86f;
            const float listBottom = 0.02f;
            float height = UiTouch.ListHeight(statCount) / (listTop - listBottom);

            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;

            Image background = UiFactory.Panel("Background", row.transform, "ui_panel", new Color(1f, 1f, 1f, 0.30f));
            UiFactory.Stretch(background.rectTransform);

            HeroData hero = partyConfig != null ? partyConfig.GetHero(index) : null;
            TextMeshProUGUI nameLabel = UiFactory.Text("Name", row.transform,
                hero != null ? hero.HeroName : $"Hero {index + 1}", UiType.Role.Section, TextAlignmentOptions.MidlineLeft, TextColor);
            UiFactory.Anchor(nameLabel.rectTransform, new Vector2(0.03f, 0.87f), new Vector2(0.97f, 0.99f));

            // ONE ROW PER STAT, stacked. Each row is a single big tap target (see UiTouch): the old five-tiles-in-a-row
            // layout left buttons about 9 dp tall.
            GameObject list = UiFactory.Node("Stats", row.transform);
            UiFactory.Anchor(list.GetComponent<RectTransform>(), new Vector2(0.02f, listBottom), new Vector2(0.98f, listTop));
            UiFactory.VerticalStack(list, UiSpace.RowGap, UiSpace.Padding(UiSpace.None));

            HeroUpgradeRowUI.StatRow[] statRows =
            {
                CreateStatRow(list.transform, HeroStatType.Attack),
                CreateStatRow(list.transform, HeroStatType.Health),
                CreateStatRow(list.transform, HeroStatType.Defense),
                CreateStatRow(list.transform, HeroStatType.CritRate),
                CreateStatRow(list.transform, HeroStatType.CritDamage)
            };

            HeroUpgradeRowUI ui = row.AddComponent<HeroUpgradeRowUI>();
            SceneWiringUtility.SetField(ui, "heroIndex", index);
            SceneWiringUtility.SetField(ui, "heroNameLabel", nameLabel);
            SceneWiringUtility.SetField(ui, "rows", statRows);
            return ui;
        }

        /// <summary>
        /// One stat row: name + effect on the left, level + cost in the middle, and TWO finger-sized buttons on the
        /// right - "+1" and "x10" (each at least <see cref="UiTouch.MinTarget"/> wide).
        ///
        /// The row face is decoration, not a button: a full-width button inside a scrolling list also fires when the
        /// player flicks the list and lets go over the same row, which would buy upgrades by accident.
        /// </summary>
        private static HeroUpgradeRowUI.StatRow CreateStatRow(Transform parent, HeroStatType statType)
        {
            GameObject rowObject = UiFactory.Node($"Stat{statType}", parent);

            LayoutElement layout = rowObject.AddComponent<LayoutElement>();
            layout.minHeight = UiTouch.RowHeight;
            layout.preferredHeight = UiTouch.RowHeight;

            Image background = UiFactory.Panel("Background", rowObject.transform, "ui_panel_light", new Color(1f, 1f, 1f, 0.28f));
            UiFactory.Stretch(background.rectTransform);

            TextMeshProUGUI name = UiFactory.Text("Name", rowObject.transform, statType.ToDisplayName(), UiType.Role.Section,
                TextAlignmentOptions.MidlineLeft, TextColor);
            UiFactory.Anchor(name.rectTransform, new Vector2(0.03f, 0.52f), new Vector2(0.40f, 0.94f));

            TextMeshProUGUI effect = UiFactory.Text("Effect", rowObject.transform, string.Empty, UiType.Role.Sentence,
                TextAlignmentOptions.MidlineLeft, DimTextColor);
            // The effect and the cost carry data whose length grows with the game (a long effect line, a big price),
            // so they auto-fit their fixed cells instead of spilling into the buttons beside them.
            UiType.ApplyAuto(effect, UiType.Role.Sentence);
            UiFactory.Anchor(effect.rectTransform, new Vector2(0.03f, 0.06f), new Vector2(0.40f, 0.48f));

            TextMeshProUGUI level = UiFactory.Text("Level", rowObject.transform, "Lv 0", UiType.Role.Small,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(level.rectTransform, new Vector2(0.41f, 0.52f), new Vector2(0.56f, 0.94f));

            TextMeshProUGUI cost = UiFactory.Text("Cost", rowObject.transform, "0", UiType.Role.Small,
                TextAlignmentOptions.Center, DimTextColor);
            UiType.ApplyAuto(cost, UiType.Role.Small);
            UiFactory.Anchor(cost.rectTransform, new Vector2(0.41f, 0.06f), new Vector2(0.65f, 0.48f));

            Button plusOne = UiFactory.Button("PlusOne", rowObject.transform, "+1", "ui_button", UiType.Role.Section, TextColor, null);
            UiFactory.Anchor(plusOne.GetComponent<RectTransform>(), new Vector2(0.67f, 0.06f), new Vector2(0.82f, 0.94f));

            Button plusTen = UiFactory.Button("PlusTen", rowObject.transform, "x10", "ui_button_gold", UiType.Role.Section, TextColor, null);
            UiFactory.Anchor(plusTen.GetComponent<RectTransform>(), new Vector2(0.84f, 0.06f), new Vector2(0.99f, 0.94f));

            return new HeroUpgradeRowUI.StatRow
            {
                statType = statType,
                background = background,
                buyOneButton = plusOne,
                plusTenButton = plusTen,
                nameLabel = name,
                effectLabel = effect,
                levelLabel = level,
                costLabel = cost
            };
        }

        private static GameObject BuildAscensionPanel(Transform parent, List<PrestigeUpgradeData> prestigeUpgrades)
        {
            GameObject panel = UiFactory.Node("AscensionPanel", parent);
            UiFactory.Stretch(panel.GetComponent<RectTransform>());
            AscensionPanelUI ui = panel.AddComponent<AscensionPanelUI>();

            TextMeshProUGUI yieldLabel = UiFactory.Text("Yield", panel.transform, "Ascend for 0 tokens", UiType.Role.Section,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(yieldLabel.rectTransform, new Vector2(0.03f, BandTitleBottom), new Vector2(0.97f, BandTop));

            TextMeshProUGUI requirementLabel = UiFactory.Text("Requirement", panel.transform, "Best stage 1", UiType.Role.Sentence,
                TextAlignmentOptions.Center, DimTextColor);
            UiFactory.Anchor(requirementLabel.rectTransform, new Vector2(0.03f, BandSubLineBottom), new Vector2(0.97f, BandTitleBottom));

            Button ascendButton = UiFactory.Button("AscendButton", panel.transform, "ASCEND", "ui_button_gold", UiType.Role.Section, TextColor, null);
            UiFactory.Anchor(ascendButton.GetComponent<RectTransform>(), new Vector2(0.24f, BandActionBottom), new Vector2(0.76f, BandActionTop));
            TextMeshProUGUI ascendButtonLabel = ascendButton.GetComponentInChildren<TextMeshProUGUI>();

            ScrollRect scroll = UiFactory.CreateScrollView(panel.transform, "PrestigeScroll", UiSpace.Small, UiSpace.Padding(UiSpace.Tight), out RectTransform list);
            UiFactory.Anchor(scroll.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, BandListTop), UiSpace.Hairline, UiSpace.Hairline, UiSpace.Hairline, UiSpace.None);

            PrestigeUpgradeRowUI[] rows = new PrestigeUpgradeRowUI[TokenRowCount(prestigeUpgrades)];

            int rowIndex = 0;

            for (int i = 0; i < prestigeUpgrades.Count; i++)
            {
                // B7 S4: gems-priced tracks are NOT ascend rows - they list on the shop shelf instead.
                PrestigeUpgradeData data = prestigeUpgrades[i];

                if (data == null || data.CostCurrency != IdleRPG.Economy.CurrencyType.PrestigeTokens)
                {
                    continue;
                }

                rows[rowIndex] = CreatePrestigeRow(list, rowIndex, data);
                rowIndex++;
            }

            SceneWiringUtility.SetField(ui, "yieldLabel", yieldLabel);
            SceneWiringUtility.SetField(ui, "requirementLabel", requirementLabel);
            SceneWiringUtility.SetField(ui, "ascendButton", ascendButton);
            SceneWiringUtility.SetField(ui, "ascendButtonLabel", ascendButtonLabel);
            SceneWiringUtility.SetField(ui, "prestigeRows", rows);
            return panel;
        }

        /// <summary>How many ASCEND rows the panel needs: token-priced upgrades only (B7 S4).</summary>
        private static int TokenRowCount(List<PrestigeUpgradeData> prestigeUpgrades)
        {
            int count = 0;

            if (prestigeUpgrades == null)
            {
                return 0;
            }

            for (int i = 0; i < prestigeUpgrades.Count; i++)
            {
                if (prestigeUpgrades[i] != null &&
                    prestigeUpgrades[i].CostCurrency == IdleRPG.Economy.CurrencyType.PrestigeTokens)
                {
                    count++;
                }
            }

            return count;
        }

        private static PrestigeUpgradeRowUI CreatePrestigeRow(Transform parent, int index, PrestigeUpgradeData data)
        {
            GameObject row = UiFactory.Node($"PrestigeRow{index}", parent);
            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.minHeight = UiTouch.RowHeight;
            layout.preferredHeight = UiTouch.RowHeight;

            Image background = UiFactory.Panel("Background", row.transform, "ui_panel", new Color(1f, 1f, 1f, 0.30f));
            UiFactory.Stretch(background.rectTransform);

            TextMeshProUGUI nameLabel = UiFactory.Text("Name", row.transform,
                data != null ? data.DisplayName : $"Upgrade {index + 1}", UiType.Role.Section, TextAlignmentOptions.MidlineLeft, TextColor);
            UiFactory.Anchor(nameLabel.rectTransform, new Vector2(0.03f, 0.58f), new Vector2(0.56f, 0.96f));

            TextMeshProUGUI effectLabel = UiFactory.Text("Effect", row.transform, "+0%", UiType.Role.Sentence,
                TextAlignmentOptions.MidlineLeft, DimTextColor);
            UiFactory.Anchor(effectLabel.rectTransform, new Vector2(0.03f, 0.12f), new Vector2(0.56f, 0.55f));

            TextMeshProUGUI levelLabel = UiFactory.Text("Level", row.transform, "Lv 0", UiType.Role.Small,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(levelLabel.rectTransform, new Vector2(0.56f, 0.58f), new Vector2(0.76f, 0.96f));

            TextMeshProUGUI costLabel = UiFactory.Text("Cost", row.transform, "1 token", UiType.Role.Small,
                TextAlignmentOptions.Center, DimTextColor);
            UiFactory.Anchor(costLabel.rectTransform, new Vector2(0.56f, 0.12f), new Vector2(0.76f, 0.55f));

            // A full-height buy button: at least UiTouch.MinTarget tall so a thumb can hit it.
            Button buyButton = UiFactory.Button("BuyButton", row.transform, "BUY", "ui_button", UiType.Role.Section, TextColor, null);
            UiFactory.Anchor(buyButton.GetComponent<RectTransform>(), new Vector2(0.78f, 0.05f), new Vector2(0.97f, 0.95f));

            PrestigeUpgradeRowUI ui = row.AddComponent<PrestigeUpgradeRowUI>();
            SceneWiringUtility.SetField(ui, "nameLabel", nameLabel);
            SceneWiringUtility.SetField(ui, "effectLabel", effectLabel);
            SceneWiringUtility.SetField(ui, "levelLabel", levelLabel);
            SceneWiringUtility.SetField(ui, "costLabel", costLabel);
            SceneWiringUtility.SetField(ui, "buyButton", buyButton);
            return ui;
        }

        private static GameObject BuildShopPanel(Transform parent)
        {
            GameObject panel = UiFactory.Node("ShopPanel", parent);
            UiFactory.Stretch(panel.GetComponent<RectTransform>());
            ShopPanelUI ui = panel.AddComponent<ShopPanelUI>();

            // Header band, mirroring the ASCEND page: title, sub-line, one big gold action button.
            TextMeshProUGUI title = UiFactory.Text("Title", panel.transform, "Shop", UiType.Role.Section,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(title.rectTransform, new Vector2(0.03f, BandTitleBottom), new Vector2(0.97f, BandTop));

            TextMeshProUGUI gemsLabel = UiFactory.Text("Gems", panel.transform, "0 gems", UiType.Role.Sentence,
                TextAlignmentOptions.Center, DimTextColor);
            UiFactory.Anchor(gemsLabel.rectTransform, new Vector2(0.03f, BandSubLineBottom), new Vector2(0.97f, BandTitleBottom));

            // The headline action (same slot as the ASCEND button): watch an ad for a gold boost.
            Button watchAdButton = UiFactory.Button("WatchAdButton", panel.transform, "WATCH AD", "ui_button_gold", UiType.Role.Section, TextColor, null);
            UiFactory.Anchor(watchAdButton.GetComponent<RectTransform>(), new Vector2(0.24f, BandActionBottom), new Vector2(0.76f, BandActionTop));
            TextMeshProUGUI watchAdLabel = watchAdButton.GetComponentInChildren<TextMeshProUGUI>();

            // The offer shelf, styled like the prestige tree (same scroll band, same card rows).
            ScrollRect scroll = UiFactory.CreateScrollView(panel.transform, "Offers", UiSpace.Small, UiSpace.Padding(UiSpace.Tight), out RectTransform offerRoot);
            UiFactory.Anchor(scroll.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, BandListTop), UiSpace.Hairline, UiSpace.Hairline, UiSpace.Hairline, UiSpace.None);

            SceneWiringUtility.SetField(ui, "gemsLabel", gemsLabel);
            SceneWiringUtility.SetField(ui, "watchAdButton", watchAdButton);
            SceneWiringUtility.SetField(ui, "watchAdLabel", watchAdLabel);
            SceneWiringUtility.SetField(ui, "offerRoot", offerRoot);
            SceneWiringUtility.SetField(ui, "rowSprite", UiFactory.LoadSprite("ui_panel"));
            SceneWiringUtility.SetField(ui, "buttonSprite", UiFactory.LoadSprite("ui_button"));
            return panel;
        }
    }
}
