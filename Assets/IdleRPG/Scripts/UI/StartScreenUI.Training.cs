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
        private int treeHeroRows;
        private int treeNodeLayer;

        private struct LevelBand
        {
            public Image Background;
            public int LevelIndex;
        }

        private readonly List<LevelBand> levelBands = new List<LevelBand>();
        private readonly List<List<TrainingNode>> levelPartyGate = new List<List<TrainingNode>>();
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

            if (statusLabel != null)
            {
                statusLabel.SetText(string.Empty);
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
            panel.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.13f, 0.98f);
            trainingPanel = panel;

            BuildColumnHeaders(panel.transform);

            GameObject scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            RectTransform sr = scrollGO.GetComponent<RectTransform>();
            sr.SetParent(panel.transform, false);
            sr.anchorMin = new Vector2(0f, 0f);
            sr.anchorMax = new Vector2(1f, TreeHeaderFraction);
            sr.offsetMin = Vector2.zero;
            sr.offsetMax = Vector2.zero;
            scrollGO.GetComponent<Image>().color = new Color(0.13f, 0.14f, 0.16f, 1f);
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
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
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
            treeContentHeight = treeTopPad * 2f + treeHeroRows * treeRowH;

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
                    FrontierRow = treeHeroRows - 1
                });
            }

            treeColumns.Add(new TreeColumn { HeroId = string.Empty, FrontierRow = treeHeroRows - 1 });   // party

            // One page-wide strip per level (6 nodes each). The strips stack without gaps, so together they are
            // the single background fill. A strip turns gold only when every hero finished that level AND the
            // party node gated inside it has been bought.
            levelBands.Clear();
            levelPartyGate.Clear();

            const int nodesPerLevel = 6;
            int levelCount = Mathf.Max(1, Mathf.CeilToInt(treeHeroRows / (float)nodesPerLevel));
            float levelHeight = nodesPerLevel * treeRowH;

            for (int level = 0; level < levelCount; level++)
            {
                levelPartyGate.Add(new List<TrainingNode>());

                GameObject bandGO = new GameObject("LevelBand", typeof(RectTransform), typeof(Image));
                RectTransform bandRect = bandGO.GetComponent<RectTransform>();
                bandRect.SetParent(treeContent, false);
                bandRect.anchorMin = new Vector2(0f, 0f);
                bandRect.anchorMax = new Vector2(1f, 0f);
                bandRect.pivot = new Vector2(0.5f, 0f);
                bandRect.offsetMin = Vector2.zero;
                bandRect.offsetMax = Vector2.zero;
                bandRect.sizeDelta = new Vector2(0f, levelHeight);
                bandRect.anchoredPosition = new Vector2(0f, level * levelHeight);

                Image band = bandGO.GetComponent<Image>();
                band.color = new Color(0.13f, 0.14f, 0.16f, 1f);
                band.raycastTarget = false;

                TextMeshProUGUI levelLabel = Text(bandGO.transform, "Level", new Vector2(0f, 0f), new Vector2(0f, 0f),
                    "Level " + (level + 1), UiType.Role.Micro, new Color(0.98f, 0.80f, 0.28f, 0.35f));
                levelLabel.rectTransform.sizeDelta = new Vector2(240f, 34f);
                levelLabel.rectTransform.anchoredPosition = new Vector2(130f, 26f);
                levelLabel.alignment = TextAlignmentOptions.Left;
                levelLabel.raycastTarget = false;

                levelBands.Add(new LevelBand { Background = band, LevelIndex = level });
            }

            if (tree != null)
            {
                IReadOnlyList<TrainingNode> allNodes = tree.Nodes;

                for (int i = 0; i < allNodes.Count; i++)
                {
                    TrainingNode node = allNodes[i];

                    if (node == null || node.Branch != TrainingBranch.Right || !node.IsPartyNode || node.IsDeferred)
                    {
                        continue;
                    }

                    int level = Mathf.Clamp(node.RequiresLeftNodes / nodesPerLevel, 0, levelCount - 1);
                    levelPartyGate[level].Add(node);
                }
            }

            treeNodeLayer = treeContent.childCount;   // bands + level badges sit behind everything added below

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
            catcherImage.color = new Color(0f, 0f, 0f, 0f);
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

                        int rowFromTop = treeHeroRows - 1 - node.Order;
                        MakeNode(node, ColumnX[c], rowFromTop, c, rowFromTop > 0 ? 1 : 0);
                    }
                }

                int partyColumn = treeColumns.Count - 1;

                for (int i = 0; i < partyNodes.Count; i++)
                {
                    int gate = Mathf.Clamp(partyNodes[i].RequiresLeftNodes, 0, Mathf.Max(0, treeHeroRows - 1));
                    int nextGate = i + 1 < partyNodes.Count
                        ? Mathf.Clamp(partyNodes[i + 1].RequiresLeftNodes, 0, Mathf.Max(0, treeHeroRows - 1))
                        : gate;

                    MakeNode(partyNodes[i], ColumnX[partyColumn], treeHeroRows - 1 - gate, partyColumn, nextGate - gate);
                }
            }

            IconBtn(panel.transform, "ScrollDown", new Vector2(0.845f, 0.005f), new Vector2(0.985f, 0.105f),
                ArrowSprite(false), ScrollTreeToBottom);

            BuildPopup(treeContent);
            trainingPanel.SetActive(false);
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
            strip.GetComponent<Image>().color = new Color(0.13f, 0.14f, 0.17f, 1f);

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
            bg.color = new Color(0.17f, 0.18f, 0.22f, 1f);

            Button button = cell.GetComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
            int captured = column;
            button.onClick.AddListener(() => ScrollToColumn(captured));

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
                portrait.color = hero.HeroIcon != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);

                TextMeshProUGUI nameLabel = Text(cell.transform, "Name", new Vector2(0.40f, 0.20f),
                    new Vector2(0.99f, 0.80f), hero.HeroName, UiType.Role.Small, Color.white);
                nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
                UiType.ApplyAuto(nameLabel, UiType.Role.Small);
            }
            else
            {
                Text(cell.transform, "Name", new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.82f),
                    "Empty", UiType.Role.Small, new Color(0.6f, 0.63f, 0.72f, 1f));
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
            bg.color = new Color(0.20f, 0.17f, 0.12f, 1f);

            Button button = cell.GetComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => ScrollToColumn(column));

            Text(cell.transform, "Title", new Vector2(0.04f, 0.76f), new Vector2(0.96f, 0.99f),
                "PARTY", UiType.Role.Small, new Color(0.98f, 0.80f, 0.28f, 1f));

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
                mini.color = hero.HeroIcon != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);
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
        private void MakeNode(TrainingNode node, float xPx, int rowFromTop, int column, int connectorRows)
        {
            float centerY = treeTopPad + rowFromTop * treeRowH + treeRowH * 0.5f;

            Image connector = null;

            if (connectorRows > 0)
            {
                GameObject conGO = new GameObject("Con", typeof(RectTransform), typeof(Image));
                RectTransform cr = conGO.GetComponent<RectTransform>();
                cr.SetParent(treeContent, false);
                cr.anchorMin = new Vector2(0f, 1f);
                cr.anchorMax = new Vector2(0f, 1f);
                cr.pivot = new Vector2(0.5f, 0.5f);
                cr.sizeDelta = new Vector2(12f, connectorRows * treeRowH);
                cr.anchoredPosition = new Vector2(xPx, -(centerY - connectorRows * treeRowH * 0.5f));
                cr.SetSiblingIndex(treeNodeLayer);   // behind the nodes, above the bands and level badges
                connector = conGO.GetComponent<Image>();
                connector.color = new Color(0.30f, 0.31f, 0.35f, 1f);
            }

            GameObject nodeGO = new GameObject("Node_" + node.NodeID, typeof(RectTransform));
            RectTransform nr = nodeGO.GetComponent<RectTransform>();
            nr.SetParent(treeContent, false);
            nr.anchorMin = new Vector2(0f, 1f);
            nr.anchorMax = new Vector2(0f, 1f);
            nr.pivot = new Vector2(0.5f, 0.5f);
            nr.sizeDelta = new Vector2(NodeSize, NodeSize);
            nr.anchoredPosition = new Vector2(xPx, -centerY);

            GameObject ringGO = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            RectTransform rr = ringGO.GetComponent<RectTransform>();
            rr.SetParent(nodeGO.transform, false);
            rr.anchorMin = Vector2.zero;
            rr.anchorMax = Vector2.one;
            rr.offsetMin = new Vector2(5f, 5f);
            rr.offsetMax = new Vector2(-5f, -5f);
            rr.localRotation = UnityEngine.Quaternion.Euler(0f, 0f, 45f);
            Image ring = ringGO.GetComponent<Image>();
            ring.color = new Color(1f, 0.95f, 0.66f, 1f);   // pale gold rim, brighter than the fill
            ring.raycastTarget = false;
            ringGO.SetActive(false);

            GameObject diaGO = new GameObject("Diamond", typeof(RectTransform), typeof(Image));
            RectTransform dr = diaGO.GetComponent<RectTransform>();
            dr.SetParent(nodeGO.transform, false);
            dr.anchorMin = Vector2.zero;
            dr.anchorMax = Vector2.one;
            dr.offsetMin = new Vector2(16f, 16f);
            dr.offsetMax = new Vector2(-16f, -16f);
            dr.localRotation = UnityEngine.Quaternion.Euler(0f, 0f, 45f);
            Image diamond = diaGO.GetComponent<Image>();
            diamond.color = new Color(0.35f, 0.36f, 0.4f, 1f);

            TextMeshProUGUI icon = Text(nodeGO.transform, "Icon", Vector2.zero, Vector2.one, IconLetter(node.Effect), UiType.Role.Section, Color.white);
            icon.raycastTarget = false;

            GameObject badgeGO = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            RectTransform br = badgeGO.GetComponent<RectTransform>();
            br.SetParent(nodeGO.transform, false);
            br.anchorMin = new Vector2(0.5f, 0.5f);
            br.anchorMax = new Vector2(0.5f, 0.5f);
            br.pivot = new Vector2(0.5f, 0.5f);
            br.sizeDelta = new Vector2(40f, 40f);
            br.anchoredPosition = new Vector2(54f, -54f);
            Image badge = badgeGO.GetComponent<Image>();
            badge.sprite = CircleSprite();
            badge.color = new Color(0.3f, 0.9f, 0.4f, 1f);

            GameObject arrowGO = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            RectTransform ar = arrowGO.GetComponent<RectTransform>();
            ar.SetParent(badgeGO.transform, false);
            ar.anchorMin = new Vector2(0.5f, 0.5f);
            ar.anchorMax = new Vector2(0.5f, 0.5f);
            ar.sizeDelta = new Vector2(22f, 22f);

            Image arrow = arrowGO.GetComponent<Image>();
            arrow.sprite = ArrowSprite(true);
            arrow.color = new Color(0.05f, 0.2f, 0.05f, 1f);
            arrow.raycastTarget = false;

            badgeGO.SetActive(false);

            Button button = nodeGO.AddComponent<Button>();
            button.targetGraphic = diamond;
            button.transition = Selectable.Transition.None;
            TrainingNode captured = node;
            int capturedColumn = column;
            button.onClick.AddListener(() => OnNodeTap(captured, capturedColumn));

            treeViews.Add(new TreeView
            {
                Node = node,
                Diamond = diamond,
                Connector = connector,
                Badge = badge,
                Ring = ring,
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
                    color = new Color(0.98f, 0.80f, 0.22f, 1f);
                }
                else if (view.Node.IsDeferred)
                {
                    color = new Color(0.12f, 0.12f, 0.14f, 1f);
                }
                else if (isNext)
                {
                    color = new Color(0.45f, 0.46f, 0.5f, 1f);
                }
                else
                {
                    color = new Color(0.30f, 0.31f, 0.35f, 1f);
                }

                if (view.Diamond != null)
                {
                    view.Diamond.color = color;
                }

                if (view.Badge != null)
                {
                    view.Badge.gameObject.SetActive(isNext);
                }

                if (view.Ring != null)
                {
                    view.Ring.gameObject.SetActive(unlocked);
                }

                if (view.Connector != null)
                {
                    view.Connector.color = unlocked
                        ? new Color(0.9f, 0.72f, 0.15f, 1f)
                        : new Color(0.30f, 0.31f, 0.35f, 1f);
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
            int[] frontierRows = new int[treeColumns.Count];

            for (int i = 0; i < frontierRows.Length; i++)
            {
                frontierRows[i] = -1;
            }

            for (int i = 0; i < treeViews.Count; i++)
            {
                TreeView view = treeViews[i];

                if (training.IsUnlocked(view.Node) || view.Node.IsDeferred)
                {
                    continue;
                }

                int c = Mathf.Clamp(view.Column, 0, treeColumns.Count - 1);
                int row = RowOfNode(view.Node);

                if (row > frontierRows[c])
                {
                    frontierRows[c] = row;
                }
            }

            for (int i = 0; i < treeColumns.Count; i++)
            {
                TreeColumn column = treeColumns[i];
                column.FrontierRow = frontierRows[i] >= 0 ? frontierRows[i] : treeHeroRows - 1;
                treeColumns[i] = column;
            }

            // One fill for the whole page: a level strip is gold when every hero finished the level AND the
            // party node gated inside that level has been bought.
            const int nodesPerLevel = 6;
            int lowest = training.LowestLeftCount;
            Color gold = new Color(0.98f, 0.78f, 0.2f, 0.10f);
            Color plain = new Color(0.13f, 0.14f, 0.16f, 1f);

            for (int i = 0; i < levelBands.Count; i++)
            {
                LevelBand band = levelBands[i];
                bool heroesDone = lowest >= (band.LevelIndex + 1) * nodesPerLevel;
                bool partyDone = true;

                if (band.LevelIndex < levelPartyGate.Count)
                {
                    List<TrainingNode> gates = levelPartyGate[band.LevelIndex];

                    for (int g = 0; g < gates.Count; g++)
                    {
                        if (!training.IsUnlocked(gates[g]))
                        {
                            partyDone = false;
                            break;
                        }
                    }
                }

                if (band.Background != null)
                {
                    band.Background.color = heroesDone && partyDone ? gold : plain;
                }
            }
        }

        private int RowOfNode(TrainingNode node)
        {
            if (node.Branch == TrainingBranch.Right)
            {
                return treeHeroRows - 1 - Mathf.Clamp(node.RequiresLeftNodes, 0, Mathf.Max(0, treeHeroRows - 1));
            }

            return treeHeroRows - 1 - node.Order;
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
            float center = treeTopPad + treeColumns[column].FrontierRow * treeRowH + treeRowH * 0.5f;

            treeContent.anchoredPosition = new Vector2(0f, Mathf.Clamp(center - viewH * 0.5f, 0f, limit));
            treeScroll.StopMovement();
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
            card.GetComponent<Image>().color = new Color(0.96f, 0.92f, 0.80f, 1f);
            treePopup = card;

            popupTitle = Text(card.transform, "Title", new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.97f),
                string.Empty, UiType.Role.Section, new Color(0.12f, 0.12f, 0.14f, 1f));
            popupBody = Text(card.transform, "Body", new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.75f),
                string.Empty, UiType.Role.Sentence, new Color(0.28f, 0.28f, 0.31f, 1f));
            popupBuy = Btn(card.transform, "Buy", new Vector2(0.12f, 0.04f), new Vector2(0.88f, 0.52f), "Buy",
                new Color(0.95f, 0.55f, 0.15f, 1f), UiType.Role.Section, BuyPopup);

            popupCoin = MakeCardIcon(popupBuy.transform, CircleSprite(), new Color(0.98f, 0.80f, 0.20f, 1f));
            popupStar = MakeCardIcon(popupBuy.transform, StarSprite(), new Color(0.80f, 0.55f, 0.98f, 1f));
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
                popupBuy.interactable = showButton && gateMet && affordable;

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
            Color clear = new Color(1f, 1f, 1f, 0f);

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
            Color clear = new Color(1f, 1f, 1f, 0f);

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
            Color clear = new Color(1f, 1f, 1f, 0f);

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
        private static void IconBtn(Transform parent, string name, Vector2 aMin, Vector2 aMax, Sprite icon,
            UnityEngine.Events.UnityAction onClick)
        {
            Image bg = Panel(parent, name, aMin, aMax, new Color(0.18f, 0.19f, 0.22f, 1f));
            Button button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(onClick);

            GameObject iconGO = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            RectTransform rt = iconGO.GetComponent<RectTransform>();
            rt.SetParent(bg.transform, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(52f, 52f);

            Image image = iconGO.GetComponent<Image>();
            image.sprite = icon;
            image.color = new Color(0.92f, 0.94f, 0.98f, 1f);
            image.raycastTarget = false;
        }

    }
}
