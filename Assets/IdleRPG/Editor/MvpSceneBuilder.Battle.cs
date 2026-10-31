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
        private const string EnemyUnitSlotPrefabPath =
            "Assets/IdleRPG/Prefabs/UI/Battle/EnemyUnitSlot.prefab";
        private const string HeroUnitSlotPrefabPath =
            "Assets/IdleRPG/Prefabs/UI/Battle/HeroUnitSlot.prefab";
        private const string AbilityCellPrefabPath =
            "Assets/IdleRPG/Prefabs/UI/Battle/AbilityCell.prefab";

        // ------------------------------------------------------------------
        // Header
        // ------------------------------------------------------------------
        private static HudHeaderUI BuildHeader(RectTransform safeArea)
        {
            Image header = KitPanel("Header", safeArea, UiTheme.Panel);
            PinBar(header.rectTransform, top: true, HeaderBandHeight, UiSpace.ScreenMargin, UiSpace.ScreenMargin);
            HudHeaderUI view = header.gameObject.AddComponent<HudHeaderUI>();

            // The currency chips and the RANKS button share ONE row driven by a layout group, so they divide the
            // width and can never overlap. They used to be placed by hand - chips at 0.03 / 0.35 / 0.65 and the
            // runtime RANKS button at 0.88 - and the token chip and the button collided by about 95 units, which is
            // how a long currency value ended up sliding under the button.
            GameObject row = UiFactory.Node("ChipRow", header.transform);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            UiFactory.Stretch(rowRect, UiSpace.Large, UiSpace.Medium, UiSpace.Large, UiSpace.Medium);

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = UiSpace.Small;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            // Currency chips (icon + value). The row divides the width between them; the RANKS button (built at
            // runtime by HudHeaderUI) takes a fixed slice at the end.
            TextMeshProUGUI goldText = CreateCurrencyChip(row.transform, "GoldChip", "ui_icon_gold");
            TextMeshProUGUI gemText = CreateCurrencyChip(row.transform, "GemChip", "ui_icon_gem");
            TextMeshProUGUI tokenText = CreateCurrencyChip(row.transform, "TokenChip", "ui_icon_token");


            // Boost chip: hidden until an ad boost is running. It stays on the HEADER, not in the row - it is an
            // overlay badge, not a fourth currency, so the layout group must not size it.
            Image boostChip = KitPanel("BoostChip", header.transform, UiTheme.Accent);
            UiFactory.Anchor(boostChip.rectTransform, new Vector2(0.03f, 0.20f), new Vector2(0.53f, 0.92f));

            TextMeshProUGUI boostText = UiFactory.Text("BoostLabel", boostChip.transform, "x2", UiType.Role.Section,
                TextAlignmentOptions.Center, UiTheme.InkOnAccent);
            UiFactory.Stretch(boostText.rectTransform, UiSpace.Medium, UiSpace.Tight, UiSpace.Medium, UiSpace.Tight);
            boostChip.gameObject.SetActive(false);

            SceneWiringUtility.SetField(view, "goldText", goldText);
            SceneWiringUtility.SetField(view, "gemsText", gemText);
            SceneWiringUtility.SetField(view, "tokensText", tokenText);
            SceneWiringUtility.SetField(view, "chipRow", rowRect);
            SceneWiringUtility.SetField(view, "boostChip", boostChip.gameObject);
            SceneWiringUtility.SetField(view, "boostText", boostText);

            return view;
        }

        /// <summary>
        /// Creates one "icon + value" chip inside the header row.
        ///
        /// The row's layout group sizes it, so the chip carries a <see cref="LayoutElement"/> instead of a
        /// hand-picked fraction. Two independently tuned fractions are exactly how the token chip and the RANKS
        /// button ended up overlapping.
        /// </summary>
        private static TextMeshProUGUI CreateCurrencyChip(Transform parent, string name, string iconSprite)
        {
            Image chip = KitPanel(name, parent, UiTheme.PanelLight);
            LayoutElement element = chip.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;

            Image icon = UiFactory.Icon("Icon", chip.transform, Color.white);
            icon.sprite = UiFactory.LoadSprite(iconSprite);
            UiFactory.Anchor(icon.rectTransform, new Vector2(0.04f, 0.12f), new Vector2(0.30f, 0.88f));

            TextMeshProUGUI value = UiFactory.Text("Value", chip.transform, "0", UiType.Role.Display,
                TextAlignmentOptions.MidlineLeft, UiTheme.Text);
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

            TextMeshProUGUI value = UiFactory.Text("Value", barObject.transform, "0 / 0", UiType.Role.Micro,
                TextAlignmentOptions.Center, UiTheme.Text);
            // The bar's value box is dynamic (it shrinks as the enemy stack grows), so an extreme pair must degrade
            // with an ellipsis instead of spilling over the sprite.
            // Masking, not Ellipsis: with NoWrap, TMP's Ellipsis AND Truncate modes render ZERO characters when the
            // text does not fit, so the label goes blank. Masking draws the text and clips it at the box edge.
            value.overflowMode = TextOverflowModes.Masking;
            UiFactory.Stretch(value.rectTransform);

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
            out RectTransform enemyAnchor, out RectTransform viewportRect, out RawImage stageDisplay)
        {
            GameObject viewport = UiFactory.Node("Viewport", pageRoot);
            viewportRect = viewport.GetComponent<RectTransform>();
            UiFactory.Anchor(viewportRect, new Vector2(0f, BattleViewportBottom), new Vector2(1f, StageRowMinY), UiSpace.ScreenMargin, UiSpace.Snug, UiSpace.ScreenMargin, UiSpace.Snug);

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

            HeroUnitView heroPrefab = AssetDatabase.LoadAssetAtPath<HeroUnitView>(HeroUnitSlotPrefabPath);

            if (heroPrefab == null)
            {
                Debug.LogError($"[MvpSceneBuilder] Missing hero slot prefab at {HeroUnitSlotPrefabPath}.");
            }
            else
            {
                SceneWiringUtility.SetField(formationBoard, "heroPrefab", heroPrefab);
            }

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

            // The display is the only child that stays in the HUD viewport. At runtime
            // BattleStageSurface moves the authored stage content behind this RawImage.
            GameObject displayObject = UiFactory.Node("StageDisplay", viewport.transform);
            stageDisplay = displayObject.AddComponent<RawImage>();
            UiFactory.Stretch(stageDisplay.rectTransform);
            stageDisplay.raycastTarget = true;
            stageDisplay.enabled = false;
        }

        /// <summary>Stage/wave row between the header and the battle screen, centered.</summary>
        private static void BuildStageRow(RectTransform pageRoot, HudHeaderUI header)
        {
            Image row = KitPanel("StageRow", pageRoot, UiTheme.Panel);
            UiFactory.Anchor(row.rectTransform, new Vector2(0f, StageRowMinY), new Vector2(1f, StageRowMaxY));

            TextMeshProUGUI label = UiFactory.Text("StageLabel", row.transform, "Stage 1", UiType.Role.Section,
                TextAlignmentOptions.Center, UiTheme.Text);
            UiFactory.Stretch(label.rectTransform, UiSpace.Large, UiSpace.None, UiSpace.Large, UiSpace.None);

            // Same field HudHeaderUI already drives on StageChanged - just relocated out of the header.
            SceneWiringUtility.SetField(header, "stageText", label);
        }

        /// <summary>
        /// The three hero ability buttons, one per party slot, in the band between the viewport and the log
        /// (Ability-Plan). They are always on screen: an ability you can only reach by opening a tab is an ability
        /// the player forgets mid-fight.
        ///
        /// ELI5: three buttons under the fight. Press one and that hero does its special move.
        /// </summary>
        private static AbilityPanelUI BuildAbilityStrip(RectTransform pageRoot, int heroCount)
        {
            Image strip = KitPanel("AbilityStrip", pageRoot, UiTheme.Panel, raycast: true);
            UiFactory.Anchor(strip.rectTransform, new Vector2(0f, LogTop), new Vector2(1f, BattleViewportBottom),
                UiSpace.ScreenMargin, UiSpace.Snug, UiSpace.ScreenMargin, UiSpace.Snug);

            AbilityPanelUI panel = strip.gameObject.AddComponent<AbilityPanelUI>();
            int buttons = heroCount < 1 ? 1 : heroCount;
            AbilityPanelUI.Row[] rows = new AbilityPanelUI.Row[buttons];

            for (int i = 0; i < buttons; i++)
            {
                rows[i] = BuildAbilityButton(strip.transform, i, buttons);
            }

            panel.ConfigureRuntime(rows);
            return panel;
        }

        /// <summary>
        /// One hero's ability cell: the prefab owns the portrait, name, ability, clock and cooldown bar. This
        /// method only places the cell and returns the runtime row the ability panel drives.
        ///
        /// The cell is the button, so the tap target is the full width instead of a small CAPTION-sized box.
        /// Every band is a FRACTION of the cell, so the strip's height cannot make the contents overflow.
        /// </summary>
        private static AbilityPanelUI.Row BuildAbilityButton(Transform parent, int index, int total)
        {
            AbilityCellView prefab = AssetDatabase.LoadAssetAtPath<AbilityCellView>(AbilityCellPrefabPath);

            if (prefab == null)
            {
                Debug.LogError($"[MvpSceneBuilder] Missing ability cell prefab at {AbilityCellPrefabPath}.");
                return null;
            }

            AbilityCellView cell = PrefabUtility.InstantiatePrefab(prefab, parent) as AbilityCellView;

            if (cell == null)
            {
                Debug.LogError("[MvpSceneBuilder] Could not instantiate the ability cell prefab.");
                return null;
            }

            GameObject instance = cell.gameObject;
            instance.name = "Hero" + index;

            UiFactory.Anchor((RectTransform)instance.transform,
                new Vector2(index / (float)total, 0f), new Vector2((index + 1) / (float)total, 1f),
                UiSpace.Small, UiSpace.Tight, UiSpace.Small, UiSpace.Tight);

            return cell.ToRow(index);
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

        /// <summary>
        /// One enemy slot: the prefab owns the sprite, name, HP bar and boss frame. The stack
        /// (<see cref="EnemyStackView"/>) owns the anchor, size and wave count at runtime.
        /// </summary>
        private static EnemyUnitView BuildEnemySlot(Transform parent, int index)
        {
            EnemyUnitView prefab = AssetDatabase.LoadAssetAtPath<EnemyUnitView>(EnemyUnitSlotPrefabPath);

            if (prefab == null)
            {
                Debug.LogError($"[MvpSceneBuilder] Missing enemy slot prefab at {EnemyUnitSlotPrefabPath}.");
                return null;
            }

            EnemyUnitView slot = PrefabUtility.InstantiatePrefab(prefab, parent) as EnemyUnitView;

            if (slot == null)
            {
                Debug.LogError("[MvpSceneBuilder] Could not instantiate the enemy slot prefab.");
                return null;
            }

            slot.name = "EnemySlot" + index;
            slot.gameObject.SetActive(false);
            return slot;
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
            TextMeshProUGUI label = UiFactory.Text("Label", template.transform, "0", UiType.Role.Damage,
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
            Image strip = KitPanel("CombatLog", pageRoot, UiTheme.Panel, raycast: true);
            UiFactory.Anchor(strip.rectTransform, Vector2.zero, new Vector2(1f, LogTop), UiSpace.ScreenMargin, UiSpace.Snug, UiSpace.ScreenMargin, UiSpace.Snug);
            CombatLogUI ui = strip.gameObject.AddComponent<CombatLogUI>();

            ScrollRect scroll = UiFactory.CreateScrollView(strip.transform, "Scroll", UiSpace.Hairline, UiSpace.Padding(UiSpace.Base, UiSpace.Snug), out RectTransform content, autoSizeContent: false);

        // The feed drives its own scroll position, so the ScrollRect must never spring the content back:
        // Clamped, no inertia.
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false;

        // Real drags pause auto-follow; nothing else does.
        LogScrollDragRelay dragRelay = scroll.gameObject.AddComponent<LogScrollDragRelay>();
        dragRelay.Bind(ui);

            // The whole pool copies this template, so the font and the dialogue size land everywhere.
            TextMeshProUGUI template = UiFactory.Text("LineTemplate", content, "line", UiType.Role.Dialogue,
                TextAlignmentOptions.MidlineLeft, UiTheme.Text);
            LayoutElement element = template.gameObject.AddComponent<LayoutElement>();
            element.minHeight = UiTheme.LogLineHeight;
            element.preferredHeight = UiTheme.LogLineHeight;
            template.gameObject.SetActive(false);

            SceneWiringUtility.SetField(ui, "scrollRect", scroll);
            SceneWiringUtility.SetField(ui, "content", content);
            SceneWiringUtility.SetField(ui, "lineTemplate", template);

            // Feed tuning (one line per action; the list is a 100-line scrollback window).
            SceneWiringUtility.SetField(ui, "maxLines", 100);
            SceneWiringUtility.SetField(ui, "lineHeight", UiTheme.LogLineHeight);
            SceneWiringUtility.SetField(ui, "lineSpacing", 4);
            SceneWiringUtility.SetField(ui, "typewriter", true);
            SceneWiringUtility.SetField(ui, "typewriterCharsPerSecond", 45f);
            SceneWiringUtility.SetField(ui, "heroHitColor", UiTheme.Text);
            SceneWiringUtility.SetField(ui, "criticalColor", UiTheme.AccentLight);
            SceneWiringUtility.SetField(ui, "goldColor", UiTheme.AccentLight);
            return ui;
        }
    }
}
