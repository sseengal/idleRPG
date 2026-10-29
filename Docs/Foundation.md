# Foundation — the locked architecture

> **Status:** Phase 2C complete, 29 Oct 2026. Phase 1 owner-confirmed.
> **Scope:** the decisions that every later visual and feature change must follow.
> **Parent:** `Architecture.md`. This page does not repeat balance numbers or screen measurements.

---

## 1. The decision

The long-term route is:

**URP 2D + camera-rendered battle stage + hybrid prefabs + services + instance event hub.**

The cheaper route was rejected: UI-only arena effects cannot be clipped to the battle viewport.

## 2. What each layer owns

| Layer | Owns | Does not own |
|---|---|---|
| `IdleRPG.Sim` | Deterministic combat, encounters, stats | Unity objects, scenes, UI |
| Application services | Run, stage flow, combat, camp, economy, save | Drawing, layout, input |
| Battle stage | Backdrop, heroes, enemies, damage numbers, camp view | Header, nav, ability strip, log |
| HUD overlay | Header, ability strip, log, nav, modals | Battle-stage effects |
| Data assets | Balance, content, tuning | Runtime state |

## 3. Pipeline truth

- Unity 6000.6.0f1.
- URP 2D is the only pipeline.
- Mobile quality uses `Assets/Settings/Mobile_RPAsset.asset` with `Mobile_Renderer2D.asset`.
- PC quality uses `Assets/Settings/PC_RPAsset.asset` with `PC_Renderer2D.asset`.
- The old 3D `Mobile_Renderer.asset` and `PC_Renderer.asset` are removed, not kept as a fallback.
- The arena now renders through `BattleStageCamera` into `BattleStageRT`. The HUD shows that texture in the viewport.

## 4. Non-negotiable rules

1. One pipeline. Never run Built-in and URP together.
2. One battle surface. Effects apply to the stage RenderTexture, never to the whole screen.
3. No HUD chrome inside the stage. The formation board is the only interactive stage content; viewport taps cross the texture through `BattleStageSurface`'s input bridge.
4. Prefabs for reusable views. The scene builder owns the shell, bands and safe areas.
5. `GameManager` is a facade. New flow goes into a service.
6. Cross-system events use the event hub. Local UI actions use direct calls.
7. Save changes ship with a migration and a round-trip test.
8. Every phase stays shippable.

## 5. Phases

| Phase | Work | Done when |
|---|---|---|
| 0 | Truth pass, URP 2D renderers, baseline | Look unchanged; build and checks pass |
| 1 | Battle stage camera + RenderTexture | **Complete 28 Oct 2026** — battle page renders in the stage; effects stay inside it |
| 2 | Prefab hybrid | **In progress** — enemy, hero and ability cells are prefabs; camp widgets next |
| 3 | Services + event hub | Run and camp flow testable without the HUD |
| 4 | Data split + save migrations | Old saves load; content is data-only |
| 5 | Tests and polish | PlayMode smoke, screenshots, performance, a11y, l10n |

## 6. Phase 0 record

- Baseline captured at 1080 × 1920 before the pipeline switch.
- Both quality levels now use a 2D renderer.
- `Main Camera` still relies on URP's runtime-added camera data.
- Docs corrected: save v11, current page bands, pipeline.
- No gameplay code changed.

## 7. Phase 1 record

- `BattleStage` is a separate root: Screen Space - Camera canvas, UI layer, 1080 × 1920 reference and match-width scaling.
- `BattleStageCamera` is a root object (not a canvas child), orthographic, depth −2, UI culling mask, with `UniversalAdditionalCameraData` added by name so the editor assembly needs no URP reference.
- `BattleStageSurface` on `StageDisplay` creates a runtime RenderTexture at the viewport's pixel size, assigns it to the camera and the RawImage, and recreates it when the viewport resizes.
- The authored viewport content moves into the stage in its original sibling order: backdrop, formation board, enemy stack, camp view. The RawImage stays in the HUD viewport behind a `RectMask2D`.
- Damage numbers move into the stage, so their unit anchors and their parent share one coordinate space. The gold flight stays on `DamageCanvas`, because it must cross from the stage to the header; `BattleStageSurface.TryStageToScreen` converts its start point.
- Input bridge: the stage raycaster is disabled for normal EventSystem use. A click on the viewport RawImage maps screen → texture pixels, raycasts the stage canvas, then bubbles the click to the Button on the hit area's parent. Formation tap-to-move works; no other stage control exists.
- Lunge stop distance: body widths and unit positions are world-space, so the 18-design-unit contact gap is converted by `BattleSpots.PaddingFor` instead of being added raw. Without that conversion the scaled stage threw attackers outside the viewport. Verified live in stage world bounds for a hero swing and an enemy swing.
- Enemy first layout: `EnemyStackView` re-runs its layout when the measured column height becomes valid, and re-reads the wave when the camp re-enables the stack. Before this, the first refresh used the pre-scale height (1574 vs 705) and left enemies at the wrong offset until the next wave.
- `ScreenController` hides the stage root while the management page is open.
- Verified at 1080 × 1920: battle, boss wave and camp render in the stage; the header, ability strip, log and nav stay on the HUD. Formation hero selection works through the bridge. Damage numbers parent to `BattleStage`. EditMode tests 29/29 pass.
- Performance reading (Editor, not a device): 28 draw calls, 16 set-pass calls, 1,546 triangles, against the Phase 0 baseline of 25 / 15 / 907.
- Open: low-end phone profile; rotation/resize smoke test; future stage effects (CRT etc.) still wait for the Phase 5 stage pass.

## 8. Phase 2 record

### 2A — enemy slot prefab

- `Assets/IdleRPG/Prefabs/UI/Battle/EnemyUnitSlot.prefab` owns the enemy icon, name label, HP bar and boss slot.
- The prefab carries the wired `EnemyUnitView` and `HpBarView` references; the scene builder no longer constructs that hierarchy by code.
- `MvpSceneBuilder.BuildEnemySlot` instantiates three connected prefab instances under `EnemyStack`; it fails loudly if the prefab is missing.
- `EnemyStackView` still owns the anchor, size, stagger and wave count, so prefab and layout code keep separate jobs.
- Verified: two-enemy wave renders with correct stack positions; prefab references survive scene save and play.

### 2B — hero view prefab

- `Assets/IdleRPG/Prefabs/UI/Battle/HeroUnitSlot.prefab` owns the icon, transparent hit area, name label, HP bar and `CanvasGroup`.
- The root carries the `Button`; `FormationBoardView` binds its tap at runtime and still owns pads, selection and board layout.
- `FormationBoardView` now has a serialized `heroPrefab`; the scene builder wires it. The old hand-built hero hierarchy and `BuildHpBar` were removed.
- Verified: three heroes instantiate and render; tap selection changes `selectedHero`; lunge contact remains inside stage bounds.

### 2C — ability cell prefab

- `Assets/IdleRPG/Prefabs/UI/Battle/AbilityCell.prefab` owns the portrait, name, ability name, clock and cooldown fill.
- `AbilityCellView` carries those references and builds the existing `AbilityPanelUI.Row` at scene build time. The panel still updates every value each frame.
- `MvpSceneBuilder.BuildAbilityButton` instantiates the prefab, names it `Hero{index}` and sets the three equal-width anchors. The old hand-built cell code was removed.
- Verified: three cells instantiate; all row references are non-null; a cast request reaches the fight and starts the cooldown.
