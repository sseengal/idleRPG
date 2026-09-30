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
            Image header = KitPanel("Header", safeArea, UiTheme.Panel);
            UiFactory.Anchor(header.rectTransform, new Vector2(0f, HeaderBottom), Vector2.one, 14f, 0f, 14f, 14f);
            HudHeaderUI view = header.gameObject.AddComponent<HudHeaderUI>();

            // Currency chips (icon + value), evenly spaced across the top row.
            TextMeshProUGUI goldText = CreateCurrencyChip(header.transform, "GoldChip", "ui_icon_gold", 0.03f, 0.32f);
            TextMeshProUGUI gemText = CreateCurrencyChip(header.transform, "GemChip", "ui_icon_gem", 0.35f, 0.62f);
            TextMeshProUGUI tokenText = CreateCurrencyChip(header.transform, "TokenChip", "ui_icon_token", 0.65f, 0.97f);


            // Boost chip: hidden until an ad boost is running.
            Image boostChip = KitPanel("BoostChip", header.transform, UiTheme.Accent);
            UiFactory.Anchor(boostChip.rectTransform, new Vector2(0.03f, 0.20f), new Vector2(0.53f, 0.92f));

            TextMeshProUGUI boostText = UiFactory.Text("BoostLabel", boostChip.transform, "x2", 28f,
                TextAlignmentOptions.Center, UiTheme.InkOnAccent);
            UiTheme.ApplyFont(boostText, UiTheme.Display, 28f);
            UiFactory.Stretch(boostText.rectTransform, 12f, 4f, 12f, 4f);
            boostChip.gameObject.SetActive(false);

            SceneWiringUtility.SetField(view, "goldText", goldText);
            SceneWiringUtility.SetField(view, "gemsText", gemText);
            SceneWiringUtility.SetField(view, "tokensText", tokenText);
            SceneWiringUtility.SetField(view, "boostChip", boostChip.gameObject);
            SceneWiringUtility.SetField(view, "boostText", boostText);

            return view;
        }

        /// <summary>Creates one "icon + value" chip inside the header.</summary>
        private static TextMeshProUGUI CreateCurrencyChip(Transform parent, string name, string iconSprite,
            float xMin, float xMax)
        {
            Image chip = KitPanel(name, parent, UiTheme.PanelLight);
            UiFactory.Anchor(chip.rectTransform, new Vector2(xMin, 0.20f), new Vector2(xMax, 0.92f));

            Image icon = UiFactory.Icon("Icon", chip.transform, Color.white);
            icon.sprite = UiFactory.LoadSprite(iconSprite);
            UiFactory.Anchor(icon.rectTransform, new Vector2(0.04f, 0.12f), new Vector2(0.30f, 0.88f));

            TextMeshProUGUI value = UiFactory.Text("Value", chip.transform, "0", UiTheme.NumberSize,
                TextAlignmentOptions.MidlineLeft, UiTheme.Text);
            UiTheme.ApplyFont(value, UiTheme.Display, UiTheme.NumberSize);
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

            Image background = UiFactory.Icon("Background", barObject.transform, new Color(0f, 0f, 0f, 0.5f), false);
            UiFactory.Stretch(background.rectTransform);

            Image ghost = CreateFilledImage("Ghost", background.transform, new Color(1f, 0.85f, 0.35f, 0.5f));
            Image fill = CreateFilledImage("Fill", background.transform, new Color(0.35f, 0.85f, 0.45f, 1f));

            TextMeshProUGUI value = UiFactory.Text("Value", barObject.transform, "0 / 0", 20f,
                TextAlignmentOptions.Center, UiTheme.Text);
            UiTheme.ApplyFont(value, UiTheme.Display, 20f);
            UiFactory.Stretch(value.rectTransform, 6f, 0f, 6f, 0f);

            SceneWiringUtility.SetField(view, "fillImage", fill);
            SceneWiringUtility.SetField(view, "ghostImage", ghost);
            SceneWiringUtility.SetField(view, "valueLabel", value);

            return view;
        }

        /// <summary>A horizontal Filled image that covers the whole parent rect.</summary>
        private static Image CreateFilledImage(string name, Transform parent, Color color)
        {
            Image image = UiFactory.Icon(name, parent, color, false);
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
            UiFactory.Anchor(viewport.GetComponent<RectTransform>(), new Vector2(0f, BattleViewportBottom), new Vector2(1f, StageRowMinY), 14f, 6f, 14f, 6f);

            // Clips the cover-fitted backdrop so its overflow never draws over the header or combat log.
            viewport.AddComponent<RectMask2D>();

            Image background = UiFactory.Icon("Background", viewport.transform, Color.white, false);
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/IdleRPG/Art/Backdrops/forest bg.png");
            background.preserveAspect = false; // aspect handled by the presenter's cover-fit rect

            // Fills the whole viewport as a simple stretched backdrop. Stays strictly inside the viewport
            // band, so it never overlaps the combat log below it or the header above it.
            UiFactory.Stretch(background.rectTransform);

            // Stage-aware backdrop: GameEvents.StageChanged swaps the sprite (stage -> catalog[(stage-1) % count])
            // when the boss is killed or a defeat rolls the party back a stage.
            BackdropPresenter backdropPresenter = background.gameObject.AddComponent<BackdropPresenter>();
            SceneWiringUtility.SetField(backdropPresenter, "background", background);
            SceneWiringUtility.SetField(backdropPresenter, "catalog", SceneWiringUtility.LoadBackdropCatalog());

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

            // Test pass 2026-09-30: bigger hero slots so placeholder art + HP bars read clearly.
            SceneWiringUtility.SetField(formationBoard, "slotSize", new Vector2(160f, 176f));
            // Cleanup pass 2026-09-30: no translucent tiles / row indicators behind heroes on the battle view.
            SceneWiringUtility.SetField(formationBoard, "slotColor", new Color(1f, 1f, 1f, 0f));

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

        /// <summary>Stage/wave row between the header and the battle screen, centered.</summary>
        private static void BuildStageRow(RectTransform pageRoot, HudHeaderUI header)
        {
            Image row = KitPanel("StageRow", pageRoot, UiTheme.Panel);
            UiFactory.Anchor(row.rectTransform, new Vector2(0f, StageRowMinY), new Vector2(1f, StageRowMaxY));

            TextMeshProUGUI label = UiFactory.Text("StageLabel", row.transform, "Stage 1", 30f,
                TextAlignmentOptions.Center, UiTheme.Text);
            UiTheme.ApplyFont(label, UiTheme.Display, 30f);
            UiFactory.Stretch(label.rectTransform, 24f, 0f, 24f, 0f);

            // Same field HudHeaderUI already drives on StageChanged - just relocated out of the header.
            SceneWiringUtility.SetField(header, "stageText", label);
        }

        /// <summary>"BATTLE LOG" row between the battle sim and the log (the future tab anchor).</summary>
        private static void BuildLogHeaderRow(RectTransform pageRoot)
        {
            Image row = KitPanel("BattleLogHeader", pageRoot, UiTheme.Panel);
            UiFactory.Anchor(row.rectTransform, new Vector2(0f, LogTop), new Vector2(1f, BattleViewportBottom), 14f, 0f, 14f, 0f);

            TextMeshProUGUI label = UiFactory.Text("LogTitle", row.transform, "BATTLE LOG", 26f,
                TextAlignmentOptions.MidlineLeft, UiTheme.TextDim);
            UiTheme.ApplyFont(label, UiTheme.Display, 26f);
            UiFactory.Stretch(label.rectTransform, 28f, 0f, 28f, 0f);
        }

        /// <summary>
        /// A battle-page surface drawn with the kit's WHITE-face sprite so a palette tint renders exactly on
        /// the token colour. Tinting theme colours over the OLD placeholder sprites darkened them (their grey
        /// faces are 0.11-0.20 brightness), which is why the page read near-black instead of slate.
        /// </summary>
        private static Image KitPanel(string name, Transform parent, Color color, bool raycast = false)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                UiArtPresetApplier.UiArtRoot + "/9-Slice/Colored/grey.png");

            Image image = UiFactory.Icon(name, parent, color, false);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = raycast;
            UiFactory.Stretch(image.rectTransform);

            if (sprite == null)
            {
                Debug.LogWarning("[MvpSceneBuilder] Missing kit face sprite - run Tools > Idle RPG > Art > Apply UI-Art Presets.");
            }

            return image;
        }

        /// <summary>One enemy slot: sprite, name, HP bar and the boss frame. Laid out by <see cref="EnemyStackView"/>.</summary>
        private static EnemyUnitView BuildEnemySlot(Transform parent, int index)
        {
            GameObject slot = UiFactory.Node("EnemySlot" + index, parent);
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            UiFactory.Anchor(slotRect, new Vector2(0f, 1f), new Vector2(1f, 1f));

            Image icon = UiFactory.Icon("Icon", slot.transform, Color.white);
            UiFactory.CenterOn(icon.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(170f, 170f));
            icon.enabled = false;

            TextMeshProUGUI nameLabel = UiFactory.Text("Name", slot.transform, "Enemy", 24f,
                TextAlignmentOptions.Center, UiTheme.Text);
            UiTheme.ApplyFont(nameLabel, UiTheme.Display, 24f);
            UiFactory.Anchor(nameLabel.rectTransform, new Vector2(0f, 0.14f), new Vector2(1f, 0.32f));

            HpBarView hpBar = CreateHpBar(slot.transform, "HpBar");
            RectTransform hpRect = hpBar.GetComponent<RectTransform>();
            // Same width logic as the heroes (0.80 of their 160px slot): a fixed ~128px bar centred under the
            // enemy, instead of stretching across the whole wide enemy column.
            UiFactory.Anchor(hpRect, new Vector2(0.5f, 0.02f), new Vector2(0.5f, 0.12f));
            hpRect.pivot = new Vector2(0.5f, 0.5f);
            hpRect.sizeDelta = new Vector2(128f, 0f);

            EnemyUnitView view = slot.AddComponent<EnemyUnitView>();
            view.ConfigureRuntime(index, icon, hpBar, nameLabel, slotRect, null);

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
            TextMeshProUGUI label = UiFactory.Text("Label", template.transform, "0", UiTheme.DamageSize,
                TextAlignmentOptions.Center, Color.white);
            UiTheme.ApplyFont(label, UiTheme.Display, UiTheme.DamageSize);
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
            Image strip = KitPanel("CombatLog", pageRoot, UiTheme.Panel, raycast: true);
            UiFactory.Anchor(strip.rectTransform, Vector2.zero, new Vector2(1f, LogTop), 14f, 6f, 14f, 6f);
            CombatLogUI ui = strip.gameObject.AddComponent<CombatLogUI>();

            ScrollRect scroll = UiFactory.CreateScrollView(strip.transform, "Scroll", 2f, new RectOffset(10, 10, 6, 6), out RectTransform content, autoSizeContent: false);

            // The whole pool copies this template, so the font and the bigger VT323 size land everywhere.
            TextMeshProUGUI template = UiFactory.Text("LineTemplate", content, "line", UiTheme.LogSize,
                TextAlignmentOptions.MidlineLeft, UiTheme.Text);
            UiTheme.ApplyFont(template, UiTheme.Sentence, UiTheme.LogSize);
            LayoutElement element = template.gameObject.AddComponent<LayoutElement>();
            element.minHeight = UiTheme.LogLineHeight;
            element.preferredHeight = UiTheme.LogLineHeight;
            template.gameObject.SetActive(false);

            SceneWiringUtility.SetField(ui, "scrollRect", scroll);
            SceneWiringUtility.SetField(ui, "content", content);
            SceneWiringUtility.SetField(ui, "lineTemplate", template);

            // Feed tuning for multi-enemy waves (Step 11e): a bigger font needs taller lines, and the
            // event colours map onto the picked palette.
            SceneWiringUtility.SetField(ui, "lineHeight", UiTheme.LogLineHeight);
            SceneWiringUtility.SetField(ui, "lineSpacing", 2);
            SceneWiringUtility.SetField(ui, "heroHitColor", UiTheme.Text);
            SceneWiringUtility.SetField(ui, "criticalColor", UiTheme.AccentLight);
            SceneWiringUtility.SetField(ui, "goldColor", UiTheme.AccentLight);
            SceneWiringUtility.SetField(ui, "maxLinesPerSecond", 5);
            SceneWiringUtility.SetField(ui, "aggregateWindowSec", 0.5f);
            SceneWiringUtility.SetField(ui, "poolSize", 36);
            return ui;
        }
    }
}
