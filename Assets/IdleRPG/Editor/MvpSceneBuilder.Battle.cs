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
    /// Battle screen construction: HUD header, HP bars, the formation/enemy viewport, damage text pool and combat log.
    /// </summary>
    public static partial class MvpSceneBuilder
    {
        // ------------------------------------------------------------------
        // Header
        // ------------------------------------------------------------------
        private static HudHeaderUI BuildHeader(RectTransform safeArea)
        {
            Image header = UiFactory.Panel("Header", safeArea, "ui_panel", new Color(1f, 1f, 1f, 0.98f));
            UiFactory.Anchor(header.rectTransform, new Vector2(0f, HeaderBottom), Vector2.one, 14f, 0f, 14f, 14f);
            HudHeaderUI view = header.gameObject.AddComponent<HudHeaderUI>();

            // Currency chips (icon + value), evenly spaced across the top row.
            TextMeshProUGUI goldText = CreateCurrencyChip(header.transform, "GoldChip", "ui_icon_gold", 0.03f, 0.32f);
            TextMeshProUGUI gemText = CreateCurrencyChip(header.transform, "GemChip", "ui_icon_gem", 0.35f, 0.62f);
            TextMeshProUGUI tokenText = CreateCurrencyChip(header.transform, "TokenChip", "ui_icon_token", 0.65f, 0.97f);

            TextMeshProUGUI stageText = UiFactory.Text("StageLabel", header.transform, "Stage 1", 34f,
                TextAlignmentOptions.MidlineRight, TextColor);
            UiFactory.Anchor(stageText.rectTransform, new Vector2(0.55f, 0.56f), new Vector2(0.98f, 0.92f));

            TextMeshProUGUI yieldText = UiFactory.Text("YieldLabel", header.transform, "Ascend: 0", 26f,
                TextAlignmentOptions.MidlineRight, DimTextColor);
            UiFactory.Anchor(yieldText.rectTransform, new Vector2(0.55f, 0.14f), new Vector2(0.98f, 0.5f));

            // Boost chip: hidden until an ad boost is running.
            Image boostChip = UiFactory.Panel("BoostChip", header.transform, "ui_button_gold", new Color(1f, 1f, 1f, 0.9f));
            UiFactory.Anchor(boostChip.rectTransform, new Vector2(0.03f, 0.06f), new Vector2(0.53f, 0.46f));

            TextMeshProUGUI boostText = UiFactory.Text("BoostLabel", boostChip.transform, "x2", 28f,
                TextAlignmentOptions.Center, new Color(0.15f, 0.1f, 0.02f, 1f));
            UiFactory.Stretch(boostText.rectTransform, 12f, 4f, 12f, 4f);
            boostChip.gameObject.SetActive(false);

            SceneWiringUtility.SetField(view, "goldText", goldText);
            SceneWiringUtility.SetField(view, "gemsText", gemText);
            SceneWiringUtility.SetField(view, "tokensText", tokenText);
            SceneWiringUtility.SetField(view, "stageText", stageText);
            SceneWiringUtility.SetField(view, "yieldText", yieldText);
            SceneWiringUtility.SetField(view, "boostChip", boostChip.gameObject);
            SceneWiringUtility.SetField(view, "boostText", boostText);

            return view;
        }

        /// <summary>Creates one "icon + value" chip inside the header.</summary>
        private static TextMeshProUGUI CreateCurrencyChip(Transform parent, string name, string iconSprite,
            float xMin, float xMax)
        {
            Image chip = UiFactory.Panel(name, parent, "ui_panel_light", new Color(1f, 1f, 1f, 0.55f));
            UiFactory.Anchor(chip.rectTransform, new Vector2(xMin, 0.55f), new Vector2(xMax, 0.95f));

            Image icon = UiFactory.Icon("Icon", chip.transform, Color.white);
            icon.sprite = UiFactory.LoadSprite(iconSprite);
            UiFactory.Anchor(icon.rectTransform, new Vector2(0.04f, 0.12f), new Vector2(0.30f, 0.88f));

            TextMeshProUGUI value = UiFactory.Text("Value", chip.transform, "0", 32f,
                TextAlignmentOptions.MidlineLeft, TextColor);
            UiFactory.Anchor(value.rectTransform, new Vector2(0.32f, 0f), new Vector2(0.98f, 1f));

            return value;
        }

        // ------------------------------------------------------------------
        // Health bars (shared by units)
        // ------------------------------------------------------------------
        private static HpBarView CreateHpBar(Transform parent, string name)
        {
            GameObject barObject = UiFactory.Node(name, parent);
            HpBarView view = barObject.AddComponent<HpBarView>();

            Image background = UiFactory.Panel("Background", barObject.transform, "ui_panel", new Color(0f, 0f, 0f, 0.65f));
            UiFactory.Stretch(background.rectTransform);

            Image ghost = CreateFilledImage("Ghost", background.transform, Color.white);
            Image fill = CreateFilledImage("Fill", background.transform, new Color(0.35f, 0.85f, 0.45f, 1f));

            TextMeshProUGUI value = UiFactory.Text("Value", barObject.transform, "0 / 0", 22f,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Stretch(value.rectTransform, 6f, 0f, 6f, 0f);

            SceneWiringUtility.SetField(view, "fillImage", fill);
            SceneWiringUtility.SetField(view, "ghostImage", ghost);
            SceneWiringUtility.SetField(view, "valueLabel", value);

            return view;
        }

        /// <summary>A horizontal Filled image that covers the whole parent rect.</summary>
        private static Image CreateFilledImage(string name, Transform parent, Color color)
        {
            Image image = UiFactory.Panel(name, parent, "ui_panel", color);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 1f;
            UiFactory.Stretch(image.rectTransform);
            return image;
        }

        // ------------------------------------------------------------------
        // Combat viewport
        // ------------------------------------------------------------------
        private static void BuildViewport(RectTransform pageRoot, RectTransform damageRoot,
            out FormationBoardView formationBoard, out EnemyStackView enemyStack, out FloatingDamageTextPool damagePool,
            out RectTransform enemyAnchor)
        {
            GameObject viewport = UiFactory.Node("Viewport", pageRoot);
            UiFactory.Anchor(viewport.GetComponent<RectTransform>(), new Vector2(0f, BattleViewportBottom), Vector2.one, 14f, 6f, 14f, 6f);

            Image background = UiFactory.Icon("Background", viewport.transform, new Color(1f, 1f, 1f, 0.9f), false);
            background.sprite = UiFactory.LoadSprite("combat_bg");
            UiFactory.Stretch(background.rectTransform);

            // The party board is drawn in code from FormationData: two vertical columns (front rank nearest the
            // enemy, back rank behind it), one view per slot. Display only - swapping lives on the Party screen.
            GameObject boardObject = UiFactory.Node("FormationBoard", viewport.transform);
            RectTransform boardRect = boardObject.GetComponent<RectTransform>();
            boardRect.anchorMin = new Vector2(0.01f, 0.5f);
            boardRect.anchorMax = new Vector2(0.01f, 0.5f);
            boardRect.pivot = new Vector2(0f, 0.5f);
            boardRect.anchoredPosition = Vector2.zero;
            boardRect.sizeDelta = new Vector2(320f, 470f);

            formationBoard = boardObject.AddComponent<FormationBoardView>();

            // The enemy side is a vertical stack of up to 3 slots, each with its own sprite, name and HP bar.
            // Enemies have no ranks: the stack is presentation only (the sim decides who gets hit).
            GameObject stackObject = UiFactory.Node("EnemyStack", viewport.transform);
            RectTransform stackRect = stackObject.GetComponent<RectTransform>();
            UiFactory.Anchor(stackRect, new Vector2(0.54f, 0.10f), new Vector2(0.98f, 0.92f));

            EnemyUnitView[] enemySlots = new EnemyUnitView[EnemyStackView.MaxSlots];

            for (int i = 0; i < enemySlots.Length; i++)
            {
                enemySlots[i] = BuildEnemySlot(stackRect, i);
                enemySlots[i].gameObject.SetActive(false);
            }

            RectTransform[] enemyAnchors = new RectTransform[EnemyStackView.MaxSlots];

            for (int i = 0; i < enemyAnchors.Length; i++)
            {
                enemyAnchors[i] = BuildEnemyAnchor(stackRect, i);
            }

            EnemyStackView stack = stackObject.AddComponent<EnemyStackView>();
            SceneWiringUtility.SetField(stack, "container", stackRect);
            SceneWiringUtility.SetField(stack, "slots", enemySlots);
            SceneWiringUtility.SetField(stack, "anchors", enemyAnchors);
            enemyStack = stack;

            // Legacy single anchor (kept as the pool's fallback) points at the first slot.
            enemyAnchor = enemyAnchors[0];

            damagePool = BuildDamageTextPool(damageRoot);
        }

        /// <summary>One enemy slot: sprite, name, HP bar and the boss frame. Laid out by <see cref="EnemyStackView"/>.</summary>
        private static EnemyUnitView BuildEnemySlot(Transform parent, int index)
        {
            GameObject slot = UiFactory.Node("EnemySlot" + index, parent);
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            UiFactory.Anchor(slotRect, new Vector2(0f, 1f), new Vector2(1f, 1f));

            Image bossFrame = UiFactory.Panel("BossFrame", slot.transform, "ui_panel_bordered", new Color(1f, 0.85f, 0.35f, 0.85f));
            UiFactory.Stretch(bossFrame.rectTransform);
            bossFrame.enabled = false;

            Image icon = UiFactory.Icon("Icon", slot.transform, Color.white);
            UiFactory.CenterOn(icon.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(170f, 170f));
            icon.enabled = false;

            TextMeshProUGUI nameLabel = UiFactory.Text("Name", slot.transform, "Enemy", 24f,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(nameLabel.rectTransform, new Vector2(0f, 0.80f), new Vector2(1f, 0.98f));

            HpBarView hpBar = CreateHpBar(slot.transform, "HpBar");
            UiFactory.Anchor(hpBar.GetComponent<RectTransform>(), new Vector2(0.10f, 0.03f), new Vector2(0.90f, 0.18f));

            EnemyUnitView view = slot.AddComponent<EnemyUnitView>();
            view.ConfigureRuntime(index, icon, hpBar, nameLabel, slotRect, bossFrame);

            return view;
        }

        /// <summary>
        /// Floating-text anchor for one enemy slot. Lives under the stack (so damage numbers draw above the
        /// enemy sprite) and is moved to the slot's vertical centre by <see cref="EnemyStackView"/>.
        /// </summary>
        private static RectTransform BuildEnemyAnchor(Transform parent, int index)
        {
            GameObject anchor = UiFactory.Node("EnemyAnchor" + index, parent);
            RectTransform rect = anchor.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1f, 1f);
            return rect;
        }

        private static FloatingDamageTextPool BuildDamageTextPool(RectTransform damageRoot)
        {
            GameObject template = UiFactory.Node("DamageTextTemplate", damageRoot);
            FloatingDamageTextView textView = template.AddComponent<FloatingDamageTextView>();
            TextMeshProUGUI label = UiFactory.Text("Label", template.transform, "0", 40f,
                TextAlignmentOptions.Center, Color.white);
            UiFactory.Stretch(label.rectTransform);

            SceneWiringUtility.SetField(textView, "label", label);
            SceneWiringUtility.SetField(textView, "rectTransform", template.GetComponent<RectTransform>());
            template.SetActive(false);

            GameObject poolObject = UiFactory.Node("DamageTextPool", damageRoot);
            FloatingDamageTextPool pool = poolObject.AddComponent<FloatingDamageTextPool>();
            SceneWiringUtility.SetField(pool, "prefab", textView);
            return pool;
        }

        /// <summary>Combat feed strip between the viewport and the control dock.</summary>
        private static CombatLogUI BuildCombatLog(RectTransform pageRoot)
        {
            Image strip = UiFactory.Panel("CombatLog", pageRoot, "ui_panel", new Color(1f, 1f, 1f, 0.95f), raycast: true);
            UiFactory.Anchor(strip.rectTransform, Vector2.zero, new Vector2(1f, BattleViewportBottom), 14f, 6f, 14f, 6f);
            CombatLogUI ui = strip.gameObject.AddComponent<CombatLogUI>();

            ScrollRect scroll = UiFactory.CreateScrollView(strip.transform, "Scroll", 2f, new RectOffset(10, 10, 6, 6), out RectTransform content, autoSizeContent: false);

            TextMeshProUGUI template = UiFactory.Text("LineTemplate", content, "line", 22f,
                TextAlignmentOptions.MidlineLeft, TextColor);
            LayoutElement element = template.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 34f;
            element.preferredHeight = 34f;
            template.gameObject.SetActive(false);

            SceneWiringUtility.SetField(ui, "scrollRect", scroll);
            SceneWiringUtility.SetField(ui, "content", content);
            SceneWiringUtility.SetField(ui, "lineTemplate", template);

            // Feed tuning for multi-enemy waves (Step 11e): more lines allowed per second, a slightly longer
            // merge window, and a deeper pool so a burst of hits never allocates during combat.
            SceneWiringUtility.SetField(ui, "maxLinesPerSecond", 5);
            SceneWiringUtility.SetField(ui, "aggregateWindowSec", 0.5f);
            SceneWiringUtility.SetField(ui, "poolSize", 36);
            return ui;
        }
    }
}
