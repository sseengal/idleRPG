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
        // Control dock
        // ------------------------------------------------------------------
        private static TabController BuildManagementPage(RectTransform pageRoot, PartyConfig partyConfig,
            List<PrestigeUpgradeData> prestigeUpgrades)
        {
            Image shell = UiFactory.Panel("ManagementShell", pageRoot, "ui_panel", new Color(1f, 1f, 1f, 0.98f));
            UiFactory.Stretch(shell.rectTransform, 14f, 6f, 14f, 6f);
            TabController tabs = shell.gameObject.AddComponent<TabController>();

            GameObject panelsRoot = UiFactory.Node("Panels", shell.transform);
            UiFactory.Anchor(panelsRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 10f, 10f, 10f, 10f);

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
            UiFactory.Anchor(bar.rectTransform, Vector2.zero, new Vector2(1f, NavBarTop), 14f, 10f, 14f, 6f);
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
                UiFactory.Anchor(buttonImage.rectTransform, new Vector2(xMin, 0.08f), new Vector2(xMin + width, 0.92f), 6f, 0f, 6f, 0f);

                Button button = buttonImage.gameObject.AddComponent<Button>();
                button.targetGraphic = buttonImage;

                TextMeshProUGUI label = UiFactory.Text("Label", buttonImage.transform, labels[i], 26f,
                    TextAlignmentOptions.Center, UiTheme.Text);
                UiTheme.ApplyFont(label, UiTheme.Display, 26f);
                UiFactory.Stretch(label.rectTransform, 8f, 4f, 8f, 4f);

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
                "Spend gold to raise each hero's stats (scroll for more)", 22f,
                TextAlignmentOptions.MidlineLeft, DimTextColor);
            UiFactory.Anchor(title.rectTransform, new Vector2(0.02f, 0.93f), new Vector2(0.98f, 1f));

            ScrollRect scroll = UiFactory.CreateScrollView(panel.transform, "UpgradeScroll", 10f, new RectOffset(4, 4, 4, 4), out RectTransform list);
            UiFactory.Anchor(scroll.GetComponent<RectTransform>(), new Vector2(0f, 0.26f), new Vector2(1f, 0.90f), 2f, 2f, 2f, 0f);

            HeroUpgradeRowUI[] rows = new HeroUpgradeRowUI[Mathf.Max(1, partyConfig != null ? partyConfig.ValidHeroCount : 1)];

            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = CreateHeroUpgradeRow(list, i, partyConfig);
            }

            SceneWiringUtility.SetField(ui, "rows", rows);

            // B6 Step 2: the AUTO strip (auto-buy + speed cards) sits at the bottom of the page.
            GameObject automationStrip = UiFactory.Node("AutomationStrip", panel.transform);
            UiFactory.Anchor(automationStrip.GetComponent<RectTransform>(),
                new Vector2(0f, 0f), new Vector2(1f, 0.24f), 4f, 2f, 4f, 2f);
            automationStrip.AddComponent<AutomationPanelUI>();

            return panel;
        }

        private static HeroUpgradeRowUI CreateHeroUpgradeRow(Transform parent, int index, PartyConfig partyConfig)
        {
            GameObject row = UiFactory.Node($"HeroRow{index}", parent);
            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.minHeight = 320f;
            layout.preferredHeight = 320f;

            Image background = UiFactory.Panel("Background", row.transform, "ui_panel", new Color(1f, 1f, 1f, 0.30f));
            UiFactory.Stretch(background.rectTransform);

            HeroData hero = partyConfig != null ? partyConfig.GetHero(index) : null;
            TextMeshProUGUI nameLabel = UiFactory.Text("Name", row.transform,
                hero != null ? hero.HeroName : $"Hero {index + 1}", 28f, TextAlignmentOptions.MidlineLeft, TextColor);
            UiFactory.Anchor(nameLabel.rectTransform, new Vector2(0.03f, 0.76f), new Vector2(0.97f, 0.99f));

            GameObject blocks = UiFactory.Node("Blocks", row.transform);
            UiFactory.Anchor(blocks.GetComponent<RectTransform>(), new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.74f));

            // Five stats: three across the top row, two on the second (a spacer keeps the tile width equal).
            UiFactory.VerticalStack(blocks, 6f, new RectOffset(0, 0, 0, 0), expandChildren: true);

            GameObject rowTop = UiFactory.Node("RowTop", blocks.transform);
            UiFactory.HorizontalStack(rowTop, 8f);

            GameObject rowBottom = UiFactory.Node("RowBottom", blocks.transform);
            UiFactory.HorizontalStack(rowBottom, 8f);

            HeroUpgradeRowUI.StatBlock[] statBlocks =
            {
                CreateStatBlock(rowTop.transform, HeroStatType.Attack, "ui_icon_gold"),
                CreateStatBlock(rowTop.transform, HeroStatType.Health, "ui_icon_gem"),
                CreateStatBlock(rowTop.transform, HeroStatType.Defense, "ui_icon_token"),
                CreateStatBlock(rowBottom.transform, HeroStatType.CritRate, "ui_icon_gold"),
                CreateStatBlock(rowBottom.transform, HeroStatType.CritDamage, "ui_icon_gem")
            };

            UiFactory.Node("Spacer", rowBottom.transform);

            HeroUpgradeRowUI ui = row.AddComponent<HeroUpgradeRowUI>();
            SceneWiringUtility.SetField(ui, "heroIndex", index);
            SceneWiringUtility.SetField(ui, "heroNameLabel", nameLabel);
            SceneWiringUtility.SetField(ui, "blocks", statBlocks);
            return ui;
        }

        private static HeroUpgradeRowUI.StatBlock CreateStatBlock(Transform parent, HeroStatType statType, string iconSprite)
        {
            GameObject block = UiFactory.Node($"Stat{statType}", parent);

            Image background = UiFactory.Panel("Background", block.transform, "ui_panel_light", new Color(1f, 1f, 1f, 0.28f));
            UiFactory.Stretch(background.rectTransform);

            TextMeshProUGUI levelLabel = UiFactory.Text("Level", block.transform, "Lv 0", 26f,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(levelLabel.rectTransform, new Vector2(0.04f, 0.70f), new Vector2(0.96f, 0.98f));

            TextMeshProUGUI effectLabel = UiFactory.Text("Effect", block.transform, "+0%", 18f,
                TextAlignmentOptions.Center, DimTextColor);
            UiFactory.Anchor(effectLabel.rectTransform, new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.70f));

            TextMeshProUGUI costLabel = UiFactory.Text("Cost", block.transform, "0", 20f,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(costLabel.rectTransform, new Vector2(0.04f, 0.32f), new Vector2(0.96f, 0.52f));

            Button plusOne = UiFactory.Button("PlusOne", block.transform, "+1", "ui_button", 22f, TextColor, null);
            UiFactory.Anchor(plusOne.GetComponent<RectTransform>(), new Vector2(0.05f, 0.04f), new Vector2(0.47f, 0.30f));

            Button plusTen = UiFactory.Button("PlusTen", block.transform, "+10", "ui_button_gold", 22f, TextColor, null);
            UiFactory.Anchor(plusTen.GetComponent<RectTransform>(), new Vector2(0.53f, 0.04f), new Vector2(0.95f, 0.30f));

            return new HeroUpgradeRowUI.StatBlock
            {
                statType = statType,
                plusOneButton = plusOne,
                plusTenButton = plusTen,
                levelLabel = levelLabel,
                costLabel = costLabel,
                effectLabel = effectLabel,
                iconImage = null,
                iconSprite = UiFactory.LoadSprite(iconSprite)
            };
        }

        private static GameObject BuildAscensionPanel(Transform parent, List<PrestigeUpgradeData> prestigeUpgrades)
        {
            GameObject panel = UiFactory.Node("AscensionPanel", parent);
            UiFactory.Stretch(panel.GetComponent<RectTransform>());
            AscensionPanelUI ui = panel.AddComponent<AscensionPanelUI>();

            TextMeshProUGUI yieldLabel = UiFactory.Text("Yield", panel.transform, "Ascend for 0 tokens", 30f,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(yieldLabel.rectTransform, new Vector2(0.03f, 0.88f), new Vector2(0.97f, 1f));

            TextMeshProUGUI requirementLabel = UiFactory.Text("Requirement", panel.transform, "Best stage 1", 22f,
                TextAlignmentOptions.Center, DimTextColor);
            UiFactory.Anchor(requirementLabel.rectTransform, new Vector2(0.03f, 0.79f), new Vector2(0.97f, 0.88f));

            Button ascendButton = UiFactory.Button("AscendButton", panel.transform, "ASCEND", "ui_button_gold", 34f, TextColor, null);
            UiFactory.Anchor(ascendButton.GetComponent<RectTransform>(), new Vector2(0.24f, 0.60f), new Vector2(0.76f, 0.77f));
            TextMeshProUGUI ascendButtonLabel = ascendButton.GetComponentInChildren<TextMeshProUGUI>();

            ScrollRect scroll = UiFactory.CreateScrollView(panel.transform, "PrestigeScroll", 8f, new RectOffset(4, 4, 4, 4), out RectTransform list);
            UiFactory.Anchor(scroll.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.57f), 2f, 2f, 2f, 0f);

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
            layout.minHeight = 116f;
            layout.preferredHeight = 116f;

            Image background = UiFactory.Panel("Background", row.transform, "ui_panel", new Color(1f, 1f, 1f, 0.30f));
            UiFactory.Stretch(background.rectTransform);

            TextMeshProUGUI nameLabel = UiFactory.Text("Name", row.transform,
                data != null ? data.DisplayName : $"Upgrade {index + 1}", 26f, TextAlignmentOptions.MidlineLeft, TextColor);
            UiFactory.Anchor(nameLabel.rectTransform, new Vector2(0.03f, 0.58f), new Vector2(0.56f, 0.96f));

            TextMeshProUGUI effectLabel = UiFactory.Text("Effect", row.transform, "+0%", 20f,
                TextAlignmentOptions.MidlineLeft, DimTextColor);
            UiFactory.Anchor(effectLabel.rectTransform, new Vector2(0.03f, 0.12f), new Vector2(0.56f, 0.55f));

            TextMeshProUGUI levelLabel = UiFactory.Text("Level", row.transform, "Lv 0", 24f,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(levelLabel.rectTransform, new Vector2(0.56f, 0.58f), new Vector2(0.76f, 0.96f));

            TextMeshProUGUI costLabel = UiFactory.Text("Cost", row.transform, "1 token", 20f,
                TextAlignmentOptions.Center, DimTextColor);
            UiFactory.Anchor(costLabel.rectTransform, new Vector2(0.56f, 0.12f), new Vector2(0.76f, 0.55f));

            Button buyButton = UiFactory.Button("BuyButton", row.transform, "BUY", "ui_button", 24f, TextColor, null);
            UiFactory.Anchor(buyButton.GetComponent<RectTransform>(), new Vector2(0.78f, 0.16f), new Vector2(0.97f, 0.86f));

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
            TextMeshProUGUI title = UiFactory.Text("Title", panel.transform, "Shop", 30f,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(title.rectTransform, new Vector2(0.03f, 0.88f), new Vector2(0.97f, 1f));

            TextMeshProUGUI gemsLabel = UiFactory.Text("Gems", panel.transform, "0 gems", 22f,
                TextAlignmentOptions.Center, DimTextColor);
            UiFactory.Anchor(gemsLabel.rectTransform, new Vector2(0.03f, 0.79f), new Vector2(0.97f, 0.88f));

            // The headline action (same slot as the ASCEND button): watch an ad for a gold boost.
            Button watchAdButton = UiFactory.Button("WatchAdButton", panel.transform, "WATCH AD", "ui_button_gold", 30f, TextColor, null);
            UiFactory.Anchor(watchAdButton.GetComponent<RectTransform>(), new Vector2(0.24f, 0.60f), new Vector2(0.76f, 0.77f));
            TextMeshProUGUI watchAdLabel = watchAdButton.GetComponentInChildren<TextMeshProUGUI>();

            // The offer shelf, styled like the prestige tree (same scroll band, same card rows).
            ScrollRect scroll = UiFactory.CreateScrollView(panel.transform, "Offers", 8f, new RectOffset(4, 4, 4, 4), out RectTransform offerRoot);
            UiFactory.Anchor(scroll.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.57f), 2f, 2f, 2f, 0f);

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
