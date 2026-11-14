using System.Collections.Generic;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Economy;
using IdleRPG.Progression;
using IdleRPG.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// TRAINING tab: one Gold chain per hero (3 columns) plus the party track paid with Perk Tokens (4th column).
    /// The party track is gated by the LOWEST hero's chain depth. Opening a node shows a card beside it; the card
    /// says why a node cannot be bought yet (gate, currency) or removes the button when there is nothing to buy.
    /// </summary>
    public sealed partial class StartScreenUI
    {
        private const int NodesPerLevel = 6;
        private const float LevelGap = 96f;   // breathing room between levels, where the level bar sits
        private const float ScrollButtonThreshold = NodesPerLevel * 220f + LevelGap;   // ~one level of scrolling

        private int treeHeroRows;
        private int treeLevelCount;
        private int treeNodeLayer;
        private GameObject scrollDownButton;

        private Image treeFill;
        private int popupColumn;

        private void ShowTraining()
        {
            EnsureManager();

            if (!treeBuilt)
            {
                BuildTrainingTree();
            }

            if (trainingPanel != null)
            {
                trainingPanel.SetActive(true);
            }

            RefreshTree();
            ScrollTreeToBottom();
        }

        private void HideTraining()
        {
            ClosePopup();

            if (trainingPanel != null && trainingPanel.activeSelf)
            {
                trainingPanel.SetActive(false);
            }
        }

        private void BuildTrainingTree()
        {
            treeBuilt = true;
            EnsureManager();

            GameObject panel = new GameObject("TrainingPanel", typeof(RectTransform), typeof(Image));
            RectTransform pr = panel.GetComponent<RectTransform>();
            pr.SetParent(lobby.transform, false);
            pr.anchorMin = new Vector2(0.02f, 0.13f);
            pr.anchorMax = new Vector2(0.98f, 0.885f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;
            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = UiTheme.PageSunken;
            panelImage.sprite = UiRuntime.PlateSprite;
            panelImage.type = Image.Type.Sliced;
            trainingPanel = panel;

            BuildColumnHeaders(panel.transform);

            GameObject scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            RectTransform sr = scrollGO.GetComponent<RectTransform>();
            sr.SetParent(panel.transform, false);
            sr.anchorMin = new Vector2(0f, 0f);
            sr.anchorMax = new Vector2(1f, TreeHeaderFraction);
            sr.offsetMin = Vector2.zero;
            sr.offsetMax = Vector2.zero;
            scrollGO.GetComponent<Image>().color = UiTheme.Row;
            treeScroll = scrollGO.GetComponent<ScrollRect>();
            treeScroll.horizontal = false;
            treeScroll.vertical = true;
            treeScroll.movementType = ScrollRect.MovementType.Clamped;
            treeScroll.scrollSensitivity = 40f;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            RectTransform vr = viewport.GetComponent<RectTransform>();
            vr.SetParent(scrollGO.transform, false);
            vr.anchorMin = Vector2.zero;
            vr.anchorMax = Vector2.one;
            vr.offsetMin = Vector2.zero;
            vr.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = Color.clear;
            treeScroll.viewport = vr;

            TrainingTreeDef tree = manager != null && manager.Training != null ? manager.Training.Tree : null;

            int heroRows = 0;
            List<TrainingNode> partyNodes = new List<TrainingNode>();

            if (tree != null)
            {
                IReadOnlyList<TrainingNode> nodes = tree.Nodes;

                for (int i = 0; i < nodes.Count; i++)
                {
                    TrainingNode node = nodes[i];

                    if (node == null)
                    {
                        continue;
                    }

                    if (node.Branch == TrainingBranch.Left)
                    {
                        if (node.Order + 1 > heroRows)
                        {
                            heroRows = node.Order + 1;
                        }
                    }
                    else if (node.IsPartyNode)
                    {
                        partyNodes.Add(node);
                    }
                }
            }

            treeHeroRows = Mathf.Max(1, heroRows);
            treeLevelCount = Mathf.Max(1, Mathf.CeilToInt(treeHeroRows / (float)NodesPerLevel));
            treeContentHeight = treeTopPad * 2f + treeHeroRows * treeRowH + treeLevelCount * LevelGap;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            treeContent = content.GetComponent<RectTransform>();
            treeContent.SetParent(viewport.transform, false);
            treeContent.anchorMin = new Vector2(0f, 1f);
            treeContent.anchorMax = new Vector2(1f, 1f);
            treeContent.pivot = new Vector2(0.5f, 1f);
            treeContent.anchoredPosition = Vector2.zero;
            treeContent.sizeDelta = new Vector2(0f, treeContentHeight);
            treeScroll.content = treeContent;

            int heroColumns = Mathf.Min(3, ColumnX.Length - 1);
            treeColumns.Clear();

            for (int i = 0; i < heroColumns; i++)
            {
                HeroData hero = HeroForColumn(i);
                treeColumns.Add(new TreeColumn
                {
                    HeroId = hero != null ? hero.HeroID : string.Empty,
                    FrontierOrder = 0
                });
            }

            treeColumns.Add(new TreeColumn { HeroId = string.Empty, FrontierOrder = 0 });   // party

            // One continuous fill: it rises row by row with the weakest hero, and stops at the next party gate
            // that has not been bought yet. Drawn behind the connectors, nodes and level bars.
            GameObject fillGO = new GameObject("LevelFill", typeof(RectTransform), typeof(Image));
            RectTransform fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.SetParent(treeContent, false);
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 0f);
            fillRect.pivot = new Vector2(0.5f, 0f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.sizeDelta = new Vector2(0f, 0f);
            fillRect.anchoredPosition = Vector2.zero;
            treeFill = fillGO.GetComponent<Image>();
            treeFill.color = UiTheme.GoldGlow;
            treeFill.raycastTarget = false;

            treeNodeLayer = treeContent.childCount;   // the fill sits behind everything added below

            // Tap anywhere off a node to dismiss the card. It sits behind the nodes, so tapping a node still
            // switches the card, and drags still bubble to the ScrollRect, so scrolling keeps working.
            GameObject catcher = new GameObject("DismissCatcher", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform cap = catcher.GetComponent<RectTransform>();
            cap.SetParent(treeContent, false);
            cap.anchorMin = Vector2.zero;
            cap.anchorMax = Vector2.one;
            cap.offsetMin = Vector2.zero;
            cap.offsetMax = Vector2.zero;
            Image catcherImage = catcher.GetComponent<Image>();
            catcherImage.color = Color.clear;
            Button catcherButton = catcher.GetComponent<Button>();
            catcherButton.targetGraphic = catcherImage;
            catcherButton.transition = Selectable.Transition.None;
            catcherButton.onClick.AddListener(ClosePopup);
            catcher.transform.SetSiblingIndex(treeNodeLayer);

            if (tree != null)
            {
                IReadOnlyList<TrainingNode> nodes = tree.Nodes;

                for (int c = 0; c < heroColumns; c++)
                {
                    string heroId = treeColumns[c].HeroId;

                    if (heroId.Length == 0)
                    {
                        continue;
                    }

                    for (int i = 0; i < nodes.Count; i++)
                    {
                        TrainingNode node = nodes[i];

                        if (node == null || node.Branch != TrainingBranch.Left || node.HeroID != heroId)
                        {
                            continue;
                        }

                        float centerY = NodeYFromTop(node.Order);
                        float connector = node.Order < treeHeroRows - 1 ? ConnectorLength(node.Order, node.Order + 1) : 0f;
                        MakeNode(node, ColumnX[c], centerY, c, connector);
                    }
                }

                int partyColumn = treeColumns.Count - 1;

                for (int i = 0; i < partyNodes.Count; i++)
                {
                    int gate = Mathf.Clamp(partyNodes[i].RequiresLeftNodes, 0, Mathf.Max(0, treeHeroRows - 1));
                    int nextGate = i + 1 < partyNodes.Count
                        ? Mathf.Clamp(partyNodes[i + 1].RequiresLeftNodes, 0, Mathf.Max(0, treeHeroRows - 1))
                        : gate;

                    float partyY = NodeYFromTop(gate);
                    float partyConnector = i + 1 < partyNodes.Count ? ConnectorLength(gate, nextGate) : 0f;
                    MakeNode(partyNodes[i], ColumnX[partyColumn], partyY, partyColumn, partyConnector);
                }
            }

            // Level bars: a thin gold strip with blue text, centred in each level gap and drawn in front.
            for (int level = 0; level < treeLevelCount; level++)
            {
                float barFromBottom = treeTopPad + (level + 1) * NodesPerLevel * treeRowH + level * LevelGap + LevelGap * 0.5f;
                CreateLevelBar(treeContent, treeContentHeight - barFromBottom, level + 1);
            }

            scrollDownButton = IconBtn(panel.transform, "ScrollDown", new Vector2(0.845f, 0.005f),
                new Vector2(0.985f, 0.105f), ArrowSprite(false), ScrollTreeToBottom);

            if (treeScroll != null)
            {
                treeScroll.onValueChanged.AddListener(_ => RefreshScrollButton());
            }

            BuildPopup(treeContent);
            trainingPanel.SetActive(false);
        }

        /// <summary>Distance from the content top to the centre of the node at <paramref name="order"/>.</summary>
        private float NodeYFromTop(int order)
        {
            int level = Mathf.Clamp(order / NodesPerLevel, 0, Mathf.Max(0, treeLevelCount - 1));
            float fromBottom = treeTopPad + order * treeRowH + level * LevelGap + treeRowH * 0.5f;
            return treeContentHeight - fromBottom;
        }

        /// <summary>Centre-to-centre distance between two nodes, including the gaps of the levels between them.</summary>
        private float ConnectorLength(int lowerOrder, int upperOrder)
        {
            int gaps = Mathf.Abs(upperOrder / NodesPerLevel - lowerOrder / NodesPerLevel);
            return (upperOrder - lowerOrder) * treeRowH + gaps * LevelGap;
        }

        /// <summary>The chain position of a node: its order for a hero, its gate for the party.</summary>
        private static int OrderOf(TrainingNode node)
        {
            return node.Branch == TrainingBranch.Right ? node.RequiresLeftNodes : node.Order;
        }

        private void ScrollTreeToBottom()
        {
            if (treeScroll == null || treeContent == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            float viewH = treeScroll.viewport != null ? treeScroll.viewport.rect.height : 0f;
            treeContent.anchoredPosition = new Vector2(0f, Mathf.Max(0f, treeContentHeight - viewH));
            treeScroll.StopMovement();
            RefreshScrollButton();
        }

        // ------------------------------------------------------------------
        // Pinned column headers: who owns which track
        // ------------------------------------------------------------------
        private void BuildColumnHeaders(Transform parent)
        {
            GameObject strip = new GameObject("TreeHeader", typeof(RectTransform), typeof(Image));
            RectTransform sr = strip.GetComponent<RectTransform>();
            sr.SetParent(parent, false);
            sr.anchorMin = new Vector2(0f, TreeHeaderFraction);
            sr.anchorMax = new Vector2(1f, 1f);
            sr.offsetMin = Vector2.zero;
            sr.offsetMax = Vector2.zero;
            Image stripImage = strip.GetComponent<Image>();
            stripImage.color = UiTheme.Row;
            stripImage.sprite = UiRuntime.PlateSprite;
            stripImage.type = Image.Type.Sliced;

            int heroColumns = Mathf.Min(3, ColumnX.Length - 1);

            for (int i = 0; i < heroColumns; i++)
            {
                BuildHeroHeader(strip.transform, i, HeroForColumn(i));
            }

            BuildPartyHeader(strip.transform, ColumnX.Length - 1);
        }

        private void BuildHeroHeader(Transform parent, int column, HeroData hero)
        {
            GameObject cell = new GameObject("HeroColumn" + column, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform cr = cell.GetComponent<RectTransform>();
            cr.SetParent(parent, false);
            cr.anchorMin = new Vector2(0f, 0.5f);
            cr.anchorMax = new Vector2(0f, 0.5f);
            cr.pivot = new Vector2(0.5f, 0.5f);
            cr.sizeDelta = new Vector2(230f, 150f);
            cr.anchoredPosition = new Vector2(ColumnX[column], 0f);

            Image bg = cell.GetComponent<Image>();
            bg.color = UiTheme.RowLight;

            Button button = cell.GetComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
            int captured = column;
            button.onClick.AddListener(() => ScrollToColumn(captured));

            Coffee.UIEffects.UIEffect cellEffect = cell.AddComponent<Coffee.UIEffects.UIEffect>();
            UiPlateButton cellPlate = cell.AddComponent<UiPlateButton>();
            cellPlate.Bind(cell.GetComponent<RectTransform>(), bg, null, cellEffect, button, UiTheme.RowLight, UiTheme.RowLight);

            if (hero != null)
            {
                GameObject portraitGO = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                RectTransform prt = portraitGO.GetComponent<RectTransform>();
                prt.SetParent(cell.transform, false);
                prt.anchorMin = new Vector2(0f, 0.5f);
                prt.anchorMax = new Vector2(0f, 0.5f);
                prt.pivot = new Vector2(0f, 0.5f);
                prt.sizeDelta = new Vector2(84f, 84f);
                prt.anchoredPosition = new Vector2(10f, 0f);

                Image portrait = portraitGO.GetComponent<Image>();
                portrait.sprite = hero.HeroIcon;
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;
                portrait.color = hero.HeroIcon != null ? Color.white : UiTheme.InkGhost;

                TextMeshProUGUI nameLabel = Text(cell.transform, "Name", new Vector2(0.40f, 0.20f),
                    new Vector2(0.99f, 0.80f), hero.HeroName, UiType.Role.Small, Color.white);
                nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
                UiType.ApplyAuto(nameLabel, UiType.Role.Small);
            }
            else
            {
                Text(cell.transform, "Name", new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.82f),
                    "Empty", UiType.Role.Small, UiTheme.TextDim);
            }
        }

        private void BuildPartyHeader(Transform parent, int column)
        {
            GameObject cell = new GameObject("PartyColumn", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform cr = cell.GetComponent<RectTransform>();
            cr.SetParent(parent, false);
            cr.anchorMin = new Vector2(0f, 0.5f);
            cr.anchorMax = new Vector2(0f, 0.5f);
            cr.pivot = new Vector2(0.5f, 0.5f);
            cr.sizeDelta = new Vector2(252f, 150f);
            cr.anchoredPosition = new Vector2(ColumnX[column], 0f);

            Image bg = cell.GetComponent<Image>();
            bg.color = UiTheme.RowWarm;

            Button button = cell.GetComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => ScrollToColumn(column));

            Text(cell.transform, "Title", new Vector2(0.04f, 0.76f), new Vector2(0.96f, 0.99f),
                "PARTY", UiType.Role.Small, UiTheme.Gold);

            // The three small portraits, so the party track reads as "everyone".
            int count = manager != null && manager.Party != null ? manager.Party.Heroes.Count : 0;
            int shown = Mathf.Min(3, count);

            for (int i = 0; i < shown; i++)
            {
                HeroData hero = manager.Party.Heroes[i];

                if (hero == null)
                {
                    continue;
                }

                GameObject miniGO = new GameObject("Mini" + i, typeof(RectTransform), typeof(Image));
                RectTransform mr = miniGO.GetComponent<RectTransform>();
                mr.SetParent(cell.transform, false);
                mr.anchorMin = new Vector2(0.5f, 0.50f);
                mr.anchorMax = new Vector2(0.5f, 0.50f);
                mr.pivot = new Vector2(0.5f, 0.5f);
                mr.sizeDelta = new Vector2(34f, 34f);
                mr.anchoredPosition = new Vector2((i - (shown - 1) * 0.5f) * 40f, 0f);

                Image mini = miniGO.GetComponent<Image>();
                mini.sprite = hero.HeroIcon;
                mini.preserveAspect = true;
                mini.raycastTarget = false;
                mini.color = hero.HeroIcon != null ? Color.white : UiTheme.InkGhost;
            }


        }

        private HeroData HeroForColumn(int column)
        {
            if (manager == null || manager.Party == null || column < 0 || column >= manager.Party.Heroes.Count)
            {
                return null;
            }

            return manager.Party.Heroes[column];
        }

        private string HeroName(string heroId)
        {
            if (manager != null && manager.Party != null)
            {
                for (int i = 0; i < manager.Party.Heroes.Count; i++)
                {
                    HeroData hero = manager.Party.Heroes[i];

                    if (hero != null && hero.HeroID == heroId)
                    {
                        return hero.HeroName;
                    }
                }
            }

            return "a hero";
        }

        // ------------------------------------------------------------------
        // One node: diamond + icon + badge + connector
        // ------------------------------------------------------------------
        private void MakeNode(TrainingNode node, float xPx, float centerY, int column, float connectorLength)
        {
            Image connector = null;

            if (connectorLength > 0f)
            {
                GameObject conGO = new GameObject("Con", typeof(RectTransform), typeof(Image));
                RectTransform cr = conGO.GetComponent<RectTransform>();
                cr.SetParent(treeContent, false);
                cr.anchorMin = new Vector2(0f, 1f);
                cr.anchorMax = new Vector2(0f, 1f);
                cr.pivot = new Vector2(0.5f, 0.5f);
                cr.sizeDelta = new Vector2(12f, connectorLength);
                cr.anchoredPosition = new Vector2(xPx, -(centerY - connectorLength * 0.5f));
                cr.SetSiblingIndex(treeNodeLayer);   // behind the nodes, above the bands and level badges
                connector = conGO.GetComponent<Image>();
                connector.color = UiTheme.LineGrey;
            }

            GameObject nodeGO = new GameObject("Node_" + node.NodeID, typeof(RectTransform));
            RectTransform nr = nodeGO.GetComponent<RectTransform>();
            nr.SetParent(treeContent, false);
            nr.anchorMin = new Vector2(0f, 1f);
            nr.anchorMax = new Vector2(0f, 1f);
            nr.pivot = new Vector2(0.5f, 0.5f);
            nr.sizeDelta = new Vector2(NodeSize, NodeSize);
            nr.anchoredPosition = new Vector2(xPx, -centerY);

            GameObject diaGO = new GameObject("Diamond", typeof(RectTransform), typeof(Image));
            RectTransform dr = diaGO.GetComponent<RectTransform>();
            dr.SetParent(nodeGO.transform, false);
            dr.anchorMin = Vector2.zero;
            dr.anchorMax = Vector2.one;
            dr.offsetMin = new Vector2(16f, 16f);
            dr.offsetMax = new Vector2(-16f, -16f);
            dr.localRotation = UnityEngine.Quaternion.Euler(0f, 0f, 45f);
            Image diamond = diaGO.GetComponent<Image>();
            diamond.color = UiTheme.LineSoft;

            TextMeshProUGUI icon = Text(nodeGO.transform, "Icon", Vector2.zero, Vector2.one, IconLetter(node.Effect), UiType.Role.Section, Color.white);
            icon.raycastTarget = false;

            // Real art when the icon set has one for this effect. The medallion IS the node then: the diamond
            // and the letter both step aside, so the icon is the button (no frame behind it).
            Sprite nodeArt = trainingIcons != null ? trainingIcons.IconFor(node.Effect) : null;
            Image art = null;
            Image nodeGlow = null;

            if (nodeArt != null)
            {
                nodeGlow = UiRuntime.CreateIcon("Glow", nodeGO.transform);
                nodeGlow.sprite = UiRuntime.SoftGlowSprite;
                UiRuntime.Anchor(nodeGlow.rectTransform, new Vector2(-0.08f, -0.08f), new Vector2(1.08f, 1.08f));
                Color glowTint = UiTheme.GoldSoft;
                glowTint.a = 0f;
                nodeGlow.color = glowTint;

                GameObject artGO = new GameObject("Art", typeof(RectTransform), typeof(Image));
                RectTransform artRect = artGO.GetComponent<RectTransform>();
                artRect.SetParent(nodeGO.transform, false);
                artRect.anchorMin = new Vector2(0.06f, 0.06f);
                artRect.anchorMax = new Vector2(0.94f, 0.94f);
                artRect.offsetMin = Vector2.zero;
                artRect.offsetMax = Vector2.zero;

                art = artGO.GetComponent<Image>();
                art.sprite = nodeArt;
                art.preserveAspect = true;
                art.raycastTarget = true;    // the medallion is the click target now

                icon.enabled = false;
                diaGO.SetActive(false);
            }

            GameObject badgeGO = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            RectTransform br = badgeGO.GetComponent<RectTransform>();
            br.SetParent(nodeGO.transform, false);
            br.anchorMin = new Vector2(0.5f, 0.5f);
            br.anchorMax = new Vector2(0.5f, 0.5f);
            br.pivot = new Vector2(0.5f, 0.5f);
            br.sizeDelta = new Vector2(40f, 40f);
            br.anchoredPosition = new Vector2(40f, -40f);
            Image badge = badgeGO.GetComponent<Image>();
            badge.sprite = CircleSprite();
            badge.color = UiTheme.GreenBadge;

            GameObject arrowGO = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            RectTransform ar = arrowGO.GetComponent<RectTransform>();
            ar.SetParent(badgeGO.transform, false);
            ar.anchorMin = new Vector2(0.5f, 0.5f);
            ar.anchorMax = new Vector2(0.5f, 0.5f);
            ar.sizeDelta = new Vector2(22f, 22f);

            Image arrow = arrowGO.GetComponent<Image>();
            arrow.sprite = ArrowSprite(true);
            arrow.color = UiTheme.GreenDeep;
            arrow.raycastTarget = false;

            badgeGO.SetActive(false);

            Button button = nodeGO.AddComponent<Button>();
            button.targetGraphic = art != null ? art : diamond;
            button.transition = Selectable.Transition.None;
            TrainingNode captured = node;
            int capturedColumn = column;
            button.onClick.AddListener(() => OnNodeTap(captured, capturedColumn));

            // The node keeps its diamond art but gains the shared press squash and disabled grayscale.
            Coffee.UIEffects.UIEffect nodeEffect = nodeGO.AddComponent<Coffee.UIEffects.UIEffect>();
            UiPlateButton nodePlate = nodeGO.AddComponent<UiPlateButton>();
            nodePlate.Bind(nodeGO.GetComponent<RectTransform>(), art != null ? art : diamond, null, nodeEffect, button, Color.white, Color.white);

            treeViews.Add(new TreeView
            {
                Node = node,
                Diamond = diamond,
                Plate = nodePlate,
                Connector = connector,
                Badge = badge,
                Glow = nodeGlow,
                Rect = nr,
                Column = column
            });
        }

        // ------------------------------------------------------------------
        // Colours, bands, frontier rows
        // ------------------------------------------------------------------
        private void RefreshTree()
        {
            EnsureManager();

            if (manager == null || manager.Training == null)
            {
                return;
            }

            TrainingService training = manager.Training;
            TrainingNode[] nextByColumn = new TrainingNode[treeColumns.Count];

            for (int i = 0; i < treeViews.Count; i++)
            {
                TreeView view = treeViews[i];

                if (training.CanUnlock(view.Node))
                {
                    int c = Mathf.Clamp(view.Column, 0, treeColumns.Count - 1);

                    if (nextByColumn[c] == null)
                    {
                        nextByColumn[c] = view.Node;
                    }
                }
            }

            for (int i = 0; i < treeViews.Count; i++)
            {
                TreeView view = treeViews[i];
                bool unlocked = training.IsUnlocked(view.Node);
                int c = Mathf.Clamp(view.Column, 0, treeColumns.Count - 1);
                bool isNext = !unlocked && nextByColumn[c] == view.Node;

                Color color;

                if (unlocked)
                {
                    color = UiTheme.GoldDeep;
                }
                else if (view.Node.IsDeferred)
                {
                    color = UiTheme.InkDark;
                }
                else if (isNext)
                {
                    color = UiTheme.InkGreyMid;
                }
                else
                {
                    color = UiTheme.LineGrey;
                }

                if (view.Diamond != null)
                {
                    view.Diamond.color = color;
                }

                // Full-colour art while owned or next; dimmed when locked or deferred (there is no frame now).
                Color iconTint = unlocked || isNext
                    ? Color.white
                    : (view.Node.IsDeferred ? UiTheme.LineSoft : UiTheme.InkGreyMid);

                if (view.Plate != null)
                {
                    view.Plate.SetFaces(iconTint, iconTint);
                }

                if (view.Badge != null)
                {
                    view.Badge.gameObject.SetActive(isNext);
                }

                if (view.Glow != null)
                {
                    // A soft gold halo only when this node is the next one AND the player can pay for it now.
                    bool canBuy = isNext && training.CanAffordNode(view.Node);
                    Color glowColor = view.Glow.color;
                    glowColor.a = canBuy ? 0.45f : 0f;
                    view.Glow.color = glowColor;
                }

                if (view.Connector != null)
                {
                    view.Connector.color = unlocked
                        ? UiTheme.GoldDim
                        : UiTheme.LineGrey;
                }
            }

            RefreshColumns(training);

            if (treePopup != null && treePopup.activeSelf)
            {
                UpdatePopup();
            }
        }

        private void RefreshColumns(TrainingService training)
        {
            // Frontier row per column (money does not matter here - only the gate); used by the portrait jump.
            int[] frontierOrders = new int[treeColumns.Count];

            for (int i = 0; i < frontierOrders.Length; i++)
            {
                frontierOrders[i] = -1;
            }

            for (int i = 0; i < treeViews.Count; i++)
            {
                TreeView view = treeViews[i];

                if (training.IsUnlocked(view.Node) || view.Node.IsDeferred)
                {
                    continue;
                }

                int c = Mathf.Clamp(view.Column, 0, treeColumns.Count - 1);
                int order = OrderOf(view.Node);

                if (frontierOrders[c] < 0 || order < frontierOrders[c])
                {
                    frontierOrders[c] = order;
                }
            }

            for (int i = 0; i < treeColumns.Count; i++)
            {
                TreeColumn column = treeColumns[i];
                column.FrontierOrder = frontierOrders[i] >= 0 ? frontierOrders[i] : 0;
                treeColumns[i] = column;
            }

            // The fill rises one ROW at a time: as far as the weakest hero has climbed, but never past a party
            // node that is still unbought (that node is the party track's hand on the bar).
            int lowest = training.LowestLeftCount;
            int gateLimit = treeHeroRows;

            if (manager.Training != null && manager.Training.Tree != null)
            {
                IReadOnlyList<TrainingNode> nodes = manager.Training.Tree.Nodes;

                for (int i = 0; i < nodes.Count; i++)
                {
                    TrainingNode node = nodes[i];

                    if (node == null || node.Branch != TrainingBranch.Right || !node.IsPartyNode || node.IsDeferred)
                    {
                        continue;
                    }

                    if (!training.IsUnlocked(node))
                    {
                        gateLimit = Mathf.Clamp(node.RequiresLeftNodes, 0, treeHeroRows);
                        break;
                    }
                }
            }

            int fillRows = Mathf.Clamp(Mathf.Min(lowest, gateLimit), 0, treeHeroRows);
            float fillHeight = fillRows <= 0
                ? 0f
                : treeTopPad + fillRows * treeRowH + (fillRows / NodesPerLevel) * LevelGap;

            if (treeFill != null)
            {
                treeFill.rectTransform.sizeDelta = new Vector2(0f, fillHeight);
            }
        }

        private void ScrollToColumn(int column)
        {
            if (treeScroll == null || treeContent == null || column < 0 || column >= treeColumns.Count)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            float viewH = treeScroll.viewport != null ? treeScroll.viewport.rect.height : 0f;
            float limit = Mathf.Max(0f, treeContentHeight - viewH);
            float center = NodeYFromTop(treeColumns[column].FrontierOrder);

            treeContent.anchoredPosition = new Vector2(0f, Mathf.Clamp(center - viewH * 0.5f, 0f, limit));
            treeScroll.StopMovement();
            RefreshScrollButton();
        }

        // ------------------------------------------------------------------
        // Node card beside the node (not a modal)
        // ------------------------------------------------------------------
        private void BuildPopup(Transform parent)
        {
            GameObject card = new GameObject("TreePopup", typeof(RectTransform), typeof(Image));
            RectTransform cr = card.GetComponent<RectTransform>();
            cr.SetParent(parent, false);
            cr.anchorMin = new Vector2(0f, 1f);
            cr.anchorMax = new Vector2(0f, 1f);
            cr.pivot = new Vector2(0f, 0.5f);
            cr.sizeDelta = new Vector2(560f, 300f);
            card.GetComponent<Image>().color = UiTheme.Parchment;
            card.GetComponent<Image>().sprite = UiRuntime.PlateSprite;
            card.GetComponent<Image>().type = Image.Type.Sliced;
            treePopup = card;

            popupTitle = Text(card.transform, "Title", new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.97f),
                string.Empty, UiType.Role.Section, UiTheme.InkDark);
            popupBody = Text(card.transform, "Body", new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.75f),
                string.Empty, UiType.Role.Sentence, UiTheme.InkSoftDark);
            popupBuy = Btn(card.transform, "Buy", new Vector2(0.12f, 0.04f), new Vector2(0.88f, 0.52f), "Buy",
                UiTheme.Orange, UiType.Role.Section, BuyPopup);

            popupCoin = MakeCardIcon(popupBuy.transform, CircleSprite(), UiTheme.GoldDeep);
            popupStar = MakeCardIcon(popupBuy.transform, StarSprite(), UiTheme.Purple);
            popupStar.gameObject.SetActive(false);

            treePopup.SetActive(false);
        }

        private static Image MakeCardIcon(Transform parent, Sprite sprite, Color color)
        {
            GameObject go = new GameObject("CostIcon", typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(40f, 40f);
            rt.anchoredPosition = new Vector2(-22f, 0f);

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private void OnNodeTap(TrainingNode node, int column)
        {
            if (treePopup == null || manager == null || manager.Training == null)
            {
                return;
            }

            if (popupNode == node && treePopup.activeSelf)
            {
                ClosePopup();
                return;
            }

            popupNode = node;
            popupColumn = column;

            RectTransform card = treePopup.GetComponent<RectTransform>();
            RectTransform nodeRect = null;

            for (int i = 0; i < treeViews.Count; i++)
            {
                if (treeViews[i].Node == node)
                {
                    nodeRect = treeViews[i].Rect;
                    break;
                }
            }

            if (nodeRect != null)
            {
                bool toRight = column <= 1;   // the two left columns open to the right, the two right ones to the left
                Vector2 p = nodeRect.anchoredPosition;
                float gap = NodeSize * 0.5f + 18f;
                card.pivot = toRight ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);
                card.anchoredPosition = new Vector2(p.x + (toRight ? gap : -gap), p.y);
            }

            treePopup.transform.SetAsLastSibling();
            treePopup.SetActive(true);
            UpdatePopup();
        }

        private void UpdatePopup()
        {
            if (popupNode == null || manager == null || manager.Training == null || treePopup == null)
            {
                return;
            }

            TrainingService training = manager.Training;
            bool owned = training.IsUnlocked(popupNode);
            bool deferred = popupNode.IsDeferred;
            bool gateMet = training.IsGateMet(popupNode);
            bool affordable = training.CanAffordNode(popupNode);
            bool showButton = !owned && !deferred;

            if (popupTitle != null)
            {
                popupTitle.SetText(OwnerLabel(popupColumn) + " \u00b7 " + FriendlyName(popupNode.Effect));
            }

            string body;

            if (owned)
            {
                body = EffectLine(popupNode);
            }
            else if (deferred)
            {
                body = "Coming soon.";
            }
            else if (!gateMet)
            {
                body = popupNode.Branch == TrainingBranch.Right
                    ? GateMessage(popupNode)
                    : LinearGateMessage(popupNode);
            }
            else if (!affordable)
            {
                body = ShortfallMessage(popupNode);
            }
            else
            {
                body = EffectLine(popupNode);
            }

            if (popupBody != null)
            {
                popupBody.SetText(body);
            }

            if (popupBuy != null)
            {
                popupBuy.gameObject.SetActive(showButton);
                popupBuy.SetInteractable(showButton && gateMet && affordable);

                TextMeshProUGUI label = popupBuy.GetComponentInChildren<TextMeshProUGUI>(true);

                if (label != null)
                {
                    label.SetText("Buy " + NumberFormatter.Format(popupNode.Cost));
                }
            }

            bool goldCost = popupNode.CostCurrency == CurrencyType.Gold;

            if (popupCoin != null)
            {
                popupCoin.gameObject.SetActive(showButton && goldCost);
            }

            if (popupStar != null)
            {
                popupStar.gameObject.SetActive(showButton && !goldCost);
            }

            // Short card when there is nothing to buy - no dead space where the button used to be.
            if (treePopup != null)
            {
                treePopup.GetComponent<RectTransform>().sizeDelta = new Vector2(560f, showButton ? 300f : 180f);

                if (popupTitle != null)
                {
                    popupTitle.rectTransform.anchorMin = new Vector2(0.05f, showButton ? 0.76f : 0.58f);
                    popupTitle.rectTransform.anchorMax = new Vector2(0.95f, showButton ? 0.97f : 0.94f);
                }

                if (popupBody != null)
                {
                    popupBody.rectTransform.anchorMin = new Vector2(0.05f, showButton ? 0.52f : 0.10f);
                    popupBody.rectTransform.anchorMax = new Vector2(0.95f, showButton ? 0.75f : 0.54f);
                }
            }
        }

        private void BuyPopup()
        {
            if (popupNode == null || manager == null || manager.Training == null)
            {
                return;
            }

            if (manager.Training.TryUnlock(popupNode))
            {
                RefreshTree();
                ClosePopup();
            }
            else
            {
                UpdatePopup();
            }
        }

        private void ClosePopup()
        {
            if (treePopup != null && treePopup.activeSelf)
            {
                treePopup.SetActive(false);
            }
        }

        // ------------------------------------------------------------------
        // Wording
        // ------------------------------------------------------------------
        private string OwnerLabel(int column)
        {
            HeroData hero = HeroForColumn(column);
            return hero != null ? hero.HeroName : "Party";
        }

        private static string IconLetter(TrainingEffect effect)
        {
            switch (effect)
            {
                case TrainingEffect.AttackFlat: return "A";
                case TrainingEffect.ArmorFlat: return "D";
                case TrainingEffect.HpFlat: return "H";
                case TrainingEffect.CampHealPercent: return "C";
                case TrainingEffect.UnlockFirstAbility: return "F";
                case TrainingEffect.BattleGoldPercent: return "$";
                case TrainingEffect.FinishGoldPercent: return "$";
                default: return "?";
            }
        }

        private static string FriendlyName(TrainingEffect effect)
        {
            switch (effect)
            {
                case TrainingEffect.AttackFlat: return "Attack";
                case TrainingEffect.ArmorFlat: return "Armor";
                case TrainingEffect.HpFlat: return "Health";
                case TrainingEffect.CampHealPercent: return "Camp healing";
                case TrainingEffect.BattleGoldPercent: return "Battle gold";
                case TrainingEffect.FinishGoldPercent: return "Finish gold";
                case TrainingEffect.UnlockFirstAbility: return "Active skill";
                case TrainingEffect.CampExperience: return "Camp XP";
                case TrainingEffect.LevelUpHealPercent: return "Level-up heal";
                case TrainingEffect.SurviveDeathShield: return "Survive once";
                default: return "Training";
            }
        }

        private static string EffectLine(TrainingNode node)
        {
            string amount = node.Amount.ToString("0.##");

            switch (node.Effect)
            {
                case TrainingEffect.AttackFlat: return "Attack +" + amount;
                case TrainingEffect.ArmorFlat: return "Armor +" + amount;
                case TrainingEffect.HpFlat: return "Max health +" + amount;
                case TrainingEffect.CampHealPercent: return "Camp healing +" + amount;
                case TrainingEffect.BattleGoldPercent: return "Battle gold +" + (node.Amount * 100d).ToString("0.##") + "%";
                case TrainingEffect.FinishGoldPercent: return "Finish gold +" + (node.Amount * 100d).ToString("0.##") + "%";
                case TrainingEffect.UnlockFirstAbility: return "Unlocks the first ability";
                default: return node.Description;
            }
        }

        /// <summary>Why the party node is shut, named after the hero who is holding it back.</summary>
        private string GateMessage(TrainingNode node)
        {
            int required = node.RequiresLeftNodes;

            if (manager.Training.TryGetLaggard(out string laggardId, out int lowest))
            {
                return "All three heroes need " + required + " training nodes - " + HeroName(laggardId) + " has " + lowest + ".";
            }

            return "All three heroes need " + required + " training nodes.";
        }

        /// <summary>A hero chain is linear: name the node directly below this one.</summary>
        private string LinearGateMessage(TrainingNode node)
        {
            IReadOnlyList<TrainingNode> nodes = manager.Training.Tree.Nodes;

            for (int i = 0; i < nodes.Count; i++)
            {
                TrainingNode other = nodes[i];

                if (other != null && other.Branch == TrainingBranch.Left &&
                    other.HeroID == node.HeroID && other.Order == node.Order - 1)
                {
                    return "Unlock " + other.Title + " first.";
                }
            }

            return "Unlock the node below first.";
        }

        /// <summary>Why the Buy is dead: what the node costs against what the purse holds.</summary>
        private string ShortfallMessage(TrainingNode node)
        {
            bool gold = node.CostCurrency == CurrencyType.Gold;
            double held = gold ? manager.Economy.Gold : manager.Economy.PrestigeTokens;
            string unit = gold ? "gold" : "tokens";

            return "Need " + NumberFormatter.Format(node.Cost) + " " + unit + " - you have " + NumberFormatter.Format(held) + ".";
        }

        // ------------------------------------------------------------------
        // Cost icons (drawn in code - no runtime sprite loading)
        // ------------------------------------------------------------------
        private static Sprite coinSprite;
        private static Sprite starSprite;
        private static Sprite arrowUpSprite;
        private static Sprite arrowDownSprite;

        private static Sprite CircleSprite()
        {
            if (coinSprite != null)
            {
                return coinSprite;
            }

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.5f;
            Color clear = Color.clear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    texture.SetPixel(x, y, distance <= radius - 0.5f ? Color.white : clear);
                }
            }

            texture.Apply();
            coinSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
            return coinSprite;
        }

        private static Sprite StarSprite()
        {
            if (starSprite != null)
            {
                return starSprite;
            }

            const int size = 64;
            float centre = size * 0.5f;
            Vector2[] points = new Vector2[10];

            for (int i = 0; i < points.Length; i++)
            {
                float angle = -Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                float radius = i % 2 == 0 ? size * 0.48f : size * 0.20f;
                points[i] = new Vector2(centre + Mathf.Cos(angle) * radius, centre + Mathf.Sin(angle) * radius);
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = Color.clear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = false;

                    for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                    {
                        if (points[i].y > y != points[j].y > y &&
                            x < (points[j].x - points[i].x) * (y - points[i].y) / (points[j].y - points[i].y) + points[i].x)
                        {
                            inside = !inside;
                        }
                    }

                    texture.SetPixel(x, y, inside ? Color.white : clear);
                }
            }

            texture.Apply();
            starSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
            return starSprite;
        }
        /// <summary>A drawn triangle (the font has no arrow glyph, so a text arrow renders as a box).</summary>
        private static Sprite ArrowSprite(bool up)
        {
            if (up && arrowUpSprite != null)
            {
                return arrowUpSprite;
            }

            if (!up && arrowDownSprite != null)
            {
                return arrowDownSprite;
            }

            const int size = 64;
            float baseY = up ? size * 0.20f : size * 0.80f;
            float apexY = up ? size * 0.84f : size * 0.16f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = Color.clear;

            for (int y = 0; y < size; y++)
            {
                float t = Mathf.InverseLerp(baseY, apexY, y);
                float half = Mathf.Lerp(size * 0.34f, 0f, t);

                for (int x = 0; x < size; x++)
                {
                    bool inside = t >= 0f && t <= 1f && half >= 0.75f && Mathf.Abs(x + 0.5f - size * 0.5f) <= half;
                    texture.SetPixel(x, y, inside ? Color.white : clear);
                }
            }

            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));

            if (up)
            {
                arrowUpSprite = sprite;
            }
            else
            {
                arrowDownSprite = sprite;
            }

            return sprite;
        }

        /// <summary>A square icon button (no label): used for the jump-to-bottom arrow.</summary>
        private static GameObject IconBtn(Transform parent, string name, Vector2 aMin, Vector2 aMax, Sprite icon,
            UnityEngine.Events.UnityAction onClick)
        {
            Image bg = Panel(parent, name, aMin, aMax, UiTheme.RowLight, UiRuntime.PlateSprite);
            Button button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(onClick);

            Coffee.UIEffects.UIEffect btnEffect = bg.gameObject.AddComponent<Coffee.UIEffects.UIEffect>();
            UiPlateButton btnPlate = bg.gameObject.AddComponent<UiPlateButton>();
            btnPlate.Bind(bg.rectTransform, bg, null, btnEffect, button, UiTheme.RowLight, UiTheme.RowLight);

            GameObject iconGO = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            RectTransform rt = iconGO.GetComponent<RectTransform>();
            rt.SetParent(bg.transform, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(52f, 52f);

            Image image = iconGO.GetComponent<Image>();
            image.sprite = icon;
            image.color = UiTheme.Text;
            image.raycastTarget = false;

            return bg.gameObject;
        }

        /// <summary>Thin gold bar with blue text, centred in the gap between two levels and drawn in front.</summary>
        private void CreateLevelBar(Transform parent, float centerY, int levelNumber)
        {
            GameObject barGO = new GameObject("LevelBar", typeof(RectTransform), typeof(Image));
            RectTransform br = barGO.GetComponent<RectTransform>();
            br.SetParent(parent, false);
            br.anchorMin = new Vector2(0f, 1f);
            br.anchorMax = new Vector2(1f, 1f);
            br.pivot = new Vector2(0.5f, 0.5f);
            br.sizeDelta = new Vector2(-24f, 56f);
            br.anchoredPosition = new Vector2(0f, -centerY);

            Image bar = barGO.GetComponent<Image>();
            bar.color = UiTheme.Gold;
            bar.raycastTarget = false;

            TextMeshProUGUI label = Text(barGO.transform, "Label", Vector2.zero, Vector2.one,
                "LEVEL " + levelNumber, UiType.Role.Small, UiTheme.PanelDeep);
            label.raycastTarget = false;
        }

        /// <summary>The jump-to-bottom button appears only after the player scrolls away from the end.</summary>
        private void RefreshScrollButton()
        {
            if (scrollDownButton == null || treeContent == null)
            {
                return;
            }

            float viewH = treeScroll != null && treeScroll.viewport != null ? treeScroll.viewport.rect.height : 0f;
            float limit = Mathf.Max(0f, treeContentHeight - viewH);
            float remaining = limit - treeContent.anchoredPosition.y;

            scrollDownButton.SetActive(remaining > ScrollButtonThreshold);
        }

    }
}
