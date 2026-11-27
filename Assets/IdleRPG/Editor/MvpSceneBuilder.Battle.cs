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

        /// <summary>The currency plate art, cropped from the owner's 100 x 100 export to its 30 x 30 block.</summary>
        private const string CurrencyPlatePath = "Assets/IdleRPG/Art/UI/Containers/CurrencyContainer.png";

        /// <summary>The green plus on a currency plate: cropped from the owner's 100 x 100 export to its 32 x 32 mark.</summary>
        private const string CurrencyPlusPath = "Assets/IdleRPG/Art/UI/Icons/currency_plus.png";

        /// <summary>
        /// 1 art pixel = 2 canvas units. The art is 30 px for a 64-unit icon, so a plate drawn at 1:1 would be half
        /// the height of the chip; at 1/2 it is 60 units and its 4 px rim is 8 units, the same air the icon leaves.
        /// </summary>
        private const float CurrencyPlateScale = 0.5f;

        // ------------------------------------------------------------------
        // Header
        // ------------------------------------------------------------------
        private static HudHeaderUI BuildHeader(RectTransform safeArea)
        {
            // The bar draws NOTHING (owner, 2026-11-25): the currency plates are the only surfaces left on it. It is
            // exactly one plate tall, pinned one page margin under the safe-area top, so the air around the plates is
            // PageMargin above and beside and SectionGap below - the same margins as the page under it.
            GameObject header = UiFactory.Node("Header", safeArea);
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, -UiSpace.PageMargin);
            headerRect.sizeDelta = new Vector2(-2f * UiSpace.PageMargin, UiSpace.HeaderBarHeight);
            HudHeaderUI view = header.AddComponent<HudHeaderUI>();

            // The currency chips and the menu button share ONE row driven by a layout group, so they divide the
            // width and can never overlap. They used to be placed by hand - chips at 0.03 / 0.35 / 0.65 and a
            // button at 0.88 - and the token chip and the button collided by about 95 units, which is how a long
            // currency value ended up sliding under the button.
            //
            // The row stops where the menu zone starts: one icon plus one page margin, which is exactly the space the
            // menu icon is centred in between the last plate and the screen edge (owner, 2026-11-25).
            GameObject row = UiFactory.Node("ChipRow", header.transform);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            UiFactory.Stretch(rowRect, 0f, 0f, UiSpace.ChipIconSize + UiSpace.PageMargin, 0f);

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = UiSpace.Space8;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            // Currency chips (icon + value). The row divides the width between them; the menu button (built at
            // runtime by HudHeaderUI) takes a fixed slice at the end.
            TextMeshProUGUI goldText = CreateCurrencyChip(row.transform, "GoldChip", "ui_icon_gold");
            TextMeshProUGUI gemText = CreateCurrencyChip(row.transform, "GemChip", "ui_icon_gem");

            // Boost badge: hidden until an ad boost is running. It is a ROW item, not a floating overlay - it used to
            // sit at fixed fractions over the header, which put it on top of the gold chip whenever the badge
            // appeared. While it shows, the row has three items instead of two.
            Image boostChip = CreateCurrencyPlate(row.transform, "BoostChip");
            LayoutElement boostElement = boostChip.gameObject.AddComponent<LayoutElement>();
            boostElement.flexibleWidth = 1f;

            TextMeshProUGUI boostText = UiFactory.Text("BoostLabel", boostChip.transform, "x2", UiType.Role.Section,
                TextAlignmentOptions.Center, UiTheme.Text);
            UiFactory.Stretch(boostText.rectTransform, UiSpace.ChipIconInset, UiSpace.LabelPadY,
                UiSpace.ChipIconInset, UiSpace.LabelPadY);
            boostChip.gameObject.SetActive(false);

            SceneWiringUtility.SetField(view, "goldText", goldText);
            SceneWiringUtility.SetField(view, "gemsText", gemText);
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
        ///
        /// The icon is <see cref="UiSpace.ChipIconSize"/> (the chip minus one inset above and below), so the picture
        /// can never be taller than its chip. The value shrinks instead of spilling: an idle game reaches six-digit
        /// numbers early, and a fixed 56 in a 176-wide box cut them off.
        /// </summary>
        private static TextMeshProUGUI CreateCurrencyChip(Transform parent, string name, string iconSprite)
        {
            Image chip = CreateCurrencyPlate(parent, name);
            LayoutElement element = chip.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;

            Image icon = UiFactory.Icon("Icon", chip.transform, Color.white);
            icon.sprite = UiFactory.LoadSprite(iconSprite);

            // Padded, not percentage-placed: the icon centres in its slot one inset in from the plate's left edge, the
            // value centres in the space that is left, and the right edge keeps the same inset (owner, 2026-11-25).
            // The slot is wider than the icon so a wide gem and a narrow key share one centre line.
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(UiSpace.ChipIconSlot, UiSpace.ChipIconSize);
            iconRect.anchoredPosition = new Vector2(UiSpace.ChipIconInset, 0f);
            icon.preserveAspect = true;

            TextMeshProUGUI value = UiFactory.Text("Value", chip.transform, "0", UiType.Role.Display,
                TextAlignmentOptions.Midline, UiTheme.Text);
            // No vertical inset: the display font's own line box is taller than the chip, so an inset would make
            // TMP shrink the number to fit the BOX instead of only shrinking a too-long number. A zero inset is not
            // a spacing choice, so it does not need a role.
            UiFactory.Anchor(value.rectTransform, Vector2.zero, Vector2.one,
                UiSpace.ChipIconInset + UiSpace.ChipIconSlot + UiSpace.Space8, 0f,
                UiSpace.ChipIconInset, 0f);
            UiType.ApplyAuto(value, UiType.Role.Display);

            // Buy badge: a tiny green plus on the icon's bottom-right corner (owner, 2026-11-25). It sits ON the
            // icon's slot corner, not beside it, so it overlaps the icon. It is decoration: the whole plate is the
            // button in the lobby, so the badge never takes a tap. HudHeaderUI shows it only between runs.
            Image badge = UiFactory.Icon("BuyBadge", chip.transform, Color.white, false);
            badge.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CurrencyPlusPath);
            badge.raycastTarget = false;
            RectTransform badgeRect = badge.rectTransform;
            badgeRect.anchorMin = new Vector2(0f, 0.5f);
            badgeRect.anchorMax = new Vector2(0f, 0.5f);
            badgeRect.pivot = new Vector2(1f, 0f);
            badgeRect.sizeDelta = new Vector2(UiSpace.ChipBadgeSize, UiSpace.ChipBadgeSize);
            badgeRect.anchoredPosition = new Vector2(
                UiSpace.ChipIconInset + UiSpace.ChipIconSlot, -UiSpace.ChipIconSize * 0.5f);
            badge.gameObject.SetActive(false);

            return value;
        }

        /// <summary>
        /// The plate every currency item sits on: the owner's CurrencyContainer art (a 30 x 30 bevelled block).
        ///
        /// It was exported on a 100 x 100 canvas with the block at x 34-63, y 38-67, so the asset HERE is that block
        /// cropped out (the source PNG at the project root is untouched). The 4 px rim is a 9-slice border, so the
        /// bevel stays 1 art pixel at any width instead of smearing when a chip grows.
        ///
        /// The plate is drawn at 2x: 1 art pixel = 2 canvas units, so the 30 px art is 60 units tall inside a 64-unit
        /// chip and the 4 px rim reads as 8 units - the same air the icon leaves.
        /// </summary>
        private static Image CreateCurrencyPlate(Transform parent, string name)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CurrencyPlatePath);

            if (sprite == null)
            {
                Debug.LogWarning($"[MvpSceneBuilder] Missing {CurrencyPlatePath}; the chip falls back to a flat block.");
                return UiFactory.Panel(name, parent, (string)null, UiTheme.SurfaceChip, raycast: false);
            }

            Image plate = UiFactory.Icon(name, parent, Color.white, false);
            plate.sprite = sprite;
            plate.type = Image.Type.Sliced;
            plate.pixelsPerUnitMultiplier = CurrencyPlateScale;
            return plate;
        }

        // ------------------------------------------------------------------
        // Health bars (shared by units)
        // ------------------------------------------------------------------
        private static HpBarView CreateHpBar(Transform parent, string name)
        {
            GameObject barObject = UiFactory.Node(name, parent);
            HpBarView view = barObject.AddComponent<HpBarView>();

            Image background = UiFactory.Icon("Background", barObject.transform, UiTheme.ShadeHalf, false);
            UiFactory.Stretch(background.rectTransform);

            Image ghost = CreateFilledImage("Ghost", background.transform, UiTheme.WithAlpha(UiTheme.HpWounded, 0.5f));
            Image fill = CreateFilledImage("Fill", background.transform, UiTheme.HpHealthy);

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
            // The arena's top follows the fixed stage band, so the air under the dots is the band's own gap and a
            // taller phone grows the arena instead (2026-11-26).
            UiFactory.Anchor(viewportRect, new Vector2(0f, XpBandTop), new Vector2(1f, 1f),
                UiSpace.PageMargin, UiSpace.Space8, UiSpace.PageMargin, StageBandHeight);

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
            // 15 Nov 2026: height 176 -> 260. The art keeps its size; the extra units are the nameplate band (name,
            // health numbers, bar) below the sprite, paid for by the log band, not by the art.
            SceneWiringUtility.SetField(formationBoard, "slotSize", new Vector2(160f, 260f));
            // Cleanup pass 2026-09-30: no translucent tiles / row indicators behind heroes on the battle view.
            SceneWiringUtility.SetField(formationBoard, "slotColor", Color.clear);

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
        /// <summary>
        /// The journey band: the land name over a trail of marks (Docs/Journey.md).
        ///
        /// ELI5: the signpost above the fight. "GREENWAKE", then dots for the steps ahead, a ring for a
        /// rest stop, a diamond for the boss. It replaces the old "Stage 3  wave 2" label.
        /// </summary>
        private static void BuildStageRow(RectTransform pageRoot)
        {
            // No plate behind the trail (owner, 2026-11-25): the land name and the marks sit straight on the page.
            // A grey bar here was a second frame above the fight that meant nothing.
            GameObject rowObject = UiFactory.Node("StageRow", pageRoot);
            RectTransform row = rowObject.GetComponent<RectTransform>();
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(0f, StageBandHeight);
            row.anchoredPosition = Vector2.zero;

            JourneyBandUI band = rowObject.AddComponent<JourneyBandUI>();

            TextMeshProUGUI land = UiFactory.Text("LandLabel", row, "GREENWAKE", UiType.Role.Section,
                TextAlignmentOptions.Center, UiTheme.Text);
            land.rectTransform.anchorMin = new Vector2(0f, 1f);
            land.rectTransform.anchorMax = new Vector2(1f, 1f);
            land.rectTransform.pivot = new Vector2(0.5f, 1f);
            land.rectTransform.sizeDelta = new Vector2(0f, StageNameLine);
            land.rectTransform.anchoredPosition = Vector2.zero;

            GameObject trail = UiFactory.Node("Trail", row);
            RectTransform trailRect = trail.GetComponent<RectTransform>();
            trailRect.anchorMin = new Vector2(0f, 1f);
            trailRect.anchorMax = new Vector2(1f, 1f);
            trailRect.pivot = new Vector2(0.5f, 1f);
            trailRect.sizeDelta = new Vector2(0f, StageTrailHeight);
            trailRect.anchoredPosition = new Vector2(0f, -(StageNameLine + UiSpace.Space8));

            const string catalogPath = "Assets/IdleRPG/Data/Config/JourneyCatalog.asset";
            JourneyCatalog catalog = AssetDatabase.LoadAssetAtPath<JourneyCatalog>(catalogPath);
            Sprite dot = AssetDatabase.LoadAssetAtPath<Sprite>(UiFactory.ArtFolder + "/ui_mark_dot.png");
            Sprite ring = AssetDatabase.LoadAssetAtPath<Sprite>(UiFactory.ArtFolder + "/ui_mark_ring.png");
            Sprite halo = AssetDatabase.LoadAssetAtPath<Sprite>(UiFactory.ArtFolder + "/ui_glow_steps.png");

            // The trail's own glow recipe: the halo art is faint (30% at its brightest), so this style runs at full
            // strength. Sharing the dot style would make the breath invisible (0.30 x 0.55 = 17% on screen).
            GlowStyle glowStyle = AssetDatabase.LoadAssetAtPath<GlowStyle>(
                "Assets/IdleRPG/Data/Config/GlowStyle_Trail.asset");

            if (catalog == null)
            {
                Debug.LogWarning($"[MvpSceneBuilder] Missing journey catalog at {catalogPath}; the band falls back to its land name.");
            }

            if (glowStyle == null)
            {
                Debug.LogWarning("[MvpSceneBuilder] Missing GlowStyle_Dot; the band will not glow.");
            }

            SceneWiringUtility.SetField(band, "landLabel", land);
            SceneWiringUtility.SetField(band, "trailRoot", trailRect);
            SceneWiringUtility.SetField(band, "catalog", catalog);
            SceneWiringUtility.SetField(band, "dotSprite", dot);
            SceneWiringUtility.SetField(band, "ringSprite", ring);
            SceneWiringUtility.SetField(band, "haloSprite", halo);
            SceneWiringUtility.SetField(band, "glowStyle", glowStyle);
        }

        /// <summary>
        /// The three hero ability buttons, one per party slot, in the band between the viewport and the log
        /// (Ability-Plan). They are always on screen: an ability you can only reach by opening a tab is an ability
        /// the player forgets mid-fight.
        ///
        /// ELI5: three buttons under the fight. Press one and that hero does its special move.
        ///
        /// The band itself draws NOTHING: it used to be a grey plate holding three darker plates, which read as a
        /// table of boxes. The band is now a bare layout node and each cell wears the app's own button face (the
        /// shared ui_frame plate in ButtonBlue), so three buttons sit straight on the battle page - the same family
        /// as every other button in the game.
        /// </summary>
        private static AbilityPanelUI BuildAbilityStrip(RectTransform pageRoot, int heroCount)
        {
            GameObject stripObject = UiFactory.Node("AbilityStrip", pageRoot);
            RectTransform stripRect = stripObject.GetComponent<RectTransform>();
            UiFactory.Anchor(stripRect, new Vector2(0f, LogTop), new Vector2(1f, BattleViewportBottom),
                UiSpace.PageMargin, UiSpace.Space8, UiSpace.PageMargin, UiSpace.Space8);

            AbilityPanelUI panel = stripObject.AddComponent<AbilityPanelUI>();
            int buttons = heroCount < 1 ? 1 : heroCount;
            AbilityPanelUI.Row[] rows = new AbilityPanelUI.Row[buttons];

            // Equal cells and one gap size: the same layout the running game applies, so a rebuild cannot differ.
            HorizontalLayoutGroup layout = stripObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = UiSpace.Space8;
            layout.padding = UiSpace.Padding(UiSpace.Space8);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childScaleWidth = false;
            layout.childScaleHeight = false;

            for (int i = 0; i < buttons; i++)
            {
                rows[i] = BuildAbilityButton(stripRect, i);
            }

            panel.ConfigureRuntime(rows);
            return panel;
        }

        /// <summary>
        /// One hero's ability cell. The cell builds itself (<see cref="AbilityCellView.Build"/>), so the scene
        /// builder and the running game share ONE layout instead of two copies that can drift apart.
        ///
        /// The cell is the button, so the tap target is the full width instead of a small CAPTION-sized box.
        /// </summary>
        private static AbilityPanelUI.Row BuildAbilityButton(Transform parent, int index)
        {
            AbilityCellView cell = AbilityCellView.Build(parent, index);

            if (cell == null)
            {
                Debug.LogError("[MvpSceneBuilder] Could not build the ability cell.");
                return null;
            }

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
                TextAlignmentOptions.Center, UiTheme.Text);
            UiFactory.Stretch(label.rectTransform);

            SceneWiringUtility.SetField(textView, "label", label);
            SceneWiringUtility.SetField(textView, "rectTransform", template.GetComponent<RectTransform>());
            template.SetActive(false);

            GameObject poolObject = UiFactory.Node("DamageTextPool", damageRoot);
            FloatingDamageTextPool pool = poolObject.AddComponent<FloatingDamageTextPool>();
            SceneWiringUtility.SetField(pool, "prefab", textView);
            return pool;
        }

        /// <summary>
        /// A flat container band (header, stage row, log, camp strip) in the owner's container art: one face,
        /// one 1 px bottom line, no rivets. Falls back to the old kit plate when the sprite is missing.
        /// </summary>
        private static Image ContainerPanel(string name, Transform parent, bool raycast = false, Color? tint = null)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/IdleRPG/Art/UI/Containers/container_panel_tint.png");

            if (sprite == null)
            {
                Debug.LogWarning("[MvpSceneBuilder] Missing container sprite - run Tools > Idle RPG > Art > Apply UI Art Presets.");
                return KitPanel(name, parent, UiTheme.Panel, raycast);
            }

            Image image = UiFactory.Icon(name, parent, tint ?? UiTheme.ContainerFace, false);
            IdleRPG.UI.UiParts.AddShadow(image.gameObject);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f / 7f;
            image.raycastTarget = raycast;
            UiFactory.Stretch(image.rectTransform);
            return image;
        }

        /// <summary>Combat feed strip between the viewport and the control dock.</summary>
        private static CombatLogUI BuildCombatLog(RectTransform pageRoot)
        {
            Image strip = ContainerPanel("CombatLog", pageRoot, raycast: true);
            UiFactory.Anchor(strip.rectTransform, Vector2.zero, new Vector2(1f, LogTop), UiSpace.PageMargin, UiSpace.Space8, UiSpace.PageMargin, UiSpace.Space8);
            CombatLogUI ui = strip.gameObject.AddComponent<CombatLogUI>();

            ScrollRect scroll = UiFactory.CreateScrollView(strip.transform, "Scroll", UiSpace.Space2, UiSpace.Padding(UiSpace.Space8, UiSpace.Space8), out RectTransform content, autoSizeContent: false);

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

            // One ink for the whole log (owner, 2026-11-23): white like every other label.
            SceneWiringUtility.SetField(ui, "ink", UiTheme.Text);
            return ui;
        }
    }
}
