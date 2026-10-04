# Refactor Plan (run mid-cycle, Apr 2026)

Goal: modularize the Idle RPG codebase for maintainability - de-duplicate transient-UI
machinery, split the big files, consolidate item-visual vocabulary. **Zero behavior change.**
Every step: compile clean -> smoke (boot, battle ticks, 3 tabs, dropdown/sheet, leaderboard,
overlay) -> commit + push.

## Phase A - shared UI kit (DONE, commit `1a46ae0`)

New `Scripts/UI/Common/` kit, used by every transient widget:

| File | What it owns |
|---|---|
| `UiTransient` | the close discipline: `SetActive(false)` FIRST, then `Destroy`; plus the full-page transparent tap-catcher builder. Root-cause fix for the recurring "invisible tap-swallowing / fall-through" bugs caused by `raycastTarget = false` panels + deferred `Destroy`. |
| `UiDockedSheet` | frame + X + content root for docked, non-modal bottom sheets (item sheet, leaderboard). Only the frame's rect eats taps. |
| `DropdownMenu` | opening button + transient option menu: outside-tap dismiss, pressed tint, selected marker, one menu at a time. |
| `ItemVisuals` | the item vocabulary in one home: rarity -> color, slot -> letter/name/stat label, class -> letter. Roster and Inventory both read it. |

Retrofitted consumers: `InventoryTabUI` (dropdowns + item sheet), `LeaderboardSheetUI`
(whole sheet), `PartyPanelUI` gear row + `PartyRosterView` (ItemVisuals).

## Phase B - file splits (DONE, commit `1a46ae0`)

| Before | After |
|---|---|
| `ItemService` 647 lines | `ItemService` 295 + `ItemService.Drops` (boss drop + routing) + `ItemService.Save` (schema v6) - two partials. |
| `PartyPanelUI` 635 lines | `PartyPanelUI` 271 (tabs + wiring only) + `PartyRosterView` 433 (cards, big portrait + stats, gear preview). |
| `InventoryTabUI` 629 lines | 481 (shed its hand-rolled dropdowns/sheet onto the kit; geometry + behaviour unchanged). |
| `LeaderboardSheetUI` 629 lines | 178 (whole docked sheet via the kit). |

## Phase C - DevOverlay + assembly (DONE for DevOverlay/Utils; full graph DEFERRED)

- `DevOverlay` 419 -> 218 + `DevOverlay.Readout` (RefreshText + money/ad lines). Same take
  as the rest: readout is the change-churn half; the core keeps host/visibility/lifecycle.
- `IdleRPG.Utils` assembly: `NumberFormatter` is a true leaf (no IdleRPG refs), split out so
  formatting tweaks stop recompiling the world. Runtime + Editor reference it explicitly.

### Assembly split - why the full graph stays deferred (one hard blocker)

Measured: `Data/` references `IdleRPG.Economy` (`PrestigeUpgradeData` -> `CurrencyType`) and
`IdleRPG.Progression`, so `Data` is not a leaf. `Core` (composition root) is the top of every
dependency cone: `GameManager` instantiates `DevOverlay` (`Debug/`) and `PartyPanelUI`
(`UI/`), so neither folder can leave `Runtime` without a reflection seam or dependency
surgery - i.e. a multi-hour refactor with real regression risk for zero runtime change.

Deferred migration path (only if compile isolation ever dominates):
1. Hoist `CurrencyType` into `IdleRPG.Data` (or a new `IdleRPG.Shared`) so `Data` becomes a
   leaf; then `Save`, `Economy`, `Progression` as leaves over Data+Sim.
2. Replace `GameManager`'s direct `DevOverlay`/UI creates with a small `IBootPresenter`
   hook (or reflection) so `Core` no longer names UI types.
3. Then `UI` (+`Debug`) become the single top assembly referencing all leaves.

## Deliberately NOT split (now)

- `CombatSimulator` 349, `CharacterAnimator` 324, `SaveData` 411, `BalanceConfig` 386,
  `GameManager` 379 - each cohesive and under the old 500+ line pain threshold; forced
  splits would add indirection without a maintenance payoff.
- Editor tooling files (scene builders, balance/assignment menus) - cohesive, already
  isolated in `Editor/`.

## House verification checklist (every phase)

1. Recompile clean (0 errors) - console `groundTruth.compilationFailed == false`.
2. Play session: boot, battle ticks, F3 overlay reads, all three Party tabs change and
   refresh, roster selects + stat readouts + gear preview, inventory slot/sort dropdowns
   open/select/dismiss, item sheet opens + equip/discard (two-tap), leaderboard opens + rows.
3. Targeted eval probes assert live state (dropdown overlays, filter values, sheet labels,
   row counts).
4. Commit is behavior-identical to the pre-change commit.