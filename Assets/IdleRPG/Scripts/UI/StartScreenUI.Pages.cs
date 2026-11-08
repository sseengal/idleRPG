using System.Collections.Generic;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// The lobby pages beside TRAINING: PARTY (the read-only roster, hosted as-is) and ASCENSION / PERKS
    /// (one row per token track, from AscensionManager.TokenUpgrades). Both build on first open and sit in the
    /// same panel band as the training page.
    /// </summary>
    public sealed partial class StartScreenUI
    {
        private GameObject partyPanel;
        private bool partyBuilt;

        private GameObject perksPanel;
        private bool perksBuilt;
        private TextMeshProUGUI perksTitle;
        private readonly List<PerkRow> perkRows = new List<PerkRow>();

        private sealed class PerkRow
        {
            public PrestigeUpgradeData Data;
            public TextMeshProUGUI EffectLabel;
            public TextMeshProUGUI LevelLabel;
            public Button BuyButton;
            public TextMeshProUGUI BuyLabel;
        }

        // ------------------------------------------------------------------
        // PARTY
        // ------------------------------------------------------------------
        private void ShowParty()
        {
            EnsureManager();

            if (!partyBuilt)
            {
                partyPanel = CreateLobbyPanel("PartyPanel");
                partyPanel.AddComponent<PartyPanelUI>();   // the roster builds itself on enable
                partyBuilt = true;
            }

            partyPanel.SetActive(true);
        }

        private void HideParty()
        {
            if (partyPanel != null && partyPanel.activeSelf)
            {
                partyPanel.SetActive(false);
            }
        }

        // ------------------------------------------------------------------
        // ASCENSION / PERKS
        // ------------------------------------------------------------------
        private void ShowPerks()
        {
            EnsureManager();

            if (!perksBuilt)
            {
                BuildPerksPanel();
            }

            perksPanel.SetActive(true);
            RefreshPerks();
        }

        private void HidePerks()
        {
            if (perksPanel != null && perksPanel.activeSelf)
            {
                perksPanel.SetActive(false);
            }
        }

        /// <summary>One lobby page panel in the shared band (the training page uses the same box).</summary>
        private GameObject CreateLobbyPanel(string name)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.SetParent(lobby.transform, false);
            rect.anchorMin = new Vector2(0.02f, 0.13f);
            rect.anchorMax = new Vector2(0.98f, 0.885f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.13f, 1f);
            return panel;
        }

        private void BuildPerksPanel()
        {
            perksBuilt = true;
            perkRows.Clear();
            perksPanel = CreateLobbyPanel("PerksPanel");

            perksTitle = Text(perksPanel.transform, "Title", new Vector2(0.03f, 0.90f), new Vector2(0.97f, 0.98f),
                "PERK POINTS 0", UiType.Role.Section, new Color(0.98f, 0.80f, 0.28f, 1f));

            Text(perksPanel.transform, "Sub", new Vector2(0.03f, 0.845f), new Vector2(0.97f, 0.90f),
                "Permanent multipliers - never reset", UiType.Role.Micro, new Color(0.6f, 0.63f, 0.72f, 1f));

            if (manager == null || manager.Ascension == null)
            {
                return;
            }

            IReadOnlyList<PrestigeUpgradeData> tracks = manager.Ascension.TokenUpgrades;
            float rowHeight = 0.125f;

            for (int i = 0; i < tracks.Count; i++)
            {
                PrestigeUpgradeData data = tracks[i];

                if (data == null)
                {
                    continue;
                }

                float top = 0.83f - i * rowHeight;
                BuildPerkRow(perksPanel.transform, data, top, rowHeight);
            }
        }

        private void BuildPerkRow(Transform parent, PrestigeUpgradeData data, float top, float rowHeight)
        {
            float bottom = top - rowHeight;
            Panel(parent, "PerkRow", new Vector2(0.02f, bottom), new Vector2(0.98f, top), new Color(0.13f, 0.14f, 0.16f, 1f));

            TextMeshProUGUI name = Text(parent, "Name", new Vector2(0.04f, top - 0.062f), new Vector2(0.52f, top - 0.008f),
                data.DisplayName, UiType.Role.Section, Color.white);
            name.alignment = TextAlignmentOptions.MidlineLeft;

            TextMeshProUGUI effect = Text(parent, "Effect", new Vector2(0.04f, bottom + 0.012f), new Vector2(0.66f, top - 0.066f),
                string.Empty, UiType.Role.Small, new Color(0.75f, 0.78f, 0.86f, 1f));
            effect.alignment = TextAlignmentOptions.MidlineLeft;
            effect.textWrappingMode = TextWrappingModes.NoWrap;
            UiType.ApplyAuto(effect, UiType.Role.Small);

            TextMeshProUGUI level = Text(parent, "Level", new Vector2(0.56f, top - 0.062f), new Vector2(0.71f, top - 0.008f),
                string.Empty, UiType.Role.Small, new Color(0.98f, 0.80f, 0.28f, 1f));
            level.alignment = TextAlignmentOptions.MidlineRight;
            level.textWrappingMode = TextWrappingModes.NoWrap;
            UiType.ApplyAuto(level, UiType.Role.Small);

            Button buy = Btn(parent, "Buy", new Vector2(0.73f, bottom + 0.008f), new Vector2(0.98f, top - 0.008f),
                "Buy", new Color(0.95f, 0.55f, 0.15f, 1f), UiType.Role.Small, () => BuyPerk(data));

            MakeCardIcon(buy.transform, StarSprite(), new Color(0.80f, 0.55f, 0.98f, 1f));

            perkRows.Add(new PerkRow
            {
                Data = data,
                EffectLabel = effect,
                LevelLabel = level,
                BuyButton = buy,
                BuyLabel = buy.GetComponentInChildren<TextMeshProUGUI>()
            });
        }

        private void RefreshPerks()
        {
            EnsureManager();

            if (!perksBuilt || manager == null || manager.Ascension == null)
            {
                return;
            }

            double held = manager.Economy != null ? manager.Economy.PrestigeTokens : 0d;

            if (perksTitle != null)
            {
                perksTitle.SetText("PERK POINTS " + NumberFormatter.Format(held));
            }

            for (int i = 0; i < perkRows.Count; i++)
            {
                PerkRow row = perkRows[i];
                PrestigeUpgradeData data = row.Data;
                int level = manager.Ascension.GetPrestigeLevel(data);
                bool maxed = manager.Ascension.IsPrestigeMaxed(data);
                double cost = manager.Ascension.GetPrestigeCost(data, 1);
                bool affordable = manager.Ascension.CanAffordPrestige(data, 1);

                if (row.LevelLabel != null)
                {
                    row.LevelLabel.SetText(data.HasLevelCap ? level + "/" + data.MaxLevel : level.ToString());
                }

                if (row.EffectLabel != null)
                {
                    if (maxed)
                    {
                        row.EffectLabel.SetText("Maxed.");
                    }
                    else if (!affordable)
                    {
                        row.EffectLabel.SetText("Need " + NumberFormatter.Format(cost) + " tokens - you have " + NumberFormatter.Format(held) + ".");
                    }
                    else
                    {
                        row.EffectLabel.SetText(PerkEffectLine(data));
                    }
                }

                if (row.BuyButton != null)
                {
                    row.BuyButton.interactable = !maxed && affordable;
                }

                if (row.BuyLabel != null)
                {
                    row.BuyLabel.SetText(maxed ? "MAX" : "Buy " + NumberFormatter.Format(cost));
                }
            }
        }

        private void BuyPerk(PrestigeUpgradeData data)
        {
            if (manager == null || manager.Ascension == null || data == null)
            {
                return;
            }

            if (manager.Ascension.TryBuyPrestigeUpgrade(data, 1))
            {
                RefreshPerks();
            }
        }

        /// <summary>"+10% damage per level" - the percent tracks are the only ones on this page.</summary>
        private static string PerkEffectLine(PrestigeUpgradeData data)
        {
            float percent = data.EffectPerLevel * 100f;
            string amount = percent.ToString("0.##") + "%";

            switch (data.EffectType)
            {
                case PrestigeEffectType.DamagePercent:
                    return "+" + amount + " damage per level";
                case PrestigeEffectType.GoldPercent:
                    return "+" + amount + " gold per level";
                case PrestigeEffectType.HealthPercent:
                    return "+" + amount + " health per level";
                default:
                    return string.IsNullOrEmpty(data.Description) ? "+" + amount + " per level" : data.Description;
            }
        }
    }
}
