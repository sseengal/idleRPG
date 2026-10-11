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

## Phase D - GameManager (analysed, NOT started)

`GameManager` is 1520 lines across six partial files, and `Architecture.md` §1.2 D1 already flags it: *"Every
feature edits the same file."* The partial split hides that - the six files are one class that owns the
composition root, the state machine, saving, offline pay, monetisation and lifetime stats. The target names
already exist in `Architecture.md` §2.1 (`SaveCoordinator`, `OfflineCoordinator`, `RunController`, `GameContext`);
`GameContext` and `RunController` are already real.

### What the analysis found (2026-10-11), before any code was moved

1. **The initialisation order is load-bearing and was undocumented.** Three separate constraints, now written
   into `GameManager.Wiring.cs` above `WireUp()`:
   - `Formation` and `SaveManager` must exist before the load (the load restores the party layout and the shop's
     offline cap bonus).
   - `IdleTimeService` must be created **after** the load: it copies `Shop.OfflineCapBonusSeconds` into its own
     cap, so creating it earlier silently ignores a bought cap extension.
   - `Context` is filled **last** because it needs every service, `Save` and `Idle` included - so nothing may
     read `Context` during the load. The backup-recovery path saves from inside `WireUp()`.
2. **Every extraction therefore needs either an explicit dependency bundle or an order change.** The snapshot
   capture is the obvious first candidate (pure mapping, and the save is the one surface that must never
   regress) but it cannot take `GameContext` without hitting constraint 1.3.
3. **The god object has no cheap seam.** Everything hangs off `GameManager`, so each extraction carries
   plumbing. That is the finding, not a reason to force it.

### Planned order (each its own commit, each verified)

| Step | What | Why first / risk |
|---|---|---|
| D1 | **DONE** (`f7b0ae2`) Snapshot mapping -> `SaveSnapshotMapper`, taking an explicit service bundle built at the call site + a `RunState` struct | Pure mapping, no control flow. `GameManager.Snapshot.cs` 141 -> 124, mapper 174 - net lines go UP by ~90 and that is the honest cost; the win is the seam, not the count. Proof: old and new bodies normalise to 76 identical lines; a live capture -> apply -> capture round-trip is byte-identical. |
| D2 | **DONE** Offline pay -> `OfflineCoordinator` (offline window, instant income, the logout-stamp rule) | `GameManager.Save.cs` 214 -> 145, coordinator 167 - net lines go UP again (~+100) for the same reason as D1. Proof: the moved bodies normalise to 62 byte-identical lines (only the two private helpers swapped order); a fixed-clock run (logout stamp an hour back) pays 991.33 gold, the hand check `3600.1s x 0.3934/s x 0.7 x 1.0` matches to the cent, the claim adds the same to the wallet AND to the lifetime total (proving the moved `RewardPaid` hook), the window is consumed so a second call pays nothing, and both guards (under the 30s floor, no stamp) refuse. |
| D3 | **DONE** Monetisation -> `MonetizationService` (ad gate, IAP register, gold-boost ad) | `GameManager.Monetization.cs` 149 -> 51, service 172 - net lines up again (~+80). Nothing here subscribes to events, so no teardown moved. Proof: the moved surface normalises to 98 byte-identical lines; a live run drove every synchronous branch and produced the old toasts exactly - ad started (no toast), the same ad still playing -> "Ad not ready yet.", no-ads owned -> "Ads removed - enjoy the quiet!", placement just marked shown -> "Ad recently used - try again in about 5 min.", unknown sku -> "Unknown product.". The two async grant bodies (ad completion, IAP grant) are covered by the line-for-line proof instead. |
| D4 | Stage/combat flow -> a stage-flow controller | The event-wiring heart. Last, and only with its own plan. |

### Finding recorded during D2, not fixed (pre-existing)

`IdleTimeService.Evaluate()` clears `Pending` on entry. So calling `GameManager.EvaluateOffline()` while the offline
popup is open **cancels the pending claim**: the next claim finds nothing pending and pays nothing (it logs a
warning). The debug hotkey and the save-debug menu both call `EvaluateOffline()` on demand, so this is reachable
today. It is not a defect introduced by this refactor, and it was left alone - but a debug re-run while the popup is
open will silently void the popup's claim.

**Not planned:** flattening the ~40 service properties (a facade is the right shape, and touching every call
site is churn with no payoff); assembly re-splitting (already deferred in Phase C with measured reasons).

**Verification limit, stated plainly:** the golden-number net is currently red (the balance is still being
built), so it is not a usable signal for these steps. They lean on: compile, the 12 edit-mode tests, a
round-trip of a real save payload captured from the live game, and a play smoke.

## House verification checklist (every phase)

1. Recompile clean (0 errors) - console `groundTruth.compilationFailed == false`.
2. Play session: boot, battle ticks, F3 overlay reads, all three Party tabs change and
   refresh, roster selects + stat readouts + gear preview, inventory slot/sort dropdowns
   open/select/dismiss, item sheet opens + equip/discard (two-tap), leaderboard opens + rows.
3. Targeted eval probes assert live state (dropdown overlays, filter values, sheet labels,
   row counts).
4. Commit is behavior-identical to the pre-change commit.